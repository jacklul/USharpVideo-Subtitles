#!/bin/bash

PACKAGE_DIR="$1"
{ [ -z "$PACKAGE_DIR" ] || [ ! -f "$PACKAGE_DIR/package.json" ] ; } && { echo "Package dir was not provided or is invalid"; exit 1; }

# Remove assembly definition files, including UdonSharp ones
find "$PACKAGE_DIR" -name "*.asmdef" -exec sh -c 'rm -v "$1" "$1.meta" "${1%.asmdef}.asset" "${1%.asmdef}.asset.meta"' _ {} \;

# Replace references to package assembly names with standard Assembly-CSharp
replace="Assembly-CSharp"

find=" Jacklul.USharpVideoSubtitles.Editor"
grep -rlF "$find" "$PACKAGE_DIR" | while IFS= read -r file; do
    sed "s|$find|$replace|g" -i "$file"
done

find=" Jacklul.USharpVideoSubtitles"
grep -rlF "$find" "$PACKAGE_DIR" | while IFS= read -r file; do
    sed "s|$find|$replace|g" -i "$file"
done

find=" USharpVideo"
grep -rlF "$find" "$PACKAGE_DIR" | while IFS= read -r file; do
    sed "s|$find|$replace|g" -i "$file"
done
