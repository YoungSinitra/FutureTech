using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;

namespace FutureTech.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            string connectionString = configuration["AzureStorage:BlobConnectionString"];
            string containerName = configuration["AzureStorage:BlobContainerName"];
            
            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.None);
            
            _logger = logger;
        }

        public async Task<string> UploadProfileImageAsync(IFormFile file, string studentId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null", nameof(file));
            }

            // Validate the file type
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || (extension != ".jpg" && extension != ".jpeg" && extension != ".png"))
            {
                throw new ArgumentException("Invalid file type. Only JPG and PNG files are allowed.", nameof(file));
            }

            // Create a unique file name using the student ID
            string blobName = $"{studentId}{extension}";
            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            // Set the content type
            var blobHttpHeaders = new BlobHttpHeaders();
            blobHttpHeaders.ContentType = file.ContentType;

            // Upload the file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, blobHttpHeaders);
            }

            // Return the full URL
            return blobClient.Uri.ToString();
        }

        public async Task<string> GetProfileImageSasUriAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                return null;
            }

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            // Generate a SAS token that's valid for 1 hour
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            return sasUri.ToString();
        }

        public async Task DeleteProfileImageAsync(string blobName)
        {
            if (string.IsNullOrEmpty(blobName))
            {
                return;
            }

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
} 