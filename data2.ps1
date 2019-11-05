
$location = $PSScriptRoot + ' "MichaelsService2\bin\Debug\MichaelsService2.exe" '

$install_util = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe"
cmd /c "$($install_util) \u $($location)"