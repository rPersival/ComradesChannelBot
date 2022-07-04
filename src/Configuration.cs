using System;
using Microsoft.Extensions.Configuration;

namespace ComradesChannelBot
{
    public class Configuration
    {
        public static IConfigurationRoot Root { get; private set; }
        public static IConfigurationRoot English { get; private set; }
        public static IConfigurationRoot Russian { get; private set; }

        public static void InitializeRoot(string filename)
        {
            Root = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(filename)
                .Build();
        }
        public static void InitializeEnglish(string filename)
        {
            English = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(filename)
                .Build();
        }

        public static void InitializeRussian(string filename)
        {
            Russian = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(filename)
                .Build();
        }
    }
}