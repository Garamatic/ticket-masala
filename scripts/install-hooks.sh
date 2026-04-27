#!/bin/bash
# Install pre-commit hooks for TicketMasala

set -e

echo "🔧 Installing pre-commit hooks..."

# Ensure hooks directory exists
mkdir -p .git/hooks

# Create symlinks to our hook scripts
ln -sf ../../.husky/pre-commit .git/hooks/pre-commit
ln -sf ../../.husky/prepare-commit-msg .git/hooks/prepare-commit-msg

# Make hook scripts executable
chmod +x .husky/pre-commit
chmod +x .husky/prepare-commit-msg

echo "✅ Pre-commit hooks installed successfully!"
echo ""
echo "📋 What the pre-commit hook does:"
echo "   1. Checks code formatting (dotnet format)"
echo "   2. Builds the solution"
echo "   3. Runs all tests"
echo ""
echo "💡 To bypass hooks in an emergency: git commit --no-verify"
echo "💡 To fix formatting issues: dotnet format"
