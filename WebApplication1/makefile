.PHONY: all run build clean publish

# Default target
all: run

# Build the project
build:
	dotnet build WebApplication1.csproj

# Run the project (build first)
run: build
	dotnet run --project WebApplication1.csproj

# Clean the project
clean:
	dotnet clean WebApplication1.csproj

# Publish the project (for Release mode)
publish:
	dotnet publish WebApplication1.csproj -c Release -o publish