FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pacman.NET.BlazorServer/Pacman.NET.BlazorServer.csproj", "Pacman.NET.BlazorServer/"]
RUN dotnet restore "Pacman.NET.BlazorServer/Pacman.NET.BlazorServer.csproj"
COPY . .
WORKDIR "/src/Pacman.NET.BlazorServer"
RUN dotnet build "Pacman.NET.BlazorServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pacman.NET.BlazorServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pacman.NET.BlazorServer.dll"]
