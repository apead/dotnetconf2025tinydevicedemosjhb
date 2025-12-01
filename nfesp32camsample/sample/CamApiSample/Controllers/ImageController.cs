using Microsoft.AspNetCore.Mvc;

namespace CamApiSample.Controllers;

/// <summary>
/// Controller for handling ESP32-CAM image uploads and retrieval
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ImageController : ControllerBase
{
    private readonly ILogger<ImageController> _logger;
    private readonly string _imageStoragePath;

    public ImageController(ILogger<ImageController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _imageStoragePath = configuration["ImageStoragePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Images");
        
        // Create directory if it doesn't exist
        if (!Directory.Exists(_imageStoragePath))
        {
            Directory.CreateDirectory(_imageStoragePath);
            _logger.LogInformation($"Created image storage directory: {_imageStoragePath}");
        }
    }

    /// <summary>
    /// Upload a JPEG image from ESP32-CAM device
    /// </summary>
    /// <remarks>
    /// Send raw JPEG binary data in the request body with Content-Type: image/jpeg
    /// </remarks>
    /// <response code="200">Image uploaded successfully</response>
    /// <response code="400">Invalid or empty image data</response>
    /// <response code="500">Server error during upload</response>
    [HttpPost("upload")]
    [Consumes("image/jpeg")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadImage()
    {
        try
        {
            using var memoryStream = new MemoryStream();
            await Request.Body.CopyToAsync(memoryStream);
            var imageData = memoryStream.ToArray();

            if (imageData.Length == 0)
            {
                _logger.LogWarning("Received empty image data");
                return BadRequest(new { error = "No image data received" });
            }

            // Generate unique filename with timestamp
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var filename = $"img_{timestamp}_{Guid.NewGuid():N}.jpg";
            var filePath = Path.Combine(_imageStoragePath, filename);

            // Save image to file system
            await System.IO.File.WriteAllBytesAsync(filePath, imageData);

            _logger.LogInformation($"Image saved: {filename}, Size: {imageData.Length} bytes");

            return Ok(new 
            { 
                message = "Image uploaded successfully",
                filename = filename,
                size = imageData.Length,
                path = filePath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return StatusCode(500, new { error = "Failed to upload image", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a list of all uploaded images
    /// </summary>
    /// <remarks>
    /// Returns metadata for all JPEG images stored on the server
    /// </remarks>
    /// <response code="200">List of images retrieved successfully</response>
    /// <response code="500">Server error retrieving images</response>
    [HttpGet("list")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public IActionResult ListImages()
    {
        try
        {
            var files = Directory.GetFiles(_imageStoragePath, "*.jpg")
                .Select(f => new FileInfo(f))
                .Select(fi => new 
                {
                    filename = fi.Name,
                    size = fi.Length,
                    created = fi.CreationTimeUtc
                })
                .OrderByDescending(f => f.created)
                .ToList();

            return Ok(new 
            { 
                count = files.Count,
                images = files
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing images");
            return StatusCode(500, new { error = "Failed to list images" });
        }
    }
}
