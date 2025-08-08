# ====================================
# NCF 桌面应用多平台发布脚本 (自包含 PowerShell)
# 说明：该脚本专用于生成包含 .NET 运行时的自包含版本
# 输出目录：publish-self-contained
# ====================================

[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$Clean,
    [string]$Platform = "",
    [switch]$SingleFile,
    [switch]$NoRestore,
    [switch]$Verbose
)

# 配置
$ProjectName = "NcfDesktopApp.GUI"
$SolutionDir = Split-Path -Parent $PSScriptRoot
$OutputDir = Join-Path $SolutionDir "publish-self-contained"
$BuildConfig = "Release"

# 支持的平台
$Platforms = @(
    "win-x64",
    "win-arm64",
    "osx-x64",
    "osx-arm64",
    "linux-x64",
    "linux-arm64"
)

# 平台显示名称
$PlatformNames = @{
    "win-x64" = "Windows x64"
    "win-arm64" = "Windows ARM64"
    "osx-x64" = "macOS Intel"
    "osx-arm64" = "macOS Apple Silicon"
    "linux-x64" = "Linux x64"
    "linux-arm64" = "Linux ARM64"
}

function Write-ColorText {
    param([string]$Text,[string]$Color = "White")
    $map = @{Red=1;Green=2;Yellow=3;Blue=4;Cyan=6;Magenta=5;White=7;Gray=8}
    try { Write-Host $Text -ForegroundColor $Color } catch { Write-Host $Text }
}

function Show-Help {
    Write-ColorText "用法: .\build-all-platforms-self-contained.ps1 [参数]" -Color "Blue"
    Write-Host ""
    Write-Host "参数:"
    Write-Host "  -Help                   显示此帮助信息"
    Write-Host "  -Clean                  发布前清理所有输出目录"
    Write-Host "  -Platform <PLATFORM>    只发布指定平台"
    Write-Host "  -SingleFile             创建单文件发布"
    Write-Host "  -NoRestore              跳过包还原"
    Write-Host "  -Verbose                显示详细输出"
    Write-Host ""
    Write-Host "说明：该脚本始终以自包含模式发布（包含 .NET 运行时）"
}

function Show-Banner {
    Write-ColorText @"

======================================
   NCF 桌面应用多平台发布工具（自包含）
======================================
"@ -Color "Blue"
    Write-Host ""
    Write-Host "项目: $ProjectName"
    Write-Host "解决方案目录: $SolutionDir"
    Write-Host "输出目录: $OutputDir"
    Write-Host "构建配置: $BuildConfig"
    Write-Host ""
}

function Test-DotNetSDK {
    Write-ColorText "🔍 检查 .NET SDK..." -Color "Blue"
    $ver = & dotnet --version 2>$null
    if ($LASTEXITCODE -ne 0) { Write-ColorText "❌ 未找到 .NET SDK，请安装 .NET 8.0 或更高版本" -Color "Red"; return $false }
    Write-ColorText "✅ .NET SDK 版本: $ver" -Color "Green"
    Write-Host ""
    return $true
}

function Clear-OutputDirectory {
    Write-ColorText "🧹 清理输出目录..." -Color "Yellow"
    if (Test-Path $OutputDir) { Remove-Item -Path $OutputDir -Recurse -Force; Write-ColorText "✅ 输出目录已清理" -Color "Green" } else { Write-ColorText "⚠️  输出目录不存在，跳过清理" -Color "Yellow" }
    Write-Host ""
}

function Restore-Packages {
    if ($NoRestore) { Write-ColorText "⏭️  跳过包还原" -Color "Yellow"; return $true }
    Write-ColorText "📦 还原 NuGet 包..." -Color "Blue"
    Push-Location $SolutionDir
    try { $out = & dotnet restore 2>&1; if ($LASTEXITCODE -eq 0) { Write-ColorText "✅ 包还原成功" -Color "Green"; if ($Verbose) { Write-Host $out }; Write-Host ""; return $true } else { Write-ColorText "❌ 包还原失败" -Color "Red"; Write-Host $out; return $false } } finally { Pop-Location }
}

function Publish-Platform { param([string]$PlatformId)
    $platformName = $PlatformNames[$PlatformId]
    $platformDir = Join-Path $OutputDir $PlatformId
    Write-ColorText "🚀 发布 $platformName ($PlatformId)..." -Color "Blue"
    $args = @("publish","-c",$BuildConfig,"-r",$PlatformId,"-o","`"$platformDir`"","--self-contained","true")
    if ($SingleFile) { $args += "-p:PublishSingleFile=true" }
    Push-Location $SolutionDir
    try {
        if ($Verbose) { Write-Host "执行命令: dotnet $($args -join ' ')" }
        $out = & dotnet @args 2>&1
        if ($LASTEXITCODE -eq 0) {
            if ((Test-Path $platformDir) -and (Get-ChildItem $platformDir).Count -gt 0) {
                $cnt = (Get-ChildItem $platformDir).Count
                Write-ColorText "✅ $platformName 发布成功 ($cnt 个文件)" -Color "Green"
                Write-Host ""
                return $true
            } else { Write-ColorText "❌ $platformName 发布失败：输出目录为空" -Color "Red"; Write-Host ""; return $false }
        } else { Write-ColorText "❌ $platformName 发布失败" -Color "Red"; Write-Host $out; Write-Host ""; return $false }
    } finally { Pop-Location }
}

function Show-Summary { param([int]$SuccessCount,[int]$TotalCount,[datetime]$StartTime)
    Write-ColorText "📊 发布总结" -Color "Blue"
    Write-Host "======================================"
    if (Test-Path $OutputDir) {
        foreach ($p in $Platforms) { $dir = Join-Path $OutputDir $p; $name = $PlatformNames[$p]; if ((Test-Path $dir) -and (Get-ChildItem $dir).Count -gt 0) { $cnt = (Get-ChildItem $dir).Count; $size = [math]::Round((Get-ChildItem $dir -Recurse | Measure-Object -Property Length -Sum).Sum/1MB,2); Write-ColorText "✅ $name: $cnt 个文件, $size MB" -Color "Green" } else { Write-ColorText "❌ $name: 发布失败" -Color "Red" } }
        Write-Host ""; Write-ColorText "📁 发布位置: $OutputDir" -Color "Blue"
    } else { Write-ColorText "❌ 未找到发布输出" -Color "Red" }
    Write-Host ""
    $elapsed = (Get-Date) - $StartTime
    Write-ColorText "⏱️  总耗时: $([math]::Round($elapsed.TotalSeconds,1))秒" -Color "Blue"
    Write-ColorText "📈 成功率: $SuccessCount/$TotalCount" -Color "Blue"
    if ($SuccessCount -eq $TotalCount) { Write-ColorText "🎉 所有平台发布成功！（自包含）" -Color "Green"; return 0 } else { Write-ColorText "⚠️  部分平台发布失败" -Color "Yellow"; return 1 }
}

function Main {
    if ($Help) { Show-Help; return 0 }
    if ($Platform -and ($Platform -notin $Platforms)) { Write-ColorText "❌ 不支持的平台: $Platform" -Color "Red"; Write-ColorText "支持的平台: $($Platforms -join ', ')" -Color "Yellow"; return 1 }
    $start = Get-Date
    Show-Banner
    if (!(Test-DotNetSDK)) { return 1 }
    if ($Clean) { Clear-OutputDirectory }
    if (!(Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
    if (!(Restore-Packages)) { return 1 }
    $success = 0; $total = 0
    if ($Platform) { $total = 1; if (Publish-Platform -PlatformId $Platform) { $success = 1 } } else { $total = $Platforms.Count; foreach ($plat in $Platforms) { if (Publish-Platform -PlatformId $plat) { $success++ } } }
    return (Show-Summary -SuccessCount $success -TotalCount $total -StartTime $start)
}

try { $exit = Main; exit $exit } catch { Write-ColorText "❌ 发生未处理的错误: $($_.Exception.Message)" -Color "Red"; if ($Verbose) { Write-Host $_.ScriptStackTrace }; exit 1 }