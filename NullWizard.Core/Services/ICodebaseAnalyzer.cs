using NullWizard.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NullWizard.Core.Services;

/// <summary>
/// Service for analyzing codebases to learn null handling patterns
/// </summary>
public interface ICodebaseAnalyzer
{
    /// <summary>
    /// Analyzes a codebase to learn null handling patterns
    /// </summary>
    /// <param name="projectPath">Path to the project file</param>
    /// <returns>Analysis result with learned patterns</returns>
    Task<CodebaseAnalysis> AnalyzeCodebaseAsync(string projectPath);
    
    /// <summary>
    /// Scans for existing null handling patterns in the codebase
    /// </summary>
    /// <param name="projectPath">Path to the project file</param>
    /// <returns>List of discovered patterns</returns>
    Task<List<NullPattern>> DiscoverPatternsAsync(string projectPath);
    
    /// <summary>
    /// Counts null-related warnings in the project
    /// </summary>
    /// <param name="projectPath">Path to the project file</param>
    /// <returns>Dictionary of warning types and counts</returns>
    Task<Dictionary<string, int>> CountNullWarningsAsync(string projectPath);
}
