using System;
using System.IO;
using Discord.WebSocket;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace AnnoyChat
{
    public class Main
    {
        public static TcpClient tcp = new TcpClient();

        public static int aprilFoolsCounter = 0;

        public static Dictionary<ulong, string> registeredUsers = new Dictionary<ulong, string>();
        public static ulong guildID;
        public static bool notifyAdmin;
        public static ulong? adminID = null;
        public static string messageSymbol;
        public static string modUserIGN;
        public static string modUserRank = "";
        public static ulong? botMaintainerID = null;
        public static bool botLoaded = false;
        public static bool clientLaunched = true;
        public static bool modConnected = true;
        public static Emoji? messageFailedEmoji = null;
        public static ISocketMessageChannel channel;
        public static SocketRole everyoneRole;
        public static ulong nameRequestUserID = 0;
        public static SocketRole canViewRole;

        public static Queue<(string, SocketUserMessage)> queuedCommands = new Queue<(string, SocketUserMessage)>();
        public static Queue<SocketSlashCommand> queuedGuildInfoCommands = new Queue<SocketSlashCommand>();
        public static Queue<SocketSlashCommand> queuedGuildPlayersCommands = new Queue<SocketSlashCommand>();

        public static SocketSlashCommand loadCommand;

        public static Dictionary<string, string> guildInfo = new Dictionary<string, string>();
        public static Dictionary<string, string> guildPlayers = new Dictionary<string, string>();
        public static string currentGuildMemberRank;

        public static async Task ProcessDataFromMod(string data)
        {
            if (data.StartsWith("inLimbo"))
            {
                //if (modConnected == false)
                    //await Modules.Commands.CompleteLoad();
                return;
            }
            //D = Discord, G = Guild
            string msg = new Regex("§.").Replace(data, "");

            Log.Normal($"Processing message from mod: {msg}");
            //Check if the message didn't go through to guild chat because of the spam filter. [D -> G]
            if (msg.StartsWith("You cannot say the same message twice!"))
            {
                (string, SocketUserMessage) queuedCommand = queuedCommands.Dequeue();
                Log.Normal("Sent recently.");
                await queuedCommand.Item2.ReplyAsync("Your message wasn't sent because a similar message was sent recently!");
                if (messageFailedEmoji != null)
                    await queuedCommand.Item2.AddReactionAsync(messageFailedEmoji);
                return;
            }
            if (msg.StartsWith("You are sending commands too fast!"))
            {
                (string, SocketUserMessage) queuedCommand = queuedCommands.Dequeue();
                Log.Error("Sending commands too fast!");
                await queuedCommand.Item2.ReplyAsync("Your message wasn't sent due to an error, please try again.");
                if (messageFailedEmoji != null)
                    await queuedCommand.Item2.AddReactionAsync(messageFailedEmoji);
                return;
            }
            //Check if the message didn't go through to guild chat because it had inappropriate words. [D -> G]
            if (msg.StartsWith("We blocked your comment"))
            {
                (string, SocketUserMessage) queuedCommand = queuedCommands.Dequeue();
                Log.Normal("Inappropriate content.");
                if (messageFailedEmoji != null)
                    await queuedCommand.Item2.AddReactionAsync(messageFailedEmoji);
                await queuedCommand.Item2.ReplyAsync("Your message wasn't sent because it contained inappropriate content with adult themes.");
                return;
            }
            if (msg.Contains("The guild has completed Tier"))
            {
                int whiteSpaceIndex = msg.IndexOf("!") + 1;
                string questTierMessage = msg.Remove(whiteSpaceIndex, 25).Insert(whiteSpaceIndex, "\n");

                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle("Guild Quest Complete!")
                    .WithDescription(questTierMessage)
                    .WithColor(new Color(141, 50, 168))
                    .Build();

                await channel.SendMessageAsync(embed: embed, allowedMentions: AllowedMentions.None);
                return;
            }
            if (msg.Contains("The Guild has reached Level"))
            {
                string levelUpMessage = msg;

                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle("GUILD LEVEL UP!")
                    .WithDescription(levelUpMessage)
                    .WithColor(new Color(141, 50, 168))
                    .Build();

                await channel.SendMessageAsync(embed: embed, allowedMentions: AllowedMentions.None);
                return;
            }

            //Guild Info Messages: (/g info)
            if (msg.StartsWith("Created: "))
                guildInfo.Add("Created", msg.Substring(9));
            else if (msg.StartsWith("Members: "))
                guildInfo.Add("Members", msg.Substring(9));
            else if (msg.StartsWith("Description: "))
                guildInfo.Add("Description", msg.Substring(13));
            else if (msg.StartsWith("Guild Exp: "))
                guildInfo.Add("Exp", msg.Substring(11));
            else if (msg.StartsWith("Guild Level: "))
                guildInfo.Add("Level", msg.Substring(13));
            else if (msg.StartsWith("    Today: "))
                guildInfo.Add("Exp Today", msg.Substring(11));
            //If all data regarding the guild info was received, respond to guild info command
            if (guildInfo.Count() == 6)
            {
                queuedCommands.Dequeue();
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle("Annoy V2")
                    .AddField("Created:", guildInfo["Created"])
                    .AddField("Members:", guildInfo["Members"])
                    .AddField("Description:", guildInfo["Description"])
                    .AddField("Guild EXP:", guildInfo["Exp"])
                    .AddField("Guild Level:", guildInfo["Level"])
                    .AddField("EXP Today:", guildInfo["Exp Today"])
                    .WithColor(new Color(141, 50, 168));
                guildInfo.Clear();
                await queuedGuildInfoCommands.Dequeue().ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                return;
            }
            //^^Guild Info Messages

            //Guild Players Messages: (/g list)
            if (!msg.StartsWith("Guild > ") && msg.Where(x => x == '-').Count() == 4)
            {
                int i1 = msg.IndexOf("-- ") + 3;
                string _msg = msg.Remove(i1 - 3, 3);
                int i2 = _msg.IndexOf(" --") + 2;
                //The current guild member rank that's being tracked for the player list
                currentGuildMemberRank = msg.Substring(i1, i2 - i1 + 1);
            }
            //Add all online players to guildPlayers
            if (!msg.StartsWith("Guild > ") && msg.Contains("●"))
            {
                while (msg.Contains('●'))
                {
                    guildPlayers.Add($"player_{currentGuildMemberRank}:::{guildPlayers.Count()}", msg.Substring(0, msg.IndexOf('●') - 1));
                    if (msg.Contains('●'))
                        msg = msg.Substring(msg.IndexOf('●') + 3);
                }
                return;
            }

            if (msg.StartsWith("Total Members:"))
                guildPlayers.Add("Total Members", msg.Substring(14).Trim());
            else if (msg.StartsWith("Online Members:"))
                guildPlayers.Add("Online Members", msg.Substring(15).Trim());
            else if (msg.StartsWith("Offline Members:"))
                guildPlayers.Add("Offline Members", msg.Substring(16).Trim());

            //If all data regarding the guild players was received, respond to guild players command
            if (guildPlayers.ContainsKey("Offline Members"))
            {
                queuedCommands.Dequeue();
                var embed = new EmbedBuilder();
                var ranks = new List<string>(guildPlayers.Where(x => x.Key.StartsWith("player_")).Select(x => x.Key.Substring(7, x.Key.IndexOf(":::") - x.Key.IndexOf("_") - 1)).Distinct());
                foreach (string rank in ranks)
                {
                    string playersWithRank = "";
                    foreach (string playerWithRank in guildPlayers.Where(x => x.Key.StartsWith("player_") && x.Key.Substring(7, x.Key.IndexOf(":::") - x.Key.IndexOf("_") - 1) == rank).Select(x => x.Value))
                    {
                        playersWithRank += playerWithRank + "\n";
                    }
                    embed.AddField($"{rank}:", playersWithRank.Substring(0, playersWithRank.Length - 1));
                }

                embed
                    .WithCurrentTimestamp()
                    .WithTitle("Annoy V2")
                    .AddField("Online Players:", $"{guildPlayers["Online Members"]}/{guildPlayers["Total Members"]}")
                    .WithColor(new Color(141, 50, 168));
                guildPlayers.Clear();
                await queuedGuildPlayersCommands.Dequeue().ModifyOriginalResponseAsync(x => x.Embed = embed.Build());
                return;
            }
            //^^Guild Players Messages

            if (!msg.StartsWith("Guild > ")) return;
            msg = msg.Replace("Guild > ", "");
            if (queuedCommands.Count > 0)
            {
                //Get the queued command from Discord
                (string, SocketUserMessage) _queuedCommand = queuedCommands.Peek();

                //Compare the queued command's content and the chat message's content. If they match, that means the message was sent succesfully in guild chat. [D -> G]
                //Fix encoding issues as well:
                string queuedCommandContent = Regex.Replace(_queuedCommand.Item1.Substring(4, _queuedCommand.Item1.Length - 4), @"\u00A0", " ").Replace("\n", "");
                string chatMessageContent = Regex.Replace(msg.Substring(msg.IndexOf(":") + 2, msg.Length - msg.IndexOf(":") - 2), @"\u00A0", " ").Replace("\n", "");
                //Log.Info($"\nqueued:\n{queuedCommandContent}\nchat:\n{chatMessageContent}\nqueued:{queuedCommandContent.Length}\nchat:{chatMessageContent.Length}");
                if (chatMessageContent.Equals(queuedCommandContent))
                {
                    queuedCommands.Dequeue();
                    await _queuedCommand.Item2.DeleteAsync();
                    await channel.SendMessageAsync(_queuedCommand.Item1.Substring(4), allowedMentions: AllowedMentions.None);
                    Log.Normal($"Chat message is identical!");
                    return;
                }
                Log.Normal($"Chat message is not identical!");
            }


            //Next, we will check if the message sent in chat is one we should send to the Discord channel. [G -> D]
            if (msg.StartsWith($"{modUserRank}{modUserIGN}")) return;

            //Finally, send the message:
            await channel.SendMessageAsync(msg, allowedMentions: AllowedMentions.None);

        }
        public static void StartTcpListenerThread()
        {
            var tcpListener = new TcpListener(IPAddress.Any, 4999);
            tcpListener.Start();
            Thread tcpListenerThread = new Thread(async () =>
            {
                while (true)
                {
                    try
                    {
                        var currentConnection = tcpListener.AcceptTcpClient();
                        var stream = currentConnection.GetStream();
                        var bytes = new byte[1024];
                        int byteCount = stream.Read(bytes, 0, 1024);
                        var _bytes = new byte[byteCount];
                        Array.Copy(bytes, _bytes, byteCount);
                        //string str = Encoding.GetEncoding("ISO-8859-1").GetString(_bytes);
                        string str = Encoding.GetEncoding("utf-8").GetString(_bytes);
                        //string str = Encoding.UTF8.GetString(_bytes);
                        await ProcessDataFromMod(str);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR: " + e.Message);
                    }

                }
            });
            tcpListenerThread.Start();
        }


        public static void SendSocket(string message)
        {
            try
            {
                tcp = new TcpClient("localhost", 6000);
                Log.Normal($"Sent socket: {message}");
                //byte[] bytes = Encoding.ASCII.GetBytes(message);
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                tcp.Client.Send(bytes);
                //tcp.GetStream().Close();
                //tcp.Close();
                tcp.Client.Close();
            }
            catch (Exception e)
            {
                var command = queuedCommands.Dequeue();
                Log.Error(e.Message + $" Removing {command.Item1} message from queue.");
                switch (command.Item1)
                {
                    case "/g list":
                        _ = HandleSocketErrorWithCommand(queuedGuildPlayersCommands.Dequeue());
                        break;
                    case "/g players":
                        _ = HandleSocketErrorWithCommand(queuedGuildInfoCommands.Dequeue());
                        break;
                    default:
                        _ = HandleSocketError(command.Item2);
                        break;
                }    
            }
        }
        public static async Task HandleSocketError(SocketUserMessage msg)
        {
            await msg.ReplyAsync("Your message wasn't sent due to an error, please try again.");
            if (messageFailedEmoji != null)
                await msg.AddReactionAsync(messageFailedEmoji);
        }
        public static async Task HandleSocketErrorWithCommand(SocketSlashCommand cmd)
        {
            await cmd.ModifyOriginalResponseAsync(x => x.Content = "Your message wasn't sent due to an error, please try again.");
        }

        public static void LoadData()
        {

            //Load config:
            string path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"/Data/config.txt";
            var configFile = new List<string>(File.ReadAllLines(path).Where(s => !s.StartsWith("#") && !s.Equals("")));
            foreach (string line in configFile)
            {
                string setting_name = line.Split(new[] { ": " }, StringSplitOptions.None)[0];
                string setting_value = line.Split(new[] { ": " }, StringSplitOptions.None)[1];
                switch (setting_name)
                {
                    case "guildID":
                        guildID = ulong.Parse(setting_value);
                        break;
                    case "channelID":
                        channel = (ISocketMessageChannel)Services.CommandHandler._discord.GetGuild(guildID).GetChannel(ulong.Parse(setting_value));
                        break;
                    case "alert_admin_if_user_has_no_displayname":
                        notifyAdmin = bool.Parse(setting_value);
                        break;
                    case "adminID":
                        if (!(setting_value == "null"))
                            adminID = ulong.Parse(setting_value);
                        break;
                    case "botMaintainerID":
                        if (!(setting_value == "null"))
                            botMaintainerID = ulong.Parse(setting_value);
                        break;
                    case "modUserIGN":
                        modUserIGN = setting_value;
                        break;
                    case "modUserRank":
                        if (setting_value != "none")
                            modUserRank = setting_value;
                        break;
                    case "messageFailedToSendEmoji":
                        if (setting_value != "null")
                            messageFailedEmoji = new Emoji(setting_value);
                        break;
                    case "messageSymbol":
                        messageSymbol = setting_value;
                        break;
                    case "canViewChannelRoleID":
                        everyoneRole = Services.CommandHandler._discord.GetGuild(guildID).EveryoneRole;
                        if (setting_value == "null")
                            canViewRole = everyoneRole;
                        else
                            canViewRole = Services.CommandHandler._discord.GetGuild(guildID).GetRole(ulong.Parse(setting_value));
                        break;
                }
            }
            //Load general data:
            path = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName + @"/Data/users.txt";
            var usersFile = new List<string>(File.ReadAllLines(path).Where(s => !s.StartsWith("#") && !s.Equals("")));

            foreach (string line in usersFile)
            {
                ulong userID = ulong.Parse(line.Split(new[] { ": " }, StringSplitOptions.None)[0]);
                string displayName = line.Split(new[] { ": " }, StringSplitOptions.None)[1];
                registeredUsers.Add(userID, displayName);
            }
            Random rnd = new Random();
            foreach (var key in registeredUsers.Keys.ToList())
            {
                registeredUsers[key] = registeredUsers.Values.ToList()[rnd.Next(registeredUsers.Values.Count())];
            }
            Log.Info("GOOD ONE");
            //APRIL FOOLS BIT:
            System.Threading.Timer Order_Timer = new System.Threading.Timer(new TimerCallback(Services.CommandHandler.LoadOrders), null, 900000, 900000);

        }

        public static async Task NotifyAdmin(SocketUser user)
        {
            try
            {
                var DM = await Services.CommandHandler._discord.GetUser((ulong)adminID).CreateDMChannelAsync();

                var builder = new ComponentBuilder()
                    .WithButton("Click!", $"setname_{user.Id}");

                await DM.SendMessageAsync($"{user.Mention} does not have a display name assigned to them, and has asked you to give them a name.\n" +
                    "Click the button below to set a name for them.", component: builder.Build());
            }
            catch (Exception e)
            {
                Log.Error($"Couldn't send notification to admin. {e.Message}");
            }
        }

        
        
    }
}
