using AnnoyChat;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AnnoyChat.Modules
{
    public class Commands
    {
        public static async Task Load(SocketSlashCommand command)
        {
            await command.DeferAsync();

            if (Main.botLoaded)
            {
                if (Main.modConnected == true)
                    await command.ModifyOriginalResponseAsync(x => x.Content = "The mod is already loaded.");
                else
                    await command.ModifyOriginalResponseAsync(x => x.Content = "The client has failed to load. Please restart the bot and try again.");
                return;
            }
            Main.botLoaded = true;

            try
            {
                //Main.tcp = new TcpClient("localhost", 6000);
                //Main.StartTcpListenerThread();
                Main.loadCommand = command;
                Main.SendSocket("checkForResponse");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                await command.ModifyOriginalResponseAsync(x => x.Content = "ERROR: Load Failed. The client is not connected! \nPlease restart the bot and run the client using 'bridge launch'.");
                return;
            }
        }
        public static async Task Launch(SocketSlashCommand command)
        {
            await command.DeferAsync();
            if (Main.clientLaunched)
            {
                if (Main.modConnected == true)
                    await command.ModifyOriginalResponseAsync(x => x.Content = "The client and mod are already online.");
                else
                    await command.ModifyOriginalResponseAsync(x => x.Content = "The client has already launched. Please proceed with /load when it has connected to the network.");
                return;
            }

            Main.clientLaunched = true;

            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName + @"/MultiMC";
            Console.WriteLine(path);
            string cmdCommand = @$"{path}./MultiMC --launch 1.16.5 --server mc.hypixel.net";
            Process.Start(@"C:/windows/system32/windowspowershell/v1.0/powershell.exe ", cmdCommand);
            await command.ModifyOriginalResponseAsync(x => x.Content = "The client is preparing to launch, please wait and proceed with /load when it has connected to the network.");
        }
        public static async Task CompleteLoad()
        {
            Main.modConnected = true;
            await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow));
            await Main.loadCommand.ModifyOriginalResponseAsync(x => x.Content = "Loaded!");
        }
        public static async Task ViewName(SocketSlashCommand command)
        {
            ulong userID = command.User.Id;
            if (command.Data.Options.Count != 0)
                userID = ((SocketUser)command.Data.Options.First().Value).Id;
            if (!Main.registeredUsers.ContainsKey(userID))
            {
                if (userID == command.User.Id)
                {
                    var builder = new ComponentBuilder()
                        .WithButton("Yes", "admin_notify", ButtonStyle.Success)
                        .WithButton("No", "admin_not_notify", ButtonStyle.Danger);

                    await command.RespondAsync("You do not have a display name set!");
                    var DM = await command.User.CreateDMChannelAsync();
                    await DM.SendMessageAsync("Would you like to notify an administrator and ask them give you a name?", component: builder.Build());

                }
                else
                    await command.RespondAsync("The user does not have a display name associated with them!");
            }
            else
            {
                if (userID == command.User.Id)
                    await command.RespondAsync($"Your display name is {Main.registeredUsers[userID]}.");
                else
                    await command.RespondAsync($"The user's display name is {Main.registeredUsers[userID]}.");
            }
        }

        public static async Task SetName(SocketSlashCommand command)
        {
            ulong userID = ((SocketUser)command.Data.Options.ElementAt(0).Value).Id;
            var user = Services.CommandHandler._discord.GetUser(userID);
            string displayName = command.Data.Options.ElementAt(1).Value.ToString();

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
                await command.RespondAsync($"A user already exists with that display name! {Services.CommandHandler._discord.GetUser((ulong)displayNameAlreadyExistsWithUserID).Mention}.");
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
                await command.RespondAsync($"Successfully set {user.Mention}'s display name to {displayName}.");
                var DM = await user.CreateDMChannelAsync();
                await DM.SendMessageAsync($"Your display name has been set to {displayName}! This means you will now be able to send messsages in #{Main.channel.Name}, and " +
                    $"your messages in Minecraft will appear under the name {displayName} when sent through the channel.\nIf you ever forget your display name, use /viewname to " +
                    $"view it.\nEnjoy chatting!");
            }
            else
            {
                Main.registeredUsers[userID] = displayName;
                await command.RespondAsync($"Successfully changed {user.Mention}'s display name to {displayName}. (Previous was {oldDisplayName})");
                var DM = await user.CreateDMChannelAsync();
                await DM.SendMessageAsync($"Your display name has been changed to {displayName}! This means you will now be able to send messsages in #{Main.channel.Name}, and " +
                    $"your messages in Minecraft will appear under the name {displayName} when sent through the channel.\nIf you ever forget your display name, use /viewname to " +
                    $"view it.\nEnjoy chatting!");
            }
        }

        public static async Task Info(SocketSlashCommand command)
        {
            //If the mod is not connected:
            if (!Main.modConnected)
            {
                await command.RespondAsync("The bot is currently disabled, please be patient while we fix the issue :)", ephemeral: true);
                await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow));
                await Main.channel.SendMessageAsync("The bot is currently disabled, please be patient while we fix the issue :)");
                return;
            }
            await command.DeferAsync();

            string content = "/g info";

            Main.queuedCommands.Enqueue(((string, SocketUserMessage))(content, null));
            Main.queuedGuildInfoCommands.Enqueue(command);

            Main.SendSocket(content);

        }
        public static async Task Players(SocketSlashCommand command)
        {
            //If the mod is not connected:
            if (!Main.modConnected)
            {
                await command.RespondAsync("The bot is currently disabled, please be patient while we fix the issue :)", ephemeral: true);
                await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow));
                await Main.channel.SendMessageAsync("The bot is currently disabled, please be patient while we fix the issue :)");
                return;
            }
            await command.DeferAsync();

            string content = "/g list";

            Main.queuedCommands.Enqueue(((string, SocketUserMessage))(content, null));
            Main.queuedGuildPlayersCommands.Enqueue(command);

            Main.SendSocket(content);

        }
    }
}
