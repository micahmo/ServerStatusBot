using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace ServerStatusBot
{
    class Program
    {
        #region Main method

        static void Main(string[] args)
        {
            _startDateTime = DateTimeOffset.UtcNow;

            _botClient = new TelegramBotClient(File.ReadAllLines("token.txt")[0]);
            _micahmo = Convert.ToInt32(File.ReadAllLines("micahmo.txt")[0]);

            _botClient.OnMessage += Bot_OnMessage;
            _botClient.StartReceiving();
            Thread.Sleep(int.MaxValue);
        }

        #endregion

        #region Event handlers

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            string messageText = e.Message.Text;
            ChatId chatId = e.Message.Chat.Id;

            if (messageText == "/status" && chatId.Identifier == _micahmo.Identifier)
            {
                var startTimeInEt = TimeZoneInfo.ConvertTime(_startDateTime, GetEasternTimeZone());

                await _botClient.SendTextMessageAsync(
                    chatId: _micahmo,
                    text: $"Status is good. Running on host '{Environment.MachineName}' (platform is {GetOsPlatform()}). Uptime is {DateTimeOffset.UtcNow - _startDateTime} " +
                          $"(since {startTimeInEt})",
                    parseMode: ParseMode.Html
                );
            }
        }

        #endregion

        #region Private methods

        private static TimeZoneInfo GetEasternTimeZone()
        {
            if (TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(tz => tz.Id == "America/New_York") is TimeZoneInfo linuxTz)
            {
                return linuxTz;
            }
            else if (TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(tz => tz.Id == "Eastern Standard Time") is TimeZoneInfo windowsTz)
            {
                return windowsTz;
            }
            else return null;
        }

        private static string GetOsPlatform()
        {
            return (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? OSPlatform.Windows.ToString() : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? OSPlatform.Linux.ToString() : "Unknown");
        }

        #endregion

        #region Private fields

        private static DateTimeOffset _startDateTime;

        private static TelegramBotClient _botClient;

        private static ChatId _micahmo;

        #endregion
    }
}
