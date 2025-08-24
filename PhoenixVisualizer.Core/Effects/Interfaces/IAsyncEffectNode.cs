using System.Threading.Tasks;
using System.Collections.Generic;
using PhoenixVisualizer.Core.Models;

namespace PhoenixVisualizer.Core.Effects.Interfaces
{
    /// <summary>
    /// Interface for effect nodes that support asynchronous processing
    /// </summary>
    public interface IAsyncEffectNode : IEffectNode
    {
        /// <summary>
        /// Process the effect asynchronously
        /// </summary>
        /// <param name="inputs">Input data dictionary</param>
        /// <param name="audioFeatures">Audio features for beat-reactive effects</param>
        /// <returns>Task containing the processed output</returns>
        Task<object> ProcessAsync(Dictionary<string, object> inputs, AudioFeatures audioFeatures);
    }
}
