version=$1
pr=$2
if [ "$GITHUB_EVENT_NAME" = 'push' ] && [ "$GITHUB_BASE_REF" = 'main' ]; then
    echo "$version"
elif [ "$GITHUB_EVENT_NAME" = 'pull_request' ]; then
    echo "${version}-pr.${pr}-$(git rev-parse --short $GITHUB_SHA)"
else
    >&2 echo "::error:: Not supported on push to branches other than main"
    exit 1
fi
