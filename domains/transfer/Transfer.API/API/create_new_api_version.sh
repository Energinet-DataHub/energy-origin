#!/bin/bash

# Base directory where the feature folders are located
BASE_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Current date for the new version
NEW_VERSION="v$(date +%Y_%m_%d)"
NEW_VERSION_SHORT="v$(date +%Y_%m_%d)" # Used in namespace and using statements

# Function to update namespaces and using statements in files
update_namespaces() {
    local new_version_path=$1
    local new_version=$2

    echo "Updating namespaces in $new_version_path to $new_version"

    # Recursively find and update C# files in the new version directory
    find "$new_version_path" -type f -name "*.cs" | while read -r file; do
        echo "Updating file: $file"
        # Replace namespace and using statements, including the "v" prefix
        sed -i "s/v[0-9]\{4\}_[0-9]\{2\}_[0-9]\{2\}/$new_version/g" "$file"
    done
}

# Function to create a new version directory and copy contents
create_new_version() {
    local api_dir=$1

    if [ -d "$api_dir" ]; then
        # Declare the variable first
        local highest_version

        # Assign the value in a separate line
        highest_version=$(find "$api_dir" -type d -name "v20*" | sort | tail -n 1 | xargs basename)

        # Check if the variable is set
        if [ -z "$highest_version" ]; then
            echo "Error finding the highest version in $api_dir"
            return 1
        fi

        local highest_version_path="$api_dir/$highest_version"
        local new_version_path="$api_dir/$NEW_VERSION"

        echo "Creating new version in $api_dir: $NEW_VERSION"

        mkdir -p "$new_version_path"
        cp -r "$highest_version_path/." "$new_version_path/"

        update_namespaces "$new_version_path" "$NEW_VERSION_SHORT"
    else
        echo "No Api directory found in $api_dir"
    fi
}

# Process each feature's Api directory
for feature_dir in "$BASE_DIR"/*/Api; do
    echo "Processing Api directory: $feature_dir"
    if [ -d "$feature_dir" ]; then
        create_new_version "$feature_dir"
    else
        echo "No Api directory in feature: $feature_dir"
    fi
done

echo "Version creation process completed."
