FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pacman.NET/Pacman.NET.csproj", "Pacman.NET/"]
RUN dotnet restore "Pacman.NET/Pacman.NET.csproj"
COPY . .
WORKDIR "/src/Pacman.NET"
RUN dotnet build "Pacman.NET.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "Pacman.NET.csproj" -c Release -o /app/publish/Pacman.NET

WORKDIR /app/publish/Pacman.NET
RUN tar -czf Pacman.NET.tar.gz ./*

CMD ["cp", "/app/publish/Pacman.NET/Pacman.NET.tar.gz", "/app/final/Pacman.NET.tar.gz"]