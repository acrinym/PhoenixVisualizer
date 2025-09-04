# NullWizard 🧙‍♂️

A powerful Roslyn-based null intelligence layer that learns null-handling patterns from codebases and provides context-aware auto-fixes.

## 🎯 Features

- **🔍 Codebase Analysis**: Automatically discovers null handling patterns in your codebase
- **🤖 Auto-Fix**: Provides intelligent suggestions and automatic fixes for null-related issues
- **📦 Package Detection**: Detects popular packages and applies tailored null handling rules
- **🔄 Version-Specific Rules**: Adapts to different .NET target frameworks
- **🎨 Roslyn Integration**: Seamless integration with Visual Studio and other IDEs
- **📊 Pattern Learning**: Learns from your existing code patterns to provide better suggestions

## 🚀 Quick Start

### Installation

```bash
# Add NullWizard to your project
dotnet add package NullWizard
```

### Basic Usage

NullWizard will automatically analyze your codebase and provide suggestions for null handling improvements.

## 🎯 Package-Specific Patterns

NullWizard includes specialized patterns for popular packages:

### UI Frameworks
- **Avalonia**: UI controls, data binding, and MVVM patterns
- **WPF**: XAML controls and data binding scenarios
- **WinForms**: Legacy Windows Forms patterns
- **Xamarin**: Mobile development patterns

### Data & Serialization
- **Newtonsoft.Json**: JSON deserialization and serialization
- **System.Text.Json**: Modern JSON handling
- **Entity Framework**: Database query results and navigation properties
- **NHibernate**: ORM patterns and lazy loading

### Web & API
- **HttpClient**: HTTP responses and content
- **RestSharp**: REST API responses
- **System.Web**: ASP.NET patterns
- **Web API**: Controller actions and model binding

### Logging & DI
- **NLog**: Logging patterns and configuration
- **Log4Net**: Legacy logging scenarios
- **Autofac**: Dependency injection containers
- **Castle Windsor**: IoC container patterns

### Mobile & Legacy
- **Mono.Android**: Android development patterns
- **MonoTouch**: iOS development patterns
- **System.Web.Mvc**: MVC patterns
- **System.Web.Http**: Web API patterns

## 📋 Supported Frameworks

- .NET Standard 1.0+
- .NET Framework 2.0+
- .NET Core 1.0+
- .NET 5.0+
- .NET 6.0+
- .NET 7.0+
- .NET 8.0+

## 🏗️ Architecture

```
NullWizard/
├── NullWizard.Core/          # Core models and interfaces
├── NullWizard.Analyzer/      # Roslyn analyzer implementation
└── NullWizard.Rules/         # Version-specific and package rules
```

### Core Components

- **ICodebaseAnalyzer**: Analyzes codebases for null patterns
- **NullPattern**: Represents discovered null handling patterns
- **NullContext**: Defines different contexts where null can occur
- **NullStrategy**: Available strategies for handling null values

## 🔧 Configuration

### Custom Rules

You can define custom null handling patterns:

```csharp
var customPattern = new NullPattern
{
    PatternId = "MyCustomPattern",
    Description = "Custom null handling for my domain",
    Context = NullContext.DomainModel,
    Strategy = NullStrategy.ValidateAndTransform,
    CodePattern = "MyClass.Property",
    FixPattern = "MyClass.Property ?? DefaultValue"
};
```

### Framework-Specific Rules

NullWizard automatically detects your target framework and applies appropriate rules:

```csharp
// For .NET Standard 1.0
var rules = VersionSpecificRules.GetNetStandard10Rules();

// For .NET Framework 2.0
var rules = VersionSpecificRules.GetNetFramework20Rules();
```

## 🎨 Integration

### Visual Studio

NullWizard integrates seamlessly with Visual Studio, providing:
- Real-time analysis
- Quick fixes and refactorings
- IntelliSense suggestions
- Error highlighting

### Command Line

```bash
# Analyze a codebase
dotnet build --verbosity normal

# NullWizard will show suggestions during build
```

## 📊 Example Patterns

### Avalonia UI Patterns

```csharp
// Before: Potential null reference
var text = textBox.Text; // CS8602 warning

// After: Null-aware handling
var text = textBox?.Text ?? string.Empty;
```

### Newtonsoft.Json Patterns

```csharp
// Before: Potential null after deserialization
var data = JsonConvert.DeserializeObject<MyData>(json);
var name = data.Name; // CS8602 warning

// After: Null-safe deserialization
var data = JsonConvert.DeserializeObject<MyData>(json) ?? new MyData();
var name = data.Name ?? "Unknown";
```

### HttpClient Patterns

```csharp
// Before: Potential null response
var response = await httpClient.GetAsync(url);
var content = response.Content; // CS8602 warning

// After: Null-safe HTTP handling
var response = await httpClient.GetAsync(url);
var content = response?.Content ?? new StringContent(string.Empty);
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Add your patterns and rules
4. Submit a pull request

### Adding New Patterns

```csharp
public static List<NullPattern> GetMyPackageRules()
{
    return new List<NullPattern>
    {
        new NullPattern
        {
            PatternId = "MyPackage.Property",
            Description = "Handle null properties in MyPackage",
            Context = NullContext.PropertyGetter,
            Strategy = NullStrategy.NullCoalescing,
            CodePattern = "MyPackage.Property",
            FixPattern = "MyPackage.Property ?? DefaultValue"
        }
    };
}
```

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with Roslyn for powerful code analysis
- Inspired by the need for better null handling in legacy .NET codebases
- Community-driven pattern discovery and improvement

---

**NullWizard**: Making null handling intelligent, one codebase at a time! 🧙‍♂️✨


