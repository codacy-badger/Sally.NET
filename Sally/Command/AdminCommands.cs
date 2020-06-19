﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Sally.NET.Core;
using Sally.NET.Module;
using Sally.NET.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sally.Command
{
    public class AdminCommands : ModuleBase
    {
        /// <summary>
        /// command group for admin commands
        /// </summary>
        [Group("sudo")]
        public class SudoCommands : ModuleBase
        {
            [Command("whois")]
            public async Task WhoIs(ulong userId)
            {
                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    //check if the user, which has written the message, has admin rights or is server owner
                    if (AdminModule.IsAuthorized(GeneralModule.GetGuildUserFromGuild(Context.Message.Author as SocketUser, guildChannel.Guild)))
                    {
                        await Context.Message.Channel.SendMessageAsync($"{Context.Message.Author.Username}, you dont have the permissions to do this!");
                        return;
                    }
                    if (Program.MyGuild.Users.ToList().Find(u => u.Id == userId) == null)
                    {
                        await Context.Message.Channel.SendMessageAsync("User couldn't be found.");
                        return;
                    }
                    //user has admin rights
                    await Context.Message.Channel.SendMessageAsync($"{userId} => {Program.MyGuild.Users.ToList().Find(u => u.Id == userId)}");
                }
                else
                {

                }
            }
            [Command("reverse")]
            public async Task ReverseUsernames()
            {
                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    //check if the user, which has written the message, has admin rights
                    if (AdminModule.IsAuthorized(GeneralModule.GetGuildUserFromGuild(Context.Message.Author as SocketUser, guildChannel.Guild)))
                    {
                        await Context.Message.Channel.SendMessageAsync($"{Context.Message.Author.Username}, you dont have the permissions to do this!");
                        return;
                    }
                    foreach (SocketGuildUser guildUser in Program.MyGuild.Users)
                    {
                        //"remove" owner
                        if (guildUser.Id == Program.MyGuild.OwnerId)
                            continue;
                        await guildUser.ModifyAsync(u => u.Nickname = new String((guildUser.Nickname != null ? guildUser.Nickname : guildUser.Username).Reverse().ToArray()));
                    }
                }
                else
                {
                    await Context.Message.Channel.SendMessageAsync("This command can't be used here.");
                    return;
                }
            }
        }

        /// <summary>
        /// command group for owner commands
        /// </summary>
        [Group("owner")]
        public class OwnerCommands : ModuleBase
        {
            [Command("apiRequests")]
            public async Task ShowCurrentApiRequests()
            {
                if (Context.Message.Author.Id != Program.BotConfiguration.meId)
                {
                    await Context.Message.Channel.SendMessageAsync("permission denied");
                    return;
                }
                await Program.Me.SendMessageAsync($"There are currently {Program.RequestCounter} Requests.");
            }

            [Command("shutdown")]
            public async Task ShutdownBot()
            {
                if (Context.Message.Author.Id != Program.BotConfiguration.meId)
                {
                    await Context.Message.Channel.SendMessageAsync("permission denied");
                    return;
                }
                await Program.Me.SendMessageAsync("I am shutting down now");
                Environment.Exit(0);
            }

            [Command("restart")]
            public async Task RestartBot()
            {
                if (Context.Message.Author.Id != Program.BotConfiguration.meId)
                {
                    await Context.Message.Channel.SendMessageAsync("permission denied");
                    return;
                }
                await Program.Me.SendMessageAsync("I am restarting now");
                Environment.Exit(1);
            }

            [Command("update")]
            public async Task PerformUpdate()
            {
                if (Context.Message.Author.Id != Program.BotConfiguration.meId)
                {
                    await Context.Message.Channel.SendMessageAsync("permission denied");
                    return;
                }

                //perform update
                await Program.Me.SendMessageAsync("I am updating now");
                Environment.Exit(2);
            }
        }
    }
}
