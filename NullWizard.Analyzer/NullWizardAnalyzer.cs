using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using NullWizard.Core.Models;
using NullWizard.Core.Services;
using NullWizard.Rules;
using System.Collections.Immutable;

namespace NullWizard.Analyzer;

/// <summary>
/// Roslyn analyzer for null handling suggestions
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullWizardAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "NW001";
    
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.NullWizardAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.NullWizardAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
    private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.NullWizardAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
    
    private const string Category = "Null Handling";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.VariableDeclaration, SyntaxKind.AssignmentExpression);
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Detect popular packages and their specific null handling patterns
        var packages = DetectPackages(context.Compilation);
        
        // Store package information for use in analysis
        context.RegisterSyntaxNodeAction(nodeContext => AnalyzeNodeWithPackages(nodeContext, packages), 
            SyntaxKind.VariableDeclaration, SyntaxKind.AssignmentExpression);
    }

    private List<string> DetectPackages(Compilation compilation)
    {
        var packages = new List<string>();
        
        // Check for popular packages by looking at referenced assemblies
        foreach (var reference in compilation.References)
        {
            var assemblyName = reference.Display;
            
            if (assemblyName.Contains("Avalonia"))
                packages.Add("avalonia");
            else if (assemblyName.Contains("Newtonsoft.Json") || assemblyName.Contains("Json.Net"))
                packages.Add("newtonsoft.json");
            else if (assemblyName.Contains("System.Data.SqlClient"))
                packages.Add("system.data.sqlclient");
            else if (assemblyName.Contains("System.Net.Http"))
                packages.Add("system.net.http");
            else if (assemblyName.Contains("RestSharp"))
                packages.Add("restsharp");
            else if (assemblyName.Contains("NLog"))
                packages.Add("nlog");
            else if (assemblyName.Contains("log4net"))
                packages.Add("log4net");
            else if (assemblyName.Contains("Autofac"))
                packages.Add("autofac");
            else if (assemblyName.Contains("Castle.Windsor"))
                packages.Add("castle.windsor");
            else if (assemblyName.Contains("NHibernate"))
                packages.Add("nhibernate");
            else if (assemblyName.Contains("EntityFramework"))
                packages.Add("entityframework");
            else if (assemblyName.Contains("System.Web"))
                packages.Add("system.web");
            else if (assemblyName.Contains("System.Windows.Forms"))
                packages.Add("system.windows.forms");
            else if (assemblyName.Contains("System.Windows.Presentation"))
                packages.Add("system.windows.presentation");
            else if (assemblyName.Contains("Xamarin.Forms"))
                packages.Add("xamarin.forms");
            else if (assemblyName.Contains("Mono.Android"))
                packages.Add("mono.android");
            else if (assemblyName.Contains("MonoTouch"))
                packages.Add("mono.touch");
        }
        
        return packages;
    }

    private void AnalyzeNodeWithPackages(SyntaxNodeAnalysisContext context, List<string> packages)
    {
        switch (context.Node)
        {
            case VariableDeclarationSyntax variableDeclaration:
                AnalyzeVariableDeclarationWithPackages(context, variableDeclaration, packages);
                break;
            case AssignmentExpressionSyntax assignment:
                AnalyzeAssignmentWithPackages(context, assignment, packages);
                break;
        }
    }

    private void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case VariableDeclarationSyntax variableDeclaration:
                AnalyzeVariableDeclaration(context, variableDeclaration);
                break;
            case AssignmentExpressionSyntax assignment:
                AnalyzeAssignment(context, assignment);
                break;
        }
    }

    private void AnalyzeVariableDeclarationWithPackages(SyntaxNodeAnalysisContext context, VariableDeclarationSyntax variableDeclaration, List<string> packages)
    {
        foreach (var variable in variableDeclaration.Variables)
        {
            if (variable.Initializer?.Value is null)
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(variable.Initializer.Value);
            if (typeInfo.Type?.IsReferenceType == true && !typeInfo.Type.IsNullableReferenceType())
            {
                // Check if this matches any package-specific patterns
                var packagePattern = GetPackageSpecificPattern(variable.Initializer.Value, packages, context.SemanticModel);
                if (packagePattern != null)
                {
                    var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText, packagePattern.Description);
                    context.ReportDiagnostic(diagnostic);
                }
                else
                {
                    // This could be a null reference
                    var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private void AnalyzeAssignmentWithPackages(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment, List<string> packages)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(assignment.Right);
        if (typeInfo.Type?.IsReferenceType == true && !typeInfo.Type.IsNullableReferenceType())
        {
            // Check if this matches any package-specific patterns
            var packagePattern = GetPackageSpecificPattern(assignment.Right, packages, context.SemanticModel);
            if (packagePattern != null)
            {
                var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), assignment.Left.ToString(), packagePattern.Description);
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                // This could be a null assignment
                var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), assignment.Left.ToString());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, VariableDeclarationSyntax variableDeclaration)
    {
        foreach (var variable in variableDeclaration.Variables)
        {
            if (variable.Initializer?.Value is null)
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(variable.Initializer.Value);
            if (typeInfo.Type?.IsReferenceType == true && !typeInfo.Type.IsNullableReferenceType())
            {
                // This could be a null reference
                var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(assignment.Right);
        if (typeInfo.Type?.IsReferenceType == true && !typeInfo.Type.IsNullableReferenceType())
        {
            // This could be a null assignment
            var diagnostic = Diagnostic.Create(Rule, assignment.GetLocation(), assignment.Left.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private NullPattern? GetPackageSpecificPattern(ExpressionSyntax expression, List<string> packages, SemanticModel semanticModel)
    {
        var code = expression.ToString();
        
        foreach (var package in packages)
        {
            var rules = LegacyPackageRules.GetRulesForPackage(package);
            
            foreach (var rule in rules)
            {
                if (MatchesPattern(code, rule.CodePattern))
                {
                    return rule;
                }
            }
        }
        
        return null;
    }

    private bool MatchesPattern(string code, string pattern)
    {
        // Simple pattern matching - in a real implementation, this would be more sophisticated
        return code.Contains(pattern.Replace("??", "").Replace("?.", "").Replace("!", "").Trim());
    }
}

/// <summary>
/// Code fix provider for null handling suggestions
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullWizardCodeFixProvider))]
public class NullWizardCodeFixProvider : CodeFixProvider
{
    public const string FixTitle = "Apply null handling pattern";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(NullWizardAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the node that triggered the diagnostic
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root == null) return;

        var node = root.FindNode(diagnosticSpan);
        if (node == null) return;

        // Register code fixes
        context.RegisterCodeFix(
            CodeAction.Create(
                title: FixTitle,
                createChangedDocument: c => ApplyNullHandlingFixAsync(context.Document, node, c),
                equivalenceKey: FixTitle),
            diagnostic);
    }

    private async Task<Document> ApplyNullHandlingFixAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root == null) return document;

        // Get the semantic model to understand the context
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
        if (semanticModel == null) return document;

        // Detect packages for package-specific fixes
        var packages = DetectPackages(semanticModel.Compilation);
        
        // Determine the best null handling strategy based on context and packages
        var strategy = DetermineNullHandlingStrategy(node, semanticModel, packages);
        
        // Apply the fix
        var newRoot = ApplyNullHandlingStrategy(root, node, strategy);
        
        return document.WithSyntaxRoot(newRoot);
    }

    private List<string> DetectPackages(Compilation compilation)
    {
        var packages = new List<string>();
        
        foreach (var reference in compilation.References)
        {
            var assemblyName = reference.Display;
            
            if (assemblyName.Contains("Avalonia"))
                packages.Add("avalonia");
            else if (assemblyName.Contains("Newtonsoft.Json") || assemblyName.Contains("Json.Net"))
                packages.Add("newtonsoft.json");
            else if (assemblyName.Contains("System.Data.SqlClient"))
                packages.Add("system.data.sqlclient");
            else if (assemblyName.Contains("System.Net.Http"))
                packages.Add("system.net.http");
            else if (assemblyName.Contains("RestSharp"))
                packages.Add("restsharp");
            else if (assemblyName.Contains("NLog"))
                packages.Add("nlog");
            else if (assemblyName.Contains("log4net"))
                packages.Add("log4net");
            else if (assemblyName.Contains("Autofac"))
                packages.Add("autofac");
            else if (assemblyName.Contains("Castle.Windsor"))
                packages.Add("castle.windsor");
            else if (assemblyName.Contains("NHibernate"))
                packages.Add("nhibernate");
            else if (assemblyName.Contains("EntityFramework"))
                packages.Add("entityframework");
            else if (assemblyName.Contains("System.Web"))
                packages.Add("system.web");
            else if (assemblyName.Contains("System.Windows.Forms"))
                packages.Add("system.windows.forms");
            else if (assemblyName.Contains("System.Windows.Presentation"))
                packages.Add("system.windows.presentation");
            else if (assemblyName.Contains("Xamarin.Forms"))
                packages.Add("xamarin.forms");
            else if (assemblyName.Contains("Mono.Android"))
                packages.Add("mono.android");
            else if (assemblyName.Contains("MonoTouch"))
                packages.Add("mono.touch");
        }
        
        return packages;
    }

    private NullStrategy DetermineNullHandlingStrategy(SyntaxNode node, SemanticModel semanticModel, List<string> packages)
    {
        // First check for package-specific patterns
        var packagePattern = GetPackageSpecificPattern(node, packages, semanticModel);
        if (packagePattern != null)
        {
            return packagePattern.Strategy;
        }
        
        // Fall back to context-based strategy
        var context = AnalyzeContext(node, semanticModel);
        
        return context switch
        {
            NullContext.JsonDeserialization => NullStrategy.NullCoalescing,
            NullContext.DatabaseResult => NullStrategy.NullCoalescing,
            NullContext.ApiResponse => NullStrategy.NullConditional,
            NullContext.UserInput => NullStrategy.ThrowIfNull,
            NullContext.ConstructorParameter => NullStrategy.ThrowIfNull,
            NullContext.MethodParameter => NullStrategy.AllowNull,
            NullContext.PropertySetter => NullStrategy.ValidateAndTransform,
            NullContext.ReturnValue => NullStrategy.UseDefault,
            _ => NullStrategy.NullCoalescing
        };
    }

    private NullPattern? GetPackageSpecificPattern(SyntaxNode node, List<string> packages, SemanticModel semanticModel)
    {
        var code = node.ToString();
        
        foreach (var package in packages)
        {
            var rules = LegacyPackageRules.GetRulesForPackage(package);
            
            foreach (var rule in rules)
            {
                if (MatchesPattern(code, rule.CodePattern))
                {
                    return rule;
                }
            }
        }
        
        return null;
    }

    private bool MatchesPattern(string code, string pattern)
    {
        // Simple pattern matching - in a real implementation, this would be more sophisticated
        return code.Contains(pattern.Replace("??", "").Replace("?.", "").Replace("!", "").Trim());
    }

    private NullContext AnalyzeContext(SyntaxNode node, SemanticModel semanticModel)
    {
        // Analyze the surrounding code to determine context
        var parent = node.Parent;
        
        // Check for JSON patterns
        if (ContainsJsonPattern(node))
            return NullContext.JsonDeserialization;
            
        // Check for database patterns
        if (ContainsDatabasePattern(node))
            return NullContext.DatabaseResult;
            
        // Check for API patterns
        if (ContainsApiPattern(node))
            return NullContext.ApiResponse;
            
        // Check for constructor parameters
        if (parent?.Ancestors().OfType<ConstructorDeclarationSyntax>().Any() == true)
            return NullContext.ConstructorParameter;
            
        // Check for method parameters
        if (parent?.Ancestors().OfType<ParameterSyntax>().Any() == true)
            return NullContext.MethodParameter;
            
        // Default to user input
        return NullContext.UserInput;
    }

    private bool ContainsJsonPattern(SyntaxNode node)
    {
        var code = node.ToString();
        return System.Text.RegularExpressions.Regex.IsMatch(code, @"JsonConvert\.|JsonSerializer\.|\[JsonProperty\]|\[JsonIgnore\]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private bool ContainsDatabasePattern(SyntaxNode node)
    {
        var code = node.ToString();
        return System.Text.RegularExpressions.Regex.IsMatch(code, @"SqlCommand|SqlDataReader|DbCommand|DbDataReader|ExecuteReader|ExecuteScalar", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private bool ContainsApiPattern(SyntaxNode node)
    {
        var code = node.ToString();
        return System.Text.RegularExpressions.Regex.IsMatch(code, @"HttpClient|WebRequest|REST|API|Endpoint", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    private SyntaxNode ApplyNullHandlingStrategy(SyntaxNode root, SyntaxNode node, NullStrategy strategy)
    {
        switch (strategy)
        {
            case NullStrategy.NullCoalescing:
                return ApplyNullCoalescing(root, node);
            case NullStrategy.NullConditional:
                return ApplyNullConditional(root, node);
            case NullStrategy.ThrowIfNull:
                return ApplyThrowIfNull(root, node);
            case NullStrategy.UseDefault:
                return ApplyUseDefault(root, node);
            default:
                return root;
        }
    }

    private SyntaxNode ApplyNullCoalescing(SyntaxNode root, SyntaxNode node)
    {
        // Apply null-coalescing operator (??)
        if (node is AssignmentExpressionSyntax assignment)
        {
            var newAssignment = assignment.WithRight(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    assignment.Right,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
            
            return root.ReplaceNode(assignment, newAssignment);
        }
        
        return root;
    }

    private SyntaxNode ApplyNullConditional(SyntaxNode root, SyntaxNode node)
    {
        // Apply null-conditional operator (?.)
        // This is more complex and would need more context
        return root;
    }

    private SyntaxNode ApplyThrowIfNull(SyntaxNode root, SyntaxNode node)
    {
        // Add null check with throw
        if (node is AssignmentExpressionSyntax assignment)
        {
            var nullCheck = SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    assignment.Right,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SyntaxFactory.ThrowStatement(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.ParseTypeName("ArgumentNullException"),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal(assignment.Left.ToString()))))))));
            
            var block = SyntaxFactory.Block(nullCheck, SyntaxFactory.ExpressionStatement(assignment));
            return root.ReplaceNode(assignment, block);
        }
        
        return root;
    }

    private SyntaxNode ApplyUseDefault(SyntaxNode root, SyntaxNode node)
    {
        // Apply default value
        if (node is AssignmentExpressionSyntax assignment)
        {
            var newAssignment = assignment.WithRight(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.CoalesceExpression,
                    assignment.Right,
                    SyntaxFactory.DefaultExpression(SyntaxFactory.ParseTypeName("object"))));
            
            return root.ReplaceNode(assignment, newAssignment);
        }
        
        return root;
    }
}
