namespace SteamAchievementAnalytics.Steam.DataObjects;

public class ExternalGame
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Achievements { get; set; }
    public int Unlocked { get; set; }
}