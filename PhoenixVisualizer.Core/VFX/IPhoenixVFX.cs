namespace PhoenixVisualizer.Core.VFX;

public interface IPhoenixVFX : IDisposable
{
    void Initialize();
    void Render(VFXRenderContext context);
}
