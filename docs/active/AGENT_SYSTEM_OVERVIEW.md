# 🤖 Agent System Overview

## Purpose
This repository has comprehensive guidelines and tools to ensure all code agents create high-quality, informative pull requests that provide complete context for reviewers and future developers.

## Component Overview

### 📋 Core Guidelines
- **[AGENTS.md](./AGENTS.md)** - Complete documentation standards for PR descriptions
- **[AGENT_PR_CHECKLIST.md](./AGENT_PR_CHECKLIST.md)** - Quick reference checklist for agents
- **[README.md](./README.md)** - Main repository overview with agent requirements

### 🛠️ Templates & Automation
- **[.github/pull_request_template.md](./.github/pull_request_template.md)** - GitHub PR template enforcing guidelines
- **[scripts/validate_pr.py](./scripts/validate_pr.py)** - Validation script for PR descriptions

## Key Principles

### 1. Build Verification First ✅
Every agent MUST run a complete build before creating a PR:
```bash
dotnet build [Solution].sln -c Release --verbosity minimal
```

### 2. Complete Status Reporting 📊
- Document exact build exit code
- Include ALL error messages with file paths and line numbers  
- Report ALL warnings even if build succeeds
- Categorize issues by severity and impact

### 3. Actionable Next Steps 🎯
- Specify what needs to be done next with priorities
- Provide concrete fix instructions for any failures
- Identify future considerations and technical debt

### 4. Honest Testing Documentation 🧪
- Report what testing was actually performed
- Don't claim testing that wasn't done
- Include manual verification steps taken

## Compliance Enforcement

### Validation Tools
```bash
# Validate a PR description
python scripts/validate_pr.py pr_description.md

# Quick compliance check
python scripts/validate_pr.py my_pr.md && echo "✅ Ready to submit"
```

### Required Sections
1. **Summary** - Brief overview
2. **What Was Done** - Completed tasks with details
3. **What Needs To Be Done Next** - Future tasks with priorities
4. **Build Status** - Success/warnings/failure with exact command
5. **Issues & Warnings** - Complete error/warning output
6. **How To Fix Build Failures** - Actionable fix instructions
7. **Testing Status** - Honest reporting of testing performed
8. **Dependencies & Requirements** - New packages, system requirements

### Status Classifications
- ✅ **SUCCESS**: Exit code 0, no errors, minimal warnings
- ⚠️ **WARNINGS**: Exit code 0 but important warnings present
- ❌ **FAILED**: Non-zero exit code, compilation errors blocking

## Usage Examples

### For GitHub PRs
1. Create PR using the template in `.github/pull_request_template.md`
2. Fill out all required sections
3. Run `scripts/validate_pr.py` to verify compliance
4. Submit PR

### For Manual PR Creation
1. Follow the format defined in `AGENTS.md`
2. Use `AGENT_PR_CHECKLIST.md` as reference
3. Validate using the script before submission

### For Code Review
1. Check that all required sections are present
2. Verify build status is documented with exact output
3. Ensure next steps are specific and prioritized
4. Confirm fix instructions are actionable

## Benefits

### For Reviewers
- Complete context for understanding changes
- Clear build status and any issues
- Specific guidance on what to focus on
- Actionable next steps for continuation

### For Future Developers
- Historical context of implementation decisions
- Complete error documentation for debugging
- Clear understanding of technical debt and future work
- Proper dependency documentation

### For Project Management
- Clear progress tracking with completed tasks
- Prioritized future work items
- Build health monitoring
- Dependency impact assessment

## Non-Compliance Consequences

**⚠️ Pull requests that don't follow these guidelines will be:**
1. **Rejected** with request for proper documentation
2. **Blocked** from merging until compliance is achieved
3. **Flagged** for agent process improvement

## Quick Reference

**Before Every PR:**
1. ✅ Run complete build verification
2. 📋 Document ALL outputs (errors AND warnings)
3. 🎯 Identify specific next steps with priorities
4. 🛠️ Provide actionable fix instructions
5. 🧪 Report honest testing status
6. ✅ Validate with compliance script

**Remember:** The goal is to provide complete context and ensure project continuity, not just document code changes.

---

*This system ensures that every PR provides maximum value to reviewers and maintains high standards for project documentation and progress tracking.*