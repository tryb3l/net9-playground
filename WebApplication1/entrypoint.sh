#!/bin/sh
set -e

# Wait for the database
echo "Waiting for PostgreSQL to start..."
postgres_ready() {
  pg_isready -h postgres -U postgres
}

until postgres_ready; do
  sleep 1
done
echo "PostgreSQL is up - continuing"

echo "Running database migrations..."
cd /app
dotnet ef database update

# Start the application
exec dotnet WebApplication1.dll