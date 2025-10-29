using NBT.Models;
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

    public async Task<NanoBananaResponse> GenerateAsync(byte[] imageBytes, string prompt)
    {
        string base64Image = Convert.ToBase64String(imageBytes);

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

        string json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent";

        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        string responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Bad response from Nano Banana API");
        }
        return JsonSerializer.Deserialize<NanoBananaResponse>(responseBody);
    }
}