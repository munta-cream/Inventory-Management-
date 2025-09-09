using Microsoft.AspNetCore.Http;

namespace Inventory_Management_Requirements.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderPath);
        Task<bool> DeleteFileAsync(string fileUrl);
        Task<string> GetFileUrlAsync(string fileName, string folderPath);
    }
}
