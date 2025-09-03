# PhoenixVisualizer Documentation

## 📁 **Documentation Organization**

### **Root Level (`/docs/`)**
- **`README.md`** - This file (documentation organization guide)
- **`/development-reports/`** - All development progress reports and fix documentation
- **`/active/`** - Current project documentation and guides
- **`/archive/`** - Outdated documentation and historical records

### **Development Reports (`/docs/development-reports/`)**
All files related to project status, bug fixes, and development progress go here:

**Current Reports:**
- `AVALONIAEDIT_FIX_REPORT.md` - AvaloniaEdit compatibility fixes
- `AVS_IMPORT_ENCODING_FIX_REPORT.md` - AVS import encoding issues
- `AVS_IMPORT_PARSING_IMPROVEMENTS_REPORT.md` - AVS parsing improvements
- `BUILD_QUALITY_REPORT.md` - Build system quality analysis
- `BUILD_STATUS_REPORT.md` - Build status and fixes
- `IMPLEMENTATION_PROGRESS_REPORT.md` - Implementation progress tracking
- `LINTER_FIXES.md` - Code linting fixes
- `PHOENIXVISUALIZER_STATUS_REPORT.md` - Overall project status
- `PHX_EDITOR_WIRING_FIX_REPORT.md` - PHX Editor UI fixes
- `PR_UPDATE_SUMMARY.md` - Pull request summaries
- `REACTIVEUI_COMPATIBILITY_FIX_REPORT.md` - ReactiveUI compatibility fixes
- `WINAMP_BINARY_FORMAT_FIX_REPORT.md` - Winamp binary format implementation

### **Active Documentation (`/docs/active/`)**
Current project documentation that users and developers need:

- **User Guides** - How to use the application
- **Developer Guides** - How to contribute and develop
- **API Documentation** - Code documentation and interfaces
- **Installation Guides** - Setup and installation instructions

### **Archive (`/docs/archive/`)**
Outdated documentation that's kept for historical reference:

- **Deprecated Features** - Documentation for removed features
- **Historical Reports** - Old development reports
- **Version-Specific Docs** - Documentation for old versions

## 📋 **Documentation Rules**

### **What Goes Where:**

1. **Development Reports** → `/docs/development-reports/`
   - Bug fix reports
   - Implementation progress
   - Build status reports
   - Compatibility fixes
   - Performance improvements

2. **User Documentation** → `/docs/active/`
   - User guides
   - Tutorials
   - Feature documentation
   - Installation guides

3. **Developer Documentation** → `/docs/active/`
   - API documentation
   - Contributing guidelines
   - Architecture documentation
   - Code style guides

4. **Historical Documentation** → `/docs/archive/`
   - Deprecated features
   - Old version docs
   - Historical reports

### **Naming Conventions:**

- **Reports**: `DESCRIPTION_REPORT.md` (e.g., `WINAMP_BINARY_FORMAT_FIX_REPORT.md`)
- **Guides**: `DESCRIPTION_GUIDE.md` (e.g., `USER_GUIDE.md`)
- **Documentation**: `DESCRIPTION.md` (e.g., `API.md`)

### **File Organization:**

- Keep related documentation together
- Use clear, descriptive names
- Include dates in filenames for time-sensitive docs
- Group by feature/component when possible

## 🚫 **What NOT to Do:**

- ❌ Don't leave documentation files in the root directory
- ❌ Don't create documentation without a clear purpose
- ❌ Don't duplicate information across multiple files
- ❌ Don't keep outdated documentation in active folders

## ✅ **What TO Do:**

- ✅ Move all development reports to `/docs/development-reports/`
- ✅ Keep user-facing docs in `/docs/active/`
- ✅ Archive old docs in `/docs/archive/`
- ✅ Use clear, descriptive filenames
- ✅ Update this README when adding new documentation types

---

**Remember: We're building software, not writing a book. Keep documentation focused, organized, and purposeful.**
