# NCF 桌面应用程序 - 跨平台兼容性指南

## 🌍 支持的平台

NCF桌面应用程序现已完全支持以下6个平台：

| 平台 | 架构 | 状态 | 测试验证 |
|------|------|------|----------|
| **Windows** | x64 | ✅ 完全支持 | ✅ 已验证 |
| **Windows** | ARM64 | ✅ 完全支持 | ⚠️ 需验证 |
| **macOS** | x64 (Intel) | ✅ 完全支持 | ⚠️ 需验证 |
| **macOS** | ARM64 (Apple Silicon) | ✅ 完全支持 | ✅ 已验证 |
| **Linux** | x64 | ✅ 完全支持 | ⚠️ 需验证 |
| **Linux** | ARM64 | ✅ 完全支持 | ⚠️ 需验证 |

## 🔧 平台特定优化

### 1. **端口检测机制**

#### Windows
```csharp
// 使用 netstat + findstr 命令
cmd.exe /c "netstat -an | findstr :port"
```

#### Unix/Linux/macOS
```csharp
// 使用 lsof 命令
lsof -i :port
```

### 2. **进程启动方式**

#### Windows
```csharp
UseShellExecute = true  // 通过 Windows Shell 启动
```

#### Unix/Linux/macOS
```csharp
UseShellExecute = false  // 直接进程启动
```

### 3. **浏览器启动**

#### Windows
```csharp
Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
```

#### macOS
```csharp
Process.Start("open", url);
```

#### Linux
```csharp
Process.Start("xdg-open", url);
```

### 4. **目录路径处理**

#### 应用数据目录
- **Windows**: `%LOCALAPPDATA%\NcfDesktopApp`
- **macOS**: `~/Library/Application Support/NcfDesktopApp`
- **Linux**: `~/.local/share/NcfDesktopApp`

#### 路径分隔符
- **Windows**: `\` (反斜杠)
- **Unix/Linux/macOS**: `/` (正斜杠)

### 5. **环境变量设置**

#### Linux 特定优化
```csharp
// 解决 Linux 上的全球化问题
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = "1"
```

#### 所有平台
```csharp
ASPNETCORE_URLS = "http://localhost:{port}"
ASPNETCORE_ENVIRONMENT = "Production"
```

## 🧪 跨平台测试

### 运行兼容性测试
```bash
dotnet run --test
```

### 测试内容
1. **平台检测**: 验证运行时平台识别
2. **命令兼容性**: 测试平台特定命令（netstat/lsof）
3. **浏览器启动**: 验证不同平台的浏览器打开机制
4. **目录路径**: 检查路径格式和特殊文件夹

### 测试结果示例（macOS ARM64）
```
🔍 跨平台兼容性测试
当前操作系统: Unix 15.5.0
运行时架构: osx-arm64
✅ macOS 平台检测正确
🔧 测试 Unix/Linux/macOS 命令兼容性...
✅ lsof 命令测试完成 (退出代码: 0)

🌐 测试浏览器启动兼容性...
macOS: 使用 'open' 命令
✅ macOS 浏览器启动配置正确

📁 测试目录路径兼容性...
LocalApplicationData: /Users/user/Library/Application Support
UserProfile: /Users/user
路径分隔符: '/'
✅ Unix 路径格式正确
```

## 🚀 部署指南

### 1. 平台特定编译
```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true

# Windows ARM64
dotnet publish -c Release -r win-arm64 --self-contained true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true

# macOS ARM64
dotnet publish -c Release -r osx-arm64 --self-contained true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true

# Linux ARM64
dotnet publish -c Release -r linux-arm64 --self-contained true
```

### 2. 依赖要求

#### 所有平台
- .NET 8.0 Runtime（如果不使用 self-contained）
- 网络连接（用于下载 NCF 包）

#### Linux 特定
- `lsof` 命令（通常预装）
- `xdg-open` 命令（通常预装）

#### macOS 特定
- `lsof` 命令（系统自带）
- `open` 命令（系统自带）

#### Windows 特定
- `netstat` 命令（系统自带）
- `cmd.exe`（系统自带）

## 🔍 故障排除

### 常见问题

#### 1. 端口检测失败
**症状**: 应用无法找到可用端口
**解决方案**: 
- 确保系统有 `lsof`（Unix）或 `netstat`（Windows）命令
- 检查防火墙设置
- 尝试手动指定不同的端口范围

#### 2. 浏览器无法启动
**症状**: 浏览器不会自动打开
**解决方案**:
- 手动访问显示的 URL
- 检查系统默认浏览器设置
- 在 Linux 上确保安装了 `xdg-open`

#### 3. 权限问题
**症状**: 无法创建目录或启动进程
**解决方案**:
- 确保应用有足够的文件系统权限
- 在 Linux/macOS 上可能需要 `chmod +x` 权限
- 检查 SELinux/AppArmor 设置（Linux）

#### 4. .NET 运行时问题
**症状**: 应用无法启动或 NCF 站点启动失败
**解决方案**:
- 确保安装了 .NET 8.0 Runtime
- 使用 self-contained 发布避免运行时依赖
- 检查环境变量设置

## 📝 开发者注意事项

### 添加新的平台支持
1. 在 `IsPortInUseAsync` 中添加平台特定的端口检测逻辑
2. 在 `OpenBrowser` 中添加浏览器启动逻辑
3. 在 `StartNcfSiteAsync` 中添加任何需要的环境变量
4. 更新 `CrossPlatformTest` 中的测试用例
5. 在 GitHub Actions 中添加新平台的 CI/CD 支持

### 维护最佳实践
- 使用 `RuntimeInformation.IsOSPlatform()` 进行平台检测
- 避免硬编码路径分隔符，使用 `Path.DirectorySeparatorChar`
- 在使用平台特定命令前添加异常处理
- 定期运行跨平台测试验证兼容性

## 🎯 未来规划

- [ ] 增加 Windows ARM64 的实际设备测试
- [ ] 增加 Linux 发行版特定的优化
- [ ] 添加容器化支持（Docker）
- [ ] 实现自动更新机制
- [ ] 添加图形界面（GUI）版本

---

## 📞 支持

如果在特定平台上遇到问题，请：
1. 运行 `dotnet run --test` 查看兼容性测试结果
2. 检查系统日志和错误信息
3. 在 GitHub Issues 中报告问题，包含平台信息和错误详情 