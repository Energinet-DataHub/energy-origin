#!/bin/bash

if dotnet tool list -g | grep -q 'dotnet-outdated'; then
  echo "dotnet-outdated is already installed. Updating..."
  dotnet tool update --global dotnet-outdated
else
  echo "Installing dotnet-outdated..."
  dotnet tool install --global dotnet-outdated
fi

cd .. || exit

find libraries/dotnet -type f -name '*.sln' | while read -r slnFile; do

  slnDir=$(dirname "$slnFile")
  cd "$slnDir" || exit

  dotnet outdated -u

  configFile="${slnDir}/configuration.yaml"
  if [ -f "$configFile" ]; then

    awk '/version:/ {
      match($0, /([0-9]+\.){2}[0-9]+/, arr)
      split(arr[0], ver, ".")
      $0 = gensub(/([0-9]+\.){2}[0-9]+/, ver[1]"."ver[2]"."ver[3]+1, 1)
    } {print}' "$configFile" > tempFile && mv tempFile "$configFile"
  fi

  cd - > /dev/null || exit
done
