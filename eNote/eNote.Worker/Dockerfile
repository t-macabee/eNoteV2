FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["eNote.Worker/eNote.Worker.csproj", "eNote.Worker/"]
COPY ["eNote.Infrastructure/eNote.Infrastructure.csproj", "eNote.Infrastructure/"]
COPY ["eNote.Application/eNote.Application.csproj", "eNote.Application/"]
COPY ["eNote.Contracts/eNote.Contracts.csproj", "eNote.Contracts/"]
COPY ["eNote.Domain/eNote.Domain.csproj", "eNote.Domain/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "eNote.Worker/eNote.Worker.csproj"

COPY . .

RUN dotnet publish "eNote.Worker/eNote.Worker.csproj" -c Release -o /app/worker/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=build /app/worker/publish .
RUN chown -R $APP_UID:$APP_UID /app
USER $APP_UID
ENTRYPOINT ["dotnet", "eNote.Worker.dll"]
