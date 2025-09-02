[avs]
PRESET_NAME=Mystical Synthesis
DESCRIPTION=Grand synthesis of alchemy, kabbalah, and Matrix reality. Philosopher's Stone meets Tree of Life in digital code rain, with elemental transmutation through Sephiroth, reality glitches in sacred geometry, and the red pill/blue pill choice of enlightenment.

[preset00]
n=600.000000
b=1.000000
sn=Superscope (Synthesis Core)
POINT
t=1.000000
dt=0.010000
RED=0.800000
GREEN=0.600000
BLUE=1.000000
INIT
n=1
t=0
PHI=1.618033988749895
PI=3.141592653589793

CODE
// Central synthesis point - Philosopher's Stone at the heart of the Tree of Life
// Combining alchemical, kabbalistic, and Matrix elements

synthesis_element = n % 4

// Alchemical core (Philosopher's Stone)
if (equal(synthesis_element,0),
  stone_angle = (n / 600) * 2 * PI
  stone_radius = 0.08 + sin(t * 3) * 0.02
  x = cos(stone_angle) * stone_radius
  y = sin(stone_angle) * stone_radius

  // Golden Philosopher's Stone
  red = 0.9, green = 0.8, blue = 0.3

  // Kabbalistic emanation from the Stone
  if (equal(synthesis_element,1),
    sephira = n % 10

    // Tree of Life Sephiroth positions scaled around the Stone
    scale_factor = 2.5
    if (equal(sephira,0), sx=0, sy=0.35*scale_factor,        // Keter
      if (equal(sephira,1), sx=0.15*scale_factor, sy=0.25*scale_factor,  // Chokmah
        if (equal(sephira,2), sx=-0.15*scale_factor, sy=0.25*scale_factor, // Binah
          if (equal(sephira,3), sx=0.2*scale_factor, sy=0.1*scale_factor,   // Chesed
            if (equal(sephira,4), sx=-0.2*scale_factor, sy=0.1*scale_factor, // Geburah
              if (equal(sephira,5), sx=0, sy=0,                            // Tiphereth
                if (equal(sephira,6), sx=0.15*scale_factor, sy=-0.15*scale_factor, // Netzach
                  if (equal(sephira,7), sx=-0.15*scale_factor, sy=-0.15*scale_factor, // Hod
                    if (equal(sephira,8), sx=0, sy=-0.25*scale_factor,     // Yesod
                      sx=0, sy=-0.4*scale_factor     // Malkuth
                    )
                  )
                )
              )
            )
          )
        )
      )
    )

    x = sx
    y = sy

    // Sephiroth colors
    if (equal(sephira,0), red=0.9, green=0.9, blue=1.0,
      if (equal(sephira,1), red=0.8, green=0.2, blue=0.8,
        if (equal(sephira,2), red=0.2, green=0.2, blue=0.8,
          if (equal(sephira,3), red=0.2, green=0.6, blue=1.0,
            if (equal(sephira,4), red=0.8, green=0.1, blue=0.1,
              if (equal(sephira,5), red=0.9, green=0.7, blue=0.2,
                if (equal(sephira,6), red=0.1, green=0.8, blue=0.3,
                  if (equal(sephira,7), red=0.8, green=0.6, blue=0.1,
                    if (equal(sephira,8), red=0.7, green=0.3, blue=0.7,
                      red=0.6, green=0.4, blue=0.2
                    )
                  )
                )
              )
            )
          )
        )
      )
    )

    // Matrix code integration
    if (equal(synthesis_element,2),
      code_stream = n % 15
      code_x = (code_stream / 15 - 0.5) * 1.8
      code_speed = 0.025 + (code_stream % 4) * 0.01
      code_y = ((t * code_speed + code_stream * 0.2) % 2.5) - 1.25

      // Code characters positioned around synthesis
      char_trail = (n / 15) % 8
      trail_offset = char_trail * 0.05
      code_y = code_y - trail_offset

      if (above(code_y, -1.25) && below(code_y, 1.25),
        x = code_x
        y = code_y

        // Matrix green with mystical variations
        red = 0.1 + sin(t * 2 + code_stream) * 0.05
        green = 0.8 + cos(t * 3 + code_stream) * 0.1
        blue = 0.2 + sin(t * 4 + code_stream) * 0.05

        // Trail fading
        trail_brightness = 1 - (char_trail / 8)
        brightness = trail_brightness * (0.5 + sin(t * 5 + char_trail) * 0.3)
        red = red * brightness
        green = green * brightness
        blue = blue * brightness
      )

      // Elemental transmutation vortices
      if (equal(synthesis_element,3),
        element = n % 4
        vortex_angle = t * (1 + element * 0.5) + element * PI/2
        vortex_radius = 0.3 + sin(t * 2 + element) * 0.1

        x = cos(vortex_angle) * vortex_radius
        y = sin(vortex_angle) * vortex_radius

        // Elemental colors
        if (equal(element,0), red=1.0, green=0.3, blue=0.1,     // Fire
          if (equal(element,1), red=0.2, green=0.6, blue=1.0,   // Water
            if (equal(element,2), red=0.3, green=0.8, blue=0.2, // Air
              red=0.6, green=0.4, blue=0.2                     // Earth
            )
          )
        )

        // Add transmutation particles
        particle_angle = (n / 4) * 2 * PI / 25
        particle_radius = vortex_radius * 0.7
        x = x + cos(particle_angle + t * 3) * particle_radius
        y = y + sin(particle_angle + t * 3) * particle_radius
      )
    )
  )
)

// Unified mystical glow
mystical_glow = sin(t * 2 + synthesis_element * PI/2) * 0.1
brightness = 0.7 + mystical_glow + getspec(0,0.1,0) * 1.5
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset01]
n=400.000000
b=1.000000
sn=Superscope (Synthesis Paths)
POINT
t=1.000000
dt=0.012000
RED=0.900000
GREEN=0.700000
BLUE=0.300000
INIT
n=1
t=0

CODE
// Connecting paths between mystical systems
// Lightning paths between Sephiroth, alchemical flows, Matrix data streams

path_type = n % 5

// Kabbalistic paths (Lightning between Sephiroth)
if (equal(path_type,0),
  path_id = n % 22

  // Get path endpoints (simplified)
  if (equal(path_id,0), start_x=0, start_y=0.35, end_x=0.15, end_y=0.25,        // Keter-Chokmah
    if (equal(path_id,5), start_x=0.15, start_y=0.25, end_x=0, end_y=0,        // Chokmah-Tiphereth
      if (equal(path_id,9), start_x=0, start_y=0, end_x=0.15, end_y=-0.15,     // Tiphereth-Netzach
        if (equal(path_id,14), start_x=0.15, start_y=-0.15, end_x=0, end_y=-0.25, // Netzach-Yesod
          if (equal(path_id,18), start_x=0, start_y=-0.25, end_x=0, end_y=-0.4,   // Yesod-Malkuth
            start_x=0, start_y=0, end_x=0, end_y=0  // Default
          )
        )
      )
    )
  )

  // Scale paths around synthesis
  scale_factor = 2.5
  start_x = start_x * scale_factor
  start_y = start_y * scale_factor
  end_x = end_x * scale_factor
  end_y = end_y * scale_factor

  // Path progress
  progress = (t * 0.8 + path_id * 0.1) % 1
  x = start_x + (end_x - start_x) * progress
  y = start_y + (end_y - start_y) * progress

  // Lightning branching
  branch = sin(t * 6 + path_id * 2) * 0.03
  path_angle = atan2(end_y - start_y, end_x - start_x)
  x = x + branch * cos(path_angle + PI/2)
  y = y + branch * sin(path_angle + PI/2)

  red = 0.9, green = 0.8, blue = 0.4,  // Golden lightning

  // Alchemical flows (Elemental circulation)
  if (equal(path_type,1),
    element_flow = n % 4
    flow_angle = t * (1.5 - element_flow * 0.2) + element_flow * PI/2
    flow_radius = 0.25 + sin(t * 2 + element_flow) * 0.08

    x = cos(flow_angle) * flow_radius
    y = sin(flow_angle) * flow_radius

    // Flow particles
    particle_count = 8
    particle_angle = (n / 4) * 2 * PI / particle_count
    particle_radius = flow_radius * 0.9
    x = x + cos(particle_angle + t * 4) * particle_radius
    y = y + sin(particle_angle + t * 4) * particle_radius

    // Elemental flow colors
    if (equal(element_flow,0), red=1.0, green=0.4, blue=0.1,     // Fire
      if (equal(element_flow,1), red=0.2, green=0.6, blue=1.0,   // Water
        if (equal(element_flow,2), red=0.3, green=0.8, blue=0.2, // Air
          red=0.6, green=0.4, blue=0.2                           // Earth
        )
      )
    )

    // Matrix data streams (Code flowing along mystical paths)
    if (equal(path_type,2),
      stream_id = n % 12
      stream_angle = (stream_id / 12) * 2 * PI
      stream_radius = 0.35 + sin(t + stream_id) * 0.1

      x = cos(stream_angle) * stream_radius
      y = sin(stream_angle) * stream_radius

      // Code characters
      char_pos = (n / 12) % 6
      char_offset = char_pos * 0.06
      char_angle = stream_angle + char_offset / stream_radius

      x = cos(char_angle) * (stream_radius - char_offset)
      y = sin(char_angle) * (stream_radius - char_offset)

      red = 0.1, green = 0.9, blue = 0.2,  // Matrix green

      // Reality glitches on code streams
      if (equal(path_type,3),
        glitch_id = n % 8
        glitch_x = (sin(t * 5 + glitch_id) - 0.5) * 1.8
        glitch_y = (cos(t * 4 + glitch_id) - 0.5) * 1.8

        // Glitch segments
        segment = (n / 8) % 6
        segment_offset = segment * 0.1
        x = glitch_x + segment_offset * cos(t * 3 + glitch_id)
        y = glitch_y + segment_offset * sin(t * 3 + glitch_id)

        red = 1.0, green = 0.2, blue = 0.2,  // Red glitches

        // Enlightenment choice visualization
        if (equal(path_type,4),
          choice = n % 2
          if (equal(choice,0),
            // Red pill path (left)
            choice_x = -0.4
            choice_angle = (n / 2) * PI / 8
            choice_radius = 0.2 + sin(t * 3 + choice_angle) * 0.05
            x = choice_x + cos(choice_angle) * choice_radius
            y = sin(choice_angle) * choice_radius
            red = 0.9, green = 0.1, blue = 0.1,  // Red pill

            // Blue pill path (right)
            choice_x = 0.4
            choice_angle = (n / 2) * PI / 8
            choice_radius = 0.2 + cos(t * 3 + choice_angle) * 0.05
            x = choice_x + cos(choice_angle) * choice_radius
            y = sin(choice_angle) * choice_radius
            red = 0.1, green = 0.3, blue = 0.9   // Blue pill
          )
        )
      )
    )
  )
)

// Mystical path energy
path_energy = sin(t * 3 + path_type * PI/3) * 0.1
brightness = 0.6 + path_energy + getspec(0,0.1,0) * 2
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset02]
n=300.000000
b=1.000000
sn=Superscope (Synthesis Symbols)
POINT
t=1.000000
dt=0.015000
RED=0.700000
GREEN=0.500000
BLUE=0.900000
INIT
n=1
t=0

CODE
// Mystical symbols from all three traditions
// Alchemical symbols, Hebrew letters, Matrix code elements

symbol_category = n % 3

// Alchemical symbols
if (equal(symbol_category,0),
  alchemy_symbol = n % 8

  // Position symbols in alchemical circle
  symbol_angle = alchemy_symbol * 2 * PI / 8
  symbol_radius = 0.4
  symbol_x = cos(symbol_angle) * symbol_radius
  symbol_y = sin(symbol_angle) * symbol_radius

  x = symbol_x
  y = symbol_y

  // Different alchemical symbols
  if (equal(alchemy_symbol,0),
    // Philosopher's Stone (circle with dot)
    stone_angle = (n / 8) * 2 * PI / 8
    stone_radius = 0.03
    x = symbol_x + cos(stone_angle) * stone_radius
    y = symbol_y + sin(stone_angle) * stone_radius
    red = 0.9, green = 0.8, blue = 0.3,  // Gold

    // Sulfur (triangle with cross)
    if (equal(alchemy_symbol,1),
      vertex = (n / 8) % 3
      if (equal(vertex,0), rel_x = 0, rel_y = 0.025,
        if (equal(vertex,1), rel_x = -0.02, rel_y = -0.025,
          rel_x = 0.02, rel_y = -0.025
        )
      )
      x = symbol_x + rel_x
      y = symbol_y + rel_y
      red = 1.0, green = 0.6, blue = 0.1,  // Yellow

      // Mercury (crescent)
      if (equal(alchemy_symbol,2),
        crescent_angle = (n / 8) * PI / 6
        crescent_radius = 0.025
        x = symbol_x + cos(crescent_angle) * crescent_radius
        y = symbol_y + sin(crescent_angle) * crescent_radius
        red = 0.7, green = 0.7, blue = 0.8   // Silver
      )
    )
  )

  // Hebrew letters on paths
  if (equal(symbol_category,1),
    letter_position = n % 22

    // Position letters along Tree of Life paths (simplified)
    if (equal(letter_position,0), path_x=0, path_y=0.3,      // Aleph
      if (equal(letter_position,5), path_x=0.1, path_y=0.1,   // Vav
        if (equal(letter_position,9), path_x=0, path_y=-0.1,   // Yod
          if (equal(letter_position,14), path_x=0.1, path_y=-0.3, // Samekh
            path_x=0, path_y=0  // Default
          )
        )
      )
    )

    // Scale around synthesis
    scale_factor = 2.0
    path_x = path_x * scale_factor
    path_y = path_y * scale_factor

    x = path_x
    y = path_y

    red = 0.4, green = 0.3, blue = 0.8,  // Mystical purple

    // Matrix code elements
    if (equal(symbol_category,2),
      code_element = n % 4

      // Binary streams
      if (equal(code_element,0),
        stream_id = n % 6
        stream_x = (stream_id / 6 - 0.5) * 1.4
        stream_speed = 0.02
        stream_y = ((t * stream_speed + stream_id * 0.1) % 2.0) - 1.0

        x = stream_x
        y = stream_y

        red = 0.1, green = 0.9, blue = 0.2,  // Matrix green

        // Glitch effects
        if (equal(code_element,1),
          glitch_x = sin(t * 6 + n) * 0.3
          glitch_y = cos(t * 5 + n) * 0.3

          x = glitch_x
          y = glitch_y

          red = 1.0, green = 0.2, blue = 0.2,  // Red glitch

          // Reality distortion fields
          if (equal(code_element,2),
            field_angle = (n / 3) * 2 * PI / 10
            field_radius = 0.2 + sin(t * 4 + field_angle) * 0.1

            x = cos(field_angle) * field_radius
            y = sin(field_angle) * field_radius

            red = 0.6, green = 0.4, blue = 0.9,  // Purple distortion

            // Enlightenment particles
            if (equal(code_element,3),
              particle_angle = (n / 3) * 2 * PI / 15
              particle_radius = 0.15 + cos(t * 3 + particle_angle) * 0.08

              x = cos(particle_angle) * particle_radius
              y = sin(particle_angle) * particle_radius

              red = 0.9, green = 0.8, blue = 0.4   // Golden enlightenment
            )
          )
        )
      )
    )
  )
)

// Symbol animation and energy
symbol_pulse = sin(t * 4 + symbol_category * PI/3) * 0.05
x = x + symbol_pulse * cos(t * 2 + n)
y = y + symbol_pulse * sin(t * 2 + n)

brightness = 0.5 + getspec(0,0.1,0) * 2
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset03]
n=250.000000
b=1.000000
sn=Superscope (Synthesis Energy)
POINT
t=1.000000
dt=0.018000
RED=0.900000
GREEN=0.600000
BLUE=0.800000
INIT
n=1
t=0

CODE
// Unified energy flow through all mystical systems
// Golden flow connecting alchemy, kabbalah, and Matrix

energy_type = n % 4

// Philosopher's Stone emanation (Golden flow)
if (equal(energy_type,0),
  emanation_angle = (n / 250) * 2 * PI
  emanation_radius = 0.05 + sin(t * 2) * 0.02

  // Central stone
  x = cos(emanation_angle) * emanation_radius
  y = sin(emanation_angle) * emanation_radius

  // Emanation rays
  ray_count = 12
  ray_id = n % ray_count
  ray_angle = ray_id * 2 * PI / ray_count
  ray_length = 0.3 + sin(t * 3 + ray_id) * 0.1

  ray_progress = (n / ray_count) % 20 / 19
  x = cos(ray_angle) * ray_length * ray_progress
  y = sin(ray_angle) * ray_length * ray_progress

  red = 0.9, green = 0.8, blue = 0.3,  // Gold

  // Sephiroth energy flow
  if (equal(energy_type,1),
    flow_particle = n % 50
    sephira_target = flow_particle % 10

    // Flow to each Sephira
    if (equal(sephira_target,0), target_x=0, target_y=0.35,
      if (equal(sephira_target,5), target_x=0, target_y=0,
        if (equal(sephira_target,8), target_x=0, target_y=-0.25,
          target_x=0, target_y=-0.4
        )
      )
    )

    // Scale targets
    target_x = target_x * 2.5
    target_y = target_y * 2.5

    // Flow progress
    flow_progress = (t * 0.6 + flow_particle * 0.02) % 1
    x = target_x * flow_progress
    y = target_y * flow_progress

    red = 0.8, green = 0.6, blue = 1.0,  // Mystical purple

    // Matrix data flow
    if (equal(energy_type,2),
      data_stream = n % 20
      data_x = (data_stream / 20 - 0.5) * 1.6
      data_speed = 0.03 + (data_stream % 4) * 0.01
      data_y = ((t * data_speed + data_stream * 0.15) % 2.2) - 1.1

      x = data_x
      y = data_y

      red = 0.1, green = 0.8, blue = 0.3,  // Matrix green

      // Reality synthesis waves
      if (equal(energy_type,3),
        wave_layer = n % 3
        wave_angle = (n / 3) * 2 * PI / 30
        wave_radius = 0.2 + wave_layer * 0.15
        wave_phase = t * (2 + wave_layer) + wave_layer * PI/3

        x = cos(wave_angle + wave_phase) * wave_radius
        y = sin(wave_angle + wave_phase) * wave_radius

        // Layer colors
        if (equal(wave_layer,0), red = 0.9, green = 0.7, blue = 0.2,  // Alchemy gold
          if (equal(wave_layer,1), red = 0.6, green = 0.4, blue = 0.9,  // Kabbalah purple
            red = 0.2, green = 0.9, blue = 0.4                         // Matrix green
          )
        )

        // Synthesis particles
        particle_angle = (n / 3) * 2 * PI / 25
        particle_radius = wave_radius * 0.8
        particle_phase = t * 4 + wave_layer * PI/6

        x = x + cos(particle_angle + particle_phase) * particle_radius
        y = y + sin(particle_angle + particle_phase) * particle_radius
      )
    )
  )
)

// Energy intensity and flow
energy_flow = sin(t * 3 + energy_type * PI/2) * 0.1
brightness = 0.6 + energy_flow + getspec(0,0.1,0) * 2.5

// High energy creates synthesis effects
if (above(getspec(0,0.1,0), 0.4),
  synthesis_boost = getspec(0,0.1,0) * 1.5
  brightness = brightness + synthesis_boost
  // Add synthesis sparkles
  sparkle = sin(t * 8 + n) * 0.05
  x = x + sparkle * cos(t * 6 + n)
  y = y + sparkle * sin(t * 6 + n)
)

red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset04]
n=150.000000
b=1.000000
sn=Superscope (Enlightenment Choice)
POINT
t=1.000000
dt=0.020000
RED=0.800000
GREEN=0.200000
BLUE=0.200000
INIT
n=1
t=0

CODE
// The ultimate choice: Enlightenment through mystical synthesis
// Red pill (mystical awakening) vs Blue pill (comfortable illusion)

choice_element = n % 3

// Red Pill Path (Left - Mystical Enlightenment)
if (equal(choice_element,0),
  red_pill_angle = (n / 150) * PI / 6
  red_pill_radius = 0.25 + sin(t * 2) * 0.05

  x = -0.4 + cos(red_pill_angle) * red_pill_radius
  y = sin(red_pill_angle) * red_pill_radius

  red = 0.9, green = 0.1, blue = 0.1,  // Red

  // Enlightenment symbols on red path
  if (equal(n % 10, 0),
    symbol_angle = (n / 10) * 2 * PI / 5
    symbol_radius = red_pill_radius * 0.8
    x = -0.4 + cos(symbol_angle) * symbol_radius
    y = sin(symbol_angle) * symbol_radius
    red = 0.9, green = 0.8, blue = 0.3   // Golden enlightenment
  )

  // Blue Pill Path (Right - Comfortable Reality)
  if (equal(choice_element,1),
    blue_pill_angle = (n / 150) * PI / 6
    blue_pill_radius = 0.25 + cos(t * 2) * 0.05

    x = 0.4 + cos(blue_pill_angle) * blue_pill_radius
    y = sin(blue_pill_angle) * blue_pill_radius

    red = 0.1, green = 0.3, blue = 0.9,  // Blue

    // Comfort symbols on blue path
    if (equal(n % 10, 5),
      symbol_angle = (n / 10) * 2 * PI / 5
      symbol_radius = blue_pill_radius * 0.8
      x = 0.4 + cos(symbol_angle) * symbol_radius
      y = sin(symbol_angle) * symbol_radius
      red = 0.7, green = 0.7, blue = 0.9   // Soft blue comfort
    )

    // Synthesis Bridge (Center - Unity of All Paths)
    if (equal(choice_element,2),
      bridge_progress = (n / 150) % 1
      bridge_height = sin(bridge_progress * PI) * 0.2

      x = -0.4 + (0.4 - (-0.4)) * bridge_progress
      y = bridge_height

      // Bridge color shifts from red to blue
      color_shift = bridge_progress
      red = 0.9 - color_shift * 0.8
      green = 0.1 + color_shift * 0.2
      blue = 0.1 + color_shift * 0.8

      // Unity particles along bridge
      if (equal(n % 5, 0),
        particle_offset = sin(t * 4 + n) * 0.03
        x = x + particle_offset
        y = y + cos(t * 4 + n) * 0.02
        red = 0.9, green = 0.8, blue = 0.4   // Golden unity
      )
    )
  )
)

// Choice intensity based on mystical awareness
choice_intensity = sin(t * 1.5) * 0.2
brightness = 0.7 + choice_intensity + getspec(0,0.1,0) * 2

// High energy moments create choice pressure
if (above(getspec(0,0.1,0), 0.6),
  choice_pressure = getspec(0,0.1,0) * 1.2
  brightness = brightness + choice_pressure
  // Add choice sparkles
  choice_sparkle = sin(t * 6 + n) * choice_pressure * 0.08
  x = x + choice_sparkle * cos(t * 5 + n)
  y = y + choice_sparkle * sin(t * 5 + n)
)

red = red * brightness
green = green * brightness
blue = blue * brightness

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
