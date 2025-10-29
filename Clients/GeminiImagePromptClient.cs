using NBT.Models;
using System.Text.Json;

public class GeminiImagePromptClient
{
    private readonly HttpClient _httpClient;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GeminiImagePromptClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<NanoBananaResponse?> GenerateAsync(
        byte[] imageBytes, 
        string prompt,
        string mimeType = "image/jpg",
        CancellationToken cancellationToken = default)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("Image bytes cannot be null or empty.", nameof(imageBytes));
        if (string.IsNullOrWhiteSpace(prompt))
            throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));


        string base64Image = Convert.ToBase64String(imageBytes);

        var request = new
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

        using var content = JsonContent.Create(request, options: _jsonOptions);

        using var response = await _httpClient
            .PostAsync("v1beta/models/gemini-2.5-flash-image:generateContent", content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Gemini API request failed with status {response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<NanoBananaResponse>(body, _jsonOptions);
    }

    private record GeminiRequest
    {
        public GeminiContent[] Contents { get; init; } = Array.Empty<GeminiContent>();
    }

    private record GeminiContent
    {
        public object[] Parts { get; init; } = Array.Empty<object>();
    }
}