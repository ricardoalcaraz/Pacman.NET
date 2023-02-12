FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Pacman.NET.PersonalRepo/Pacman.NET.PersonalRepo.csproj", "Pacman.NET.PersonalRepo/"]
RUN dotnet restore "Pacman.NET.PersonalRepo/Pacman.NET.PersonalRepo.csproj"
COPY . .
WORKDIR "/src/Pacman.NET.PersonalRepo"
RUN dotnet build "Pacman.NET.PersonalRepo.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Pacman.NET.PersonalRepo.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Pacman.NET.PersonalRepo.dll"]
