# ==============================================================================
# WebView2 Quick Cleanup Script for Testing Auto-Installation
# ==============================================================================
# Purpose: Remove WebView2 Runtime registry entries (and optionally files)
#          to test the auto-installation feature of NCF Desktop App
#
# IMPORTANT: Only registry cleanup is needed for testing!
#            File deletion is optional and may fail due to system locks.
# ==============================================================================

param(
    [switch]$SkipFileCleanup = $false
)

# Check admin rights
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "ERROR: This script requires administrator privileges" -ForegroundColor Red
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "WebView2 Cleanup for Testing" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will clean WebView2 registry entries" -ForegroundColor White
Write-Host "to allow testing of auto-installation." -ForegroundColor White
Write-Host ""

# ==============================================================================
# Step 1: Stop WebView2 Processes
# ==============================================================================
Write-Host "Step 1: Stopping WebView2 processes..." -ForegroundColor Yellow

$processNames = @("msedgewebview2", "WebView2", "msedge")
$stoppedCount = 0

foreach ($procName in $processNames) {
    $processes = Get-Process -Name $procName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            try {
                Write-Host "  - Stopping: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Cyan
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $stoppedCount++
            } catch {
                Write-Host "  - Failed to stop PID $($proc.Id): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
}

if ($stoppedCount -gt 0) {
    Write-Host "  ✅ Stopped $stoppedCount process(es)" -ForegroundColor Green
    Write-Host "  ⏳ Waiting 3 seconds for processes to fully terminate..." -ForegroundColor Gray
    Start-Sleep -Seconds 3
} else {
    Write-Host "  ✅ No WebView2 processes running" -ForegroundColor Green
}

# ==============================================================================
# Step 2: Stop Edge Update Services (optional)
# ==============================================================================
Write-Host ""
Write-Host "Step 2: Stopping Edge update services..." -ForegroundColor Yellow

$services = @("edgeupdate", "edgeupdatem")
foreach ($svc in $services) {
    $service = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq "Running") {
        try {
            Stop-Service -Name $svc -Force -ErrorAction Stop
            Write-Host "  - Stopped service: $svc" -ForegroundColor Green
        } catch {
            Write-Host "  - Could not stop service: $svc (not critical)" -ForegroundColor Yellow
        }
    }
}

# ==============================================================================
# Step 3: Delete Registry Keys (CRITICAL for testing)
# ==============================================================================
Write-Host ""
Write-Host "Step 3: Deleting registry keys..." -ForegroundColor Yellow
Write-Host "  (This is the CRITICAL step for testing auto-installation)" -ForegroundColor Cyan

$regPaths = @(
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
    "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}"
)

$regSuccess = $true
foreach ($regPath in $regPaths) {
    if (Test-Path $regPath) {
        try {
            Remove-Item -Path $regPath -Recurse -Force -ErrorAction Stop
            Write-Host "  ✅ Deleted: $regPath" -ForegroundColor Green
        } catch {
            Write-Host "  ❌ Failed: $regPath - $($_.Exception.Message)" -ForegroundColor Red
            $regSuccess = $false
        }
    } else {
        Write-Host "  ✓ Not found (already clean): $regPath" -ForegroundColor Gray
    }
}

# ==============================================================================
# Step 4: Delete Installation Files (OPTIONAL, may fail)
# ==============================================================================
Write-Host ""
if ($SkipFileCleanup) {
    Write-Host "Step 4: Skipping file cleanup (--SkipFileCleanup)" -ForegroundColor Gray
    $filesDeleted = $false
} else {
    Write-Host "Step 4: Deleting installation files..." -ForegroundColor Yellow
    Write-Host "  (This is OPTIONAL - file deletion may fail due to system locks)" -ForegroundColor Cyan
    
    $installDir = "${env:ProgramFiles(x86)}\Microsoft\EdgeWebView\Application"
    $filesDeleted = $false
    
    if (Test-Path $installDir) {
        Write-Host "  - Found: $installDir" -ForegroundColor Yellow
        Write-Host "  - Attempting to delete (5 retries)..." -ForegroundColor Cyan
        
        for ($i = 1; $i -le 5; $i++) {
            try {
                Remove-Item -Path $installDir -Recurse -Force -ErrorAction Stop
                Write-Host "  ✅ Files deleted successfully!" -ForegroundColor Green
                $filesDeleted = $true
                break
            } catch {
                if ($i -lt 5) {
                    Write-Host "  - Attempt $i/5 failed, retrying..." -ForegroundColor Yellow
                    Start-Sleep -Seconds 2
                } else {
                    Write-Host "  ⚠️  Could not delete files: $($_.Exception.Message)" -ForegroundColor Yellow
                    Write-Host "     Reason: Files may be locked by system services or Edge browser" -ForegroundColor Gray
                }
            }
        }
    } else {
        Write-Host "  ✓ Directory not found (already clean)" -ForegroundColor Gray
        $filesDeleted = $true
    }
}

# ==============================================================================
# Step 5: Verification
# ==============================================================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Verification & Results" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$regExists = $false
foreach ($regPath in $regPaths) {
    if (Test-Path $regPath) {
        $regExists = $true
        break
    }
}

$filesExist = Test-Path $installDir

Write-Host "Registry:  $(if (-not $regExists) { '✅ Cleaned' } else { '❌ Still exists' })" -ForegroundColor $(if (-not $regExists) { 'Green' } else { 'Red' })
Write-Host "Files:     $(if (-not $filesExist) { '✅ Cleaned' } else { '⚠️  Still exist' })" -ForegroundColor $(if (-not $filesExist) { 'Green' } else { 'Yellow' })

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan

if (-not $regExists) {
    Write-Host "🎉 SUCCESS: Ready to test!" -ForegroundColor Green
    Write-Host ""
    Write-Host "The registry has been cleaned, which is sufficient" -ForegroundColor White
    Write-Host "for testing the auto-installation feature." -ForegroundColor White
    
    if ($filesExist) {
        Write-Host ""
        Write-Host "Note: Installation files still exist, but this is OK!" -ForegroundColor Cyan
        Write-Host "      The app checks registry, not files." -ForegroundColor Gray
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Next Steps:" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "1. Open a NORMAL PowerShell window (not admin)" -ForegroundColor White
    Write-Host "2. Navigate to your publish folder:" -ForegroundColor White
    Write-Host "   cd Y:\...\publish-self-contained\win-arm64-final" -ForegroundColor Gray
    Write-Host "3. Run: .\NcfDesktopApp.GUI.exe" -ForegroundColor White
    Write-Host "4. Watch the logs for auto-installation!" -ForegroundColor White
    
} else {
    Write-Host "❌ FAILED: Registry cleanup failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Suggestions:" -ForegroundColor Yellow
    Write-Host "  1. Close all applications using WebView2" -ForegroundColor White
    Write-Host "  2. Close all browsers (Edge, Chrome)" -ForegroundColor White
    Write-Host "  3. Restart your computer" -ForegroundColor White
    Write-Host "  4. Run this script again" -ForegroundColor White
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

