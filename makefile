.PHONY: all run build clean publish docker-up docker-down migrate add-migration container-migrate test

# --- Configuration ---
COMPOSE_FILE := ./docker-compose.yml
APP_PROJECT := ./src/WebApp/WebApp.csproj
TEST_PROJECT := ./tests/WebApp.UnitTests/WebApp.UnitTests.csproj
ENV_FILE := ./src/WebApp/.dev.env

# --- Main Targets ---
# Default target: build and run tests
all: build test

# Build the project
build:
	dotnet build $(APP_PROJECT)

# Run the project (build first)
run: build
	dotnet run --project $(APP_PROJECT)

# Run all tests
test:
	dotnet run --project $(TEST_PROJECT)

# Clean the project
clean:
	dotnet clean $(APP_PROJECT)
	dotnet clean $(TEST_PROJECT)

# Publish the project (for Release mode)
publish:
	dotnet publish $(APP_PROJECT) -c Release -o publish

# --- Docker Targets ---
# Bring up Docker containers (app + Postgres)
docker-up du:
	docker-compose -f $(COMPOSE_FILE) up --build

# Bring down Docker containers
docker-down dd:
	docker-compose -f $(COMPOSE_FILE) down --volumes

# Prune all Docker volumes
docker-prune drm:
	docker system prune -a --volumes

# Start postgres with port mapping to localhost (for development)
docker-postgres-local:
	docker-compose -f $(COMPOSE_FILE) up -d postgres
	@echo "PostgreSQL is available at localhost:5432"

# --- EF Core Migration Targets ---
# Add a new migration locally
add-migration:
	@if [ -z "$(name)" ]; then \
		echo "Please provide a migration name: make add-migration name=<migration_name>"; \
	else \
		export $$(grep -v '^#' $(ENV_FILE) | xargs) && \
		echo "Using CONNECTION_STRING from $(ENV_FILE)" && \
		dotnet ef migrations add $(name) --project $(APP_PROJECT) --startup-project $(APP_PROJECT) -o Data/Migrations; \
	fi

# Apply migrations locally
migrate-local:
	@export $$(grep -v '^#' $(ENV_FILE) | xargs) && \
	dotnet ef database update --project $(APP_PROJECT) --startup-project $(APP_PROJECT)

# Full reset: drop DB, apply migrations
reset-db: drop-database migrate-local

# Drop database (CAUTION!)
drop-database:
	@echo "WARNING: This will delete all data in the database!"
	@read -p "Are you sure? [y/N] " confirm; \
	if [ "$$confirm" = "y" ] || [ "$$confirm" = "Y" ]; then \
		docker-compose -f $(COMPOSE_FILE) exec postgres dropdb -U postgres net9playground --if-exists; \
		docker-compose -f $(COMPOSE_FILE) exec postgres createdb -U postgres net9playground; \
	fi