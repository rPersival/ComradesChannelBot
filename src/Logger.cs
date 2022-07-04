using System;
using System.IO;
using System.Threading.Tasks;
using Discord;

namespace ComradesChannelBot
{
    public class Logger
    {
        private static string _logDirectory { get; set; }
        private static string _logFile => Path.Combine(_logDirectory, $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}.txt");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public static void Initialize()
        {
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            DiscordBot.Client.Log += OnLogAsync;
        }

        private static Task OnLogAsync(LogMessage msg)
        {
            /* if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
                File.Create(_logFile).Dispose(); */

            string logText = $"{DateTime.UtcNow.ToString("hh:mm:ss")}z [{msg.Severity}] {msg.Source}: {msg.Exception?.ToString() ?? msg.Message}";
            //File.AppendAllText(_logFile, logText + "\n");     // Write the log text to a file

            return Console.Out.WriteLineAsync(logText);       // Write the log text to the console
        }

        public static void Log(string message)
        {
            /* if (!Directory.Exists(_logDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(_logDirectory);
            if (!File.Exists(_logFile))               // Create today's log file if it doesn't exist
                File.Create(_logFile).Dispose(); */

            string logtext = $"{DateTime.UtcNow.ToString("hh:mm:ss")}z [Custom] ComradesChannelBot: {message}";
            //File.AppendAllText(_logFile, logtext + "\n");

            Console.Out.WriteLine(logtext);
        }
    }
}