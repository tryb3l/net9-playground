#!/bin/sh
set -e

echo "Waiting for PostgreSQL to start..."

if ! command -v pg_isready > /dev/null; then
  echo "Installing PostgreSQL client..."
  apk add --no-cache postgresql-client
fi

postgres_ready() {
  pg_isready -h postgres -U postgres
}

until postgres_ready; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done
echo "PostgreSQL is up - continuing"

exec dotnet WebApplication1.dll