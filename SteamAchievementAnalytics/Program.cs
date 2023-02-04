using System.Collections.Immutable;
using System.Net.Http.Json;
using Newtonsoft.Json;
using SteamAchievementAnalytics.Steam.DataObjects;

namespace SteamAchievementAnalytics;
// check input params

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Library userLibrary = new();
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
                        userLibrary = await GetFromApi(args[cache + 1], args[cache + 2]);
                        if (userLibrary.Equals(Library.Empty))
                            Console.WriteLine("Api return invalid values!");

                        if (userLibrary.Games.Count == 0)
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
                    if (!GetFromCache(args[cache + 1], out userLibrary))
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
                    DumpToFile(args[cache + 1], userLibrary);
                    break;
                case "ds":
                case "-dataset":

                    if (args.Length - i < 1)
                    {
                        Console.WriteLine("missing parameters after {0}", args[i]);
                        return;
                    }

                    i += 1; // moved argument pointer 1 ahead since it expects 1 params for this function
                    Datasheet(args[cache + 1], userLibrary);
                    break;
                default:
                    Console.WriteLine("Unrecognized argument: {0}", args[i]);
                    break;
            }
        }
    }

    /// <summary>
    /// Read the given file and sets it to the out parameter <paramref name="library"/>
    /// </summary>
    /// <param name="cacheFile">file path</param>
    /// <returns>success of the read operation</returns>
    private static bool GetFromCache(string cacheFile, out Library library)
    {
        using var sr = new StreamReader(cacheFile);
        library = JsonConvert.DeserializeObject<Library>(sr.ReadToEnd()) ?? new Library();
        return !library.Equals(new Library());
    }

    /// <summary>
    /// pull data from the steam api
    /// </summary>
    /// <param name="key">steam api key</param>
    /// <param name="userId">steam user id</param>
    /// <returns>Steam Game data. Can be null</returns>
    private static async Task<Library> GetFromApi(string key, string userId)
    {
        // save cursor position (only relevant if IsHuman is set)
        (int left, int top) cursor = Console.GetCursorPosition();

        HttpClient client = new();

        #region Games

        string userGamesUrl = Global.SteamAPI.GetUserGamesUrl(key, userId);

        // Send request to the api for all games
        var userGamesResponse = await client.SendAsync(Global.Http.Get(userGamesUrl));
        if (!userGamesResponse.IsSuccessStatusCode)
            return Library.Empty;
        
        // Response Object
        var userGamesObject = JsonConvert
                                  .DeserializeObject<Steam.API.userGames.UserGames>(await userGamesResponse
                                      .Content
                                      .ReadAsStringAsync()) ??
                              new Steam.API.userGames.UserGames();

        // Check if the response object contains data
        if (userGamesObject.Equals(new Steam.API.userGames.UserGames()))
            return Library.Empty;

        #endregion // Games

        List<Task<Game>> games = new();
        for (var index = 0; index < userGamesObject.response.games.Length; index++)
        {
            var apiGame = userGamesObject.response.games[index];
            games.Add(GetGameAsync(client, apiGame.appid, key, userId));
        }

        await Task.WhenAll(games);
        Library library = new Library();
        foreach (var game in games)
        {
            var g = await game;
            if (g.Equals(Game.Empty))
                continue;
            library.Games.Add(g);
        }

        Console.SetCursorPosition(cursor.left, cursor.top);
        return library;
    }

    private static async Task<Game> GetGameAsync(HttpClient client, int appId, string apikey, string userId)
    {
        Game game = new Game();
        game.Id = appId;

        string achievementUrl = Global.SteamAPI.GetGameAchievementUrl(apikey, userId, appId.ToString());

        // Send request for all achievements of a given game
        var gameAchievementResponse = await client.SendAsync(Global.Http.Get(achievementUrl));

        // Check if response is good
        if (!gameAchievementResponse.IsSuccessStatusCode)
            return Game.Empty;

        // Response Object
        var gameAchievementObject = await gameAchievementResponse.Content
            .ReadFromJsonAsync<Steam.API.achievements.userAchievements.Achievements>();

        // Check if the response object contains data
        if (gameAchievementObject is null)
            return Game.Empty;

        #region Name

        game.Name = gameAchievementObject.playerstats.gameName;

        #endregion

        #region Achievements

        string globalAchievementUrl = Global.SteamAPI.GetgameGlobalAchievementsUrl(appId.ToString());
        var globalAchievementResponse = await client.SendAsync(Global.Http.Get(globalAchievementUrl));

        if (!globalAchievementResponse.IsSuccessStatusCode)
            return Game.Empty;

        // Send request for all completion rates of the achievements for a given game
        var globalAchievementsObject = await globalAchievementResponse.Content
            .ReadFromJsonAsync<Steam.API.achievements.globalAchievements.Achievements>();

        if (!gameAchievementObject.playerstats.success || gameAchievementObject.playerstats.achievements is null)
            return Game.Empty;


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

        return game;
    }

    /// <summary>
    /// uses the global library object to print out data based on <paramref name="type"/>.
    /// </summary>
    /// <param name="type">type of datasheet</param>
    private static void Datasheet(string type, Library library)
    {
        IEnumerable<Game> games = library.Games;

        foreach (char c in type)
        {
            switch (c)
            {
                case 'c':
                    Console.WriteLine($"AverageCompletion={games.TotalCompletion()}");
                    break;
                case 'u':
                    games = games.Unfinished();
                    break;
                case 'a':
                    games = games.Started();
                    break;
                case 'y':
                    games.PrintCompletion();
                    break;
                case 'x':
                    games.PrintDifficulty();
                    break;
                case 'g':
                    games = games.SortedByCompletion(true);
                    break;
                case 'h':
                    games = games.SortedByCompletion(false);
                    break;
                case 'i':
                    games = games.SortedByDifficulty(true);
                    break;
                case 'j':
                    games = games.SortedByDifficulty(false);
                    break;
                case 'z':
                    Console.WriteLine($"TotalGames={games.Count()}");
                    break;
                case 'r':
                    Console.WriteLine($"TotalAchievements={games.Sum(g=>g.Achievements.Count)}");
                    break;
                default:
                    Console.WriteLine("Unable to find datasheet value {0}", type);
                    return;
            }
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

Datasets:

Print output with
c       Print total completion
r       Print total game count
y       Print completion
x       Print difficulty
z       Print total achievement count

sort // limit the dataset with
------------------------------
u       Only Unfinished (remove all finished (100%))
a       Only Started (remove all not started (0%))
g       Sorted by completion ASC
h       Sorted by completion DESC
i       Sorted by difficulty ASC
j       Sorted by difficulty DESC

Examples:
-la [Key] [userid] -d cache.json -ds c      This will print out the total completion average.
-lf cache.json -ds auix                     This will print out a all games and there difficulty to 100% sorted by the difficulty.
");
    }

    /// <summary>
    /// Serializes global Library object and writes it to the given file
    /// </summary>
    /// <param name="fileName">file path</param>
    private static void DumpToFile(string fileName, Library library)
    {
        using var streamWriter = new StreamWriter(string.Format(fileName));
        streamWriter.Write(JsonConvert.SerializeObject(library));
    }
}

internal static class Extension
{
    /// <summary>
    /// Print data to the Console
    /// </summary>
    /// <param name="data"></param>
    /// <param name="library"></param>
    public static void PrintCompletion(this IEnumerable<Game> games)
        => games.ToList().ForEach(n => Console.WriteLine("{0}={1}", n.Name, n.Completion));

    /// <summary>
    /// Print data to the Console
    /// </summary>
    /// <param name="data"></param>
    /// <param name="library"></param>
    public static void PrintDifficulty(this IEnumerable<Game> games)
        => games.ToList().ForEach(n => Console.WriteLine("{0}={1}", n.Name, n.Difficulty));
}