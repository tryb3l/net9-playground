FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files and restore as a distinct layer
COPY ["src/WebApp/WebApp.csproj", "WebApp/"]

COPY ["NuGet.Config", "."]

RUN dotnet restore "WebApp/WebApp.csproj" --configfile NuGet.Config

COPY ./src/WebApp ./WebApp

WORKDIR /src/WebApp

RUN dotnet publish "WebApp.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

WORKDIR /app

RUN apk add --no-cache postgresql-client krb5-libs icu-libs libgss gcompat

COPY --from=build /app/publish .

COPY src/WebApp/entrypoint.sh /app/

RUN chmod +x /app/entrypoint.sh

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["/app/entrypoint.sh"]