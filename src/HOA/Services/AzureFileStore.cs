using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class AzureFileStore : IFileStore
    {
        public static string ConnectionString;

        public Task DeleteFile(string id)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("arb");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(id);
            return blockBlob.DeleteAsync();
        }

        public async Task<Stream> RetriveFile(string id)
        {
            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("arb");

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(id);

            return await blockBlob.OpenReadAsync();
        }

        public async Task<string> StoreFile(string code, Stream data)
        {
            var id = string.Format("{0}={1}", code, Guid.NewGuid().ToString());

            // Retrieve storage account from connection string.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionString);

            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            CloudBlobContainer container = blobClient.GetContainerReference("arb");

            // Create the container if it doesn't already exist.
            await container.CreateIfNotExistsAsync();

            // Retrieve reference to a blob named "myblob".
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(id);
            await blockBlob.UploadFromStreamAsync(data);

            return id;
        }
    }
}
