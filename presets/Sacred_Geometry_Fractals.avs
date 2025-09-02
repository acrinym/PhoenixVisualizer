[avs]
PRESET_NAME=Sacred Geometry Fractals
DESCRIPTION=Sacred geometry patterns with fractal recursion and 3D shader effects. Mathematical relationships visualized through golden ratio spirals, Metatron's Cube, and fractal recursion.

[preset00]
n=300.000000
b=1.000000
sn=Superscope (Metatrons Cube)
POINT
t=1.000000
dt=0.012000
RED=0.800000
GREEN=0.600000
BLUE=1.000000
INIT
n=1
t=0
PHI=1.618033988749895
PI=3.141592653589793

CODE
// Metatron's Cube - Sacred geometry visualization
// 13 circles arranged in Flower of Life pattern

circle_index = n % 13
center_distance = 0.25

// Calculate positions for 13 circles in Metatron's Cube
if (equal(circle_index,0),
  // Central circle
  cx=0, cy=0,
  if (equal(circle_index,1),
    // Inner ring (6 circles)
    cx=cos(0)*center_distance, cy=sin(0)*center_distance,
    if (equal(circle_index,2),
      cx=cos(PI/3)*center_distance, cy=sin(PI/3)*center_distance,
      if (equal(circle_index,3),
        cx=cos(2*PI/3)*center_distance, cy=sin(2*PI/3)*center_distance,
        if (equal(circle_index,4),
          cx=cos(PI)*center_distance, cy=sin(PI)*center_distance,
          if (equal(circle_index,5),
            cx=cos(4*PI/3)*center_distance, cy=sin(4*PI/3)*center_distance,
            if (equal(circle_index,6),
              cx=cos(5*PI/3)*center_distance, cy=sin(5*PI/3)*center_distance,
              if (equal(circle_index,7),
                // Outer ring (6 circles)
                cx=cos(PI/6)*center_distance*2, cy=sin(PI/6)*center_distance*2,
                if (equal(circle_index,8),
                  cx=cos(PI/2)*center_distance*2, cy=sin(PI/2)*center_distance*2,
                  if (equal(circle_index,9),
                    cx=cos(5*PI/6)*center_distance*2, cy=sin(5*PI/6)*center_distance*2,
                    if (equal(circle_index,10),
                      cx=cos(7*PI/6)*center_distance*2, cy=sin(7*PI/6)*center_distance*2,
                      if (equal(circle_index,11),
                        cx=cos(3*PI/2)*center_distance*2, cy=sin(3*PI/2)*center_distance*2,
                        if (equal(circle_index,12),
                          cx=cos(11*PI/6)*center_distance*2, cy=sin(11*PI/6)*center_distance*2,
                          cx=0, cy=0  // Fallback
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

// Calculate point on circle circumference
circle_radius = 0.08
angle = t * 2 + circle_index * 0.5
x = cx + cos(angle) * circle_radius
y = cy + sin(angle) * circle_radius

// Audio-reactive modulation
audio_scale = 1 + getspec(0,0.2,0) * 2
x = x * audio_scale
y = y * audio_scale

// Color based on circle position and audio
distance_from_center = sqrt(cx*cx + cy*cy)
red = 0.5 + 0.5 * cos(distance_from_center * 10 + t)
green = 0.4 + 0.6 * sin(distance_from_center * 8 + t * 1.2)
blue = 0.8 + 0.2 * cos(distance_from_center * 12 + t * 0.8)

// Energy modulation
energy = getspec(0,0.1,0) * 3
red = red * (0.2 + energy)
green = green * (0.2 + energy)
blue = blue * (0.2 + energy)

FRAME
t=t+dt

[preset01]
n=250.000000
b=1.000000
sn=Superscope (Golden Spiral)
POINT
t=1.000000
dt=0.010000
RED=1.000000
GREEN=0.800000
BLUE=0.400000
INIT
n=1
t=0
PHI=1.618033988749895

CODE
// Golden spiral using Fibonacci sequence and golden ratio
// Logarithmic spiral with golden ratio proportions

spiral_turns = 3
max_radius = 0.4
angle_step = (n / 250) * spiral_turns * 2 * PI

// Golden spiral formula: r = a * phi^(2*theta/PI)
growth_factor = pow(PHI, angle_step / PI)
radius = max_radius * growth_factor / pow(PHI, spiral_turns * 2)

x = cos(angle_step) * radius
y = sin(angle_step) * radius

// Add golden ratio rectangles along the spiral
rect_size = radius * 0.1
// Rectangle visualization through additional points
if (equal(n % 10, 0),
  // Draw rectangle corners
  rect_angle = angle_step - PI/4
  rect_x = cos(rect_angle) * (radius * 1.1)
  rect_y = sin(rect_angle) * (radius * 1.1)
  x = rect_x
  y = rect_y
)

// Audio-reactive spiral tightening
audio_tighten = getspec(0,0.1,0) * 0.5
x = x * (1 - audio_tighten)
y = y * (1 - audio_tighten)

// Color gradient along spiral
color_pos = (n / 250)
red = 1.0 - color_pos * 0.5
green = 0.8 - color_pos * 0.3
blue = 0.4 + color_pos * 0.4

// Brightness based on spiral position
brightness = 0.3 + (1 - color_pos) * 0.7
red = red * brightness
green = green * brightness
blue = blue * brightness

FRAME
t=t+dt

[preset02]
n=200.000000
b=1.000000
sn=Superscope (Fractal Recursion)
POINT
t=1.000000
dt=0.015000
RED=0.400000
GREEN=0.800000
BLUE=0.600000
INIT
n=1
t=0
fractal_depth=4

CODE
// Fractal recursion visualization
// Self-similar patterns repeating at different scales

// Calculate fractal level and position
fractal_level = n % fractal_depth
base_angle = (n / 200) * 4 * PI

// Recursive scaling factor
scale = pow(0.5, fractal_level)
angle = base_angle + fractal_level * PI/3

// Fractal position calculation
x = cos(angle) * scale * 0.5
y = sin(angle) * scale * 0.5

// Add fractal branches
branch_factor = fractal_level * 0.3
x = x + cos(angle * 3) * branch_factor * 0.1
y = y + sin(angle * 3) * branch_factor * 0.1

// Audio-reactive fractal expansion
audio_expand = getspec(0,0.2,0) * 2
x = x * (1 + audio_expand)
y = y * (1 + audio_expand)

// Color based on fractal depth
depth_ratio = fractal_level / fractal_depth
red = 0.2 + depth_ratio * 0.6
green = 0.4 + depth_ratio * 0.4
blue = 0.3 + depth_ratio * 0.5

// Energy modulation
energy = getspec(0,0.1,0) * 2
red = red * (0.5 + energy)
green = green * (0.5 + energy)
blue = blue * (0.5 + energy)

FRAME
t=t+dt

[preset03]
n=180.000000
b=1.000000
sn=Superscope (Platonic Solids)
POINT
t=1.000000
dt=0.008000
RED=0.600000
GREEN=0.400000
BLUE=0.800000
INIT
n=1
t=0
solid_type=4  // 0=Tetra, 1=Octa, 2=Cube, 3=Icosa, 4=Dodeca

CODE
// Platonic solids visualization
// Five perfect polyhedra with mathematical precision

vertex_index = n % (solid_type == 0 ? 4 : solid_type == 1 ? 6 : solid_type == 2 ? 8 : solid_type == 3 ? 12 : 20)
rotation_angle = t * 0.5

// Tetrahedron vertices
if (equal(solid_type,0),
  if (equal(vertex_index,0), vx=1, vy=1, vz=1,
    if (equal(vertex_index,1), vx=1, vy=-1, vz=-1,
      if (equal(vertex_index,2), vx=-1, vy=1, vz=-1,
        if (equal(vertex_index,3), vx=-1, vy=-1, vz=1,
          vx=0, vy=0, vz=0
        )
      )
    )
  )
)

// Cube vertices
if (equal(solid_type,1),
  if (equal(vertex_index,0), vx=-1, vy=-1, vz=-1,
    if (equal(vertex_index,1), vx=1, vy=-1, vz=-1,
      if (equal(vertex_index,2), vx=1, vy=1, vz=-1,
        if (equal(vertex_index,3), vx=-1, vy=1, vz=-1,
          if (equal(vertex_index,4), vx=-1, vy=-1, vz=1,
            if (equal(vertex_index,5), vx=1, vy=-1, vz=1,
              if (equal(vertex_index,6), vx=1, vy=1, vz=1,
                if (equal(vertex_index,7), vx=-1, vy=1, vz=1,
                  vx=0, vy=0, vz=0
                )
              )
            )
          )
        )
      )
    )
  )
)

// Octahedron vertices
if (equal(solid_type,2),
  if (equal(vertex_index,0), vx=0, vy=0, vz=1.414,
    if (equal(vertex_index,1), vx=1.414, vy=0, vz=0,
      if (equal(vertex_index,2), vx=0, vy=1.414, vz=0,
        if (equal(vertex_index,3), vx=-1.414, vy=0, vz=0,
          if (equal(vertex_index,4), vx=0, vy=-1.414, vz=0,
            if (equal(vertex_index,5), vx=0, vy=0, vz=-1.414,
              vx=0, vy=0, vz=0
            )
          )
        )
      )
    )
  )
)

// Icosahedron vertices (approximated)
if (equal(solid_type,3),
  golden_ratio = 1.618
  if (equal(vertex_index,0), vx=0, vy=1, vz=golden_ratio,
    if (equal(vertex_index,1), vx=0, vy=1, vz=-golden_ratio,
      if (equal(vertex_index,2), vx=0, vy=-1, vz=golden_ratio,
        if (equal(vertex_index,3), vx=0, vy=-1, vz=-golden_ratio,
          if (equal(vertex_index,4), vx=1, vy=golden_ratio, vz=0,
            if (equal(vertex_index,5), vx=1, vy=-golden_ratio, vz=0,
              if (equal(vertex_index,6), vx=-1, vy=golden_ratio, vz=0,
                if (equal(vertex_index,7), vx=-1, vy=-golden_ratio, vz=0,
                  if (equal(vertex_index,8), vx=golden_ratio, vy=0, vz=1,
                    if (equal(vertex_index,9), vx=golden_ratio, vy=0, vz=-1,
                      if (equal(vertex_index,10), vx=-golden_ratio, vy=0, vz=1,
                        if (equal(vertex_index,11), vx=-golden_ratio, vy=0, vz=-1,
                          vx=0, vy=0, vz=0
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

// Dodecahedron vertices (simplified)
if (equal(solid_type,4),
  phi = 1.618
  if (equal(vertex_index,0), vx=0, vy=1/phi, vz=phi,
    if (equal(vertex_index,1), vx=0, vy=1/phi, vz=-phi,
      if (equal(vertex_index,2), vx=0, vy=-1/phi, vz=phi,
        if (equal(vertex_index,3), vx=0, vy=-1/phi, vz=-phi,
          if (equal(vertex_index,4), vx=1/phi, vy=phi, vz=0,
            if (equal(vertex_index,5), vx=1/phi, vy=-phi, vz=0,
              if (equal(vertex_index,6), vx=-1/phi, vy=phi, vz=0,
                if (equal(vertex_index,7), vx=-1/phi, vy=-phi, vz=0,
                  if (equal(vertex_index,8), vx=phi, vy=0, vz=1/phi,
                    if (equal(vertex_index,9), vx=phi, vy=0, vz=-1/phi,
                      if (equal(vertex_index,10), vx=-phi, vy=0, vz=1/phi,
                        if (equal(vertex_index,11), vx=-phi, vy=0, vz=-1/phi,
                          vx=0, vy=0, vz=0
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

// 3D rotation
cos_rot = cos(rotation_angle)
sin_rot = sin(rotation_angle)
x_temp = vx * cos_rot - vz * sin_rot
z_temp = vx * sin_rot + vz * cos_rot
vx = x_temp
vz = z_temp

// Perspective projection
distance = 3
x = vx * distance / (distance + vz)
y = vy * distance / (distance + vz)

// Audio-reactive scaling
audio_scale = 1 + getspec(0,0.1,0) * 1.5
x = x * audio_scale
y = y * audio_scale

// Color based on vertex position
red = 0.5 + vx * 0.3
green = 0.4 + vy * 0.3
blue = 0.6 + vz * 0.2

// Energy modulation
energy = getspec(0,0.1,0) * 2
red = red * (0.3 + energy)
green = green * (0.3 + energy)
blue = blue * (0.3 + energy)

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
