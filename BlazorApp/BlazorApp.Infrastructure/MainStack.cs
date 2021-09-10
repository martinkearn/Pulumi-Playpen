using BlazorApp.Infrastructure.Helpers;
using Pulumi;
using Pulumi.AzureNative.Insights;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

class MainStack : Stack
{
    private const string AppName = "BlazorServer";
    public MainStack()
    {
        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("BlazorApp");

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
        var container = new BlobContainer("deploymentzips", new BlobContainerArgs
        {
            AccountName = storageAccount.Name,
            PublicAccess = PublicAccess.None,
            ResourceGroupName = resourceGroup.Name,
        });

        // Create a zip of the app's publish output and upload it to the blob container
        var blob = new Blob($"{AppName.ToLowerInvariant()}.zip", new BlobArgs
        {
            AccountName = storageAccount.Name,
            ContainerName = container.Name,
            ResourceGroupName = resourceGroup.Name,
            Type = BlobType.Block,
            Source = new FileArchive($"..\\{nameof(BlazorApp)}.{AppName}\\bin\\Debug\\net6.0\\publish") // This path should be set to the output of `dotnet publish` command
        });

        // Generate SAS url for the app output zip in storage
        var deploymentZipBlobSasUrl = OutputHelpers.SignedBlobReadUrl(blob, container, storageAccount, resourceGroup);

        // Create application insights
        var appInsights = new Component("appinsights", new ComponentArgs
        {
            ApplicationType = ApplicationType.Web,
            Kind = "web",
            ResourceGroupName = resourceGroup.Name,
        });

        // Create app service plan for app
        var appServicePlan = new AppServicePlan("appserviceplan", new AppServicePlanArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuDescriptionArgs
            {
                Tier = "Shared",
                Name = "D1"
            }
        });

        // Create app service. Set WEBSITE_RUN_FROM_PACKAGE to use the zip in storage
        var app = new WebApp($"{AppName.ToLowerInvariant()}appservice", new WebAppArgs
        {
            ResourceGroupName = resourceGroup.Name,
            ServerFarmId = appServicePlan.Id,
            SiteConfig = new SiteConfigArgs
            {
                AppSettings = new[]
                {
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

        // Output the app service url
        this.AppServiceUrl = Output.Format($"https://{app.DefaultHostName}");
    }

    [Output]
    public Output<string> AppServiceUrl { get; set; }
}
