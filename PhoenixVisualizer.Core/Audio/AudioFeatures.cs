// Global using redirects to fix namespace conflicts
// This allows existing code to continue working without modification

global using AudioFeatures = PhoenixVisualizer.Core.Models.AudioFeatures;

namespace PhoenixVisualizer.Core.Audio
{
    // This namespace now redirects to the correct AudioFeatures
    // Existing code can continue using PhoenixVisualizer.Core.Audio.AudioFeatures
}
