[avs]
PRESET_NAME=Alchemical Mercury Transmutation
DESCRIPTION=Alchemical mercury visualization showing liquid metal transmutation, elemental transformation, and the philosopher's stone process. Features flowing mercury with color changes representing different alchemical stages.

[preset00]
n=450.000000
b=1.000000
sn=Superscope (Liquid Mercury Flow)
POINT
t=1.000000
dt=0.011000
RED=0.200000
GREEN=0.800000
BLUE=1.000000
INIT
n=1
t=0
PI=3.141592653589793
PHI=1.618033988749895
MERCURY_DENSITY=13.546  // g/cm³
ALCHEMICAL_STAGES=7

CODE
// Liquid mercury simulation with alchemical transmutation
// Based on real mercury properties and alchemical symbolism

// Calculate mercury flow patterns
flow_angle = t * 2 + n * 0.1
flow_radius = 0.3 + 0.1 * sin(t * 3 + n * 0.05)
flow_speed = 1 + getspec(0,0.1,0) * 2

// Mercury droplet positions
x = cos(flow_angle) * flow_radius * flow_speed
y = sin(flow_angle) * flow_radius * flow_speed

// Add surface tension waves
surface_wave = sin(t * 5 + n * 0.2) * 0.02
x = x + surface_wave * cos(flow_angle)
y = y + surface_wave * sin(flow_angle)

// Alchemical color stages based on transformation process
stage = n % ALCHEMICAL_STAGES

// Nigredo (Blackening) - Initial chaos
if (equal(stage,0),
  red = 0.1, green = 0.1, blue = 0.1,  // Black phase
  // Albedo (Whitening) - Purification
  if (equal(stage,1),
    red = 0.9, green = 0.9, blue = 0.9,  // White phase
    // Citrinitas (Yellowing) - Enlightenment
    if (equal(stage,2),
      red = 0.9, green = 0.8, blue = 0.2,  // Golden yellow
      // Rubedo (Reddening) - Completion
      if (equal(stage,3),
        red = 0.9, green = 0.1, blue = 0.1,  // Deep red
        // Multiplicatio (Multiplication) - Expansion
        if (equal(stage,4),
          red = 0.5 + 0.5 * sin(t * 2), green = 0.2, blue = 0.8,  // Purple/violet
          // Projectio (Projection) - Transmutation
          if (equal(stage,5),
            red = 1.0, green = 0.5, blue = 0.0,  // Orange/gold
            // Fixatio (Fixation) - Stabilization
            red = 0.8, green = 0.8, blue = 0.2   // Bright gold
          )
        )
      )
    )
  )
)

// Audio-reactive mercury properties
audio_energy = getspec(0,0.1,0) * 3
viscosity = 1 + audio_energy * 0.5  // Mercury becomes more fluid with bass

// Apply viscosity to movement
x = x * viscosity
y = y * viscosity

// Mercury's high surface tension creates bead-like formations
if (above(audio_energy, 0.5),
  // High energy creates mercury beads
  bead_factor = 1 + sin(t * 10 + n) * 0.2
  x = x * bead_factor
  y = y * bead_factor
)

// Energy modulation
brightness = 0.3 + audio_energy * 0.7
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset01]
n=320.000000
b=1.000000
sn=Superscope (Alchemical Symbols)
POINT
t=1.000000
dt=0.015000
RED=1.000000
GREEN=0.600000
BLUE=0.200000
INIT
n=1
t=0
GOLDEN_RATIO=1.618033988749895

CODE
// Alchemical symbols and elemental representations
// Triangles, circles, squares representing elements and processes

symbol_type = n % 8
rotation = t * 0.5 + symbol_type * 0.8
scale = 0.15 + getspec(0,0.2,0) * 0.1

// Philosopher's Stone (center symbol)
if (equal(symbol_type,0),
  // Circled dot - represents the philosopher's stone
  angle = (n / 320) * 4 * PI
  radius = scale * (0.8 + 0.2 * sin(t * 3))
  x = cos(angle) * radius
  y = sin(angle) * radius
  red = 0.9, green = 0.8, blue = 0.3,  // Gold

  // Fire triangle (upward pointing)
  if (equal(symbol_type,1),
    vertex = (n % 3)
    if (equal(vertex,0), x = 0, y = scale,      // Top
      if (equal(vertex,1), x = -scale * 0.8, y = -scale,  // Bottom left
        if (equal(vertex,2), x = scale * 0.8, y = -scale   // Bottom right
        )
      )
    )
    x = x + cos(rotation) * 0.3
    y = y + sin(rotation) * 0.3
    red = 1.0, green = 0.3, blue = 0.1,  // Red/orange

    // Water triangle (downward pointing)
    if (equal(symbol_type,2),
      vertex = (n % 3)
      if (equal(vertex,0), x = 0, y = -scale,     // Bottom
        if (equal(vertex,1), x = -scale * 0.8, y = scale,   // Top left
          if (equal(vertex,2), x = scale * 0.8, y = scale    // Top right
          )
        )
      )
      x = x + cos(rotation + PI) * 0.3
      y = y + sin(rotation + PI) * 0.3
      red = 0.2, green = 0.4, blue = 0.9,  // Blue

      // Air triangle (upward with line)
      if (equal(symbol_type,3),
        vertex = (n % 3)
        if (equal(vertex,0), x = 0, y = scale,      // Top
          if (equal(vertex,1), x = -scale * 0.8, y = -scale,  // Bottom left
            if (equal(vertex,2), x = scale * 0.8, y = -scale   // Bottom right
            )
          )
        )
        // Add horizontal line for air
        if (equal(vertex,3), x = -scale * 0.6, y = 0,   // Left line
          if (equal(vertex,4), x = scale * 0.6, y = 0    // Right line
          )
        )
        x = x + cos(rotation + PI/2) * 0.25
        y = y + sin(rotation + PI/2) * 0.25
        red = 0.8, green = 0.9, blue = 1.0,  // Light blue

        // Earth square
        if (equal(symbol_type,4),
          vertex = (n % 4)
          if (equal(vertex,0), x = -scale, y = scale,     // Top left
            if (equal(vertex,1), x = scale, y = scale,     // Top right
              if (equal(vertex,2), x = scale, y = -scale,   // Bottom right
                if (equal(vertex,3), x = -scale, y = -scale  // Bottom left
                )
              )
            )
          )
          x = x + cos(rotation + PI/4) * 0.35
          y = y + sin(rotation + PI/4) * 0.35
          red = 0.4, green = 0.8, blue = 0.2,  // Green

          // Salt symbol (circle with dot)
          if (equal(symbol_type,5),
            if (equal(n % 2, 0),
              // Outer circle
              angle = (n / 320) * 2 * PI
              x = cos(angle) * scale * 0.8
              y = sin(angle) * scale * 0.8
              red = 0.9, green = 0.9, blue = 0.9,  // White
              // Inner dot
              x = 0, y = 0
              red = 0.9, green = 0.9, blue = 0.9   // White
            )
            x = x + cos(rotation + PI/6) * 0.2
            y = y + sin(rotation + PI/6) * 0.2,

            // Mercury symbol (crescent and circle)
            if (equal(symbol_type,6),
              if (equal(n % 2, 0),
                // Crescent
                angle = (n / 320) * PI
                x = cos(angle) * scale + scale * 0.3
                y = sin(angle) * scale
                red = 0.7, green = 0.7, blue = 0.8,  // Silver
                // Circle
                x = -scale * 0.3, y = 0
                red = 0.7, green = 0.7, blue = 0.8   // Silver
              )
              x = x + cos(rotation + PI/3) * 0.15
              y = y + sin(rotation + PI/3) * 0.15,

              // Sulfur symbol (triangle with cross)
              if (equal(symbol_type,7),
                vertex = (n % 3)
                if (equal(vertex,0), x = 0, y = scale,      // Top
                  if (equal(vertex,1), x = -scale * 0.8, y = -scale,  // Bottom left
                    if (equal(vertex,2), x = scale * 0.8, y = -scale   // Bottom right
                    )
                  )
                )
                // Add cross lines
                if (equal(vertex,3), x = 0, y = scale * 0.5,     // Vertical line top
                  if (equal(vertex,4), x = 0, y = -scale * 0.5,    // Vertical line bottom
                    if (equal(vertex,5), x = -scale * 0.4, y = 0,    // Horizontal line left
                      if (equal(vertex,6), x = scale * 0.4, y = 0     // Horizontal line right
                      )
                    )
                  )
                )
                x = x + cos(rotation + 2*PI/3) * 0.28
                y = y + sin(rotation + 2*PI/3) * 0.28
                red = 1.0, green = 0.6, blue = 0.1   // Yellow/gold
              )
            )
          )
        )
      )
    )
  )
)

// Audio-reactive symbol intensity
symbol_energy = getspec(0,0.1,0) * 2
red = red * (0.3 + symbol_energy)
green = green * (0.3 + symbol_energy)
blue = blue * (0.3 + symbol_energy)

FRAME
t=t+dt

[preset02]
n=280.000000
b=1.000000
sn=Superscope (Transmutation Particles)
POINT
t=1.000000
dt=0.013000
RED=0.800000
GREEN=0.400000
BLUE=0.800000
INIT
n=1
t=0
PARTICLES=80

CODE
// Particle system showing alchemical transmutation
// Particles changing state and color as they transform

particle_id = n % PARTICLES
evolution_stage = (t + particle_id * 0.1) % (2 * PI)
transformation_progress = (sin(evolution_stage) + 1) / 2  // 0 to 1

// Spiral path for transformation journey
spiral_angle = evolution_stage + particle_id * 0.2
spiral_radius = 0.1 + transformation_progress * 0.3
x = cos(spiral_angle) * spiral_radius
y = sin(spiral_angle) * spiral_radius

// Add chaotic motion during transformation
chaos_factor = sin(evolution_stage * 3) * 0.05
x = x + chaos_factor * cos(particle_id * 7)
y = y + chaos_factor * sin(particle_id * 7)

// Color evolution through alchemical stages
if (below(transformation_progress, 0.2),
  red = 0.1, green = 0.1, blue = 0.1,  // Nigredo - black
  if (below(transformation_progress, 0.4),
    red = 0.9, green = 0.9, blue = 0.9,  // Albedo - white
    if (below(transformation_progress, 0.6),
      red = 0.9, green = 0.8, blue = 0.2,  // Citrinitas - yellow
      if (below(transformation_progress, 0.8),
        red = 0.9, green = 0.1, blue = 0.1,  // Rubedo - red
        red = 0.8, green = 0.8, blue = 0.2   // Final gold
      )
    )
  )
)

// Particle size based on transformation energy
particle_size = 0.02 + transformation_progress * 0.03
// This would be used for point size in actual rendering

// Audio-reactive transformation speed
audio_boost = getspec(0,0.1,0) * 2
if (above(audio_boost, 0.5),
  // Accelerate transformation during high energy
  transformation_progress = min(1, transformation_progress + audio_boost * 0.1)
)

// Brightness based on transformation completeness
brightness = 0.4 + transformation_progress * 0.6
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset03]
n=200.000000
b=1.000000
sn=Superscope (Elemental Vortex)
POINT
t=1.000000
dt=0.017000
RED=0.600000
GREEN=0.900000
BLUE=0.600000
INIT
n=1
t=0

CODE
// Elemental vortex showing the four elements in rotation
// Fire, Air, Water, Earth in alchemical balance

element = n % 4
element_angle = (t * 2) + element * (PI / 2)
element_radius = 0.25

// Base position for each element
x = cos(element_angle) * element_radius
y = sin(element_angle) * element_radius

// Element-specific patterns
if (equal(element,0),
  // Fire - upward swirling motion
  fire_swirl = sin(t * 4 + n * 0.3) * 0.05
  x = x + fire_swirl * cos(element_angle + PI/2)
  y = y + fire_swirl * sin(element_angle + PI/2)
  red = 1.0, green = 0.4, blue = 0.1,  // Red/orange

  if (equal(element,1),
    // Air - light, floating motion
    air_float = cos(t * 3 + n * 0.2) * 0.04
    x = x + air_float * cos(element_angle)
    y = y + air_float * sin(element_angle)
    red = 0.7, green = 0.9, blue = 1.0,  // Light blue

    if (equal(element,2),
      // Water - flowing, wave-like motion
      water_wave = sin(t * 2 + n * 0.1) * 0.06
      x = x + water_wave * cos(element_angle - PI/2)
      y = y + water_wave * sin(element_angle - PI/2)
      red = 0.1, green = 0.5, blue = 0.9,  // Blue

      // Earth - stable, grounding motion
      if (equal(element,3),
        earth_stability = cos(t * 1.5 + n * 0.05) * 0.03
        x = x + earth_stability * cos(element_angle + PI)
        y = y + earth_stability * sin(element_angle + PI)
        red = 0.3, green = 0.7, blue = 0.2   // Green
      )
    )
  )
)

// Central alchemical balance point
center_energy = getspec(0,0.1,0) * 3
if (below(n, 50),
  // Central philosopher's stone
  central_angle = (n / 50) * 2 * PI
  x = cos(central_angle) * 0.08 * (1 + center_energy)
  y = sin(central_angle) * 0.08 * (1 + center_energy)
  red = 0.9, green = 0.7, blue = 0.3,  // Gold
  brightness = 0.6 + center_energy
  red = red * brightness
  green = green * brightness
  blue = blue * brightness
)

// Audio-reactive elemental intensification
element_boost = getspec(0,0.1,0) * 2
red = red * (0.4 + element_boost * 0.6)
green = green * (0.4 + element_boost * 0.6)
blue = blue * (0.4 + element_boost * 0.6)

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
