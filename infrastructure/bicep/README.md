# Azure Service Bus Bicep Deployment

This directory contains production-ready **Azure Bicep** templates to provision Azure Service Bus on **Subscription `8419b0f2-ff8e-4fe0-924e-47223fdea478` ("Subscription 1")**.

---

## 📁 Files

- **[`main.bicep`](file:///C:/dev/Clientele%202/infrastructure/bicep/main.bicep)**: Subscription-scoped deployment template. Provisions the Resource Group (`rg-enterprise-messaging-prod`) and deploys the Service Bus module.
- **[`servicebus.bicep`](file:///C:/dev/Clientele%202/infrastructure/bicep/servicebus.bicep)**: Resource Group scoped module that creates:
  - Azure Service Bus Namespace (Standard tier, TLS 1.2 minimum)
  - `enterprise-messages` Queue configured with **Peek-Lock** (`lockDuration: PT1M`), **Dead-Lettering** (`maxDeliveryCount: 10`, `deadLetteringOnMessageExpiration: true`), and 14-day default message TTL.
  - Shared Access Policy `EnterpriseAppClientRule` with `Send` and `Listen` rights.
  - Connection string outputs ready for `appsettings.json`.
- **[`main.bicepparam`](file:///C:/dev/Clientele%202/infrastructure/bicep/main.bicepparam)**: Parameter file pre-configured for Subscription `8419b0f2-ff8e-4fe0-924e-47223fdea478`.
- **[`deploy.ps1`](file:///C:/dev/Clientele%202/infrastructure/bicep/deploy.ps1)**: Automated deployment script with automatic connection string extraction and optional `appsettings.json` update.

---

## 🚀 Quick Deployment Options

### Option 1: Using the PowerShell Deployment Script (Recommended)

1. Open PowerShell in `infrastructure/bicep`:
   ```powershell
   cd "C:\dev\Clientele 2\infrastructure\bicep"
   ```

2. Run the deployment script:
   ```powershell
   .\deploy.ps1 -UpdateAppSettings
   ```
   *The `-UpdateAppSettings` flag will automatically inject the deployed connection string directly into `src/EnterpriseApp.Api/appsettings.json`.*

---

### Option 2: Using Azure CLI Directly

1. Login and select Subscription 1:
   ```bash
   az login
   az account set --subscription 8419b0f2-ff8e-4fe0-924e-47223fdea478
   ```

2. Deploy at subscription scope:
   ```bash
   az deployment sub create \
     --name "servicebus-prod-deployment" \
     --location eastus \
     --template-file main.bicep \
     --parameters main.bicepparam
   ```

3. Query the connection string output:
   ```bash
   az deployment sub show \
     --name "servicebus-prod-deployment" \
     --query "properties.outputs.serviceBusConnectionString.value" \
     --output tsv
   ```

---

### Option 3: Using Azure PowerShell

```powershell
Connect-AzAccount
Set-AzContext -SubscriptionId "8419b0f2-ff8e-4fe0-924e-47223fdea478"

New-AzSubscriptionDeployment `
  -Name "servicebus-prod-deployment" `
  -Location "eastus" `
  -TemplateFile ".\main.bicep" `
  -TemplateParameterFile ".\main.bicepparam"
```

---

## ⚙️ Connecting the Application

Once deployed, update `src/EnterpriseApp.Api/appsettings.json` (or use .NET User Secrets):

```json
{
  "AzureServiceBus": {
    "ConnectionString": "Endpoint=sb://sb-enterprise-xxxx.servicebus.windows.net/;SharedAccessKeyName=EnterpriseAppClientRule;SharedAccessKey=xxxx",
    "QueueName": "enterprise-messages"
  }
}
```

Or via .NET User Secrets from the API folder:
```powershell
dotnet user-secrets set "AzureServiceBus:ConnectionString" "<your-connection-string>" --project src/EnterpriseApp.Api
dotnet user-secrets set "AzureServiceBus:QueueName" "enterprise-messages" --project src/EnterpriseApp.Api
```
