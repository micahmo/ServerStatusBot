using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using QBittorrent.Client;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ServerStatusBot
{
    class Program
    {
        #region Main method

        static void Main(string[] args)
        {
            //Test
            _startDateTime = DateTimeOffset.UtcNow;

            string botTokenEnv = Environment.GetEnvironmentVariable("BOT_TOKEN");
            if (string.IsNullOrEmpty(botTokenEnv))
            {
                Console.WriteLine("Error retrieving Telegram bot token. Be sure to set BOT_TOKEN environment variable. " +
                                  "If running from Visual Studio, set the env vars in settings.env");
                return;
            }
            else
            {
                _botClient = new TelegramBotClient(botTokenEnv);
            }

            string chatIdEnv = Environment.GetEnvironmentVariable("CHAT_ID");
            if (string.IsNullOrEmpty(chatIdEnv))
            {
                Console.WriteLine("Error retrieving chat ID. Be sure to set CHAT_ID environment variable. " +
                                  "If running from Visual Studio, set the env vars in settings.env");
                return;

            }
            else
            {
                _chatId = Convert.ToInt32(chatIdEnv);
            }

            string qBittorrentServerEnv = Environment.GetEnvironmentVariable("QBITTORRENT_SERVER");
            string qBittorrentUsernameEnv = Environment.GetEnvironmentVariable("QBITTORRENT_USERNAME");
            string qBittorrentPasswordEnv = Environment.GetEnvironmentVariable("QBITTORRENT_PASSWORD");
            if (string.IsNullOrEmpty(qBittorrentServerEnv) || string.IsNullOrEmpty(qBittorrentUsernameEnv) || string.IsNullOrEmpty(qBittorrentPasswordEnv))
            {
                Console.WriteLine("Warning: Unable to retrieve one or more qBittorrent environment variables. qBittorrent status will be unavailable.");
            }
            else
            {
                _qBittorrentServer = qBittorrentServerEnv;
                _qBittorrentUsername = qBittorrentUsernameEnv;
                _qBittorrentPassword = qBittorrentPasswordEnv;
            }

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

            try
            {
                if (messageText.ToLower() == "/status" && chatId.Identifier == _chatId.Identifier)
                {
                    var startTimeInEt = TimeZoneInfo.ConvertTime(_startDateTime, GetEasternTimeZone());

                    await _botClient.SendTextMessageAsync(
                        chatId: _chatId,
                        text: string.Join(Environment.NewLine,
                            $"Status is good. Running on container '{Environment.MachineName}'. Platform is {GetOsPlatform()}.",
                            $"Bot uptime is {DateTimeOffset.UtcNow - _startDateTime} (since {startTimeInEt})."),
                        parseMode: ParseMode.Html
                    );
                }
                else if (messageText.ToLower() == "/qbittorrentstatus" && chatId.Identifier == _chatId.Identifier && string.IsNullOrEmpty(_qBittorrentServer) == false)
                {
                    // Instantiate the client
                    QBittorrentClient qBittorrentClient = new QBittorrentClient(new Uri($"http://{_qBittorrentServer}:8080"));
                    ApiVersion apiVersion = default;

                    try
                    {
                        await qBittorrentClient.LoginAsync(_qBittorrentUsername, _qBittorrentPassword);
                        apiVersion = await qBittorrentClient.GetApiVersionAsync();
                    }
                    catch (HttpRequestException ex) when (ex.InnerException is SocketException || ex is QBittorrentClientRequestException)
                    {
                        if ((ex.InnerException as SocketException)?.ErrorCode == 111)
                        {
                            // This means the login failed, which we will handle below.
                            await _botClient.SendTextMessageAsync(
                                chatId: _chatId,
                                text: "There was an error communicating with the qBittorrent server. It may be offline.");

                            return;
                        }
                        
                        if ((ex as QBittorrentClientRequestException)?.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // This means the login failed, which we will handle below.
                            await _botClient.SendTextMessageAsync(
                                chatId: _chatId,
                                text: "There was an error communicating with the qBittorrent server. You may have the wrong username/password.");

                            return;
                        }

                        throw;
                    }

                    // If we get here the server is online.
                    await _botClient.SendTextMessageAsync(
                        chatId: _chatId,
                        text: $"The qBittorrent server is online and running API version {apiVersion}.");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: "There was an error processing your request.");

                Trace.WriteLine(string.Join(Environment.NewLine,
                    "Processing Error.",
                    $"Message:  {messageText}",
                    $"Sender name / ID:  {e.Message.Chat.Username} / {chatId}",
                    "",
                    ex.ToString()));
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

        private static ChatId _chatId;

        private static string _qBittorrentServer;

        private static string _qBittorrentUsername;

        private static string _qBittorrentPassword;

        #endregion
    }
}
