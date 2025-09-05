#!/usr/bin/env python3
"""
Simple fix for Node files - just change method names.
Uses explicit string operations, NO REGEX.
"""

import os
import glob

def fix_node_file(filepath):
    """Fix a single Node file with simple method name changes."""
    print(f"Fixing {filepath}")
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix EffectRegistry.Create() calls
    content = content.replace('EffectRegistry.Create(', 'EffectRegistry.CreateByName(')
    
    # Fix IEffectNode.Process() calls - just change to Render with simple parameters
    content = content.replace('.Process(f,', '.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = ')
    content = content.replace('.Process(f, c)', '.Render(f.Spectrum, f.Spectrum, new RenderContext { Width = _width, Height = _height, Waveform = f.Spectrum, Spectrum = f.Spectrum, Time = f.Time, Beat = f.Beat, Volume = f.Volume, Canvas = c })')
    
    # Add missing closing brace for RenderContext
    content = content.replace('Canvas =  canvas);', 'Canvas = canvas });')
    content = content.replace('Canvas = c })', 'Canvas = c });')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

def main():
    """Fix all Node files with simple method name changes."""
    visuals_dir = 'PhoenixVisualizer.Visuals'
    
    # Find all Node*.cs files
    node_files = glob.glob(os.path.join(visuals_dir, 'Node*.cs'))
    
    print(f"Found {len(node_files)} Node files to fix:")
    for filepath in node_files:
        print(f"  {filepath}")
    
    # Fix each file
    for filepath in node_files:
        try:
            fix_node_file(filepath)
            print(f"✓ Fixed {filepath}")
        except Exception as e:
            print(f"✗ Error fixing {filepath}: {e}")
    
    print(f"\nFixed {len(node_files)} Node files!")

if __name__ == '__main__':
    main()


