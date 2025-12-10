{ pkgs, ... }:

{
  difftastic.enable = true;

  packages = [ pkgs.git ];

  languages.dotnet.enable = true;
  scripts.build.exec = ''
    dotnet build -o release -c Release LethalMod.sln
  '';
  scripts.release.exec = ''
    build
    VERSION=$(cat ./manifest.json | ${pkgs.jq}/bin/jq -r '.version_number')
    cp ./release/LethalMod.dll ./LethalMod.dll
    cp ./LethalMod/Plugin.cs ./Source.cs
    ${pkgs.zip}/bin/zip LethalMod_$VERSION.zip ./LethalMod.dll ./icon.png ./manifest.json ./README.md ./Source.cs
    rm ./Source.cs ./LethalMod.dll
  '';

  pre-commit.hooks = {
    check-merge-conflicts.enable = true;
    check-executables-have-shebangs.enable = true;
    check-shebang-scripts-are-executable.enable = true;
    check-symlinks.enable = true;
    check-yaml.enable = true;
    detect-private-keys.enable = true;
    editorconfig-checker.enable = true;
    end-of-file-fixer.enable = true;
    trim-trailing-whitespace.enable = true;
  };
}
