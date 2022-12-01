using System.Net.Http.Json;
using Newtonsoft.Json;
using SteamAchievementAnalytics.Steam.DataObjects;

namespace SteamAchievementAnalytics;
// check input params

internal static class Program
{
    private static Library _userLibrary = new Library();
    private static bool _isHuman;

    private static async Task Main(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            int cache = i; // to cache the i index in case multiple arguments are given the the procedures
            if (!args[i].StartsWith("-"))
                continue;
            // code below will only be run if args[i] start with '-'

            switch (args[i][1..].ToLower()) // Substring(1)
            {
                case "h":
                case "-help":
                    PrintHelp();
                    break;
                case "hu":
                case "-human":
                    _isHuman = true;
                    break;
                case "la":
                case "-load-api":
                    if (args.Length - i < 3)
                    {
                        Console.WriteLine("missing parameters after {0}", args[i]);
                        return;
                    }

                    i += 2; // moved argument pointer 2 ahead since it expects 2 params for this function
                    try
                    {
                        bool hasGames = await GetFromApi(args[cache + 1], args[cache + 2]);
                        if (!hasGames)
                            Console.WriteLine("This steam user might have no games!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            "Something went wrong accessing the api! Did you provide valid arguments?\n{0}", ex);
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

                    i += 1; // moved argument pointer 1 ahead since it expects 1 params for this function
                    DumpToFile(args[cache + 1]);
                    break;
                case "ds":
                case "-dataset":

                    if (args.Length - i < 1)
                    {
                        Console.WriteLine("missing parameters after {0}", args[i]);
                        return;
                    }

                    i += 1; // moved argument pointer 1 ahead since it expects 1 params for this function
                    Datasheet(args[cache + 1]);
                    break;
                default:
                    Console.WriteLine("Unrecognized argument: {0}", args[i]);
                    break;
            }
        }
    }

    /// <summary>
    /// Read the given file and fills the global library object
    /// </summary>
    /// <param name="cacheFile">file path</param>
    /// <returns></returns>
    private static bool GetFromCache(string cacheFile)
    {
        using var sr = new StreamReader(cacheFile);
        _userLibrary = JsonConvert.DeserializeObject<Library>(sr.ReadToEnd()) ?? new Library();
        return !_userLibrary.Equals(new Library());
    }

    /// <summary>
    /// pull data from the steam api to create the global library object
    /// </summary>
    /// <param name="key">steam api key</param>
    /// <param name="userId">steam user id</param>
    /// <returns></returns>
    private static async Task<bool> GetFromApi(string key, string userId)
    {
        // save cursor position (only relevant if IsHuman is set)
        (int left, int top) cursor = Console.GetCursorPosition();

        HttpClient client = new();

        #region Games

        string userGamesUrl = Global.SteamAPI.GetUserGamesUrl(key, userId);

        // Send request to the api for all games
        var userGamesResponse = await client.SendAsync(Global.Http.Get(userGamesUrl));

        // Response Object
        var userGamesObject = JsonConvert
                                  .DeserializeObject<Steam.API.userGames.UserGames>(await userGamesResponse
                                      .Content
                                      .ReadAsStringAsync()) ??
                              new Steam.API.userGames.UserGames();

        // Check if the response object contains data
        if (userGamesObject.Equals(new Steam.API.userGames.UserGames()))
            return false;

        #endregion // Games

        for (var index = 0; index < userGamesObject.response.games.Length; index++)
        {
            var apiGame = userGamesObject.response.games[index];

            Game game = new Game();
            game.Id = apiGame.appid;

            string achievementUrl = Global.SteamAPI.GetGameAchievementUrl(key, userId, apiGame.appid.ToString());

            // Send request for all achievements of a given game
            var gameAchievementResponse = await client.SendAsync(Global.Http.Get(achievementUrl));

            // Response Object
            var gameAchievementObject = await gameAchievementResponse.Content
                .ReadFromJsonAsync<Steam.API.achievements.userAchievements.Achievements>();

            // Check if the response object contains data
            if (gameAchievementObject is null)
                continue;

            #region Name

            game.Name = gameAchievementObject.playerstats.gameName;

            #endregion

            #region Achievements

            string globalAchievementUrl = Global.SteamAPI.GetgameGlobalAchievementsUrl(apiGame.appid.ToString());
            var globalAchievementResponse = await client.SendAsync(Global.Http.Get(globalAchievementUrl));

            // Send request for all completion rates of the achievements for a given game
            var globalAchievementsObject = await globalAchievementResponse.Content
                .ReadFromJsonAsync<Steam.API.achievements.globalAchievements.Achievements>();

            if (!gameAchievementObject.playerstats.success || gameAchievementObject.playerstats.achievements is null)
                continue;

            // Write Achievements into the game object (for the library)
            foreach (var apiAchievement in gameAchievementObject.playerstats.achievements)
            {
                var achievement1 = apiAchievement;
                Achievement achievement = new()
                {
                    Achieved = apiAchievement.achieved == 1,
                    Percent = globalAchievementsObject.achievementpercentages.achievements
                        .First(a => a.name == achievement1.apiname).percent,
                    Name = apiAchievement.apiname
                };

                game.Achievements.Add(achievement);
            }

            #endregion

            // Add game to the library
            _userLibrary.Games.Add(game);

            if (!_isHuman)
                continue;

            // Human output ("Update Text")
            Console.SetCursorPosition(cursor.left, cursor.top);

            Console.WriteLine(
                "Loading games: {0}/{1} {2:0.00}%"
                , index + 1
                , userGamesObject.response.game_count,
                ((index + 1) / (float) userGamesObject.response.game_count * 100F));
        }

        Console.SetCursorPosition(cursor.left, cursor.top);
        return true;
    }

    /// <summary>
    /// uses the global library object to print out data based on <paramref name="type"/>.
    /// </summary>
    /// <param name="type">typw of datasheet</param>
    private static void Datasheet(string type)
    {
        switch (type)
        {
            case "c":
            case "completion":
            case "comp":
                Console.Write(_userLibrary.TotalCompletion());
                break;
            case "l":
            case "list":
                _userLibrary.TotalNames().PrintCompletion(_userLibrary);
                break;
            case "ls":
            case "list-started":
                _userLibrary.TotalStartedNames().PrintCompletion(_userLibrary);
                break;
            case "sla":
            case "sorted-list-ascending":
                _userLibrary.TotalNamesSortedByCompletion(true).PrintCompletion(_userLibrary);
                break;
            case "sld":
            case "sorted-list-descending":
                _userLibrary.TotalNamesSortedByCompletion(false).PrintCompletion(_userLibrary);
                break;
            case "slas":
            case "sorted-lst-ascending-stared":
                _userLibrary.TotalStartedNamesSortedByCompletion(true).PrintCompletion(_userLibrary);
                break;
            case "slds":
            case "sorted-lst-descending-stared":
                _userLibrary.TotalStartedNamesSortedByCompletion(false).PrintCompletion(_userLibrary);
                break;
            case "lu":
            case "list-unfinished":
                _userLibrary.TotalUnfinsishedNames().PrintCompletion(_userLibrary);
                break;
            case "slau":
            case "sorted-list-ascending-unfinished":
                _userLibrary.TotalUnfinishedNamesSortedByCompletion(true).PrintCompletion(_userLibrary);
                break;
            case "sldu":
            case "sorted-list-descending-unfinished":
                _userLibrary.TotalUnfinishedNamesSortedByCompletion(false).PrintCompletion(_userLibrary);
                break;
            case "ld":
            case "list-difficulty":
                _userLibrary.TotalUnfinsishedNames().PrintDifficulty(_userLibrary);
                break;
            case "slad":
            case "sorted-list-ascending-difficulty":
                _userLibrary.TotalUnfinishedNamesSortedByDifficulty(true).PrintDifficulty(_userLibrary);
                break;
            case "sldd":
            case "sorted-list-descending-difficulty":
                _userLibrary.TotalUnfinishedNamesSortedByDifficulty(false).PrintDifficulty(_userLibrary);
                break;
            default:
                Console.WriteLine("Unable to find process {0}", type);
                break;
        }
    }

    /// <summary>
    /// Prints out help textwall
    /// </summary>
    private static void PrintHelp()
    {
        Console.Write(@"Args
-----
-h --help                               Displays this info
-la --load-api      [apikey] [userid]   Loads user game data with the achievements from the api
-lf --load-file     [file]              Loads user game data from a given file
-d --dump           [file]              Dumps the user game data to a file (for -lf)
-ds --dataset       [dataset]           Outputs data 
-hu --human                             Get Human info (e.g. total games loaded from the api)                       

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

ld list-difficulty                      Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty]
slad sorted-list-ascending-difficulty   Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty ascending
sldd sorted-list-descending-difficulty  Prints out unfinished games (min. 1 achievement) with the completion of that game [game]=[difficulty] sorted by difficulty descending

Examples:
-la [Key] [userid] -d cache.json -ds slas
-lf cache.json -ds sldu 
");
    }

    /// <summary>
    /// Serializes global Library object and writes it to the given file
    /// </summary>
    /// <param name="fileName">file path</param>
    private static void DumpToFile(string fileName)
    {
        using var streamWriter = new StreamWriter(string.Format(fileName));
        streamWriter.Write(JsonConvert.SerializeObject(_userLibrary));
    }
}

internal static class Extension
{
    /// <summary>
    /// Print data to the Console
    /// </summary>
    /// <param name="data"></param>
    /// <param name="library"></param>
    public static void PrintCompletion(this List<string> data, Library library)
        => data.ForEach(n => Console.WriteLine("{0}={1}", n, library.CompletionByName(n)));

    /// <summary>
    /// Print data to the Console
    /// </summary>
    /// <param name="data"></param>
    /// <param name="library"></param>
    public static void PrintDifficulty(this List<string> data, Library library)
        => data.ForEach(n => Console.WriteLine("{0}={1}", n, library.DifficultyByName(n)));
}