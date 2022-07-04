using System.Timers;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ComradesChannelBot.Modules
{
    public class ChannelTimer
    {
        private ulong _channelId;
        private double _interval;
        public ChannelTimer(ulong channelId)
        {
            this._channelId = channelId;
        }

        public void Set(double interval)
        {
            _interval = interval;
            var timer = new Timer(interval) {AutoReset = false};
            timer.Elapsed += OnElapsed;
            timer.Enabled = true;
            Logger.Log($"Timer for {_channelId} set.");
        }

        private async void OnElapsed(object source, ElapsedEventArgs e)
        {
            var guild = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"));
            
            SocketVoiceChannel channel = DiscordBot.Client
                .GetGuild(Configuration.Root.GetValue<ulong>("guild"))
                .GetChannel(_channelId) as SocketVoiceChannel;
            
            if (channel == null)
            {
                Logger.Log("Unable to delete channel. This channel is already deleted by user.");
                CacheService.СurrentlyCreated--;
                return;
            }
            
            var embed = new EmbedBuilder
            {
                Color = Color.Orange,
                Title = CacheService.Lang["warning_title_channel"],
                Description = CacheService.Lang["channel_warning"].Replace("{channel.Name}", channel.Name)
            };
            
            if (channel.Users.Count != 0)
            {
                new ChannelTimer(_channelId).Set(_interval);
                //Logger.Log("Unable to delete channel. Users.Count != 0. Resetting timer.");
                return;
            }
            
            try
            {
                await channel.DeleteAsync();
                await guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel")).SendMessageAsync("", false, embed.Build());
                CacheService.СurrentlyCreated--;
                Logger.Log($"Channel {_channelId} deleted. Currently created: {CacheService.СurrentlyCreated}.");
            }
            catch
            {
                //Logger.Log("Unable to delete channel. This channel is already deleted by user.");
                //CacheService.СurrentlyCreated--;
            }
        }
    }
}