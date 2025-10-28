using NBT.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

public class GeminiImagePromptClient
{
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public GeminiImagePromptClient(string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("x-goog-api-key", _apiKey);
    }

    public async Task<byte[]> GenerateAsync(byte[] imageBytes, string prompt)
    {
        string base64Image = Convert.ToBase64String(imageBytes);

        // Construct the request payload
        var requestBody = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/jpeg",
                                data = base64Image
                            }
                        }
                    }
                }
            }
        };

        // Serialize JSON
        string json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // API endpoint (Gemini 2.5 Flash Image)
        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent";

        // Send request
        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        string responseBody = await response.Content.ReadAsStringAsync();
        NanoBananaResponse result = JsonSerializer.Deserialize<NanoBananaResponse>(responseBody);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Error:");
            Console.WriteLine(JsonSerializer.Serialize(result));
            throw new ExternalException("Bad response from Nano Banana API");
        }

        var b64Img = result?.Candidates[0].Content.Parts[0].InlineData.Data;
        if (string.IsNullOrEmpty(b64Img))
        {
            Console.WriteLine("No image data found in response.");
            throw new Exception("Returned no image");
        }

        // Decode and save the image
        byte[] outputImageBytes = Convert.FromBase64String(b64Img);
        return outputImageBytes;
    }
}
