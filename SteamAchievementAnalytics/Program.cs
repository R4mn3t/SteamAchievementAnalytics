using System.ComponentModel;
using System.Data;
using SteamAchievmentAnalytics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SteamAchievmentAnalytics.Steam.DataObjects;

namespace SteamAchievmentAnalytics;
// check input params

internal class Program
{
    public static Library library = new Library();

    private static async Task Main(string[] args)
    {
        // [params check]
        if (args.Length < 1)
            return;
        if (!args[0].StartsWith('-'))
            return;
        switch (args[0].Substring(1).ToLower())
        {
            case "h":
            case "-help":
                Help();
                break;
            case "g":
            case "-get":
                if (args.Length != 3)
                    return;
                await Get(args[1], args[2]);
                break;
            case "c":
            case "-cache":
                if (args.Length != 2)
                    return;
                Cache(args[1]);
                break;
        }

        return;
    }

    private static void Cache(string cache)
    {
        using StreamReader sr = new StreamReader(cache);
        library = JsonConvert.DeserializeObject<Library>(sr.ReadToEnd()) ?? new Library();
        if (library.Equals(new Library()))
            return;
        float? value = library.TotalCompletion();
        using StreamWriter sw = new StreamWriter("games.txt");
        var names = library.TotalStartedNamesSortedByCompletion(false);
        foreach (string name in names)
        {
            sw.WriteLine(string.Format("{0}:{1}", name, library.CompletionByName(name)));
        }

        Console.WriteLine(value);
    }

    private static async Task Get(string key, string userId)
    {
        // save cursor position
        (int left, int top) cursor = Console.GetCursorPosition();

        HttpClient client = new();
        string userGamesUrl = Global.SteamAPI.GetUserGamesUrl(key, userId);


        var userGamesReponse = await client.SendAsync(Global.Http.Get(userGamesUrl));

        var apiGames = Newtonsoft.Json.JsonConvert
                           .DeserializeObject<Steam.API.userGames.UserGames>(await userGamesReponse.Content
                               .ReadAsStringAsync()) ??
                       new Steam.API.userGames.UserGames();

        if (apiGames.Equals(new Steam.API.userGames.UserGames()))
            return;

        
        for (var index = 0; index < apiGames.response.games.Length; index++)
        {
            var apiGame = apiGames.response.games[index];

            Game game = new Game();
            game.Id = apiGame.appid;

            string achievementUrl = Global.SteamAPI.GetGameAchievementUrl(key, userId, apiGame.appid.ToString());
            var gameAchievementResponse = await client.SendAsync(Global.Http.Get(achievementUrl));

            string gameData = await gameAchievementResponse.Content.ReadAsStringAsync();
            var apiAchievements = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Steam.API.achievements.userAchievements.Achievements>(gameData);

            #region Name

            game.Name = apiAchievements.playerstats.gameName;

            #endregion

            #region Achievements

            string globalAchievementUrl = Global.SteamAPI.GetgameGlobalAchievementsUrl(apiGame.appid.ToString());
            var globalAchievementResponse = await client.SendAsync(Global.Http.Get(globalAchievementUrl));

            string globalGameData = await globalAchievementResponse.Content.ReadAsStringAsync();
            var globalAchievements = Newtonsoft.Json.JsonConvert
                .DeserializeObject<Steam.API.achievements.globalAchievements.Achievements>(globalGameData);

            if (!apiAchievements.playerstats.success)
                continue;

            for (int i = 0; i < globalAchievements.achievementpercentages.achievements.Length; i++)
            {
                var apiGlobalAchievement = globalAchievements.achievementpercentages.achievements[i];
                var apiAchievement = apiAchievements.playerstats.achievements[i];

                Achievement achievement = new Achievement();
                achievement.Achieved = apiAchievement.achieved;
                achievement.Percent = apiGlobalAchievement.percent;
                achievement.Name = apiAchievement.apiname;

                game.Achievements.Add(achievement);
            }

            #endregion

            library.Games.Add(game);


            Console.SetCursorPosition(cursor.left, cursor.top);
            Console.WriteLine(string.Format(
                "Loading games: {0}/{1} {2}% {3}"
                , index + 1
                , apiGames.response.game_count
                , ((float) (index + 1) / (float) apiGames.response.game_count * 100F).ToString("0.00")
                , apiGame.appid.ToString()));
        }

        using StreamWriter sr = new StreamWriter(string.Format("{0}.json", userId));
        sr.Write(JsonConvert.SerializeObject(library));
    }

    private static void Help()
    {
        Console.WriteLine("program -[option]\nprogram -g [key] [userId]\nprogram -c [cache]");
    }
}