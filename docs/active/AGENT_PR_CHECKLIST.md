# 🤖 Agent PR Quick Checklist

## Before Creating ANY Pull Request

### 1. Build Verification Required ✅
```bash
# ALWAYS run this command first
dotnet build [Solution].sln -c Release --verbosity minimal

# Capture the COMPLETE output including:
# - Exit code
# - All error messages  
# - All warning messages
# - File paths and line numbers
```

### 2. Documentation Requirements ✅
- [ ] Fill out ALL sections in AGENTS.md format
- [ ] Include exact build command used
- [ ] Copy complete error/warning text (don't summarize)
- [ ] List specific next steps with priorities
- [ ] Provide actionable fix instructions

### 3. Status Classification ✅
**Build Result Types:**
- ✅ **SUCCESS**: Exit code 0, no errors, minimal warnings OK
- ⚠️ **WARNINGS**: Exit code 0 but important warnings present  
- ❌ **FAILED**: Non-zero exit code, compilation errors blocking

### 4. Error Reporting Standards ✅
```
GOOD ✅:
/workspace/Project/File.cs(42,15): error CS0246: The type 'MissingType' could not be found

BAD ❌:
There's an error about a missing type in File.cs
```

### 5. Warning Categorization ✅
- **🔥 Critical**: CS0xxx errors, AVLN3000 XAML errors
- **⚡ Important**: CS8xxx nullable warnings, functionality impact
- **📝 Style**: CS1xxx documentation, deprecated APIs

### 6. Next Steps Format ✅
```markdown
🔄 **Immediate Next Steps:**
- [ ] Fix XAML binding errors in ConfigWindow.axaml (priority: high)
- [ ] Update obsolete API usage (priority: medium)
- [ ] Add null checks for safety (priority: low)
```

## Common Mistakes to Avoid ❌

1. **DON'T** submit PRs without running build
2. **DON'T** summarize errors - copy exact text
3. **DON'T** skip warning documentation
4. **DON'T** forget to specify priorities
5. **DON'T** omit next steps or fix instructions

## Agent Automation Commands

```bash
# Standard build check
export PATH="$PATH:/home/ubuntu/.dotnet"
dotnet build PhoenixVisualizer.sln -c Release --verbosity minimal

# With error filtering for large outputs
dotnet build PhoenixVisualizer.sln -c Release --verbosity minimal | grep -E "(error|Error|warning|Warning)" | head -20

# Count specific warning types
dotnet build PhoenixVisualizer.sln -c Release 2>&1 | grep -c "CS8"
```

## Template Usage

1. **Start with** AGENTS.md format
2. **Use** .github/pull_request_template.md for GitHub PRs
3. **Reference** this checklist before submitting
4. **Verify** all sections are complete

---

⚠️ **NON-COMPLIANCE = PR REJECTION**

Pull requests that don't follow these guidelines will be rejected and require resubmission with proper documentation.