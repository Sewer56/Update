using System;
using ShellProgressBar;

namespace Sewer56.Update.Tool.Utility;

public class DummyProgressBar : IProgressBar
{
    public void Dispose() { }

    public ChildProgressBar Spawn(int maxTicks, string message, ProgressBarOptions options = null) { return null; }

    public void Tick(string message = null) { }

    public void Tick(int newTickCount, string message = null) { }

    public void WriteLine(string message) { }

    public void WriteErrorLine(string message) { }

    public IProgress<T> AsProgress<T>(Func<T, string> message = null, Func<T, double?> percentage = null)
    {
        return new Progress<T>();
    }

    public int MaxTicks { get; set; }
    public string Message { get; set; }
    public double Percentage { get; }
    public int CurrentTick { get; }
    public ConsoleColor ForegroundColor { get; set; }
}