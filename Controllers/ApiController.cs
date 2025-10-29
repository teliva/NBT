using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NBT;
using NBT.Models;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Threading;

namespace NanoBananaBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : ControllerBase
{
    private readonly GeminiImagePromptClient _gemini;
    private readonly AppSettings _settings;

    public ApiController(IConfiguration configuration, GeminiImagePromptClient gemini, IOptions<AppSettings> options)
    {
        _gemini = gemini;
        _settings = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> PostGenerate([FromForm] MessageUpload request, CancellationToken cancellationToken)
    {
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("No image uploaded.");

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Prompt cannot be empty.");

        if (string.IsNullOrWhiteSpace(_settings.FileDirectory))
        {
            return StatusCode(500, "Data folder not specified in configuration.");
        }

        var uploadsFolder = CreateUniqueUploadsFolder(_settings.FileDirectory);

        // Save original image and JSON metadata
        byte[] imageBytes = await SaveImageAndMetadataAsync(request, uploadsFolder, cancellationToken);

        // Call Gemini API
        NanoBananaResponse? apiResponse = await _gemini.GenerateAsync(imageBytes, request.Text, "image/jpg", cancellationToken);

        string retB64 = apiResponse?.Candidates?[0]?.Content?.Parts?[1]?.InlineData?.Data
                        ?? throw new Exception("No image data returned from Gemini API.");

        byte[] outputImageBytes = Convert.FromBase64String(retB64);

        await SaveGeneratedImageDataAsync(outputImageBytes, apiResponse.UsageMetadata, uploadsFolder, "Img_Back.jpg", cancellationToken);

        return File(outputImageBytes, "image/jpeg", "Img_Back.jpg");
    }

    private string CreateUniqueUploadsFolder(string baseDirectory)
    {
        var folder = Path.Combine(baseDirectory, Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        return folder;
    }

    private async Task<byte[]> SaveImageAndMetadataAsync(MessageUpload request, string folder, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(folder, "Img_Out.png");
        byte[] imageBytes;

        // Read image into memory once
        await using (var memoryStream = new MemoryStream())
        {
            await request.Image.CopyToAsync(memoryStream, cancellationToken);
            imageBytes = memoryStream.ToArray();

            // Save original image to file
            await System.IO.File.WriteAllBytesAsync(filePath, imageBytes, cancellationToken);
        }

        // Save JSON metadata
        var jsonData = new
        {
            Text = request.Text,
            UploadedAt = DateTime.UtcNow
        };

        var jsonFilePath = Path.Combine(folder, "prompt.json");

        await System.IO.File.WriteAllTextAsync(
            jsonFilePath,
            JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken
        );

        return imageBytes;
    }

    private async Task SaveGeneratedImageDataAsync(byte[] outputImageBytes, UsageMetadata usageMetadata, string folder, string fileName, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.WriteAsync(outputImageBytes, 0, outputImageBytes.Length, cancellationToken);

        var jsonFilePath = Path.Combine(folder, "metadata.json");

        await System.IO.File.WriteAllTextAsync(
            jsonFilePath,
            JsonSerializer.Serialize(usageMetadata, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken
        );
    }
}

public class MessageUpload
{
    public string Text { get; set; } = string.Empty;
    public IFormFile Image { get; set; } = default!;
}