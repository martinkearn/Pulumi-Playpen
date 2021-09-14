using CSharpFunction.Infrastructure.Helpers;
using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

/// <summary>
/// See https://github.com/pulumi/examples/tree/master/azure-cs-functions
/// </summary>
class MainStack : Stack
{
    private const string FunctionName = "HelloWorldFunction";

    public MainStack()
    {
// Create resource group
var resourceGroup = new ResourceGroup("CSharpFunction");

// Create storage account
var storageAccount = new StorageAccount("storage", new StorageAccountArgs
{
    ResourceGroupName = resourceGroup.Name,
    Sku = new SkuArgs
    {
        Name = SkuName.Standard_LRS
    },
    Kind = Pulumi.AzureNative.Storage.Kind.StorageV2
});

// Add blob container to storage account
var container = new BlobContainer("functionzips", new BlobContainerArgs
{
    AccountName = storageAccount.Name,
    PublicAccess = PublicAccess.None,
    ResourceGroupName = resourceGroup.Name,
});

// Create a zip of the function's publish output and upload it to the blob container
var blob = new Blob($"{FunctionName.ToLowerInvariant()}.zip", new BlobArgs
{
    AccountName = storageAccount.Name,
    ContainerName = container.Name,
    ResourceGroupName = resourceGroup.Name,
    Type = BlobType.Block,
    Source = new FileArchive($"..\\{FunctionName}\\bin\\Debug\\netcoreapp3.1\\publish") // This path should be set to the output of `dotnet publish` command
});

// Generate SAS url for the function output zip in storage
var deploymentZipBlobSasUrl = OutputHelpers.SignedBlobReadUrl(blob, container, storageAccount, resourceGroup);

// Create application insights
var appInsights = new Component("appinsights", new ComponentArgs
{
    ApplicationType = ApplicationType.Web,
    Kind = "web",
    ResourceGroupName = resourceGroup.Name,
});

// Create app service plan for function app
var appServicePlan = new AppServicePlan("appserviceplan", new AppServicePlanArgs
{
    ResourceGroupName = resourceGroup.Name,

    // Consumption plan SKU
    Sku = new SkuDescriptionArgs
    {
        Tier = "Dynamic",
        Name = "Y1"
    }
});

// Create function app. Set WEBSITE_RUN_FROM_PACKAGE to use the zip in storage
var app = new WebApp($"{FunctionName.ToLowerInvariant()}appservice", new WebAppArgs
{
    Kind = "FunctionApp",
    ResourceGroupName = resourceGroup.Name,
    ServerFarmId = appServicePlan.Id,
    SiteConfig = new SiteConfigArgs
    {
        AppSettings = new[]
        {
            new NameValuePairArgs{
                Name = "AzureWebJobsStorage",
                Value = OutputHelpers.GetConnectionString(resourceGroup.Name, storageAccount.Name),
            },
            new NameValuePairArgs{
                Name = "FUNCTIONS_WORKER_RUNTIME",
                Value = "dotnet",
            },
            new NameValuePairArgs{
                Name = "FUNCTIONS_EXTENSION_VERSION",
                Value = "~3",
            },
            new NameValuePairArgs{
                Name = "WEBSITE_RUN_FROM_PACKAGE",
                Value = deploymentZipBlobSasUrl,
            },
            new NameValuePairArgs{
                Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                Value = Output.Format($"InstrumentationKey={appInsights.InstrumentationKey}"),
            },
        },
    },
});

// Output the function endpoint
this.Endpoint = Output.Format($"https://{app.DefaultHostName}/api/{FunctionName}?name=Pulumi");
    }

    [Output]
    public Output<string> Endpoint { get; set; }
}
