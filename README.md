# jinspect

A command-line tool that infers the schema of JSON files by sampling their contents. Useful for quickly understanding the shape of large or unfamiliar JSON datasets without reading them manually.

Given a JSON file, jinspect will tell you:
- What fields exist and their types
- Which fields are optional (with presence counts)
- Array length ranges
- Sample values for leaf fields

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

<!-- screenshot -->

## License

MIT
