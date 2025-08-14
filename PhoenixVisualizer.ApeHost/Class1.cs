using PhoenixVisualizer.PluginHost;

namespace PhoenixVisualizer.ApeHost;

public interface IApeHost
{
	void Register(IApeEffect effect);
}

public sealed class ApeHost : IApeHost
{
	public void Register(IApeEffect effect) { /* registry to be implemented */ }
}
