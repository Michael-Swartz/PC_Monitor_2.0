$file = $PSScriptRoot + "data2.ps1"
Start-Process powershell -argument $file -Verb runAs 