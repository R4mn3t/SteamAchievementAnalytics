namespace SteamAchievmentAnalytics.Steam.DataObjects;

public class Library
{
    public Library()
    {
        Games = new List<Game>();
    }

    public List<Game> Games { get; set; }

    public int StartedGames()
    {
        int gameCount = Games.Count;
        float totalCompletion = 0F;
        foreach (Game game in Games)
        {
            float completion = game.Completion();
            if (completion == 0F)
                gameCount--;
        }

        return gameCount;
    }

    public float TotalCompletion()
    {
        int gameCount = Games.Count;
        float totalCompletion = 0F;
        foreach (Game game in Games)
        {
            float completion = game.Completion();
            if (completion == 0F)
            {
                gameCount--;
                continue;
            }

            totalCompletion += completion;
        }

        return totalCompletion / gameCount;
    }

    public List<string> AllNames()
    {
        List<string> names = new List<string>();
        Games.ForEach(g => names.Add(g.Name));
        return names;
    }

    public List<string> AllStartedNames()
    {
        List<string> names = new List<string>();
        foreach (Game game in Games.Where(g => g.Completion() > 0F))
        {
            if (game.Id == 791180)
            {
                throw new Exception();
            }

            names.Add(game.Name);
        }

        return names;
    }

    public float CompletionByName(string name)
        => Games.Where(g => g.Name == name).First().Completion();
}