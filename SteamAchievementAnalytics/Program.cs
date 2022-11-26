using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
        int cache = 0;
        for (int i = 0; i < args.Length; i++)
        {
            cache = i;
            if (args[i].StartsWith("-"))
                switch (args[i].Substring(1).ToLower())
                {
                    case "h":
                    case "-help":

                        PrintHelp();

                        break;

                    case "la":
                    case "-load-api":

                        if (args.Length - i < 3)
                        {
                            Console.WriteLine("missing parameters after {0}", args[i]);
                            return;
                        }

                        i += 2;
                        try
                        {
                            bool hasNoGames = await GetFromApi(args[cache + 1], args[cache + 1]);
                            if (hasNoGames)
                                Console.WriteLine("This steam user might have no games!");
                        }
                        catch
                        {
                            Console.WriteLine(
                                "Something went wrong accessing the api! Did you provide valid arguments?");
                        }

                        break;

                    case "lf":
                    case "-load-file":

                        if (args.Length - i < 2)
                            return;

                        i += 1;
                        
                        if (!GetFromCache(args[cache + 1]))
                            Console.WriteLine("Unable to load file");

                        break;
                    case "d":
                    case "-dump":

                        if (args.Length - i < 1)
                        {
                            Console.WriteLine("missing parameters after {0}", args[i]);
                            return;
                        }

                        i += 1;

                        DumpToFile(args[cache + 1]);

                        break;
                    case "p":
                    case "-process":

                        if (args.Length - i < 1)
                        {
                            Console.WriteLine("missing parameters after {0}", args[i]);
                            return;
                        }

                        i += 1;

                        Process(args[cache + 1]);
                        break;
                }
        }

        // [params check]
        if (args.Length < 1)
            return;
        if (!args[0].StartsWith('-'))
            return;
        return;
    }

    private static bool GetFromCache(string cacheFile)
    {
        using StreamReader sr = new StreamReader(cacheFile);
        library = JsonConvert.DeserializeObject<Library>(sr.ReadToEnd()) ?? new Library();
        return !library.Equals(new Library());
    }

    private static async Task<bool> GetFromApi(string key, string userId)
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
            return false;


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

        return true;
    }

    private static void Process(string type)
    {
        switch (type)
        {
            case "c":
            case "completion":
            case "comp":
                Console.Write(library.TotalCompletion());
                break;
            default:
                Console.WriteLine("Unable to find process {0}", type);
                break;
        }
    }

    private static void PrintHelp()
    {
        Console.Write(@"Args
-----
-h --help                               Displays this info
-la --load-api      [apikey] [userid]   Loads user game data with the achievements from the api
-lf --load-file     [file]              Loads user game data from a given file
-d --dump           [file]              Dumps the user game data to a file (for -lf)
-p --process        [process]           Outputs data based on a process

Processes:
c comp completion                       Prints out total completion average
Examples:
-la [Key] [userid] -d cache.json -p x1
-lf cache.json -p x2
");
    }

    private static void DumpToFile(string fileName)
    {
        using var streamWriter = new StreamWriter(string.Format(fileName));
        streamWriter.Write(JsonConvert.SerializeObject(library));
    }
}