[avs]
PRESET_NAME=Matrix Reality Code
DESCRIPTION=Matrix reality code visualization with digital rain, cascading green text, reality glitches, red pill/blue pill choices, and simulation-within-simulation layers. Neo's journey through the Matrix code.

[preset00]
n=500.000000
b=1.000000
sn=Superscope (Digital Rain)
POINT
t=1.000000
dt=0.008000
RED=0.200000
GREEN=1.000000
BLUE=0.300000
INIT
n=1
t=0
STREAM_COUNT=20

CODE
// Matrix-style digital rain with cascading green characters
// Based on the Matrix movie visual effects

stream_id = n % STREAM_COUNT
stream_x = (stream_id / STREAM_COUNT - 0.5) * 1.6  // Spread across screen

// Stream position and speed
stream_speed = 0.02 + (stream_id % 3) * 0.01  // Vary speeds
stream_y = ((t * stream_speed + stream_id * 0.1) % 2.2) - 1.1

// Character trail effect (multiple points per stream)
trail_position = (n / STREAM_COUNT) % 15
trail_offset = trail_position * 0.03
stream_y = stream_y - trail_offset

// Only show streams that are on screen
if (above(stream_y, -1.1) && below(stream_y, 1.1) && above(stream_y + trail_offset, -1.1),
  x = stream_x
  y = stream_y

  // Matrix green color with slight variations
  red = 0.1 + sin(t * 2 + stream_id) * 0.1
  green = 0.8 + cos(t * 3 + stream_id) * 0.2
  blue = 0.2 + sin(t * 4 + stream_id) * 0.1

  // Trail fading effect (brighter at bottom, fade upward)
  trail_brightness = 1 - (trail_position / 15)
  brightness = trail_brightness * (0.6 + sin(t * 5 + trail_position) * 0.4)
  red = red * brightness
  green = green * brightness
  blue = blue * brightness

  // Random character-like glitches
  if (equal(trail_position % 7, 0),
    // Add some "noise" to simulate characters
    glitch = sin(t * 10 + trail_position * 2) * 0.02
    x = x + glitch
    y = y + sin(t * 8 + trail_position * 1.5) * 0.01
  )
)

// Audio-reactive rain intensity
rain_intensity = getspec(0,0.1,0) * 2
if (above(rain_intensity, 0.3),
  // Increase stream density during high energy
  stream_count_multiplier = 1 + rain_intensity
  // This would create more streams in actual implementation
)

FRAME
t=t+dt

[preset01]
n=300.000000
b=1.000000
sn=Superscope (Reality Glitches)
POINT
t=1.000000
dt=0.012000
RED=1.000000
GREEN=0.200000
BLUE=0.200000
INIT
n=1
t=0

CODE
// Reality glitches and distortions
// Matrix "breaking" effects and digital artifacts

glitch_type = n % 5

// Horizontal glitch lines
if (equal(glitch_type,0),
  glitch_x = (sin(t * 8 + n * 0.5) - 0.5) * 2
  glitch_y = (n / 300 - 0.5) * 2
  glitch_length = 0.3 + sin(t * 6 + n) * 0.2

  // Draw horizontal glitch line segments
  segment = (n % 10)
  x = glitch_x + (segment - 5) * glitch_length / 10
  y = glitch_y

  red = 1.0, green = 0.2, blue = 0.2,  // Red glitch

  // Vertical glitch lines
  if (equal(glitch_type,1),
    glitch_y = (cos(t * 7 + n * 0.3) - 0.5) * 2
    glitch_x = (n / 300 - 0.5) * 2
    glitch_length = 0.4 + cos(t * 5 + n) * 0.3

    segment = (n % 8)
    x = glitch_x
    y = glitch_y + (segment - 4) * glitch_length / 8

    red = 0.2, green = 1.0, blue = 0.2,  // Green glitch

    // Digital blocks (pixelated corruption)
    if (equal(glitch_type,2),
      block_x = floor((n % 20) / 4) * 0.2 - 0.5
      block_y = floor((n / 20) % 15) * 0.13 - 0.8
      block_size = 0.05

      // Create block pattern
      sub_x = (n % 4) * block_size / 4
      sub_y = floor(n / 80) % 4 * block_size / 4

      x = block_x + sub_x
      y = block_y + sub_y

      // Random block colors (red, green, blue corruption)
      color_type = n % 3
      if (equal(color_type,0), red = 1.0, green = 0.0, blue = 0.0,  // Red blocks
        if (equal(color_type,1), red = 0.0, green = 1.0, blue = 0.0,  // Green blocks
          red = 0.0, green = 0.0, blue = 1.0   // Blue blocks
        )
      )

      // Screen tearing effect
      if (equal(glitch_type,3),
        tear_y = (sin(t * 4 + n * 0.2) - 0.5) * 2
        tear_offset = sin(t * 12 + n) * 0.1
        x = (n / 300 - 0.5) * 2 + tear_offset
        y = tear_y

        red = 0.8, green = 0.8, blue = 0.8,  // White tearing

        // Color inversion zones
        if (equal(glitch_type,4),
          zone_x = cos(t * 3 + n * 0.1) * 0.3
          zone_y = sin(t * 3 + n * 0.1) * 0.3
          zone_radius = 0.15 + sin(t * 2 + n) * 0.05

          angle = (n % 20) * 2 * PI / 20
          x = zone_x + cos(angle) * zone_radius
          y = zone_y + sin(angle) * zone_radius

          // Inverted colors (cyan instead of red, etc.)
          red = 0.0, green = 1.0, blue = 1.0   // Cyan (inverted red)
        )
      )
    )
  )
)

// Glitch timing - only show glitches occasionally
glitch_active = sin(t * 2 + glitch_type * 3) > 0.7
if (equal(glitch_active, 0),
  x = 0, y = 0, red = 0, green = 0, blue = 0  // Hide inactive glitches
)

// Audio-reactive glitch frequency
glitch_frequency = getspec(0,0.1,0) * 4
if (above(glitch_frequency, 1.0),
  // More glitches during high energy
  glitch_active = 1
)

FRAME
t=t+dt

[preset02]
n=200.000000
b=1.000000
sn=Superscope (Red Pill Blue Pill)
POINT
t=1.000000
dt=0.016000
RED=0.800000
GREEN=0.200000
BLUE=0.200000
INIT
n=1
t=0

CODE
// Red Pill vs Blue Pill choice visualization
// Neo's moment of decision in the Matrix

choice_side = n % 2  // 0 = Red Pill, 1 = Blue Pill

// Pill positions
if (equal(choice_side,0),
  // Red Pill (left side - awakening)
  pill_x = -0.3
  pill_y = 0
  red = 0.9, green = 0.1, blue = 0.1,  // Red

  // Blue Pill (right side - ignorance)
  if (equal(choice_side,1),
    pill_x = 0.3
    pill_y = 0
    red = 0.1, green = 0.3, blue = 0.9   // Blue
  )
)

// Pill shape (capsule-like)
capsule_aspect = 2.5  // Length to width ratio
capsule_width = 0.08

angle = (n / 200) * 2 * PI
if (below(abs(sin(angle)), 0.3),
  // Pill ends (hemispheres)
  radius = capsule_width / 2
  x = pill_x + cos(angle) * radius
  y = pill_y + sin(angle) * radius
  ,
  // Pill body (rectangle)
  body_x = pill_x + sin(angle) * (capsule_width / capsule_aspect) * 0.5
  body_y = pill_y + cos(angle) * (capsule_width / 2)
  x = body_x
  y = body_y
)

// Pill labels and effects
if (equal(choice_side,0) && equal(n % 50, 0),
  // Red pill glow effects
  glow_angle = (n / 50) * 2 * PI / 5
  glow_radius = 0.15 + sin(t * 4) * 0.05
  x = pill_x + cos(glow_angle) * glow_radius
  y = pill_y + sin(glow_angle) * glow_radius
  red = 0.8, green = 0.2, blue = 0.2,
  brightness = 0.6 + sin(t * 6 + glow_angle) * 0.4
  red = red * brightness
  green = green * brightness
  blue = blue * brightness
)

if (equal(choice_side,1) && equal(n % 50, 25),
  // Blue pill glow effects
  glow_angle = (n / 50) * 2 * PI / 5
  glow_radius = 0.15 + cos(t * 4) * 0.05
  x = pill_x + cos(glow_angle) * glow_radius
  y = pill_y + sin(glow_angle) * glow_radius
  red = 0.2, green = 0.4, blue = 0.8,
  brightness = 0.6 + cos(t * 6 + glow_angle) * 0.4
  red = red * brightness
  green = green * brightness
  blue = blue * brightness
)

// Choice consequence visualization
choice_made = sin(t * 0.5) > 0  // Oscillating choice
if (equal(choice_made, 1),
  // Red pill chosen - reality distortion
  if (equal(choice_side,0),
    distortion = sin(t * 8 + n) * 0.1
    x = x + distortion
    y = y + cos(t * 6 + n) * 0.05
    red = red * 1.5, green = green * 0.5, blue = blue * 0.5  // Intensify red
  ),
  // Blue pill chosen - comforting blue
  if (equal(choice_side,1),
    soothing = cos(t * 3 + n) * 0.08
    x = x + soothing * 0.5
    y = y + sin(t * 2 + n) * 0.03
    red = red * 0.5, green = green * 0.8, blue = blue * 1.2  // Enhance blue
  )
)

// Audio-reactive choice pressure
choice_pressure = getspec(0,0.1,0) * 3
if (above(choice_pressure, 1.0),
  // High energy makes choice more intense
  red = red * (1 + choice_pressure * 0.5)
  green = green * (1 + choice_pressure * 0.3)
  blue = blue * (1 + choice_pressure * 0.3)
)

FRAME
t=t+dt

[preset03]
n=180.000000
b=1.000000
sn=Superscope (Simulation Layers)
POINT
t=1.000000
dt=0.014000
RED=0.400000
GREEN=0.600000
BLUE=0.800000
INIT
n=1
t=0

CODE
// Simulation within simulation layers
// Multiple nested Matrix realities

layer = n % 4
layer_scale = 1 - layer * 0.2  // Each layer smaller
layer_rotation = t * (0.5 + layer * 0.3)  // Each layer rotates differently

// Base pattern for each layer
pattern_angle = (n / 180) * 2 * PI * (1 + layer)
pattern_radius = 0.2 * layer_scale

x = cos(pattern_angle + layer_rotation) * pattern_radius
y = sin(pattern_angle + layer_rotation) * pattern_radius

// Layer-specific visual styles
if (equal(layer,0),
  // Innermost layer - most "real"
  red = 0.6, green = 0.8, blue = 0.4,  // Bright green
  // Add some stability
  stability = cos(t * 2 + layer) * 0.02
  x = x + stability
  y = y + stability

  if (equal(layer,1),
    // Second layer - slightly distorted
    red = 0.4, green = 0.7, blue = 0.6,  // Muted green
    // Add mild distortion
    distortion = sin(t * 3 + layer * 2) * 0.03
    x = x + distortion
    y = y + sin(t * 2.5 + layer * 1.5) * 0.02

    if (equal(layer,2),
      // Third layer - more distorted
      red = 0.3, green = 0.5, blue = 0.7,  // Blue-green
      // Add significant distortion
      distortion1 = sin(t * 4 + layer * 3) * 0.05
      distortion2 = cos(t * 3.5 + layer * 2.5) * 0.04
      x = x + distortion1 + distortion2
      y = y + sin(t * 3.8 + layer * 2.8) * 0.05

      // Outermost layer - heavily distorted
      if (equal(layer,3),
        red = 0.2, green = 0.4, blue = 0.8,  // Blue
        // Add heavy distortion
        distortion1 = sin(t * 5 + layer * 4) * 0.08
        distortion2 = cos(t * 4.5 + layer * 3.5) * 0.06
        distortion3 = sin(t * 6 + layer * 5) * 0.04
        x = x + distortion1 + distortion2 + distortion3
        y = y + cos(t * 5.5 + layer * 4.5) * 0.07 + sin(t * 4.8 + layer * 3.8) * 0.05
      )
    )
  )
)

// Layer transparency effect
layer_alpha = 1 - layer * 0.15
brightness = 0.4 + layer_alpha * 0.6
red = red * brightness
green = green * brightness
blue = blue * brightness

// Audio-reactive layer peeling
layer_peel = getspec(0,0.1,0) * 3
if (above(layer_peel, 1.5),
  // High energy "peels back" layers
  if (above(layer, 1),
    // Outer layers become more visible/intense
    red = red * (1 + layer_peel * 0.3)
    green = green * (1 + layer_peel * 0.3)
    blue = blue * (1 + layer_peel * 0.3)
  )
)

FRAME
t=t+dt

[preset04]
n=120.000000
b=1.000000
sn=Superscope (Code Streams)
POINT
t=1.000000
dt=0.020000
RED=0.100000
GREEN=0.900000
BLUE=0.200000
INIT
n=1
t=0

CODE
// Matrix code streams with binary and hexadecimal
// Authentic Matrix-style cascading characters

code_stream = n % 8
stream_x = (code_stream / 8 - 0.5) * 1.4
stream_speed = 0.015 + (code_stream % 3) * 0.008
stream_y = ((t * stream_speed + code_stream * 0.15) % 2.4) - 1.2

// Character positions in stream
char_position = (n / 8) % 12
char_y = stream_y - char_position * 0.04

// Only show characters on screen
if (above(char_y, -1.2) && below(char_y, 1.2),
  x = stream_x
  y = char_y

  // Matrix-style bright green
  red = 0.1, green = 0.9, blue = 0.2

  // Character brightness fade
  char_brightness = 1 - (char_position / 12)
  brightness = char_brightness * (0.7 + sin(t * 4 + char_position) * 0.3)
  red = red * brightness
  green = green * brightness
  blue = blue * brightness

  // Random character effects (simulate different characters)
  char_type = n % 4
  if (equal(char_type, 0),
    // Bright lead character (white)
    if (equal(char_position, 0),
      red = 0.9, green = 0.9, blue = 0.9
    )
  )

  if (equal(char_type, 1),
    // Binary digits (0s and 1s)
    binary_value = sin(t * 6 + n) > 0 ? 1 : -1
    if (equal(binary_value, 1),
      x = x + sin(t * 8 + n) * 0.01  // Slight movement for 1s
    )
  )

  if (equal(char_type, 2),
    // Hexadecimal digits
    hex_offset = cos(t * 5 + n) * 0.015
    x = x + hex_offset
  )

  if (equal(char_type, 3),
    // Katakana-style characters (simplified)
    kana_wobble = sin(t * 7 + n) * 0.008
    x = x + kana_wobble
    y = y + cos(t * 6 + n) * 0.005
  )
)

// Audio-reactive code intensity
code_intensity = getspec(0,0.1,0) * 2.5
if (above(code_intensity, 1.0),
  // More streams during high energy
  stream_multiplier = 1 + code_intensity * 0.5
  // Would create additional streams in full implementation
  red = red * (1 + code_intensity * 0.3)
  green = green * (1 + code_intensity * 0.3)
  blue = blue * (1 + code_intensity * 0.3)
)

FRAME
t=t+dt

[preset05]
n=80.000000
b=1.000000
sn=Superscope (Neural Network)
POINT
t=1.000000
dt=0.025000
RED=0.600000
GREEN=0.600000
BLUE=1.000000
INIT
n=1
t=0

CODE
// Neural network visualization
// The Matrix as a neural network of the mind

node_id = n % 20
layer = floor(node_id / 5)  // 4 layers
node_in_layer = node_id % 5

// Layer positions
layer_x = (layer / 3 - 0.5) * 1.2
layer_y_base = (node_in_layer / 4 - 0.5) * 1.4

// Node pulsing
node_pulse = sin(t * 3 + node_id * 0.5) * 0.03
x = layer_x + node_pulse * cos(t + node_id)
y = layer_y_base + node_pulse * sin(t * 1.2 + node_id)

red = 0.4, green = 0.6, blue = 0.8  // Neural blue

// Connection lines between layers
if (equal(n % 2, 0),
  connection_id = floor(n / 2) % 15
  from_layer = floor(connection_id / 5)
  to_layer = from_layer + 1
  from_node = connection_id % 5
  to_node = (connection_id + from_layer * 2) % 5  // Pseudo-random connections

  if (below(to_layer, 4),
    from_x = (from_layer / 3 - 0.5) * 1.2
    from_y = (from_node / 4 - 0.5) * 1.4
    to_x = (to_layer / 3 - 0.5) * 1.2
    to_y = (to_node / 4 - 0.5) * 1.4

    // Connection line segments
    connection_progress = (n % 10) / 9
    x = from_x + (to_x - from_x) * connection_progress
    y = from_y + (to_y - from_y) * connection_progress

    red = 0.2, green = 0.8, blue = 0.6  // Connection green
  )
)

// Neural activation patterns
activation = sin(t * 4 + node_id * 0.8) * 0.5 + 0.5
brightness = 0.3 + activation * 0.7
red = red * brightness
green = green * brightness
blue = blue * brightness

// Audio-reactive neural firing
neural_fire = getspec(0,0.1,0) * 3
if (above(neural_fire, 1.2),
  // High energy causes neural cascades
  cascade_effect = sin(t * 8 + node_id * 2) * neural_fire * 0.1
  x = x + cascade_effect * cos(t * 6 + node_id)
  y = y + cascade_effect * sin(t * 6 + node_id)
  red = red * (1 + neural_fire * 0.4)
  green = green * (1 + neural_fire * 0.4)
  blue = blue * (1 + neural_fire * 0.4)
)

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
