#!/usr/bin/env bash
# Builds and runs fresh consumers of CachingServiceWithAOPSupport VERSION from SOURCE (a folder of packed nupkgs, or
# "nuget.org"), in three dependency combinations, on net10.0 and, on Windows, net48:
#   floor    only this package: NuGet resolves its floors (Autofac 6.5, Autofac.Extras.DynamicProxy 7.1)
#   current  plus Autofac 9.3.4 and Autofac.Extras.DynamicProxy 8.1.0, the latest on 2026-09-27
#   mixed    plus Autofac 9.3.4 only: DynamicProxy stays at the 7.1 floor (plan D5; what an Autofac 9 user gets)
# Each consumer gets its own packages folder, so a cached copy of the same version cannot stand in for SOURCE's.
# Usage: tests/consumers/run.sh VERSION SOURCE
set -euo pipefail

version="$1"
source="$2"
here="$(cd "$(dirname "$0")" && pwd)"
work="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/caching-consumers"
rm -rf "$work"
mkdir -p "$work"

if [ "$source" != nuget.org ]; then
  source="$(cd "$source" && pwd)"
  # Git Bash on Windows: dotnet nuget add source refuses /d/a/... and D:/a/...; it takes D:\a\... (skill L-073).
  if command -v cygpath >/dev/null; then source="$(cygpath -w "$source")"; fi
fi

frameworks="net10.0"
case "$(uname -s)" in MINGW* | MSYS* | CYGWIN*) frameworks="net10.0 net48" ;; esac

extra_refs() {
  case "$1" in
    floor) ;;
    current) printf '%s\n' '<PackageReference Include="Autofac" Version="9.3.4" />' '<PackageReference Include="Autofac.Extras.DynamicProxy" Version="8.1.0" />' ;;
    mixed) printf '%s\n' '<PackageReference Include="Autofac" Version="9.3.4" />' ;;
  esac
}

expected() {
  case "$1" in
    floor) printf '%s\n' 'Autofac 6.5.' 'Autofac.Extras.DynamicProxy 7.1.' ;;
    current) printf '%s\n' 'Autofac 9.3.' 'Autofac.Extras.DynamicProxy 8.1.' ;;
    mixed) printf '%s\n' 'Autofac 9.3.' 'Autofac.Extras.DynamicProxy 7.1.' ;;
  esac
}

for tfm in $frameworks; do
  for combo in floor current mixed; do
    dir="$work/$combo-$tfm"
    mkdir -p "$dir"
    cp "$here/Program.cs" "$dir/Program.cs"
    cat > "$dir/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>$tfm</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="CachingServiceWithAOPSupport" Version="[$version]" />
    <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.3" PrivateAssets="all" />
    $(extra_refs "$combo")
  </ItemGroup>
</Project>
EOF
    # Empty MSBuild files stop the consumer from inheriting this repository's props when the work folder is inside it.
    printf '<Project>\n</Project>\n' > "$dir/Directory.Build.props"
    printf '<Project>\n</Project>\n' > "$dir/Directory.Build.targets"
    dotnet new nugetconfig -o "$dir" >/dev/null
    if [ "$source" != nuget.org ]; then
      dotnet nuget add source "$source" -n local --configfile "$dir/nuget.config" >/dev/null
    fi
    echo "== $combo on $tfm"
    out=$(NUGET_PACKAGES="$work/packages-$combo-$tfm" dotnet run --project "$dir/Consumer.csproj" -c Release 2>&1) || { echo "$out"; exit 1; }
    echo "$out" | tail -5
    while IFS= read -r want; do
      echo "$out" | grep -qF "$want" || { echo "::error::$combo on $tfm: expected '$want'"; exit 1; }
    done < <(expected "$combo")
  done
done
echo "all consumers answered as expected"
