FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

RUN dotnet tool install --global dotnet-ef --version 9.0.6 || dotnet tool update --global dotnet-ef --version 9.0.6

# Copy project files and restore as a distinct layer
COPY ["src/WebApp/WebApp.csproj", "WebApp/"]
COPY ["NuGet.Config", "."]
RUN dotnet restore "WebApp/WebApp.csproj" --configfile NuGet.Config

# Copy the entire source code into the container
COPY ./src/WebApp ./WebApp

WORKDIR /src/WebApp

RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/publish .

# Copy the entrypoint script
COPY src/WebApp/entrypoint.sh /app/
RUN chmod +x /app/entrypoint.sh

EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=https://+:443;http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["/app/entrypoint.sh"]