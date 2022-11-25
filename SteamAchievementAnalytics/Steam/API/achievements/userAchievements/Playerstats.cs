namespace SteamAchievmentAnalytics.Steam.API.achievements.userAchievements;

public class Playerstats
{
    public string steamID { get; set; }
    public string gameName { get; set; }
    public Achievement[] achievements { get; set; }
    public bool success { get; set; }
    public string error { get; set; }
}