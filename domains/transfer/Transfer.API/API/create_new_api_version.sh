#!/bin/sh

BASE_DIR="$(cd "$(dirname "$0")" && pwd)"

NEW_VERSION="v$(date +%Y_%m_%d)"
NEW_VERSION_SHORT="v$(date +%Y_%m_%d)"
API_VERSION_DATE="$(date +%Y%m%d)"

update_files() {
    new_version_path=$1
    new_version=$2
    api_version_date=$3

    echo "Updating namespaces in $new_version_path to $new_version"

    find "$new_version_path" -type f -name "*.cs" | while IFS= read -r file; do
        echo "Updating file: $file"

        tmp_file=$(mktemp)

        sed "s/v[0-9]\{4\}_[0-9]\{2\}_[0-9]\{2\}/$new_version/g" "$file" > "$tmp_file" && mv "$tmp_file" "$file"

        sed "s/\[ApiVersion(\"[0-9]\{8\}\")\]/\[ApiVersion(\"$api_version_date\")\]/g" "$file" > "$tmp_file" && mv "$tmp_file" "$file"
    done
}

create_new_version() {
    api_dir=$1

    if [ -d "$api_dir" ]; then
        highest_version=$(find "$api_dir" -type d -name "v20*" | sort | tail -n 1 | xargs basename)

        if [ -z "$highest_version" ]; then
            echo "Error finding the highest version in $api_dir"
            return 1
        fi

        highest_version_path="$api_dir/$highest_version"
        new_version_path="$api_dir/$NEW_VERSION"

        echo "Creating new version in $api_dir: $NEW_VERSION"

        mkdir -p "$new_version_path"
        cp -r "$highest_version_path/." "$new_version_path/"

        update_files "$new_version_path" "$NEW_VERSION_SHORT" "$API_VERSION_DATE"
    else
        echo "No Api directory found in $api_dir"
    fi
}

for feature_dir in "$BASE_DIR"/*/Api; do
    echo "Processing Api directory: $feature_dir"
    if [ -d "$feature_dir" ]; then
        create_new_version "$feature_dir"
    else
        echo "No Api directory in feature: $feature_dir"
    fi
done

echo "Version creation process completed."
