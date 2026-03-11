# jinspect

A command-line tool that infers the schema of JSON files by sampling their contents. Useful for quickly understanding the shape of large or unfamiliar JSON datasets without reading them manually.

Given a JSON file, jinspect will tell you:
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

## Usage

```
jinspect <FILE> [options]
```

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

## License

MIT
