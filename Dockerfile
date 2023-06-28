FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY *.sln ./
COPY *.csproj ./
RUN dotnet restore
COPY . .
WORKDIR /src/
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

EXPOSE 5000/tcp

FROM base AS final
WORKDIR /app
COPY --from=publish /app/ .
ENTRYPOINT ["dotnet", "twitchBot.dll"]