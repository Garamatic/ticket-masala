#!/bin/bash

# Ticket Masala Multi-Tenant Deployment Script for Fly.io
# This script deploys all tenant instances to separate Fly.io apps

set -e

echo "🚀 Deploying Ticket Masala Multi-Tenant Architecture to Fly.io"
echo "================================================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to deploy a tenant
deploy_tenant() {
    local tenant_name=$1
    local config_file=$2
    
    echo -e "\n${BLUE}📦 Deploying ${tenant_name} tenant...${NC}"
    
    if [ ! -f "$config_file" ]; then
        echo -e "${RED}❌ Configuration file $config_file not found!${NC}"
        return 1
    fi
    
    # Get the app name from config file
    app_name=$(grep "^app = " "$config_file" | cut -d "'" -f 2)
    
    # Check if app exists, create if it doesn't
    if ! fly status --app "$app_name" &> /dev/null; then
        echo -e "${YELLOW}🏗️  Creating new app: ${app_name}${NC}"
        if ! fly apps create "$app_name" --org personal; then
            echo -e "${RED}❌ Failed to create app ${app_name}${NC}"
            return 1
        fi
    fi
    
    # Create volume based on tenant type
    case $tenant_name in
        "Default")
            create_volume "$app_name" "ticket_data"
            ;;
        "Government")
            create_volume "$app_name" "government_data"
            ;;
        "Healthcare")
            create_volume "$app_name" "healthcare_data"
            ;;
        "Helpdesk")
            create_volume "$app_name" "helpdesk_data"
            ;;
        "Landscaping")
            create_volume "$app_name" "landscaping_data"
            ;;
    esac
    
    # Deploy using the specific config file
    if fly deploy --config "$config_file"; then
        echo -e "${GREEN}✅ ${tenant_name} tenant deployed successfully!${NC}"
        echo -e "${GREEN}🌐 URL: https://${app_name}.fly.dev${NC}"
    else
        echo -e "${RED}❌ Failed to deploy ${tenant_name} tenant${NC}"
        return 1
    fi
}

# Check if flyctl is installed
if ! command -v fly &> /dev/null; then
    echo -e "${RED}❌ flyctl is not installed. Please install it first:${NC}"
    echo "   curl -L https://fly.io/install.sh | sh"
    exit 1
fi

# Check if user is logged in
if ! fly auth whoami &> /dev/null; then
    echo -e "${YELLOW}⚠️  You need to log in to Fly.io first:${NC}"
    echo "   fly auth login"
    exit 1
fi

# Function to create volume for a tenant
create_volume() {
    local app_name=$1
    local volume_name=$2
    
    echo -e "${BLUE}💾 Creating volume ${volume_name} for ${app_name}...${NC}"
    
    # Check if volume already exists
    if fly volumes list --app "$app_name" | grep -q "$volume_name"; then
        echo -e "${YELLOW}⚠️  Volume ${volume_name} already exists for ${app_name}${NC}"
    else
        if fly volumes create "$volume_name" --app "$app_name" --region ord --size 1; then
            echo -e "${GREEN}✅ Volume ${volume_name} created successfully${NC}"
        else
            echo -e "${RED}❌ Failed to create volume ${volume_name}${NC}"
            return 1
        fi
    fi
}

echo -e "${YELLOW}📋 Available tenants:${NC}"
echo "   1. Default (IT Helpdesk)"
echo "   2. Government Services"
echo "   3. Healthcare Clinic"
echo "   4. IT Helpdesk"
echo "   5. Landscaping Services"
echo ""

# Ask user which tenants to deploy
echo -e "${YELLOW}Which tenants would you like to deploy?${NC}"
echo "   a) All tenants"
echo "   d) Default only"
echo "   g) Government only"
echo "   h) Healthcare only"
echo "   k) Helpdesk only"
echo "   l) Landscaping only"
echo ""
read -p "Enter your choice (a/d/g/h/k/l): " choice

case $choice in
    a|A)
        echo -e "\n${BLUE}🚀 Deploying all tenants...${NC}"
        deploy_tenant "Default" "fly.toml"
        deploy_tenant "Government" "fly-government.toml"
        deploy_tenant "Healthcare" "fly-healthcare.toml"
        deploy_tenant "Helpdesk" "fly-helpdesk.toml"
        deploy_tenant "Landscaping" "fly-landscaping.toml"
        ;;
    d|D)
        deploy_tenant "Default" "fly.toml"
        ;;
    g|G)
        deploy_tenant "Government" "fly-government.toml"
        ;;
    h|H)
        deploy_tenant "Healthcare" "fly-healthcare.toml"
        ;;
    k|K)
        deploy_tenant "Helpdesk" "fly-helpdesk.toml"
        ;;
    l|L)
        deploy_tenant "Landscaping" "fly-landscaping.toml"
        ;;
    *)
        echo -e "${RED}❌ Invalid choice. Exiting.${NC}"
        exit 1
        ;;
esac

echo -e "\n${GREEN}🎉 Deployment completed!${NC}"
echo -e "\n${BLUE}📝 Next steps:${NC}"
echo "   1. Create volumes for persistent data:"
echo "      fly volumes create <volume_name> --region ord --size 1"
echo ""
echo "   2. Set up custom domains (optional):"
echo "      fly certs add <your-domain.com>"
echo ""
echo "   3. Monitor your apps:"
echo "      fly status"
echo "      fly logs"
echo ""
echo -e "${YELLOW}💡 Tip: Each tenant runs as a separate Fly.io app with isolated data and configuration.${NC}"