using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using SteamAchievmentAnalytics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
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
                            bool hasGames = await GetFromApi(args[cache + 1], args[cache + 2]);
                            if (!hasGames)
                                Console.WriteLine("This steam user might have no games!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(
                                "Something went wrong accessing the api! Did you provide valid arguments?\n{0}",
                                ex.ToString());
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
                    case "ds":
                    case "-dataset":

                        if (args.Length - i < 1)
                        {
                            Console.WriteLine("missing parameters after {0}", args[i]);
                            return;
                        }

                        i += 1;

                        Process(args[cache + 1]);
                        break;
                    default:
                        Console.WriteLine("Unrecognized argument: {0}", args[i]);
                        break;
                }
        }
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

            var apiAchievements = await gameAchievementResponse.Content
                .ReadFromJsonAsync<Steam.API.achievements.userAchievements.Achievements>();

            #region Name

            game.Name = apiAchievements.playerstats.gameName;

            #endregion

            #region Achievements

            string globalAchievementUrl = Global.SteamAPI.GetgameGlobalAchievementsUrl(apiGame.appid.ToString());
            var globalAchievementResponse = await client.SendAsync(Global.Http.Get(globalAchievementUrl));

            var globalAchievements = await globalAchievementResponse.Content
                .ReadFromJsonAsync<Steam.API.achievements.globalAchievements.Achievements>();

            if (!apiAchievements.playerstats.success || apiAchievements.playerstats.achievements is null)
                continue;

            for (int i = 0; i < apiAchievements.playerstats.achievements.Length; i++)
            {
                var apiAchievement = apiAchievements.playerstats.achievements[i];

                Achievement achievement = new Achievement();
                achievement.Achieved = apiAchievement.achieved == 1;
                achievement.Percent = globalAchievements.achievementpercentages.achievements
                    .First(a => a.name == apiAchievement.apiname).percent;
                achievement.Name = apiAchievement.apiname;

                game.Achievements.Add(achievement);
            }

            #endregion

            library.Games.Add(game);


            Console.SetCursorPosition(cursor.left, cursor.top);
            Console.WriteLine(
                "Loading games: {0}/{1} {2}%"
                , index + 1
                , apiGames.response.game_count
                , ((float) (index + 1) / (float) apiGames.response.game_count * 100F).ToString("0.00"));
        }

        Console.SetCursorPosition(cursor.left, cursor.top);
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
            case "l":
            case "list":
                library.TotalNames().PrintCompletion(library);
                break;
            case "ls":
            case "list-started":
                library.TotalStartedNames().PrintCompletion(library);
                break;
            case "sla":
            case "sorted-list-ascending":
                library.TotalNamesSortedByCompletion(true).PrintCompletion(library);
                break;
            case "sld":
            case "sorted-list-descending":
                library.TotalNamesSortedByCompletion(false).PrintCompletion(library);
                break;
            case "slas":
            case "sorted-lst-ascending-stared":
                library.TotalStartedNamesSortedByCompletion(true).PrintCompletion(library);
                break;
            case "slds":
            case "sorted-lst-descending-stared":
                library.TotalStartedNamesSortedByCompletion(false).PrintCompletion(library);
                break;
            case "lu":
            case "list-unfinished":
                library.TotalUnfinsishedNames().PrintCompletion(library);
                break;
            case "slau":
            case "sorted-list-ascending-unfinished":
                library.TotalUnfinishedNamesSortedByCompletion(true).PrintCompletion(library);
                break;
            case "sldu":
            case "sorted-list-descending-unfinished":
                library.TotalUnfinishedNamesSortedByCompletion(false).PrintCompletion(library);
                break;
            case "ld":
            case "list-difficulty":
                library.TotalUnfinsishedNames().PrintDifficulty(library);
                break;
            case "slad":
            case "sorted-list-ascending-difficulty":
                library.TotalUnfinishedNamesSortedByDifficulty(true).PrintDifficulty(library);
                break;
            case "sldd":
            case "sorted-list-descending-difficulty":
                library.TotalUnfinishedNamesSortedByDifficulty(false).PrintDifficulty(library);
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
-ds --dataset       [dataset]           Outputs data
-c --calculate      [calculation]       Calculates data and outputs the result                         

Datasets:
c comp completion                       Prints out total completion average
l list                                  Prints out all games with achievements and the completion of that game [game]=[completion]
ls list-started                         Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion]
sla sorted-list-ascending               Prints out all games with achievements and the completion of that game [game]=[completion] sorted by completion ascending
sld sorted-list-descending              Prints out all games with achievements and the completion of that game [game]=[completion] sorted by completion descending
slas sorted-list-ascending-stared       Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion ascending
slds sorted-list-descending-stared      Prints out started games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion descending

lu list-unfinished                      Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion]
slau sorted-list-ascending-unfinished   Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion ascending
sldu sorted-list-descending-unfinished  Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[completion] sorted by completion descending

lu list-difficulty                      Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty]
slad sorted-list-ascending-difficulty   Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty ascending
sldd sorted-list-descending-difficulty  Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty descending

Examples:
-la [Key] [userid] -d cache.json -ds slas
-lf cache.json -ds sldu 
");
    }

    private static void DumpToFile(string fileName)
    {
        using var streamWriter = new StreamWriter(string.Format(fileName));
        streamWriter.Write(JsonConvert.SerializeObject(library));
    }
}

internal static class Extension
{
    public static void PrintCompletion(this List<string> data, Library library)
        => data.ForEach(n => Console.WriteLine("{0}={1}", n, library.CompletionByName(n)));

    public static void PrintDifficulty(this List<string> data, Library library)
        => data.ForEach(n => Console.WriteLine("{0}={1}", n, library.DifficultyByName(n)));
}