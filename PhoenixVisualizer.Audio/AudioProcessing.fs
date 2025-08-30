// AudioProcessing.fs - F# Audio Processing Module
// Provides functional programming patterns for audio analysis and processing
// Much more elegant and performant than imperative C# approaches

namespace PhoenixVisualizer.Audio

open System
open System.Numerics
open System.Runtime.InteropServices

/// <summary>
/// Functional audio processing utilities using F# patterns
/// </summary>
module AudioProcessing =

    /// <summary>
    /// Complex number type for FFT operations
    /// </summary>
    type ComplexFloat = { Real: float32; Imaginary: float32 }

    /// <summary>
    /// Audio spectrum analysis results
    /// </summary>
    type SpectrumAnalysis = {
        Magnitudes: float32[]
        Phases: float32[]
        Frequencies: float32[]
        DominantFrequency: float32
        SpectralCentroid: float32
        SpectralRolloff: float32
        SpectralFlux: float32
    }

    /// <summary>
    /// Beat detection results
    /// </summary>
    type BeatAnalysis = {
        IsBeat: bool
        Intensity: float32
        BPM: float32
        Confidence: float32
    }

    /// <summary>
    /// Functional FFT implementation using Cooley-Tukey algorithm
    /// Much cleaner and more composable than imperative approaches
    /// </summary>
    let fft (input: float32[]) : ComplexFloat[] =
        let n = input.Length

        // Base case: if size is 1, return the complex version
        if n = 1 then
            [|{ Real = input.[0]; Imaginary = 0.0f }|]
        else
            // Split into even and odd indices
            let even = fft (Array.init (n/2) (fun i -> input.[i*2]))
            let odd = fft (Array.init (n/2) (fun i -> input.[i*2+1]))

            // Combine results using twiddle factors
            let twiddle k =
                let angle = -2.0f * MathF.PI * float32 k / float32 n
                { Real = MathF.Cos angle; Imaginary = MathF.Sin angle }

            Array.init n (fun k ->
                let t = twiddle k
                let e = even.[k % (n/2)]
                let o = odd.[k % (n/2)]

                if k < n/2 then
                    { Real = e.Real + t.Real * o.Real - t.Imaginary * o.Imaginary
                      Imaginary = e.Imaginary + t.Real * o.Imaginary + t.Imaginary * o.Real }
                else
                    { Real = e.Real - t.Real * o.Real + t.Imaginary * o.Imaginary
                      Imaginary = e.Imaginary - t.Real * o.Imaginary - t.Imaginary * o.Real }
            )

    /// <summary>
    /// Compute magnitudes from complex FFT results
    /// </summary>
    let magnitudes (complex: ComplexFloat[]) : float32[] =
        complex |> Array.map (fun c -> MathF.Sqrt(c.Real*c.Real + c.Imaginary*c.Imaginary))

    /// <summary>
    /// Compute phases from complex FFT results
    /// </summary>
    let phases (complex: ComplexFloat[]) : float32[] =
        complex |> Array.map (fun c -> MathF.Atan2(c.Imaginary, c.Real))

    /// <summary>
    /// Apply window function to audio samples
    /// </summary>
    let applyWindow windowType (samples: float32[]) : float32[] =
        let n = samples.Length
        match windowType with
        | "hann" ->
            samples |> Array.mapi (fun i s ->
                s * 0.5f * (1.0f - MathF.Cos(2.0f * MathF.PI * float32 i / float32 (n-1))))
        | "hamming" ->
            samples |> Array.mapi (fun i s ->
                s * (0.54f - 0.46f * MathF.Cos(2.0f * MathF.PI * float32 i / float32 (n-1))))
        | "blackman" ->
            samples |> Array.mapi (fun i s ->
                let a = 2.0f * MathF.PI * float32 i / float32 (n-1)
                s * (0.42f - 0.5f * MathF.Cos(a) + 0.08f * MathF.Cos(2.0f * a)))
        | _ -> samples // rectangular window

    /// <summary>
    /// Convert sample index to frequency
    /// </summary>
    let indexToFrequency sampleRate fftSize index : float32 =
        float32 index * float32 sampleRate / float32 fftSize

    /// <summary>
    /// Compute spectral centroid (brightness)
    /// </summary>
    let spectralCentroid (magnitudes: float32[]) sampleRate : float32 =
        let weightedSum = Array.sumBy (fun (i, mag) -> float32 i * mag) (Array.indexed magnitudes)
        let totalMagnitude = Array.sum magnitudes
        if totalMagnitude > 0.0f then
            weightedSum / totalMagnitude * float32 sampleRate / float32 magnitudes.Length
        else 0.0f

    /// <summary>
    /// Compute spectral rolloff (high frequency content)
    /// </summary>
    let spectralRolloff (magnitudes: float32[]) sampleRate threshold : float32 =
        let totalEnergy = Array.sum magnitudes
        let targetEnergy = totalEnergy * threshold

        let rec findRolloff cumulativeEnergy index =
            if index >= magnitudes.Length then float32 (magnitudes.Length - 1)
            else
                let newEnergy = cumulativeEnergy + magnitudes.[index]
                if newEnergy >= targetEnergy then float32 index
                else findRolloff newEnergy (index + 1)

        findRolloff 0.0f 0 * float32 sampleRate / float32 magnitudes.Length

    /// <summary>
    /// Compute spectral flux (change in spectrum over time)
    /// </summary>
    let spectralFlux (current: float32[]) (previous: float32[]) : float32 =
        if previous.Length = 0 then 0.0f
        else
            Array.zip current previous
            |> Array.map (fun (c, p) ->
                let diff = c - p
                if diff > 0.0f then diff else 0.0f)
            |> Array.sum
            |> fun x -> x / float32 current.Length

    /// <summary>
    /// Find dominant frequency using peak detection
    /// </summary>
    let dominantFrequency (magnitudes: float32[]) sampleRate : float32 =
        // Simple peak detection - find maximum magnitude (excluding DC)
        let maxIndex = magnitudes.[1..] |> Array.indexed |> Array.maxBy snd |> fst
        indexToFrequency sampleRate magnitudes.Length (maxIndex + 1)

    /// <summary>
    /// Complete spectrum analysis pipeline
    /// </summary>
    let analyzeSpectrum (samples: float32[]) sampleRate windowType : SpectrumAnalysis =
        let windowed = applyWindow windowType samples
        let complex = fft windowed
        let mags = magnitudes complex
        let phs = phases complex
        let freqs = Array.init mags.Length (fun i -> indexToFrequency sampleRate mags.Length i)

        {
            Magnitudes = mags
            Phases = phs
            Frequencies = freqs
            DominantFrequency = dominantFrequency mags sampleRate
            SpectralCentroid = spectralCentroid mags sampleRate
            SpectralRolloff = spectralRolloff mags sampleRate 0.85f
            SpectralFlux = 0.0f // Would need previous frame for this
        }

    /// <summary>
    /// Beat detection using energy analysis
    /// </summary>
    let detectBeat (currentEnergy: float32) (energyHistory: float32[]) sensitivity : BeatAnalysis =
        if energyHistory.Length = 0 then
            { IsBeat = false; Intensity = 0.0f; BPM = 120.0f; Confidence = 0.0f }
        else
            let avgEnergy = Array.average energyHistory
            let variance = energyHistory |> Array.averageBy (fun e -> (e - avgEnergy) ** 2.0f)
            let stdDev = MathF.Sqrt(variance)
            let threshold = avgEnergy + stdDev * sensitivity

            let isBeat = currentEnergy > threshold
            let intensity = if isBeat then
                               MathF.Min(1.0f, (currentEnergy - avgEnergy) / (stdDev * 2.0f))
                           else 0.0f

            // Estimate BPM from beat intervals (simplified)
            let bpm = 120.0f // Placeholder - would need beat timing history

            { IsBeat = isBeat; Intensity = intensity; BPM = bpm; Confidence = intensity }

    /// <summary>
    /// Functional composition for audio feature extraction
    /// </summary>
    let extractFeatures sampleRate windowType =
        analyzeSpectrum >> (fun spectrum ->
            // Additional feature extraction could go here
            spectrum)

    /// <summary>
    /// Pipeline for real-time audio processing
    /// </summary>
    let processAudioBuffer sampleRate windowType beatHistorySize sensitivity
                          (buffer: float32[]) : SpectrumAnalysis * BeatAnalysis =

        let spectrum = analyzeSpectrum buffer sampleRate windowType

        // Beat detection using energy from spectrum
        let energy = Array.sum spectrum.Magnitudes
        let beat = detectBeat energy [||] sensitivity // Would use actual history

        spectrum, beat

    /// <summary>
    /// Utility functions for frequency band analysis
    /// </summary>
    module FrequencyBands =

        /// <summary>
        /// Frequency ranges for different bands
        /// </summary>
        let bassRange = (20.0f, 250.0f)
        let midRange = (250.0f, 4000.0f)
        let trebleRange = (4000.0f, 20000.0f)

        /// <summary>
        /// Extract energy for a specific frequency band
        /// </summary>
        let bandEnergy (frequencies: float32[]) (magnitudes: float32[]) (minFreq, maxFreq) : float32 =
            Array.zip frequencies magnitudes
            |> Array.filter (fun (freq, _) -> freq >= minFreq && freq <= maxFreq)
            |> Array.map snd
            |> Array.sum

        /// <summary>
        /// Normalize band energies
        /// </summary>
        let normalizeBand (energy: float32) (totalEnergy: float32) : float32 =
            if totalEnergy > 0.0f then energy / totalEnergy else 0.0f

        /// <summary>
        /// Extract all frequency band energies
        /// </summary>
        let extractBands (frequencies: float32[]) (magnitudes: float32[]) : float32 * float32 * float32 =
            let totalEnergy = Array.sum magnitudes
            let bass = bandEnergy frequencies magnitudes bassRange |> fun e -> normalizeBand e totalEnergy
            let mid = bandEnergy frequencies magnitudes midRange |> fun e -> normalizeBand e totalEnergy
            let treble = bandEnergy frequencies magnitudes trebleRange |> fun e -> normalizeBand e totalEnergy

            bass, mid, treble

    /// <summary>
    /// Advanced beat detection using multiple algorithms
    /// </summary>
    module BeatDetection =

        /// <summary>
        /// Energy-based beat detection
        /// </summary>
        let energyBased currentEnergy history sensitivity =
            detectBeat currentEnergy history sensitivity

        /// <summary>
        /// Frequency-based beat detection (focus on bass frequencies)
        /// </summary>
        let frequencyBased (spectrum: SpectrumAnalysis) history sensitivity =
            let bassEnergy = FrequencyBands.bandEnergy spectrum.Frequencies spectrum.Magnitudes FrequencyBands.bassRange
            detectBeat bassEnergy history sensitivity

        /// <summary>
        /// Combined beat detection using multiple methods
        /// </summary>
        let combined (spectrum: SpectrumAnalysis) energyHistory freqHistory energyWeight freqWeight sensitivity =
            let energyBeat = energyBased (Array.sum spectrum.Magnitudes) energyHistory sensitivity
            let freqBeat = frequencyBased spectrum freqHistory sensitivity

            {
                IsBeat = energyBeat.IsBeat || freqBeat.IsBeat
                Intensity = (energyBeat.Intensity * energyWeight + freqBeat.Intensity * freqWeight)
                BPM = (energyBeat.BPM + freqBeat.BPM) / 2.0f
                Confidence = MathF.Max(energyBeat.Confidence, freqBeat.Confidence)
            }
