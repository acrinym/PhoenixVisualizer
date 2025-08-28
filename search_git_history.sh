#!/bin/bash

# Phoenix Visualizer Git History Search Tool (Bash Version)
# This tool helps you search through git history to find files across different PRs and branches
# 
# Usage: Run this script from the PhoenixVisualizer project root directory
# Requirements: Git must be installed and accessible

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
NC='\033[0m' # No Color

# Default values
SEARCH_TERM=""
FILE_PATTERN="*"
BRANCH="main"
MAX_RESULTS=50
SHOW_CONTENT=false
VERBOSE=false

# Help function
show_help() {
    echo -e "${CYAN}Phoenix Visualizer Git History Search Tool (Bash Version)${NC}"
    echo "================================================================"
    echo ""
    echo "This tool helps you search through git history to find files across different PRs and branches."
    echo ""
    echo "USAGE:"
    echo "    ./search_git_history.sh \"filename.cs\""
    echo "    ./search_git_history.sh \"class name\" \"*.cs\""
    echo "    ./search_git_history.sh \"function name\" \"*\" 50 true"
    echo ""
    echo "PARAMETERS:"
    echo "    \$1 - Search term (required)"
    echo "    \$2 - File pattern (default: \"*\")"
    echo "    \$3 - Max results (default: 50)"
    echo "    \$4 - Show content (default: false, use \"true\" to enable)"
    echo ""
    echo "EXAMPLES:"
    echo "    # Find all files containing \"AvsPresetInfo\""
    echo "    ./search_git_history.sh \"AvsPresetInfo\""
    echo ""
    echo "    # Search for \"NsEelEvaluator\" in C# files only"
    echo "    ./search_git_history.sh \"NsEelEvaluator\" \"*.cs\""
    echo ""
    echo "    # Find commits that mention \"circular dependency\" with content"
    echo "    ./search_git_history.sh \"circular dependency\" \"*\" 100 true"
    echo ""
}

# Check if git is available
check_git() {
    if ! command -v git &> /dev/null; then
        echo -e "${RED}❌ Git is not available. Please install Git and ensure it's in your PATH.${NC}"
        echo -e "${YELLOW}Download from: https://git-scm.com/downloads${NC}"
        exit 1
    fi
    
    GIT_VERSION=$(git --version 2>&1)
    echo -e "${GREEN}✅ Git found: $GIT_VERSION${NC}"
}

# Search git log for commits
search_git_log() {
    local search_term="$1"
    local branch="$2"
    local max_results="$3"
    
    echo -e "${CYAN}🔍 Searching git log for commits containing '$search_term'...${NC}"
    
    local log_command="git log --all --grep=\"$search_term\" --oneline --max-count=$max_results"
    if [ "$branch" != "main" ]; then
        log_command="git log $branch --grep=\"$search_term\" --oneline --max-count=$max_results"
    fi
    
    local commits
    commits=$(eval $log_command 2>&1)
    
    if [ -n "$commits" ]; then
        echo -e "${GREEN}Found commits:${NC}"
        echo "$commits" | while IFS= read -r line; do
            echo -e "  ${WHITE}$line${NC}"
        done
    else
        echo -e "${YELLOW}No commits found matching '$search_term'${NC}"
    fi
}

# Search file content in git history
search_git_file_content() {
    local search_term="$1"
    local file_pattern="$2"
    local branch="$3"
    
    echo -e "${CYAN}🔍 Searching file content for '$search_term' in '$file_pattern' files...${NC}"
    
    local grep_command="git grep -l --all-match \"$search_term\" -- \"$file_pattern\""
    if [ "$branch" != "main" ]; then
        grep_command="git grep -l --all-match \"$search_term\" $branch -- \"$file_pattern\""
    fi
    
    local files
    files=$(eval $grep_command 2>&1)
    
    if [ -n "$files" ]; then
        local file_count=$(echo "$files" | wc -l)
        echo -e "${GREEN}Found $file_count files currently containing '$search_term':${NC}"
        echo "$files" | while IFS= read -r line; do
            echo -e "  ${WHITE}$line${NC}"
        done
    else
        echo -e "${YELLOW}No files currently contain '$search_term'${NC}"
    fi
    
    echo "$files"
}

# Search across all branches
search_all_branches() {
    local search_term="$1"
    local file_pattern="$2"
    
    echo -e "${CYAN}🌿 Searching across all branches...${NC}"
    
    local branches
    branches=$(git branch -a | sed 's/^[ *]*//')
    
    local found_results=false
    
    while IFS= read -r branch; do
        local branch_name=$(echo "$branch" | sed 's/remotes\/origin\///' | sed 's/remotes\///' | sed 's/origin\///')
        echo -e "  ${GRAY}Searching branch: $branch_name${NC}"
        
        local branch_results
        branch_results=$(git log "$branch_name" --name-only --grep="$search_term" --max-count=10 2>&1)
        
        if [ -n "$branch_results" ]; then
            found_results=true
            echo -e "${GREEN}BRANCH: $branch_name${NC}"
            echo "$branch_results"
            echo "---"
        fi
    done <<< "$branches"
    
    if [ "$found_results" = false ]; then
        echo -e "${YELLOW}No results found across branches${NC}"
    fi
}

# Show detailed content
show_detailed_content() {
    local search_term="$1"
    local files="$2"
    
    if [ "$SHOW_CONTENT" = false ] || [ -z "$files" ]; then
        return
    fi
    
    echo -e "${YELLOW}5️⃣  DETAILED FILE CONTENT${NC}"
    echo -e "${YELLOW}===========================${NC}"
    
    echo "$files" | while IFS= read -r file; do
        if [ -f "$file" ]; then
            echo -e "${CYAN}📄 File: $file${NC}"
            echo -e "${WHITE}Content containing '$search_term':${NC}"
            echo -e "${GRAY}----------------------------------------${NC}"
            
            local line_number=0
            while IFS= read -r line; do
                ((line_number++))
                if echo "$line" | grep -q "$search_term"; then
                    # Show context (2 lines before and after)
                    local start=$((line_number - 2))
                    local end=$((line_number + 2))
                    
                    # Get the context lines
                    local context_lines
                    context_lines=$(sed -n "${start},${end}p" "$file" 2>/dev/null)
                    
                    echo "$context_lines" | while IFS= read -r context_line; do
                        local current_line=$((start++))
                        if [ "$current_line" -eq "$line_number" ]; then
                            echo -e "${YELLOW}>>> $current_line: $context_line${NC}"
                        else
                            echo -e "${WHITE}    $current_line: $context_line${NC}"
                        fi
                    done
                    echo ""
                fi
            done < "$file"
        fi
    done
}

# GitHub web search suggestions
show_github_tips() {
    echo -e "${YELLOW}6️⃣  GITHUB WEB SEARCH SUGGESTIONS${NC}"
    echo -e "${YELLOW}=====================================${NC}"
    echo -e "${CYAN}Since you mentioned GitHub web search limitations, here are manual search strategies:${NC}"
    echo ""
    echo -e "${WHITE}🔍 GitHub Web Search Tips:${NC}"
    echo -e "${GRAY}  • Use 'filename:filename.cs' to search specific files${NC}"
    echo -e "${GRAY}  • Use 'repo:username/repo' to limit search to your repository${NC}"
    echo -e "${GRAY}  • Use 'path:folder/' to search specific directories${NC}"
    echo -e "${GRAY}  • Use 'language:csharp' to limit to C# files${NC}"
    echo ""
    echo -e "${WHITE}📋 Manual Search Commands:${NC}"
    echo -e "${GRAY}  • Search commits: https://github.com/username/repo/commits?q=SearchTerm${NC}"
    echo -e "${GRAY}  • Search code: https://github.com/username/repo/search?q=SearchTerm${NC}"
    echo -e "${GRAY}  • Search issues: https://github.com/username/repo/issues?q=SearchTerm${NC}"
    echo -e "${GRAY}  • Search PRs: https://github.com/username/repo/pulls?q=SearchTerm${NC}"
    echo ""
}

# Main execution
main() {
    # Check parameters
    if [ $# -eq 0 ]; then
        show_help
        exit 1
    fi
    
    if [ "$1" = "-h" ] || [ "$1" = "--help" ] || [ "$1" = "help" ]; then
        show_help
        exit 0
    fi
    
    SEARCH_TERM="$1"
    [ -n "$2" ] && FILE_PATTERN="$2"
    [ -n "$3" ] && MAX_RESULTS="$3"
    [ -n "$4" ] && [ "$4" = "true" ] && SHOW_CONTENT=true
    
    echo -e "${GREEN}🚀 Starting comprehensive git history search...${NC}"
    echo -e "${WHITE}Search Term: '$SEARCH_TERM'${NC}"
    echo -e "${WHITE}File Pattern: '$FILE_PATTERN'${NC}"
    echo -e "${WHITE}Branch: '$BRANCH'${NC}"
    echo -e "${WHITE}Max Results: $MAX_RESULTS${NC}"
    echo -e "${WHITE}Show Content: $SHOW_CONTENT${NC}"
    echo ""
    
    # Check git availability
    check_git
    
    # 1. Search commit messages
    echo -e "${YELLOW}1️⃣  SEARCHING COMMIT MESSAGES${NC}"
    echo -e "${YELLOW}================================${NC}"
    search_git_log "$SEARCH_TERM" "$BRANCH" "$MAX_RESULTS"
    echo ""
    
    # 2. Search current file content
    echo -e "${YELLOW}2️⃣  SEARCHING CURRENT FILE CONTENT${NC}"
    echo -e "${YELLOW}=====================================${NC}"
    local found_files
    found_files=$(search_git_file_content "$SEARCH_TERM" "$FILE_PATTERN" "$BRANCH")
    echo ""
    
    # 3. Search across all branches
    echo -e "${YELLOW}3️⃣  SEARCHING ACROSS ALL BRANCHES${NC}"
    echo -e "${YELLOW}=====================================${NC}"
    search_all_branches "$SEARCH_TERM" "$FILE_PATTERN"
    echo ""
    
    # 4. Show detailed content if requested
    show_detailed_content "$SEARCH_TERM" "$found_files"
    echo ""
    
    # 5. GitHub web search suggestions
    show_github_tips
    
    # Summary
    echo -e "${GREEN}🎯 SEARCH SUMMARY${NC}"
    echo -e "${GREEN}=================${NC}"
    echo -e "${WHITE}Search completed for: '$SEARCH_TERM'${NC}"
    echo ""
    
    echo -e "${CYAN}💡 TIP: Use this tool before creating PRs to find related changes and avoid conflicts!${NC}"
    echo -e "${CYAN}💡 TIP: Run with 'true' as the last parameter to see exactly where your search term appears!${NC}"
}

# Run main function with all arguments
main "$@"