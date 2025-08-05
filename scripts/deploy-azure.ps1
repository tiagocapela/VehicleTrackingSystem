# scripts/deploy-azure.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location = "East US",
    
    [Parameter(Mandatory=$true)]
    [string]$SqlAdminUsername,
    
    [Parameter(Mandatory=$true)]
    [SecureString]$SqlAdminPassword,
    
    [string]$WebAppName = "vehicle-tracking-$(Get-Random -Maximum 9999)",
    [string]$TcpServiceName = "gps-tcp-service-$(Get-Random -Maximum 9999)"
)

Write-Host "Starting Azure deployment..." -ForegroundColor Green

# Login to Azure
Write-Host "Logging in to Azure..." -ForegroundColor Yellow
az login

# Create resource group
Write-Host "Creating resource group: $ResourceGroupName" -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location

# Convert SecureString to plain text for deployment
$SqlAdminPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword))

# Deploy ARM template
Write-Host "Deploying ARM template..." -ForegroundColor Yellow
$deploymentResult = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "../azuredeploy.json" `
    --parameters `
        webAppName=$WebAppName `
        tcpServiceName=$TcpServiceName `
        sqlAdministratorLogin=$SqlAdminUsername `
        sqlAdministratorPassword=$SqlAdminPasswordPlain `
    --output json | ConvertFrom-Json

if ($deploymentResult.properties.provisioningState -eq "Succeeded") {
    Write-Host "ARM template deployment completed successfully!" -ForegroundColor Green
    
    $webAppUrl = $deploymentResult.properties.outputs.webAppUrl.value
    $tcpServiceUrl = $deploymentResult.properties.outputs.tcpServiceUrl.value
    
    Write-Host "Web App URL: $webAppUrl" -ForegroundColor Cyan
    Write-Host "TCP Service URL: $tcpServiceUrl" -ForegroundColor Cyan
} else {
    Write-Error "ARM template deployment failed!"
    exit 1
}

# Build and publish applications
Write-Host "Building and publishing applications..." -ForegroundColor Yellow

# Build MVC Application
Write-Host "Building MVC Application..." -ForegroundColor Yellow
Set-Location "../src/VehicleTracking.Web"
dotnet publish -c Release -o "./publish"

# Create deployment package
Compress-Archive -Path "./publish/*" -DestinationPath "./mvc-app.zip" -Force

# Deploy MVC Application
Write-Host "Deploying MVC Application..." -ForegroundColor Yellow
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $WebAppName --src "./mvc-app.zip"

# Build TCP Service
Write-Host "Building TCP Service..." -ForegroundColor Yellow
Set-Location "../TcpGpsService"
dotnet publish -c Release -o "./publish"

# Create deployment package
Compress-Archive -Path "./publish/*" -DestinationPath "./tcp-service.zip" -Force

# Deploy TCP Service
Write-Host "Deploying TCP Service..." -ForegroundColor Yellow
az webapp deployment source config-zip --resource-group $ResourceGroupName --name $TcpServiceName --src "./tcp-service.zip"

# Run database migrations
Write-Host "Running database migrations..." -ForegroundColor Yellow

# Get connection string
$sqlServerName = az sql server list --resource-group $ResourceGroupName --query "[0].name" -o tsv
$connectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=VehicleTrackingDb;Persist Security Info=False;User ID=$SqlAdminUsername;Password=$SqlAdminPasswordPlain;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Update connection string and run migrations for MVC app
$env:ConnectionStrings__DefaultConnection = $connectionString
dotnet ef database update --project "../VehicleTracking.Web"

# Update connection string for TCP service
$tcpConnectionString = "Server=tcp:$sqlServerName.database.windows.net,1433;Initial Catalog=GpsTrackingDb;Persist Security Info=False;User ID=$SqlAdminUsername;Password=$SqlAdminPasswordPlain;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$env:ConnectionStrings__DefaultConnection = $tcpConnectionString
dotnet ef database update --project "../TcpGpsService"

Write-Host "Deployment completed successfully!" -ForegroundColor Green
Write-Host "Web Application: $webAppUrl" -ForegroundColor Cyan
Write-Host "TCP Service: $tcpServiceUrl" -ForegroundColor Cyan
Write-Host "Configure your MV730 devices to connect to: $tcpServiceName.azurewebsites.net:8888" -ForegroundColor Yellow

# Clean up
Remove-Item "./mvc-app.zip" -Force -ErrorAction SilentlyContinue
Remove-Item "../VehicleTracking.Web/mvc-app.zip" -Force -ErrorAction SilentlyContinue
Remove-Item "./tcp-service.zip" -Force -ErrorAction SilentlyContinue

