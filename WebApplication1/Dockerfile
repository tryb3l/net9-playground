FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /app

COPY ["WebApplication1.csproj", "./"]
COPY ["NuGet.Config", "./"]

RUN dotnet restore "WebApplication1.csproj"

COPY . ./

RUN dotnet publish "WebApplication1.csproj" -c Release -o out

RUN dotnet tool install --global dotnet-ef --version 9.0.2

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app
COPY --from=build-env /app/out .

COPY entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["/app/entrypoint.sh"]