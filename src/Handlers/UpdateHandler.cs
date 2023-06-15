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
        private static readonly SocketGuild Guild =
            DiscordBot.Client.GetGuild(Configuration.Root.GetValue<ulong>("guild"));

        private static readonly SocketCategoryChannel CategoryChannel = Guild
            .GetCategoryChannel(Configuration.Root.GetValue<ulong>("category"));

        private static readonly SocketCategoryChannel CustomCategoryChannel = Guild
            .GetCategoryChannel(Configuration.Root.GetValue<ulong>("customcategory"));
        
        private static readonly Dictionary<string, ulong> Emotes = new Dictionary<string, ulong> {
            { "pepe_coffee", Configuration.Root.GetValue<ulong>("pepe_coffee") }
        };
        
        public static async Task OnMessageRecievedAsync(SocketMessage m)
        {
            if (!(m is SocketUserMessage msg)) return;
            if (string.IsNullOrWhiteSpace(msg.Content)) return;
            if (msg.Author.Id == DiscordBot.Client.CurrentUser.Id) return;
            
            SocketCommandContext context = new SocketCommandContext(DiscordBot.Client, msg);

            if (Guild is null)
            {
                Logger.Log("Cannot find Guild by id");
                await context.Channel.SendMessageAsync(null, false, 
                    GetErrorEmbed(CacheService.Lang["guild_error"]));
                return;
            }
            
            if (CategoryChannel is null)
            {
                Logger.Log("Cannot find Category by id");
                await context.Channel.SendMessageAsync(null, false, 
                    GetErrorEmbed(CacheService.Lang["category_error"]));
                return;
            }

            if (CustomCategoryChannel is null)
            {
                Logger.Log("Cannot find CustomCategory by id");
                await context.Channel.SendMessageAsync(null, false, 
                    GetErrorEmbed(CacheService.Lang["custom_category_error"]));
                return;
            }
            
            SocketUser user = context.User;
            SocketGuildUser thisUser = context.User as SocketGuildUser;
            
            string[] splitMessage = context.Message.Content.Split(' ');
            
            SocketRole comrades = context.Guild.Roles.FirstOrDefault(x 
                => x.Id == Configuration.Root.GetValue<ulong>("comrades"));

            if (msg.Channel.Id == Configuration.Root.GetValue<ulong>("authchannel"))
            {
                if (splitMessage[0] == CacheService.Code)
                {
                    if (thisUser != null && thisUser.Roles.Contains(comrades))
                    {
                        await context.Channel.SendMessageAsync(null, false, 
                            GetWarningEmbed(CacheService.Lang["auth_warn"], context.Message.Author));
                        await msg.DeleteAsync();
                        return;
                    }
                    await (user as IGuildUser)?.AddRoleAsync(comrades)!;
                    Logger.Log("Passed");
                    string successDescription = CacheService.Lang["auth_success"]
                        ?.Replace("{guild.Name}", Guild.Name);
                    await context.Channel.SendMessageAsync(null, false, 
                        GetSuccessEmbed(successDescription, context.Message.Author));
                    await msg.DeleteAsync();
                    return;
                }
                string errorDescription = CacheService.Lang["auth_error"]?
                    .Replace("{MentionUtils.MentionChannel(714800261750849596)}",
                        MentionUtils.MentionChannel(714800261750849596));
                await context.Channel.SendMessageAsync(null, false, 
                    GetErrorEmbed(errorDescription, context.Message.Author));
                await msg.DeleteAsync();
                return;
            }

            if (msg.Content.Contains(DiscordBot.Client.CurrentUser.Mention))
            {
                if (thisUser != null && thisUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x 
                        => x.Permissions.Administrator)))
                {
                    await context.Channel.SendMessageAsync("wtf discord mod?");
                    return;
                }
                string[] randommessages = Configuration.Root.GetSection("randommessage").Get<string[]>();
                string randommessage = randommessages[new Random().Next(randommessages.Length)];
                await context.Channel.SendMessageAsync(randommessage);
                return;
            }

            String prefix = Configuration.Root["prefix"];
            if (prefix is null) return;
            if (!splitMessage.First().StartsWith(prefix)) return;
            string command = splitMessage.First().Remove(0, 1).ToLower();
            
            string[] args = splitMessage.Skip(1).ToArray();
            
            switch (command)
            {
                case "lang":
                    await LangCommand(context, args, 
                        string.Join(' ', splitMessage.Skip(1)), msg);
                    break;

                case "cc":
                case "createchannel":
                    await CreateChannelCommand(context, args,
                        string.Join(' ', splitMessage.Skip(1)));
                    await msg.DeleteAsync();
                    break;
                
                case "createguild":
                case "cg":
                    await CreateGuildCommand(context, args,string.Join(' ', splitMessage.Skip(1)));
                    break;
                
                case "code":
                    await CodeCreateCommand(context, args, string.Join(' ', splitMessage.Skip(1)), msg);
                    break;

                case "welcome":
                    await WelcomeMessage(context, msg, args, string.Join(' ', splitMessage.Skip(1)));
                    break;

                case "settings":
                    await SettingMessage(context, msg, args, splitMessage[1]);
                    break;
                
                case "customrolemessage":
                case "crm":
                    await CustomRoleMessage(context, args);
                    break;
                    
                case "help":
                    await HelpCommand(context);
                    break;
                
                default:
                    string errorDescription = CacheService.Lang["command_doesnt_exist"]
                        ?.Replace("{Configuration.Root[\"prefix\"]}", Configuration.Root["prefix"]);
                    
                    await context.Channel.SendMessageAsync(null, false, 
                        GetErrorEmbed(errorDescription, context.Message.Author));
                    break;
            }
        }
        
        public static async Task OnUserJoinVoiceChannel(SocketUser user, SocketVoiceState state,
            SocketVoiceState state2)
        {
            if (state.VoiceChannel == state2.VoiceChannel || state2.VoiceChannel == null || 
                state2.VoiceChannel.Id != Configuration.Root.GetValue<ulong>("createch") ||
                state.VoiceChannel?.Id == Configuration.Root.GetValue<ulong>("createch")) return;

            if (user is SocketGuildUser currentUser)
            {
                string channelName = (string.IsNullOrWhiteSpace(currentUser.Nickname) 
                    ? user.Username : currentUser.Nickname) + "'s channel";
            
                Embed errorEmbed = GetErrorEmbed(CacheService.Lang["channel_error_existed"], currentUser);
                if (Guild.Channels.FirstOrDefault(x => x is SocketVoiceChannel channel 
                                                       && channel.CategoryId == 
                                                       CustomCategoryChannel.Id
                                                       && channel.Name == channelName) != null) 
                {
                    await Guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                        .SendMessageAsync(null, false, errorEmbed);
                    await currentUser.ModifyAsync(x => x.Channel = null);
                    await currentUser.SendMessageAsync(null, false, errorEmbed);
                    return;
                }
                RestVoiceChannel voiceChannel = await Guild.CreateVoiceChannelAsync(channelName,
                    x => x.CategoryId = CustomCategoryChannel.Id);
                CacheService.СurrentlyCreated++;
                new ChannelTimer(voiceChannel.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                await currentUser.ModifyAsync(x => x.ChannelId = voiceChannel.Id);
                OverwritePermissions currentPermsEvery = voiceChannel.GetPermissionOverwrite(Guild.EveryoneRole) 
                                                         ?? new OverwritePermissions();
                await voiceChannel.AddPermissionOverwriteAsync(user,
                    currentPermsEvery.Modify(manageChannel: PermValue.Allow));
                
                await Guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync(null, false, GetSuccessEmbed(
                        CacheService.Lang["channel_success_user"]
                            ?.Replace("{channelName}",
                        channelName).Replace("{currentUser.Mention}", currentUser.Mention)));
            }
        }

        public static async Task OnReactionAdded(Cacheable<IUserMessage, ulong> a, Cacheable<IMessageChannel, ulong> mc,
            SocketReaction reaction) 
        {
            if (reaction.User.Value.Id == DiscordBot.Client.CurrentUser.Id) return;
            if (reaction.MessageId != CacheService.WelcomeMessageId) return;
            SocketGuild sockguild = ((SocketGuildChannel)reaction.Channel).Guild;
            SocketGuildUser user = reaction.User.Value as SocketGuildUser;
            IMessage msg = await reaction.Channel.GetMessageAsync(reaction.MessageId);
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
            
            SocketRole role = sockguild.GetRole(859064656710729759); //events
            if (role == null) return;
            if (user != null && user.Roles.Contains(role)) {
                return;
            }

            if (user != null) await user.AddRoleAsync(role);
        }
        
        public static async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> a, Cacheable<IMessageChannel, ulong> mc,
            SocketReaction reaction)
        {
            if (reaction.User.Value.Id == DiscordBot.Client.CurrentUser.Id) return;
            if (reaction.MessageId != CacheService.WelcomeMessageId) return;
            if (!Emotes.ContainsKey(reaction.Emote.Name)) return;
            SocketGuild socketGuild = ((SocketGuildChannel)reaction.Channel).Guild;
            SocketGuildUser user = reaction.User.Value as SocketGuildUser;
            SocketRole role = socketGuild.GetRole(859064656710729759);
            if (role == null) return;
            if (user != null && user.Roles.Contains(role))
                await user.RemoveRoleAsync(role);
        }
        
        #region Commands
        private static async Task LangCommand(SocketCommandContext context, string[] args, string lang,
            SocketMessage msg)
        {
            SocketUser user = context.User;
            SocketGuildUser currentUser = user as SocketGuildUser;

            if (args.Length < 1)
            {
                await context.Channel.SendMessageAsync(null, false, 
                    GetInfoEmbed(CacheService.Lang["current_lang"], context.Message.Author));
                await msg.DeleteAsync();
                return;
            }

            if (currentUser != null && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x => 
                    x.Permissions.Administrator)))
            {
                await context.Channel.SendMessageAsync(null, false, 
                    GetErrorEmbed(CacheService.Lang["perms_error"], context.Message.Author));
                await msg.DeleteAsync();
                return;
            }
            
            switch (lang)
            {
                case "en-us":
                    CacheService.Lang = Configuration.English;
                    await context.Channel.SendMessageAsync(null, false, 
                        GetSuccessEmbed(CacheService.Lang["lang_changed"], context.Message.Author));
                    await msg.DeleteAsync();
                    return;

                case "ru-ru":
                    CacheService.Lang = Configuration.Russian;
                    await context.Channel.SendMessageAsync(null, false,
                        GetSuccessEmbed(CacheService.Lang["lang_changed"], context.Message.Author));
                    await msg.DeleteAsync();
                    return;

                default:
                    await context.Channel.SendMessageAsync(null, false, 
                        GetErrorEmbed(CacheService.Lang["lang_notsupported"], context.Message.Author));
                    await msg.DeleteAsync();
                    return;
            }
        }
        
        private static async Task CreateChannelCommand(SocketCommandContext context, string[] args,
            string name)
        {
            name = name.Trim();
            if (CacheService.СurrentlyCreated >= Configuration.Root.GetValue<int>("limit"))
            {
                EmbedBuilder embed = new EmbedBuilder
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
                RestVoiceChannel ch = await context.Guild.CreateVoiceChannelAsync(name, x => 
                    x.CategoryId = CategoryChannel.Id);
                OverwritePermissions currentPermsEvery = ch.GetPermissionOverwrite(Guild.EveryoneRole)
                                                         ?? new OverwritePermissions();
                await ch.AddPermissionOverwriteAsync(context.User, currentPermsEvery.Modify(
                    manageChannel: PermValue.Allow));
                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title_channel"],
                    Description = CacheService.Lang["channel_success_name"]
                        ?.Replace("{NameShort}", name)
                        .Replace("{context.Message.Author.Mention}", context.Message.Author.Mention)
                };
                await Guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync("", false, embed.Build());
                new ChannelTimer(ch.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                CacheService.СurrentlyCreated++;
                Logger.Log($"Channel {ch.Id} created. Currently created: {CacheService.СurrentlyCreated}.");
            }
            
            else if (args.Length == 0)
            {
                string[] randonamesfirst = CacheService.Lang.GetSection("randomfirst").Get<string[]>();
                string randonamefirst = randonamesfirst[new Random().Next(randonamesfirst.Length)];

                string[] randonamessecond = CacheService.Lang.GetSection("randomsecond").Get<string[]>();
                string randonamesecond = randonamessecond[new Random().Next(randonamessecond.Length)];

                RestVoiceChannel ch = await context.Guild.CreateVoiceChannelAsync($"{randonamefirst} " +
                                                                     $"\"{randonamesecond}\"", x =>
                    x.CategoryId = CategoryChannel.Id);

                OverwritePermissions currentPermsEvery = ch.GetPermissionOverwrite(Guild.EveryoneRole) 
                                                         ?? new OverwritePermissions();
                await ch.AddPermissionOverwriteAsync(context.User, currentPermsEvery
                    .Modify(manageChannel: PermValue.Allow));

                EmbedBuilder embed = new EmbedBuilder
                {
                    Color = Color.Green,
                    Title = CacheService.Lang["success_title_channel"],
                    Description = CacheService.Lang["channel_success_randname"]
                        ?.Replace("{randonamefirst}"
                        , randonamefirst).Replace("{randonamesecond}", randonamesecond)
                        .Replace("{context.Message.Author.Mention}", context.Message.Author.Mention)
                };
                await Guild.GetTextChannel(Configuration.Root.GetValue<ulong>("logchannel"))
                    .SendMessageAsync("", false, embed.Build());

                new ChannelTimer(ch.Id).Set(Configuration.Root.GetValue<double>("deletionms"));
                CacheService.СurrentlyCreated++;

                Logger.Log($"Channel {ch.Id} created. Currently created: {CacheService.СurrentlyCreated}.");
            }
            else
            {
                EmbedBuilder embed = new EmbedBuilder
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
            RestUserMessage rum = await context.Channel.SendMessageAsync($"*{name}*");
            await rum.AddReactionsAsync(new List<Emoji>
            {
                new Emoji("\u2705"),
                new Emoji("\u274C")
            }.ToArray()); //todo: reactions
        }
        
        private static async Task WelcomeMessage(SocketCommandContext context, SocketMessage msg, string[] args, 
            string id)
        {
            SocketUser user = context.User;
            SocketGuildUser currentUser = user as SocketGuildUser;
            EmbedBuilder embed = new EmbedBuilder();
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
                string[] entries = CacheService.Lang.GetSection("roles_selection").Get<string[]>();
                string[] entriesDesc = CacheService.Lang.GetSection("roles_desc").Get<string[]>();
                embed.Color = Color.Blue;
                embed.Title = CacheService.Lang["select_title"];
                List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
                for (int i = 0; i < entries.Length; i++) fields.Add(new EmbedFieldBuilder
                    { Name = i + 1 + ". " + entries[i] + " \n", Value = entriesDesc[i] + "\n" });
                embed.Fields = fields;
                RestUserMessage message = await context.Channel.SendMessageAsync(null, false, embed.Build());
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
        
        private static async Task SettingMessage(SocketCommandContext context, SocketMessage msg, string[] args, 
            string setting)
        {
            EmbedBuilder embed = new EmbedBuilder();
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
                    SocketGuildChannel[] channel = msg.MentionedChannels.ToArray();
                    if (channel.Length != 1) return;
                    //CacheService.AuthChannel = channel[0].Id;
                    await context.Channel.SendMessageAsync("");
                    return;

                case "defrole":
                    SocketRole[] role = msg.MentionedRoles.ToArray();
                    if (role.Length != 1) return;
                    //CacheService.DefaultRole = role[0].Id;
                    return;

                case "logchannel":
                    SocketRole[] logchannel = msg.MentionedRoles.ToArray();
                    if (logchannel.Length != 1) return;
                    //CacheService.LogChannel = logchannel[0].Id;
                    return;

                case "limchannel":
                    string limchannel = args[1];
                    if (!int.TryParse(limchannel, out int lim)) return;
                    if (lim < 0 || lim > 30) return;
                    //CacheService.ChannelLim = lim;
                    return;

                case "category":
                    if (!ulong.TryParse(args[1], out _)) return;
                    //CacheService.FirstCategory = category;
                    return;

                case "react":
                    Emote emote = null;
                    foreach (string t in args)
                        if (Emote.TryParse(t, out Emote tempEmote))
                        {
                            emote = tempEmote;
                            break;
                        }
                    SocketRole[] rolesem = msg.MentionedRoles.ToArray();
                    if (rolesem.Length != 1) return;
                    if (emote != null) CacheService.emotes.Add(emote.Name, rolesem[0].Id);
                    //CacheService.CustomEmotesMsgId = reactmsg.Id;
                    return;

                case "reactremove":
                    Emote emoterem = null;
                    foreach (string t in args)
                    {
                        if (Emote.TryParse(t, out Emote tempEmote))
                        {
                            emoterem = tempEmote;
                            break;
                        }
                    }

                    if (emoterem != null) CacheService.emotes.Remove(emoterem.Name);
                    return;

                case "reactlist":
                    string emotesList = string.Empty;
                    string[] emotesKey = CacheService.emotes.Keys.ToArray();
                    ulong[] emotesValues = CacheService.emotes.Values.ToArray();
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
                        RestCategoryChannel custcategory = await context.Guild
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
            SocketUser user = context.User;
            if (user is SocketGuildUser currentUser && !currentUser.Roles.Contains(context.Guild.Roles
                    .FirstOrDefault(x => x.Permissions.Administrator)))
            {
                EmbedBuilder embed = new EmbedBuilder
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
                EmbedBuilder embed = new EmbedBuilder
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
                EmbedBuilder embed = new EmbedBuilder
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
                EmbedBuilder embed = new EmbedBuilder
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
            SocketUser user = context.User;
            SocketUserMessage msg = context.Message;
            if (user is SocketGuildUser currentUser
                && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x => 
                    x.Id == Configuration.Root.GetValue<ulong>("eventmanager")))
                && !currentUser.Roles.Contains(context.Guild.Roles.FirstOrDefault(x => 
                    x.Permissions.Administrator)))
                return;
            if (args.Count != 3) return;
            if (ulong.TryParse(args[0], out ulong msgId) && msg.MentionedRoles.Count == 1)
            {
                List<Emote> emote = msg.Tags.Where(x => x.Type == TagType.Emoji)
                    .Select(t => (Emote)t.Value).ToList();
                if (emote.Count != 1) return;
                RestUserMessage userMsg = (RestUserMessage) await context.Channel.GetMessageAsync(msgId);
                await userMsg.AddReactionAsync(emote[0]);
            }
        }
        
        private static async Task HelpCommand(SocketCommandContext context)
        {
            string firstCategoryName = CategoryChannel.Name;
            
            EmbedBuilder embed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = CacheService.Lang["commands"],
                Description = CacheService.Lang["prefix"]
                    ?.Replace("{Configuration.Root[\"prefix\"]}",
                    Configuration.Root["prefix"]),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder {Name =
                            $"**`{Configuration.Root["prefix"]}createchannel`** <num> [permissions*] *(alias: cc)*",
                        Value =  CacheService.Lang["help_value_1"]
                            ?.Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}createchannel`** <num> <name> [permissions*] *(alias: cc)*",
                        Value = CacheService.Lang["help_value_2"]
                            ?.Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`{Configuration.Root["prefix"]}createchannel`** <name> [permissions*] *(alias: cc)*",
                        Value = CacheService.Lang["help_value_3"]
                            ?.Replace("{FirstCategoryName}",
                            firstCategoryName)},
                    new EmbedFieldBuilder {Name = $"**`*permissions`**", Value = CacheService.Lang["spec_perms"]}
                },
                Footer = new EmbedFooterBuilder { Text = CacheService.Lang["help_footer"] }
            };
            EmbedBuilder adminOnlyEmbed = new EmbedBuilder
            {
                Color = Color.Blue,
                Title = CacheService.Lang["admins_only"],
                Description = CacheService.Lang["prefix"]
                    ?.Replace("{Configuration.Root[\"prefix\"]}",
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

        private static Embed GetEmbed(string title, string description, Color color)
        {
            return new EmbedBuilder().WithColor(color).WithDescription(description)
                .WithDescription(title).Build();
        }

        private static Embed GetErrorEmbed(string description, SocketUser author = null)
        {
            string thisDesctiption = author is null ? description : author.Mention + description;
            return GetEmbed(CacheService.Lang["error_title"], thisDesctiption, Color.Red);
        }

        private static Embed GetWarningEmbed(string description, SocketUser author = null)
        {
            string thisDesctiption = author is null ? description : author.Mention + description;
            return GetEmbed(CacheService.Lang["warning_title"], thisDesctiption, Color.Orange);
        }

        private static Embed GetSuccessEmbed(string description, SocketUser author = null)
        {
            string thisDescription = author is null ? description : author.Mention + description;
            return GetEmbed(CacheService.Lang["success_title"], thisDescription, Color.Green);
        }

        private static Embed GetInfoEmbed(string description, SocketUser author = null)
        {
            string thisDescription = author is null ? description : author.Mention + description;
            return GetEmbed(CacheService.Lang["info_title"], thisDescription, Color.Blue);
        }
    }
}