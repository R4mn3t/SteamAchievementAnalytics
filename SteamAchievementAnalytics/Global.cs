using System;
using System.Dynamic;

namespace SteamAchievmentAnalytics
{
    public static class Global
    {
        public static class SteamAPI
        {
            private static string gameAchievmentsUrl =
                "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={2}&key={0}&steamid={1}";
            public static string GetGameAchievementUrl(string apiKey, string userId, string appId)
                => string.Format(gameAchievmentsUrl, apiKey, userId, appId);
            private static string userGamesUrl =
                "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&skip_unvetted_apps=false&include_played_free_games=1";
            public static string GetUserGamesUrl(string apiKey, string userId)
                => string.Format(userGamesUrl, apiKey, userId);
            private static string globalAchievmentUrl =
                "http://api.steampowered.com/ISteamUserStats/GetGlobalAchievementPercentagesForApp/v0002/?gameid={0}/&format=json";
            public static string GetgameGlobalAchievementsUrl(string appId)
                => string.Format(globalAchievmentUrl, appId);
        }

        public static class Http
        {
            public static HttpRequestMessage Get(string url)
                => new HttpRequestMessage(HttpMethod.Get, url);
        }
    }
}