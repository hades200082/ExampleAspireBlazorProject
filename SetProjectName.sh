#!/bin/bash

# Function to convert a string to PascalCase
to_pascal_case() {
    echo "$1" | awk -F'[^a-zA-Z0-9]+' '{ for(i=1;i<=NF;i++) { $i=toupper(substr($i,1,1)) tolower(substr($i,2)) } }1' | tr -d ' '
}

# Check for the first argument
if [ -z "$1" ]; then
    read -p "What is the name of your project? " project_name
else
    project_name=$1
fi

# Convert to PascalCase
project_name=$(to_pascal_case "$project_name")

# Get the script directory
script_dir=$(dirname "$(realpath "$0")")

# Function to replace text in files and directories
replace_template_text() {
    local path=$1

    if [ -d "$path" ]; then
        # Replace directory names
        find "$path" -depth -name '*[Tt]emplate[Pp]roject*' | while read -r dir; do
            new_name=$(echo "$dir" | sed -E "s/[Tt]emplate[Pp]roject/$project_name/g")
            mv "$dir" "$new_name"
        done
    elif [ -f "$path" ]; then
        # Replace content in files
        sed -i "s/[Tt]emplate[Pp]roject/$project_name/g" "$path"
    fi
}

# Iterate over all files and directories
find "$script_dir" -type d -o -type f | while read -r item; do
    if [ "$item" != "$(realpath "$0")" ]; then
        replace_template_text "$item"
    fi
done
