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

    public class General : ModuleBase<SocketCommandContext>
    {
        public static Random rnd = new Random();

        [Command("ping")]
        public async Task Ping()
        {
            await ReplyAsync(Context.Client.Latency.ToString() + " ms");
        }

        [Command("load")]
        public async Task Load()
        {
            if (!(((SocketGuildUser)Context.User).GuildPermissions.Administrator == true || Context.User.Id == Main.botMaintainerID))
                return;
            if (Main.botLoaded)
            {
                if (Main.modConnected == true)
                    await Context.Message.ReplyAsync("The mod is already loaded.");
                else
                    await Context.Message.ReplyAsync("The client has failed to load. Please restart the bot and try again.");
                return;
            }
            Main.botLoaded = true;

            try
            {
                //Main.loadCommand = await Context.Message.ReplyAsync("Loading...");
                Main.tcp = new TcpClient("localhost", 6000);
                Main.StartTcpListenerThread();
                Main.SendSocket("checkForResponse");
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
                //await Main.loadCommand.ModifyAsync(x => x.Content = "ERROR: Load Failed. The client is not connected! \nPlease restart the bot and run the client using /launch.");
                return;
            }
        }

        [Command("launch")]
        public async Task Launch()
        {
            if (Main.clientLaunched)
            {
                if (Main.modConnected == true)
                    await Context.Message.ReplyAsync("The client and mod are already online.");
                else
                    await Context.Message.ReplyAsync("The client has already launched. Please proceed with /load when it has connected to the network.");
                return;
            }

            Main.clientLaunched = true;

            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.Parent.Parent.FullName + @"/MultiMC";
            Console.WriteLine(path);
            string cmdCommand = @$"{path}./MultiMC --launch 1.16.5 --server mc.hypixel.net";
            Process.Start(@"C:/windows/system32/windowspowershell/v1.0/powershell.exe ", cmdCommand);
            await Context.Message.ReplyAsync("The client is preparing to launch, please wait and proceed with /load when it has connected to the network.");

        }

        [Command("disable")]
        public async Task Disable()
        {
            if (!(((SocketGuildUser)Context.User).GuildPermissions.Administrator == true || Context.User.Id == Main.botMaintainerID))
                return;
            try
            {
                await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow));
                await Main.channel.SendMessageAsync("The bot is currently disabled, please be patient while we fix the issue :)");
                await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Deny, viewChannel: PermValue.Allow));
            }
            catch (Exception e)
            {
                await Context.Message.ReplyAsync("Command failed.");
                Log.Error(e.Message);
            }
        }

        [Command("enable")]
        public async Task Enable()
        {
            if (!(((SocketGuildUser)Context.User).GuildPermissions.Administrator == true || Context.User.Id == Main.botMaintainerID))
                return;
            try
            {
                await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Main.canViewRole, OverwritePermissions.InheritAll.Modify(sendMessages: PermValue.Allow, viewChannel: PermValue.Allow));
            }
            catch (Exception e)
            {
                await Context.Message.ReplyAsync("Command failed.");
                Log.Error(e.Message);
            }
        }

        [Command("givepermsplsty")]
        public async Task TempPermsCommand()
        {
            if (!(((SocketGuildUser)Context.User).GuildPermissions.Administrator == true || Context.User.Id == Main.botMaintainerID))
                return;
             await ((IGuildChannel)Main.channel).AddPermissionOverwriteAsync(Context.User, OverwritePermissions.AllowAll(Main.channel));
        }

        [Command("slowmode")]
        public async Task SlowMode(int seconds)
        {
            if (!(((SocketGuildUser)Context.User).GuildPermissions.Administrator == true || Context.User.Id == Main.botMaintainerID))
                return;
            try
            {
                await ((SocketTextChannel)Main.channel).ModifyAsync(x => x.SlowModeInterval = seconds);
                await Context.Message.ReplyAsync($"Changed slowmode in the channel to {seconds} seconds.");
            }
            catch (Exception e)
            {
                await Context.Message.ReplyAsync("Command failed.");
                Log.Error(e.Message);
            }
        }
    }

}
