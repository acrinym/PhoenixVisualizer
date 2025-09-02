[avs]
PRESET_NAME=Quantum Shader Reactor
DESCRIPTION=Advanced GLSL-style shader effects with quantum field visualization, particle systems, and real-time audio-reactive geometry. Combines ray-marching, fluid dynamics, and quantum-inspired patterns.

[preset00]
n=350.000000
b=1.000000
sn=Superscope (Quantum Field)
POINT
t=1.000000
dt=0.014000
RED=0.200000
GREEN=0.800000
BLUE=1.000000
INIT
n=1
t=0
PI=3.141592653589793
E=2.718281828459045

CODE
// Quantum field visualization
// Wave function collapse and probability distributions

// Quantum numbers (n, l, m)
quantum_n = 1 + (n % 3)  // Principal quantum number
quantum_l = n % quantum_n  // Azimuthal quantum number
quantum_m = quantum_l - (n % (2*quantum_l + 1))  // Magnetic quantum number

// Calculate electron orbital positions using quantum mechanics
r = (quantum_n * quantum_n) * 0.1 * (1 + 0.1 * cos(t * 2))
theta = acos(quantum_m / sqrt(quantum_l*(quantum_l+1))) + t
phi = quantum_m * PI / quantum_l + t * 1.5

// Convert spherical to cartesian coordinates
x = r * sin(theta) * cos(phi)
y = r * sin(theta) * sin(phi)
z = r * cos(theta)

// Perspective projection
distance = 2.5
x_proj = x * distance / (distance + z)
y_proj = y * distance / (distance + z)

// Audio-reactive quantum fluctuations
audio_fluctuation = getspec(0,0.15,0) * 3
x_proj = x_proj + (random() - 0.5) * audio_fluctuation * 0.05
y_proj = y_proj + (random() - 0.5) * audio_fluctuation * 0.05

// Probability density coloring
probability = exp(-r) * (sin(theta) ^ quantum_l) ^ 2
red = 0.1 + probability * 0.4
green = 0.6 + probability * 0.4
blue = 0.9 + probability * 0.1

// Energy level modulation
energy = getspec(0,0.1,0) * 2
red = red * (0.5 + energy)
green = green * (0.5 + energy)
blue = blue * (0.5 + energy)

FRAME
t=t+dt

[preset01]
n=280.000000
b=1.000000
sn=Superscope (Ray Marching Geometry)
POINT
t=1.000000
dt=0.018000
RED=1.000000
GREEN=0.600000
BLUE=0.300000
INIT
n=1
t=0

CODE
// Ray marching through 3D signed distance fields
// Complex geometry with lighting and reflections

// Generate 3D scene using distance functions
scene_x = (n / 280 - 0.5) * 4
scene_y = (random() - 0.5) * 4
scene_z = t * 2

// Distance to sphere
sphere_dist = sqrt(scene_x*scene_x + scene_y*scene_y + scene_z*scene_z) - 1

// Distance to box
box_dist = max(abs(scene_x), max(abs(scene_y), abs(scene_z))) - 0.8

// Combine shapes using smooth minimum
k = 0.3
h = clamp(0.5 + 0.5*(box_dist-sphere_dist)/k, 0, 1)
combined_dist = box_dist*(1-h) + sphere_dist*h - k*h*(1-h)

// Audio-reactive deformation
deform = getspec(0,0.2,0) * 2
combined_dist = combined_dist + sin(scene_x*5 + t*3) * deform * 0.1

// Ray marching simulation (simplified)
march_distance = 0
for (i = 0; i < 10; i = i + 1,
  march_distance = march_distance + combined_dist * 0.1
)

// Project to 2D
x = scene_x * 0.3 + sin(t) * 0.1
y = scene_y * 0.3 + cos(t) * 0.1

// Color based on distance and normal
normal_x = scene_x / combined_dist
normal_y = scene_y / combined_dist
normal_z = scene_z / combined_dist

diffuse = max(0, normal_z) * 0.8 + 0.2
red = diffuse
green = diffuse * 0.7
blue = diffuse * 0.4

// Add specular highlights
specular = pow(max(0, normal_z), 10)
red = red + specular
green = green + specular * 0.5
blue = blue + specular * 0.3

FRAME
t=t+dt

[preset02]
n=220.000000
b=1.000000
sn=Superscope (Fluid Dynamics)
POINT
t=1.000000
dt=0.012000
RED=0.400000
GREEN=0.700000
BLUE=0.900000
INIT
n=1
t=0

CODE
// Fluid dynamics simulation
// Navier-Stokes equations approximated for visualization

particle_id = n % 80
fluid_x = cos(particle_id * 2 * PI / 80) * 0.4
fluid_y = sin(particle_id * 2 * PI / 80) * 0.4

// Velocity field (simplified Navier-Stokes)
velocity_x = sin(fluid_y * 5 + t * 2) * 0.1
velocity_y = cos(fluid_x * 5 + t * 2) * 0.1

// Advection (particle movement)
fluid_x = fluid_x + velocity_x * dt * 20
fluid_y = fluid_y + velocity_y * dt * 20

// Boundary conditions (keep particles in bounds)
fluid_x = max(min(fluid_x, 0.5), -0.5)
fluid_y = max(min(fluid_y, 0.5), -0.5)

// Vorticity confinement
vorticity = (velocity_x - velocity_y) * 5
fluid_x = fluid_x + cos(vorticity + t) * 0.02
fluid_y = fluid_y + sin(vorticity + t) * 0.02

// Audio-reactive turbulence
turbulence = getspec(0,0.1,0) * 4
fluid_x = fluid_x + (random() - 0.5) * turbulence * 0.05
fluid_y = fluid_y + (random() - 0.5) * turbulence * 0.05

x = fluid_x
y = fluid_y

// Color based on velocity magnitude
velocity_magnitude = sqrt(velocity_x*velocity_x + velocity_y*velocity_y)
red = 0.2 + velocity_magnitude * 2
green = 0.4 + velocity_magnitude * 1.5
blue = 0.6 + velocity_magnitude * 1

// Temperature visualization (cool to hot)
temperature = velocity_magnitude * 3
red = red + temperature * 0.3
green = green + temperature * 0.2
blue = blue - temperature * 0.1

FRAME
t=t+dt

[preset03]
n=160.000000
b=1.000000
sn=Superscope (Quantum Entanglement)
POINT
t=1.000000
dt=0.016000
RED=0.800000
GREEN=0.400000
BLUE=0.800000
INIT
n=1
t=0

CODE
// Quantum entanglement visualization
// Correlated particle pairs with instantaneous connections

pair_id = n % 40
is_particle_a = (n % 80) < 40

// Entangled particle positions
if (is_particle_a,
  // Particle A
  ent_x = cos(pair_id * PI / 20 + t) * 0.3
  ent_y = sin(pair_id * PI / 20 + t) * 0.3,
  // Particle B (correlated)
  ent_x = cos(pair_id * PI / 20 + t + PI) * 0.3
  ent_y = sin(pair_id * PI / 20 + t + PI) * 0.3
)

// Quantum uncertainty principle
uncertainty = getspec(0,0.1,0) * 2
ent_x = ent_x + (random() - 0.5) * uncertainty * 0.1
ent_y = ent_y + (random() - 0.5) * uncertainty * 0.1

// EPR paradox visualization (Bell's theorem)
if (is_particle_a,
  // Draw connection line to entangled partner
  partner_x = cos(pair_id * PI / 20 + t + PI) * 0.3
  partner_y = sin(pair_id * PI / 20 + t + PI) * 0.3
  x = (ent_x + partner_x) * 0.5  // Midpoint for connection
  y = (ent_y + partner_y) * 0.5
  red = 0.8, green = 0.4, blue = 0.8,
  // Draw particles
  x = ent_x
  y = ent_y
  red = 1.0, green = 0.2, blue = 0.6
)

FRAME
t=t+dt

[preset04]
n=120.000000
b=1.000000
sn=Superscope (Holographic Interference)
POINT
t=1.000000
dt=0.020000
RED=0.600000
GREEN=0.900000
BLUE=0.600000
INIT
n=1
t=0

CODE
// Holographic interference patterns
// Multiple wave sources creating complex interference

// Multiple wave sources
wave_sources = 5
source_index = n % wave_sources

// Source positions in circle
source_angle = source_index * 2 * PI / wave_sources
source_x = cos(source_angle) * 0.4
source_y = sin(source_angle) * 0.4

// Calculate interference at evaluation point
eval_x = (n / 120 - 0.5) * 1.2
eval_y = (random() - 0.5) * 1.2

// Sum of waves from all sources
total_amplitude = 0
for (i = 0; i < wave_sources; i = i + 1,
  source_ang = i * 2 * PI / wave_sources
  src_x = cos(source_ang) * 0.4
  src_y = sin(source_ang) * 0.4
  distance = sqrt((eval_x - src_x)^2 + (eval_y - src_y)^2)
  phase = distance * 10 - t * 3
  wave = sin(phase) / (distance + 0.1)
  total_amplitude = total_amplitude + wave
)

// Interference pattern
x = eval_x
y = eval_y

// Color based on interference (constructive/destructive)
interference_strength = abs(total_amplitude)
if (above(total_amplitude, 0),
  red = 0.6 + interference_strength * 0.4
  green = 0.9 - interference_strength * 0.3
  blue = 0.6 + interference_strength * 0.4,
  red = 0.6 - interference_strength * 0.4
  green = 0.9 + interference_strength * 0.3
  blue = 0.6 - interference_strength * 0.4
)

// Audio modulation of wavelength
audio_freq = getspec(0,0.1,0) * 5 + 1
// This would modulate the phase calculation above

FRAME
t=t+dt

[MAIN]
NUMFRAGS=5
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
