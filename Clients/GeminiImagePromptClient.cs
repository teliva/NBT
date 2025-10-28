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

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine("Error:");
            Console.WriteLine(responseBody);
            throw new ExternalException("Bad response from Nano Banana API");
        }

        // Parse the base64 image data from the JSON
        using var doc = JsonDocument.Parse(responseBody);
        string? base64Output = null;

        // Traverse JSON for "data" field
        base64Output = FindLargeString(doc.RootElement);

        if (string.IsNullOrEmpty(base64Output))
        {
            Console.WriteLine("No image data found in response.");
            throw new Exception("Returned no image");
        }

        // Decode and save the image
        byte[] outputImageBytes = Convert.FromBase64String(base64Output);
        return outputImageBytes;
    }

    // Recursive method to find large string (likely base64 image)
    string? FindLargeString(JsonElement element, int minLength = 1000)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var result = FindLargeString(prop.Value, minLength);
                    if (result != null) return result;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var result = FindLargeString(item, minLength);
                    if (result != null) return result;
                }
                break;

            case JsonValueKind.String:
                var str = element.GetString();
                if (str != null && str.Length > minLength)
                {
                    return str;
                }
                break;
        }
        return null;
    }
}
