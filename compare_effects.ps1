# Get documentation files (MD)
$docFiles = Get-ChildItem docs/Docs/Effects/*.md |
    Where-Object { $_.Name -notlike "*EFFECT_NAMING*" -and $_.Name -notlike "*EffectsImplementationStatus*" -and $_.Name -notlike "*EffectsIndex*" -and $_.Name -notlike "*MISSING_EFFECTS*" -and $_.Name -notlike "*WHERE_WE_ARE*" -and $_.Name -notlike "*AVS_COMPATIBILITY*" } |
    Select-Object -ExpandProperty Name |
    ForEach-Object { $_.Replace('.md', '') }

# Get implementation files (CS)
$implFiles = Get-ChildItem PhoenixVisualizer.Core/Effects/Nodes/AvsEffects/*.cs |
    Where-Object { $_.Name -notlike "*backup*" -and $_.Name -notlike "*EffectNodeFixer*" -and $_.Name -notlike "*fix.py*" -and $_.Name -notlike "*test_cache.py*" } |
    Select-Object -ExpandProperty Name |
    ForEach-Object {
        $name = $_.Replace('EffectsNode.cs', '').Replace('Effects.cs', '').Replace('Node.cs', '').Replace('.cs', '')
        # Normalize naming for comparison
        $name = $name.Replace('ColorReduction', 'ColorreductionEffects')
        $name = $name.Replace('Colorreplace', 'ColorreplaceEffects')
        $name = $name.Replace('ColorFade', 'ColorfadeEffects')
        $name = $name.Replace('ContrastEnhancement', 'ContrastEnhancementEffects')
        $name = $name.Replace('CustomBPM', 'CustomBPMEffects')
        $name = $name.Replace('DynamicColorModulation', 'DcolormodEffects')
        $name = $name.Replace('DynamicDistanceModifier', 'DynamicDistanceModifierEffects')
        $name = $name.Replace('DynamicMovement', 'DynamicMovementEffects')
        $name = $name.Replace('DynamicShift', 'DynamicShiftEffects')
        $name = $name.Replace('EffectStacking', 'EffectStacking')
        $name = $name.Replace('Fastbright', 'FastbrightEffects')
        $name = $name.Replace('FastBrightness', 'FastBrightnessEffects')
        $name = $name.Replace('Interleave', 'InterleavingEffects')
        $name = $name.Replace('Onetone', 'OnetoneEffects')
        $name = $name.Replace('OscilloscopeRing', 'OscilloscopeRing')
        $name = $name.Replace('OscilloscopeStar', 'OscilloscopeStar')
        $name = $name.Replace('ParticleSwarm', 'ParticleSystems')
        $name = $name.Replace('ParticleSystems', 'ParticleSystems')
        $name = $name.Replace('RotatingStarPatterns', 'RotatingStarPatterns')
        $name = $name.Replace('RotBlit', 'RotatedBlitting')
        $name = $name.Replace('Simple', 'SimpleEffects')
        $name = $name.Replace('SpectrumVisualization', 'SpectrumVisualization')
        $name = $name.Replace('Stack', 'StackEffects')
        $name = $name.Replace('Starfield', 'StarfieldEffects')
        $name = $name.Replace('Superscope', 'Superscope')
        $name = $name.Replace('Transition', 'Transitions')
        $name = $name.Replace('VideoDelay', 'VideoDelayEffects')
        $name = $name.Replace('WaterBump', 'WaterBumpEffects')
        $name
    }

Write-Host "=== PHOENIX VISUALIZER EFFECTS COMPARISON ===" -ForegroundColor Cyan
Write-Host ""

# Compare and report
$implemented = 0
$documented = 0
$matches = 0

foreach ($doc in $docFiles) {
    $documented++
    $found = $false
    foreach ($impl in $implFiles) {
        if ($impl -eq $doc) {
            $found = $true
            $matches++
            break
        }
    }

    if ($found) {
        Write-Host "✅ $doc.cs = $doc.md" -ForegroundColor Green
    } else {
        Write-Host "❌ MISSING: $doc.cs (documented but not implemented)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=== SUMMARY ===" -ForegroundColor Yellow
Write-Host "📄 Total documented effects: $documented" -ForegroundColor White
Write-Host "🔧 Total implemented effects: $($implFiles.Count)" -ForegroundColor White
Write-Host "✅ Matching effects: $matches" -ForegroundColor Green
Write-Host "❌ Missing implementations: $($documented - $matches)" -ForegroundColor Red

# Show extra implementations
$extraImpls = @()
foreach ($impl in $implFiles) {
    $found = $false
    foreach ($doc in $docFiles) {
        if ($impl -eq $doc) {
            $found = $true
            break
        }
    }
    if (-not $found) {
        $extraImpls += $impl
    }
}

if ($extraImpls.Count -gt 0) {
    Write-Host ""
    Write-Host "🔍 Extra implementations (not in docs):" -ForegroundColor Magenta
    foreach ($extra in $extraImpls) {
        Write-Host "   $extra.cs" -ForegroundColor Magenta
    }
}
