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
            _qBittorrentServer = File.ReadAllLines("qBittorrent.txt")[0];
            _qBittorrentUsername = File.ReadAllLines("qBittorrent.txt")[1];
            _qBittorrentPassword = File.ReadAllLines("qBittorrent.txt")[2];

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
                if (messageText.ToLower() == "/status" && chatId.Identifier == _micahmo.Identifier)
                {
                    var startTimeInEt = TimeZoneInfo.ConvertTime(_startDateTime, GetEasternTimeZone());

                    await _botClient.SendTextMessageAsync(
                        chatId: _micahmo,
                        text: string.Join(Environment.NewLine,
                            $"Status is good. Running on container '{Environment.MachineName}'. Platform is {GetOsPlatform()}.",
                            $"Bot uptime is {DateTimeOffset.UtcNow - _startDateTime} (since {startTimeInEt})."),
                        parseMode: ParseMode.Html
                    );
                }
                else if (messageText.ToLower() == "/qbittorrentstatus" && chatId.Identifier == _micahmo.Identifier)
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
                                chatId: _micahmo,
                                text: "There was an error communicating with the qBittorrent server. It may be offline.");

                            return;
                        }
                        
                        if ((ex as QBittorrentClientRequestException)?.StatusCode == HttpStatusCode.Forbidden)
                        {
                            // This means the login failed, which we will handle below.
                            await _botClient.SendTextMessageAsync(
                                chatId: _micahmo,
                                text: "There was an error communicating with the qBittorrent server. You may have the wrong username/password.");

                            return;
                        }

                        throw;
                    }

                    // If we get here the server is online.
                    await _botClient.SendTextMessageAsync(
                        chatId: _micahmo,
                        text: $"The qBittorrent server is online and running API version {apiVersion}.");
                }
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _micahmo,
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

        private static ChatId _micahmo;

        private static string _qBittorrentServer;

        private static string _qBittorrentUsername;

        private static string _qBittorrentPassword;

        #endregion
    }
}
