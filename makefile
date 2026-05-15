.PHONY: all run dev build clean publish help docker-up docker-down docker-prune docker-postgres-local migrate-local add-migration reset-db drop-database update-ef-tools apple-up apple-run apple-postgres apple-network apple-volumes apple-down

# --- Configuration ---

# Docker
COMPOSE_FILE := ./docker-compose.yml
APP_PROJECT := ./src/WebApp/WebApp.csproj
SOLUTION_FILE := ./net-playground.sln
ENV_FILE := ./src/WebApp/.dev.env

# Apple Containers
APPLEC_NETWORK := blog-network
APPLEC_WEB_IMAGE := webapp-apple-local

# --- Main Targets ---
# Default target: build and run tests
all: build test

# Build the project
build:
	dotnet build $(SOLUTION_FILE)

# Run the project
run:
	dotnet run --project $(APP_PROJECT)

# Start local postgres then run the app
dev: docker-postgres-local
	@echo "Waiting for PostgreSQL to be ready..."
	@until docker-compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) exec -T postgres pg_isready -U postgres > /dev/null 2>&1; do \
		printf '.'; sleep 1; \
	done
	@echo " PostgreSQL is ready."
	dotnet run --project $(APP_PROJECT)

# Run all tests
test:
	dotnet test $(SOLUTION_FILE)

# Clean the project
clean:
	dotnet clean $(SOLUTION_FILE)

# Publish the project (for Release mode)
publish:
	dotnet publish $(APP_PROJECT) -c Release -o publish

# Show available targets
help:
	@echo ""
	@echo "Usage: make <target>"
	@echo ""
	@echo "Main targets:"
	@echo "  all                     Build solution and run all tests"
	@echo "  build                   Build the solution"
	@echo "  run                     Run the app (requires postgres already running)"
	@echo "  dev                     Start local postgres, then run the app"
	@echo "  test                    Run all tests"
	@echo "  clean                   Clean build outputs"
	@echo "  publish                 Publish in Release mode"
	@echo ""
	@echo "EF Core:"
	@echo "  add-migration name=X    Add a new EF Core migration"
	@echo "  migrate-local           Apply migrations against local DB"
	@echo "  reset-db                Drop DB and re-apply all migrations"
	@echo "  drop-database           Drop the database (prompts for confirmation)"
	@echo "  update-ef-tools         Update the dotnet-ef global tool"
	@echo ""
	@echo "Docker:"
	@echo "  docker-up   (du)        Build and start all containers"
	@echo "  docker-down (dd)        Stop containers and remove volumes"
	@echo "  docker-prune (drm)      Prune all Docker system resources"
	@echo "  docker-postgres-local   Start only postgres mapped to localhost:5432"
	@echo ""
	@echo "Apple Containers (macOS):"
	@echo "  apple-up                Start full stack in Apple native containers"
	@echo "  apple-run               Start Apple postgres, then run .NET app locally"
	@echo "  apple-postgres          Start or create the Apple postgres container"
	@echo "  apple-network           Create the Apple container network"
	@echo "  apple-volumes           Create named volumes for Apple containers"
	@echo "  apple-down              Stop and delete all Apple containers"
	@echo ""

# --- Docker Targets ---

docker-up du:
	docker-compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) up --build

docker-down dd:
	docker-compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) down --volumes

docker-prune drm:
	docker system prune -a --volumes

# Start postgres with port mapping to localhost
docker-postgres-local:
	docker-compose --env-file $(ENV_FILE) -f $(COMPOSE_FILE) up -d postgres
	@echo "PostgreSQL is available at localhost:5432"

# --- EF Core Migration Targets ---

# Add a new migration locally
add-migration:
	@if [ -z "$(name)" ]; then \
		echo "Please provide a migration name: make add-migration name=<migration_name>"; \
	else \
		set -a && . $(ENV_FILE) && set +a && \
		echo "Using CONNECTION_STRING from $(ENV_FILE)" && \
		dotnet ef migrations add $(name) --project $(APP_PROJECT) --startup-project $(APP_PROJECT) -o Data/Migrations; \
	fi

# Apply migrations locally
migrate-local:
	@set -a && . $(ENV_FILE) && set +a && \
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

# Update EF Core tools
update-ef-tools:
	dotnet tool update --global dotnet-ef

# --- Apple Containers Targets (macOS Only) ---

# Starts BOTH the .NET App and Postgres natively isolated. (For simulating full deployment)
apple-up: apple-network apple-volumes apple-postgres
	@echo "Waiting for Postgres to initialize..."
	@sleep 3
	@echo "Building WebApp natively..."
	container build -t $(APPLEC_WEB_IMAGE) -f src/WebApp/Dockerfile .

	@echo "Cleaning up old web container if it exists..."
	@container list --all | grep -qw "web" && container delete --force web || true

	@echo "Starting fresh WebApp container..."
	@set -a && . $(ENV_FILE) && set +a && \
	container run -d --name web --network $(APPLEC_NETWORK) \
		-p 5008:80 -p 7024:443 \
		--cap-add NET_BIND_SERVICE \
		--env-file $(ENV_FILE) \
		-e ASPNETCORE_ENVIRONMENT=Development \
		-e CONNECTION_STRING="Host=postgres;Database=$$POSTGRES_DB;Username=$$POSTGRES_USER;Password=$$POSTGRES_PASSWORD" \
		-v $(HOME)/.aspnet/https:/https:ro \
		-v dataprotection-keys:/root/.aspnet/DataProtection-Keys \
		-v $(PWD)/src/WebApp/wwwroot/uploads:/app/wwwroot/uploads \
		$(APPLEC_WEB_IMAGE)
	@echo "Apple native containers are UP!"

# Start Apple Postgres container, then run the .NET app locally
apple-run: apple-postgres
	@echo "Waiting for Postgres to accept connections..."
	@until container exec postgres pg_isready -U postgres > /dev/null 2>&1; do \
		printf '.'; sleep 1; \
	done
	@echo " PostgreSQL is ready."
	@echo "Starting .NET WebApp locally (HTTPS Profile)..."
	dotnet run --launch-profile "https" --project $(APP_PROJECT)

# Starts ONLY Postgres natively.
apple-postgres: apple-network apple-volumes
	@if container list --all | grep -qw "postgres"; then \
		echo "Starting existing Postgres container..."; \
		container start postgres; \
	else \
		echo "Creating and starting new Postgres container..."; \
		PG_DB=$$(grep '^POSTGRES_DB=' $(ENV_FILE) | cut -d '=' -f2- | tr -d '\r'); \
		PG_USER=$$(grep '^POSTGRES_USER=' $(ENV_FILE) | cut -d '=' -f2- | tr -d '\r'); \
		PG_PASS=$$(grep '^POSTGRES_PASSWORD=' $(ENV_FILE) | cut -d '=' -f2- | tr -d '\r'); \
		container run -d --name postgres --network $(APPLEC_NETWORK) \
			-p 5432:5432 \
			--cap-add CHOWN \
			--cap-add FOWNER \
			--cap-add SETUID \
			--cap-add SETGID \
			--cap-add DAC_OVERRIDE \
			-e POSTGRES_DB="$$PG_DB" \
			-e POSTGRES_USER="$$PG_USER" \
			-e POSTGRES_PASSWORD="$$PG_PASS" \
			-e POSTGRES_HOST_AUTH_METHOD=trust \
			-v apple-pgdata:/var/lib/postgresql/18/data \
			postgres:18-alpine; \
	fi

# Set up the isolated network for Apple containers
apple-network:
	@container network ls | grep -q "$(APPLEC_NETWORK)" || \
		(echo "Creating network $(APPLEC_NETWORK)..." && container network create $(APPLEC_NETWORK))

# Set up Apple named volumes
apple-volumes:
	@container volume ls | grep -q "apple-pgdata" || \
		(echo "Creating volume apple-pgdata..." && container volume create --journal apple-pgdata)
	@container volume ls | grep -q "dataprotection-keys" || \
		(echo "Creating volume dataprotection-keys..." && container volume create dataprotection-keys)

# Stop and Clean up Apple Containers
apple-down:
	-container stop web postgres
	-container delete --force web postgres
	-container network delete $(APPLEC_NETWORK)
	@echo "Apple native containers are DOWN."