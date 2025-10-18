FROM mcr.microsoft.com/dotnet/sdk:8.0

# Bash kabuğu kullanalım (Debian tabanlı SDK image’ta var)
SHELL ["/bin/bash", "-c"]

WORKDIR /src

# Container’ın daima açık kalması için
ENTRYPOINT ["bash", "-c", "echo '🛠 Builder container is running with .NET SDK ready...'; while true; do sleep 3600; done"]