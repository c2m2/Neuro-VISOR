#!/bin/bash
# quick and dirty insert license script
## Usage:  find . -iname "*.cs" -print0 | xargs -0 ./insert_license.sh 

LICENSE=license.txt
FILE=$1

if [ ! -f "$1" ]; then
  echo "Usage: $(basename $0)) FILENAME"
  exit 2
fi

# could also use sed
cat "$LICENSE" | cat - "$FILE"  > temp.txt && mv temp.txt "$FILE"
