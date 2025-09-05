#!/usr/bin/env python3
"""
Fix all Node files to use SkiaCanvasAdapter for ISkiaCanvas conversion
"""

import os
import re

def fix_node_file(file_path):
    """Fix a single Node file to use SkiaCanvasAdapter"""
    print(f"Fixing {file_path}")
    
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check if this file already has the adapter pattern
    if 'SkiaCanvasAdapter' in content:
        print(f"{file_path} already uses SkiaCanvasAdapter")
        return
    
    # Find the RenderContext creation pattern
    patterns = [
        (r'(Canvas = canvas)', r'Canvas = new SkiaCanvasAdapter(canvas)'),
        (r'(Canvas = c)', r'Canvas = new SkiaCanvasAdapter(c)'),
        (r'(Canvas=canvas)', r'Canvas=new SkiaCanvasAdapter(canvas)')
    ]
    
    fixed = False
    for pattern, replacement in patterns:
        if re.search(pattern, content):
            # Replace with adapter
            content = re.sub(pattern, replacement, content)
            fixed = True
            break
    
    if fixed:
        # Write the updated content
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        
        print(f"Fixed {file_path}")
    else:
        print(f"No Canvas pattern found in {file_path}")

def main():
    """Main function to fix all Node files"""
    
    # List of Node files to fix
    node_files = [
        "PhoenixVisualizer.Visuals/NodeAudioFlowField.cs",
        "PhoenixVisualizer.Visuals/NodeBassBloom.cs",
        "PhoenixVisualizer.Visuals/NodeBassKicker.cs",
        "PhoenixVisualizer.Visuals/NodeBassParticles.cs",
        "PhoenixVisualizer.Visuals/NodeBeatKaleidoTunnel.cs",
        "PhoenixVisualizer.Visuals/NodeBeatRings.cs",
        "PhoenixVisualizer.Visuals/NodeButterflyField.cs",
        "PhoenixVisualizer.Visuals/NodeGeoLattice.cs",
        "PhoenixVisualizer.Visuals/NodeHexGridPulse.cs",
        "PhoenixVisualizer.Visuals/NodeKaleidoBeats.cs",
        "PhoenixVisualizer.Visuals/NodeParticlesBeat.cs",
        "PhoenixVisualizer.Visuals/NodePixelSortPlasma.cs",
        "PhoenixVisualizer.Visuals/NodePlasmaWarp.cs",
        "PhoenixVisualizer.Visuals/NodePulseTunnel.cs",
        "PhoenixVisualizer.Visuals/NodeRainbowSpectrum.cs",
        "PhoenixVisualizer.Visuals/NodeScopeKaleidoGlow.cs",
        "PhoenixVisualizer.Visuals/NodeScopeRibbon.cs",
        "PhoenixVisualizer.Visuals/NodeSpectrumNebula.cs",
        "PhoenixVisualizer.Visuals/NodeTextBeatEcho.cs",
        "PhoenixVisualizer.Visuals/NodeTextEcho.cs",
        "PhoenixVisualizer.Visuals/NodeTriangulateScope.cs",
        "PhoenixVisualizer.Visuals/NodeVectorFieldScope.cs",
        "PhoenixVisualizer.Visuals/NodeVectorGrid.cs",
        "PhoenixVisualizer.Visuals/NodeWaveStarfield.cs",
        "PhoenixVisualizer.Visuals/NodeXsFireworks.cs",
        "PhoenixVisualizer.Visuals/NodeXsLcdScrub.cs",
        "PhoenixVisualizer.Visuals/NodeXsLisa.cs",
        "PhoenixVisualizer.Visuals/NodeXsLightning.cs",
        "PhoenixVisualizer.Visuals/NodeXsPenrose.cs",
        "PhoenixVisualizer.Visuals/NodeXsPlasma.cs",
        "PhoenixVisualizer.Visuals/NodeXsRotor.cs",
        "PhoenixVisualizer.Visuals/NodeXsRorschach.cs",
        "PhoenixVisualizer.Visuals/NodeXsVortex.cs"
    ]
    
    for file_path in node_files:
        if os.path.exists(file_path):
            fix_node_file(file_path)
        else:
            print(f"File not found: {file_path}")
    
    print("All Node files have been fixed!")

if __name__ == "__main__":
    main()
