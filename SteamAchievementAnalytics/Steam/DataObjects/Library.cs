using System.Linq;

namespace SteamAchievementAnalytics.Steam.DataObjects;

public class Library
{
    public Library()
    {
        Games = new List<Game>();
    }

    public List<Game> Games { get; set; }

    public float? TotalCompletion()
        => Games.Where(g => g.Completion is not null && g.Completion != 0F).Average(g => g.Completion ?? 0);
    
    public List<string> TotalNames()
        => Games.ConvertAll<string>(g => g.Name);
    
    public List<string> TotalStartedNames()
        => Games.Where(g => g.Completion > 0F).ToList().ConvertAll<string>(g => g.Name);
    
    public List<string> TotalUnfinsishedNames()
        => Games.Where(g => g.Completion is > 0F and < 100F).ToList().ConvertAll<string>(g => g.Name);
    
    public float? CompletionByName(string name)
        => Games.First(g => g.Name == name).Completion;
    
    public float? DifficultyByName(string name)
        => Games.First(g => g.Name == name).Difficulty;
    
    public List<string> TotalNamesSortedByCompletion(bool asc)
        => GamesSortedByCompletion(asc).ConvertAll<string>(g => g.Name);
    
    public List<string> TotalStartedNamesSortedByCompletion(bool asc)
        => GamesSortedByCompletion(asc).Where(g => g.Completion > 0F).ToList().ConvertAll<string>(g => g.Name);
    
    
    public List<string> TotalUnfinishedNamesSortedByCompletion(bool asc)
        => GamesSortedByCompletion(asc).Where(g => g.Completion is > 0F and < 100F).ToList().ConvertAll<string>(g => g.Name);
    
    public List<string> TotalNamesSortedByDifficulty(bool asc)
        => GamesSortedByDifficulty(asc).ConvertAll<string>(g => g.Name);
    
    public List<string> TotalUnfinishedNamesSortedByDifficulty(bool asc)
        => GamesSortedByDifficulty(asc).Where(g => g.Completion is > 0F and < 100F).ToList().ConvertAll<string>(g => g.Name);
    

    private List<Game> GamesSortedByCompletion(bool asc)
    {
        var copy = new Game[Games.Count];
        Games.CopyTo(copy);
        var list = copy.ToList();
        list.Sort((g1, g2) =>
        {
            if (g1.Completion < g2.Completion) return asc ? -1 : 1;
            if (g1.Completion > g2.Completion) return asc ? 1 : -1;
            return 0;
        });
        return list;
    }

    private List<Game> GamesSortedByDifficulty(bool asc)
    {
        var copy = new Game[Games.Count];
        Games.CopyTo(copy);
        var list = copy.ToList();
        list.Sort((g1, g2) =>
        {
            if (g1.Difficulty < g2.Difficulty) return asc ? -1 : 1;
            if (g1.Difficulty > g2.Difficulty) return asc ? 1 : -1;
            return 0;
        });
        return list;
    }

}