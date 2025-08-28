#!/usr/bin/env python3
"""
Fix Effect Nodes - Replace old interface methods with new BaseEffectNode interface
NEW VERSION: Properly handles regions and maintains file structure
"""

import os
import shutil
import re

def fix_effect_file_improved(file_path):
    """Fix a single effect file by replacing old methods and cleaning up regions"""
    print(f"Fixing: {file_path}")
    
    # Read the file
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    original_content = content
    changes_made = False
    
    # Step 1: Replace ProcessFrame with ProcessCore (generic pattern)
    process_frame_pattern = r'public override void ProcessFrame\([^)]*\)\s*\{[^}]*\}'
    if re.search(process_frame_pattern, content, re.DOTALL):
        # Find the ProcessFrame method and replace with ProcessCore
        content = re.sub(process_frame_pattern, 
                        '''        protected override object ProcessCore(Dictionary<string, object> inputs, AudioFeatures audioFeatures)
        {
            if (!inputs.TryGetValue("Image", out var input) || input is not ImageBuffer imageBuffer)
                return GetDefaultOutput();

            var output = new ImageBuffer(imageBuffer.Width, imageBuffer.Height);
            
            // TODO: Implement actual effect logic here
            // For now, just copy input to output
            for (int i = 0; i < output.Pixels.Length; i++)
            {
                output.Pixels[i] = imageBuffer.Pixels[i];
            }
            
            return output;
        }

        public override object GetDefaultOutput()
        {
            return new ImageBuffer(800, 600);
        }''', content, flags=re.DOTALL)
        print("  ✓ Replaced ProcessFrame with ProcessCore")
        changes_made = True
    
    # Step 2: Remove GetConfiguration method (generic pattern)
    get_config_pattern = r'public override Dictionary<string, object> GetConfiguration\(\)\s*\{[^}]*\}'
    if re.search(get_config_pattern, content, re.DOTALL):
        content = re.sub(get_config_pattern, '', content, flags=re.DOTALL)
        print("  ✓ Removed GetConfiguration method")
        changes_made = True
    
    # Step 3: Remove ApplyConfiguration method (generic pattern)
    apply_config_pattern = r'public override void ApplyConfiguration\(Dictionary<string, object> config\)\s*\{[^}]*\}'
    if re.search(apply_config_pattern, content, re.DOTALL):
        content = re.sub(apply_config_pattern, '', content, flags=re.DOTALL)
        print("  ✓ Removed ApplyConfiguration method")
        changes_made = True
    
    # Step 4: Clean up incomplete regions
    # Find and fix incomplete #region Configuration sections
    incomplete_region_pattern = r'#region Configuration\s*\n\s*\n\s*\n\s*#endregion'
    if re.search(incomplete_region_pattern, content):
        content = re.sub(incomplete_region_pattern, '', content)
        print("  ✓ Cleaned up incomplete Configuration region")
        changes_made = True
    
    # Also fix any trailing incomplete regions
    trailing_region_pattern = r'#region Configuration\s*\n\s*\n\s*\n\s*$'
    if re.search(trailing_region_pattern, content):
        content = re.sub(trailing_region_pattern, '', content)
        print("  ✓ Cleaned up trailing incomplete Configuration region")
        changes_made = True
    
    # Step 5: Ensure proper file ending
    # Remove any trailing whitespace and ensure proper closing
    content = content.rstrip() + '\n'
    
    # Check if content changed
    if content != original_content:
        # Backup original file
        backup_path = file_path + ".backup"
        shutil.copy2(file_path, backup_path)
        print(f"  ✓ Created backup: {backup_path}")
        
        # Write fixed content
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  ✓ Fixed file: {file_path}")
        
        # Verify file structure
        verify_file_structure(file_path)
        
        return True
    else:
        print("  ⚠ No changes made")
        return False

def verify_file_structure(file_path):
    """Verify that the file has proper structure after fixing"""
    with open(file_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Check for basic structure
    issues = []
    
    # Check for unmatched regions
    region_count = content.count('#region')
    endregion_count = content.count('#endregion')
    if region_count != endregion_count:
        issues.append(f"Unmatched regions: {region_count} regions, {endregion_count} endregions")
    
    # Check for incomplete regions
    if '#region Configuration\n\n\n\n#endregion' in content:
        issues.append("Found incomplete Configuration region")
    
    # Check for proper class ending
    if not content.strip().endswith('}'):
        issues.append("File doesn't end with proper closing brace")
    
    if issues:
        print(f"  ⚠ Structure issues found: {', '.join(issues)}")
    else:
        print("  ✓ File structure verified as correct")

def main():
    """Main function - test with two files to verify batch processing"""
    print("=== NEW Effect Node Fixer ===")
    print("This version properly handles regions and maintains file structure")
    
    # Test with two files to verify batch processing
    test_files = [
        "PhoenixVisualizer.Core/Effects/Nodes/AvsEffects/SVPEffectsNode.cs",
        "PhoenixVisualizer.Core/Effects/Nodes/AvsEffects/SimpleEffectsNode.cs"
    ]
    
    print(f"\nTesting with 2 files to verify batch processing:")
    for i, test_file in enumerate(test_files, 1):
        print(f"\n[{i}/2] Processing: {test_file}")
        
        if os.path.exists(test_file):
            success = fix_effect_file_improved(test_file)
            
            if success:
                print(f"  ✅ SUCCESS: Fixed {test_file}")
            else:
                print(f"  ⚠ No changes needed for {test_file}")
        else:
            print(f"  ❌ File not found: {test_file}")
    
    print(f"\n✅ BATCH TEST COMPLETE: Processed {len(test_files)} files")
    print("Ready to scale up to larger batches!")

if __name__ == "__main__":
    main()
