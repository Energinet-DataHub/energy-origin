#!/bin/sh
set -e

projectFile="$1"
if [ ! -f "$projectFile" ]; then
    echo "You must provide a valid path to an existing .csproj file, the given argument was '$projectFile'."
    exit 1
fi

projectFolder=$(dirname "$projectFile")

migrations=$(
    cd "$(dirname "$projectFile")"
    while [ ! -d migrations ] && [ ! "$(pwd)" = '/' ]; do
        cd ..
    done
    printf '%s/migrations' "$(pwd)"
)
if [ ! -d "$migrations" ]; then
    echo "Script was unable to detect the migrations folder. You may have create it yourself, if there are no migrations already."
    exit 2
fi

migrations="$migrations/$(basename "$projectFile" .csproj)."

dotnet tool list -g | grep dotnet-ef >/dev/null || dotnet tool install --global dotnet-ef >/dev/null

generate() {
    projectFile="$1"
    previous="$2"
    name="$3"
    output="$4"
    echo dotnet ef --project "$projectFile" migrations script "$previous" "$name" --no-transactions -o "$output"| sh -x
}

previous="0"
for name in $(find "$projectFolder" -name "*.Designer.cs" -exec basename "{}" .Designer.cs \; | sort); do
    generate "$projectFile" "$previous" "$name" "$migrations$name.up.sql"
    previous="$name"
done
for name in $(find "$projectFolder" -name "*.Designer.cs" -exec basename "{}" .Designer.cs \; | sort -r); do
    generate "$projectFile" "$previous" "$name" "$migrations$previous.down.sql"
    previous="$name"
done
generate "$projectFile" "$previous" "0" "$migrations$previous.down.sql"
