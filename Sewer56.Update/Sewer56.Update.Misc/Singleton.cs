namespace Sewer56.Update.Misc;

/// <summary>
/// A class that holds a single instance of a specific type indefinitely.
/// </summary>
public class Singleton<T> where T : new()
{
    /// <summary/>
    public static T Instance { get; } = new T();
}