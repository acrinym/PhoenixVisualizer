# Phoenix Visualizer Git History Search Tools

This directory contains powerful tools to help you search through git history to find files across different PRs and branches, addressing GitHub web search limitations.

## 🚀 Quick Start

### Windows (PowerShell)
```powershell
# Basic search
.\search_git_history.ps1 "NsEelEvaluator"

# Search in C# files only
.\search_git_history.ps1 "NsEelEvaluator" "*.cs"

# Search with content preview
.\search_git_history.ps1 "circular dependency" "*.cs" 100 true

# Get help
.\search_git_history.ps1 -Help
```

### Linux/Mac (Bash)
```bash
# Basic search
./search_git_history.sh "NsEelEvaluator"

# Search in C# files only
./search_git_history.sh "NsEelEvaluator" "*.cs"

# Search with content preview
./search_git_history.sh "circular dependency" "*.cs" 100 true

# Get help
./search_git_history.sh help
```

## 🛠️ Available Tools

### 1. `search_git_history.ps1` (PowerShell - Windows)
- **Full-featured** search tool with colored output
- **Progress tracking** and detailed results
- **Parameter validation** and error handling
- **Content preview** with context lines

### 2. `search_git_history.sh` (Bash - Linux/Mac)
- **Cross-platform** compatibility
- **Colored output** for better readability
- **Efficient** git operations
- **Content preview** capabilities

### 3. `export_with_build_status.ps1` (PowerShell - Windows)
- **Build status tracking** included in exports
- **Error/warning counting** for project health
- **Comprehensive** source code dumps
- **Progress bars** and detailed feedback

## 🔍 Search Capabilities

### What You Can Search For:
- **File names** - Find specific files across history
- **Class names** - Locate class implementations
- **Function names** - Find function definitions
- **Error messages** - Track down compilation issues
- **Comments** - Search through code documentation
- **Any text** - Search for arbitrary content

### Search Scope:
- **Current branch** - Search in active branch
- **All branches** - Cross-branch search
- **Git history** - Search through all commits
- **File patterns** - Limit to specific file types
- **Content** - Search actual file contents

## 📋 Common Use Cases

### 1. Finding Related Changes
```bash
# Before creating a PR, search for related work
./search_git_history.sh "VLC integration" "*.cs"
```

### 2. Debugging Issues
```bash
# Find where an error was introduced
./search_git_history.sh "circular dependency" "*.cs" 50 true
```

### 3. Code Review Preparation
```bash
# See what files contain a specific feature
./search_git_history.sh "NsEelEvaluator" "*.cs"
```

### 4. Finding Duplicate Code
```bash
# Search for potential code duplication
./search_git_history.sh "class AvsPresetInfo" "*.cs"
```

## 🔧 Advanced Usage

### PowerShell Parameters
```powershell
.\search_git_history.ps1 -SearchTerm "search text" `
                         -FilePattern "*.cs" `
                         -Branch "feature/branch" `
                         -MaxResults 100 `
                         -ShowContent `
                         -Verbose
```

### Bash Parameters
```bash
./search_git_history.sh "search text" "*.cs" 100 true
# Parameters: search_term file_pattern max_results show_content
```

## 📊 Output Examples

### Search Results
```
🚀 Starting comprehensive git history search...
Search Term: 'NsEelEvaluator'
File Pattern: '*.cs'
Branch: 'main'
Max Results: 50

1️⃣  SEARCHING COMMIT MESSAGES
================================
Found commits:
  abc1234 Add NsEelEvaluator interface
  def5678 Implement NsEelEvaluator in PluginHost

2️⃣  SEARCHING CURRENT FILE CONTENT
=====================================
Found 3 files currently containing 'NsEelEvaluator':
  PhoenixVisualizer.Core/Effects/Interfaces/INsEelEvaluator.cs
  PhoenixVisualizer.PluginHost/NsEelEvaluator.cs
  PhoenixVisualizer.Core/Avs/CompleteAvsPresetLoader.cs
```

### Content Preview (with -ShowContent)
```
5️⃣  DETAILED FILE CONTENT
===========================
📄 File: PhoenixVisualizer.Core/Effects/Interfaces/INsEelEvaluator.cs
Content containing 'NsEelEvaluator':
----------------------------------------
    >>> 15: public interface INsEelEvaluator
       16: {
       17:     double Evaluate(string expression);
       18:     void SetAudioData(float[] leftChannel, float[] rightChannel);
       19: }
```

## 🌐 GitHub Web Search Alternatives

When the web interface is limited, use these manual search strategies:

### Search Operators
- `filename:filename.cs` - Search specific files
- `repo:username/repo` - Limit to your repository
- `path:folder/` - Search specific directories
- `language:csharp` - Limit to C# files

### Direct URLs
- **Commits**: `https://github.com/username/repo/commits?q=SearchTerm`
- **Code**: `https://github.com/username/repo/search?q=SearchTerm`
- **Issues**: `https://github.com/username/repo/issues?q=SearchTerm`
- **PRs**: `https://github.com/username/repo/pulls?q=SearchTerm`

## 🚨 Troubleshooting

### Common Issues

#### Git Not Found
```bash
# Install Git first
# Windows: https://git-scm.com/downloads
# Linux: sudo apt-get install git
# Mac: brew install git
```

#### Permission Denied
```bash
# Make script executable (Linux/Mac)
chmod +x search_git_history.sh
```

#### No Results Found
- **Check spelling** of search terms
- **Verify file patterns** are correct
- **Ensure** you're in the right directory
- **Try broader** search terms first

### Performance Tips
- **Limit results** with `MaxResults` parameter
- **Use file patterns** to narrow search scope
- **Search specific branches** when possible
- **Use content preview** sparingly for large files

## 📈 Best Practices

### Before Creating PRs
1. **Search for related work** to avoid conflicts
2. **Check for duplicate implementations**
3. **Verify naming conventions** are consistent
4. **Look for similar patterns** to follow

### During Code Reviews
1. **Search for related changes** in other files
2. **Check for breaking changes** across branches
3. **Verify integration points** are properly handled
4. **Look for test coverage** of similar features

### For Debugging
1. **Search error messages** to find root causes
2. **Look for recent changes** that might have introduced issues
3. **Check multiple branches** for different implementations
4. **Use content preview** to see exact context

## 🔗 Related Tools

- **Export Scripts**: Create comprehensive project backups
- **Build Status**: Track compilation health over time
- **Source Dumps**: Full project state snapshots
- **Git Operations**: Advanced git workflow tools

## 📝 Contributing

To improve these search tools:

1. **Test** with different search scenarios
2. **Report** any issues or edge cases
3. **Suggest** new search capabilities
4. **Optimize** performance for large repositories

---

**💡 Pro Tip**: Use these tools regularly to maintain awareness of your codebase and avoid conflicts during development!