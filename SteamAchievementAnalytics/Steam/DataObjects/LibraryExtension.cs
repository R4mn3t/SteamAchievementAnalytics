using System.Collections.Immutable;

namespace SteamAchievementAnalytics.Steam.DataObjects;

public static class LibraryExtension
{
    public static List<string> GetNames(this List<Game> games)
        => games.ConvertAll<string>(g => g.Name);

    public static IEnumerable<Game> Started(this IEnumerable<Game> games)
        => games.Where(g => g.Completion > 0F);

    public static IEnumerable<Game> Unfinished(this IEnumerable<Game> games)
        => games.Where(g => g.Completion < 100F);

    public static IEnumerable<Game> SortedByCompletion(this IEnumerable<Game> game, bool asc)
    {
        var copy = new Game[game.Count()];
        game.ToList().CopyTo(copy);
        var list = copy.ToList();
        list.Sort((g1, g2) =>
        {
            if (g1.Completion < g2.Completion) return asc ? -1 : 1;
            if (g1.Completion > g2.Completion) return asc ? 1 : -1;
            return 0;
        });
        return list.ToImmutableArray();
    }

    public static IEnumerable<Game> SortedByDifficulty(this IEnumerable<Game> game, bool asc)
    {
        var copy = new Game[game.Count()];
        game.ToList().CopyTo(copy);
        var list = copy.ToList();
        list.Sort((g1, g2) =>
        {
            if (g1.Difficulty < g2.Difficulty) return asc ? -1 : 1;
            if (g1.Difficulty > g2.Difficulty) return asc ? 1 : -1;
            return 0;
        });
        return list;
    }
    public static float? TotalCompletion(this IEnumerable<Game> games)
        => games.Where(g => g.Completion is not null && g.Completion != 0F).Average(g => g.Completion ?? 0);
}