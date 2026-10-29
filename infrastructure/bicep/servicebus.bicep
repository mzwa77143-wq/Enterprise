@description('The name of the Azure Service Bus namespace. Must be globally unique across Azure.')
param namespaceName string = 'sb-enterprise-${uniqueString(resourceGroup().id)}'

@description('Azure region where the Service Bus will be deployed.')
param location string = resourceGroup().location

@description('The pricing tier for Azure Service Bus.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param skuName string = 'Standard'

@description('The name of the main queue for enterprise messaging.')
param queueName string = 'enterprise-messages'

@description('The duration of a peek-lock on the message (ISO 8601 duration format).')
param lockDuration string = 'PT1M'

@description('The maximum number of delivery attempts before dead-lettering.')
param maxDeliveryCount int = 10

@description('Whether to route expired messages to the dead-letter sub-queue.')
param deadLetteringOnMessageExpiration bool = true

@description('Default message time-to-live (ISO 8601 duration format).')
param defaultMessageTimeToLive string = 'P14D'

@description('Tags to associate with the created Azure resources.')
param tags object = {
  Environment: 'Production'
  Application: 'EnterpriseSolution'
  ManagedBy: 'Bicep'
}

// 1. Service Bus Namespace
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  sku: {
    name: skuName
    tier: skuName
  }
  tags: tags
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

// 2. Enterprise Messages Queue (Peek-Lock & Dead-Letter configured)
resource serviceBusQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: queueName
  properties: {
    lockDuration: lockDuration
    maxDeliveryCount: maxDeliveryCount
    deadLetteringOnMessageExpiration: deadLetteringOnMessageExpiration
    defaultMessageTimeToLive: defaultMessageTimeToLive
    enableBatchedOperations: true
    requiresDuplicateDetection: false
    requiresSession: false
    maxSizeInMegabytes: 1024
  }
}

// 3. Application Shared Access Policy (Send and Listen rights for App API)
resource appAuthRule 'Microsoft.ServiceBus/namespaces/AuthorizationRules@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'EnterpriseAppClientRule'
  properties: {
    rights: [
      'Listen'
      'Send'
    ]
  }
}

// Outputs for client configuration
output serviceBusNamespaceId string = serviceBusNamespace.id
output serviceBusNamespaceName string = serviceBusNamespace.name
output serviceBusQueueName string = serviceBusQueue.name
output serviceBusEndpoint string = serviceBusNamespace.properties.serviceBusEndpoint

@description('Primary Connection String with Send and Listen permissions for appsettings.json.')
#disable-next-line outputs-should-not-contain-secrets
output serviceBusConnectionString string = appAuthRule.listKeys().primaryConnectionString

@description('Root Manage Shared Access Key connection string.')
#disable-next-line outputs-should-not-contain-secrets
output rootManageConnectionString string = listKeys('${serviceBusNamespace.id}/authorizationRules/RootManageSharedAccessKey', serviceBusNamespace.apiVersion).primaryConnectionString
