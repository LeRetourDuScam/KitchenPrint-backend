using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TIBG.Models
{
    public class ChatMessage
    {
        [Required]
        public string Role { get; set; } = string.Empty; 

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime? Timestamp { get; set; }
    }

    public class HealthChatRequest
    {
        [Required]
        public int ScorePercentage { get; set; }

        [Required]
        public string HealthLevel { get; set; } = string.Empty;

        public List<string> WeakCategories { get; set; } = new List<string>();

        public List<string> RiskFactors { get; set; } = new List<string>();

        public List<string> RecommendedExercises { get; set; } = new List<string>();

        public List<string> Recommendations { get; set; } = new List<string>();

        [Required]
        public List<ChatMessage> ConversationHistory { get; set; } = new List<ChatMessage>();

        [Required]
        public string UserMessage { get; set; } = string.Empty;

        public string Language { get; set; } = "fr";
    }

    public class ChatResponse
    {
        [Required]
        public string Message { get; set; } = string.Empty;
    }

    // Detailed user health profile for personalized exercises
    public class UserHealthProfile
    {
        // Personal Info
        public string AgeRange { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;

        // Cardiovascular
        public bool HasHeartCondition { get; set; }
        public bool HasFamilyHeartHistory { get; set; }
        public string HasHighBloodPressure { get; set; } = string.Empty;
        public string ChestPainFrequency { get; set; } = string.Empty;
        public string BreathlessnessFrequency { get; set; } = string.Empty;

        // Cardio Endurance (physical tests)
        public string CardioEndurance { get; set; } = string.Empty;

        // Musculoskeletal
        public bool HasJointProblems { get; set; }
        public string BackPainFrequency { get; set; } = string.Empty;
        public string JointPainFrequency { get; set; } = string.Empty;
        public string MobilityLevel { get; set; } = string.Empty;

        // Physical Fitness Tests
        public string SquatAbility { get; set; } = string.Empty;
        public string OverheadLiftAbility { get; set; } = string.Empty;
        public string CarryingAbility { get; set; } = string.Empty;
        public string BalanceLevel { get; set; } = string.Empty;
        public string SittingRisingAbility { get; set; } = string.Empty;
        public string GripStrength { get; set; } = string.Empty;

        // Respiratory
        public bool HasRespiratoryCondition { get; set; }
        public string BreathingDifficulty { get; set; } = string.Empty;

        // Metabolic
        public string DiabetesStatus { get; set; } = string.Empty;
        public string WeightCategory { get; set; } = string.Empty;
        public double? HeightCm { get; set; }
        public double? WeightKg { get; set; }
        public double? Bmi { get; set; }

        // Lifestyle
        public string SmokingStatus { get; set; } = string.Empty;
        public string SleepHours { get; set; } = string.Empty;
        public string AlcoholConsumption { get; set; } = string.Empty;
        public string DietQuality { get; set; } = string.Empty;

        // Physical Activity
        public string WeeklyExerciseFrequency { get; set; } = string.Empty;
        public string StairsCapacity { get; set; } = string.Empty;
        public string LastRegularExercise { get; set; } = string.Empty;
        public string DailySittingHours { get; set; } = string.Empty;

        // Mental Health
        public string StressLevel { get; set; } = string.Empty;
        public string AnxietyFrequency { get; set; } = string.Empty;
        public string MotivationLevel { get; set; } = string.Empty;
    }

    public class CategoryScoreDetail
    {
        public string Category { get; set; } = string.Empty;
        public string CategoryLabel { get; set; } = string.Empty;
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int Percentage { get; set; }
    }

    public class ExercisesRequest
    {
        [Required]
        public int ScorePercentage { get; set; }

        [Required]
        public string HealthLevel { get; set; } = string.Empty;

        public List<string> WeakCategories { get; set; } = new List<string>();

        public List<string> RiskFactors { get; set; } = new List<string>();

        public List<string> Recommendations { get; set; } = new List<string>();

        public List<CategoryScoreDetail> CategoryScores { get; set; } = new List<CategoryScoreDetail>();

        public UserHealthProfile UserProfile { get; set; } = new UserHealthProfile();

        public string Language { get; set; } = "fr";
    }

    public class ExerciseAi
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string Repetitions { get; set; } = string.Empty;
        public int Sets { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public List<string> Benefits { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
        public List<string> Equipment { get; set; } = new List<string>();
    }

    public class ExercisesResponse
    {
        public List<ExerciseAi> Exercises { get; set; } = new List<ExerciseAi>();
    }
}
