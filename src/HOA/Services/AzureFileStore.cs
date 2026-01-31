using Azure.Storage.Blobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HOA.Services
{
    public class AzureFileStore : IFileStore
    {
        public static string ConnectionString;

        private BlobContainerClient GetContainerClient()
        {
            var blobServiceClient = new BlobServiceClient(ConnectionString);
            return blobServiceClient.GetBlobContainerClient("arb");
        }

        public async Task DeleteFile(string id)
        {
            var containerClient = GetContainerClient();
            var blobClient = containerClient.GetBlobClient(id);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<Stream> RetriveFile(string id)
        {
            var containerClient = GetContainerClient();
            var blobClient = containerClient.GetBlobClient(id);
            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task<string> StoreFile(string code, Stream data)
        {
            var id = string.Format("{0}={1}", code, Guid.NewGuid().ToString());

            var containerClient = GetContainerClient();
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(id);
            await blobClient.UploadAsync(data, overwrite: true);

            return id;
        }
    }
}
