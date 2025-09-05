#!/usr/bin/env python3
"""
Fix Node files - add missing closing braces for RenderContext.
Uses explicit string operations, NO REGEX.
"""

import os
import glob

def fix_node_file(filepath):
    """Fix a single Node file by adding missing closing braces."""
    print(f"Fixing {filepath}")
    
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Fix missing closing brace for RenderContext
    content = content.replace('Canvas = c);', 'Canvas = c });')
    content = content.replace('Canvas =  canvas);', 'Canvas = canvas });')
    
    with open(filepath, 'w', encoding='utf-8') as f:
        f.write(content)

def main():
    """Fix all Node files by adding missing closing braces."""
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
