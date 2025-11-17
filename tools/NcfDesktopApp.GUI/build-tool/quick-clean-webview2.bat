@echo off
chcp 65001 > nul

echo.
echo ========================================
echo  WebView2 Cleanup for Testing
echo ========================================
echo.
echo This script will clean WebView2 registry
echo to test the auto-installation feature.
echo.
echo ========================================
echo  IMPORTANT NOTES:
echo ========================================
echo.
echo 1. This script REQUIRES administrator privileges
echo 2. Only registry cleanup is needed for testing
echo 3. File deletion may fail (but that's OK!)
echo.
echo ----------------------------------------
echo  How to run:
echo ----------------------------------------
echo 1. Right-click this file
echo 2. Select "Run as Administrator"
echo 3. Wait for completion
echo.
pause
echo.
echo Starting cleanup...
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0quick-clean-webview2.ps1"

echo.
echo Cleanup script finished.
echo.
pause

