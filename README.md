# jinspect

A command-line tool that infers the schema of JSON files by sampling their contents. Useful for quickly understanding the shape of large or unfamiliar JSON datasets without reading them manually. Accepts a file path or piped input from stdin.

Given a JSON file or piped JSON, jinspect will tell you:
- What fields exist and their types
- Which fields are optional (with presence counts)
- Array length ranges
- Sample values for leaf fields
- Interactively build jq queries from the inferred schema

## Install

```
dotnet tool install -g jinspect
```

Or clone and build from source:

```
dotnet build
```

## Development

This project uses [Task](https://taskfile.dev) as a task runner. Install it with `winget install Task.Task`, `brew install go-task`, or see [taskfile.dev/installation](https://taskfile.dev/installation).

```sh
task          # list available tasks
task build    # build the project
task test     # run all tests
task bench    # run benchmarks
task pack     # create NuGet package
task publish  # test, pack, and publish to NuGet (requires NUGET_API_KEY)
task release  # create a GitHub release from the version in the csproj
```

## Usage

```
jinspect [FILE] [options]
```

When `FILE` is omitted, jinspect reads JSON from stdin.

**Options:**

| Flag | Default | Description |
|---|---|---|
| `-s`, `--sample` | 50 | Number of top-level array elements to sample |
| `-d`, `--max-depth` | 10 | Maximum nesting depth to traverse |
| `--inner-sample` | 5 | Number of elements to sample in nested arrays |
| `-f`, `--fuzzy` | off | Fuzzy-match the filename (searches current directory recursively for `.json` files) |
| `-q`, `--query` | off | Interactively build a jq query from the inferred schema |

**Examples:**

```sh
# Inspect a file directly
jinspect data/orders.json

# Sample more elements from a large array
jinspect huge-dataset.json -s 500

# Limit depth for deeply nested structures
jinspect config.json -d 3

# Fuzzy-match a filename
jinspect orders -f

# Pipe JSON from another command
curl -s https://api.example.com/data | jinspect

# Pipe with options
cat data.json | jinspect -s 100 -d 5
```

## Interactive jq query builder

Use `--query` to navigate the inferred schema and select fields. jinspect outputs a ready-to-run jq command.

```sh
# Build a jq query interactively
jinspect data.json -q

# Run the generated query directly
$(jinspect data.json -q)

# Combine with fuzzy matching
$(jinspect orders -fq)
```

The interactive tree supports:
- **↑↓** / **jk** to navigate
- **→** / **Enter** to expand nodes
- **←** to collapse
- **Space** to toggle field selection
- **a** to select all, **n** to clear
- **Esc** / **q** to confirm and output the jq command

All UI output goes to stderr, so only the jq command reaches stdout — making it safe for piping and subshells.

Note: `-q` is not available when reading from stdin, since the interactive UI requires terminal input.

## License

MIT
