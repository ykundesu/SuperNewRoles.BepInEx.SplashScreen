if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$BIEdir = $dir + "BepInEx\"

$ver = "v" + (Get-ChildItem -Path ($BIEdir) -Filter ("*.dll") -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "([\d+\.]+?\d+)[\.0]*$", '${1}'

Compress-Archive -Path ($BIEdir) -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "BepInEx.SplashScreen_" + $ver + ".zip")
