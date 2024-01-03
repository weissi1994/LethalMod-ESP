build:
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet build /app/LethalMod.sln

release: clean
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet build -o release -c Release /app/LethalMod.sln

clean:
	rm -rf release/LethalMod.dll
