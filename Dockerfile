# Multi-stage Dockerfile for Trip Analyzer
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["TripAnalyzer.csproj", "./"]
RUN dotnet restore "TripAnalyzer.csproj"

# Copy source code and build
COPY . .
RUN dotnet build "TripAnalyzer.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "TripAnalyzer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image setup
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Install required Linux runtime dependency
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=publish /app/publish .

# Environment variable for dynamic port binding (Render compatibility)
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "TripAnalyzer.dll"]