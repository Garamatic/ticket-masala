#!/bin/bash
set -e

echo "📊 Running tests with coverage..."

# Restore tools
dotnet tool restore

# Clean up old results
rm -rf ./TestResults

# Run tests with coverage collection
dotnet test src/TicketMasala.Tests/TicketMasala.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings src/TicketMasala.Tests/coverlet.runsettings \
  --results-directory ./TestResults \
  --verbosity minimal

# Find the coverage file
COVERAGE_FILE=$(find ./TestResults -name "coverage.cobertura.xml" | head -1)

if [ -f "$COVERAGE_FILE" ]; then
    echo "📈 Generating coverage report..."
    dotnet reportgenerator \
        -reports:"$COVERAGE_FILE" \
        -targetdir:"./TestResults/CoverageReport" \
        -reporttypes:"Html;MarkdownSummary"
    
    echo ""
    echo "✅ Coverage report generated!"
    echo "📁 HTML Report: ./TestResults/CoverageReport/index.html"
    echo "📄 Markdown Summary: ./TestResults/CoverageReport/Summary.md"
else
    echo "❌ Coverage file not found"
    exit 1
fi
