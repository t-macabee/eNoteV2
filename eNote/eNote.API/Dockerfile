FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["eNote.API/eNote.API.csproj", "eNote.API/"]
COPY ["eNote.Application/eNote.Application.csproj", "eNote.Application/"]
COPY ["eNote.Contracts/eNote.Contracts.csproj", "eNote.Contracts/"]
COPY ["eNote.Domain/eNote.Domain.csproj", "eNote.Domain/"]
COPY ["eNote.Infrastructure/eNote.Infrastructure.csproj", "eNote.Infrastructure/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "eNote.API/eNote.API.csproj"

COPY . .

RUN dotnet publish "eNote.API/eNote.API.csproj" -c Release -o /app/api/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
COPY --from=build /app/api/publish .
RUN mkdir -p /app/wwwroot/uploads \
    && chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENTRYPOINT ["dotnet", "eNote.API.dll"]
