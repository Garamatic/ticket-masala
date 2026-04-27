#!/bin/bash

# Check coverage against thresholds without failing the build
# Usage: ./scripts/check-coverage.sh [threshold_percentage]

THRESHOLD=${1:-50}

echo "📊 Checking coverage against ${THRESHOLD}% threshold..."

# Find coverage summary if it exists
SUMMARY_FILE="./TestResults/CoverageReport/Summary.md"

if [ ! -f "$SUMMARY_FILE" ]; then
    echo "⚠️  No coverage report found. Run ./scripts/test-coverage.sh first."
    exit 0
fi

# Extract line coverage percentage
COVERAGE=$(grep -oP '(?<=\*\*Line coverage:\*\* )[0-9.]+' "$SUMMARY_FILE" 2>/dev/null || echo "0")

echo "Current line coverage: ${COVERAGE}%"
echo "Target threshold: ${THRESHOLD}%"

# Compare using bc for float comparison
if (( $(echo "$COVERAGE >= $THRESHOLD" | bc -l 2>/dev/null || echo "0") )); then
    echo "✅ Coverage meets threshold!"
else
    echo "⚠️  Coverage is below threshold. Consider adding more tests."
    echo "   Note: Threshold checking is informational only - not blocking builds."
fi
