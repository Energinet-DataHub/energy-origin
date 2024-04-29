#!/bin/sh

echo "Navigating to the base directory..."
# Move up one directory level from the current script location
cd .. || { echo "Failed to navigate to the base directory"; exit 1; }

echo "Starting to find .sln files within domains directory and its subdirectories..."
# Find all .sln files within the domains folder and its subdirectories
find domains -type f -name '*.sln' | while read -r slnFile; do
 echo "Processing solution file: $slnFile"
 # Navigate to the .sln file directory
 slnDir=$(dirname "$slnFile")
 cd "$slnDir" || { echo "Failed to navigate to solution directory: $slnDir"; exit 1; }

 echo "Running dotnet outdated..."
 # Update using dotnet outdated and capture the output
 outdatedOutput=$(dotnet outdated -u --exclude "MassTransit")

 # Check if no dependencies were updated
 if echo "$outdatedOutput" | grep -q "No outdated dependencies were detected"; then
    echo "No dependencies were updated for $slnFile."
 else
    echo "Dependencies were updated for $slnFile."
 fi

 # Return to the original directory before processing the next .sln file
 cd - > /dev/null || { echo "Failed to navigate back"; exit 1; }
done
