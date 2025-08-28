# Phoenix Visualizer Git History Search Tool
# This tool helps you search through git history to find files across different PRs and branches
# 
# Usage: Run this script from the PhoenixVisualizer project root directory
# Requirements: Git must be installed and accessible

param(
    [Parameter(Mandatory=$true)]
    [string]$SearchTerm,
    
    [string]$FilePattern = "*",
    [string]$Branch = "main",
    [int]$MaxResults = 50,
    [switch]$ShowContent,
    [switch]$Verbose,
    [switch]$Help
)

# Help function
function Show-Help {
    Write-Host @"
Phoenix Visualizer Git History Search Tool
==========================================

This tool helps you search through git history to find files across different PRs and branches.

USAGE:
    .\search_git_history.ps1 -SearchTerm "filename.cs"
    .\search_git_history.ps1 -SearchTerm "class name" -FilePattern "*.cs"
    .\search_git_history.ps1 -SearchTerm "function name" -ShowContent
    .\search_git_history.ps1 -SearchTerm "error message" -MaxResults 100

PARAMETERS:
    -SearchTerm     The text to search for (required)
    -FilePattern    File pattern to search in (default: "*")
    -Branch         Git branch to search (default: "main")
    -MaxResults     Maximum number of results to show (default: 50)
    -ShowContent    Show file content around matches
    -Verbose       Show detailed search progress
    -Help          Show this help message

EXAMPLES:
    # Find all files containing "AvsPresetInfo"
    .\search_git_history.ps1 -SearchTerm "AvsPresetInfo"
    
    # Search for "NsEelEvaluator" in C# files only
    .\search_git_history.ps1 -SearchTerm "NsEelEvaluator" -FilePattern "*.cs"
    
    # Find commits that mention "circular dependency"
    .\search_git_history.ps1 -SearchTerm "circular dependency" -ShowContent
    
    # Search in a specific branch
    .\search_git_history.ps1 -SearchTerm "VLC" -Branch "feature/vlc-integration"

"@ -ForegroundColor Cyan
}

if ($Help) {
    Show-Help
    exit
}

# Validate git is available
try {
    $gitVersion = git --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Git not found"
    }
    Write-Host "✅ Git found: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Git is not available. Please install Git and ensure it's in your PATH." -ForegroundColor Red
    Write-Host "Download from: https://git-scm.com/downloads" -ForegroundColor Yellow
    exit 1
}

# Function to search git log for commits
function Search-GitLog {
    param([string]$SearchTerm, [string]$Branch, [int]$MaxResults)
    
    Write-Host "🔍 Searching git log for commits containing '$SearchTerm'..." -ForegroundColor Cyan
    
    $logCommand = "git log --all --grep=`"$SearchTerm`" --oneline --max-count=$MaxResults"
    if ($Branch -ne "main") {
        $logCommand = "git log $Branch --grep=`"$SearchTerm`" --oneline --max-count=$MaxResults"
    }
    
    try {
        $commits = Invoke-Expression $logCommand 2>&1
        return $commits
    } catch {
        Write-Host "⚠️  Error searching git log: $($_.Exception.Message)" -ForegroundColor Yellow
        return @()
    }
}

# Function to search git log for file changes
function Search-GitFileChanges {
    param([string]$SearchTerm, [string]$FilePattern, [string]$Branch, [int]$MaxResults)
    
    Write-Host "📁 Searching for files matching '$FilePattern' containing '$SearchTerm'..." -ForegroundColor Cyan
    
    $logCommand = "git log --all --name-only --grep=`"$SearchTerm`" --max-count=$MaxResults"
    if ($Branch -ne "main") {
        $logCommand = "git log $Branch --name-only --grep=`"$SearchTerm`" --max-count=$MaxResults"
    }
    
    try {
        $fileChanges = Invoke-Expression $logCommand 2>&1
        return $fileChanges
    } catch {
        Write-Host "⚠️  Error searching file changes: $($_.Exception.Message)" -ForegroundColor Yellow
        return @()
    }
}

# Function to search file content in git history
function Search-GitFileContent {
    param([string]$SearchTerm, [string]$FilePattern, [string]$Branch, [int]$MaxResults)
    
    Write-Host "🔍 Searching file content for '$SearchTerm' in '$FilePattern' files..." -ForegroundColor Cyan
    
    $grepCommand = "git grep -l --all-match `"$SearchTerm`" -- `"$FilePattern`""
    if ($Branch -ne "main") {
        $grepCommand = "git grep -l --all-match `"$SearchTerm`" $Branch -- `"$FilePattern`""
    }
    
    try {
        $files = Invoke-Expression $grepCommand 2>&1
        return $files
    } catch {
        Write-Host "⚠️  Error searching file content: $($_.Exception.Message)" -ForegroundColor Yellow
        return @()
    }
}

# Function to get file content at specific commit
function Get-GitFileContent {
    param([string]$CommitHash, [string]$FilePath)
    
    try {
        $content = git show "$CommitHash`:$FilePath" 2>&1
        return $content
    } catch {
        return "Error reading file content: $($_.Exception.Message)"
    }
}

# Function to search across all branches
function Search-AllBranches {
    param([string]$SearchTerm, [string]$FilePattern)
    
    Write-Host "🌿 Searching across all branches..." -ForegroundColor Cyan
    
    try {
        $branches = git branch -a | ForEach-Object { $_.Trim() }
        $results = @()
        
        foreach ($branch in $branches) {
            $branchName = $branch.Replace("remotes/origin/", "").Replace("remotes/", "").Replace("origin/", "")
            Write-Host "  Searching branch: $branchName" -ForegroundColor Gray
            
            $branchResults = git log $branchName --name-only --grep="$SearchTerm" --max-count=10 2>&1
            if ($branchResults) {
                $results += "BRANCH: $branchName"
                $results += $branchResults
                $results += "---"
            }
        }
        
        return $results
    } catch {
        Write-Host "⚠️  Error searching branches: $($_.Exception.Message)" -ForegroundColor Yellow
        return @()
    }
}

# Main search execution
Write-Host "🚀 Starting comprehensive git history search..." -ForegroundColor Green
Write-Host "Search Term: '$SearchTerm'" -ForegroundColor White
Write-Host "File Pattern: '$FilePattern'" -ForegroundColor White
Write-Host "Branch: '$Branch'" -ForegroundColor White
Write-Host "Max Results: $MaxResults" -ForegroundColor White
Write-Host ""

# 1. Search commit messages
Write-Host "1️⃣  SEARCHING COMMIT MESSAGES" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Yellow
$commits = Search-GitLog -SearchTerm $SearchTerm -Branch $Branch -MaxResults $MaxResults
if ($commits) {
    Write-Host "Found $($commits.Count) commits:" -ForegroundColor Green
    $commits | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Host "No commits found matching '$SearchTerm'" -ForegroundColor Yellow
}
Write-Host ""

# 2. Search file changes
Write-Host "2️⃣  SEARCHING FILE CHANGES" -ForegroundColor Yellow
Write-Host "=============================" -ForegroundColor Yellow
$fileChanges = Search-GitFileChanges -SearchTerm $SearchTerm -FilePattern $FilePattern -Branch $Branch -MaxResults $MaxResults
if ($fileChanges) {
    Write-Host "Found file changes in commits:" -ForegroundColor Green
    $fileChanges | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Host "No file changes found matching '$SearchTerm'" -ForegroundColor Yellow
}
Write-Host ""

# 3. Search current file content
Write-Host "3️⃣  SEARCHING CURRENT FILE CONTENT" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
$currentFiles = Search-GitFileContent -SearchTerm $SearchTerm -FilePattern $FilePattern -Branch $Branch -MaxResults $MaxResults
if ($currentFiles) {
    Write-Host "Found $($currentFiles.Count) files currently containing '$SearchTerm':" -ForegroundColor Green
    $currentFiles | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Host "No files currently contain '$SearchTerm'" -ForegroundColor Yellow
}
Write-Host ""

# 4. Search across all branches
Write-Host "4️⃣  SEARCHING ACROSS ALL BRANCHES" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
$allBranchResults = Search-AllBranches -SearchTerm $SearchTerm -FilePattern $FilePattern
if ($allBranchResults) {
    Write-Host "Found results across branches:" -ForegroundColor Green
    $allBranchResults | ForEach-Object { Write-Host "  $_" -ForegroundColor White }
} else {
    Write-Host "No results found across branches" -ForegroundColor Yellow
}
Write-Host ""

# 5. Show detailed content if requested
if ($ShowContent -and $currentFiles) {
    Write-Host "5️⃣  DETAILED FILE CONTENT" -ForegroundColor Yellow
    Write-Host "===========================" -ForegroundColor Yellow
    
    foreach ($file in $currentFiles) {
        if (Test-Path $file) {
            Write-Host "📄 File: $file" -ForegroundColor Cyan
            Write-Host "Content containing '$SearchTerm':" -ForegroundColor White
            Write-Host "----------------------------------------" -ForegroundColor Gray
            
            try {
                $content = Get-Content $file -Raw
                $lines = $content -split "`n"
                
                for ($i = 0; $i -lt $lines.Count; $i++) {
                    if ($lines[$i] -match [regex]::Escape($SearchTerm)) {
                        $start = [math]::Max(0, $i - 2)
                        $end = [math]::Min($lines.Count - 1, $i + 2)
                        
                        for ($j = $start; $j -le $end; $j++) {
                            $prefix = if ($j -eq $i) { ">>> " } else { "    " }
                            $lineNum = $j + 1
                            Write-Host "$prefix$lineNum`: $($lines[$j])" -ForegroundColor $(if ($j -eq $i) { "Yellow" } else { "White" })
                        }
                        Write-Host ""
                    }
                }
            } catch {
                Write-Host "Error reading file: $($_.Exception.Message)" -ForegroundColor Red
            }
            Write-Host ""
        }
    }
}

# 6. GitHub web search suggestions
Write-Host "6️⃣  GITHUB WEB SEARCH SUGGESTIONS" -ForegroundColor Yellow
Write-Host "=====================================" -ForegroundColor Yellow
Write-Host "Since you mentioned GitHub web search limitations, here are manual search strategies:" -ForegroundColor Cyan
Write-Host ""
Write-Host "🔍 GitHub Web Search Tips:" -ForegroundColor White
Write-Host "  • Use 'filename:filename.cs' to search specific files" -ForegroundColor Gray
Write-Host "  • Use 'repo:username/repo' to limit search to your repository" -ForegroundColor Gray
Write-Host "  • Use 'path:folder/' to search specific directories" -ForegroundColor Gray
Write-Host "  • Use 'language:csharp' to limit to C# files" -ForegroundColor Gray
Write-Host ""
Write-Host "📋 Manual Search Commands:" -ForegroundColor White
Write-Host "  • Search commits: https://github.com/username/repo/commits?q=SearchTerm" -ForegroundColor Gray
Write-Host "  • Search code: https://github.com/username/repo/search?q=SearchTerm" -ForegroundColor Gray
Write-Host "  • Search issues: https://github.com/username/repo/issues?q=SearchTerm" -ForegroundColor Gray
Write-Host "  • Search PRs: https://github.com/username/repo/pulls?q=SearchTerm" -ForegroundColor Gray
Write-Host ""

# Summary
Write-Host "🎯 SEARCH SUMMARY" -ForegroundColor Green
Write-Host "=================" -ForegroundColor Green
Write-Host "Search completed for: '$SearchTerm'" -ForegroundColor White
Write-Host "Commits found: $($commits.Count)" -ForegroundColor White
Write-Host "Files currently containing term: $($currentFiles.Count)" -ForegroundColor White
Write-Host "File changes found: $($fileChanges.Count)" -ForegroundColor White
Write-Host ""

Write-Host "💡 TIP: Use this tool before creating PRs to find related changes and avoid conflicts!" -ForegroundColor Cyan
Write-Host "💡 TIP: Run with -ShowContent to see exactly where your search term appears in files!" -ForegroundColor Cyan