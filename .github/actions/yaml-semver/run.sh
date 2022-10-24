git=$1
file=$2
path=$3
pr=$4

cd "$git"
version=$(yaml-get --query "$path" "$file")
if [[ "$GITHUB_EVENT_NAME" == 'push' && github.ref_name == 'main']]; then
    echo "::set-output name=version::$version"
elif [[ "$GITHUB_EVENT_NAME" == 'pull_request' ]]; then
    echo "::set-output name=version::${version}-pr.${pr}-$(git rev-parse --short $GITHUB_SHA)"
else
    echo "::error:: Not supported on push to branches other than main"
    exit 1
fi
