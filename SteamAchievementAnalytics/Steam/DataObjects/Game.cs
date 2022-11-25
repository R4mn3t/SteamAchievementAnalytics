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

    public float Completion()
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