#!/bin/bash
# Deploy Tenant-Specific Container App Revisions
# This script creates separate revisions for each tenant with their own config and database

set -e

RESOURCE_GROUP="rg-masala-demo"
CONTAINER_APP="ca-ticket-masala"
IMAGE_TAG="${1:-latest}"  # Use provided tag or 'latest'
ACR="acrmasalademo.azurecr.io"
IMAGE_NAME="ticket-masala"

echo "🚀 Deploying tenant revisions for image: $ACR/$IMAGE_NAME:$IMAGE_TAG"

# Array of tenants
TENANTS=("desgoffe" "whitman" "liberty" "hennessey")

for TENANT in "${TENANTS[@]}"; do
    echo ""
    echo "📦 Creating revision for tenant: $TENANT"
    
    az containerapp revision copy \
        --name $CONTAINER_APP \
        --resource-group $RESOURCE_GROUP \
        --image "$ACR/$IMAGE_NAME:$IMAGE_TAG" \
        --revision-suffix "$TENANT" \
        --set-env-vars \
            MASALA_TENANT="$TENANT" \
            MASALA_CONFIG_PATH="/app/tenants/$TENANT/config" \
            MASALA_DB_PATH="/app/inputs/data/$TENANT.db"
    
    echo "✅ Revision created: $CONTAINER_APP--$TENANT"
done

echo ""
echo "🎉 All tenant revisions deployed successfully!"
echo ""
echo "📊 Traffic Distribution:"
az containerapp ingress traffic show \
    --name $CONTAINER_APP \
    --resource-group $RESOURCE_GROUP \
    --output table

echo ""
echo "💡 To set equal traffic distribution across all tenants:"
echo "az containerapp ingress traffic set \\"
echo "  --name $CONTAINER_APP \\"
echo "  --resource-group $RESOURCE_GROUP \\"
echo "  --revision-weight \\"
for TENANT in "${TENANTS[@]}"; do
    echo "    $CONTAINER_APP--$TENANT=25 \\"
done | sed '$ s/ \\$//'
