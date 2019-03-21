﻿using Discord.WebSocket;
using Discord_Chan.db;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Chan.services
{
    static class UserManagerService
    {
        public static async Task InitializeHandler(DiscordSocketClient client)
        {
            client.UserJoined += Client_UserJoined;
        }

        private static async Task Client_UserJoined(SocketGuildUser userNew)
        {
            //check if the user is complete new
            User joinedUser = DataAccess.Instance.users.Find(u => u.Id == userNew.Id);
            if (joinedUser == null)
            {
                User user = new User(userNew.Id, 10, false);
                DataAccess.Instance.InsertUser(user);
            }
            //user was on this server before
            else
            {
                DataAccess.Instance.LoadSpecUser(joinedUser);
            }
        }
    }
}
