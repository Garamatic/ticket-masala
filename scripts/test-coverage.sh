#!/bin/bash

echo "📊 Running tests with coverage..."

# Restore tools
dotnet tool restore

# Clean up old results
rm -rf ./TestResults

# Run Domain tests with coverage (continue even if tests fail)
echo "🧪 Running Domain tests..."
dotnet test src/TicketMasala.Domain.Tests/TicketMasala.Domain.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings src/TicketMasala.Tests/coverlet.runsettings \
  --results-directory ./TestResults \
  --verbosity minimal || true

# Run Web tests with coverage (continue even if tests fail)
echo "🧪 Running Web tests..."
dotnet test src/TicketMasala.Tests/TicketMasala.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --settings src/TicketMasala.Tests/coverlet.runsettings \
  --results-directory ./TestResults \
  --verbosity minimal || true

# Find all coverage files
COVERAGE_FILES=$(find ./TestResults -name "coverage.cobertura.xml" 2>/dev/null | tr '\n' ';')

if [ -n "$COVERAGE_FILES" ]; then
    echo "📈 Generating coverage report..."
    dotnet reportgenerator \
        -reports:"$COVERAGE_FILES" \
        -targetdir:"./TestResults/CoverageReport" \
        -reporttypes:"Html;MarkdownSummary"

    echo ""
    echo "✅ Coverage report generated!"
    echo "📁 HTML Report: ./TestResults/CoverageReport/index.html"
    echo "📄 Markdown Summary: ./TestResults/CoverageReport/Summary.md"
else
    echo "❌ No coverage files found"
    exit 1
fi
