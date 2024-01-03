build:
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet build /app/LethalMod.sln
