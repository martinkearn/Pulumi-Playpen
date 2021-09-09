using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

class MainStack : Stack
{
    public MainStack()
    {
        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("BlazorApp");

        // Create an Azure resource (Storage Account)
        var storageAccount = new StorageAccount("storage", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });
    }
}
