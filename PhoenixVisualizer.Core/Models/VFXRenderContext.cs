using System;
using System.Drawing;
using System.Numerics;

namespace PhoenixVisualizer.Core.Models
{
    /// <summary>
    /// Provides rendering context for VFX effects
    /// </summary>
    public class VFXRenderContext
    {
        /// <summary>
        /// Width of the rendering surface
        /// </summary>
        public int Width { get; set; }
        
        /// <summary>
        /// Height of the rendering surface
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Current frame number
        /// </summary>
        public long FrameNumber { get; set; }
        
        /// <summary>
        /// Time since start in seconds
        /// </summary>
        public float Time { get; set; }
        
        /// <summary>
        /// Delta time since last frame in seconds
        /// </summary>
        public float DeltaTime { get; set; }
        
        /// <summary>
        /// Current FPS
        /// </summary>
        public float FPS { get; set; }
        
        /// <summary>
        /// Whether we're currently rendering
        /// </summary>
        public bool IsRendering { get; set; }
        
        /// <summary>
        /// Current camera position (for 3D effects)
        /// </summary>
        public Vector3 CameraPosition { get; set; }
        
        /// <summary>
        /// Current camera target (for 3D effects)
        /// </summary>
        public Vector3 CameraTarget { get; set; }
        
        /// <summary>
        /// Current camera up vector (for 3D effects)
        /// </summary>
        public Vector3 CameraUp { get; set; }
        
        /// <summary>
        /// Field of view in radians (for 3D effects)
        /// </summary>
        public float FieldOfView { get; set; } = (float)(Math.PI / 4.0); // 45 degrees
        
        /// <summary>
        /// Near clipping plane (for 3D effects)
        /// </summary>
        public float NearClip { get; set; } = 0.1f;
        
        /// <summary>
        /// Far clipping plane (for 3D effects)
        /// </summary>
        public float FarClip { get; set; } = 1000.0f;
        
        /// <summary>
        /// Current view matrix (for 3D effects)
        /// </summary>
        public Matrix4x4 ViewMatrix { get; set; }
        
        /// <summary>
        /// Current projection matrix (for 3D effects)
        /// </summary>
        public Matrix4x4 ProjectionMatrix { get; set; }
        
        /// <summary>
        /// Current world matrix (for 3D effects)
        /// </summary>
        public Matrix4x4 WorldMatrix { get; set; }
        
        /// <summary>
        /// Background color for the scene
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Black;
        
        /// <summary>
        /// Whether to clear the background each frame
        /// </summary>
        public bool ClearBackground { get; set; } = true;
        
        /// <summary>
        /// Current blend mode for rendering
        /// </summary>
        public BlendMode BlendMode { get; set; } = BlendMode.Normal;
        
        /// <summary>
        /// Current cull mode for 3D rendering
        /// </summary>
        public CullMode CullMode { get; set; } = CullMode.Back;
        
        /// <summary>
        /// Whether depth testing is enabled
        /// </summary>
        public bool DepthTestEnabled { get; set; } = true;
        
        /// <summary>
        /// Whether depth writing is enabled
        /// </summary>
        public bool DepthWriteEnabled { get; set; } = true;
        
        /// <summary>
        /// Current line width for line rendering
        /// </summary>
        public float LineWidth { get; set; } = 1.0f;
        
        /// <summary>
        /// Current point size for point rendering
        /// </summary>
        public float PointSize { get; set; } = 1.0f;
        
        /// <summary>
        /// Update the camera matrices based on current camera properties
        /// </summary>
        public void UpdateCameraMatrices()
        {
            ViewMatrix = Matrix4x4.CreateLookAt(CameraPosition, CameraTarget, CameraUp);
            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, (float)Width / Height, NearClip, FarClip);
        }
        
        /// <summary>
        /// Get the aspect ratio of the rendering surface
        /// </summary>
        public float AspectRatio => (float)Width / Height;
        
        /// <summary>
        /// Get the center point of the rendering surface
        /// </summary>
        public Vector2 Center => new Vector2(Width * 0.5f, Height * 0.5f);
        
        /// <summary>
        /// Get the size of the rendering surface as a vector
        /// </summary>
        public Vector2 Size => new Vector2(Width, Height);
    }
    
    /// <summary>
    /// Blend modes for rendering
    /// </summary>
    public enum BlendMode
    {
        Normal,
        Additive,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        ColorDodge,
        ColorBurn,
        HardLight,
        SoftLight,
        Difference,
        Exclusion
    }
    
    /// <summary>
    /// Cull modes for 3D rendering
    /// </summary>
    public enum CullMode
    {
        None,
        Front,
        Back
    }
}