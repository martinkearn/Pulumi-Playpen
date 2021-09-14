using CSharpFunctionDemo.Infrastructure.Helpers;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using Pulumi.AzureNative.Storage.Inputs;

class MyStack : Stack
{
    public MyStack()
    {
        // Create an Azure Resource Group
        var resourceGroup = new ResourceGroup("resourceGroup");

        // Create an Azure resource (Storage Account)
        var storageAccount = new StorageAccount("sa", new StorageAccountArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Sku = new SkuArgs
            {
                Name = SkuName.Standard_LRS
            },
            Kind = Kind.StorageV2
        });

        // Export the primary key of the Storage Account
        this.PrimaryStorageKey = Output.Tuple(resourceGroup.Name, storageAccount.Name).Apply(names =>
            Output.CreateSecret(OutputHelpers.GetStorageAccountPrimaryKey(names.Item1, names.Item2).Result));
    }

    [Output]
    public Output<string> PrimaryStorageKey { get; set; }
}
