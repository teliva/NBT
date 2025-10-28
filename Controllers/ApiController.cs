using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace NanoBananaBackend.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : ControllerBase
{
    private readonly IConfiguration _config;
    public ApiController(IConfiguration configuration)
    {
        _config = configuration;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> PostGenerate([FromForm] MessageUpload request)
    {
        if (request.Image == null || request.Image.Length == 0)
            return BadRequest("No image uploaded.");

        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Prompt cannot be empty.");

        if (string.IsNullOrWhiteSpace(_config["App:FileDirectory"]))
        {
            return StatusCode(500, "Data folder not specified in configuration.");
        }

        if (string.IsNullOrEmpty(_config["App:ApiKey"]))
        {
            return StatusCode(500, "Gemini API key not specified in configuration.");
        }

        var uploadsFolder = Path.Combine(_config["App:FileDirectory"], Guid.NewGuid().ToString());
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueName = $"Img_Out";
        var filePath = Path.Combine(uploadsFolder, uniqueName);


        var jsonData = new
        {
            Text = request.Text,
            UploadedAt = DateTime.UtcNow
        };

        var jsonFilePath = Path.Combine(uploadsFolder, Path.GetFileNameWithoutExtension("prompt") + ".json");
        await System.IO.File.WriteAllTextAsync(jsonFilePath, JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true }));

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.Image.CopyToAsync(stream);
        }
        
        byte[] imageBytes;
        await using (var memoryStream = new MemoryStream())
        {
            await request.Image.CopyToAsync(memoryStream);
            imageBytes = memoryStream.ToArray();
        }

        // Call Gemini API
        GeminiImagePromptClient gipc = new GeminiImagePromptClient(_config["App:ApiKey"]);
        try
        {
            await gipc.GenerateAsync(imageBytes, request.Text);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error generating image: {ex.Message}");
        }

        return Ok();
    }

    public async Task<IActionResult> ImageGenerate(byte[] img)
    {
        return Ok();
    }
}
public class MessageUpload
{
    public string Text { get; set; } = string.Empty;
    public IFormFile Image { get; set; } = default!;
}