.PHONY: all run build clean publish docker-up docker-down migrate add-migration

COMPOSE_FILE := docker-compose.yml
APP_PROJECT := WebApplication1.csproj

# Default target: run the app
all: run

# Build the project
build:
	dotnet build $(APP_PROJECT)

# Run the project (build first)
run: build
	dotnet run --project $(APP_PROJECT)

# Clean the project
clean:
	dotnet clean $(APP_PROJECT)

# Publish the project (for Release mode)
publish:
	dotnet publish $(APP_PROJECT) -c Release -o publish

# Update the db with the latest migrations
migrate:
	dotnet ef database update --project $(APP_PROJECT) --startup-project $(APP_PROJECT)

# Add a new migration passing the migration name as a parameter
add-migration:
	@if [ -z $(name) ]; then \
		echo "Please provide a migration name: make add-migration name=<migration_name>"; \
	else \
		dotnet ef migrations add $(name) --project $(APP_PROJECT) --startup-project $(APP_PROJECT); \
	fi

# Bring up Docker containers (app + Postgres)
docker-up:
	docker-compose -f $(COMPOSE_FILE) up --build

# Bring down Docker containers
docker-down:
	docker-compose -f $(COMPOSE_FILE) down