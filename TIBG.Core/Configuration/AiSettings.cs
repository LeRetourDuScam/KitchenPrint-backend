namespace TIBG.API.Core.Configuration
{
    public class AiSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string ModelName { get; set; } = "llama-3.3-70b-versatile";
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
        public int CacheDurationMinutes { get; set; } = 60;
        public int MaxRetries { get; set; } = 3;
        public int TimeoutSeconds { get; set; } = 120;
    }
}
