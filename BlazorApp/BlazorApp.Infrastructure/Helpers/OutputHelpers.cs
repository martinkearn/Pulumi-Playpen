using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Storage;
using System;

namespace BlazorApp.Infrastructure.Helpers
{
    public static class OutputHelpers
    {
        public static Output<string> SignedBlobReadUrl(Blob blob, BlobContainer container, StorageAccount account, ResourceGroup resourceGroup)
        {
            return Output.Tuple(blob.Name, container.Name, account.Name, resourceGroup.Name)
                .Apply(t =>
                {
                    (string blobName, string containerName, string accountName, string resourceGroupName) = t;

                    var blobSAS = ListStorageAccountServiceSAS.InvokeAsync(new ListStorageAccountServiceSASArgs
                    {
                        AccountName = accountName,
                        Protocols = HttpProtocol.Https,
                        SharedAccessStartTime = DateTime.Now.Subtract(new TimeSpan(365, 0, 0, 0)).ToString("yyyy-MM-dd"),
                        SharedAccessExpiryTime = DateTime.Now.AddDays(3650).ToString("yyyy-MM-dd"),
                        Resource = SignedResource.C,
                        ResourceGroupName = resourceGroupName,
                        Permissions = Permissions.R,
                        CanonicalizedResource = "/blob/" + accountName + "/" + containerName,
                        ContentType = "application/json",
                        CacheControl = "max-age=5",
                        ContentDisposition = "inline",
                        ContentEncoding = "deflate",
                    });
                    return Output.Format($"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{blobSAS.Result.ServiceSasToken}");
                });
        }
    }
}
