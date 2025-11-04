@echo off
chcp 65001 > nul
setlocal enabledelayedexpansion

REM ====================================
REM NCF 桌面应用多平台发布脚本 (Windows)
REM ====================================

set "PROJECT_NAME=NcfDesktopApp.GUI"
set "SOLUTION_DIR=%~dp0..\"
set "OUTPUT_DIR=%SOLUTION_DIR%publish"
set "BUILD_CONFIG=Release"

REM 支持的平台
set "PLATFORMS=win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64"

REM 默认参数
set "CLEAN=false"
set "SPECIFIC_PLATFORM="
set "SELF_CONTAINED=false"
set "SINGLE_FILE=false"
set "NO_RESTORE=false"

REM 解析命令行参数
:parse_args
if "%~1"=="" goto end_parse_args
if /i "%~1"=="/h" goto show_help
if /i "%~1"=="-h" goto show_help
if /i "%~1"=="--help" goto show_help
if /i "%~1"=="/c" (
    set "CLEAN=true"
    shift /1
    goto parse_args
)
if /i "%~1"=="--clean" (
    set "CLEAN=true"
    shift /1
    goto parse_args
)
if /i "%~1"=="/p" (
    set "SPECIFIC_PLATFORM=%~2"
    shift /1
    shift /1
    goto parse_args
)
if /i "%~1"=="--platform" (
    set "SPECIFIC_PLATFORM=%~2"
    shift /1
    shift /1
    goto parse_args
)
if /i "%~1"=="--self-contained" (
    set "SELF_CONTAINED=true"
    shift /1
    goto parse_args
)
if /i "%~1"=="--single-file" (
    set "SINGLE_FILE=true"
    shift /1
    goto parse_args
)
if /i "%~1"=="--no-restore" (
    set "NO_RESTORE=true"
    shift /1
    goto parse_args
)
echo 未知选项: %~1
goto show_help

:end_parse_args

REM 显示帮助信息
:show_help
echo.
echo 用法: %~nx0 [选项]
echo.
echo 选项:
echo   /h, -h, --help             显示此帮助信息
echo   /c, --clean                发布前清理所有输出目录
echo   /p, --platform PLATFORM    只发布指定平台
echo   --self-contained           创建自包含发布 (包含.NET运行时)
echo   --single-file              创建单文件发布
echo   --no-restore               跳过包还原
echo.
echo 支持的平台: %PLATFORMS%
echo.
echo 示例:
echo   %~nx0                      # 发布所有平台
echo   %~nx0 /c                   # 清理并发布所有平台
echo   %~nx0 /p win-x64           # 只发布Windows x64
echo   %~nx0 --self-contained     # 创建自包含版本
echo.
if "%~1"=="/h" exit /b 0
if "%~1"=="-h" exit /b 0
if "%~1"=="--help" exit /b 0
exit /b 1

REM 显示横幅
:show_banner
echo.
echo ======================================
echo    NCF 桌面应用多平台发布工具
echo ======================================
echo.
echo 项目: %PROJECT_NAME%
echo 解决方案目录: %SOLUTION_DIR%
echo 输出目录: %OUTPUT_DIR%
echo 构建配置: %BUILD_CONFIG%
echo.
goto :eof

REM 检查 .NET SDK
:check_dotnet
echo [92m🔍 检查 .NET SDK...[0m
where dotnet >nul 2>&1
if errorlevel 1 (
    echo [91m❌ 未找到 .NET SDK，请安装 .NET 8.0 或更高版本[0m
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set "DOTNET_VERSION=%%i"
echo [92m✅ .NET SDK 版本: %DOTNET_VERSION%[0m
echo.
goto :eof

REM 清理输出目录
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

REM 还原包
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

REM 发布平台
:publish_platform
set "platform=%~1"
set "platform_dir=%OUTPUT_DIR%\%platform%"

REM 设置平台显示名称
if "%platform%"=="win-x64" set "platform_name=Windows x64"
if "%platform%"=="win-arm64" set "platform_name=Windows ARM64"
if "%platform%"=="osx-x64" set "platform_name=macOS Intel"
if "%platform%"=="osx-arm64" set "platform_name=macOS Apple Silicon"
if "%platform%"=="linux-x64" set "platform_name=Linux x64"
if "%platform%"=="linux-arm64" set "platform_name=Linux ARM64"

echo [94m🚀 发布 !platform_name! (%platform%)...[0m

REM 构建发布命令
set "cmd=dotnet publish -c %BUILD_CONFIG% -r %platform% -o "%platform_dir%""

if "%SELF_CONTAINED%"=="true" (
    set "cmd=!cmd! --self-contained true"
) else (
    set "cmd=!cmd! --self-contained false"
)

if "%SINGLE_FILE%"=="true" (
    set "cmd=!cmd! -p:PublishSingleFile=true"
)

REM 执行发布
cd /d "%SOLUTION_DIR%"
!cmd!
if errorlevel 1 (
    echo [91m❌ !platform_name! 发布失败[0m
    set "PUBLISH_FAILED=true"
    goto :eof
)

REM 检查发布结果
if exist "%platform_dir%" (
    for /f %%i in ('dir /b "%platform_dir%" ^| find /c /v ""') do set "file_count=%%i"
    if !file_count! gtr 0 (
        echo [92m✅ !platform_name! 发布成功 (!file_count! 个文件^)[0m
        
        REM 显示主程序文件信息
        if "%platform:~0,3%"=="win" (
            set "main_exe=%platform_dir%\%PROJECT_NAME%.exe"
        ) else (
            set "main_exe=%platform_dir%\%PROJECT_NAME%"
        )
        
        if exist "!main_exe!" (
            for %%F in ("!main_exe!") do set "file_size=%%~zF"
            set /a "file_size_mb=!file_size!/1024/1024"
            echo [92m   主程序: %PROJECT_NAME%^(!file_size_mb! MB^)[0m
        )
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

REM 显示发布总结
:show_summary
echo [94m📊 发布总结[0m
echo ======================================

if exist "%OUTPUT_DIR%" (
    set "success_platforms="
    set "failed_platforms="
    
    for %%p in (%PLATFORMS%) do (
        set "platform_dir=%OUTPUT_DIR%\%%p"
        if exist "!platform_dir!" (
            for /f %%i in ('dir /b "!platform_dir!" ^| find /c /v ""') do set "file_count=%%i"
            if !file_count! gtr 0 (
                echo [92m✅ %%p: !file_count! 个文件[0m
                set "success_platforms=!success_platforms! %%p"
            ) else (
                echo [91m❌ %%p: 发布失败[0m
                set "failed_platforms=!failed_platforms! %%p"
            )
        ) else (
            echo [91m❌ %%p: 发布失败[0m
            set "failed_platforms=!failed_platforms! %%p"
        )
    )
    
    echo.
    echo [94m📁 发布位置: %OUTPUT_DIR%[0m
) else (
    echo [91m❌ 未找到发布输出[0m
)
echo.
goto :eof

REM 主程序
call :show_banner
call :check_dotnet

if "%CLEAN%"=="true" call :clean_output

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

call :restore_packages

REM 记录开始时间
for /f "tokens=1-4 delims=:.," %%a in ("%time%") do (
   set /a "start=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)

set "success_count=0"
set "total_count=0"
set "PUBLISH_FAILED=false"

if not "%SPECIFIC_PLATFORM%"=="" (
    REM 发布特定平台
    set /a "total_count=1"
    call :publish_platform "%SPECIFIC_PLATFORM%"
    if not "%PUBLISH_FAILED%"=="true" set /a "success_count=1"
) else (
    REM 发布所有平台
    for %%p in (%PLATFORMS%) do (
        set /a "total_count+=1"
        call :publish_platform "%%p"
        if not "!PUBLISH_FAILED!"=="true" set /a "success_count+=1"
        set "PUBLISH_FAILED=false"
    )
)

REM 计算耗时
for /f "tokens=1-4 delims=:.," %%a in ("%time%") do (
   set /a "end=(((%%a*60)+1%%b %% 100)*60+1%%c %% 100)*100+1%%d %% 100"
)
set /a "elapsed=(end-start)/100"

call :show_summary

echo [94m⏱️  总耗时: %elapsed%秒[0m
echo [94m📈 成功率: %success_count%/%total_count%[0m

if %success_count% equ %total_count% (
    echo [92m🎉 所有平台发布成功！[0m
    exit /b 0
) else (
    echo [93m⚠️  部分平台发布失败[0m
    exit /b 1
)