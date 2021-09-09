using System;
using System.Threading.Tasks;
using CSharpFunction.Infrastructure.Helpers;
using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Kind = Pulumi.AzureNative.Storage.Kind;

/// <summary>
/// See https://github.com/pulumi/examples/tree/master/azure-cs-functions
/// </summary>
class MainStack : Stack
{
    public MainStack()
    {
        var resourceGroup = new ResourceGroup("CSharpFunction");

        var storageAccount = new StorageAccount("storageaccount", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });

        var appServicePlan = new AppServicePlan("functionappservice", new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,

            // Consumption plan SKU
            Sku = new SkuDescriptionArgs
            {
                Tier = "Dynamic",
                Name = "Y1"
            }
        });

        var container = new BlobContainer("functionzips", new BlobContainerArgs
        {
            AccountName = storageAccount.Name,
            PublicAccess = PublicAccess.None,
            ResourceGroupName = resourceGroup.Name,
        });

        var blob = new Blob("helloworldfunction.zip", new BlobArgs
        {
            AccountName = storageAccount.Name,
            ContainerName = container.Name,
            ResourceGroupName = resourceGroup.Name,
            Type = BlobType.Block,
            Source = new FileArchive("..\\HelloWorldFunction\\bin\\Debug\\netcoreapp3.1\\publish") // This path should be set to the output of `dotnet publish`
        });

        // Application insights
        var appInsights = new Component("appInsights", new ComponentArgs
        {
            ApplicationType = ApplicationType.Web,
            Kind = "web",
            ResourceGroupName = resourceGroup.Name,
        });

        var functionZipBlobSasUrl = OutputHelpers.SignedBlobReadUrl(blob, container, storageAccount, resourceGroup);

        var app = new WebApp("helloworldfunctionapp", new WebAppArgs
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
                        Value = functionZipBlobSasUrl,
                    },
                    new NameValuePairArgs{
                        Name = "APPLICATIONINSIGHTS_CONNECTION_STRING",
                        Value = Output.Format($"InstrumentationKey={appInsights.InstrumentationKey}"),
                    },
                },
            },
        });

        this.FunctionZip = functionZipBlobSasUrl;

        this.Endpoint = Output.Format($"https://{app.DefaultHostName}/api/HelloWorldFunction?name=Pulumi");
    }

    [Output]
    public Output<string> Endpoint { get; set; }

    [Output]
    public Output<string> FunctionZip { get; set; }
}
