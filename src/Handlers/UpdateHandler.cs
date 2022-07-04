using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComradesChannelBot.Modules;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace ComradesChannelBot.Handlers
{
    public static class UpdateHandler
    {
        private static readonly Dictionary<string, ulong> Emotes = new Dictionary<string, ulong> {
            { "pepe_coffee", Configuration.Root.GetValue<ulong>("pepe_coffee") }
        };
        public static async Task OnMessageRecievedAsync(SocketMessage m)
        {
            SocketUserMessage msg = m as SocketUserMessage;
            if (msg == null) return;
            if (string.IsNullOrWhiteSpace(msg.Content)) return;
            if (msg.Author.Id == DiscordBot.Client.CurrentUser.Id) return;
            var context = new SocketCommandContext(DiscordBot.Client, msg);
            
            var user = context.User;
            var guild = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"));
            var thisUser = context.User as SocketGuildUser;
            var splited = context.Message.Content.Split(' ');
            var comrades = context.Guild.Roles.FirstOrDefault(x
                => x.Id == Configuration.Root.GetValue<ulong>("comrades"));

            var embed = new EmbedBuilder();

            if (msg.Channel.Id == Configuration.Root.GetValue<ulong>("authchannel"))
            {
                if (splited[0] == CacheService.Code)
                {
                    if (thisUser != null && thisUser.Roles.Contains(comrades))
                    {
                        embed.Color = Color.Orange;
                        embed.Title = CacheService.Lang["warning_title"];
                        embed.Description = context.Message.Author.Mention + CacheService.Lang["auth_warn"];
                        await context.Channel.SendMessageAsync(null, false, embed.Build());
                        await msg.DeleteAsync();
                        return;
                    }

                    await (user as IGuildUser)?.AddRoleAsync(comrades)!;
                    Logger.Log("Passed");
                    embed.Color = Color.Green;
                    embed.Title = CacheService.Lang["success_title"];
                    embed.Description = context.Message.Author.Mention + CacheService.Lang["auth_success"]
                        .Replace("{guild.Name}", guild.Name);
                    await context.Channel.SendMessageAsync(null, false, embed.Build());
                    await msg.DeleteAsync();
                    return;
                }

                embed.Color = Color.Red;
                embed.Title = CacheService.Lang["error_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["auth_error"]
                    .Replace("{MentionUtils.MentionChannel(714800261750849596)}",
                        MentionUtils.MentionChannel(714800261750849596));
                //embed.Footer = new EmbedFooterBuilder { Text = $"" };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
                return;
            }

            if (msg.Content.Contains(DiscordBot.Client.CurrentUser.Mention))
            {
                if (thisUser != null && 
                    thisUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x
                        => x.Permissions.Administrator)))
                {
                    await context.Channel.SendMessageAsync("wtf discord mod?");
                    return;
                }
                var randommessages = Configuration.Root.GetSection("randommessage").Get<string[]>();
                var randommessage = randommessages[new Random().Next(randommessages.Length)];
                await context.Channel.SendMessageAsync(randommessage);
                return;
            }
            if (!splited.First().StartsWith(Configuration.Root["prefix"])) return; //чекаем что команда
            string command = splited.First().Remove(0, 1).ToLower();
            string[] args = splited.Skip(1).ToArray();
            switch (command)
            {
                case "lang":
                    await LangCommand(context, args, 
                        string.Join(' ', splited.Skip(1)), msg);
                    break;

                case "cc":
                case "createchannel":
                    await CreateChannelCommand(context, false, args,
                        string.Join(' ', splited.Skip(1)));
                    await msg.DeleteAsync();
                    break;

                case "cgc":
                case "creategamingchannel":
                    await CreateChannelCommand(context, true, args,
                        string.Join(' ', splited.Skip(1)));
                    await msg.DeleteAsync();
                    break;
                
                case "createguild":
                case "cg":
                    await CreateGuildCommand(context, args,string.Join(' ', splited.Skip(1)));
                    break;
                
                case "code":
                    await CodeCreateCommand(context, args, string.Join(' ', splited.Skip(1)), msg);
                    break;

                case "welcome":
                    await WelcomeMessage(context, msg, args, string.Join(' ', splited.Skip(1)));
                    break;

                case "settings":
                    await SettingMessage(context, msg, args, splited[1]);
                    break;
                
                case "customrolemessage":
                case "crm":
                    await CustomRoleMessage(context, args);
                    break;
                    
                case "help":
                    await HelpCommand(context);
                    break;
                
                default:
                    var embedComm = new EmbedBuilder
                    {
                        Color = Color.Red,
                        Title = CacheService.Lang["error_title"],
                        Description = context.Message.Author.Mention + CacheService.Lang["command_doesnt_exist"]
                            .Replace("{Configuration.Root[\"prefix\"]}", Configuration.Root["prefix"])
                    };
                    await context.Channel.SendMessageAsync(null, false, embedComm.Build());
                    break;
            }
        }
        public static async Task OnUserJoinVoiceChannel(SocketUser user, SocketVoiceState state,
            SocketVoiceState state2)
        {
            // If user muted, deafened or shared screen then return
            if (state.VoiceChannel == state2.VoiceChannel || state2.VoiceChannel == null || 
                state2.VoiceChannel.Id != Configuration.Root.GetValue<ulong>("createch") ||
                state.VoiceChannel?.Id == Configuration.Root.GetValue<ulong>("createch")) return;

            var currentUser = user as SocketGuildUser;
            var guild = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"));
            if (currentUser != null)
            {
                string channelName = (string.IsNullOrWhiteSpace(currentUser.Nickname) 
                    ? user.Username : currentUser.Nickname) + "'s channel";
            
                var embedKick = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = CacheService.Lang["error_title_channel"],
                    Description = currentUser.Mention + CacheService.Lang["channel_error_existed"]
                };
            
                if (guild.Channels.FirstOrDefault(x => x is SocketVoiceChannel channel 
                                                       && channel.CategoryId == 
                                                       Configuration.Root.GetValue<ulong>("customcategory")
                                                       && channel.Name == channelName) != null) 
                {
                    await guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                        .SendMessageAsync(null, false, embedKick.Build());
                    await currentUser.ModifyAsync(x => x.Channel = null);
                    await currentUser.SendMessageAsync(null, false, embedKick.Build());
                    return;
                }
                RestVoiceChannel voiceChannel = await guild.CreateVoiceChannelAsync(channelName,
                    x => x.CategoryId = Configuration.Root.GetValue<ulong>("customcategory"));
                CacheService.СurrentlyCreated++; //таймер для канала
                new ChannelTimer(voiceChannel.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                await currentUser.ModifyAsync(x => x.ChannelId = voiceChannel.Id);
                OverwritePermissions currentPermsEvery = voiceChannel
                                                             .GetPermissionOverwrite(DiscordBot.Client
                                                                 .GetGuild(Configuration.Root.GetValue<ulong>("guild")).EveryoneRole) 
                                                         ?? new OverwritePermissions();
                await voiceChannel.AddPermissionOverwriteAsync(user,
                    currentPermsEvery.Modify(manageChannel: PermValue.Allow));
                var embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title_channel"],
                    Description = CacheService.Lang["channel_success_user"].Replace("{channelName}",
                        channelName).Replace("{currentUser.Mention}", currentUser.Mention)
                };
                await guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync(null, false, embed.Build());
            }
        }

        public static async Task OnReactionAdded(Cacheable<IUserMessage, ulong> a, ISocketMessageChannel mc,
            SocketReaction reaction) 
        {
            if (reaction.User.Value.Id == DiscordBot.Client.CurrentUser.Id) return;
            if (reaction.MessageId != CacheService.WelcomeMessageId) return;
            var sockguild = ((SocketGuildChannel)reaction.Channel).Guild;
            var user = reaction.User.Value as SocketGuildUser;
            var msg = await reaction.Channel.GetMessageAsync(reaction.MessageId);
            if (reaction.Emote.Name == "🔄")
            {
                if(user != null && !user.Roles.Contains(sockguild.Roles.FirstOrDefault(
                       x => x.Permissions.Administrator))) {
                    await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value.Id);
                    return;
                }
                
                await msg.RemoveAllReactionsAsync();
                await (msg as IUserMessage).AddReactionsAsync(
                    Emotes.Keys.Select(x => sockguild.Emotes.FirstOrDefault(e => e.Name == x)).ToArray());
                return; 
            }
            if (msg.Reactions[reaction.Emote].ReactionCount > 2) return; //if two at the same time
            if (!Emotes.ContainsKey(reaction.Emote.Name)) // if not listed delete
            {
                await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value.Id);
                return;
            } 
            
            //var role = sockguild.Roles.FirstOrDefault(r => r.Id == emotes[reaction.Emote.Name]);
            var role = sockguild.GetRole(859064656710729759); //events
            if (role == null) return;
            if (user != null && user.Roles.Contains(role)) {
                //await msg.RemoveReactionAsync(reaction.Emote, reaction.User.Value.Id);
                return;
            }

            if (user != null) await user.AddRoleAsync(role);

            //await (msg as IUserMessage).AddReactionsAsync(new List<Emoji>
            //{
            //    new Emoji("\uD83C\uDF46")
            //}.ToArray());
        }
        

        public static async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> a, ISocketMessageChannel mc,
            SocketReaction reaction)
        {
            if (reaction.User.Value.Id == DiscordBot.Client.CurrentUser.Id) return;
            if (reaction.MessageId != CacheService.WelcomeMessageId) return;
            if (!Emotes.ContainsKey(reaction.Emote.Name)) return;
            var socketGuild = ((SocketGuildChannel)reaction.Channel).Guild;
            var user = reaction.User.Value as SocketGuildUser;
            var role = socketGuild.GetRole(859064656710729759);
            if (role == null) return;
            if (user != null && user.Roles.Contains(role))
                await user.RemoveRoleAsync(role);
        }
        #region Commands
        private static async Task LangCommand(SocketCommandContext context, string[] args, string lang,
            SocketMessage msg)
        {
            var user = context.User;
            var currentUser = user as SocketGuildUser;
            var embed = new EmbedBuilder();

            if (args.Length < 1)
            {
                embed.Color = Color.Blue;
                embed.Title = CacheService.Lang["info_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["current_lang"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
                return;
            }

            if (currentUser != null && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(
                    x => x.Permissions.Administrator)))
            {
                embed.Color = Color.Red;
                embed.Title = CacheService.Lang["error_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["perms_error"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
                return;
            }
            
            switch (lang)
            {
                case "en-us":
                    CacheService.Lang = Configuration.English;
                    embed.Color = Color.Green;
                    embed.Title = CacheService.Lang["success_title"];
                    embed.Description = context.Message.Author.Mention + CacheService.Lang["lang_changed"];
                    await context.Channel.SendMessageAsync(null, false, embed.Build());
                    await msg.DeleteAsync();
                    return;

                case "ru-ru":
                    CacheService.Lang = Configuration.Russian;
                    embed.Color = Color.Green;
                    embed.Title = CacheService.Lang["success_title"];
                    embed.Description = context.Message.Author.Mention + CacheService.Lang["lang_changed"];
                    await context.Channel.SendMessageAsync(null, false, embed.Build());
                    await msg.DeleteAsync();
                    return;

                default:
                    embed.Color = Color.Red;
                    embed.Title = CacheService.Lang["error_title"];
                    embed.Description = context.Message.Author.Mention + CacheService.Lang["lang_notsupported"];
                    await context.Channel.SendMessageAsync(null, false, embed.Build());
                    await msg.DeleteAsync();
                    return;
            }
        }
        private static async Task CreateChannelCommand(SocketCommandContext context, bool gaming, string[] args,
            string name)
        {
            var guild = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"));
            name = name.Trim();
            if (CacheService.СurrentlyCreated >= Configuration.Root.GetValue<int>("limit"))
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = CacheService.Lang["error_title_channel"],
                    Description = context.Message.Author.Mention + CacheService.Lang["channel_error_limit_ch"]
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                return;
            }
            
            if (args.Length > 0)
            {
                var ch = await context.Guild.CreateVoiceChannelAsync(name, x => 
                    x.CategoryId = Configuration.Root.GetValue<ulong>(!gaming ? "category" : "gamingcategory"));
                OverwritePermissions currentPermsEvery = ch.GetPermissionOverwrite(DiscordBot.Client.GetGuild(
                    Configuration.Root.GetValue<ulong>("guild")).EveryoneRole) ?? new OverwritePermissions();
                await ch.AddPermissionOverwriteAsync(context.User, currentPermsEvery.Modify(
                    manageChannel: PermValue.Allow));
                var embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title_channel"],
                    Description = CacheService.Lang["channel_success_name"].Replace("{NameShort}", name)
                        .Replace("{context.Message.Author.Mention}", context.Message.Author.Mention)
                };
                await guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync("", false, embed.Build());
                new ChannelTimer(ch.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                CacheService.СurrentlyCreated++;
                Logger.Log($"Channel {ch.Id} created. Currently created: {CacheService.СurrentlyCreated}.");
            }
            
            else if (args.Length == 0)
            {
                var randonamesfirst = CacheService.Lang.GetSection("randomfirst").Get<string[]>();
                var randonamefirst = randonamesfirst[new Random().Next(randonamesfirst.Length)];

                var randonamessecond = CacheService.Lang.GetSection("randomsecond").Get<string[]>();
                var randonamesecond = randonamessecond[new Random().Next(randonamessecond.Length)];

                var ch = await context.Guild.CreateVoiceChannelAsync($"{randonamefirst} " +
                                                                     $"\"{randonamesecond}\"", x =>
                    x.CategoryId = Configuration.Root.GetValue<ulong>(!gaming ? "category" : "gamingcategory"));

                OverwritePermissions currentPermsEvery = ch.GetPermissionOverwrite(DiscordBot.Client.GetGuild(
                    Configuration.Root.GetValue<ulong>("guild")).EveryoneRole) ?? new OverwritePermissions();
                await ch.AddPermissionOverwriteAsync(context.User, currentPermsEvery
                    .Modify(manageChannel: PermValue.Allow));

                var embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title_channel"],
                    Description = CacheService.Lang["channel_success_randname"].Replace("{randonamefirst}"
                        , randonamefirst).Replace("{randonamesecond}", randonamesecond)
                        .Replace("{context.Message.Author.Mention}", context.Message.Author.Mention)
                };
                await guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync("", false, embed.Build());

                new ChannelTimer(ch.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                CacheService.СurrentlyCreated++;

                Logger.Log($"Channel {ch.Id} created. Currently created: {CacheService.СurrentlyCreated}.");
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = CacheService.Lang["error_title_channel"],
                    Description = CacheService.Lang["channel_error_args"]
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }
        private static async Task CreateGuildCommand(SocketCommandContext context, string[] args, string name)
        {
            if (args.Length != 1) return;
            await context.Channel.SendMessageAsync("Check");
            var rum = await context.Channel.SendMessageAsync($"*{name}*");
            await rum.AddReactionsAsync(new List<Emoji>
            {
                new Emoji("\u2705"),
                new Emoji("\u274C")
            }.ToArray()); //todo: reactions
        }
        private static async Task WelcomeMessage(SocketCommandContext context, SocketMessage msg, string[] args, 
            string id)
        {
            var user = context.User;
            var currentUser = user as SocketGuildUser;
            var embed = new EmbedBuilder();
            if (currentUser != null && !currentUser.Roles.Contains(context.Guild.Roles
                    .FirstOrDefault(x => x.Permissions.Administrator)))
            {
                embed.Color = Color.Red;
                embed.Title = CacheService.Lang["error_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["perms_error"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
            }

            else if (ulong.TryParse(id, out ulong nums))
            {
                CacheService.WelcomeMessageId = nums;
                embed.Color = Color.Green;
                embed.Title = CacheService.Lang["success_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["welcome_success"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
            }
            
            
            else if (args.Length == 0)
            {
                var entries = CacheService.Lang.GetSection("roles_selection").Get<string[]>();
                var entriesDesc = CacheService.Lang.GetSection("roles_desc").Get<string[]>();
                embed.Color = Color.Blue;
                embed.Title = CacheService.Lang["select_title"];
                var fields = new List<EmbedFieldBuilder>();
                for (int i = 0; i < entries.Length; i++) fields.Add(new EmbedFieldBuilder
                    { Name = i + 1 + ". " + entries[i] + " \n", Value = entriesDesc[i] + "\n" });
                embed.Fields = fields;
                var message = await context.Channel.SendMessageAsync(null, false, embed.Build());
                CacheService.WelcomeMessageId = message.Id;
                await message.AddReactionsAsync(
                    Emotes.Keys.Select(x => (message.Channel as SocketGuildChannel)?
                        .Guild.Emotes.FirstOrDefault(e => e.Name == x)).ToArray());
                await msg.DeleteAsync();
            }
            

            else
            {
                embed.Color = Color.Red;
                embed.Title = CacheService.Lang["error_title"];
                embed.Description = CacheService.Lang["welcome_error"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
            }
        }
        private static async Task SettingMessage(SocketCommandContext context, SocketMessage msg, string[] args, string setting)
        {
            var embed = new EmbedBuilder();
            if (!((SocketGuildUser)context.User).Roles.Contains(context.Guild.Roles.FirstOrDefault(
                    x => x.Permissions.Administrator)))
            {
                embed.Color = Color.Red;
                embed.Title = CacheService.Lang["error_title"];
                embed.Description = context.Message.Author.Mention + CacheService.Lang["perms_error"];
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
                return;
            }
            switch (setting)
            {
                case "authchannel":
                    var channel = msg.MentionedChannels.ToArray();
                    if (channel.Length != 1) return;
                    //CacheService.AuthChannel = channel[0].Id;
                    await context.Channel.SendMessageAsync("");
                    return;

                case "defrole":
                    var role = msg.MentionedRoles.ToArray();
                    if (role.Length != 1) return;
                    //CacheService.DefaultRole = role[0].Id;
                    return;

                case "logchannel":
                    var logchannel = msg.MentionedRoles.ToArray();
                    if (logchannel.Length != 1) return;
                    //CacheService.LogChannel = logchannel[0].Id;
                    return;

                case "limchannel":
                    var limchannel = args[1];
                    if (!int.TryParse(limchannel, out int lim)) return;
                    if (lim < 0 || lim > 30) return;
                    //CacheService.ChannelLim = lim;
                    return;

                case "category":
                    if (!ulong.TryParse(args[1], out _)) return;
                    //CacheService.FirstCategory = category;
                    return;

                case "gamingcategory":
                    if (args.Length == 2 && ulong.TryParse(args[1], out ulong _))
                    {
                        //CacheService.SecondCategory = gcategory;
                    }

                    else if (args.Length < 2)
                    {
                        await context.Guild.CreateCategoryChannelAsync("Gaming Category");
                        //CacheService.SecondCategory = gcategorycreated;
                    }
                    return;

                case "react":
                    Emote emote = null;
                    foreach (var t in args)
                        if (Emote.TryParse(t, out var tempEmote))
                        {
                            emote = tempEmote;
                            break;
                        }
                    var rolesem = msg.MentionedRoles.ToArray();
                    if (rolesem.Length != 1) return;
                    if (emote != null) CacheService.emotes.Add(emote.Name, rolesem[0].Id);
                    //CacheService.CustomEmotesMsgId = reactmsg.Id;
                    return;

                case "reactremove":
                    Emote emoterem = null;
                    foreach (var t in args)
                    {
                        if (Emote.TryParse(t, out var tempEmote))
                        {
                            emoterem = tempEmote;
                            break;
                        }
                    }

                    if (emoterem != null) CacheService.emotes.Remove(emoterem.Name);
                    return;

                case "reactlist":
                    string emotesList = string.Empty;
                    var emotesKey = CacheService.emotes.Keys.ToArray();
                    var emotesValues = CacheService.emotes.Values.ToArray();
                    for (int i = 0; i < CacheService.emotes.Count; i++)
                        emotesList += emotesKey[i] + " - " + emotesValues[i] + "\n";
                    await context.Channel.SendMessageAsync(emotesList);
                    return;

                case "createchannel":
                    if (args.Length == 2 && ulong.TryParse(args[1], out ulong result))
                    {
                        await context.Channel.SendMessageAsync("args[0] is ulong");
                        if (context.Guild.GetChannel(result) == null)
                        {
                            Logger.Log("Bad Request (Http: 400)");
                            return;
                        }
                    }

                    else if (args.Length < 2)
                    {
                        var custcategory = await context.Guild
                            .CreateCategoryChannelAsync("Custom Channels");
                        await context.Guild
                            .CreateVoiceChannelAsync("[+] Create Channel",
                                x => x.CategoryId = custcategory.Id);
                    }
                    return;

                default:
                    return;
            }
        }
        private static async Task CodeCreateCommand(SocketCommandContext context, string[] args,
            string code, SocketMessage msg)
        {
            var user = context.User;
            if (user is SocketGuildUser currentUser && !currentUser.Roles.Contains(context.Guild.Roles
                    .FirstOrDefault(x => x.Permissions.Administrator)))
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = CacheService.Lang["error_title"],
                    Description = context.Message.Author.Mention + CacheService.Lang["perms_error"]
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
            }

            else if (args.Length < 1)
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Blue,
                    Title = CacheService.Lang["info_title"],
                    Description = context.Message.Author.Mention + CacheService.Lang["code_is"] + "**" + CacheService.Code + "**"
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
            }

            else if (args.Length == 1)
            {
                CacheService.Code = code;
                var embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title"],
                    Description = context.Message.Author.Mention + CacheService.Lang["code_success"]
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
            }

            else
            {
                var embed = new EmbedBuilder
                {
                    Color = Color.Red,
                    Title = CacheService.Lang["error_title"],
                    Description = CacheService.Lang["code_error_args"]
                };
                await context.Channel.SendMessageAsync(null, false, embed.Build());
                await msg.DeleteAsync();
            }
        }
        private static async Task CustomRoleMessage(SocketCommandContext context, IReadOnlyList<string> args)
        {
            var user = context.User;
            var msg = context.Message;
            if (user is SocketGuildUser currentUser
                && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x => 
                    x.Id == Configuration.Root.GetValue<ulong>("eventmanager")))
                && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x => 
                    x.Permissions.Administrator)))
                return;
            if (args.Count != 3) return;
            if (ulong.TryParse(args[0], out ulong msgId) && msg.MentionedRoles.Count == 1)
            {
                var emote = msg.Tags.Where(x => x.Type == TagType.Emoji)
                    .Select(t => (Emote)t.Value).ToList();
                if (emote.Count != 1) return;
                var userMsg = (RestUserMessage) await context.Channel.GetMessageAsync(msgId);
                await userMsg.AddReactionAsync(emote[0]);
            }
        }
        private static async Task HelpCommand(SocketCommandContext context) //help
        {
            var firstCategoryName = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"))
                .GetCategoryChannel(Configuration.Root.GetValue<ulong>("category")).Name;
            var secondCategoryName = DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"))
                .GetCategoryChannel(Configuration.Root.GetValue<ulong>("gamingcategory")).Name;
            var embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = CacheService.Lang["commands"],
                Description = CacheService.Lang["prefix"].Replace("{Configuration.Root[\"prefix\"]}",
                    Configuration.Root["prefix"]),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder {Name =
                            $"**`{Configuration.Root["prefix"]}createchannel`** <num> [permissions*] *(alias: cc)*",
                        Value =  CacheService.Lang["help_value_1"].Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}createchannel`** <num> <name> [permissions*] *(alias: cc)*",
                        Value = CacheService.Lang["help_value_2"].Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}createchannel`** <name> [permissions*] *(alias: cc)*",
                        Value = CacheService.Lang["help_value_3"].Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}creategamingchannel`** <num> [permissions*] *(alias: cgc)*",
                        Value = CacheService.Lang["help_value_4"].Replace("{SecondCategoryName}",
                            secondCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}creategamingchannel`** <num> <name> [permissions*] *(alias: cgc)*",
                        Value = CacheService.Lang["help_value_5"].Replace("{SecondCategoryName}",
                            secondCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`*permissions`**", Value = CacheService.Lang["spec_perms"]}
                },
                Footer = new EmbedFooterBuilder { Text = CacheService.Lang["help_footer"] }
            };
            var adminOnlyEmbed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = CacheService.Lang["admins_only"],
                Description = CacheService.Lang["prefix"].Replace("{Configuration.Root[\"prefix\"]}",
                    Configuration.Root["prefix"]),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}lang`** [lang]",
                        Value = CacheService.Lang["help_value_8"]},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}code`** [code]",
                        Value = CacheService.Lang["help_value_7"]},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}welcome`** [messageId]",
                        Value = CacheService.Lang["help_value_9"]}
                },
                Footer = new EmbedFooterBuilder { Text = CacheService.Lang["help_footer"] }
            };

            await context.Channel.SendMessageAsync(null, false, embed.Build());
            await context.Channel.SendMessageAsync(null, false, adminOnlyEmbed.Build());
        }
        #endregion
    }
}