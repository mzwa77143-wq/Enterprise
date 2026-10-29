<#
.SYNOPSIS
    Deploys Azure Service Bus to Azure Subscription 8419b0f2-ff8e-4fe0-924e-47223fdea478 ("Subscription 1") using Bicep.

.DESCRIPTION
    Provisions a Resource Group and Azure Service Bus Namespace with the 'enterprise-messages' queue,
    configured with Peek-Lock and Dead-Letter handling matching the EnterpriseSolution architecture.

.PARAMETER SubscriptionId
    Target Subscription ID. Defaults to 8419b0f2-ff8e-4fe0-924e-47223fdea478.

.PARAMETER ResourceGroupName
    Name of the Resource Group to create/use. Defaults to 'rg-enterprise-messaging-prod'.

.PARAMETER Location
    Azure region for deployment. Defaults to 'eastus'.

.PARAMETER UpdateAppSettings
    Switch to automatically update src/EnterpriseApp.Api/appsettings.json with the generated connection string.

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -UpdateAppSettings
#>

[CmdletBinding()]
param(
    [string]$SubscriptionId = "8419b0f2-ff8e-4fe0-924e-47223fdea478",
    [string]$ResourceGroupName = "rg-enterprise-messaging-prod",
    [string]$Location = "eastus",
    [string]$QueueName = "enterprise-messages",
    [switch]$UpdateAppSettings
)

$ErrorActionPreference = "Stop"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Azure Service Bus Bicep Deployment" -ForegroundColor Cyan
Write-Host " Subscription: $SubscriptionId ('Subscription 1')" -ForegroundColor Cyan
Write-Host " Resource Group: $ResourceGroupName" -ForegroundColor Cyan
Write-Host " Region: $Location" -ForegroundColor Cyan
Write-Host " Queue: $QueueName" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# Check for Azure CLI
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error @"
Azure CLI ('az') is not installed or not in PATH.
Please install the Azure CLI from https://aka.ms/installazurecliwindows or run:
  winget install Microsoft.AzureCLI
Then restart your shell and re-run this script.
"@
    exit 1
}

# 1. Login check
Write-Host "`n[1/4] Verifying Azure Authentication..." -ForegroundColor Yellow
$account = az account show --output json 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not currently logged in. Opening Azure login..." -ForegroundColor Yellow
    az login --output none
    $account = az account show --output json | ConvertFrom-Json
}

# 2. Set Target Subscription
Write-Host "`n[2/4] Setting active subscription to $SubscriptionId..." -ForegroundColor Yellow
az account set --subscription $SubscriptionId
$currentSub = az account show --output json | ConvertFrom-Json
Write-Host "Active Subscription: $($currentSub.name) ($($currentSub.id))" -ForegroundColor Green

# 3. Execute Bicep Deployment
$bicepPath = Join-Path $PSScriptRoot "main.bicep"
$paramPath = Join-Path $PSScriptRoot "main.bicepparam"
$deploymentName = "servicebus-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Host "`n[3/4] Deploying Service Bus via Bicep ($deploymentName)..." -ForegroundColor Yellow
$deployResultJson = az deployment sub create `
    --name $deploymentName `
    --location $Location `
    --template-file $bicepPath `
    --parameters $paramPath `
    --output json

$deployResult = $deployResultJson | ConvertFrom-Json

$namespaceName = $deployResult.properties.outputs.serviceBusNamespaceName.value
$queue = $deployResult.properties.outputs.serviceBusQueueName.value
$connectionString = $deployResult.properties.outputs.serviceBusConnectionString.value

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host " DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host " Namespace: $namespaceName" -ForegroundColor Green
Write-Host " Queue: $queue" -ForegroundColor Green
Write-Host " Connection String:" -ForegroundColor Yellow
Write-Host " $connectionString" -ForegroundColor White
Write-Host "==========================================================" -ForegroundColor Green

# 4. Optional Update of appsettings.json
if ($UpdateAppSettings) {
    Write-Host "`n[4/4] Updating src/EnterpriseApp.Api/appsettings.json..." -ForegroundColor Yellow
    $appsettingsPath = Join-Path $PSScriptRoot "..\..\src\EnterpriseApp.Api\appsettings.json"
    
    if (Test-Path $appsettingsPath) {
        $json = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
        $json.AzureServiceBus.ConnectionString = $connectionString
        $json.AzureServiceBus.QueueName = $queue
        $json | ConvertTo-Json -Depth 10 | Set-Content $appsettingsPath -Encoding UTF8
        Write-Host "Successfully updated appsettings.json with live Azure Service Bus connection string!" -ForegroundColor Green
    } else {
        Write-Warning "Could not find appsettings.json at $appsettingsPath"
    }
} else {
    Write-Host "`nTip: Run with -UpdateAppSettings to automatically write the connection string to appsettings.json" -ForegroundColor Cyan
}
