[avs]
PRESET_NAME=Kabbalistic Tree of Life
DESCRIPTION=Kabbalistic Tree of Life (Etz Chaim) with 10 Sephiroth, 22 paths, Hebrew letters, and energy flows. Shows the divine emanation structure, lightning paths, and mystical numerology of the Kabbalah.

[preset00]
n=400.000000
b=1.000000
sn=Superscope (Tree Structure)
POINT
t=1.000000
dt=0.012000
RED=0.600000
GREEN=0.400000
BLUE=0.800000
INIT
n=1
t=0
PI=3.141592653589793
PHI=1.618033988749895

CODE
// Kabbalistic Tree of Life structure with 10 Sephiroth and 22 paths
// Based on authentic Kabbalistic geometry and Sephiroth positions

// Sephiroth positions (authentic Kabbalistic layout)
sephira = n % 10

// Keter (Crown) - top center
if (equal(sephira,0),
  x = 0, y = 0.35,
  // Chokmah (Wisdom) - top right
  if (equal(sephira,1),
    x = 0.15, y = 0.25,
    // Binah (Understanding) - top left
    if (equal(sephira,2),
      x = -0.15, y = 0.25,
      // Chesed (Mercy) - middle right
      if (equal(sephira,3),
        x = 0.2, y = 0.1,
        // Geburah (Severity) - middle left
        if (equal(sephira,4),
          x = -0.2, y = 0.1,
          // Tiphereth (Beauty) - center
          if (equal(sephira,5),
            x = 0, y = 0,
            // Netzach (Victory) - bottom right
            if (equal(sephira,6),
              x = 0.15, y = -0.15,
              // Hod (Glory) - bottom left
              if (equal(sephira,7),
                x = -0.15, y = -0.15,
                // Yesod (Foundation) - bottom center
                if (equal(sephira,8),
                  x = 0, y = -0.25,
                  // Malkuth (Kingdom) - bottom center lowest
                  x = 0, y = -0.4
                )
              )
            )
          )
        )
      )
    )
  )
)

// Add pulsing effect to Sephiroth
pulse = sin(t * 2 + sephira) * 0.02
x = x + pulse * cos(sephira * PI/5)
y = y + pulse * sin(sephira * PI/5)

// Sephiroth colors based on Kabbalistic associations
if (equal(sephira,0), red = 0.9, green = 0.9, blue = 1.0,      // Keter - White
  if (equal(sephira,1), red = 0.8, green = 0.2, blue = 0.8,    // Chokmah - Purple
    if (equal(sephira,2), red = 0.2, green = 0.2, blue = 0.8,  // Binah - Dark Blue
      if (equal(sephira,3), red = 0.2, green = 0.6, blue = 1.0, // Chesed - Light Blue
        if (equal(sephira,4), red = 0.8, green = 0.1, blue = 0.1, // Geburah - Red
          if (equal(sephira,5), red = 0.9, green = 0.7, blue = 0.2, // Tiphereth - Gold
            if (equal(sephira,6), red = 0.1, green = 0.8, blue = 0.3, // Netzach - Green
              if (equal(sephira,7), red = 0.8, green = 0.6, blue = 0.1, // Hod - Orange
                if (equal(sephira,8), red = 0.7, green = 0.3, blue = 0.7, // Yesod - Purple
                  red = 0.6, green = 0.4, blue = 0.2   // Malkuth - Brown
                )
              )
            )
          )
        )
      )
    )
  )
)

// Audio-reactive Sephiroth glow
audio_glow = getspec(0,0.1,0) * 2
brightness = 0.5 + audio_glow
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset01]
n=350.000000
b=1.000000
sn=Superscope (Lightning Paths)
POINT
t=1.000000
dt=0.010000
RED=1.000000
GREEN=0.800000
BLUE=0.400000
INIT
n=1
t=0

CODE
// Lightning paths connecting the Sephiroth
// 22 paths representing the Hebrew alphabet connections

path = n % 22

// Define start and end Sephiroth for each path
// Using authentic Kabbalistic path connections
if (equal(path,0), start_seph=0, end_seph=1,      // Keter to Chokmah (Aleph)
  if (equal(path,1), start_seph=0, end_seph=2,    // Keter to Binah (Beth)
    if (equal(path,2), start_seph=1, end_seph=2,  // Chokmah to Binah (Gimel)
      if (equal(path,3), start_seph=1, end_seph=3, // Chokmah to Chesed (Daleth)
        if (equal(path,4), start_seph=2, end_seph=4, // Binah to Geburah (Heh)
          if (equal(path,5), start_seph=1, end_seph=5, // Chokmah to Tiphereth (Vav)
            if (equal(path,6), start_seph=2, end_seph=5, // Binah to Tiphereth (Zayin)
              if (equal(path,7), start_seph=3, end_seph=5, // Chesed to Tiphereth (Cheth)
                if (equal(path,8), start_seph=4, end_seph=5, // Geburah to Tiphereth (Teth)
                  if (equal(path,9), start_seph=5, end_seph=6, // Tiphereth to Netzach (Yod)
                    if (equal(path,10), start_seph=5, end_seph=7, // Tiphereth to Hod (Kaph)
                      if (equal(path,11), start_seph=5, end_seph=8, // Tiphereth to Yesod (Lamed)
                        if (equal(path,12), start_seph=3, end_seph=6, // Chesed to Netzach (Mem)
                          if (equal(path,13), start_seph=4, end_seph=7, // Geburah to Hod (Nun)
                            if (equal(path,14), start_seph=6, end_seph=8, // Netzach to Yesod (Samekh)
                              if (equal(path,15), start_seph=7, end_seph=8, // Hod to Yesod (Ayin)
                                if (equal(path,16), start_seph=6, end_seph=9, // Netzach to Malkuth (Peh)
                                  if (equal(path,17), start_seph=7, end_seph=9, // Hod to Malkuth (Tzaddi)
                                    if (equal(path,18), start_seph=8, end_seph=9, // Yesod to Malkuth (Qoph)
                                      if (equal(path,19), start_seph=3, end_seph=8, // Chesed to Yesod (Resh)
                                        if (equal(path,20), start_seph=4, end_seph=8, // Geburah to Yesod (Shin)
                                          start_seph=5, end_seph=9   // Tiphereth to Malkuth (Tav)
                                        )
                                      )
                                    )
                                  )
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )
)

// Get Sephiroth positions
// Keter
if (equal(start_seph,0), start_x=0, start_y=0.35,
  if (equal(start_seph,1), start_x=0.15, start_y=0.25,
    if (equal(start_seph,2), start_x=-0.15, start_y=0.25,
      if (equal(start_seph,3), start_x=0.2, start_y=0.1,
        if (equal(start_seph,4), start_x=-0.2, start_y=0.1,
          if (equal(start_seph,5), start_x=0, start_y=0,
            if (equal(start_seph,6), start_x=0.15, start_y=-0.15,
              if (equal(start_seph,7), start_x=-0.15, start_y=-0.15,
                if (equal(start_seph,8), start_x=0, start_y=-0.25,
                  start_x=0, start_y=-0.4  // Malkuth
                )
              )
            )
          )
        )
      )
    )
  )
)

// Malkuth
if (equal(end_seph,0), end_x=0, end_y=0.35,
  if (equal(end_seph,1), end_x=0.15, end_y=0.25,
    if (equal(end_seph,2), end_x=-0.15, end_y=0.25,
      if (equal(end_seph,3), end_x=0.2, end_y=0.1,
        if (equal(end_seph,4), end_x=-0.2, end_y=0.1,
          if (equal(end_seph,5), end_x=0, end_y=0,
            if (equal(end_seph,6), end_x=0.15, end_y=-0.15,
              if (equal(end_seph,7), end_x=-0.15, end_y=-0.15,
                if (equal(end_seph,8), end_x=0, end_y=-0.25,
                  end_x=0, end_y=-0.4  // Malkuth
                )
              )
            )
          )
        )
      )
    )
  )
)

// Calculate position along the path
path_progress = (n / 350) % 1
path_length = sqrt((end_x - start_x)^2 + (end_y - start_y)^2)
if (above(path_length, 0),
  x = start_x + (end_x - start_x) * path_progress
  y = start_y + (end_y - start_y) * path_progress
)

// Add lightning branching effect
lightning_branch = sin(t * 8 + path * 0.5) * 0.02
branch_angle = atan2(end_y - start_y, end_x - start_x)
x = x + lightning_branch * cos(branch_angle + PI/2)
y = y + lightning_branch * sin(branch_angle + PI/2)

// Lightning color (bright white/blue)
red = 0.9 + sin(t * 10 + path) * 0.1
green = 0.9 + cos(t * 10 + path) * 0.1
blue = 1.0

// Audio-reactive lightning intensity
lightning_energy = getspec(0,0.1,0) * 3
brightness = 0.6 + lightning_energy
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset02]
n=280.000000
b=1.000000
sn=Superscope (Hebrew Letters)
POINT
t=1.000000
dt=0.016000
RED=0.800000
GREEN=0.600000
BLUE=1.000000
INIT
n=1
t=0

CODE
// Hebrew letters positioned on the 22 paths
// Each letter has mystical significance in Kabbalah

letter_index = n % 22
path_progress = 0.5  // Center of each path

// Get path endpoints for letter positioning
if (equal(letter_index,0), start_x=0, start_y=0.35, end_x=0.15, end_y=0.25,      // Aleph
  if (equal(letter_index,1), start_x=0, start_y=0.35, end_x=-0.15, end_y=0.25,   // Beth
    if (equal(letter_index,2), start_x=0.15, start_y=0.25, end_x=-0.15, end_y=0.25, // Gimel
      if (equal(letter_index,3), start_x=0.15, start_y=0.25, end_x=0.2, end_y=0.1,   // Daleth
        if (equal(letter_index,4), start_x=-0.15, start_y=0.25, end_x=-0.2, end_y=0.1, // Heh
          if (equal(letter_index,5), start_x=0.15, start_y=0.25, end_x=0, end_y=0,     // Vav
            if (equal(letter_index,6), start_x=-0.15, start_y=0.25, end_x=0, end_y=0,  // Zayin
              if (equal(letter_index,7), start_x=0.2, start_y=0.1, end_x=0, end_y=0,    // Cheth
                if (equal(letter_index,8), start_x=-0.2, start_y=0.1, end_x=0, end_y=0, // Teth
                  if (equal(letter_index,9), start_x=0, start_y=0, end_x=0.15, end_y=-0.15, // Yod
                    if (equal(letter_index,10), start_x=0, start_y=0, end_x=-0.15, end_y=-0.15, // Kaph
                      if (equal(letter_index,11), start_x=0, start_y=0, end_x=0, end_y=-0.25,    // Lamed
                        if (equal(letter_index,12), start_x=0.2, start_y=0.1, end_x=0.15, end_y=-0.15, // Mem
                          if (equal(letter_index,13), start_x=-0.2, start_y=0.1, end_x=-0.15, end_y=-0.15, // Nun
                            if (equal(letter_index,14), start_x=0.15, start_y=-0.15, end_x=0, end_y=-0.25,   // Samekh
                              if (equal(letter_index,15), start_x=-0.15, start_y=-0.15, end_x=0, end_y=-0.25, // Ayin
                                if (equal(letter_index,16), start_x=0.15, start_y=-0.15, end_x=0, end_y=-0.4,   // Peh
                                  if (equal(letter_index,17), start_x=-0.15, start_y=-0.15, end_x=0, end_y=-0.4, // Tzaddi
                                    if (equal(letter_index,18), start_x=0, start_y=-0.25, end_x=0, end_y=-0.4,    // Qoph
                                      if (equal(letter_index,19), start_x=0.2, start_y=0.1, end_x=0, end_y=-0.25,   // Resh
                                        if (equal(letter_index,20), start_x=-0.2, start_y=0.1, end_x=0, end_y=-0.25, // Shin
                                          start_x=0, start_y=0, end_x=0, end_y=-0.4    // Tav
                                        )
                                      )
                                    )
                                  )
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )
)

// Position letter at center of path
x = start_x + (end_x - start_x) * path_progress
y = start_y + (end_y - start_y) * path_progress

// Hebrew letter representations (simplified geometric forms)
// Aleph - Mother letter, air, ox (represented as diagonal cross)
if (equal(letter_index,0),
  // Diagonal cross
  if (equal(n % 4, 0), x = x - 0.02, y = y - 0.02,   // Top-left to bottom-right
    if (equal(n % 4, 1), x = x + 0.02, y = y + 0.02,   // Bottom-right to top-left
      if (equal(n % 4, 2), x = x - 0.02, y = y + 0.02, // Bottom-left to top-right
        x = x + 0.02, y = y - 0.02    // Top-right to bottom-left
      )
    )
  ),

  // Shin - Fire letter (represented as three upward branches)
  if (equal(letter_index,20),
    branch = (n % 9) % 3
    if (equal(branch, 0), x = x, y = y - 0.01,          // Center stem
      if (equal(branch, 1), x = x - 0.015, y = y - 0.005, // Left branch
        x = x + 0.015, y = y - 0.005    // Right branch
      )
    ),
    // Other letters represented as simple circles
    angle = (n % 16) * 2 * PI / 16
    radius = 0.008
    x = x + cos(angle) * radius
    y = y + sin(angle) * radius
  )
)

// Mystical letter color (deep blue/purple)
red = 0.3 + sin(t * 2 + letter_index) * 0.2
green = 0.2 + cos(t * 2 + letter_index) * 0.1
blue = 0.8 + sin(t * 3 + letter_index) * 0.2

// Audio-reactive letter glow
letter_glow = getspec(0,0.1,0) * 2
brightness = 0.4 + letter_glow
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset03]
n=200.000000
b=1.000000
sn=Superscope (Energy Flow)
POINT
t=1.000000
dt=0.014000
RED=0.900000
GREEN=0.700000
BLUE=0.100000
INIT
n=1
t=0

CODE
// Energy flow particles moving along the Tree of Life paths
// Represents the divine light flowing through the Sephiroth

particle_id = n % 200
path_id = particle_id % 22

// Particle progress along its path (0 to 1)
progress = (t * 0.5 + particle_id * 0.1) % 1

// Get path endpoints
if (equal(path_id,0), start_x=0, start_y=0.35, end_x=0.15, end_y=0.25,
  if (equal(path_id,1), start_x=0, start_y=0.35, end_x=-0.15, end_y=0.25,
    if (equal(path_id,2), start_x=0.15, start_y=0.25, end_x=-0.15, end_y=0.25,
      if (equal(path_id,3), start_x=0.15, start_y=0.25, end_x=0.2, end_y=0.1,
        if (equal(path_id,4), start_x=-0.15, start_y=0.25, end_x=-0.2, end_y=0.1,
          if (equal(path_id,5), start_x=0.15, start_y=0.25, end_x=0, end_y=0,
            if (equal(path_id,6), start_x=-0.15, start_y=0.25, end_x=0, end_y=0,
              if (equal(path_id,7), start_x=0.2, start_y=0.1, end_x=0, end_y=0,
                if (equal(path_id,8), start_x=-0.2, start_y=0.1, end_x=0, end_y=0,
                  if (equal(path_id,9), start_x=0, start_y=0, end_x=0.15, end_y=-0.15,
                    if (equal(path_id,10), start_x=0, start_y=0, end_x=-0.15, end_y=-0.15,
                      if (equal(path_id,11), start_x=0, start_y=0, end_x=0, end_y=-0.25,
                        if (equal(path_id,12), start_x=0.2, start_y=0.1, end_x=0.15, end_y=-0.15,
                          if (equal(path_id,13), start_x=-0.2, start_y=0.1, end_x=-0.15, end_y=-0.15,
                            if (equal(path_id,14), start_x=0.15, start_y=-0.15, end_x=0, end_y=-0.25,
                              if (equal(path_id,15), start_x=-0.15, start_y=-0.15, end_x=0, end_y=-0.25,
                                if (equal(path_id,16), start_x=0.15, start_y=-0.15, end_x=0, end_y=-0.4,
                                  if (equal(path_id,17), start_x=-0.15, start_y=-0.15, end_x=0, end_y=-0.4,
                                    if (equal(path_id,18), start_x=0, start_y=-0.25, end_x=0, end_y=-0.4,
                                      if (equal(path_id,19), start_x=0.2, start_y=0.1, end_x=0, end_y=-0.25,
                                        if (equal(path_id,20), start_x=-0.2, start_y=0.1, end_x=0, end_y=-0.25,
                                          start_x=0, start_y=0, end_x=0, end_y=-0.4
                                        )
                                      )
                                    )
                                  )
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        )
      )
    )
  )
)

// Calculate particle position along path
x = start_x + (end_x - start_x) * progress
y = start_y + (end_y - start_y) * progress

// Add flowing motion to energy particles
flow_offset = sin(t * 4 + particle_id * 0.5) * 0.01
path_angle = atan2(end_y - start_y, end_x - start_x)
x = x + flow_offset * cos(path_angle + PI/2)
y = y + flow_offset * sin(path_angle + PI/2)

// Energy particle color based on Sephiroth associations
if (below(path_id, 3), red = 0.9, green = 0.9, blue = 1.0,        // Upper paths - white/light
  if (below(path_id, 9), red = 0.6, green = 0.4, blue = 0.8,      // Middle paths - purple
    red = 0.8, green = 0.6, blue = 0.2                           // Lower paths - gold
  )
)

// Audio-reactive energy intensity
energy_flow = getspec(0,0.1,0) * 3
brightness = 0.3 + energy_flow
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset04]
n=150.000000
b=1.000000
sn=Superscope (Three Pillars)
POINT
t=1.000000
dt=0.018000
RED=0.500000
GREEN=0.800000
BLUE=0.500000
INIT
n=1
t=0

CODE
// The Three Pillars of the Tree of Life
// Pillar of Mercy (right), Pillar of Severity (left), Pillar of Balance (center)

pillar = n % 3

// Pillar of Mercy (right side - Chesed, Netzach)
if (equal(pillar,0),
  pillar_x = 0.18
  if (below(n, 50),
    // Chesed position
    pillar_y = 0.1,
    // Netzach position
    pillar_y = -0.15
  )
  red = 0.2, green = 0.6, blue = 1.0,  // Light blue for Mercy

  // Pillar of Severity (left side - Geburah, Hod)
  if (equal(pillar,1),
    pillar_x = -0.18
    if (below(n, 50),
      // Geburah position
      pillar_y = 0.1,
      // Hod position
      pillar_y = -0.15
    )
    red = 0.8, green = 0.1, blue = 0.1,  // Red for Severity

    // Pillar of Balance (center - Keter, Tiphereth, Yesod, Malkuth)
    if (equal(pillar,2),
      pillar_x = 0
      if (below(n, 37),
        pillar_y = 0.35,  // Keter
        if (below(n, 74),
          pillar_y = 0,   // Tiphereth
          if (below(n, 111),
            pillar_y = -0.25, // Yesod
            pillar_y = -0.4   // Malkuth
          )
        )
      )
      red = 0.9, green = 0.7, blue = 0.2   // Gold for Balance
    )
  )
)

// Add pillar visualization lines
x = pillar_x
y = pillar_y

// Pillar connection lines (vertical)
if (equal(n % 10, 0),
  // Draw vertical lines connecting Sephiroth in each pillar
  if (equal(pillar,0),  // Mercy pillar
    line_progress = (n / 150) * 2 - 1
    if (below(line_progress, 0),
      x = 0.18, y = 0.1 + line_progress * 0.25,  // Chesed to Netzach
      x = 0.18, y = -0.15 + (line_progress + 1) * 0.25
    ),
    if (equal(pillar,1),  // Severity pillar
      line_progress = (n / 150) * 2 - 1
      if (below(line_progress, 0),
        x = -0.18, y = 0.1 + line_progress * 0.25,  // Geburah to Hod
        x = -0.18, y = -0.15 + (line_progress + 1) * 0.25
      ),
      if (equal(pillar,2),  // Balance pillar
        line_progress = (n / 150) * 4 - 2
        if (below(line_progress, 0.5),
          x = 0, y = 0.35 + line_progress * 0.35,  // Keter to Tiphereth
          if (below(line_progress, 1.5),
            x = 0, y = 0 + (line_progress - 0.5) * 0.25,  // Tiphereth to Yesod
            x = 0, y = -0.25 + (line_progress - 1.5) * 0.15  // Yesod to Malkuth
          )
        )
      )
    )
  )
)

// Pillar energy vibration
vibration = sin(t * 3 + pillar * 2) * 0.01
x = x + vibration * cos(pillar * PI/3)
y = y + vibration * sin(pillar * PI/3)

// Audio-reactive pillar resonance
pillar_resonance = getspec(0,0.1,0) * 2
brightness = 0.5 + pillar_resonance
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
