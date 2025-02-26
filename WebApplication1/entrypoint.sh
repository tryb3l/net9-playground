#!/bin/sh
set -e

# Install PostgreSQL client if not present
if ! command -v pg_isready > /dev/null; then
  echo "Installing PostgreSQL client..."
  apk update && apk add --no-cache postgresql-client
fi

# Check if postgres is up and running before continuing
echo "Waiting for PostgreSQL to start..."
until pg_isready -h postgres -U postgres; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 2
done
echo "PostgreSQL is up - continuing"

echo "Starting application..."
dotnet WebApplication1.dll