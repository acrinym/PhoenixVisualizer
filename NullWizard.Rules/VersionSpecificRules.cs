using NullWizard.Core.Models;

namespace NullWizard.Rules;

/// <summary>
/// Version-specific null handling rules for different .NET frameworks
/// </summary>
public static class VersionSpecificRules
{
    /// <summary>
    /// Gets null handling rules for a specific target framework
    /// </summary>
    /// <param name="targetFramework">The target framework</param>
    /// <returns>List of applicable null handling patterns</returns>
    public static List<NullPattern> GetRulesForFramework(string targetFramework)
    {
        return targetFramework.ToLowerInvariant() switch
        {
            "netstandard1.0" => GetNetStandard10Rules(),
            "netstandard1.1" => GetNetStandard11Rules(),
            "netstandard1.2" => GetNetStandard12Rules(),
            "netstandard1.3" => GetNetStandard13Rules(),
            "netstandard1.4" => GetNetStandard14Rules(),
            "netstandard1.5" => GetNetStandard15Rules(),
            "netstandard1.6" => GetNetStandard16Rules(),
            "net20" => GetNet20Rules(),
            "net35" => GetNet35Rules(),
            "net40" => GetNet40Rules(),
            "net45" => GetNet45Rules(),
            _ => GetDefaultRules()
        };
    }

    private static List<NullPattern> GetNetStandard10Rules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "json_deserialization_netstandard10",
                Description = "JSON deserialization with null-coalescing for .NET Standard 1.0",
                Context = NullContext.JsonDeserialization,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var result = JsonConvert.DeserializeObject<MyType>(json) ?? new MyType();",
                FixPattern = "var result = JsonConvert.DeserializeObject<MyType>(json) ?? new MyType();",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0" }
            },
            new()
            {
                PatternId = "database_result_netstandard10",
                Description = "Database result handling with null-coalescing for .NET Standard 1.0",
                Context = NullContext.DatabaseResult,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var value = reader[\"ColumnName\"] as string ?? string.Empty;",
                FixPattern = "var value = reader[\"ColumnName\"] as string ?? string.Empty;",
                OccurrenceCount = 1,
                Confidence = 0.9,
                ApplicableFrameworks = new[] { "netstandard1.0" }
            }
        };
    }

    private static List<NullPattern> GetNetStandard11Rules()
    {
        var rules = GetNetStandard10Rules();
        rules.Add(new()
        {
            PatternId = "api_response_netstandard11",
            Description = "API response handling with null-conditional for .NET Standard 1.1",
            Context = NullContext.ApiResponse,
            Strategy = NullStrategy.NullConditional,
            CodePattern = "var name = response?.Data?.Name ?? \"Unknown\";",
            FixPattern = "var name = response?.Data?.Name ?? \"Unknown\";",
            OccurrenceCount = 1,
            Confidence = 0.9,
            ApplicableFrameworks = new[] { "netstandard1.1" }
        });
        return rules;
    }

    private static List<NullPattern> GetNetStandard12Rules()
    {
        var rules = GetNetStandard11Rules();
        rules.Add(new()
        {
            PatternId = "constructor_validation_netstandard12",
            Description = "Constructor parameter validation for .NET Standard 1.2",
            Context = NullContext.ConstructorParameter,
            Strategy = NullStrategy.ThrowIfNull,
            CodePattern = "if (parameter == null) throw new ArgumentNullException(nameof(parameter));",
            FixPattern = "if (parameter == null) throw new ArgumentNullException(nameof(parameter));",
            OccurrenceCount = 1,
            Confidence = 0.95,
            ApplicableFrameworks = new[] { "netstandard1.2" }
        });
        return rules;
    }

    private static List<NullPattern> GetNetStandard13Rules()
    {
        var rules = GetNetStandard12Rules();
        rules.Add(new()
        {
            PatternId = "property_setter_netstandard13",
            Description = "Property setter validation for .NET Standard 1.3",
            Context = NullContext.PropertySetter,
            Strategy = NullStrategy.ValidateAndTransform,
            CodePattern = "set { _value = value ?? throw new ArgumentNullException(nameof(value)); }",
            FixPattern = "set { _value = value ?? throw new ArgumentNullException(nameof(value)); }",
            OccurrenceCount = 1,
            Confidence = 0.9,
            ApplicableFrameworks = new[] { "netstandard1.3" }
        });
        return rules;
    }

    private static List<NullPattern> GetNetStandard14Rules()
    {
        var rules = GetNetStandard13Rules();
        rules.Add(new()
        {
            PatternId = "return_value_netstandard14",
            Description = "Return value handling for .NET Standard 1.4",
            Context = NullContext.ReturnValue,
            Strategy = NullStrategy.UseDefault,
            CodePattern = "return result ?? default(MyType);",
            FixPattern = "return result ?? default(MyType);",
            OccurrenceCount = 1,
            Confidence = 0.85,
            ApplicableFrameworks = new[] { "netstandard1.4" }
        });
        return rules;
    }

    private static List<NullPattern> GetNetStandard15Rules()
    {
        var rules = GetNetStandard14Rules();
        rules.Add(new()
        {
            PatternId = "method_parameter_netstandard15",
            Description = "Method parameter handling for .NET Standard 1.5",
            Context = NullContext.MethodParameter,
            Strategy = NullStrategy.AllowNull,
            CodePattern = "public void Process(string? input) { /* handle null */ }",
            FixPattern = "public void Process(string? input) { /* handle null */ }",
            OccurrenceCount = 1,
            Confidence = 0.8,
            ApplicableFrameworks = new[] { "netstandard1.5" }
        });
        return rules;
    }

    private static List<NullPattern> GetNetStandard16Rules()
    {
        var rules = GetNetStandard15Rules();
        rules.Add(new()
        {
            PatternId = "legacy_integration_netstandard16",
            Description = "Legacy code integration for .NET Standard 1.6",
            Context = NullContext.LegacyCode,
            Strategy = NullStrategy.NullForgiving,
            CodePattern = "var result = legacyMethod()!;",
            FixPattern = "var result = legacyMethod()!;",
            OccurrenceCount = 1,
            Confidence = 0.7,
            ApplicableFrameworks = new[] { "netstandard1.6" }
        });
        return rules;
    }

    private static List<NullPattern> GetNet20Rules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "net20_basic_null_check",
                Description = "Basic null checking for .NET 2.0",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.ThrowIfNull,
                CodePattern = "if (input == null) throw new ArgumentNullException(\"input\");",
                FixPattern = "if (input == null) throw new ArgumentNullException(\"input\");",
                OccurrenceCount = 1,
                Confidence = 0.95,
                ApplicableFrameworks = new[] { "net20" }
            }
        };
    }

    private static List<NullPattern> GetNet35Rules()
    {
        var rules = GetNet20Rules();
        rules.Add(new()
        {
            PatternId = "net35_extension_methods",
            Description = "Extension method null handling for .NET 3.5",
            Context = NullContext.MethodParameter,
            Strategy = NullStrategy.NullConditional,
            CodePattern = "public static string SafeToString(this object? obj) => obj?.ToString() ?? string.Empty;",
            FixPattern = "public static string SafeToString(this object? obj) => obj?.ToString() ?? string.Empty;",
            OccurrenceCount = 1,
            Confidence = 0.9,
            ApplicableFrameworks = new[] { "net35" }
        });
        return rules;
    }

    private static List<NullPattern> GetNet40Rules()
    {
        var rules = GetNet35Rules();
        rules.Add(new()
        {
            PatternId = "net40_nullable_types",
            Description = "Nullable types handling for .NET 4.0",
            Context = NullContext.ReturnValue,
            Strategy = NullStrategy.UseDefault,
            CodePattern = "return nullableValue ?? default(T);",
            FixPattern = "return nullableValue ?? default(T);",
            OccurrenceCount = 1,
            Confidence = 0.9,
            ApplicableFrameworks = new[] { "net40" }
        });
        return rules;
    }

    private static List<NullPattern> GetNet45Rules()
    {
        var rules = GetNet40Rules();
        rules.Add(new()
        {
            PatternId = "net45_callinfo",
            Description = "Caller information for .NET 4.5",
            Context = NullContext.MethodParameter,
            Strategy = NullStrategy.ThrowIfNull,
            CodePattern = "if (parameter == null) throw new ArgumentNullException(nameof(parameter));",
            FixPattern = "if (parameter == null) throw new ArgumentNullException(nameof(parameter));",
            OccurrenceCount = 1,
            Confidence = 0.95,
            ApplicableFrameworks = new[] { "net45" }
        });
        return rules;
    }

    private static List<NullPattern> GetDefaultRules()
    {
        return new List<NullPattern>
        {
            new()
            {
                PatternId = "default_null_coalescing",
                Description = "Default null-coalescing pattern",
                Context = NullContext.UserInput,
                Strategy = NullStrategy.NullCoalescing,
                CodePattern = "var result = input ?? defaultValue;",
                FixPattern = "var result = input ?? defaultValue;",
                OccurrenceCount = 1,
                Confidence = 0.8,
                ApplicableFrameworks = new[] { "netstandard1.0", "netstandard1.1", "netstandard1.2", "netstandard1.3", "netstandard1.4", "netstandard1.5", "netstandard1.6" }
            }
        };
    }
}
