{
  description = "VintageStory build flake";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
  };

  outputs = inputs @ {
    self,
    nixpkgs,
    flake-parts,
    ...
  }:
    flake-parts.lib.mkFlake {
      inherit inputs;
    } {
      systems = ["x86_64-linux" "aarch64-linux"];

      perSystem = {pkgs, ...}: {
        packages.default = pkgs.stdenv.mkDerivation {
          pname = "BetterProspecting";
          version = "1.5.0";

          src = pkgs.lib.cleanSource ./.;

          nativeBuildInputs = with pkgs; [
            dotnet-sdk_7
            vintagestory
          ];

          buildPhase = ''
            export VINTAGE_STORY="${pkgs.vintagestory}/share"
            dotnet run --project BetterProspecting/CakeBuild/CakeBuild.csproj
          '';

          installPhase = ''
            mkdir -p $out
            cp -r BetterProspecting/Releases/* $out/
          '';
        };
      };
    };
}
