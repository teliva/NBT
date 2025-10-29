using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using NBT.Models;
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

        var uniqueName = $"Img_Out.png";
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

        NanoBananaResponse apiResponse = await gipc.GenerateAsync(imageBytes, request.Text);


        var retB64 = apiResponse?.Candidates[0].Content.Parts[1].InlineData.Data;
        if (string.IsNullOrEmpty(retB64))
        {
            Console.WriteLine("No image data found in response.");
            throw new Exception("Returned no image");
        }

        // Decode and save the image
        byte[] outputImageBytes = Convert.FromBase64String(retB64);

        var uniqueNameBack = "Img_Back.png";
        var filePathBack = Path.Combine(uploadsFolder, uniqueNameBack);

        // Save to file
        await using (var stream = new FileStream(filePathBack, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.WriteAsync(outputImageBytes, 0, outputImageBytes.Length);
        }

        return Ok();
    }
}
public class MessageUpload
{
    public string Text { get; set; } = string.Empty;
    public IFormFile Image { get; set; } = default!;
}