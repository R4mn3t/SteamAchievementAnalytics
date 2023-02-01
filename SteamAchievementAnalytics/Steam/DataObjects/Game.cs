using SteamAchievementAnalytics.Steam.API.achievements.userAchievements;

namespace SteamAchievementAnalytics.Steam.DataObjects;

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

    private float? _difficulty;

    public float? Difficulty
    {
        get
        {
            if (Achievements.Count != 0)
                _difficulty ??= CalculateDifficulty();
            return _difficulty;
        }
    }

    private float CalculateCompletion()
        => (float)Achievements.Average(a => a.Achieved ? 100M: 0);

    private float CalculateDifficulty()
    {
        var notAchieved = Achievements.Where(g => !g.Achieved);
        if (!notAchieved.Any())
            return 100F;
        return notAchieved.Average(a => a.Percent);
    }

    public static Game Empty => new Game();
}