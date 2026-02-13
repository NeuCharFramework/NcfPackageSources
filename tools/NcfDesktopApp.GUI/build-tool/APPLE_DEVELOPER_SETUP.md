# 🍎 Apple 开发者账号配置指南

## 📋 目录

1. [注册 Apple 开发者账号](#1-注册-apple-开发者账号)
2. [配置证书和密钥](#2-配置证书和密钥)
3. [在脚本中使用签名](#3-在脚本中使用签名)
4. [公证应用程序（可选）](#4-公证应用程序可选)
5. [常见问题](#5-常见问题)

---

## 1. 注册 Apple 开发者账号

### 1.1 访问注册页面

1. 访问 [Apple Developer 官网](https://developer.apple.com/)
2. 点击右上角 **"Account"** 或 **"Enroll"**
3. 使用您的 Apple ID 登录（如果没有，需要先创建）

### 1.2 选择账号类型

**个人开发者账号**：
- 💰 **费用**：$99/年（约 ¥688/年）
- ✅ **适合**：个人开发者、小型项目
- 📝 **需要**：身份证或护照验证

**企业开发者账号**：
- 💰 **费用**：$299/年（约 ¥2,088/年）
- ✅ **适合**：公司、组织
- 📝 **需要**：公司营业执照、DUNS 编号

**推荐**：对于大多数开发者，**个人开发者账号**已经足够。

### 1.3 完成注册流程

1. **填写个人信息**
   - 姓名、地址、联系方式
   - 支付信息（信用卡）

2. **验证身份**
   - 上传身份证或护照照片
   - 等待 Apple 审核（通常 24-48 小时）

3. **激活账号**
   - 收到确认邮件后，登录 [Apple Developer Portal](https://developer.apple.com/account/)
   - 接受开发者协议

---

## 2. 配置证书和密钥

### 2.1 创建 App-Specific Password（用于公证）

如果需要进行公证（Notarization），需要创建 App-Specific Password：

1. 访问 [Apple ID 账户页面](https://appleid.apple.com/)
2. 登录后，进入 **"App-Specific Passwords"** 部分
3. 点击 **"Generate Password"**
4. 输入标签（如 "Notarization"）
5. 复制生成的密码（只显示一次，请妥善保存）

### 2.2 安装 Xcode Command Line Tools

```bash
# 检查是否已安装
xcode-select -p

# 如果未安装，执行以下命令
xcode-select --install
```

### 2.3 创建证书（自动方式 - 推荐）

使用 Xcode 自动管理证书（最简单）：

1. 打开 **Xcode**
2. 进入 **Preferences** > **Accounts**
3. 点击 **"+"** 添加您的 Apple ID
4. 选择您的账号，点击 **"Manage Certificates"**
5. 点击 **"+"** 选择 **"Developer ID Application"**
6. Xcode 会自动创建并下载证书到钥匙串

### 2.4 创建证书（手动方式）

如果需要手动创建：

1. 登录 [Apple Developer Portal](https://developer.apple.com/account/)
2. 进入 **Certificates, Identifiers & Profiles**
3. 点击 **Certificates** > **"+"**
4. 选择 **Developer ID Application**（用于分发到 Mac App Store 外）
5. 按照向导创建证书请求（CSR）：
   ```bash
   # 在终端中执行
   openssl req -new -newkey rsa:2048 -nodes -keyout private_key.pem -out certificate_request.csr
   ```
6. 上传 CSR 文件，下载证书
7. 双击下载的证书文件，导入到钥匙串

### 2.5 验证证书安装

```bash
# 查看所有可用的代码签名证书
security find-identity -v -p codesigning

# 应该能看到类似这样的输出：
# 1) ABC123DEF456 "Developer ID Application: Your Name (TEAM_ID)"
#      1 valid identities found
```

**重要**：记下证书的完整名称（包括引号内的内容），后续会用到。

---

## 3. 在脚本中使用签名

### 3.1 基本签名（自动查找证书）

脚本会自动查找系统中的 "Developer ID Application" 证书：

```bash
# 自动查找并使用证书签名
./build-tool/create-macos-app.sh --sign --create-dmg
```

### 3.2 指定签名身份

如果系统中有多个证书，或需要指定特定证书：

```bash
# 查看可用的签名身份
security find-identity -v -p codesigning

# 使用指定的签名身份
./build-tool/create-macos-app.sh \
    --sign \
    --identity "Developer ID Application: Your Name (TEAM_ID)" \
    --create-dmg
```

**注意**：签名身份名称必须完全匹配，包括引号。

### 3.3 验证签名

签名完成后，验证签名是否成功：

```bash
# 验证应用程序包的签名
codesign -dv --verbose=4 "macos-app/NCF Desktop-Universal.app"

# 检查签名详细信息
codesign --display --verbose=2 "macos-app/NCF Desktop-Universal.app"

# 验证签名有效性
spctl --assess --verbose "macos-app/NCF Desktop-Universal.app"
```

**成功标志**：
- `codesign` 输出显示签名信息
- `spctl` 返回 `accepted` 或 `source=Developer ID`

---

## 4. 公证应用程序（可选）

公证（Notarization）是 Apple 的额外安全验证，可以让应用程序通过 Gatekeeper 检查，用户无需手动允许。

### 4.1 配置公证凭据

创建 `~/.appstoreconnect/private_keys` 目录并配置：

```bash
# 创建目录
mkdir -p ~/.appstoreconnect/private_keys

# 设置环境变量（在 ~/.zshrc 或 ~/.bash_profile 中添加）
export APPLE_ID="your-apple-id@example.com"
export APPLE_APP_SPECIFIC_PASSWORD="your-app-specific-password"
export APPLE_TEAM_ID="YOUR_TEAM_ID"  # 从 Apple Developer Portal 获取
```

**获取 Team ID**：
1. 登录 [Apple Developer Portal](https://developer.apple.com/account/)
2. 在右上角可以看到 **Team ID**（格式：ABC123DEF4）

### 4.2 执行公证

```bash
# 签名并公证应用程序
./build-tool/create-macos-app.sh --sign --notarize --create-dmg
```

**注意**：当前脚本版本可能还没有完全实现公证功能。如果需要手动公证：

```bash
# 1. 先签名
./build-tool/create-macos-app.sh --sign --create-dmg

# 2. 创建 zip 文件（公证需要）
cd macos-app
zip -r "NCF Desktop.zip" "NCF Desktop-Universal.app"

# 3. 提交公证
xcrun notarytool submit "NCF Desktop.zip" \
    --apple-id "$APPLE_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" \
    --team-id "$APPLE_TEAM_ID" \
    --wait

# 4. 装订公证票据（Staple）
xcrun stapler staple "NCF Desktop-Universal.app"
```

### 4.3 验证公证状态

```bash
# 检查公证状态
spctl --assess --verbose --type execute "macos-app/NCF Desktop-Universal.app"

# 应该显示：accepted source=Notarized Developer ID
```

---

## 5. 常见问题

### Q1: 提示 "未找到有效的签名身份"

**原因**：系统中没有安装 "Developer ID Application" 证书。

**解决方案**：
1. 确认已注册 Apple 开发者账号
2. 在 Xcode 中创建证书（推荐）：
   - Xcode > Preferences > Accounts > Manage Certificates
   - 点击 "+" > "Developer ID Application"
3. 或手动从 Apple Developer Portal 下载证书

### Q2: 签名后仍然显示安全警告

**可能原因**：
- 使用了 ad-hoc 签名（`-`）而不是开发者证书
- 证书已过期
- 应用程序被修改后未重新签名

**解决方案**：
```bash
# 检查签名状态
codesign -dv --verbose=4 "macos-app/NCF Desktop-Universal.app"

# 重新签名
codesign --force --deep --sign "Developer ID Application: Your Name" \
    "macos-app/NCF Desktop-Universal.app"
```

### Q3: 证书过期怎么办？

**解决方案**：
1. 登录 [Apple Developer Portal](https://developer.apple.com/account/)
2. 进入 Certificates 页面
3. 创建新的 "Developer ID Application" 证书
4. 下载并安装新证书
5. 使用新证书重新签名应用程序

### Q4: 如何查看证书有效期？

```bash
# 查看证书详细信息
security find-identity -v -p codesigning

# 查看钥匙串中的证书
open /Applications/Utilities/Keychain\ Access.app
# 在左侧选择 "login" > "My Certificates"
# 找到您的证书，双击查看有效期
```

### Q5: 公证失败怎么办？

**常见错误**：

1. **"Invalid credentials"**
   - 检查 App-Specific Password 是否正确
   - 确认 Apple ID 和 Team ID 正确

2. **"The signature is invalid"**
   - 确保应用程序已正确签名
   - 检查所有嵌套的二进制文件都已签名

3. **"The executable is missing"**
   - 确认应用程序包结构完整
   - 检查 Info.plist 中的 CFBundleExecutable 设置

**调试方法**：
```bash
# 查看详细错误信息
xcrun notarytool log <submission-id> \
    --apple-id "$APPLE_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" \
    --team-id "$APPLE_TEAM_ID"
```

### Q6: 费用是多少？

- **个人开发者账号**：$99/年（约 ¥688/年）
- **企业开发者账号**：$299/年（约 ¥2,088/年）

**注意**：费用按年收取，自动续费。

### Q7: 免费账号可以签名吗？

**不可以**。只有付费的 Apple 开发者账号才能：
- 创建 "Developer ID Application" 证书
- 进行代码签名
- 提交公证

免费 Apple ID 只能用于：
- 在 Xcode 中开发和测试
- 在个人设备上安装测试应用（需要设备注册）

---

## 📚 相关资源

- [Apple Developer 官网](https://developer.apple.com/)
- [Apple Developer Portal](https://developer.apple.com/account/)
- [代码签名文档](https://developer.apple.com/documentation/security/code_signing_services)
- [公证文档](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [证书管理指南](https://developer.apple.com/support/certificates/)

---

## 🎯 快速参考

### 完整签名流程

```bash
# 1. 构建应用程序
./build-tool/build-all-platforms-self-contained.sh -p osx-arm64
./build-tool/build-all-platforms-self-contained.sh -p osx-x64

# 2. 创建并签名应用程序包
./build-tool/create-macos-app.sh \
    --sign \
    --identity "Developer ID Application: Your Name (TEAM_ID)" \
    --create-dmg \
    --clean

# 3. 验证签名
codesign -dv --verbose=4 "macos-app/NCF Desktop-Universal.app"
spctl --assess --verbose "macos-app/NCF Desktop-Universal.app"

# 4. （可选）公证
xcrun notarytool submit "macos-app/NCF Desktop.zip" \
    --apple-id "$APPLE_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" \
    --team-id "$APPLE_TEAM_ID" \
    --wait
```

---

**最后更新**：2025-01-XX  
**适用版本**：NCF Desktop App v1.0.0+



