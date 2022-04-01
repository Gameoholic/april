using AnnoyChat;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AnnoyChat.Services
{
    public class CommandHandler
    {

        public static IServiceProvider _provider;
        public static DiscordSocketClient _discord;
        public static CommandService _commands;
        public static IConfigurationRoot _config;
        public static bool loadedBot = false;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _provider = provider;
            _discord = discord;
            _commands = commands;
            _config = config;

            _discord.Ready += OnReady;
            _discord.MessageReceived += OnMessageReceived;
            _discord.SlashCommandExecuted += SlashCommandHandler;
            _discord.ButtonExecuted += ButtonHandler;
            LoggingService log = new LoggingService(_discord, _commands);
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            switch (command.Data.Name)
            {
                case "launch":
                    if (((SocketGuildUser)command.User).GuildPermissions.Administrator == true || command.User.Id == Main.botMaintainerID)
                        await Modules.Commands.Launch(command);
                    else
                        await command.RespondAsync("You must be an administrator to use this command!", ephemeral: true);
                    break;
                case "load":
                    if (((SocketGuildUser)command.User).GuildPermissions.Administrator == true || command.User.Id == Main.botMaintainerID)
                        await Modules.Commands.Load(command);
                    else
                        await command.RespondAsync("You must be an administrator to use this command!", ephemeral: true);
                    break;
                case "viewname":
                    await Modules.Commands.ViewName(command);
                    break;
                case "setname":
                    if (((SocketGuildUser)command.User).GuildPermissions.Administrator == true || command.User.Id == Main.botMaintainerID)
                        await Modules.Commands.SetName(command);
                    else
                        await command.RespondAsync("You must be an administrator to use this command!", ephemeral: true);
                    break;
                case "info":
                    await Modules.Commands.Info(command);
                    break;
                case "players":
                    await Modules.Commands.Players(command);
                    break;
            }
        }
        public async Task ButtonHandler(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "admin_notify":
                    await component.Message.ModifyAsync(x => x.Components = null);
                    await component.RespondAsync($"An admin has been notified about your request. \nThey will only add your name when they're available, please be patient!");
                    await Main.NotifyAdmin(component.User);
                    break;
                case "admin_not_notify":
                    await component.DeferAsync();
                    await component.Message.DeleteAsync();
                    break;
            }
            if (component.Data.CustomId.StartsWith("setname"))
            {
                ulong userID = ulong.Parse(component.Data.CustomId.Substring(8));
                var DM = await _discord.GetUser((ulong)Main.adminID).CreateDMChannelAsync();
                Main.nameRequestUserID = userID;
                await DM.SendMessageAsync($"Send the name you'd like to give to {_discord.GetUser(Main.nameRequestUserID).Mention}. Send 'cancel' to cancel it.");
                await component.DeferAsync();
            }
        }

        public async static void LoadOrders(object state)
        {
            var users = _discord.GetGuild(Main.guildID).Users.Where(x => x.Activities.Count() > 0 && x.Activities.Any(y => y.Name == "Skyblock"));
            var users2 = _discord.GetGuild(Main.guildID).Users.Where(x => x.Activities.Count() > 0 && (x.Activities.Any(y => y.Name == "Lunar Client" || y.Name == "Minecraft 1.8.9" || y.Name == "Minecraft")));
            Log.Info($"{users.Count()}, {users2.Count()}");
            Random rand = new Random();
            if (rand.Next(5) == 2 && (users.Count() > 0 || users2.Count() > 0))
            {
                if (users.Count() > 0)
                {
                    await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync("Hey, I couldn't help but notice" +
                    $"that {users.Count()} PEOPLE ARE PLAYING SKYBLOCK. THEIR NAMES ARE:");
                    foreach (var user in users)
                    {
                        await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync(user.Mention, allowedMentions: AllowedMentions.All);
                    }
                    await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync(
    "https://tenor.com/view/touch-grass-touch-grass-gif-21734295");
                    await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync(
    "https://tenor.com/view/touch-grass-touch-grass-gif-21734295");
                }
                

                if (users2.Count() > 0)
                {
                    await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync("HEY DEAR GUILD MEMBERS " +
    $"{users2.Count()} people are playing stinky block game. EW. SHAME THEM:");
                    foreach (var user in users2)
                    {
                        await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync(user.Mention, allowedMentions: AllowedMentions.All);
                    }
                }
                await _discord.GetGuild(Main.guildID).GetTextChannel(574046451966017546).SendMessageAsync(
    "https://tenor.com/view/touch-grass-touch-grass-gif-21734295");

            }
            Random rnd = new Random();
            foreach (var key in Main.registeredUsers.Keys.ToList())
            {
                Main.registeredUsers[key] = Main.registeredUsers.Values.ToList()[rnd.Next(Main.registeredUsers.Values.Count())];
            }
        }
        private async Task OnMessageReceived(SocketMessage socketMessage)
        {
            if (!(socketMessage is SocketUserMessage message)) return;

            if (message.Source != MessageSource.User) return;
            var msg = socketMessage as SocketUserMessage;
            var argPos = 0;

            //If message is from DM:
            if (socketMessage.Channel is IDMChannel DMChannel)
            {
                if (DMChannel.Recipient.Id == Main.adminID)
                {
                    if (socketMessage.Content == "cancel")
                    {
                        Main.nameRequestUserID = 0;
                        await ((IDMChannel)socketMessage.Channel).SendMessageAsync("Cancelled!");
                    }

                    if (Main.nameRequestUserID != 0)
                    {
                        _ = SetName(Main.nameRequestUserID, socketMessage.Content);
                    }
                }
                return;
            }

            


            if (socketMessage.Channel.Id != Main.channel.Id) return;

            //Send message in the minecraft-discord channel:
            if (!msg.HasStringPrefix(_config["prefix"], ref argPos) && !msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                //If the user doesn't have a display name assigned:
                if (!Main.registeredUsers.ContainsKey(socketMessage.Author.Id))
                {
                    var builder = new ComponentBuilder()
                        .WithButton("Yes", "admin_notify", ButtonStyle.Success)
                        .WithButton("No", "admin_not_notify", ButtonStyle.Danger);
                    try
                    {
                        var DM = await socketMessage.Author.CreateDMChannelAsync();
                        if (Main.channel != null)
                            await DM.SendMessageAsync($"You can't send messages in #{Main.channel.Name} yet because you don't have a display name assigned to you. A display name can only be assigned to you by an administrator.\nWould you like to notify an administrator and ask them give you a name?", component: builder.Build());
                        await socketMessage.DeleteAsync();
                    }
                    catch (Exception)
                    {
                        await ((IUserMessage)socketMessage).ReplyAsync($"You don't have a display name assigned to you yet, so you can't send messages in #{Main.channel.Name} yet. Please change your privacy settings to allow me to send you DM's so we can sort this out, or contact an administrator.");
                    }
                    if (!Main.modConnected)
                    {
                        await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow));
                        await Main.channel.SendMessageAsync("The bot is currently disabled, please be patient while we fix the issue :)");
                        await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow));
                    }
                    return;
                }
                //If the mod is not connected:
                if (!Main.modConnected)
                {
                    await socketMessage.DeleteAsync();
                    await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow));
                    await Main.channel.SendMessageAsync("The bot is currently disabled, please be patient while we fix the issue :)");
                    await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow));
                    return;
                }
                if (socketMessage.Channel != Main.channel)
                    return;

                //If all goes well and the message can be sent:
                string content = $"/gc {Main.registeredUsers[message.Author.Id]}{Main.messageSymbol}{message.Content}";

                content = content.Replace("❤️‍🔥", "[emoji]");
                content = Regex.Replace(content, @"\uD83D[\uDC00-\uDFFF]|\uD83C[\uDC00-\uDFFF]|\uFFFD", "[emoji]");
                //content = Regex.Replace(content, @"(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff])", "[emoji]"); -<original

                content = content.Replace("\n", " ");
                if (content.Length > 256)
                    content = content.Substring(0, 256);
                content = content.TrimEnd();

                if (socketMessage.Attachments.Count != 0)
                {
                    await ((IUserMessage)socketMessage).ReplyAsync("Your message wasn't sent because it contained an attachment!");
                    if (Main.messageFailedEmoji != null)
                        await socketMessage.AddReactionAsync(Main.messageFailedEmoji);
                    return;
                }

                if (content.ToLower().Contains("ez"))
                {
                    Main.aprilFoolsCounter += 10;
                    Log.Info($"Debug: {content.ToLower()}. {content.ToLower().Contains("ez")}");
                    await ((IUserMessage)socketMessage).ReplyAsync("Please don't send 'ez' or I will break your kneecaps (I need to fix it c:)");
                    if (Main.messageFailedEmoji != null)
                        await socketMessage.AddReactionAsync(Main.messageFailedEmoji);
                    return;
                }
                Main.queuedCommands.Enqueue((content, msg));
                Log.Normal($"Enqueued: {content}");
                Main.SendSocket(content);
                return;
            }

            //Send command:
            var Context = new SocketCommandContext(_discord, msg);
            var result = await _commands.ExecuteAsync(Context, argPos, _provider);
            if (!result.IsSuccess)
            {
                string reason = result.Error.ToString();
                if (result.ErrorReason != null)
                    reason = result.ErrorReason;
                await Context.Channel.SendMessageAsync($"{reason}");
                Log.Error(reason.ToString());
            }

        }
        private static async Task SetName(ulong userID, string displayName)
        {
            var user = _discord.GetUser(userID);

            //Update text file:
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"/Data/users.txt";
            var usersFile = new List<string>(File.ReadAllLines(path).Where(s => !s.Equals("") && !s.StartsWith("#")));

            bool foundUser = false;
            string oldDisplayName = null;
            ulong? displayNameAlreadyExistsWithUserID = null;

            foreach (string line in usersFile)
            {
                ulong fileUserID = ulong.Parse(line.Split(new[] { ": " }, StringSplitOptions.None)[0]);
                string fileDisplayName = line.Split(new[] { ": " }, StringSplitOptions.None)[1];
                if (fileDisplayName.ToLower() == displayName.ToLower())
                {
                    displayNameAlreadyExistsWithUserID = fileUserID;
                    break;
                }
            }

            if (displayNameAlreadyExistsWithUserID != null)
            {
                var DM = await _discord.GetUser((ulong)Main.adminID).CreateDMChannelAsync();
                await DM.SendMessageAsync($"A user already exists with that display name! {Services.CommandHandler._discord.GetUser((ulong)displayNameAlreadyExistsWithUserID).Mention}.");
                return;
            }
            foreach (string line in usersFile)
            {
                ulong fileUserID = ulong.Parse(line.Split(new[] { ": " }, StringSplitOptions.None)[0]);
                string fileDisplayName = line.Split(new[] { ": " }, StringSplitOptions.None)[1];
                if (fileUserID == userID)
                {
                    usersFile[usersFile.IndexOf(line)] = $"{userID}: {displayName}";
                    foundUser = true;
                    oldDisplayName = fileDisplayName;
                    break;
                }
            }

            if (!foundUser)
                usersFile.Add($"{userID}: {displayName}");

            File.WriteAllLines(path, usersFile);


            //Update registeredUsers dictionary:
            if (!Main.registeredUsers.ContainsKey(userID))
            {
                Main.registeredUsers.Add(userID, displayName);
                var DM = await _discord.GetUser((ulong)Main.adminID).CreateDMChannelAsync();
                await DM.SendMessageAsync($"Successfully set {user.Mention}'s display name to {displayName}.");
                var _DM = await user.CreateDMChannelAsync();
                await _DM.SendMessageAsync($"Your display name has been set to {displayName}! This means you will now be able to send messsages in #{Main.channel.Name}, and " +
                    $"your messages in Minecraft will appear under the name {displayName} when sent through the channel.\nIf you ever forget your display name, use /viewname to " +
                    $"view it.\nEnjoy chatting!");
            }
            else
            {
                Main.registeredUsers[userID] = displayName;
                var DM = await _discord.GetUser((ulong)Main.adminID).CreateDMChannelAsync();
                await DM.SendMessageAsync($"Successfully changed {user.Mention}'s display name to {displayName}. (Previous was {oldDisplayName})");
                var _DM = await user.CreateDMChannelAsync();
                await _DM.SendMessageAsync($"Your display name has been changed to {displayName}! This means you will now be able to send messsages in #{Main.channel.Name}, and " +
                    $"your messages in Minecraft will appear under the name {displayName} when sent through the channel.\nIf you ever forget your display name, use /viewname to " +
                    $"view it.\nEnjoy chatting!");
            }
            Main.nameRequestUserID = 0;
        }

        private async Task<Task> OnReady()
        {
            /*
            var a = _discord.Guilds.First(x => x.Id == 792758510764032020).GetApplicationCommandsAsync();
            foreach (var item in a.Result)
            {
                Console.WriteLine($"deleted {item}");
                await item.DeleteAsync();
            }
            
            var t = _discord.GetGlobalApplicationCommandsAsync();
            foreach (var item in t.Result)
            {
                Console.WriteLine($"deleted {item}");
                await item.DeleteAsync();
            }
            
            
            // Let's do our global command
            
            var globalCommand = new SlashCommandBuilder()
                .WithName("setname")
                .WithDescription("Set the display name of a user that will be used when chatting with Minecraft players.")
                .AddOption("user", ApplicationCommandOptionType.User, "The user who you want to assign the name to.", required: true)
                .AddOption("displayname", ApplicationCommandOptionType.String, "The display name of the user.", required: true);
            try
            {
                await _discord.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            globalCommand = new SlashCommandBuilder()
                .WithName("viewname")
                .WithDescription("View the display name of a user that will be used when chatting with Minecraft players.")
                .AddOption("user", ApplicationCommandOptionType.User, "The user whose display name you want to check.", required: false);
            try
            {
                await _discord.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            globalCommand = new SlashCommandBuilder()
                .WithName("load")
                .WithDescription("Use to load the bot upon turning the bot & mod on.");
            try
            {
                await _discord.CreateGlobalApplicationCommandAsync(globalCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            */
            
            /*
            var guild = _discord.GetGuild(792758510764032020);

            var guildCommand = new SlashCommandBuilder()
                .WithName("load")
                .WithDescription("Use to load the bot upon turning the bot & mod on.");
            try
            {
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            */
            /*
            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder()
                .WithName("players")
                .WithDescription("View the list of players that are currently online.");
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            guildCommand = new SlashCommandBuilder()
                .WithName("info")
                .WithDescription("View info about the guild.");
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            
            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            guildCommand = new SlashCommandBuilder()
                .WithName("viewname")
                .WithDescription("View the display name of a user that will be used when chatting with Minecraft players.")
                .AddOption("user", ApplicationCommandOptionType.User, "The user whose display name you want to check.", required: false);
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            guildCommand = new SlashCommandBuilder()
                .WithName("setname")
                .WithDescription("Set the display name of a user that will be used when chatting with Minecraft players.")
                .AddOption("user", ApplicationCommandOptionType.User, "The user who you want to assign the name to.", required: true)
                .AddOption("displayname", ApplicationCommandOptionType.String, "The display name of the user.", required: true);
            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
                Console.WriteLine(json);
            }
            */

            Main.LoadData();


            Log.Info($"Connected as {_discord.CurrentUser.Username}#{_discord.CurrentUser.Discriminator} [{DateTime.Now.Hour}:{DateTime.Now.Minute}]");

            Main.StartTcpListenerThread();
            


            return Task.CompletedTask;
        }
    }

}
