using SteamAchievmentAnalytics.Steam.API.achievements.userAchievements;

namespace SteamAchievmentAnalytics.Steam.DataObjects;

public class Game
{
    public Game()
    {
        Achievements = new List<Achievement>();
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public List<Achievement> Achievements { get; set; }

    private float? _completion;

    public float? Completion
    {
        get
        {
            if (Achievements.Count != 0)
                _completion ??= CalculateCompletion();
            return _completion;
        }
    }

    private float CalculateCompletion()
    {
        int totalAchieved = 0;
        foreach (Achievement achievement in Achievements)
        {
            if (achievement.Achieved)
                totalAchieved++;
        }

        if (totalAchieved == 0)
            return 0F;

        return (float) totalAchieved / Achievements.Count;
    }
}