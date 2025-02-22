# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "./WebApplication1.csproj"
COPY . .
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Expose the port specified in launchSettings.json (e.g., 80)
EXPOSE 80
ENTRYPOINT ["dotnet", "WebApplication1.dll"]