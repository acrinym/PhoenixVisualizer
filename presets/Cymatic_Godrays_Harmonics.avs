[avs]
PRESET_NAME=Cymatic Godrays Harmonics
DESCRIPTION=Earth harmonics visualization with volumetric godrays and particle systems. Combines cymatic patterns, sacred light rays, and reactive particles for a mesmerizing spiritual experience.

[preset00]
n=400.000000
b=1.000000
sn=Superscope (Cymatic Earth Harmonics)
POINT
t=1.000000
dt=0.010000
RED=1.000000
GREEN=1.000000
BLUE=1.000000
INIT
n=1
t=0
PI=3.141592653589793
PHI=1.618033988749895
EARTH_FREQ=7.83

CODE
// Earth harmonics cymatic visualization
// Base frequency modulated by audio spectrum
base_freq = EARTH_FREQ + (getspec(0,0.1,0) * 10)
harmonic = (t * base_freq * 2 * PI) % (2 * PI)

// Generate cymatic nodal patterns using Chladni equations
// Chladni pattern: r = sqrt(n) * J_m(k*r) * cos(m*theta)
r = sqrt(n) * 0.1
theta = t * 4 + n * 0.1

// Calculate nodal displacement
displacement = cos(harmonic) * sin(theta * 3) * 0.3
radial_pattern = cos(r * base_freq * 2) * 0.2

// Combine patterns for final position
x = (cos(theta + displacement) * (0.3 + radial_pattern)) + (getosc(0,0.5,0) * 0.1)
y = (sin(theta + displacement) * (0.3 + radial_pattern)) + (getosc(0.25,0.5,0) * 0.1)

// Color based on harmonic series and audio energy
red = 0.5 + 0.5 * cos(harmonic * 2)
green = 0.3 + 0.7 * sin(harmonic * 3)
blue = 0.8 + 0.2 * cos(harmonic * 5)

// Brightness modulation by bass energy
brightness = 0.3 + (getspec(0,0.1,0) * 2)
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
n=400.000000
t=t+dt

[preset01]
n=300.000000
b=1.000000
sn=Superscope (Godrays Particles)
POINT
t=1.000000
dt=0.015000
RED=1.000000
GREEN=0.800000
BLUE=0.400000
INIT
n=1
t=0
ray_count=12
ray_angle=0

CODE
// Volumetric godrays with particle system
// Rays emanating from center with particle trails

ray_index = n % ray_count
base_angle = (ray_index / ray_count) * 2 * 3.14159 + ray_angle
distance = (n / 300) * 0.8

// Add audio-reactive modulation
audio_mod = getspec(0,0.2,0) * 2
angle_offset = audio_mod * sin(t * 3 + ray_index)

// Calculate ray position
ray_angle = base_angle + angle_offset
x = cos(ray_angle) * distance
y = sin(ray_angle) * distance

// Particle trail effect
trail_factor = 1 - (n / 300)
x = x * trail_factor
y = y * trail_factor

// Color gradient along rays (warm to cool)
color_pos = distance
red = 1.0 - color_pos * 0.3
green = 0.8 - color_pos * 0.2
blue = 0.4 + color_pos * 0.4

// Audio energy modulation
energy = getspec(0,0.1,0) * 3
red = red * (0.5 + energy * 0.5)
green = green * (0.5 + energy * 0.5)
blue = blue * (0.5 + energy * 0.5)

FRAME
ray_angle = ray_angle + 0.02
t=t+dt

[preset02]
n=200.000000
b=1.000000
sn=Superscope (Solfeggio Harmonics)
POINT
t=1.000000
dt=0.008000
RED=0.600000
GREEN=0.800000
BLUE=1.000000
INIT
n=1
t=0
// Solfeggio frequencies: 396, 417, 528, 639, 741, 852 Hz
SOLFREQS=6
SOLFREQ1=396
SOLFREQ2=417
SOLFREQ3=528
SOLFREQ4=639
SOLFREQ5=741
SOLFREQ6=852

CODE
// Solfeggio frequency visualization
// Six sacred tones arranged in harmonic pattern

tone_index = (n % SOLFREQS) + 1
freq = if(equal(tone_index,1),SOLFREQ1,if(equal(tone_index,2),SOLFREQ2,if(equal(tone_index,3),SOLFREQ3,if(equal(tone_index,4),SOLFREQ4,if(equal(tone_index,5),SOLFREQ5,SOLFREQ6)))))

// Calculate position based on frequency ratios
angle = (tone_index / SOLFREQS) * 2 * 3.14159 + t
radius = 0.2 + (freq / 1000) * 0.3

// Audio-reactive modulation
spec_mod = getspec(0,0.15,0) * 3
radius = radius + spec_mod * 0.1

x = cos(angle) * radius
y = sin(angle) * radius

// Color based on Solfeggio tone (ROYGBIV spectrum)
hue = (tone_index / SOLFREQS)
red = 0.5 + 0.5 * cos(hue * 2 * 3.14159)
green = 0.5 + 0.5 * cos((hue + 0.33) * 2 * 3.14159)
blue = 0.5 + 0.5 * cos((hue + 0.67) * 2 * 3.14159)

// Energy modulation
energy = getspec(0,0.1,0) * 2
red = red * (0.3 + energy)
green = green * (0.3 + energy)
blue = blue * (0.3 + energy)

FRAME
t=t+dt

[preset03]
n=150.000000
b=1.000000
sn=Superscope (Particle Swarm)
POINT
t=1.000000
dt=0.012000
RED=1.000000
GREEN=0.500000
BLUE=0.200000
INIT
n=1
t=0
swarm_size=50
particle_speed=2

CODE
// Particle swarm intelligence visualization
// Particles attracted to audio energy centers

particle_id = n % swarm_size
group_angle = (particle_id / swarm_size) * 2 * 3.14159

// Audio energy centers
bass_center_x = cos(t * 2) * 0.3
bass_center_y = sin(t * 2) * 0.3
treble_center_x = cos(t * 3 + 3.14159) * 0.2
treble_center_y = sin(t * 3 + 3.14159) * 0.2

// Particle position with swarm behavior
base_x = cos(group_angle + t * particle_speed) * 0.4
base_y = sin(group_angle + t * particle_speed) * 0.4

// Attraction to audio centers
bass_energy = getspec(0,0.1,0) * 2
treble_energy = getspec(0.8,0.2,0) * 2

// Calculate attraction forces
bass_dx = bass_center_x - base_x
bass_dy = bass_center_y - base_y
treble_dx = treble_center_x - base_x
treble_dy = treble_center_y - base_y

// Apply forces
x = base_x + (bass_dx * bass_energy + treble_dx * treble_energy) * 0.1
y = base_y + (bass_dy * bass_energy + treble_dy * treble_energy) * 0.1

// Color based on dominant attraction
if (above(bass_energy, treble_energy),
  red = 1.0, green = 0.3, blue = 0.1,  // Warm colors for bass
  red = 0.2, green = 0.8, blue = 1.0   // Cool colors for treble
)

// Brightness modulation
brightness = 0.4 + (getspec(0,0.1,0) + getspec(0.8,0.2,0)) * 1.5
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[MAIN]
NUMFRAGS=4
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
