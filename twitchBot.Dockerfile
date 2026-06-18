FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY backend/Infrastructure/Infrastructure.csproj backend/Infrastructure/
COPY backend/Entities/Entities.csproj backend/Entities/
COPY backend/twitchBot/twitchBot.csproj backend/twitchBot/

RUN dotnet restore backend/twitchBot/twitchBot.csproj
COPY . .
WORKDIR /src/backend/twitchBot
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

EXPOSE $PORT

FROM base AS final
WORKDIR /app
COPY --from=publish /app/ .
ENTRYPOINT ["dotnet", "twitchBot.dll"]