using KitchenPrint.API.Core.Configuration;
using KitchenPrint.Contracts.DataAccess;
using KitchenPrint.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace KitchenPrint.API.Core.DataAccess
{
    public class EcoChatService : IEcoChatService
    {
        private readonly HttpClient _httpClient;
        private readonly AiSettings _aiSettings;
        private readonly ILogger<EcoChatService> _logger;

        public EcoChatService(
            HttpClient httpClient,
            IOptions<AiSettings> aiSettings,
            ILogger<EcoChatService> logger)
        {
            _httpClient = httpClient;
            _aiSettings = aiSettings.Value;
            _logger = logger;
        }

        public async Task<string> SendMessageAsync(EcoChatRequest request)
        {
            var systemPrompt = BuildSystemPrompt(request);
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add conversation history (last 10 messages)
            foreach (var msg in request.ConversationHistory.TakeLast(10))
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            messages.Add(new { role = "user", content = request.UserMessage });

            var requestBody = new
            {
                model = _aiSettings.ModelName,
                messages,
                max_tokens = 500,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_aiSettings.ApiKey}");

            var response = await _httpClient.PostAsync(_aiSettings.BaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI API returned {Status} for eco chat", response.StatusCode);
                return request.Language == "fr"
                    ? "Désolé, je ne peux pas répondre pour le moment. Veuillez réessayer."
                    : "Sorry, I can't respond right now. Please try again.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var message = aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            return message ?? (request.Language == "fr" ? "Aucune réponse disponible." : "No response available.");
        }

        private static string BuildSystemPrompt(EcoChatRequest request)
        {
            var lang = request.Language == "en" ? "English" : "French";
            var sb = new StringBuilder();

            sb.AppendLine($"Tu es un assistant éco-culinaire expert en impact environnemental des aliments. Réponds en {lang}.");
            sb.AppendLine("Tu aides les utilisateurs à comprendre et réduire l'empreinte écologique de leurs recettes.");
            sb.AppendLine("Sois concis, pratique, encourageant et donne des conseils actionnables.");
            sb.AppendLine("Base tes réponses sur les données AGRIBALYSE quand pertinent.");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(request.RecipeName))
            {
                sb.AppendLine($"Contexte de la recette actuelle :");
                sb.AppendLine($"- Nom : {request.RecipeName}");

                if (request.Servings.HasValue)
                    sb.AppendLine($"- Portions : {request.Servings}");
                if (request.TotalCarbonKg.HasValue)
                    sb.AppendLine($"- Empreinte carbone totale : {request.TotalCarbonKg:F2} kg CO₂");
                if (request.TotalWaterLiters.HasValue)
                    sb.AppendLine($"- Empreinte eau totale : {request.TotalWaterLiters:F1} L");
                if (!string.IsNullOrEmpty(request.EcoScore))
                    sb.AppendLine($"- Éco-score : {request.EcoScore}");

                if (request.Ingredients.Count > 0)
                {
                    sb.AppendLine("- Ingrédients :");
                    foreach (var ing in request.Ingredients.OrderByDescending(i => i.CarbonPercentage).Take(10))
                    {
                        sb.AppendLine($"  • {ing.Name}: {ing.QuantityGrams}g, {ing.CarbonContributionKg:F3} kg CO₂ ({ing.CarbonPercentage:F1}%), {ing.WaterContributionLiters:F1} L eau");
                    }
                }
            }

            return sb.ToString();
        }
    }
}
