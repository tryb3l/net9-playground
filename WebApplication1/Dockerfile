# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine3.18 AS build
WORKDIR /src
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "./WebApplication1.csproj"
COPY . .
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine3.18 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "WebApplication1.dll"]