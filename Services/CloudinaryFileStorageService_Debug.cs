using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Inventory_Management_Requirements.Services
{
    public class CloudinaryFileStorageService_Debug : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly IConfiguration _configuration;

        public CloudinaryFileStorageService_Debug(IConfiguration configuration)
        {
            _configuration = configuration;

            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            Console.WriteLine($"=== CLOUDINARY DEBUG ===");
            Console.WriteLine($"CloudName: {cloudName}");
            Console.WriteLine($"ApiKey: {apiKey?.Substring(0, Math.Min(8, apiKey?.Length ?? 0))}...");
            Console.WriteLine($"ApiSecret: {apiSecret?.Substring(0, Math.Min(8, apiSecret?.Length ?? 0))}...");

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                Console.WriteLine("ERROR: Cloudinary configuration is missing or incomplete");
                throw new ArgumentException("Cloudinary configuration is missing or incomplete");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);

            Console.WriteLine("Cloudinary service initialized successfully");
            Console.WriteLine("=== END CLOUDINARY DEBUG ===");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderPath)
        {
            Console.WriteLine($"=== UPLOAD START ===");
            Console.WriteLine($"File: {file?.FileName}, Size: {file?.Length}, Type: {file?.ContentType}");

            if (file == null || file.Length == 0)
            {
                Console.WriteLine("ERROR: File is null or empty");
                throw new ArgumentException("File is empty");
            }

            try
            {
                await using var stream = file.OpenReadStream();
                Console.WriteLine($"Stream created, length: {stream.Length}");

                // Check if file is an image
                if (file.ContentType.StartsWith("image/"))
                {
                    Console.WriteLine("Uploading as image...");
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folderPath,
                        Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                    };

                    Console.WriteLine($"Upload params: Folder={folderPath}, FileName={file.FileName}");

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    Console.WriteLine($"Upload result received:");
                    Console.WriteLine($"  Status: {uploadResult.StatusCode}");
                    Console.WriteLine($"  PublicId: {uploadResult.PublicId}");
                    Console.WriteLine($"  SecureUrl: {uploadResult.SecureUrl}");

                    if (uploadResult.Error != null)
                    {
                        Console.WriteLine($"ERROR: {uploadResult.Error.Message}");
                        throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");
                    }

                    Console.WriteLine("=== UPLOAD SUCCESSFUL ===");
                    return uploadResult.SecureUrl.ToString();
                }
                else
                {
                    Console.WriteLine("Uploading as raw file...");
                    // For non-image files (PDFs, documents, etc.)
                    var uploadParams = new RawUploadParams
                    {
                        File = new FileDescription(file.FileName, stream),
                        Folder = folderPath
                    };

                    Console.WriteLine($"Upload params: Folder={folderPath}, FileName={file.FileName}");

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                    Console.WriteLine($"Upload result received:");
                    Console.WriteLine($"  Status: {uploadResult.StatusCode}");
                    Console.WriteLine($"  PublicId: {uploadResult.PublicId}");
                    Console.WriteLine($"  SecureUrl: {uploadResult.SecureUrl}");

                    if (uploadResult.Error != null)
                    {
                        Console.WriteLine($"ERROR: {uploadResult.Error.Message}");
                        throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");
                    }

                    Console.WriteLine("=== UPLOAD SUCCESSFUL ===");
                    return uploadResult.SecureUrl.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== UPLOAD EXCEPTION ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return false;

            try
            {
                var publicId = GetPublicIdFromUrl(fileUrl);
                var deleteParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deleteParams);
                return result.Result == "ok";
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GetFileUrlAsync(string fileName, string folderPath)
        {
            var publicId = $"{folderPath}/{Path.GetFileNameWithoutExtension(fileName)}";
            var url = _cloudinary.Api.UrlImgUp.BuildUrl(publicId);
            return Task.FromResult(url);
        }

        private string GetPublicIdFromUrl(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var publicId = path.Substring(1); // Remove leading slash
            var lastDotIndex = publicId.LastIndexOf('.');
            if (lastDotIndex > 0)
            {
                publicId = publicId.Substring(0, lastDotIndex);
            }
            return publicId;
        }
    }
}
