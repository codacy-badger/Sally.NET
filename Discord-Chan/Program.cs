﻿using Discord;
using Discord.WebSocket;
using Sally_NET.Command;
using Sally_NET.Config;
using Sally_NET.Database;
using Sally_NET.Service;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace Sally_NET
{
    class Program
    {
        public static BotConfiguration BotConfiguration
        {
            get;
            private set;
        }

        public static DiscordSocketClient Client
        {
            get;
            private set;
        }

        public static SocketGuild MyGuild
        {
            get;
            private set;
        }
        public static SocketUser Me
        {
            get;
            private set;
        }

        public static DateTime StartTime
        {
            get;
            private set;
        }
        private static int requestCounter;
        public static int RequestCounter
        {
            get
            {
                return requestCounter;
            }
            set
            {
                requestCounter = value;
                File.WriteAllText("ApiRequests.txt", requestCounter.ToString());
            }
        }
        private static int startValue;
        public static string GenericFooter 
        {
            get;
            private set;
        }
        public static string GenericThumbnailUrl
        {
            get;
            private set;
        }

        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                startValue = Int32.Parse(args[0]);
            }
            Console.CancelKeyPress += Console_CancelKeyPress;
            new Program().MainAsync().GetAwaiter().GetResult();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DataAccess.Instance.Dispose();
            Environment.Exit(0);
        }

        public async Task MainAsync()
        {
            StartTime = DateTime.Now;
            string[] moods = { "Sad", "Meh", "Happy", "Extatic" };
            if (!Directory.Exists("mood"))
            {
                Directory.CreateDirectory("mood");
            }
            //download content
            using (var client = new WebClient())
            {
                foreach (string item in moods)
                {
                    if (!File.Exists($"mood/{item}.png"))
                    {
                        client.DownloadFile($"https://cdn.its-sally.net/content/{item}.png", $"mood/{item}.png");
                    }
                }
            }

            BotConfiguration = JsonConvert.DeserializeObject<BotConfiguration>(File.ReadAllText("configuration.json"));
            DataAccess.Initialize(BotConfiguration);

            RequestCounter = Int32.Parse(File.ReadAllText("ApiRequests.txt"));

            Client = new DiscordSocketClient();

            VoiceRewardService.InitializeHandler(Client);
            UserManagerService.InitializeHandler(Client);
            MoodDictionary.InitializeMoodDictionary();
            WeatherSubService.InitializeWeatherSub();
            RoleManagerService.InitializeHandler();
            await CommandHandlerService.InitializeHandler(Client);
            await CachedService.InitializeHandler();
            Client.Ready += Client_Ready;

            Client.Log += Log;

            await Client.LoginAsync(TokenType.Bot, BotConfiguration.token);
            await Client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task Client_Ready()
        {
            GenericFooter = "Provided by Sally, your friendly and helpful Discordbot!";
            GenericThumbnailUrl = "https://static-cdn.jtvnw.net/emoticons/v1/279825/3.0";

            MyGuild = Client.Guilds.Where(g => g.Id == BotConfiguration.guildId).First();
            foreach (SocketGuildUser user in MyGuild.Users)
            {
                if (DataAccess.Instance.users.Find(u => u.Id == user.Id) == null)
                {
                    DataAccess.Instance.InsertUser(new Database.User(user.Id, 10, false));
                }
                //check if user is already in a voice channel
                if (user.VoiceChannel != null)
                {
                    //start tracking if user detected
                    VoiceRewardService.StartTrackingVoiceChannel(DataAccess.Instance.users.Find(u => u.Id == user.Id));
                }
                if (user.Id == BotConfiguration.meId)
                {
                    Me = user as SocketUser;
                }
            }
            StatusNotifierService.InitializeService();
            MusicCommands.Initialize(Client);
            switch (startValue)
            {
                case 0:
                    //shutdown
                    break;
                case 1:
                    //restarting
                    await Me.SendMessageAsync("I have restored and restarted successfully.");
                    break;
                case 2:
                    //updating
                    //check if an update is nessasarry
                    await Me.SendMessageAsync("I at all the new features and restarted successfully.");
                    break;
                default:
                    break;
            }
            await MoodHandleService.InitializeHandler(Client);
            
        }
    }
}
