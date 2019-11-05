$file = $PSScriptRoot + "data.ps1"
Start-Process powershell -argument $file -Verb runAs 