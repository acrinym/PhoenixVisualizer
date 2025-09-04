using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using NullWizard.Core.Models;
using System.Text.RegularExpressions;

namespace NullWizard.Core.Services;

/// <summary>
/// Implementation of codebase analyzer that learns null handling patterns
/// </summary>
public class CodebaseAnalyzer : ICodebaseAnalyzer
{
    public async Task<CodebaseAnalysis> AnalyzeCodebaseAsync(string projectPath)
    {
        var analysis = new CodebaseAnalysis
        {
            ProjectPath = projectPath,
            AnalyzedAt = DateTime.UtcNow
        };

        // Load the project
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        analysis.TargetFramework = project.TargetFramework ?? "unknown";

        // Analyze the project
        var compilation = await project.GetCompilationAsync();
        if (compilation == null) return analysis;

        // Discover patterns
        analysis.Patterns = await DiscoverPatternsAsync(projectPath);
        
        // Count warnings
        analysis.NullWarningCounts = await CountNullWarningsAsync(projectPath);

        return analysis;
    }

    public async Task<List<NullPattern>> DiscoverPatternsAsync(string projectPath)
    {
        var patterns = new List<NullPattern>();
        
        // Load project and get all syntax trees
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(projectPath);
        var compilation = await project.GetCompilationAsync();
        
        if (compilation == null) return patterns;

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var root = await syntaxTree.GetRootAsync();
            var patternsInFile = AnalyzeSyntaxTree(root);
            patterns.AddRange(patternsInFile);
        }

        // Group and aggregate patterns
        return AggregatePatterns(patterns);
    }

    public async Task<Dictionary<string, int>> CountNullWarningsAsync(string projectPath)
    {
        var warnings = new Dictionary<string, int>();
        
        // This would integrate with the actual build process
        // For now, we'll return a placeholder
        warnings["CS8601"] = 0; // Possible null reference assignment
        warnings["CS8602"] = 0; // Dereference of a possibly null reference
        warnings["CS8603"] = 0; // Possible null reference return
        warnings["CS8604"] = 0; // Possible null reference argument
        
        return warnings;
    }

    private List<NullPattern> AnalyzeSyntaxTree(SyntaxNode root)
    {
        var patterns = new List<NullPattern>();

        // Find null-coalescing patterns (??)
        var nullCoalescingNodes = root.DescendantNodes()
            .OfType<BinaryExpressionSyntax>()
            .Where(n => n.OperatorToken.Kind() == SyntaxKind.QuestionQuestionToken);

        foreach (var node in nullCoalescingNodes)
        {
            patterns.Add(new NullPattern
            {
                PatternId = "null_coalescing",
                Description = "Null-coalescing operator usage",
                Context = DetermineContext(node),
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = node.ToString(),
                FixPattern = node.ToString(),
                OccurrenceCount = 1,
                Confidence = 1.0,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            });
        }

        // Find null-conditional patterns (?.)
        var nullConditionalNodes = root.DescendantNodes()
            .OfType<ConditionalAccessExpressionSyntax>();

        foreach (var node in nullConditionalNodes)
        {
            patterns.Add(new NullPattern
            {
                PatternId = "null_conditional",
                Description = "Null-conditional operator usage",
                Context = DetermineContext(node),
                Strategy = NullStrategy.NullConditional,
                CodePattern = node.ToString(),
                FixPattern = node.ToString(),
                OccurrenceCount = 1,
                Confidence = 1.0,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            });
        }

        // Find null-forgiving patterns (!)
        var nullForgivingNodes = root.DescendantNodes()
            .OfType<PostfixUnaryExpressionSyntax>()
            .Where(n => n.OperatorToken.Kind() == SyntaxKind.ExclamationToken);

        foreach (var node in nullForgivingNodes)
        {
            patterns.Add(new NullPattern
            {
                PatternId = "null_forgiving",
                Description = "Null-forgiving operator usage",
                Context = DetermineContext(node),
                Strategy = NullStrategy.NullForgiving,
                CodePattern = node.ToString(),
                FixPattern = node.ToString(),
                OccurrenceCount = 1,
                Confidence = 1.0,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            });
        }

        return patterns;
    }

    private NullContext DetermineContext(SyntaxNode node)
    {
        // Analyze the surrounding context to determine what type of null handling this is
        var parent = node.Parent;
        
        // Check if we're in a constructor
        if (parent?.Ancestors().OfType<ConstructorDeclarationSyntax>().Any() == true)
            return NullContext.ConstructorParameter;
            
        // Check if we're in a method parameter
        if (parent?.Ancestors().OfType<ParameterSyntax>().Any() == true)
            return NullContext.MethodParameter;
            
        // Check if we're in a property
        if (parent?.Ancestors().OfType<PropertyDeclarationSyntax>().Any() == true)
            return NullContext.PropertySetter;
            
        // Check if we're in a return statement
        if (parent?.Ancestors().OfType<ReturnStatementSyntax>().Any() == true)
            return NullContext.ReturnValue;
            
        // Check for JSON deserialization patterns
        if (ContainsJsonPattern(node))
            return NullContext.JsonDeserialization;
            
        // Check for database patterns
        if (ContainsDatabasePattern(node))
            return NullContext.DatabaseResult;
            
        // Check for API patterns
        if (ContainsApiPattern(node))
            return NullContext.ApiResponse;
            
        // Default to user input
        return NullContext.UserInput;
    }

    private bool ContainsJsonPattern(SyntaxNode node)
    {
        var code = node.ToString();
        return Regex.IsMatch(code, @"JsonConvert\.|JsonSerializer\.|\[JsonProperty\]|\[JsonIgnore\]", RegexOptions.IgnoreCase);
    }

    private bool ContainsDatabasePattern(SyntaxNode node)
    {
        var code = node.ToString();
        return Regex.IsMatch(code, @"SqlCommand|SqlDataReader|DbCommand|DbDataReader|ExecuteReader|ExecuteScalar", RegexOptions.IgnoreCase);
    }

    private bool ContainsApiPattern(SyntaxNode node)
    {
        var code = node.ToString();
        return Regex.IsMatch(code, @"HttpClient|WebRequest|REST|API|Endpoint", RegexOptions.IgnoreCase);
    }

    private List<NullPattern> AggregatePatterns(List<NullPattern> patterns)
    {
        var aggregated = new List<NullPattern>();
        var groups = patterns.GroupBy(p => new { p.PatternId, p.Context, p.Strategy });

        foreach (var group in groups)
        {
            var first = group.First();
            aggregated.Add(new NullPattern
            {
                PatternId = first.PatternId,
                Description = first.Description,
                Context = first.Context,
                Strategy = first.Strategy,
                CodePattern = first.CodePattern,
                FixPattern = first.FixPattern,
                OccurrenceCount = group.Count(),
                Confidence = Math.Min(1.0, group.Count() / 10.0), // Higher confidence with more occurrences
                ApplicableFrameworks = first.ApplicableFrameworks
            });
        }

        return aggregated;
    }
}
