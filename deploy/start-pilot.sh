#!/bin/bash

# 1. Define Paths
# Using user home directory dynamically
DB_PATH="$HOME/masala_data/pilot.db"
# Ensure the directory exists
mkdir -p "$HOME/masala_data"

APP_DIR="./deploy/linux-pilot"
APP_PATH="$APP_DIR/TicketMasala.Web"

# 2. Safety Check: Clean State?
if [ "$1" == "--reset" ]; then
    if [ -f "$DB_PATH" ]; then
        rm "$DB_PATH"
        echo "⚠️ Database wiped."
    fi
    
    # Check if seed file exists before trying to seed
    SEED_FILE="$HOME/downloads/huge_backlog.csv"
    if [ -f "$SEED_FILE" ]; then
         echo "Re-seeding from $SEED_FILE..."
        "$APP_PATH" --seed "$SEED_FILE" --db "$DB_PATH"
    else
        echo "ℹ️ No seed file found at $SEED_FILE. Starting with empty/default DB."
    fi
fi

# 3. Tuning Linux for SQLite
# Increase max open files (soft limit)
ulimit -n 65535 2>/dev/null || echo "Note: Could not set ulimit (requires permission), proceeding with defaults."

# 4. Launch the Engine
echo "🚀 Launching Ticket Masala Pilot on Port 5000..."

# Kestrel Configuration for High Performance
export Kestrel__Endpoints__Http__Url="http://localhost:5000"
export ConnectionStrings__DefaultConnection="Data Source=$DB_PATH;Cache=Shared"
# FORCE SQLite Provider (Critical fix)
export DatabaseProvider="Sqlite"
# Enable Development Mode to see errors
export ASPNETCORE_ENVIRONMENT="Development"

# Ensure Config Files are present for Single-File App
echo "🔧 Configuring Environment..."
cp src/TicketMasala.Web/masala_config.json "$APP_DIR/" 2>/dev/null || echo "⚠️ Warning: masala_config.json not found in src"
cp src/TicketMasala.Web/masala_domains.yaml "$APP_DIR/" 2>/dev/null || echo "⚠️ Warning: masala_domains.yaml not found in src"

# Ensure executable permission
chmod +x "$APP_PATH" 2>/dev/null

echo "Starting application from $APP_DIR..."
# Change directory to app dir so ContentRootPath is correct for relative file access if needed
cd "$APP_DIR"
./TicketMasala.Web
