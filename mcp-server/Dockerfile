FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution info and project dependencies first to leverage cache
COPY PersonaMcpServer.sln ./
COPY Directory.Packages.props ./
COPY nuget.config ./
COPY src/*.csproj ./src/
COPY tests/*.csproj ./tests/

RUN dotnet restore PersonaMcpServer.sln

# Copy remaining files and build/publish
COPY . .
RUN dotnet build PersonaMcpServer.sln -c Release

FROM build AS publish
RUN dotnet publish src/mcp-server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 3000

ENTRYPOINT ["dotnet", "mcp-server.dll"]