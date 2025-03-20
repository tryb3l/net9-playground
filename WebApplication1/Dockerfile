FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

RUN dotnet tool install --global dotnet-ef --version 9.0.3 || dotnet tool update --global dotnet-ef --version 9.0.3

COPY ["WebApplication1/WebApplication1.csproj", "WebApplication1/"]
COPY ["NuGet.Config", "."]

# Restore packages (separate layer for caching)
RUN dotnet restore "WebApplication1/WebApplication1.csproj" --configfile NuGet.Config -v n

COPY . .

WORKDIR /src/WebApplication1

RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# Copy the entrypoint script (removing the problematic line that was causing errors)
COPY WebApplication1/entrypoint.sh /app/
RUN chmod +x /app/entrypoint.sh

EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=https://+:443;http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["/app/entrypoint.sh"]