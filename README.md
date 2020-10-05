# ThumbnailCreatorBot

A [Telegram bot](https://core.telegram.org/bots) written using the C# [Telegram.Bot](https://github.com/TelegramBots/Telegram.Bot) client. Reports the status and other info about the server that it is running on.

# Building and Running

## Development
With Visual Studio and Docker Desktop installed, clone the repository and open in Visual Studio. Run using the "Docker" option in Visual Studio. It will automatically create and run a Docker container which can be inspected and debugged from Visual Studio.

To set environment variables, rename (or copy) the `settings.env.sample` file to `settings.env` and enter the desired values.

## Deployment
To run outside of a development environment, use `docker run` to pull and set up the image from Docker Hub.
```
docker run -d \
  --name=ServerStatusBot \
  --net=host \
  -e BOT_TOKEN=<your_bot_token> \
  -e CHAT_ID=<your_chat_ID> \
  -e QBITTORRENT_SERVER=<your_qBittorrent_server_optional> \
  -e QBITTORRENT_USERNAME=<your_qBittorrent_username_optional> \
  -e QBITTORRENT_PASSWORD=<your_qBittorrent_password_optional> \
  micahmo/serverstatusbot
```

## Unraid
Use the Unraid container template to easily configure and run on an Unraid server.

- In Unraid, go to the Docker tab.
- Scroll to the bottom and edit the "Template Repositories" area.
- Add `https://github.com/micahmo/docker-templates` on a new line and press Save.
- Choose Add Container.
- In the Template drop down, choose `ServerStatusBot` from the list.
- Set variables as desired and Apply.
