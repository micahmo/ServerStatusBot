# Set the Docker host to the first command-line argument (if any)
# Example usage: .\rebuild_docker_release.ps1 ssh://user@server
$env:docker_host = $args[0]

# Stop/delete the existing container, then delete the existing image
docker stop serverstatusbot-release-container
docker rm serverstatusbot-release-container
docker rmi serverstatusbot-release

$env:docker_host = ""