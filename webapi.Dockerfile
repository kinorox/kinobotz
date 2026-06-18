FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Infrastructure/Infrastructure.csproj backend/Infrastructure/
COPY backend/Entities/Entities.csproj backend/Entities/
COPY backend/webapi/webapi.csproj backend/webapi/

RUN dotnet restore backend/webapi/webapi.csproj
COPY . .
WORKDIR /src/backend/webapi
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

EXPOSE $PORT

FROM base AS final
WORKDIR /app
COPY --from=publish /app/ .
ENTRYPOINT ["dotnet", "webapi.dll"]