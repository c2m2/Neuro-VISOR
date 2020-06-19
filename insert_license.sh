#!/bin/bash
# quick and dirty insert license script

LICENSE=license.txt
FILE=$1

if [ ! -f "$1" ]; then
  echo "Usage: $(basename $0)) FILENAME"
  exit 2
fi

# could also use sed
cat "$LICENSE" | cat - "$FILE"  > temp.txt && move temp.txt "$FILE"
