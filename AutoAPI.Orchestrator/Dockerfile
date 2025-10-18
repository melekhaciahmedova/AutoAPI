# --------------------------------------------------------
# AutoAPI Builder (Rebuild Container)
# Bu container, .NET SDK içerir ve sürekli açık kalır.
# --------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:8.0

# Çalışma dizini
WORKDIR /src

# Host’taki tüm dosyaları paylaşmaya gerek yok, sadece mount edilir.
# (COPY . .) gerekmez, çünkü docker-compose volume zaten /src'ye mount edecek.

# Container’ın sürekli açık kalması için:
ENTRYPOINT ["tail", "-f", "/dev/null"]