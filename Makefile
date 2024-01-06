clean:
	rm -rf release/LethalMod.dll

build:
	dotnet build ./LethalMod.sln

release: clean
	dotnet build -o release -c Release ./LethalMod.sln

package: release
	cp ./release/LethalMod.dll .
	cp ./LethalMod/Plugin.cs Source.cs
	zip release.zip ./icon.png ./README.md ./manifest.json ./LethalMod.dll ./Source.cs
	rm ./LethalMod.dll
	rm ./Source.cs
