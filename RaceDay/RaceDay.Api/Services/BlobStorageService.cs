using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>The only place in the whole solution that talks to Azure Blob Storage.</summary>
public class BlobStorageService
{
    private const long MaxFileBytes = 5 * 1024 * 1024;   // 5 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/gif", "image/webp" };

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;
    private BlobContainerClient? _container;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Returns an error message if the file is not an acceptable image, otherwise null.</summary>
    public static string? ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return "Please choose an image file.";

        if (file.Length > MaxFileBytes)
            return "The image must be 5 MB or smaller.";

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
            return "Only JPG, PNG, GIF or WEBP images are allowed.";

        return null;
    }

    // The container client is created on first use, so the API still starts
    // (and the tests and CI still run) on a machine with no Azure secret.
    private BlobContainerClient GetContainer()
    {
        if (_container != null) return _container;

        var connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");

        var containerName = _configuration["AzureBlobStorage:ContainerName"] ?? "raceday-images";
        _container = new BlobContainerClient(connectionString, containerName);
        return _container;
    }

    /// <summary>Uploads the file and returns its public URL, or null if the upload failed.</summary>
    public async Task<string?> UploadAsync(IFormFile file, string folder)
    {
        try
        {
            var container = GetContainer();
            await container.CreateIfNotExistsAsync();   // does nothing if the container exists

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var blobName = $"{folder}/{Guid.NewGuid():N}{extension}";   // random name, no clashes
            var blob = container.GetBlobClient(blobName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            });

            return blob.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uploading an image to Azure Blob Storage failed.");
            return null;
        }
    }

    /// <summary>Removes an old image when it is replaced. Best effort: never throws.</summary>
    public async Task DeleteAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            var container = GetContainer();
            var marker = "/" + container.Name + "/";
            var path = new Uri(url).AbsolutePath;
            var index = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return;

            var blobName = Uri.UnescapeDataString(path[(index + marker.Length)..]);
            await container.GetBlobClient(blobName).DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete the old image from Azure Blob Storage.");
        }
    }
}