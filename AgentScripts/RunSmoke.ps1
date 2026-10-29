$ErrorActionPreference = 'Stop'
$smokeBuildPath = [IO.Path]::GetFullPath('Temp/VhsPlayerSmoke/VhsInheritance.exe')
$smokeLogPath = [IO.Path]::GetFullPath('Temp/verification/standalone-smoke.log')
$smokeReportPath = [IO.Path]::GetFullPath('Temp/verification/standalone-smoke.json')
$smokeProcess = Start-Process -FilePath $smokeBuildPath -ArgumentList @('-screen-width', '1280', '-screen-height', '720', '-screen-fullscreen', '0', '-logFile', $smokeLogPath) -PassThru -WindowStyle Hidden
if (!$smokeProcess.WaitForExit(45000)) { throw 'Isolated smoke player did not finish within 45 seconds' }
$smokeProcess.Refresh()
if ($smokeProcess.ExitCode -ne 0) { throw "Isolated player failed with exit code $($smokeProcess.ExitCode); see $smokeLogPath" }
$smokeReport = Get-Content -LiteralPath $smokeReportPath -Raw -Encoding utf8 | ConvertFrom-Json
if (!$smokeReport.passed -or $smokeReport.frame -le 2) { throw "Player validation failed: $($smokeReport.error)" }
$smokeReport | ConvertTo-Json
