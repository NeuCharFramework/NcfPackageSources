# WebView Quick Start Guide

## 🚀 Quick Start

### First run

1. **Launch the application**
   ```
Double-click NcfDesktopApp.GUI.exe
   ```

2. **Automatically install WebView2** (first time only)
- The application will automatically detect the WebView2 Runtime
- If not installed, it will be automatically downloaded and installed.
- Show installation progress
- Automatically use the built-in browser after installation is complete

3. **Use NCF**
- Click the "Start NCF" button
- Wait for the NCF application to start
- The built-in browser will automatically open the NCF interface

---

## 🎯 Main functions

### 1. Address bar navigation

**Enter new URL**:
1. Enter the URL in the address bar (for example:`localhost:3000`）
2. Press **Enter** or click the **→** button
3. Automatically navigate to new page

**Autocomplete protocol**:
- input`localhost:5000`→ automatically converted to`http://localhost:5000`
- input`www.example.com`→ automatically converted to`http://www.example.com`

### 2. Navigation control

- **← Back**: Return to the previous page (may not be available)
- **→ Forward**: Forward to the next page (may not be available)
- **↻ Refresh**: Reload the current page

### 3. Close the tag

1. Click the **✕Close Tag** button
2. Pop up a confirmation dialog box
3. Select:
- **Close** - closes the tab and stops NCF
- **Cancel** - Cancel the operation

---

## 🛠️ Troubleshooting

### WebView2 installation failed

**symptom**:
- Display "Built-in browser initialization failed"
- The area is black or an error message is displayed

**Solution**:

#### Option 1: Use an external browser (temporary)
1. Click the **🌍 Open in external browser** button
2. The system default browser will open NCF

#### Option 2: Manually install WebView2
1. Click the **⬇️ Download WebView2 Runtime** button
2. The browser will open the download page
3. Download and run the installer
4. Restart the NCF desktop application

#### Option 3: Command line installation
Run in PowerShell (admin):
```powershell
$url = "https://go.microsoft.com/fwlink/p/?LinkId=2124703"
$output = "$env:TEMP\WebView2.exe"
Invoke-WebRequest -Uri $url -OutFile $output
Start-Process -FilePath $output -Wait
```

### The address bar cannot be edited.

**symptom**:
- Unable to enter text in the address bar

**Solution**:
- Click on the address bar to make sure it has focus
- Check if there are other dialogs in the foreground
- Restart the app

### The process cannot be stopped

**symptom**:
- After closing the tab, there is still the Senparc.Web process in the task manager

**Solution**:
- This issue has been fixed in this version
- use`taskkill /T /F`Forcefully terminate the entire process tree
- If you still have problems, manually end the process in Task Manager

---

## 💡 Tips for use

### 1. Quick navigation
- use`Ctrl+L`Focus address bar (if supported)
- Enter the URL directly and press Enter

### 2. Prevent misuse
- A confirmation dialog will pop up when closing a tab
- Read the prompts carefully

### 3. External browser
- If there is a problem with the built-in browser, you can always use an external browser
- Click "Open in external browser"

### 4. Log viewing
- Detailed logs can be viewed on the "Settings" tab
- The log will show WebView2 detection and installation information

---

## 📝 FAQ

### Q: Why is the first startup so slow?
**A**: The first startup requires downloading and installing WebView2 Runtime (about 2-5 minutes), subsequent startups will be very fast.

### Q: Can I skip the WebView2 installation?
**A**: You can click "Open in external browser" to use the system browser, but the built-in browser function will not be available.

### Q: Will WebView2 update automatically?
**A**: Yes, WebView2 updates automatically through the Microsoft Edge update channel, no manual action required.

### Q: How to uninstall WebView2?
**A**: 
1. Open "Settings" → "Apps" → "Apps & Features"
2. Search for "Microsoft Edge WebView2 Runtime"
3. Click "Uninstall"

### Q: What operating systems are supported?
**A**: 
- ✅ Windows 10/11 (x64, x86, ARM64)
- ✅ macOS (using system WKWebView)
- ✅ Linux (using WebKitGTK)

---

## 🔗 Related resources

- [Detailed function description](./WEBVIEW2_AUTO_SETUP.md)
- [Change Log](./CHANGELOG_WEBVIEW.md)
- [WebView2 official document](https://learn.microsoft.com/microsoft-edge/webview2/)

---

## 📞 Get help

If you encounter problems:
1. View in-app logs
2. Consult this document
3. View [Detailed Function Description](./WEBVIEW2_AUTO_SETUP.md)
4. Contact technical support

---

**Version**: 1.1.0
**Last updated**: 2025-11-14

