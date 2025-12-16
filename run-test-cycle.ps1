# Automated CSV Export Test and Fix Cycle
# Runs export and verification in a loop until all tests pass or max iterations reached

param(
    [int]$MaxIterations = 10,
    [string]$XmlPath = "data\gabinet_export_2025_12_09.xml"
)

$ErrorActionPreference = "Stop"
$IterationCount = 0
$Success = $false

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "   Automated CSV Export Test Cycle" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Max iterations: $MaxIterations" -ForegroundColor Yellow
Write-Host "XML file: $XmlPath" -ForegroundColor Yellow
Write-Host ""

# Build the project first
Write-Host "Building project..." -ForegroundColor Green
try {
    dotnet build --configuration Release 2>&1 | Tee-Object -Variable buildOutput
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BUILD FAILED!" -ForegroundColor Red
        Write-Host $buildOutput -ForegroundColor Red
        exit 1
    }
    Write-Host "Build successful!" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "BUILD ERROR: $_" -ForegroundColor Red
    exit 1
}

while ($IterationCount -lt $MaxIterations -and -not $Success) {
    $IterationCount++
    
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "   ITERATION $IterationCount / $MaxIterations" -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host ""
    
    # Step 1: Run export
    Write-Host "STEP 1: Running CSV export..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        $exportOutput = dotnet run --configuration Release -- export $XmlPath 2>&1
        $exportExitCode = $LASTEXITCODE
        
        Write-Host $exportOutput
        
        if ($exportExitCode -ne 0) {
            Write-Host ""
            Write-Host "EXPORT FAILED with exit code $exportExitCode" -ForegroundColor Red
            Write-Host "Output:" -ForegroundColor Red
            Write-Host $exportOutput -ForegroundColor Red
            
            # Check for specific errors
            if ($exportOutput -match "Exception|Error|error") {
                Write-Host ""
                Write-Host "Detected errors in export. Breaking cycle." -ForegroundColor Red
                break
            }
        } else {
            Write-Host ""
            Write-Host "Export completed successfully!" -ForegroundColor Green
        }
    } catch {
        Write-Host "EXPORT EXCEPTION: $_" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        break
    }
    
    Write-Host ""
    Write-Host "STEP 2: Running verification tests..." -ForegroundColor Yellow
    Write-Host ""
    
    try {
        $verifyOutput = dotnet run --configuration Release -- verify 2>&1
        $verifyExitCode = $LASTEXITCODE
        
        Write-Host $verifyOutput
        
        if ($verifyExitCode -eq 0) {
            Write-Host ""
            Write-Host "================================================================================" -ForegroundColor Green
            Write-Host "   ALL TESTS PASSED!" -ForegroundColor Green
            Write-Host "================================================================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "CSV files successfully generated and verified after $IterationCount iteration(s)" -ForegroundColor Green
            $Success = $true
        } else {
            Write-Host ""
            Write-Host "VERIFICATION FAILED with exit code $verifyExitCode" -ForegroundColor Red
            
            # Analyze verification output for specific issues
            if ($verifyOutput -match "B³êdy: (\d+)") {
                $errorCount = $matches[1]
                Write-Host "Found $errorCount error(s) in verification" -ForegroundColor Red
            }
            
            if ($verifyOutput -match "Ostrze¿enia: (\d+)") {
                $warningCount = $matches[1]
                Write-Host "Found $warningCount warning(s) in verification" -ForegroundColor Yellow
            }
            
            # Check if we should continue or stop
            if ($verifyOutput -match "Plik nie istnieje") {
                Write-Host ""
                Write-Host "Critical error: CSV files not generated. Breaking cycle." -ForegroundColor Red
                break
            }
        }
    } catch {
        Write-Host "VERIFICATION EXCEPTION: $_" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        break
    }
    
    if (-not $Success -and $IterationCount -lt $MaxIterations) {
        Write-Host ""
        Write-Host "Tests did not pass. Iteration $IterationCount complete." -ForegroundColor Yellow
        Write-Host "Continuing to next iteration..." -ForegroundColor Yellow
        Write-Host ""
        Start-Sleep -Seconds 2
    }
}

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "   TEST CYCLE SUMMARY" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total iterations: $IterationCount" -ForegroundColor Yellow
if ($Success) {
    Write-Host "Result: SUCCESS - All CSV files generated correctly!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Result: FAILED - Tests did not pass after $IterationCount iteration(s)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please review the errors above and fix the issues manually." -ForegroundColor Yellow
    exit 1
}
