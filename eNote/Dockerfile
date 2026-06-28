FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["eNote.sln", "./"]
COPY ["eNote.API/eNote.API.csproj", "eNote.API/"]
COPY ["eNote.Worker/eNote.Worker.csproj", "eNote.Worker/"]
COPY ["eNote.Application/eNote.Application.csproj", "eNote.Application/"]
COPY ["eNote.Domain/eNote.Domain.csproj", "eNote.Domain/"]
COPY ["eNote.Infrastructure/eNote.Infrastructure.csproj", "eNote.Infrastructure/"]
COPY ["eNote.Contracts/eNote.Contracts.csproj", "eNote.Contracts/"]
COPY ["eNote.Tests/eNote.Tests.csproj", "eNote.Tests/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "eNote.sln"

COPY . .

RUN dotnet publish "eNote.API/eNote.API.csproj" -c Release -o /app/api/publish /p:UseAppHost=false
RUN dotnet publish "eNote.Worker/eNote.Worker.csproj" -c Release -o /app/worker/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
COPY --from=build /app/api/publish .
ENTRYPOINT ["dotnet", "eNote.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /app/worker/publish .
ENTRYPOINT ["dotnet", "eNote.Worker.dll"]