Properties {
  $Configuration = "Release"
  $Version = "1.0.0"
  $ArtifactsDir = (Join-Path $PSScriptRoot "artifacts")
}

Task Default -Depends Build, Test, Pack

TaskSetup {
  Set-Location $PSScriptRoot
}

Task Build {
  Exec { dotnet build -c $Configuration "-p:Version=$Version.0" --verbosity minimal -nologo }
}

Task Test {
  Set-Location src\Tests
  Exec { dotnet test -c $Configuration --no-build --verbosity quiet -nologo }
}

Task Pack {
  Remove-Item $ArtifactsDir -Recurse -Force -ErrorAction SilentlyContinue
  Exec { dotnet pack -c $Configuration --no-build "-p:Version=$Version" -o $ArtifactsDir --verbosity minimal -nologo }
}
