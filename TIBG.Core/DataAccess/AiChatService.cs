using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using TIBG.API.Core.Configuration;
using TIBG.Models;

namespace TIBG.API.Core.DataAccess
{
    public class AiChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiChatService> _logger;
        private readonly IOptions<AiSettings> _settings;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly string _baseUrl;
        private readonly int _maxRetries;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private const string DefaultBaseUrl = "https://api.groq.com/openai/v1/chat/completions";

        public AiChatService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AiChatService> logger,
            IOptions<AiSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings;

            _apiKey = !string.IsNullOrWhiteSpace(_settings.Value.ApiKey)
                ? _settings.Value.ApiKey
                : configuration["Ai:ApiKey"] ?? throw new ArgumentException("Ai_API_KEY not configured");

            _modelName = string.IsNullOrWhiteSpace(_settings.Value.ModelName)
                ? "llama-3.3-70b-versatile"
                : _settings.Value.ModelName;

            _baseUrl = string.IsNullOrWhiteSpace(_settings.Value.BaseUrl)
                ? DefaultBaseUrl
                : _settings.Value.BaseUrl;

            _maxRetries = _settings.Value.MaxRetries > 0 ? _settings.Value.MaxRetries : 3;

            // Augmenter le timeout pour gérer le cold start de Render (peut prendre 50+ secondes)
            var timeoutSeconds = _settings.Value.TimeoutSeconds > 0 ? _settings.Value.TimeoutSeconds : 120;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Configuration de la politique de retry avec backoff exponentiel
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult(r => (int)r.StatusCode >= 500 || r.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    _maxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryAttempt}/{MaxRetries} after {Delay}s due to: {Error}",
                            retryAttempt, _maxRetries, timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });
        }

        private object[] BuildMessageHistory(string systemPrompt, List<ChatMessage> history, string userMessage)
        {
            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            foreach (var msg in history.TakeLast(10))
            {
                messages.Add(new { role = msg.Role.ToLower(), content = msg.Content });
            }

            messages.Add(new { role = "user", content = userMessage });

            return messages.ToArray();
        }

        private class AiChatResponse
        {
            public Choice[]? Choices { get; set; }

            public class Choice
            {
                public Message? Message { get; set; }
            }

            public class Message
            {
                public string? Content { get; set; }
            }
        }

        public async Task<string> GetHealthChatResponseAsync(HealthChatRequest request)
        {
            try
            {
                var systemPrompt = BuildHealthSystemPrompt(request);
                var messages = BuildMessageHistory(systemPrompt, request.ConversationHistory, request.UserMessage);

                var requestBody = new
                {
                    model = _modelName,
                    messages = messages,
                    max_completion_tokens = 800,
                    temperature = 0.7,
                    top_p = 0.9,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);

                _logger.LogInformation("Sending health chat request to AI API with model: {Model}", _modelName);

                // Utiliser la politique de retry pour la requête
                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    return await _httpClient.PostAsync(_baseUrl, content);
                });

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI API error: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"AI API returned {response.StatusCode}");
                }

                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<AiChatResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogError("AI API returned empty content for health chat");
                    throw new AiApiException("AI API returned empty content");
                }

                _logger.LogInformation("Successfully received health chat response from AI API");
                return aiResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health chat response from AI API");
                throw;
            }
        }

        private string BuildHealthSystemPrompt(HealthChatRequest request)
        {
            var languageInstruction = request.Language switch
            {
                "fr" => "Réponds TOUJOURS en français.",
                "en" => "Always respond in English.",
                _ => "Réponds TOUJOURS en français."
            };

            var weakCategories = request.WeakCategories.Any() 
                ? string.Join(", ", request.WeakCategories) 
                : "Aucune catégorie faible";

            var riskFactors = request.RiskFactors.Any() 
                ? string.Join(", ", request.RiskFactors) 
                : "Aucun facteur de risque identifié";

            var exercises = request.RecommendedExercises.Any() 
                ? string.Join(", ", request.RecommendedExercises.Take(8)) 
                : "Exercices adaptés au profil";

            var recommendations = request.Recommendations.Any() 
                ? string.Join("\n- ", request.Recommendations.Take(5)) 
                : "Suivre les conseils généraux de santé";

            return $@"Tu es FytAI, un coach santé et bien-être expert. Tu aides les utilisateurs à comprendre leurs résultats de questionnaire de santé et à améliorer leur bien-être.

            {languageInstruction}

            === RÉSULTATS DU QUESTIONNAIRE DE SANTÉ ===
            - Score global de santé: {request.ScorePercentage}%
            - Niveau de santé: {request.HealthLevel}
            - Catégories à améliorer: {weakCategories}
            - Facteurs de risque identifiés: {riskFactors}
            - Exercices recommandés: {exercises}
            - Recommandations personnalisées:
            - {recommendations}

            === TON RÔLE ===
            - Répondre aux questions sur les résultats du questionnaire de santé
            - Expliquer le score et le niveau de santé de manière bienveillante
            - Donner des conseils pratiques et personnalisés pour améliorer les catégories faibles
            - Guider l'utilisateur sur les exercices recommandés (endurance cardio-vasculaire, renforcement musculaire, équilibre, souplesse)
            - Motiver et encourager l'utilisateur dans sa démarche de santé
            - Être empathique, professionnel et encourageant
            - Garder les réponses concises (2-4 paragraphes maximum)
            - Toujours rappeler de consulter un professionnel de santé pour des problèmes médicaux
            - Lorsque la question concerne le cardio, expliquer l'importance de l'endurance cardio-vasculaire (base pour le cœur, les poumons et la circulation) et recommander des données objectifs mesurables (fréquence cardiaque, capacité d'effort)
            - Pour les questions musculaires, mentionner les recommandations OMS (2x/semaine renforcement musculaire) et proposer des tests simples (squat, porter une charge, dead hang)
            - Pour les questions métaboliques, expliquer l'IMC si pertinent (poids en kg divisé par taille en m², normal entre 18.5 et 25)
            - Pour les questions sur l'alimentation, détailler les recommandations (fruits/légumes, protéines, hydratation, limiter ultra-transformés)
            - Pour les personnes âgées, mentionner les tests d'équilibre (lever de chaise, test de Tinetti, test de Berg, sitting rising test) comme indicateurs de risque de chute

            === STYLE DE COMMUNICATION ===
            Ton: Encourageant, bienveillant et professionnel
            Approche: Pratique avec des exemples concrets, des critères quantifiables et des étapes réalisables";
        }

        public async Task<ExercisesResponse> GetRecommendedExercisesAsync(ExercisesRequest request)
        {
            try
            {
                var systemPrompt = BuildExercisesSystemPrompt(request);
                var userMessage = BuildExercisesUserMessage(request);

                var messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessage }
                };

                var requestBody = new
                {
                    model = _modelName,
                    messages = messages,
                    max_completion_tokens = 2000,
                    temperature = 0.7,
                    top_p = 0.9,
                    stream = false
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending exercises request to AI API with model: {Model}", _modelName);

                var response = await _httpClient.PostAsync(_baseUrl, content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("AI API error: {StatusCode} - {Error}", response.StatusCode, error);
                    throw new HttpRequestException($"AI API returned {response.StatusCode}");
                }

                var responseText = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<AiChatResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content;
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _logger.LogError("AI API returned empty content for exercises");
                    throw new AiApiException("AI API returned empty content");
                }

                _logger.LogInformation("Successfully received exercises response from AI API");

                // Parse JSON response
                var exercisesResponse = ParseExercisesResponse(aiResponse);
                return exercisesResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercises from AI API");
                throw;
            }
        }

        private string BuildExercisesSystemPrompt(ExercisesRequest request)
        {
            var languageInstruction = request.Language switch
            {
                "fr" => "Réponds TOUJOURS en français.",
                "en" => "Always respond in English.",
                _ => "Réponds TOUJOURS en français."
            };

            return $@"Tu es FytAI, un coach sportif et préparateur physique expert avec 20 ans d'expérience. Tu génères des programmes d'exercices HAUTEMENT personnalisés basés sur le profil de santé COMPLET de l'utilisateur.

            {languageInstruction}

            === RÈGLES STRICTES ===
            1. Tu dois TOUJOURS répondre avec un JSON valide et rien d'autre
            2. Le JSON doit contenir exactement 4 exercices PARFAITEMENT adaptés au profil spécifique de l'utilisateur
            3. ANALYSE ATTENTIVEMENT chaque donnée du profil pour personnaliser les exercices
            4. ADAPTE la difficulté, l'intensité et le type d'exercice selon:
               - Les conditions médicales (cardio, respiratoire, articulaire, diabète)
               - Le niveau d'activité actuel et l'historique sportif
               - Les douleurs et limitations physiques déclarées
               - Le mode de vie (sommeil, stress, sédentarité)
               - Le poids et l'IMC
            5. ÉVITE absolument les exercices contre-indiqués pour les conditions de l'utilisateur
            6. PRIORISE les exercices qui ciblent les catégories faibles identifiées
            7. N'utilise PAS de markdown, pas de texte explicatif, SEULEMENT le JSON

            === FORMAT DE RÉPONSE (JSON STRICT) ===
            {{
              ""exercises"": [
                {{
                  ""name"": ""Nom de l'exercice"",
                  ""description"": ""Description courte et personnalisée mentionnant pourquoi cet exercice est adapté au profil"",
                  ""duration"": ""10-15 min"",
                  ""repetitions"": ""10-12"",
                  ""sets"": 3,
                  ""category"": ""cardio|strength|flexibility|balance|breathing|relaxation|core|mobility"",
                  ""difficulty"": ""beginner|intermediate|advanced"",
                  ""benefits"": [""Bénéfice spécifique au profil 1"", ""Bénéfice spécifique 2"", ""Bénéfice 3""],
                  ""instructions"": [""Étape détaillée 1"", ""Étape 2"", ""Étape 3"", ""Étape 4""],
                  ""equipment"": [""Aucun""] ou [""Équipement nécessaire""]
                }}
              ]
            }}";
        }

        private string BuildExercisesUserMessage(ExercisesRequest request)
        {
            var profile = request.UserProfile;
            
            // Build detailed category scores
            var categoryScoresText = request.CategoryScores.Any()
                ? string.Join("\n", request.CategoryScores.Select(cs => $"   - {cs.CategoryLabel}: {cs.Percentage}% ({cs.Score}/{cs.MaxScore})"))
                : "   Aucun score détaillé";

            var weakCategories = request.WeakCategories.Any()
                ? string.Join(", ", request.WeakCategories)
                : "Aucune catégorie faible identifiée";

            var riskFactors = request.RiskFactors.Any()
                ? string.Join(", ", request.RiskFactors)
                : "Aucun facteur de risque";

            var recommendations = request.Recommendations.Any()
                ? string.Join("\n   - ", request.Recommendations.Take(5))
                : "Suivre les conseils généraux";

            // Build cardiovascular profile
            var cardiovascularProfile = $@"
   - Tranche d'âge: {TranslateAge(profile.AgeRange)}
   - Médicaments: {TranslateMedications(profile.Medications)}
   - Condition cardiaque: {(profile.HasHeartCondition ? "OUI - ATTENTION REQUISE" : "Non")}
   - Antécédents familiaux cardiaques: {(profile.HasFamilyHeartHistory ? "OUI - FACTEUR DE RISQUE" : "Non")}
   - Hypertension: {TranslateBloodPressure(profile.HasHighBloodPressure)}
   - Douleurs thoraciques: {TranslateFrequency(profile.ChestPainFrequency)}
   - Essoufflement: {TranslateFrequency(profile.BreathlessnessFrequency)}
   - Endurance cardio-vasculaire: {TranslateCardioEndurance(profile.CardioEndurance)}";

            // Build musculoskeletal profile
            var musculoskeletalProfile = $@"
   - Problèmes articulaires: {(profile.HasJointProblems ? "OUI - ADAPTER LES EXERCICES" : "Non")}
   - Douleurs dorsales: {TranslateFrequency(profile.BackPainFrequency)}
   - Douleurs articulaires: {TranslateFrequency(profile.JointPainFrequency)}
   - Capacité de mobilité: {TranslateMobility(profile.MobilityLevel)}";

            // Build physical fitness tests profile
            var fitnessTestsProfile = $@"
   - Capacité de squat: {TranslateFitnessAbility(profile.SquatAbility)}
   - Lever charge au-dessus de la tête (10kg): {TranslateFitnessAbility(profile.OverheadLiftAbility)}
   - Port de courses sur 100m: {TranslateFitnessAbility(profile.CarryingAbility)}
   - Niveau d'équilibre: {TranslateBalanceLevel(profile.BalanceLevel)}
   - Sitting Rising Test (se relever du sol): {TranslateFitnessAbility(profile.SittingRisingAbility)}
   - Force de préhension (Dead Hang/Grip): {TranslateGripStrength(profile.GripStrength)}";

            // Build respiratory profile
            var respiratoryProfile = $@"
   - Condition respiratoire: {(profile.HasRespiratoryCondition ? "OUI - EXERCICES DOUX RECOMMANDÉS" : "Non")}
   - Difficultés respiratoires: {TranslateFrequency(profile.BreathingDifficulty)}";

            // Build metabolic profile
            var bmiInfo = profile.Bmi.HasValue ? $"{profile.Bmi.Value:F1}" : "Non renseigné";
            var heightInfo = profile.HeightCm.HasValue ? $"{profile.HeightCm.Value} cm" : "Non renseigné";
            var weightInfo = profile.WeightKg.HasValue ? $"{profile.WeightKg.Value} kg" : "Non renseigné";
            var metabolicProfile = $@"
   - Diabète: {TranslateDiabetes(profile.DiabetesStatus)}
   - Catégorie de poids/IMC: {TranslateWeight(profile.WeightCategory)}
   - IMC calculé: {bmiInfo} (normal: 18.5-25, surpoids: 25-30, obésité: >30)
   - Taille: {heightInfo}
   - Poids: {weightInfo}";

            // Build lifestyle profile
            var lifestyleProfile = $@"
   - Tabagisme: {TranslateSmoking(profile.SmokingStatus)}
   - Sommeil: {TranslateSleep(profile.SleepHours)}
   - Consommation d'alcool: {TranslateAlcohol(profile.AlcoholConsumption)}
   - Qualité de l'alimentation: {TranslateDiet(profile.DietQuality)}";

            // Build physical activity profile
            var activityProfile = $@"
   - Exercice hebdomadaire: {TranslateExerciseFrequency(profile.WeeklyExerciseFrequency)}
   - Capacité à monter les escaliers: {TranslateStairs(profile.StairsCapacity)}
   - Dernière pratique sportive régulière: {TranslateLastExercise(profile.LastRegularExercise)}
   - Heures assis par jour: {TranslateSitting(profile.DailySittingHours)}";

            // Build mental health profile
            var mentalProfile = $@"
   - Niveau de stress: {TranslateStress(profile.StressLevel)}
   - Fréquence d'anxiété: {TranslateAnxiety(profile.AnxietyFrequency)}
   - Niveau de motivation: {TranslateMotivation(profile.MotivationLevel)}";

            return $@"Génère 4 exercices HAUTEMENT personnalisés pour cet utilisateur avec son profil COMPLET:

=== RÉSUMÉ SANTÉ ===
- Score global de santé: {request.ScorePercentage}%
- Niveau de santé: {request.HealthLevel}
- Catégories à améliorer: {weakCategories}
- Facteurs de risque identifiés: {riskFactors}

=== SCORES DÉTAILLÉS PAR CATÉGORIE ===
{categoryScoresText}

=== PROFIL CARDIOVASCULAIRE ==={cardiovascularProfile}

=== PROFIL MUSCULO-SQUELETTIQUE ==={musculoskeletalProfile}

=== TESTS DE CONDITION PHYSIQUE ==={fitnessTestsProfile}

=== PROFIL RESPIRATOIRE ==={respiratoryProfile}

=== PROFIL MÉTABOLIQUE ==={metabolicProfile}

=== MODE DE VIE ==={lifestyleProfile}

=== ACTIVITÉ PHYSIQUE ACTUELLE ==={activityProfile}

=== SANTÉ MENTALE ==={mentalProfile}

=== RECOMMANDATIONS PERSONNALISÉES ===
   - {recommendations}

=== CONSIGNES ===
1. Analyse CHAQUE élément du profil pour créer des exercices vraiment personnalisés
2. ADAPTE l'intensité selon le niveau d'activité et les conditions médicales
3. ÉVITE les exercices contre-indiqués (ex: pas de cardio intense si problème cardiaque)
4. CIBLE les catégories faibles avec des exercices appropriés
5. INCLUS des exercices de respiration/relaxation si stress élevé
6. PROPOSE des exercices réalisables selon la mobilité déclarée
7. Pour les catégories cardio, inclure de l'endurance cardio-vasculaire (base pour le cœur, poumons et circulation)
8. Pour les muscles, suivre les recommandations OMS (2x/semaine renforcement musculaire) et adapter selon les tests physiques
9. Pour les personnes âgées (60+), intégrer des exercices de prévention des chutes (équilibre, lever de chaise)
10. Si l'IMC est disponible, adapter l'intensité en conséquence

Réponds UNIQUEMENT avec le JSON, sans aucun texte avant ou après.";
        }

        // Helper methods for translation
        private string TranslateFrequency(string value) => value switch
        {
            "never" => "Jamais",
            "rarely" => "Rarement",
            "sometimes" => "Parfois",
            "often" => "Souvent",
            "chronic" => "Chronique",
            _ => "Non renseigné"
        };

        private string TranslateBloodPressure(string value) => value switch
        {
            "no" => "Non",
            "controlled" => "Oui, contrôlée",
            "uncontrolled" => "Oui, non contrôlée - ATTENTION",
            "unknown" => "Inconnu",
            _ => "Non renseigné"
        };

        private string TranslateMobility(string value) => value switch
        {
            "easily" => "Bonne mobilité",
            "difficulty" => "Mobilité limitée",
            "no" => "Mobilité très réduite",
            "not-tried" => "Non évalué",
            _ => "Non renseigné"
        };

        private string TranslateDiabetes(string value) => value switch
        {
            "no" => "Non",
            "prediabetes" => "Prédiabète",
            "type2-controlled" => "Diabète type 2 contrôlé",
            "type2-uncontrolled" => "Diabète type 2 non contrôlé - ATTENTION",
            "type1" => "Diabète type 1",
            _ => "Non renseigné"
        };

        private string TranslateWeight(string value) => value switch
        {
            "underweight" => "Sous-poids",
            "normal" => "Poids normal",
            "overweight" => "Surpoids",
            "obese1" => "Obésité modérée",
            "obese2" => "Obésité sévère",
            "unknown" => "Inconnu",
            _ => "Non renseigné"
        };

        private string TranslateSmoking(string value) => value switch
        {
            "never" => "Non-fumeur",
            "former" => "Ancien fumeur",
            "recent-quit" => "Arrêt récent",
            "occasional" => "Fumeur occasionnel",
            "regular" => "Fumeur régulier",
            _ => "Non renseigné"
        };

        private string TranslateSleep(string value) => value switch
        {
            "less-5" => "Moins de 5h - INSUFFISANT",
            "5-6" => "5-6h - Peu suffisant",
            "7-8" => "7-8h - Optimal",
            "9-plus" => "Plus de 9h",
            _ => "Non renseigné"
        };

        private string TranslateAlcohol(string value) => value switch
        {
            "never" => "Jamais",
            "occasional" => "Occasionnel",
            "moderate" => "Modéré",
            "regular" => "Régulier",
            "heavy" => "Excessif",
            _ => "Non renseigné"
        };

        private string TranslateDiet(string value) => value switch
        {
            "excellent" => "Excellente",
            "good" => "Bonne",
            "average" => "Moyenne",
            "poor" => "Mauvaise",
            _ => "Non renseigné"
        };

        private string TranslateExerciseFrequency(string value) => value switch
        {
            "0" => "Aucun exercice - SÉDENTAIRE",
            "1-2" => "1-2 fois/semaine",
            "3-4" => "3-4 fois/semaine",
            "5-plus" => "5+ fois/semaine - ACTIF",
            _ => "Non renseigné"
        };

        private string TranslateStairs(string value) => value switch
        {
            "easily" => "Facilement",
            "moderate" => "Avec effort modéré",
            "difficulty" => "Avec difficulté",
            "no" => "Incapable",
            _ => "Non renseigné"
        };

        private string TranslateLastExercise(string value) => value switch
        {
            "current" => "Actuellement actif",
            "less-month" => "Moins d'un mois",
            "1-6-months" => "1-6 mois",
            "6-12-months" => "6-12 mois",
            "more-year" => "Plus d'un an - REPRISE PROGRESSIVE NÉCESSAIRE",
            _ => "Non renseigné"
        };

        private string TranslateSitting(string value) => value switch
        {
            "less-4" => "Moins de 4h",
            "4-6" => "4-6h",
            "6-8" => "6-8h - Sédentarité modérée",
            "more-8" => "Plus de 8h - TRÈS SÉDENTAIRE",
            _ => "Non renseigné"
        };

        private string TranslateStress(string value) => value switch
        {
            "low" => "Faible",
            "moderate" => "Modéré",
            "high" => "Élevé",
            "very-high" => "Très élevé - RELAXATION RECOMMANDÉE",
            _ => "Non renseigné"
        };

        private string TranslateAnxiety(string value) => value switch
        {
            "never" => "Jamais",
            "few-days" => "Quelques jours",
            "more-half" => "Plus de la moitié du temps",
            "nearly-every" => "Presque tous les jours",
            _ => "Non renseigné"
        };

        private string TranslateMotivation(string value) => value switch
        {
            "always" => "Toujours motivé",
            "usually" => "Généralement motivé",
            "sometimes" => "Parfois motivé",
            "rarely" => "Rarement motivé - EXERCICES FACILES RECOMMANDÉS",
            _ => "Non renseigné"
        };

        private string TranslateAge(string value) => value switch
        {
            "18-29" => "18-29 ans",
            "30-39" => "30-39 ans",
            "40-49" => "40-49 ans",
            "50-59" => "50-59 ans",
            "60-69" => "60-69 ans - ADAPTER LES EXERCICES",
            "70-plus" => "70+ ans - PRÉVENTION DES CHUTES PRIORITAIRE",
            _ => "Non renseigné"
        };

        private string TranslateMedications(string value) => value switch
        {
            "none" => "Aucun médicament",
            "vitamins" => "Vitamines/compléments uniquement",
            "one" => "Un médicament régulier",
            "multiple" => "Plusieurs médicaments - VÉRIFIER CONTRE-INDICATIONS",
            "many" => "Nombreux médicaments - ATTENTION PARTICULIÈRE REQUISE",
            _ => "Non renseigné"
        };

        private string TranslateCardioEndurance(string value) => value switch
        {
            "excellent" => "Excellente - Capable d'effort soutenu 30+ min",
            "good" => "Bonne - Effort modéré sans difficulté",
            "average" => "Moyenne - S'essouffle après 10-15 min",
            "poor" => "Faible - S'essouffle rapidement",
            "very-poor" => "Très faible - REPRENDRE PROGRESSIVEMENT",
            _ => "Non évaluée"
        };

        private string TranslateFitnessAbility(string value) => value switch
        {
            "easily" => "Facilement",
            "with-effort" => "Avec effort",
            "difficulty" => "Avec difficulté",
            "unable" => "Incapable",
            "not-tested" => "Non testé",
            "yes" => "Oui",
            "no" => "Non",
            _ => "Non évalué"
        };

        private string TranslateBalanceLevel(string value) => value switch
        {
            "excellent" => "Excellent - Tenue unipode 30+ sec",
            "good" => "Bon - Équilibre stable",
            "moderate" => "Modéré - Quelques déséquilibres",
            "poor" => "Faible - RISQUE DE CHUTE",
            "very-poor" => "Très faible - RISQUE DE CHUTE ÉLEVÉ",
            "not-tested" => "Non évalué",
            _ => "Non renseigné"
        };

        private string TranslateGripStrength(string value) => value switch
        {
            "excellent" => "Excellente - Dead hang 60+ sec",
            "good" => "Bonne - Dead hang 30-60 sec",
            "average" => "Moyenne - Dead hang 15-30 sec",
            "poor" => "Faible - Dead hang < 15 sec",
            "unable" => "Incapable de s'accrocher",
            "not-tested" => "Non évalué",
            _ => "Non renseigné"
        };

        private ExercisesResponse ParseExercisesResponse(string aiResponse)
        {
            try
            {
                // Clean up the response - remove markdown code blocks if present
                var cleanedResponse = aiResponse.Trim();
                if (cleanedResponse.StartsWith("```json"))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```"))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }
                cleanedResponse = cleanedResponse.Trim();

                var result = JsonSerializer.Deserialize<ExercisesResponse>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new ExercisesResponse();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse exercises JSON response: {Response}", aiResponse);
                return new ExercisesResponse();
            }
        }
    }
}
