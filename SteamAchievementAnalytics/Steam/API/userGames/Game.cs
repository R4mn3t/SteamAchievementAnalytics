namespace SteamAchievementAnalytics.Steam.API.userGames;

public class Game
{
    public int appid { get; set; }
    public int playtime_forever { get; set; }
    public int playtime_windows_forever { get; set; }
    public int playtime_mac_forever { get; set; }
    public int playtime_linux_forever { get; set; }
    public int rtime_last_played { get; set; }
}