# ====================================
# NCF 桌面应用多平台自包含发布脚本 (PowerShell)
# ====================================

[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$Clean,
    [string]$Platform = "",
    [switch]$SingleFile,
    [switch]$NoRestore,
    [switch]$ReadyToRun,
    [switch]$CreateApp,
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

# 颜色函数
function Write-ColorText {
    param(
        [string]$Text,
        [string]$Color = "White"
    )
    
    $colorMap = @{
        "Red" = [ConsoleColor]::Red
        "Green" = [ConsoleColor]::Green
        "Yellow" = [ConsoleColor]::Yellow
        "Blue" = [ConsoleColor]::Blue
        "Cyan" = [ConsoleColor]::Cyan
        "Magenta" = [ConsoleColor]::Magenta
        "White" = [ConsoleColor]::White
        "Gray" = [ConsoleColor]::Gray
    }
    
    if ($colorMap.ContainsKey($Color)) {
        Write-Host $Text -ForegroundColor $colorMap[$Color]
    } else {
        Write-Host $Text
    }
}

# 显示帮助信息
function Show-Help {
    Write-ColorText "用法: .\build-all-platforms-self-contained.ps1 [参数]" -Color "Blue"
    Write-Host ""
    Write-Host "参数:"
    Write-Host "  -Help                   显示此帮助信息"
    Write-Host "  -Clean                  发布前清理所有输出目录"
    Write-Host "  -Platform <PLATFORM>    只发布指定平台"
    Write-Host "  -SingleFile             创建单文件发布"
    Write-Host "  -NoRestore              跳过包还原"
    Write-Host "  -ReadyToRun             启用 ReadyToRun 优化"
    Write-Host "  -CreateApp              自动为 macOS 创建 .app 包（仅限 macOS 平台）"
    Write-Host "  -Verbose                显示详细输出"
    Write-Host ""
    Write-ColorText "支持的平台: $($Platforms -join ', ')" -Color "Yellow"
    Write-Host ""
    Write-Host "示例:"
    Write-Host "  .\build-all-platforms-self-contained.ps1                    # 发布所有平台（自包含）"
    Write-Host "  .\build-all-platforms-self-contained.ps1 -Clean             # 清理并发布所有平台"
    Write-Host "  .\build-all-platforms-self-contained.ps1 -Platform win-x64  # 只发布Windows x64"
    Write-Host "  .\build-all-platforms-self-contained.ps1 -SingleFile        # 创建单文件版本"
    Write-Host "  .\build-all-platforms-self-contained.ps1 -ReadyToRun        # 启用性能优化"
    Write-Host ""
    Write-ColorText "注意: 此脚本始终创建自包含发布（包含.NET运行时）" -Color "Yellow"
}

# 显示横幅
function Show-Banner {
    Write-ColorText @"

==================================================
   NCF 桌面应用多平台自包含发布工具
==================================================
"@ -Color "Blue"
    Write-Host ""
    Write-Host "项目: $ProjectName"
    Write-Host "解决方案目录: $SolutionDir"
    Write-Host "输出目录: $OutputDir"
    Write-Host "构建配置: $BuildConfig"
    Write-Host "发布类型: 自包含 (包含 .NET 运行时)"
    Write-Host ""
}

# 检查 .NET SDK
function Test-DotNetSDK {
    Write-ColorText "🔍 检查 .NET SDK..." -Color "Blue"
    
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-ColorText "✅ .NET SDK 版本: $dotnetVersion" -Color "Green"
            Write-Host ""
            return $true
        }
    }
    catch {
        # 处理异常
    }
    
    Write-ColorText "❌ 未找到 .NET SDK，请安装 .NET 8.0 或更高版本" -Color "Red"
    return $false
}

# 清理输出目录
function Clear-OutputDirectory {
    Write-ColorText "🧹 清理输出目录..." -Color "Yellow"
    
    if (Test-Path $OutputDir) {
        Remove-Item -Path $OutputDir -Recurse -Force
        Write-ColorText "✅ 输出目录已清理" -Color "Green"
    } else {
        Write-ColorText "⚠️  输出目录不存在，跳过清理" -Color "Yellow"
    }
    Write-Host ""
}

# 还原包
function Restore-Packages {
    if ($NoRestore) {
        Write-ColorText "⏭️  跳过包还原" -Color "Yellow"
        return $true
    }
    
    Write-ColorText "📦 还原 NuGet 包..." -Color "Blue"
    
    Push-Location $SolutionDir
    try {
        $output = & dotnet restore 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-ColorText "✅ 包还原成功" -Color "Green"
            if ($Verbose) {
                Write-Host $output
            }
            Write-Host ""
            return $true
        } else {
            Write-ColorText "❌ 包还原失败" -Color "Red"
            Write-Host $output
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

# 发布平台
function Publish-Platform {
    param(
        [string]$PlatformId
    )
    
    $platformName = $PlatformNames[$PlatformId]
    $platformDir = Join-Path $OutputDir $PlatformId
    
    Write-ColorText "🚀 发布 $platformName ($PlatformId) - 自包含版本..." -Color "Blue"
    
    # 构建发布命令参数
    $publishArgs = @(
        "publish"
        "-c", $BuildConfig
        "-r", $PlatformId
        "-o", "`"$platformDir`""
        "--self-contained", "true"
    )
    
    if ($SingleFile) {
        $publishArgs += "-p:PublishSingleFile=true"
        $publishArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
        $publishArgs += "-p:IncludeAllContentForSelfExtract=true"
    }
    
    if ($ReadyToRun) {
        $publishArgs += "-p:PublishReadyToRun=true"
    }
    
    # 添加优化选项
    $publishArgs += "-p:PublishTrimmed=false"  # 禁用裁剪以保证兼容性
    $publishArgs += "-p:TieredCompilation=true"
    $publishArgs += "-p:TieredPGO=true"
    
    # 执行发布
    Push-Location $SolutionDir
    try {
        if ($Verbose) {
            Write-Host "执行命令: dotnet $($publishArgs -join ' ')"
        }
        
        $output = & dotnet @publishArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            # 检查发布结果
            if ((Test-Path $platformDir) -and (Get-ChildItem $platformDir).Count -gt 0) {
                $fileCount = (Get-ChildItem $platformDir).Count
                Write-ColorText "✅ $platformName 发布成功 ($fileCount 个文件)" -Color "Green"
                
                # 显示主程序文件信息
                $mainExe = if ($PlatformId.StartsWith("win-")) {
                    Join-Path $platformDir "$ProjectName.exe"
                } else {
                    Join-Path $platformDir $ProjectName
                }
                
                if (Test-Path $mainExe) {
                    $fileSize = [math]::Round((Get-Item $mainExe).Length / 1MB, 2)
                    $exeName = if ($PlatformId.StartsWith("win-")) { "$ProjectName.exe" } else { $ProjectName }
                    Write-ColorText "   主程序: $exeName ($fileSize MB)" -Color "Green"
                    
                    # 对于Unix系统，设置可执行权限（如果在支持的系统上运行）
                    if ($PlatformId.StartsWith("osx-") -or $PlatformId.StartsWith("linux-")) {
                        if ($IsLinux -or $IsMacOS) {
                            try {
                                & chmod +x $mainExe
                                Write-ColorText "   ✅ 已设置可执行权限" -Color "Green"
                            }
                            catch {
                                Write-ColorText "   ⚠️  无法设置可执行权限 (在目标平台上需要手动设置)" -Color "Yellow"
                            }
                        } else {
                            Write-ColorText "   ℹ️  在目标平台上需要设置可执行权限: chmod +x $ProjectName" -Color "Cyan"
                        }
                    }
                }
                
                # 显示总目录大小
                $dirSize = [math]::Round((Get-ChildItem $platformDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
                Write-ColorText "   📦 总大小: $dirSize MB" -Color "Green"
                
                if ($Verbose) {
                    Write-Host "发布输出:"
                    Write-Host $output
                }
                
                Write-Host ""
                return $true
            } else {
                Write-ColorText "❌ $platformName 发布失败：输出目录为空" -Color "Red"
                if ($Verbose) {
                    Write-Host $output
                }
                Write-Host ""
                return $false
            }
        } else {
            Write-ColorText "❌ $platformName 发布失败" -Color "Red"
            Write-Host $output
            Write-Host ""
            return $false
        }
    }
    finally {
        Pop-Location
    }
}

# 显示发布总结
function Show-Summary {
    param(
        [int]$SuccessCount,
        [int]$TotalCount,
        [datetime]$StartTime
    )
    
    Write-ColorText "📊 自包含发布总结" -Color "Blue"
    Write-Host "======================================"
    
    if (Test-Path $OutputDir) {
        foreach ($platform in $Platforms) {
            $platformDir = Join-Path $OutputDir $platform
            $platformName = $PlatformNames[$platform]
            
            if ((Test-Path $platformDir) -and (Get-ChildItem $platformDir).Count -gt 0) {
                $fileCount = (Get-ChildItem $platformDir).Count
                $dirSize = [math]::Round((Get-ChildItem $platformDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
                Write-ColorText "✅ $platformName`: $fileCount 个文件, $dirSize MB" -Color "Green"
            } else {
                Write-ColorText "❌ $platformName`: 发布失败" -Color "Red"
            }
        }
        
        Write-Host ""
        Write-ColorText "📁 发布位置: $OutputDir" -Color "Blue"
        
        if (Test-Path $OutputDir) {
            $totalSize = [math]::Round((Get-ChildItem $OutputDir -Recurse | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
            Write-ColorText "📦 总大小: $totalSize MB" -Color "Blue"
        }
    } else {
        Write-ColorText "❌ 未找到发布输出" -Color "Red"
    }
    
    Write-Host ""
    
    # 显示耗时和成功率
    $elapsed = (Get-Date) - $StartTime
    Write-ColorText "⏱️  总耗时: $([math]::Round($elapsed.TotalSeconds, 1))秒" -Color "Blue"
    Write-ColorText "📈 成功率: $SuccessCount/$TotalCount" -Color "Blue"
    
    if ($SuccessCount -eq $TotalCount) {
        Write-ColorText "🎉 所有平台自包含发布成功！" -Color "Green"
        Write-ColorText "💡 提示: 自包含版本无需目标机器安装.NET运行时即可运行" -Color "Yellow"
        return 0
    } else {
        Write-ColorText "⚠️  部分平台发布失败" -Color "Yellow"
        return 1
    }
}

# 主程序
function Main {
    # 显示帮助
    if ($Help) {
        Show-Help
        return 0
    }
    
    # 验证特定平台
    if ($Platform -and ($Platform -notin $Platforms)) {
        Write-ColorText "❌ 不支持的平台: $Platform" -Color "Red"
        Write-ColorText "支持的平台: $($Platforms -join ', ')" -Color "Yellow"
        return 1
    }
    
    $startTime = Get-Date
    
    Show-Banner
    
    # 检查 .NET SDK
    if (!(Test-DotNetSDK)) {
        return 1
    }
    
    # 清理输出目录
    if ($Clean) {
        Clear-OutputDirectory
    }
    
    # 创建输出目录
    if (!(Test-Path $OutputDir)) {
        New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    }
    
    # 还原包
    if (!(Restore-Packages)) {
        return 1
    }
    
    # 发布平台
    $successCount = 0
    $totalCount = 0
    
    if ($Platform) {
        # 发布特定平台
        $totalCount = 1
        if (Publish-Platform -PlatformId $Platform) {
            $successCount = 1
        }
    } else {
        # 发布所有平台
        $totalCount = $Platforms.Count
        foreach ($plat in $Platforms) {
            if (Publish-Platform -PlatformId $plat) {
                $successCount++
            }
        }
    }
    
    # 🆕 如果发布了 macOS 平台且启用了 -CreateApp，自动创建 .app 包
    if ($CreateApp) {
        Write-ColorText "📦 检测到 -CreateApp 参数，准备创建 macOS .app 包..." -Color "Blue"
        
        # 检查是否发布了 macOS 平台
        $macosPublished = $false
        if ($Platform) {
            if ($Platform.StartsWith("osx-")) {
                $macosPublished = $true
            }
        } else {
            # 检查是否有任何 macOS 平台被发布
            foreach ($plat in $Platforms) {
                if ($plat.StartsWith("osx-")) {
                    $platformDir = Join-Path $OutputDir $plat
                    if ((Test-Path $platformDir) -and (Get-ChildItem $platformDir).Count -gt 0) {
                        $macosPublished = $true
                        break
                    }
                }
            }
        }
        
        if ($macosPublished) {
            Write-ColorText "🍎 正在创建 macOS .app 包..." -Color "Blue"
            
            # 检查是否在 macOS 上运行
            if ($IsMacOS) {
                # 调用 create-macos-app.sh
                $createAppScript = Join-Path $PSScriptRoot "create-macos-app.sh"
                if (Test-Path $createAppScript) {
                    & bash $createAppScript
                    Write-ColorText "✅ macOS .app 包创建完成" -Color "Green"
                } else {
                    Write-ColorText "❌ 未找到 create-macos-app.sh 脚本" -Color "Red"
                }
            } else {
                Write-ColorText "⚠️  当前系统不是 macOS，无法创建 .app 包" -Color "Yellow"
                Write-ColorText "   请在 macOS 系统上运行以下命令创建 .app 包:" -Color "Yellow"
                Write-ColorText "   ./build-tool/create-macos-app.sh" -Color "Yellow"
            }
        } else {
            Write-ColorText "⚠️  未发布 macOS 平台，跳过 .app 包创建" -Color "Yellow"
        }
        Write-Host ""
    }
    
    # 显示总结
    return Show-Summary -SuccessCount $successCount -TotalCount $totalCount -StartTime $startTime
}

# 执行主程序
try {
    $exitCode = Main
    exit $exitCode
}
catch {
    Write-ColorText "❌ 发生未处理的错误: $($_.Exception.Message)" -Color "Red"
    if ($Verbose) {
        Write-Host $_.ScriptStackTrace
    }
    exit 1
}