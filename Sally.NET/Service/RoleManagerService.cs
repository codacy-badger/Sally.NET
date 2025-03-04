﻿
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Sally.NET.Core;
using Sally.NET.Core.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sally.NET.Service
{
    public static class RoleManagerService
    {
        public static Dictionary<int, ulong> roleDictionary = new Dictionary<int, ulong>
        {
            { 0, 583712540069199893},
            { 5, 583712661347237888},
            { 10, 583712739407691817},
            { 15, 583712794101284894},
            { 20, 583712865878409226},
            { 25, 583712966327926785},
            { 30, 583713031410679826},
            { 35, 583713106623201281},
            { 40, 583713192258306058},
            { 45, 583713277792485389},
            { 50, 583713406247239843},
            { 55, 583713511176404992},
            { 60, 583713745595793410},
            { 65, 583714001687412746},
            { 70, 583714264070619158},
            { 75, 583714339018637314}

        };

        public static Dictionary<ulong, Dictionary<int, ulong>> RankRoleCollection = new Dictionary<ulong, Dictionary<int, ulong>>();
        public static BotCredentials credentials { get; set; }
        public static DiscordSocketClient client { get; private set; }
        public static SocketGuild myGuild { get; set; }

        public static void InitializeHandler(DiscordSocketClient client, BotCredentials credentials)
        {
            RoleManagerService.credentials = credentials;
            RoleManagerService.client = client;
            fetchAllServer();
            GuildUser.OnLevelUp += GuildUser_LevelUp;
        }
        private static async void GuildUser_LevelUp(GuildUser myGuildUser)
        {
            ulong guildId = myGuildUser.GuildId;
            if (RankRoleCollection.ContainsKey(myGuildUser.GuildId))
            {
                SocketGuild guild = client.GetGuild(myGuildUser.GuildId);
                Dictionary<int, ulong> SpecificRoleCollection = RankRoleCollection[guildId];
                //check if user level is a key
                if (SpecificRoleCollection.ContainsKey((int)myGuildUser.Level))
                {
                    //get value of key-specific value
                    ulong roleId = SpecificRoleCollection.GetValueOrDefault((int)myGuildUser.Level);
                    //get user from guild
                    SocketGuildUser guildUser = guild.Users.ToList().Find(u => u.Id == myGuildUser.Id);
                    //remove any other level-specific roles
                    foreach (KeyValuePair<int, ulong> entry in SpecificRoleCollection)
                    {
                        if (guildUser.Roles.ToList().Find(r => r.Id == entry.Value) != null)
                            await guildUser.RemoveRoleAsync(guild.Roles.ToList().Find(r => r.Id == entry.Value));
                    }
                    //add new role to user
                    await guildUser.AddRoleAsync(myGuild.Roles.ToList().Find(r => r.Id == roleId));
                }
                CreateOrAddRole(guild, $"Level {myGuildUser.Level}", myGuildUser.Id, new[] { $"Level " });
            }

        }
        public static async void CreateOrAddRole(SocketGuild guild, string role, ulong id, string[] removeCriteria = null, Color? color = null)
        {
            SocketRole newRole = guild.Roles.ToList().Find(r => r.Name == role);
            if (newRole == null)
            {
                await guild.CreateRoleAsync(role, color: color, isMentionable: false);
                while (newRole == null)
                {
                    newRole = guild.Roles.ToList().Find(r => r.Name == role);
                }
            }
            SocketGuildUser gUser = guild.Users.ToList().Find(u => u.Id == id);
            if (removeCriteria != null)
            {
                SocketRole oldRole = gUser.Roles.ToList().Find(r => removeCriteria.ToList().Find(c => r.Name.Contains(c)) != null);
                if (oldRole != null)
                {
                    await gUser.RemoveRoleAsync(oldRole);
                }
            }
            await gUser.AddRoleAsync(newRole);
        }

        private static void fetchAllServer()
        {
            if (!File.Exists("meta/rankRoles.json"))
            {
                File.Create("meta/rankRoles.json").Dispose();
            }
            RankRoleCollection = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<int, ulong>>>(File.ReadAllText("meta/rankRoles.json"));
            if (RankRoleCollection == null)
            {
                RankRoleCollection = new Dictionary<ulong, Dictionary<int, ulong>>();
            }
            List<SocketGuild> guilds = client.Guilds.ToList();
            foreach (SocketGuild guild in guilds)
            {
                if (!RankRoleCollection.ContainsKey(guild.Id))
                {
                    RankRoleCollection.Add(guild.Id, new Dictionary<int, ulong>());
                }
            }
            SaveRankRoleCollection();
        }

        public static void SaveRankRoleCollection()
        {
            File.WriteAllText("meta/rankRoles.json", JsonConvert.SerializeObject(RankRoleCollection));
        }
    }
}
