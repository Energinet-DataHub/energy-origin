#!/bin/bash

# Base directory where the API version folders are located
BASE_DIR="domains/transfer/Transfer.API/API"

# Current date for the new version
NEW_VERSION="v$(date +%Y_%m_%d)"
NEW_VERSION_SHORT="$(date +%Y_%m_%d)"

# Function to update namespaces and using statements in files
update_namespaces() {
    local new_version_path=$1
    local old_version=$2
    local new_version=$3

    # Recursively find and update C# files
    find "$new_version_path" -type f -name "*.cs" | while read -r file; do
        # Replace namespace and using statements
        sed -i "s/$old_version/$new_version/g" "$file"
    done
}

# Function to create a new version directory and copy contents
create_new_version() {
    local feature_dir=$1
    local api_dir="$feature_dir/Api"

    if [ -d "$api_dir" ]; then
        local highest_version=""
        for dir in "$api_dir"/v20*; do
            [[ -d $dir ]] && highest_version=$(basename "$dir")
        done

        if [ -z "$highest_version" ]; then
            echo "Error finding the highest version in $api_dir"
            return 1
        fi

        local highest_version_path="$api_dir/$highest_version"
        local new_version_path="$api_dir/$NEW_VERSION"

        echo "Creating new version in $api_dir: $NEW_VERSION"

        mkdir -p "$new_version_path"
        cp -r "$highest_version_path/." "$new_version_path/"

        # Update namespaces and using statements
        update_namespaces "$new_version_path" "$highest_version" "$NEW_VERSION_SHORT"
    else
        echo "No Api directory found in $feature_dir"
    fi
}

# Process each feature directory
for feature_dir in "$BASE_DIR"/*; do
    if [ -d "$feature_dir" ]; then
        create_new_version "$feature_dir"
    fi
done

echo "Version creation process completed."
