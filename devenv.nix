{ pkgs, ... }:

{
  difftastic.enable = true;

  packages = [ pkgs.git ];

  languages.dotnet.enable = true;

  pre-commit.hooks = {
    check-merge-conflicts.enable = true;
    check-executables-have-shebangs.enable = true;
    check-shebang-scripts-are-executable.enable = true;
    check-symlinks.enable = true;
    check-yaml.enable = true;
    commitizen.enable = true;
    # conform.enable = true;
    detect-private-keys.enable = true;
    editorconfig-checker.enable = true;
    end-of-file-fixer.enable = true;
    trim-trailing-whitespace.enable = true;
    mdl.enable = true;
  };
}
