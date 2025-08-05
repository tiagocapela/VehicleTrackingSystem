# scripts/build-and-deploy.sh
#!/bin/bash

set -e

# Configuration
RESOURCE_GROUP=""
LOCATION="East US"
WEB_APP_NAME=""
TCP_SERVICE_NAME=""
SQL_ADMIN_USERNAME=""
SQL_ADMIN_PASSWORD=""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting deployment process...${NC}"

# Check if required variables are set
if [ -z "$RESOURCE_GROUP" ] || [ -z "$WEB_APP_NAME" ] || [ -z "$TCP_SERVICE_NAME" ] || [ -z "$SQL_ADMIN_USERNAME" ] || [ -z "$SQL_ADMIN_PASSWORD" ]; then
    echo -e "${RED}Please configure the required variables at the top of this script${NC}"
    exit 1
fi

# Login to Azure
echo -e "${YELLOW}Logging in to Azure...${NC}"
az login

# Build applications
echo -e "${YELLOW}Building applications...${NC}"

# Build MVC Application
echo -e "${CYAN}Building MVC Application...${NC}"
cd ../src/VehicleTracking.Web
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish

# Create deployment package
zip -r mvc-app.zip ./publish/*

# Build TCP Service
echo -e "${CYAN}Building TCP Service...${NC}"
cd ../TcpGpsService
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o ./publish

# Create deployment package
zip -r tcp-service.zip ./publish/*

# Deploy to Azure
echo -e "${YELLOW}Deploying to Azure...${NC}"

# Deploy MVC Application
echo -e "${CYAN}Deploying MVC Application...${NC}"
az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$WEB_APP_NAME" \
    --src "./mvc-app.zip"

# Deploy TCP Service
echo -e "${CYAN}Deploying TCP Service...${NC}"
az webapp deployment source config-zip \
    --resource-group "$RESOURCE_GROUP" \
    --name "$TCP_SERVICE_NAME" \
    --src "./tcp-service.zip"

# Clean up
rm -f mvc-app.zip tcp-service.zip
rm -rf ../VehicleTracking.Web/publish
rm -rf ./publish

echo -e "${GREEN}Deployment completed successfully!${NC}"
echo -e "${CYAN}Web Application: https://$WEB_APP_NAME.azurewebsites.net${NC}"
echo -e "${CYAN}TCP Service: https://$TCP_SERVICE_NAME.azurewebsites.net${NC}"
echo -e "${YELLOW}Configure your MV730 devices to connect to: $TCP_SERVICE_NAME.azurewebsites.net:8888${NC}"

