#!/bin/sh
set -e

# Use environment variables for connection details, with defaults
DB_HOST="${DB_HOST:-postgres}"
DB_USER="${DB_USER:-postgres}"

echo "Waiting for PostgreSQL to start at ${DB_HOST}..."

# Install psql client if not present
if ! command -v pg_isready > /dev/null; then
  echo "Installing PostgreSQL client..."
  apk add --no-cache postgresql-client
fi

# Loop until the database is ready
until pg_isready -h "$DB_HOST" -U "$DB_USER" -q; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done

echo "PostgreSQL is up - continuing"

# Execute the main application
exec dotnet WebApp.dll