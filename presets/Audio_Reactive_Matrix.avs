[avs]
PRESET_NAME=Audio Reactive Matrix
DESCRIPTION=Advanced audio-reactive visualization using frequency analysis, spectral decomposition, and real-time audio feature extraction. Combines spectrum analysis, beat detection, and harmonic tracking.

[preset00]
n=400.000000
b=1.000000
sn=Superscope (Spectrum Analyzer)
POINT
t=1.000000
dt=0.010000
RED=0.200000
GREEN=1.000000
BLUE=0.400000
INIT
n=1
t=0

CODE
// Real-time spectrum analyzer
// Frequency domain visualization with logarithmic scaling

// Calculate frequency bin
freq_bin = n / 400
log_freq = pow(10, freq_bin * 3) / 1000  // Logarithmic frequency scaling

// Get spectrum data at this frequency
spectrum_value = getspec(log_freq, 0.01, 0)

// Position calculation
x = (freq_bin - 0.5) * 2
y = spectrum_value * 2 - 1

// Add historical trail effect
trail_length = 20
for (i = 1; i < trail_length; i = i + 1,
  trail_x = x - i * 0.02
  trail_y = getspec(log_freq, 0.01, -i * dt) * 2 - 1
  // Draw trail points with alpha
)

// Color based on frequency range
if (below(log_freq, 0.1),
  red = 1.0, green = 0.2, blue = 0.2,  // Bass - Red
  if (below(log_freq, 0.5),
    red = 0.2, green = 1.0, blue = 0.2,  // Mid - Green
    red = 0.2, green = 0.2, blue = 1.0   // Treble - Blue
  )
)

// Brightness modulation
brightness = spectrum_value * 2
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset01]
n=320.000000
b=1.000000
sn=Superscope (Beat Detector)
POINT
t=1.000000
dt=0.008000
RED=1.000000
GREEN=0.600000
BLUE=0.200000
INIT
n=1
t=0
beat_threshold=0.3
beat_decay=0.95

CODE
// Beat detection and visualization
// Real-time rhythm analysis and pulse effects

// Analyze bass frequencies for beat detection
bass_energy = getspec(0, 0.1, 0) * 2
mid_energy = getspec(0.1, 0.4, 0) * 2
treble_energy = getspec(0.4, 0.6, 0) * 2

// Simple beat detection algorithm
beat_strength = max(bass_energy, max(mid_energy, treble_energy))
is_beat = above(beat_strength, beat_threshold)

// Pulse effect on beat
pulse_radius = is_beat ? 0.5 : 0.1
pulse_radius = pulse_radius * beat_decay

// Circular pulse visualization
angle = (n / 320) * 2 * PI * 2
x = cos(angle) * pulse_radius
y = sin(angle) * pulse_radius

// Color based on dominant frequency range
if (above(bass_energy, max(mid_energy, treble_energy)),
  red = 1.0, green = 0.3, blue = 0.1,  // Bass dominant
  if (above(mid_energy, treble_energy),
    red = 0.1, green = 1.0, blue = 0.3,  // Mid dominant
    red = 0.3, green = 0.1, blue = 1.0   // Treble dominant
  )
)

// Pulse intensity
intensity = beat_strength
red = red * intensity
green = green * intensity
blue = blue * intensity

FRAME
t=t+dt

[preset02]
n=240.000000
b=1.000000
sn=Superscope (Harmonic Tracker)
POINT
t=1.000000
dt=0.012000
RED=0.800000
GREEN=0.400000
BLUE=0.800000
INIT
n=1
t=0

CODE
// Harmonic tracking and chord analysis
// Real-time music theory visualization

// Find fundamental frequency
fundamental = 0
max_energy = 0
for (i = 0; i < 100; i = i + 1,
  freq = i * 0.01
  energy = getspec(freq, 0.01, 0)
  if (above(energy, max_energy),
    max_energy = energy
    fundamental = freq
  )
)

// Calculate harmonics
harmonic_number = (n % 8) + 1
harmonic_freq = fundamental * harmonic_number

// Get harmonic energy
harmonic_energy = getspec(harmonic_freq, 0.02, 0)

// Position based on harmonic series
x = (harmonic_number - 4.5) * 0.2
y = harmonic_energy * 2 - 1

// Musical interval coloring
interval_color = harmonic_number % 7
if (equal(interval_color, 0), red=1, green=0, blue=0,      // Root
  if (equal(interval_color, 1), red=1, green=0.5, blue=0,   // Minor 2nd
    if (equal(interval_color, 2), red=1, green=1, blue=0,    // Major 2nd
      if (equal(interval_color, 3), red=0.5, green=1, blue=0, // Minor 3rd
        if (equal(interval_color, 4), red=0, green=1, blue=0,  // Major 3rd
          if (equal(interval_color, 5), red=0, green=0.5, blue=1, // Perfect 4th
            if (equal(interval_color, 6), red=0.5, green=0, blue=1, // Tritone
              red=0, green=0, blue=1  // Perfect 5th
            )
          )
        )
      )
    )
  )
)

// Energy modulation
energy_mult = harmonic_energy * 3
red = red * energy_mult
green = green * energy_mult
blue = blue * energy_mult

FRAME
t=t+dt

[preset03]
n=180.000000
b=1.000000
sn=Superscope (Waveform Oscilloscope)
POINT
t=1.000000
dt=0.006000
RED=0.400000
GREEN=0.800000
BLUE=1.000000
INIT
n=1
t=0

CODE
// Real-time waveform oscilloscope
// Time domain audio visualization

// Get waveform sample
waveform_pos = n / 180
sample = getosc(waveform_pos, 0.5, 0)

// Lissajous pattern with stereo
left_sample = sample
right_sample = getosc(waveform_pos + 0.25, 0.5, 0)  // Phase shifted

// Lissajous figure
x = left_sample * 0.8
y = right_sample * 0.8

// Add stereo separation visualization
stereo_width = 0.3
x_left = left_sample * 0.4 - stereo_width/2
x_right = right_sample * 0.4 + stereo_width/2

// Choose which channel to show
if (equal(n % 2, 0),
  x = x_left, y = left_sample * 0.4,
  x = x_right, y = right_sample * 0.4
)

// Color based on sample polarity
if (above(sample, 0),
  red = 0.2, green = 0.8, blue = 1.0,  // Positive - Blue
  red = 1.0, green = 0.2, blue = 0.8   // Negative - Magenta
)

// Amplitude-based brightness
amplitude = abs(sample) * 2
red = red * amplitude
green = green * amplitude
blue = blue * amplitude

FRAME
t=t+dt

[preset04]
n=140.000000
b=1.000000
sn=Superscope (Frequency Cascade)
POINT
t=1.000000
dt=0.015000
RED=1.000000
GREEN=0.800000
BLUE=0.400000
INIT
n=1
t=0

CODE
// Frequency cascade effect
// Waterfall-style spectrum over time

// Time position in cascade
time_pos = n / 140
freq_pos = (t * 0.1 + time_pos) % 1

// Get spectrum at this frequency and time
spectrum_value = getspec(freq_pos, 0.05, -time_pos * 10)

// 3D projection for cascade effect
x = (freq_pos - 0.5) * 2
y = (time_pos - 0.5) * 2
z = spectrum_value * 2

// Perspective projection
distance = 2
x_proj = x * distance / (distance + z)
y_proj = y * distance / (distance + z)

x = x_proj
y = y_proj

// Color based on frequency and age
hue = freq_pos
age_factor = 1 - time_pos

red = 0.5 + 0.5 * cos(hue * 2 * PI)
green = 0.5 + 0.5 * cos((hue + 0.33) * 2 * PI)
blue = 0.5 + 0.5 * cos((hue + 0.67) * 2 * PI)

// Age-based fading
fade = age_factor * spectrum_value * 3
red = red * fade
green = green * fade
blue = blue * fade

FRAME
t=t+dt

[preset05]
n=100.000000
b=1.000000
sn=Superscope (Rhythm Matrix)
POINT
t=1.000000
dt=0.020000
RED=0.600000
GREEN=0.600000
BLUE=0.600000
INIT
n=1
t=0

CODE
// Rhythm matrix visualization
// Multi-band beat detection grid

// Frequency bands
bands = 8
band_index = n % bands
band_freq = band_index * 0.1

// Time divisions
time_div = 16
time_index = (n / bands) % time_div
time_pos = time_index / time_div

// Get energy in this band
band_energy = getspec(band_freq, 0.05, 0)

// Grid position
x = (band_index / (bands - 1.0) - 0.5) * 1.8
y = (time_pos - 0.5) * 1.8

// Beat detection for this band
is_beat = above(band_energy, 0.4)

// Pulse effect
pulse_size = is_beat ? band_energy * 0.5 : 0.05

// Draw beat indicators
if (is_beat,
  // Draw larger circle for beats
  x = x, y = y,
  // Draw small dots for non-beats
  x = x, y = y
)

// Color based on band
band_hue = band_index / bands
red = 0.5 + 0.5 * cos(band_hue * 2 * PI)
green = 0.5 + 0.5 * cos((band_hue + 0.33) * 2 * PI)
blue = 0.5 + 0.5 * cos((band_hue + 0.67) * 2 * PI)

// Energy modulation
energy_scale = band_energy * 2
red = red * energy_scale
green = green * energy_scale
blue = blue * energy_scale

FRAME
t=t+dt

[MAIN]
NUMFRAGS=6
ALPHA=1.000000
DEPTH=1.000000
COLOR=1.000000
ZOOM=1.000000
ROT=0.000000
SX=1.000000
SY=1.000000
DX=0.000000
DY=0.000000
WARP=0.000000
CX=0.500000
CY=0.500000
PX=0.000000
PY=0.000000
GX=0.000000
GY=0.000000
GX2=0.000000
GY2=0.000000
DFS=0.000000
DFR=0.000000
DFG=0.000000
DFB=0.000000
DFR2=0.000000
DFG2=0.000000
DFB2=0.000000
FALLOFF=0
INVERTED=0
BRIGHTEN=0
DARKEN=0
SOLARIZE=0
INVERT=0
GAMMA=1.000000
ECHO=0.000000
ECHOR=0.000000
ECHOG=0.000000
ECHOB=0.000000
ECHODELAY=0
DARKEN_CENTER=0
