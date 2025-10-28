using System.Text.Json.Serialization;

namespace NBT.Models
{

    public class NanoBananaResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; }
    }

    public class Part
    {
        [JsonPropertyName("inlineData")]
        public InlineData InlineData { get; set; }
    }

    public class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }
    }

    public class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }

        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }

        [JsonPropertyName("promptTokensDetails")]
        public object PromptTokensDetails { get; set; }

        [JsonPropertyName("candidatesTokensDetails")]
        public object CandidatesTokensDetails { get; set; }

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; }

        [JsonPropertyName("responseId")]
        public string ResponseId { get; set; }
    }
}