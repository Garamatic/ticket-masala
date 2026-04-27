#!/bin/bash
set -e

echo "🧪 Running Ticket Masala Tests..."

# Run all tests with normal verbosity
dotnet test --verbosity normal --no-restore "$@"

echo "✅ All tests passed!"
