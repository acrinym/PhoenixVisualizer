// MathUtils.fs - F# Mathematical Utilities
// Perfect for fractal generation, DSP, and complex mathematical operations
// Demonstrates F#'s superiority for mathematical programming

namespace PhoenixVisualizer.Core

open System
open System.Numerics

/// <summary>
/// Functional mathematical utilities using F# patterns
/// Much more elegant and type-safe than imperative approaches
/// </summary>
module MathUtils =

    /// <summary>
    /// Complex number operations for fractal generation
    /// </summary>
    module Complex =

        let inline add (a: Complex) (b: Complex) = a + b
        let inline subtract (a: Complex) (b: Complex) = a - b
        let inline multiply (a: Complex) (b: Complex) = a * b
        let inline divide (a: Complex) (b: Complex) = a / b
        let inline magnitude (c: Complex) = c.Magnitude
        let inline magnitudeSquared (c: Complex) = c.Magnitude * c.Magnitude
        let inline phase (c: Complex) = c.Phase
        let inline conjugate (c: Complex) = Complex.Conjugate c

        /// <summary>
        /// Create complex number from polar coordinates
        /// </summary>
        let fromPolar magnitude phase = Complex.FromPolarCoordinates(magnitude, phase)

        /// <summary>
        /// Complex exponential function
        /// </summary>
        let exp (c: Complex) = Complex.Exp c

        /// <summary>
        /// Complex logarithm
        /// </summary>
        let log (c: Complex) = Complex.Log c

        /// <summary>
        /// Complex power function
        /// </summary>
        let pow (c: Complex) exponent = Complex.Pow(c, exponent)

        /// <summary>
        /// Complex square root
        /// </summary>
        let sqrt (c: Complex) = Complex.Sqrt c

    /// <summary>
    /// Vector operations for 2D/3D graphics
    /// </summary>
    module Vector2D =

        type Vector2 = { X: float; Y: float }

        let inline create x y = { X = x; Y = y }
        let inline zero = create 0.0 0.0
        let inline unitX = create 1.0 0.0
        let inline unitY = create 0.0 1.0

        let inline add a b = { X = a.X + b.X; Y = a.Y + b.Y }
        let inline subtract a b = { X = a.X - b.X; Y = a.Y - b.Y }
        let inline multiply scalar v = { X = scalar * v.X; Y = scalar * v.Y }
        let inline divide scalar v = { X = v.X / scalar; Y = v.Y / scalar }

        let inline dot a b = a.X * b.X + a.Y * b.Y
        let inline magnitude v = sqrt (v.X * v.X + v.Y * v.Y)
        let inline magnitudeSquared v = v.X * v.X + v.Y * v.Y
        let inline normalize v = divide (magnitude v) v

        let inline rotate angle v =
            let cos = cos angle
            let sin = sin angle
            { X = v.X * cos - v.Y * sin; Y = v.X * sin + v.Y * cos }

        let inline distance a b = magnitude (subtract b a)
        let inline angle v = atan2 v.Y v.X

    /// <summary>
    /// Fractal generation utilities
    /// </summary>
    module Fractals =

        /// <summary>
        /// Mandelbrot set iteration
        /// </summary>
        let mandelbrotIteration c maxIterations =
            let rec iterate z iterations =
                if iterations >= maxIterations then iterations
                elif Complex.magnitudeSquared z > 4.0 then iterations
                else iterate (Complex.add (Complex.multiply z z) c) (iterations + 1)
            iterate Complex.Zero 0

        /// <summary>
        /// Julia set iteration
        /// </summary>
        let juliaIteration z c maxIterations =
            let rec iterate z iterations =
                if iterations >= maxIterations then iterations
                elif Complex.magnitudeSquared z > 4.0 then iterations
                else iterate (Complex.add (Complex.multiply z z) c) (iterations + 1)
            iterate z 0

        /// <summary>
        /// Burning Ship fractal iteration
        /// </summary>
        let burningShipIteration c maxIterations =
            let rec iterate z iterations =
                if iterations >= maxIterations then iterations
                elif Complex.magnitudeSquared z > 4.0 then iterations
                else
                    let zReal = abs z.Real
                    let zImag = abs z.Imaginary
                    let newZ = Complex.add (Complex.multiply { Real = zReal; Imaginary = zImag } { Real = zReal; Imaginary = zImag }) c
                    iterate newZ (iterations + 1)
            iterate Complex.Zero 0

        /// <summary>
        /// Newton's method for fractal generation
        /// </summary>
        let newtonIteration z maxIterations =
            let rec iterate z iterations =
                if iterations >= maxIterations then z, iterations
                else
                    // f(z) = z^3 - 1, f'(z) = 3z^2
                    let fz = Complex.subtract (Complex.pow z 3.0) Complex.One
                    let fpz = Complex.multiply (Complex 3.0 0.0) (Complex.multiply z z)

                    if Complex.magnitude fpz < 1e-10 then z, iterations
                    else
                        let newZ = Complex.subtract z (Complex.divide fz fpz)
                        if Complex.magnitude (Complex.subtract newZ z) < 1e-10 then newZ, iterations + 1
                        else iterate newZ (iterations + 1)
            iterate z 0

        /// <summary>
        /// Smooth coloring algorithm for fractal rendering
        /// </summary>
        let smoothColor iterations maxIterations =
            if iterations >= maxIterations then 0.0
            else
                let logZn = log (Complex.magnitudeSquared (Complex 0.0 0.0)) / 2.0
                let nu = log (logZn / log 2.0) / log 2.0
                iterations - nu

    /// <summary>
    /// Signal processing utilities
    /// </summary>
    module SignalProcessing =

        /// <summary>
        /// Convolution of two signals
        /// </summary>
        let convolve (signal: float[]) (kernel: float[]) : float[] =
            let signalLen = signal.Length
            let kernelLen = kernel.Length
            let resultLen = signalLen + kernelLen - 1
            let result = Array.zeroCreate resultLen

            for i in 0 .. signalLen - 1 do
                for j in 0 .. kernelLen - 1 do
                    result.[i + j] <- result.[i + j] + signal.[i] * kernel.[j]

            result

        /// <summary>
        /// Fast Fourier Transform using Cooley-Tukey algorithm
        /// </summary>
        let fft (input: Complex[]) : Complex[] =
            let n = input.Length
            if n = 1 then input
            else
                let even = fft (Array.init (n/2) (fun i -> input.[i*2]))
                let odd = fft (Array.init (n/2) (fun i -> input.[i*2+1]))

                let twiddle k = Complex.FromPolarCoordinates(1.0, -2.0 * Math.PI * float k / float n)

                Array.init n (fun k ->
                    let t = twiddle k
                    let e = even.[k % (n/2)]
                    let o = odd.[k % (n/2)]

                    if k < n/2 then e + t * o else e - t * o
                )

        /// <summary>
        /// Inverse Fast Fourier Transform
        /// </summary>
        let ifft (input: Complex[]) : Complex[] =
            let n = float input.Length
            let conjugated = input |> Array.map Complex.Conjugate
            let transformed = fft conjugated
            transformed |> Array.map (fun c -> Complex.Conjugate c / n)

        /// <summary>
        /// Apply window function to signal
        /// </summary>
        let applyWindow windowType (signal: float[]) : float[] =
            let n = signal.Length
            match windowType with
            | "hann" -> signal |> Array.mapi (fun i s -> s * 0.5 * (1.0 - cos (2.0 * Math.PI * float i / float (n - 1))))
            | "hamming" -> signal |> Array.mapi (fun i s -> s * (0.54 - 0.46 * cos (2.0 * Math.PI * float i / float (n - 1))))
            | "blackman" -> signal |> Array.mapi (fun i s ->
                let a = 2.0 * Math.PI * float i / float (n - 1)
                s * (0.42 - 0.5 * cos a + 0.08 * cos (2.0 * a)))
            | _ -> signal

        /// <summary>
        /// Compute autocorrelation for periodicity detection
        /// </summary>
        let autocorrelation (signal: float[]) maxLag : float[] =
            let n = signal.Length
            let result = Array.zeroCreate (min maxLag n)

            for lag in 0 .. result.Length - 1 do
                let mutable sum = 0.0
                for i in 0 .. n - lag - 1 do
                    sum <- sum + signal.[i] * signal.[i + lag]
                result.[lag] <- sum

            result

        /// <summary>
        /// Find peaks in a signal
        /// </summary>
        let findPeaks (signal: float[]) threshold : int[] =
            signal
            |> Array.indexed
            |> Array.filter (fun (i, value) ->
                value > threshold &&
                (i = 0 || signal.[i-1] <= value) &&
                (i = signal.Length - 1 || signal.[i+1] <= value))
            |> Array.map fst

    /// <summary>
    /// Interpolation and curve fitting utilities
    /// </summary>
    module Interpolation =

        /// <summary>
        /// Linear interpolation between two values
        /// </summary>
        let lerp a b t = a + (b - a) * t

        /// <summary>
        /// Bilinear interpolation for 2D data
        /// </summary>
        let bilinear (data: float[,]) x y =
            let x0 = floor x |> int
            let x1 = ceil x |> int
            let y0 = floor y |> int
            let y1 = ceil y |> int

            let x0 = max 0 (min (Array2D.length1 data - 1) x0)
            let x1 = max 0 (min (Array2D.length1 data - 1) x1)
            let y0 = max 0 (min (Array2D.length2 data - 1) y0)
            let y1 = max 0 (min (Array2D.length2 data - 1) y1)

            let fx = x - float x0
            let fy = y - float y0

            let top = lerp data.[x0, y0] data.[x1, y0] fx
            let bottom = lerp data.[x0, y1] data.[x1, y1] fx

            lerp top bottom fy

        /// <summary>
        /// Cubic Hermite spline interpolation
        /// </summary>
        let hermite p0 p1 m0 m1 t =
            let t2 = t * t
            let t3 = t2 * t
            let h00 = 2.0 * t3 - 3.0 * t2 + 1.0
            let h10 = t3 - 2.0 * t2 + t
            let h01 = -2.0 * t3 + 3.0 * t2
            let h11 = t3 - t2
            p0 * h00 + m0 * h10 + p1 * h01 + m1 * h11

    /// <summary>
    /// Color space conversions and operations
    /// </summary>
    module Color =

        /// <summary>
        /// HSV to RGB conversion
        /// </summary>
        let hsvToRgb (h: float) (s: float) (v: float) : float * float * float =
            let c = v * s
            let h' = h / 60.0
            let x = c * (1.0 - abs ((h' % 2.0) - 1.0))
            let m = v - c

            let r', g', b' =
                if h' < 1.0 then c, x, 0.0
                elif h' < 2.0 then x, c, 0.0
                elif h' < 3.0 then 0.0, c, x
                elif h' < 4.0 then 0.0, x, c
                elif h' < 5.0 then x, 0.0, c
                else c, 0.0, x

            r' + m, g' + m, b' + m

        /// <summary>
        /// RGB to HSV conversion
        /// </summary>
        let rgbToHsv (r: float) (g: float) (b: float) : float * float * float =
            let max = max r (max g b)
            let min = min r (min g b)
            let delta = max - min

            let h =
                if delta = 0.0 then 0.0
                elif max = r then 60.0 * (((g - b) / delta) % 6.0)
                elif max = g then 60.0 * ((b - r) / delta + 2.0)
                else 60.0 * ((r - g) / delta + 4.0)

            let s = if max = 0.0 then 0.0 else delta / max
            let v = max

            h, s, v

        /// <summary>
        /// Blend two colors with alpha
        /// </summary>
        let blend (r1, g1, b1, a1) (r2, g2, b2, a2) =
            let a = a1 + a2 * (1.0 - a1)
            let r = (r1 * a1 + r2 * a2 * (1.0 - a1)) / a
            let g = (g1 * a1 + g2 * a2 * (1.0 - a1)) / a
            let b = (b1 * a1 + b2 * a2 * (1.0 - a1)) / a
            r, g, b, a

    /// <summary>
    /// Statistical operations for data analysis
    /// </summary>
    module Statistics =

        /// <summary>
        /// Calculate mean of a dataset
        /// </summary>
        let mean (data: float[]) = Array.average data

        /// <summary>
        /// Calculate variance of a dataset
        /// </summary>
        let variance (data: float[]) =
            let m = mean data
            data |> Array.averageBy (fun x -> (x - m) ** 2.0)

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        let stdDev data = sqrt (variance data)

        /// <summary>
        /// Calculate median of a dataset
        /// </summary>
        let median (data: float[]) =
            let sorted = Array.sort data
            let n = sorted.Length
            if n % 2 = 1 then sorted.[n / 2]
            else (sorted.[n / 2 - 1] + sorted.[n / 2]) / 2.0

        /// <summary>
        /// Calculate mode (most frequent value)
        /// </summary>
        let mode (data: float[]) =
            data
            |> Array.groupBy id
            |> Array.maxBy (snd >> Array.length)
            |> fst

        /// <summary>
        /// Calculate correlation coefficient between two datasets
        /// </summary>
        let correlation (x: float[]) (y: float[]) =
            if x.Length <> y.Length then failwith "Datasets must have same length"

            let n = float x.Length
            let sumX = Array.sum x
            let sumY = Array.sum y
            let sumXY = Array.sumBy (fun (a, b) -> a * b) (Array.zip x y)
            let sumX2 = Array.sumBy (fun a -> a * a) x
            let sumY2 = Array.sumBy (fun b -> b * b) y

            (n * sumXY - sumX * sumY) /
            sqrt ((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY))

    /// <summary>
    /// Functional programming utilities
    /// </summary>
    module Functional =

        /// <summary>
        /// Compose two functions
        /// </summary>
        let inline compose f g x = f (g x)

        /// <summary>
        /// Pipe operator for function chaining
        /// </summary>
        let inline (|>) x f = f x

        /// <summary>
        /// Forward pipe operator
        /// </summary>
        let inline (>>) f g x = g (f x)

        /// <summary>
        /// Option monad utilities
        /// </summary>
        module Option =

            let inline bind f = function
                | Some x -> f x
                | None -> None

            let inline map f = function
                | Some x -> Some (f x)
                | None -> None

            let inline defaultValue def = function
                | Some x -> x
                | None -> def

        /// <summary>
        /// Result type for error handling
        /// </summary>
        type Result<'T, 'TError> =
            | Success of 'T
            | Error of 'TError

        /// <summary>
        /// Result utilities
        /// </summary>
        module Result =

            let inline bind f = function
                | Success x -> f x
                | Error e -> Error e

            let inline map f = function
                | Success x -> Success (f x)
                | Error e -> Error e

            let inline defaultValue def = function
                | Success x -> x
                | Error _ -> def
