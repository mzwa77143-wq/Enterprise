targetScope = 'subscription'

@description('Target Azure Subscription ID.')
param subscriptionId string = '8419b0f2-ff8e-4fe0-924e-47223fdea478'

@description('Resource Group name for the enterprise solution messaging infrastructure.')
param resourceGroupName string = 'rg-enterprise-messaging-prod'

@description('Azure region for the Resource Group and Service Bus deployment.')
param location string = 'eastus'

@description('Pricing tier of the Service Bus.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('Name of the Service Bus queue.')
param queueName string = 'enterprise-messages'

@description('Optional custom namespace name. If empty, a globally unique name is generated.')
param customNamespaceName string = ''

// 1. Create or ensure Resource Group exists on Subscription 1
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    SubscriptionId: subscriptionId
    SubscriptionName: 'Subscription 1'
    Application: 'EnterpriseSolution'
    Environment: 'Production'
  }
}

// 2. Deploy Service Bus module into the Resource Group
module serviceBus 'servicebus.bicep' = {
  scope: rg
  name: 'deploy-servicebus-${uniqueString(rg.id)}'
  params: {
    location: location
    skuName: skuName
    queueName: queueName
    namespaceName: !empty(customNamespaceName) ? customNamespaceName : 'sb-enterprise-${uniqueString(rg.id)}'
  }
}

// Outputs
output subscriptionId string = subscriptionId
output resourceGroupName string = rg.name
output serviceBusNamespaceName string = serviceBus.outputs.serviceBusNamespaceName
output serviceBusQueueName string = serviceBus.outputs.serviceBusQueueName
output serviceBusEndpoint string = serviceBus.outputs.serviceBusEndpoint

#disable-next-line outputs-should-not-contain-secrets
output serviceBusConnectionString string = serviceBus.outputs.serviceBusConnectionString
