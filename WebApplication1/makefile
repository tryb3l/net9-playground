.PHONY: all run build clean publish docker-up docker-down

# Default target: run the app
all: run

# Build the project
build:
	dotnet build ./WebApplication1.csproj

# Run the project (build first)
run: build
	dotnet run --project ./WebApplication1.csproj

# Clean the project
clean:
	dotnet clean ./WebApplication1.csproj

# Publish the project (for Release mode)
publish:
	dotnet publish ./WebApplication1.csproj -c Release -o publish

# Update the db with the latest migrations
migrate:
	dotnet ef database update --project ./WebApplication1.csproj --startup-project ./WebApplication1.csproj

# Add a new migration passing the migration name as a parameter
add-migration:
	@if [ -z $(name) ]; then \
		echo "Please provide a migration name: make add-migration name=<migration_name>"; \
	else \
		dotnet ef migrations add $(name) --project ./WebApplication1.csproj --startup-project ./WebApplication1.csproj; \
	fi

# Bring up Docker containers (app + Postgres)
docker-up:
	docker-compose -f docker-compose.yml up --build

# Bring down Docker containers
docker-down:
	docker-compose -f docker-compose.yml down