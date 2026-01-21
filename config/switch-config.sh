#!/bin/bash

# Ticket Masala Config Switcher
# Easily switch between Azure and Localhost configurations

set -e

CONFIG_DIR="/home/juan/Projects/garamatic/ticket-masala/config"
cd "$CONFIG_DIR"

show_help() {
    cat << EOF
Ticket Masala Config Switcher

Usage: ./switch-config.sh [localhost|azure|status]

Commands:
    localhost   Switch to localhost demo configuration
    azure       Switch back to Azure configuration
    status      Show current configuration

Examples:
    ./switch-config.sh localhost
    ./switch-config.sh azure
    ./switch-config.sh status
EOF
}

show_status() {
    echo "📊 Current Configuration Status"
    echo "================================"
    echo ""
    
    if [ -f "seed_data.json" ]; then
        echo "✅ seed_data.json exists"
        if grep -q "localhost.dev" seed_data.json 2>/dev/null; then
            echo "   → Currently using: LOCALHOST config"
        elif grep -q "desgoffe.gov" seed_data.json 2>/dev/null; then
            echo "   → Currently using: AZURE config (Desgoffe)"
        else
            echo "   → Currently using: UNKNOWN config"
        fi
    else
        echo "❌ seed_data.json NOT FOUND"
    fi
    
    echo ""
    
    if [ -f "masala_domains.yaml" ]; then
        echo "✅ masala_domains.yaml exists"
        if grep -q "Support" masala_domains.yaml 2>/dev/null; then
            echo "   → Currently using: LOCALHOST config"
        elif grep -q "Gardening" masala_domains.yaml 2>/dev/null; then
            echo "   → Currently using: AZURE config"
        else
            echo "   → Currently using: UNKNOWN config"
        fi
    else
        echo "❌ masala_domains.yaml NOT FOUND"
    fi
    
    echo ""
    echo "📁 Available Configs:"
    [ -f "seed_data.localhost.json" ] && echo "   ✅ seed_data.localhost.json" || echo "   ❌ seed_data.localhost.json"
    [ -f "masala_domains.localhost.yaml" ] && echo "   ✅ masala_domains.localhost.yaml" || echo "   ❌ masala_domains.localhost.yaml"
    [ -f "seed_data.azure.backup.json" ] && echo "   ✅ seed_data.azure.backup.json" || echo "   ⚠️  seed_data.azure.backup.json (not backed up yet)"
    [ -f "masala_domains.azure.backup.yaml" ] && echo "   ✅ masala_domains.azure.backup.yaml" || echo "   ⚠️  masala_domains.azure.backup.yaml (not backed up yet)"
}

switch_to_localhost() {
    echo "🔄 Switching to LOCALHOST configuration..."
    echo ""
    
    # Backup Azure configs if not already backed up
    if [ ! -f "seed_data.azure.backup.json" ] && [ -f "seed_data.json" ]; then
        echo "📦 Backing up current seed_data.json → seed_data.azure.backup.json"
        cp seed_data.json seed_data.azure.backup.json
    fi
    
    if [ ! -f "masala_domains.azure.backup.yaml" ] && [ -f "masala_domains.yaml" ]; then
        echo "📦 Backing up current masala_domains.yaml → masala_domains.azure.backup.yaml"
        cp masala_domains.yaml masala_domains.azure.backup.yaml
    fi
    
    # Check if localhost configs exist
    if [ ! -f "seed_data.localhost.json" ]; then
        echo "❌ ERROR: seed_data.localhost.json not found!"
        exit 1
    fi
    
    if [ ! -f "masala_domains.localhost.yaml" ]; then
        echo "❌ ERROR: masala_domains.localhost.yaml not found!"
        exit 1
    fi
    
    # Copy localhost configs
    echo "✅ Copying seed_data.localhost.json → seed_data.json"
    cp seed_data.localhost.json seed_data.json
    
    echo "✅ Copying masala_domains.localhost.yaml → masala_domains.yaml"
    cp masala_domains.localhost.yaml masala_domains.yaml
    
    echo ""
    echo "✨ Successfully switched to LOCALHOST configuration!"
    echo ""
    echo "🚀 Next steps:"
    echo "   1. Restart your application: cd ../src/TicketMasala.Web && dotnet run"
    echo "   2. Access at: http://localhost:5000"
    echo "   3. Login with: admin@localhost.dev or any demo user"
}

switch_to_azure() {
    echo "🔄 Switching to AZURE configuration..."
    echo ""
    
    # Check if Azure backups exist
    if [ ! -f "seed_data.azure.backup.json" ]; then
        echo "❌ ERROR: seed_data.azure.backup.json not found!"
        echo "   Cannot restore Azure config without backup."
        exit 1
    fi
    
    if [ ! -f "masala_domains.azure.backup.yaml" ]; then
        echo "❌ ERROR: masala_domains.azure.backup.yaml not found!"
        echo "   Cannot restore Azure config without backup."
        exit 1
    fi
    
    # Restore Azure configs
    echo "✅ Restoring seed_data.azure.backup.json → seed_data.json"
    cp seed_data.azure.backup.json seed_data.json
    
    echo "✅ Restoring masala_domains.azure.backup.yaml → masala_domains.yaml"
    cp masala_domains.azure.backup.yaml masala_domains.yaml
    
    echo ""
    echo "✨ Successfully switched to AZURE configuration!"
    echo ""
    echo "🚀 Next steps:"
    echo "   1. Restart your application"
    echo "   2. Deploy to Azure if needed"
}

# Main script logic
case "${1:-}" in
    localhost)
        switch_to_localhost
        ;;
    azure)
        switch_to_azure
        ;;
    status)
        show_status
        ;;
    -h|--help|help)
        show_help
        ;;
    "")
        show_help
        ;;
    *)
        echo "❌ Unknown command: $1"
        echo ""
        show_help
        exit 1
        ;;
esac
