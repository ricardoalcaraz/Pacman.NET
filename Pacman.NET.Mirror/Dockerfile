FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Pacman.NET.Mirror/Pacman.NET.Mirror.csproj", "Pacman.NET.Mirror/"]
RUN dotnet restore "Pacman.NET.Mirror/Pacman.NET.Mirror.csproj"
COPY . .
WORKDIR "/src/Pacman.NET.Mirror"
RUN dotnet build "Pacman.NET.Mirror.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pacman.NET.Mirror.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pacman.NET.Mirror.dll"]
