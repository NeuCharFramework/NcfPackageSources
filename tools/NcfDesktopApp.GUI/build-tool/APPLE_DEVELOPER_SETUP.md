[中文版](APPLE_DEVELOPER_SETUP.cn.md)

# 🍎 Apple Developer Account Configuration Guide

## 📋 Table of Contents

1. [Register Apple developer account](#1-Register-apple-developer account)
2. [Configure Certificate and Key](#2-Configure Certificate and Key)
3. [Use signatures in scripts](#3-Use signatures in scripts)
4. [Notarization Application (Optional)](#4-Notarization Application Optional)
5. [FAQ](#5-FAQ)

---

## 1. Register an Apple developer account

### 1.1 Visit the registration page

1. Visit [Apple Developer official website](https://developer.apple.com/)
2. Click **"Account"** or **"Enroll"** in the upper right corner
3. Sign in with your Apple ID (if you don’t have one, you need to create it first)

### 1.2 Select account type

**Personal developer account**:
- 💰 **Fee**: $99/year (approximately ¥688/year)
- ✅ **Suitable**: individual developers, small projects
- 📝 **Required**: ID card or passport verification

**Enterprise Developer Account**:
- 💰 **Fee**: $299/year (approximately ¥2,088/year)
- ✅ **Suitable**: companies, organizations
- 📝 **Required**: Company business license, DUNS number

**Recommendation**: For most developers, a **Personal Developer Account** is sufficient.

### 1.3 Complete the registration process

1. **Fill in personal information**
   - Name, address, contact information
   - Payment information (credit card)

2. **Verify identity**
   - Upload ID card or passport photo
   - Waiting for Apple review (usually 24-48 hours)

3. **Activate account**
   - After receiving the confirmation email, log in to [Apple Developer Portal](https://developer.apple.com/account/)
   - Accept the Developer Agreement

---

## 2. Configure certificates and keys

### 2.1 Create App-Specific Password (for notarization)

If notarization is required, you need to create an App-Specific Password:

1. Visit [Apple ID account page](https://appleid.apple.com/)
2. After logging in, enter the **"App-Specific Passwords"** section
3. Click **"Generate Password"**
4. Enter the label (such as "Notarization")
5. Copy the generated password (only displayed once, please keep it properly)

### 2.2 Install Xcode Command Line Tools
```bash
# 检查是否已安装
xcode-select -p

# 如果未安装，执行以下命令
xcode-select --install
```
### 2.3 Create certificate (automatic method - recommended)

Use Xcode to automatically manage certificates (easiest):

1. Open **Xcode**
2. Go to **Preferences** > **Accounts**
3. Click **"+"** to add your Apple ID
4. Select your account and click **"Manage Certificates"**
5. Click **"+"** to select **"Developer ID Application"**
6. Xcode will automatically create and download the certificate to the keychain

### 2.4 Create certificate (manual method)

If you need to create it manually:

1. Log in to [Apple Developer Portal](https://developer.apple.com/account/)
2. Enter **Certificates, Identifiers & Profiles**
3. Click **Certificates** > **"+"**
4. Select **Developer ID Application** (for distribution outside the Mac App Store)
5. Follow the wizard to create a certificate request (CSR):
```bash
   # 在终端中执行
   openssl req -new -newkey rsa:2048 -nodes -keyout private_key.pem -out certificate_request.csr
   ```
6. Upload the CSR file and download the certificate
7. Double-click the downloaded certificate file and import it into the keychain

### 2.5 Verify certificate installation
```bash
# 查看所有可用的代码签名证书
security find-identity -v -p codesigning

# 应该能看到类似这样的输出：
# 1) ABC123DEF456 "Developer ID Application: Your Name (TEAM_ID)"
#      1 valid identities found
```
**Important**: Write down the full name of the certificate (including the content in quotation marks), which will be used later.

---

## 3. Use signatures in scripts

### 3.1 Basic signature (automatically find certificate)

The script automatically looks for the "Developer ID Application" certificate on the system:
```bash
# 自动查找并使用证书签名
./build-tool/create-macos-app.sh --sign --create-dmg
```
### 3.2 Specify signing identity

If you have multiple certificates on your system, or need to specify a specific certificate:
```bash
# 查看可用的签名身份
security find-identity -v -p codesigning

# 使用指定的签名身份
./build-tool/create-macos-app.sh \
    --sign \
    --identity "Developer ID Application: Your Name (TEAM_ID)" \
    --create-dmg
```
**Note**: The signing identity name must match exactly, including quotes.

### 3.3 Verify signature

After the signature is completed, verify whether the signature was successful:
```bash
# 验证应用程序包的签名
codesign -dv --verbose=4 "macos-app/NCF Desktop-Universal.app"

# 检查签名详细信息
codesign --display --verbose=2 "macos-app/NCF Desktop-Universal.app"

# 验证签名有效性
spctl --assess --verbose "macos-app/NCF Desktop-Universal.app"
```
**Success Flag**:
- `codesign` output displays signature information
- `spctl` returns `accepted` or `source=Developer ID`

---

## 4. Notarization application (optional)

Notarization is Apple's additional security verification that allows applications to pass Gatekeeper checks without the user having to manually allow them.

### 4.1 Configure notarization credentials

Create the `~/.appstoreconnect/private_keys` directory and configure:
```bash
# 创建目录
mkdir -p ~/.appstoreconnect/private_keys

# 设置环境变量（在 ~/.zshrc 或 ~/.bash_profile 中添加）
export APPLE_ID="your-apple-id@example.com"
export APPLE_APP_SPECIFIC_PASSWORD="your-app-specific-password"
export APPLE_TEAM_ID="YOUR_TEAM_ID"  # 从 Apple Developer Portal 获取
```
**Get Team ID**:
1. Log in to [Apple Developer Portal](https://developer.apple.com/account/)
2. You can see **Team ID** in the upper right corner (format: ABC123DEF4)

### 4.2 Perform notarization
```bash
# 签名并公证应用程序
./build-tool/create-macos-app.sh --sign --notarize --create-dmg
```
**Note**: The current script version may not fully implement the notarization function. If manual notarization is required:
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
### 4.3 Verify notarization status
```bash
# 检查公证状态
spctl --assess --verbose --type execute "macos-app/NCF Desktop-Universal.app"

# 应该显示：accepted source=Notarized Developer ID
```
---

## 5. FAQ

### Q1: Prompt "No valid signing identity found"

**Cause**: The "Developer ID Application" certificate is not installed in the system.

**Solution**:
1. Confirm that you have registered an Apple developer account
2. Create the certificate in Xcode (recommended):
   - Xcode > Preferences > Accounts > Manage Certificates
   - Click "+" > "Developer ID Application"
3. Or manually download the certificate from the Apple Developer Portal

### Q2: Security warning still displayed after signing

**Possible reasons**:
- Used ad-hoc signing (`-`) instead of developer certificate
- The certificate has expired
- Application was modified and not re-signed

**Solution**:
```bash
# 检查签名状态
codesign -dv --verbose=4 "macos-app/NCF Desktop-Universal.app"

# 重新签名
codesign --force --deep --sign "Developer ID Application: Your Name" \
    "macos-app/NCF Desktop-Universal.app"
```
### Q3: What should I do if the certificate expires?

**Solution**:
1. Log in to [Apple Developer Portal](https://developer.apple.com/account/)
2. Enter the Certificates page
3. Create a new "Developer ID Application" certificate
4. Download and install the new certificate
5. Re-sign the application with the new certificate

### Q4: How to check the validity period of the certificate?
```bash
# 查看证书详细信息
security find-identity -v -p codesigning

# 查看钥匙串中的证书
open /Applications/Utilities/Keychain\ Access.app
# 在左侧选择 "login" > "My Certificates"
# 找到您的证书，双击查看有效期
```
### Q5: What should I do if notarization fails?

**Common Mistakes**:

1. **"Invalid credentials"**
   - Check if the App-Specific Password is correct
   - Confirm Apple ID and Team ID are correct

2. **"The signature is invalid"**
   - Make sure the application is signed correctly
   - Check that all nested binaries are signed

3. **"The executable is missing"**
   - Confirm that the application package structure is complete
   - Check CFBundleExecutable setting in Info.plist

**Debugging method**:
```bash
# 查看详细错误信息
xcrun notarytool log <submission-id> \
    --apple-id "$APPLE_ID" \
    --password "$APPLE_APP_SPECIFIC_PASSWORD" \
    --team-id "$APPLE_TEAM_ID"
```
### Q6: How much does it cost?

- **Personal Developer Account**: $99/year (approximately ¥688/year)
- **Enterprise Developer Account**: $299/year (approximately ¥2,088/year)

**Note**: Fees are charged annually and automatically renewed.

### Q7: Can a free account sign?

**Can't**. Only paid Apple developer accounts can:
- Create "Developer ID Application" certificate
- Perform code signing
- Submit notarization

A free Apple ID can only be used with:
- Develop and test in Xcode
- Install the test app on your personal device (device registration required)

---

## 📚 Related resources

- [Apple Developer official website](https://developer.apple.com/)
- [Apple Developer Portal](https://developer.apple.com/account/)
- [Code Signing Documentation](https://developer.apple.com/documentation/security/code_signing_services)
- [Notarized Document](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)
- [Certificate Management Guide](https://developer.apple.com/support/certificates/)

---

## 🎯 Quick Reference

### Complete signature process
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

**Last Update**: 2025-01-XX
**Applicable version**: NCF Desktop App v1.0.0+
