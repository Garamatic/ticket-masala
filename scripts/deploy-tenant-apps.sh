#!/bin/bash
# Deploy Separate Container Apps for Each Tenant
# This creates 4 independent Container Apps, each with its own database and config

set -e

RESOURCE_GROUP="rg-masala-demo"
IMAGE_TAG="${1:-latest}"
ACR="acrmasalademo.azurecr.io"
IMAGE_NAME="ticket-masala"
LOCATION="westeurope"

echo "🚀 Deploying separate Container Apps for each tenant"
echo "📦 Using image: $ACR/$IMAGE_NAME:$IMAGE_TAG"
echo ""

# Array of tenants
TENANTS=("desgoffe" "whitman" "liberty" "hennessey")

for TENANT in "${TENANTS[@]}"; do
    APP_NAME="ca-ticket-masala-$TENANT"
    
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "📦 Deploying Container App: $APP_NAME"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    
    # ** NEW: Get ACR Credentials **
    ACR_USERNAME=$(az acr credential show --name "acrmasalademo" --query "username" --output tsv)
    ACR_PASSWORD=$(az acr credential show --name "acrmasalademo" --query "passwords[0].value" --output tsv)

    # Check if Container App exists
    if az containerapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
        echo "✅ Container App exists, updating..."
        
        az containerapp update \
            --name "$APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --image "$ACR/$IMAGE_NAME:$IMAGE_TAG" \
            --registry-server "$ACR" \
            --registry-username "$ACR_USERNAME" \
            --registry-password "$ACR_PASSWORD" \
            --set-env-vars \
                MASALA_TENANT="$TENANT" \
                MASALA_CONFIG_PATH="/app/tenants/$TENANT/config" \
                MASALA_DB_PATH="/app/inputs/data/$TENANT.db"
    else
        echo "🆕 Creating new Container App..."
        
        az containerapp create \
            --name "$APP_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --image "$ACR/$IMAGE_NAME:$IMAGE_TAG" \
            --registry-server "$ACR" \
            --registry-username "$ACR_USERNAME" \
            --registry-password "$ACR_PASSWORD" \
            --environment "cae-masala-demo" \
            --target-port 8080 \
            --ingress external \
            --min-replicas 0 \
            --max-replicas 1 \
            --cpu 0.5 \
            --memory 1.0Gi \
            --env-vars \
                MASALA_TENANT="$TENANT" \
                MASALA_CONFIG_PATH="/app/tenants/$TENANT/config" \
                MASALA_DB_PATH="/app/inputs/data/$TENANT.db"
    fi
    
    # Get the URL
    URL=$(az containerapp show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.configuration.ingress.fqdn" \
        --output tsv)
    
    echo "✅ Deployed: https://$URL"
    echo ""
done

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 All tenant Container Apps deployed!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📋 Container App URLs:"
for TENANT in "${TENANTS[@]}"; do
    APP_NAME="ca-ticket-masala-$TENANT"
    URL=$(az containerapp show \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --query "properties.configuration.ingress.fqdn" \
        --output tsv 2>/dev/null || echo "Not found")
    echo "  $TENANT: https://$URL"
done
echo ""
echo "💡 Update your frontend portals to point to these URLs"
