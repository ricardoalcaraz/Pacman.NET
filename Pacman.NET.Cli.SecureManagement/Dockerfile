FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pacman.NET.Cli.SecureManagement/Pacman.NET.Cli.SecureManagement.csproj", "Pacman.NET.Cli.SecureManagement/"]
RUN dotnet restore "Pacman.NET.Cli.SecureManagement/Pacman.NET.Cli.SecureManagement.csproj"
COPY . .
WORKDIR "/src/Pacman.NET.Cli.SecureManagement"
RUN dotnet build "Pacman.NET.Cli.SecureManagement.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pacman.NET.Cli.SecureManagement.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pacman.NET.Cli.SecureManagement.dll"]
