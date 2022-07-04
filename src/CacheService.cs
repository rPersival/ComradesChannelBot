using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ComradesChannelBot
{
    public static class CacheService
    {
        public static int СurrentlyCreated { get; set; }
        public static ulong UserId { get; set; } = 0;
        public static string ChannelName { get; set; } = null;
        public static ulong ChannelId { get; set; } = 0;

        public static Dictionary<string, ulong> emotes = new Dictionary<string, ulong>();
        public static ulong WelcomeMessageId { get; set; }
        public static IConfigurationRoot Lang { get; set; } = Configuration.English;
        public static string Code { get; set; } = "529386";
    }
}