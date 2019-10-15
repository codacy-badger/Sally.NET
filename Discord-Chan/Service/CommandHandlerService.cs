﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Sally_NET.Command;
using Sally_NET.Core;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sally_NET.Service
{
    static class CommandHandlerService
    {
        private static CommandService commands;
        private static IServiceProvider services;
        private static char prefix = '$';

        public static async Task InitializeHandler(DiscordSocketClient client)
        {
            commands = new CommandService();
            services = new ServiceCollection()
              .AddSingleton(Program.Client)
              .AddSingleton<InteractiveService>()
              .BuildServiceProvider();
            client.MessageReceived += CommandHandler;
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        /// <summary>
        /// "Parses" the user input and classifies it using <see cref="InputType"/>
        /// </summary>
        /// <param name="message">Raw input</param>
        /// <returns>Classified message including input</returns>
        private static Input ClassifyAs(SocketUserMessage message)
        {
            int argPos = 0;

            if (message.HasCharPrefix(prefix, ref argPos))
            {
                return new Input { Type = InputType.Command, Message = message, ArgumentPosition = argPos };
            }
            if (message.HasMentionPrefix(Program.Client.CurrentUser, ref argPos))
            {
                return new Input { Type = InputType.Mention, Message = message, ArgumentPosition = argPos };
            }

            // Fallback for all other types of input
            return new Input { Type = InputType.NaturalInput, Message = message };
        }

        /// <summary>
        /// Handles classified input (like responding to commands, etc.)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static async Task HandleMessage(Input input)
        {
            if (input.Type == InputType.Command || input.Type == InputType.Mention)
            {
                await HandleCommand(input);
                return;
            }

            if (input.Type == InputType.NaturalInput)
            {
                await HandleNaturalInput(input);
                return;
            }
        }

        private static async Task HandleCommand(Input input)
        {
            int argPos = input.ArgumentPosition ?? 0;

            SocketUserMessage message = input.Message;

            // Create a Command Context
            SocketCommandContext context = new SocketCommandContext(Program.Client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            IResult result = await commands.ExecuteAsync(context, argPos, services);
            string commandResult;
            if (result.Error == CommandError.UnknownCommand)
            {
                Dictionary<string, int> messageCompareValues = new Dictionary<string, int>();
                List<Type> commandClasses = typeof(Sally_NET.Command.GeneralCommands)
                    .Assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ModuleBase)) && !t.IsAbstract).ToList();
                foreach (Type commandClass in commandClasses)
                {
                    MemberInfo[] memInfo = commandClass.GetMembers();
                    foreach (MemberInfo memberInfo in memInfo)
                    {
                        string resultCommand = "";
                        Object[] att = memberInfo.GetCustomAttributes(typeof(CommandAttribute), false);
                        if (att.Length == 0)
                            continue;
                        Type parent = commandClass;
                        while (parent != null)
                        {
                            Object[] classAtt = parent.GetCustomAttributes(typeof(GroupAttribute), false);
                            if (classAtt.Length != 0)
                            {
                                resultCommand = ((GroupAttribute)classAtt[0]).Prefix + " " + resultCommand;
                            }
                            parent = parent.DeclaringType;
                        }

                        resultCommand += ((CommandAttribute)att[0]).Text;
                        string copMessage = message.Content.Substring(1);
                        int stringValue;
                        while (copMessage.Contains(" "))
                        {
                            stringValue = CalcLevenshteinDistance(resultCommand, copMessage);
                            if (!messageCompareValues.ContainsKey(resultCommand))
                            {
                                messageCompareValues.Add(resultCommand, stringValue);
                            }
                            if (messageCompareValues[resultCommand] > stringValue)
                            {
                                messageCompareValues[resultCommand] = stringValue;
                            }
                            copMessage = copMessage.Substring(0, copMessage.LastIndexOf(" "));
                        }
                        stringValue = CalcLevenshteinDistance(resultCommand, copMessage);
                        if (!messageCompareValues.ContainsKey(resultCommand))
                        {
                            messageCompareValues.Add(resultCommand, stringValue);
                        }
                        if (messageCompareValues[resultCommand] > stringValue)
                        {
                            messageCompareValues[resultCommand] = stringValue;
                        }
                    }
                }

                int minValue = messageCompareValues.Values.Min();
                commandResult = String.Join(Environment.NewLine, messageCompareValues.Where(d => d.Value == minValue).Select(k => k.Key));
                await context.Channel.SendMessageAsync($"{result.ErrorReason} ¯\\_(ツ)_/¯, but did you mean: {commandResult}");
                return;
            }

            //Error Handler
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync($"{result.ErrorReason} ¯\\_(ツ)_/¯");
        }

        private static async Task HandleNaturalInput(Input input)
        {
            SocketUserMessage message = input.Message;

            if ((message.Channel as SocketDMChannel) != null)
            {
                //dm channel
                if (message.Author.Id == Program.Client.CurrentUser.Id)
                    return;
                //privat message
                Program.RequestCounter++;


                dynamic messageOutput = JsonConvert.DeserializeObject<dynamic>(ApiRequestService.StartRequest("cleverapi", message).Result);
                await message.Channel.SendMessageAsync(messageOutput["output"].ToString());
            }
        }

        private static async Task CommandHandler(SocketMessage arg)
        {
            // Don't process the command if it was a System Message
            SocketUserMessage message = arg as SocketUserMessage;

            // Don't process the command if it is invalid
            if (message == null)
            {
                return;
            }

            if (Program.MyGuild.Users.ToList().Find(u => u.Id == message.Author.Id) == null)
            {
                return;
            }
            if(arg.Author.IsBot)
            {
                return;
            }


            //await MessageHandlerService.DeleteStartMessages(message);
            Input input = ClassifyAs(message);
            await HandleMessage(input);
        }

        public static int CalcLevenshteinDistance(string a, string b)
        {
            if (String.IsNullOrEmpty(a) && String.IsNullOrEmpty(b))
            {
                return 0;
            }
            if (String.IsNullOrEmpty(a))
            {
                return b.Length;
            }
            if (String.IsNullOrEmpty(b))
            {
                return a.Length;
            }
            int lengthA = a.Length;
            int lengthB = b.Length;
            var distances = new int[lengthA + 1, lengthB + 1];
            for (int i = 0; i <= lengthA; distances[i, 0] = i++)
                ;
            for (int j = 0; j <= lengthB; distances[0, j] = j++)
                ;

            for (int i = 1; i <= lengthA; i++)
                for (int j = 1; j <= lengthB; j++)
                {
                    int cost = b[j - 1] == a[i - 1] ? 0 : 1;
                    distances[i, j] = Math.Min
                        (
                        Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost
                        );
                }
            return distances[lengthA, lengthB];
        }
    }
}
