using Godot;
using System;
using System.IO;
using System.Text;

public static class GameLogger
{
    private static readonly object Sync = new();
    private static string logPath;

    public static string LogPath => logPath ?? ProjectSettings.GlobalizePath("user://logs");

    public static void Initialize()
    {
        lock (Sync)
        {
            try
            {
                string directory = ProjectSettings.GlobalizePath("user://logs");
                Directory.CreateDirectory(directory);
                logPath = Path.Combine(directory, $"game-{DateTime.Now:yyyy-MM-dd}.log");
                WriteLine("INFO", "日志会话开始");
                WriteLine("INFO", $"日志文件：{logPath}");
            }
            catch (Exception exception)
            {
                GD.PushError($"无法初始化游戏日志：{exception.Message}");
            }
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warning(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            if (string.IsNullOrEmpty(logPath)) return;
            WriteLine(level, message);
        }
    }

    private static void WriteLine(string level, string message)
    {
        string line = $"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{level}] {message}{System.Environment.NewLine}";
        try
        {
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            GD.PushError($"写入游戏日志失败：{exception.Message}");
        }

        GD.Print(line.TrimEnd());
    }
}
