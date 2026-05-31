namespace IT_Project2526.Utilities;

public static class RandomDataHelper
{
    private static readonly Random _random = new Random();

    private static readonly string[] _adjectives = 
    { 
        "Critical", "Minor", "Urgent", "Strange", "Intermittent", "Persistent", "Unexpected", "Annoying", "Major", "Cosmetic" 
    };

    private static readonly string[] _nouns = 
    { 
        "Error", "Bug", "Glitch", "Failure", "Crash", "Issue", "Problem", "Defect", "Slowdown", "Outage" 
    };

    private static readonly string[] _components = 
    { 
        "Login Page", "Dashboard", "API", "Database", "Payment Gateway", "Search Function", "User Profile", "Email Service", "Reports", "Mobile App" 
    };

    private static readonly string[] _actions = 
    { 
        "failing", "crashing", "loading slowly", "timing out", "displaying wrong data", "throwing 500 error", "unresponsive", "missing buttons", "redirecting incorrectly", "freezing" 
    };

    public static string GenerateTicketTitle()
    {
        return $"{GetRandom(_adjectives)} {GetRandom(_nouns)} in {GetRandom(_components)}";
    }

    public static string GenerateTicketDescription()
    {
        return $"The {GetRandom(_components)} is {GetRandom(_actions)} when I try to access it. This happens {GetRandom(new[] { "every time", "sometimes", "rarely" })}. Please investigate.";
    }

    private static T GetRandom<T>(T[] array)
    {
        return array[_random.Next(array.Length)];
    }
}
