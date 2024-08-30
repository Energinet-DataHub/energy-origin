#!/bin/sh

echo "Navigating to the base directory..."
# Move up one directory level from the current script location
cd .. || { echo "Failed to navigate to the base directory"; exit 1; }

echo "Starting to find .sln files in libraries/dotnet..."
# Find all .sln files in libraries/dotnet and its subdirectories
find libraries/dotnet -type f -name '*.sln' | while read -r slnFile; do
 echo "Processing solution file: $slnFile"
 # Navigate to the .sln file directory
 slnDir=$(dirname "$slnFile")
 cd "$slnDir" || { echo "Failed to navigate to solution directory: $slnDir"; exit 1; }

 echo "Running dotnet outdated..."
 # Update using dotnet outdated and capture the output
 outdatedOutput=$(dotnet outdated -u --exclude "MassTransit")

 # Check if no dependencies were updated
 if echo "$outdatedOutput" | grep -q "No outdated dependencies were detected"; then
    echo "No dependencies were updated for $slnFile. Skipping version increment."
    # Skip to the next iteration of the loop, effectively skipping the version increment step
    cd - > /dev/null || { echo "Failed to navigate back"; exit 1; }
    continue
 fi

 echo "Dependencies were updated for $slnFile. Incrementing version in configuration.yaml..."
 # Find the configuration.yaml file in the same directory as the .sln file
 configFile=$(find . -maxdepth 1 -type f -name 'configuration.yaml')
 if [ -n "$configFile" ]; then
    # Read the current version, increment the patch version, and write to a temporary file
    tempFile=$(mktemp)
    awk -F. -v OFS=. 'NF==1{print ++$NF}; NF>1{if(length($NF+1)>length($NF))$(NF-1)++; $NF=sprintf("%0*d", length($NF), ($NF+1)%(10^length($NF))); print}' "$configFile" > "$tempFile"

    # Replace the original file with the updated temporary file
    mv "$tempFile" "$configFile"
 fi

 # Return to the original directory before processing the next .sln file
 cd - > /dev/null || { echo "Failed to navigate back"; exit 1; }
done
