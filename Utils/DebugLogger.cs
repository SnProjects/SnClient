using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace SnClient.Utils;

public class DebugLogger : INotifyPropertyChanged
{
    private static DebugLogger m_instance = new DebugLogger();
    private StringBuilder logBuilder = new StringBuilder();
    private const int MaxLines = 50; // Limit to prevent memory issues

    public static DebugLogger Instance => m_instance;

    private string _debugLogText;
    public string DebugLogText
    {
        get => _debugLogText;
        private set
        {
            _debugLogText = value;
            OnPropertyChanged();
        }
    }

    private DebugLogger() { }

    public static void Log(string message)
    {
        Trace.WriteLine(message);
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        m_instance.logBuilder.AppendLine($"[{timestamp}] {message}");

        // Keep only the last MaxLines
        var lines = m_instance.logBuilder.ToString().Split('\n');
        if (lines.Length > MaxLines)
        {
            m_instance.logBuilder.Clear();
            m_instance.logBuilder.Append(string.Join("\n", lines.Skip(lines.Length - MaxLines)));
        }

        m_instance.DebugLogText = m_instance.logBuilder.ToString();
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}