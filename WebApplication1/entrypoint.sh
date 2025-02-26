#!/bin/sh

# Apply database migrations
echo "Applying database migrations..."
dotnet ef database update

if [ $? -eq 0 ]; then
    echo "Migrations applied successfully."
    # Start the application
    exec dotnet WebApplication1.dll
else
    echo "Migration failed.  Exiting."
    exit 1
fi