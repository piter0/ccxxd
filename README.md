# ccxxd
A command line tool to make a hexdump or do the reverse.

## Usage
```
ccxxd <file path> [options]
```

### Options
-  `-c <value>`     Set octets per line.
-  `-e`             Use little-endian format.
-  `-g <value>`     Group bytes (default: 2).
-  `-l <value>`     Limit length.
-  `-r`             Revert hex to binary.
-  `-s <value>`     Start at seek offset.
