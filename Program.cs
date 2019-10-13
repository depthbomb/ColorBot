using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Discord;
using Discord.WebSocket;

namespace ColorBot
{
    public class Program
    {
        private DiscordSocketClient client;
        //  Bot owner ID, for admin commands
        private ulong[] adminIds = new ulong[] { 133325534548590594, 191791042326953984};
        //  Guild the bot will operate in
        private ulong guildId = 500165128793096193;
        //  Channel in which the role commands will work in
        private ulong roleChannel = 507876423533592586;
        //  The game the bot will appear to be playing
        private string playingGame = "By depthbomb for Coby | ?about";

        private IEnumerable<IRole> validRoles;

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string token = args[0] ?? null;
                new Program().StartBot(token).GetAwaiter().GetResult();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("You must launch the bot with arguments.");
                Console.ResetColor();
                Console.ReadKey();
            }
        }

        public async Task StartBot(string token)
        {
            if (token == null)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Bot token is missing. Please launch the bot with its first argument being its token.");
                Console.ResetColor();
                Console.ReadKey();
            }
            else
            {
                client = new DiscordSocketClient();
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
                await client.SetGameAsync(playingGame);

                #region Events
                client.Log += Client_Log;
                client.Ready += Client_Ready;
                client.MessageReceived += Client_MessageReceived;
                #endregion

                await Task.Delay(-1);
            }
        }

        private Task Client_Ready()
        {
            LoadRoles();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Load up the roles
        /// </summary>
        private void LoadRoles()
        {
            SocketGuild guild = client.GetGuild(guildId);

            //  A hardcoded array of valid color rules that the bot will work with
            ulong[] roleIds = new ulong[]
            {
                500194642458050561, //  Light Blue
                500172092214607875, //  Blue
                500194724154703873, //  Light Purple
                500173202987679765, //  Purple
                500188030594973696, //  Orange
                500194776373657601, //  Light Orange
                500171852568723456, //  Red
                500201785399705602, //  Pink
                500270967109582855, //  Green
                515875271946141711, //  Black
            };

            //  Grab all of these roles from the guild
            validRoles = guild.Roles.Where(r => roleIds.Contains(r.Id));

            foreach (IRole role in validRoles)
                Console.WriteLine($"Loaded role {role.Name}");
        }

        private Task Client_Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles messages that the bot can see
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task Client_MessageReceived(SocketMessage message)
        {
            ISocketMessageChannel channel = message.Channel;
            SocketGuildUser author = message.Author as SocketGuildUser;

            //  Command prefix, currently just hardcoded but wouldn't be too hard to turn into a configurable value
            char prefix = '?';

            //  Get the entire message content with whitespace trimmed from both ends
            string messageContent = message.Content.Trim();

            //  A bit of over-complicating the process of splitting a message up into parts for processing commands
            //  We split it by one or more spaces just in case the user accidentally adds more than one space between
            //  the command and its arguments
            string[] messageParts = Regex.Split(messageContent, @"\s+");

            //  The first word of the message which CAN be the command (see `isCommand`)
            string command = messageParts.First();

            //  Everything else in the message that could be command arguments
            string[] args = messageParts.Skip(1).ToArray();

            //  Returns true if `command` starts with our prefix
            bool isCommand = command.StartsWith(prefix.ToString());

            string mention = $"<@{author.Id}>";
            string footer = $"\n\nType `?color help` in this channel to learn how to change your color role.";

            //  Only process commands if the message is sent in the role channel
            if (channel.Id == roleChannel)
            {
                if (isCommand)
                {
                    switch (command.TrimStart(prefix))
                    {
                        #region Color
                        case "color":
                        case "colour":
                        case "colors":
                        case "colours":
                            //  Put all the color role names into an array
                            string[] validRoleNames = validRoles.Select(r => r.Name).ToArray();
                            if (args.Length > 0)
                            {
                                string action = args[0];
                                if (action == "add")
                                {
                                    string desiredRole = string.Join(" ", args.Skip(1));
                                    IRole colorRole = validRoles.Where(r => string.Equals(r.Name, desiredRole, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
                                    if (colorRole != null)
                                    {
                                        //  Remove all other color roles
                                        await author.RemoveRolesAsync(validRoles);
                                        //  ...and give them their desired color!
                                        await author.AddRoleAsync(colorRole);

                                        await channel.SendMessageAsync($"{mention}, you have been given the `{desiredRole}` role! {footer}");
                                    }
                                    else
                                    {
                                        await channel.SendMessageAsync($"{mention}, the role `{desiredRole}` is not available. {footer}");
                                    }
                                }
                                else if (action == "clear")
                                {
                                    await author.RemoveRolesAsync(validRoles);
                                    await channel.SendMessageAsync($"{mention}, your roles have been cleared! {footer}");
                                }
                                else
                                {
                                    string[] helpMessage = new string[]
                                    {
                                        $"Type `{prefix}color` to get a list of all available color roles",
                                        $"Type `{prefix}color clear` to remove all of your color roles",
                                        $"Type `{prefix}color add <role name>` to get that color role",
                                        $"\nType `{prefix}color help` to see this message",
                                    };
                                    await channel.SendMessageAsync(string.Join("\n", helpMessage));
                                }
                            }
                            else
                            {
                                await channel.SendMessageAsync($"{mention}, available colors are: {string.Join(", ", validRoleNames)}\n\nRole names are **not** case-sensitive. Hurrah! {footer}");
                            }
                            break;
                        #endregion
                        case "about":
                        case "info":
                            await channel.SendMessageAsync($"I'm a bot developed by depthbomb#0163 for the `Something About A Snep` Discord server. I am written in C# on .NET Core 3.0.\n\nI'm also open source! Check out my innards here: https://github.com/depthbomb/ColorBot");
                            break;
                    }
                }
            }
            else
            {
                //  In case we want to do something based on non-command messages...
            }

            await Task.CompletedTask;
        }
    }
}
