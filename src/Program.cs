﻿using System;
using System.Threading.Tasks;
using ComradesChannelBot.Handlers;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ComradesChannelBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("ComradesChannelBot\nStarting up...");
            Configuration.InitializeRoot("configuration.json");
            Configuration.InitializeEnglish("en-us.json");
            Configuration.InitializeRussian("ru-ru.json");
            await DiscordBot.Initialize(Configuration.Root["dtoken"]);
            Logger.Initialize();
            Logger.Log("All services initialized.");
            
            DiscordBot.Client.MessageReceived += async e
                => { await UpdateHandler.OnMessageRecievedAsync(e); };
            DiscordBot.Client.UserVoiceStateUpdated += async (u, from, to)
                => { await UpdateHandler.OnUserJoinVoiceChannel(u, from, to); };
            DiscordBot.Client.ReactionAdded += async (a, b, reaction)
                => { await UpdateHandler.OnReactionAdded(a, b, reaction); };
            DiscordBot.Client.ReactionRemoved += async (a, b, reaction)
                => { await UpdateHandler.OnReactionRemoved(a, b, reaction); };

            Logger.Log("Receiving messages.");
            await Task.Delay(-1);
        }
    }
}