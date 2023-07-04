FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Entities/Entities.csproj Entities/
COPY twitchBot/twitchBot.csproj twitchBot/

RUN dotnet restore twitchBot/twitchBot.csproj
COPY . .
WORKDIR /src/twitchBot
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

EXPOSE $PORT/tcp

FROM base AS final
WORKDIR /app
COPY --from=publish /app/ .
ENTRYPOINT ["dotnet", "twitchBot.dll"]