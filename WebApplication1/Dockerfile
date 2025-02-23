# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

COPY ["WebApplication1.csproj", "./"]

# Copy everything else *before* restoring packages
COPY . .

# Copy NuGet.Config (Now it won't be overwritten)
COPY ["NuGet.Config", "./"]

RUN dotnet restore "WebApplication1.csproj"
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WebApplication1.dll"]