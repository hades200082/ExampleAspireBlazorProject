param (
    [string]$projectName
)

# Function to convert a string to PascalCase
function ConvertTo-PascalCase($str) {
    $str -replace '[^a-zA-Z0-9]', ' ' |
            ForEach-Object { $_.ToLower() } |
            ForEach-Object { $_ -replace '(\s|^)(\w)', { $args[1].ToUpper() } }
}

if (-not $projectName) {
    $projectName = Read-Host "What is the name of your project?"
}

$projectName = ConvertTo-PascalCase $projectName

# Get the script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Replace text in files and directories
function Replace-TemplateText {
    param (
        [string]$path
    )

    if (Test-Path $path -PathType Container) {
        # Replace directory names
        Get-ChildItem -Path $path -Recurse -Force | ForEach-Object {
            $newName = $_.Name -replace '(?i)TemplateProject', $projectName
            if ($_.Name -ne $newName) {
                Rename-Item -Path $_.FullName -NewName $newName -Force
            }
        }
    } else {
        # Replace content in files
        (Get-Content -Path $path) -replace '(?i)TemplateProject', $projectName | Set-Content -Path $path
    }
}

# Iterate over all files and directories
Get-ChildItem -Path $scriptDir -Recurse -Force | ForEach-Object {
    if ($_.FullName -ne $MyInvocation.MyCommand.Path) {
        Replace-TemplateText -path $_.FullName
    }
}
