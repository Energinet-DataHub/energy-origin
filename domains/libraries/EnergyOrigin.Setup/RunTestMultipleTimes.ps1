$testPath = "C:\Users\saiuan\RiderProjects\energy-origin\domains\libraries\EnergyOrigin.Setup\EnergyOrigin.Setup.Tests\RabbitMq\RabbitMqConnectionTest.cs"
$testDir = Split-Path -Parent $testPath
$projectFile = Get-ChildItem -Path $testDir -Filter *.csproj -Recurse | Select-Object -First 1
$logFile = "TestRunResults_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

function Write-Log {
    param (
        [string]$Message,
        [string]$Color = "White"
    )

    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] $Message" -ForegroundColor $Color
    Add-Content -Path $logFile -Value "[$timestamp] $Message"
}

Write-Log "Starting test execution. Results will be logged to $logFile" "Cyan"
Write-Log "Project file: $($projectFile.FullName)" "Cyan"

$failedIterations = @()
$successCount = 0

for ($i = 1; $i -le 100; $i++) {
    Write-Log "Running test iteration $i of 100" "Cyan"

    dotnet test $projectFile.FullName --filter "FullyQualifiedName~RabbitMqConnectionTest" --no-restore --configuration Release -warnaserror --logger:"console;verbosity=minimal" -p:ParallelizeAssemblies=true -p:ParallelizeTestCollections=true

    if ($LASTEXITCODE -ne 0) {
        Write-Log "Test failed on iteration $i with exit code $LASTEXITCODE" "Red"
        $failedIterations += $i
    } else {
        $successCount++
        Write-Log "Test passed on iteration $i" "Green"
    }

    [System.GC]::Collect()
    [System.GC]::WaitForPendingFinalizers()

    Start-Sleep -Seconds 2
}

Write-Log "Test run complete" "Cyan"
Write-Log "Successful iterations: $successCount / 100" "Cyan"

if ($failedIterations.Count -gt 0) {
    Write-Log "Failed iterations: $($failedIterations.Count) / 100" "Red"
    Write-Log "Failed on iterations: $($failedIterations -join ', ')" "Red"
    exit 1  # Exit with error code
} else {
    Write-Log "All iterations passed successfully!" "Green"
    exit 0  # Exit with success code
}
