@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

REM ====================================
REM NCF 桌面应用多平台发布脚本 (自包含 Windows)
REM 说明：该脚本专用于生成包含 .NET 运行时的自包含版本
REM 输出目录：publish-self-contained
REM ====================================

set "PROJECT_NAME=NcfDesktopApp.GUI"
set "SOLUTION_DIR=%~dp0..\"
set "OUTPUT_DIR=%SOLUTION_DIR%publish-self-contained"
set "BUILD_CONFIG=Release"

REM 支持的平台
set "PLATFORMS=win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64"

REM 默认参数
set "CLEAN=false"
set "SPECIFIC_PLATFORM="
set "SINGLE_FILE=false"
set "NO_RESTORE=false"

:parse_args
if "%~1"=="" goto end_parse_args
if /i "%~1"=="/h" goto show_help
if /i "%~1"=="-h" goto show_help
if /i "%~1"=="--help" goto show_help
if /i "%~1"=="/c" ( set "CLEAN=true" & shift /1 & goto parse_args )
if /i "%~1"=="--clean" ( set "CLEAN=true" & shift /1 & goto parse_args )
if /i "%~1"=="/p" ( set "SPECIFIC_PLATFORM=%~2" & shift /1 & shift /1 & goto parse_args )
if /i "%~1"=="--platform" ( set "SPECIFIC_PLATFORM=%~2" & shift /1 & shift /1 & goto parse_args )
if /i "%~1"=="--single-file" ( set "SINGLE_FILE=true" & shift /1 & goto parse_args )
if /i "%~1"=="--no-restore" ( set "NO_RESTORE=true" & shift /1 & goto parse_args )
echo 未知选项: %~1
goto show_help

:end_parse_args

:show_help
echo.
echo 用法: %~nx0 [选项]
echo.
echo 选项:
echo   /h, -h, --help             显示此帮助信息
echo   /c, --clean                发布前清理所有输出目录
echo   /p, --platform PLATFORM    只发布指定平台
echo   --single-file              创建单文件发布
echo   --no-restore               跳过包还原
echo.
echo 说明：该脚本始终以自包含模式发布（包含 .NET 运行时）
echo.
if "%~1"=="/h" exit /b 0
if "%~1"=="-h" exit /b 0
if "%~1"=="--help" exit /b 0
exit /b 1

:show_banner
echo.
echo ======================================
echo    NCF 桌面应用多平台发布工具（自包含）
echo ======================================
echo.
echo 项目: %PROJECT_NAME%
echo 解决方案目录: %SOLUTION_DIR%
echo 输出目录: %OUTPUT_DIR%
echo 构建配置: %BUILD_CONFIG%
echo.
goto :eof

:check_dotnet
echo [94m🔍 检查 .NET SDK...[0m
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [91m❌ 未找到 .NET SDK，请安装 .NET 8.0 或更高版本[0m
    exit /b 1
)
for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"
echo [92m✅ .NET SDK 版本: %DOTNET_VERSION%[0m
echo.
goto :eof

:clean_output
echo [93m🧹 清理输出目录...[0m
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
    echo [92m✅ 输出目录已清理[0m
) else (
    echo [93m⚠️  输出目录不存在，跳过清理[0m
)
echo.
goto :eof

:restore_packages
if "%NO_RESTORE%"=="true" (
    echo [93m⏭️  跳过包还原[0m
    goto :eof
)
echo [94m📦 还原 NuGet 包...[0m
cd /d "%SOLUTION_DIR%"
dotnet restore
if errorlevel 1 (
    echo [91m❌ 包还原失败[0m
    exit /b 1
)
echo [92m✅ 包还原成功[0m
echo.
goto :eof

:publish_platform
set "platform=%~1"
set "platform_dir=%OUTPUT_DIR%\%platform%"
if "%platform%"=="win-x64" set "platform_name=Windows x64"
if "%platform%"=="win-arm64" set "platform_name=Windows ARM64"
if "%platform%"=="osx-x64" set "platform_name=macOS Intel"
if "%platform%"=="osx-arm64" set "platform_name=macOS Apple Silicon"
if "%platform%"=="linux-x64" set "platform_name=Linux x64"
if "%platform%"=="linux-arm64" set "platform_name=Linux ARM64"

echo [94m🚀 发布 !platform_name! (%platform%)...[0m
set "cmd=dotnet publish -c %BUILD_CONFIG% -r %platform% -o "%platform_dir%" --self-contained true"
if "%SINGLE_FILE%"=="true" set "cmd=!cmd! -p:PublishSingleFile=true"
cd /d "%SOLUTION_DIR%"
!cmd!
if errorlevel 1 (
    echo [91m❌ !platform_name! 发布失败[0m
    set "PUBLISH_FAILED=true"
    goto :eof
)
if exist "%platform_dir%" (
    for /f %%i in ('dir /b "%platform_dir%" ^| find /c /v ""') do set "file_count=%%i"
    if !file_count! gtr 0 (
        echo [92m✅ !platform_name! 发布成功 (!file_count! 个文件^)[0m
    ) else (
        echo [91m❌ !platform_name! 发布失败：输出目录为空[0m
        set "PUBLISH_FAILED=true"
    )
) else (
    echo [91m❌ !platform_name! 发布失败：未找到输出目录[0m
    set "PUBLISH_FAILED=true"
)
echo.
goto :eof

:show_summary
echo [94m📊 发布总结[0m
echo ======================================
if exist "%OUTPUT_DIR%" (
    for %%p in (%PLATFORMS%) do (
        set "platform_dir=%OUTPUT_DIR%\%%p"
        if exist "!platform_dir!" (
            for /f %%i in ('dir /b "!platform_dir!" ^| find /c /v ""') do set "file_count=%%i"
            if !file_count! gtr 0 (
                echo [92m✅ %%p: !file_count! 个文件[0m
            ) else (
                echo [91m❌ %%p: 发布失败[0m
            )
        ) else (
            echo [91m❌ %%p: 发布失败[0m
        )
    )
    echo.
    echo [94m📁 发布位置: %OUTPUT_DIR%[0m
) else (
    echo [91m❌ 未找到发布输出[0m
)
echo.
goto :eof

call :show_banner
call :check_dotnet
if "%CLEAN%"=="true" call :clean_output
if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
call :restore_packages

set "success_count=0"
set "total_count=0"
set "PUBLISH_FAILED=false"

if not "%SPECIFIC_PLATFORM%"=="" (
    set /a "total_count=1"
    call :publish_platform "%SPECIFIC_PLATFORM%"
    if not "%PUBLISH_FAILED%"=="true" set /a "success_count=1"
) else (
    for %%p in (%PLATFORMS%) do (
        set /a "total_count+=1"
        call :publish_platform "%%p"
        if not "!PUBLISH_FAILED!"=="true" set /a "success_count+=1"
        set "PUBLISH_FAILED=false"
    )
)

call :show_summary

echo [94m📈 成功率: %success_count%/%total_count%[0m
if %success_count% equ %total_count% (
    echo [92m🎉 所有平台发布成功！（自包含）[0m
    exit /b 0
) else (
    echo [93m⚠️  部分平台发布失败[0m
    exit /b 1
)