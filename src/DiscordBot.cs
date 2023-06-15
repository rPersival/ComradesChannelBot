using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace ComradesChannelBot
{
    public class DiscordBot
    {
        public static DiscordSocketClient Client { get; private set; }

        public static async Task Initialize(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) throw new ArgumentNullException(nameof(token));
            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000,
                GatewayIntents = GatewayIntents.All
            });
            await Client.LoginAsync(TokenType.Bot, token);
            await Client.StartAsync();
        }
    }
}