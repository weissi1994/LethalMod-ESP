build:
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet rebuild /app/LethalMod.sln

release:
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet rebuild -o release -c Release /app/LethalMod.sln
