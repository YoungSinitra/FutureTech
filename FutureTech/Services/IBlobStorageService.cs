using Microsoft.AspNetCore.Http;

namespace FutureTech.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadProfileImageAsync(IFormFile file, string studentId);
        Task<string> GetProfileImageSasUriAsync(string blobName);
        Task DeleteProfileImageAsync(string blobName);
    }
} 