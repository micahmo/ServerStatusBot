# Set the Docker host to the first command-line argument (if any)
# Example usage: .\rebuild_docker_release.ps1 ssh://user@server
$env:docker_host = $args[0]

# Rebuild/publish the code
dotnet publish -c Release

# Stop/delete the existing container, then delete the existing image
docker stop serverstatusbot-release-container
docker rm serverstatusbot-release-container
docker rmi serverstatusbot-release

# Rebuild the image, recreate the container, and start the container
docker build -t serverstatusbot-release -f Dockerfile_release .

# Use Docker run to create + start the container
# -d: Detached mode (so that we're not stuck with stdout in tty)
# --net=host: Binding the host allows the container name to inherit the host's hostname
docker run -d --net=host --restart always --name serverstatusbot-release-container serverstatusbot-release

$env:docker_host = ""