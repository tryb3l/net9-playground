.PHONY: all run build clean publish docker-up docker-down migrate add-migration container-migrate

COMPOSE_FILE := ../docker-compose.yml
APP_PROJECT := ./WebApplication1.csproj

# Default target: run the app
all: docker-up

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
migrate-local:
	dotnet ef database update --project $(APP_PROJECT) --startup-project $(APP_PROJECT)
export-env:
	@echo "Exporting environment variables from .dev.env"
	@export $$(grep -v '^#' ./.dev.env | xargs) && \
	echo "Environment variables exported. Use in current shell session."
	@echo "CONNECTION_STRING=$${CONNECTION_STRING}"

# Add a new migration passing the migration name as a parameter
add-migration:
	@if [ -z $(name) ]; then \
	 echo "Please provide a migration name: make add-migration name=<migration_name>"; \
	else \
		read -p "Connection string (leave empty to use .dev.env): " conn_str; \
	if [ -z "$$conn_str" ]; then \
		export $$(grep -v '^#' ./.dev.env | xargs) && \
		echo "Using CONNECTION_STRING: $${CONNECTION_STRING}" && \
		dotnet ef migrations add $(name) --project $(APP_PROJECT) --startup-project $(APP_PROJECT) -o Data/Migrations; \
	 else \
		CONNECTION_STRING="$$conn_str" dotnet ef migrations add $(name) --project $(APP_PROJECT) --startup-project $(APP_PROJECT) -o Data/Migrations; \
		fi \
	fi

# Add a migration using Docker container
add-migration-docker:
	@if [ -z $(name) ]; then \
		echo "Please provide a migration name: make add-migration-docker name=<migration_name>"; \
	else \
		docker-compose -f $(COMPOSE_FILE) exec web dotnet ef migrations add $(name) -o Data/Migrations; \
	fi

# Bring up Docker containers (app + Postgres)
docker-up du:
	docker-compose -f $(COMPOSE_FILE) up --build

# Bring down Docker containers
docker-down:
	docker-compose -f $(COMPOSE_FILE) down --volumes

# Prune all Docker volumes
docker-prune drm:
	docker system prune -a --volumes

# Start postgres with port mapping to localhost (for development)
docker-postgres-local:
	docker-compose -f $(COMPOSE_FILE) up -d postgres
	@echo "PostgreSQL is available at localhost:5432"

# Wait for PostgreSQL to be ready
wait-postgres:
	@echo "Waiting for PostgreSQL to be ready..."
	@until docker-compose -f $(COMPOSE_FILE) exec -T postgres pg_isready -U postgres > /dev/null 2>&1; do \
		echo "PostgreSQL is not ready - waiting..."; \
		sleep 2; \
	done
	@echo "PostgreSQL is ready!"

# Run migrations using local database connection
migrate-local-mapped: docker-postgres-local wait-postgres
	@echo "Creating temporary connection string for local migration..."
	@export $$(grep -v '^#' ./.dev.env | xargs) && \
	DB_NAME=$$(echo "$${CONNECTION_STRING}" | grep -o 'Database=[^;]*' | cut -d'=' -f2) && \
	USERNAME=$$(echo "$${CONNECTION_STRING}" | grep -o 'Username=[^;]*' | cut -d'=' -f2) && \
	PASSWORD=$$(echo "$${CONNECTION_STRING}" | grep -o 'Password=[^;]*' | cut -d'=' -f2) && \
	echo "Running database migrations to $$DB_NAME..." && \
	CONNECTION_STRING="Host=localhost;Port=5432;Database=$$DB_NAME;Username=$$USERNAME;Password=$$PASSWORD" \
	dotnet ef database update \
		--project $(APP_PROJECT) \
		--startup-project $(APP_PROJECT)

# Drop database (CAUTION!)
drop-database:
	@echo "WARNING: This will delete all data in the database!"
	@read -p "Are you sure? [y/N] " confirm; \
	if [ "$$confirm" = "y" ] || [ "$$confirm" = "Y" ]; then \
		docker-compose -f $(COMPOSE_FILE) exec postgres dropdb -U postgres net9playground --if-exists; \
		docker-compose -f $(COMPOSE_FILE) exec postgres createdb -U postgres net9playground; \
	fi

# Full reset: drop DB, apply migrations, and seed data
reset-db: drop-database migrate-local