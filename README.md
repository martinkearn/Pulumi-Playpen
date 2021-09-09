# Pulumi Playpen
A playpen for experimenting and learning about Pulumi for Azure.

## GetStartedAzure

The result of the following tutorial: [Get Started with Azure | Pulumi](https://www.pulumi.com/docs/get-started/azure/)

Does the following:

- Creates a storage account
- Configures storage account for static website support
- Uploads an index.html file
- Configures the website endpoint and storage key as outputs

## CSharpFunction

Loosely based on [Pulumi Examples - Azure Functions on a Linux App Service Plan](https://github.com/pulumi/examples/tree/master/azure-cs-functions), this sample deploys a C# Azure Function from a separate project.

Does the following:

- Creates the storage account and a blob container
- Zips the function and uploads the zip to the blob container
- Generates a SAS url for the zip in storage
- Sets up Application Insights
- Creates an App Service plan
- Creates a Function App and configures it to run from the zip in storage

