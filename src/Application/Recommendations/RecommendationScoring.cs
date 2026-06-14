namespace StartupConnect.Application.Recommendations;

public static class RecommendationScoring
{
    public const int SkillMatchPoints = 50;
    public const int ExperiencePoints = 15;
    public const int OpenRolePoints = 15;
    public const int ProfileCompletenessPoints = 15;
    public const int HistoryPoints = 10;
    public const int SavedBehaviorPoints = 5;

    public static int ClampScore(int score) => Math.Clamp(score, 0, 100);

    public static double SkillMatchRatio(int matchedSkills, int requiredSkills)
    {
        if (requiredSkills <= 0)
        {
            return 0;
        }

        return Math.Clamp((double)matchedSkills / requiredSkills, 0, 1);
    }

    public static int SkillMatchScore(int matchedSkills, int requiredSkills)
    {
        return (int)Math.Round(SkillMatchRatio(matchedSkills, requiredSkills) * SkillMatchPoints);
    }
}
