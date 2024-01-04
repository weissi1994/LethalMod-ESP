clean:
	rm -rf release/LethalMod.dll

build:
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet build /app/LethalMod.sln

release: clean
	docker run --rm -it -v "${PWD}:/app" -w /app mcr.microsoft.com/dotnet/sdk:latest /usr/bin/dotnet build -o release -c Release /app/LethalMod.sln

package: release
	cp ./release/LethalMod.dll .
	cp ./LethalMod/Plugin.cs Source.cs
	nix-shell -p zip --run 'zip release.zip ./icon.png ./README.md ./manifest.json ./LethalMod.dll ./Source.cs'
	rm ./LethalMod.dll
	rm ./Source.cs
