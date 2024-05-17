# find-diff-action

This action lets you find which areas of your repository has changed, so you can customize or skip parts of subsequent workflows.

By default all changes are reported, but the action can be configured to only consider certain folders or files and certain depths.

The action produces both a list of relevant changes and a pattern for these changes.

# Usage

## List all changes

```yaml
- uses: ./.github/actions/find-diff-action
```

## List all changes in all top level folders

```yaml
- uses: ./.github/actions/find-diff-action
  with:
    paths: */
```

## List all changes in src and doc folders

```yaml
- uses: ./.github/actions/find-diff-action
  with:
    paths: src doc
```

## List all subfolders with changes in src and doc folders

```yaml
- uses: ./.github/actions/find-diff-action
  with:
    paths: src/**/ doc/**/
```

## List all changed files

```yaml
- uses: ./.github/actions/find-diff-action
  with:
    paths: **/*.*
```

## List all subfolders with changes up two levels below classes folder

```yaml
- uses: ./.github/actions/find-diff-action
  with:
    paths: classes/*/*/
```

# Outputs

The action outputs three variables, one is a plain new line separated list of changes, the second a regular expression that can match everything on the list of changes and the third is a boolean indication of if there was any matches.

## Sample for `list`

```
.github/workflows/tests
action.yaml
LICENSE
```

## Sample for `pattern`

```
^.github/workflows/tests|^action.yaml|^LICENSE
```

## Sample for `matches`

```
true
```

# License

The scripts and documentation in this project are released under the [MIT License](LICENSE)
