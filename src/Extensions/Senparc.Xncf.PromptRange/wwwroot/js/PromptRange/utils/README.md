[中文版](README.cn.md)

# PromptRange Utils - Utility library

## 📚 Overview

This is the tool class library of the PromptRange project, which provides a series of reusable auxiliary functions, using the traditional IIFE (immediate execution function) mode, which can be used directly in the browser without building tools.

All tool classes are mounted under the global namespace `window.PromptRangeUtils`.

---

## ⚠️ IMPORTANT NOTE

### About API requests

**The project has a complete axios package** (`servicePR`), including:
- ✅ Request/Response Interceptor
- ✅ Automatic error handling and message prompts
- ✅ Token automatically injected
- ✅ 401/403 automatically redirected

**Just use the existing `servicePR` of the project**:```javascript
// ✅ 推荐：使用项目现有的 servicePR
servicePR.post('/api/xxx', { data: {...} })
    .then(res => {
        if (res.data.success) {
            // 成功处理
        }
    });
```---

## 📦 Tool list

### 1. HtmlHelper - HTML operation tool
**File**: `htmlHelper.js`

**Function**:
- HTML escaping
- Regular expression escaping
- UUID generation
- File size formatting
- Anti-shake/throttle function
- Deep cloning
- URL parameter acquisition
- Null value judgment

**Usage Example**:```javascript
var HtmlHelper = window.PromptRangeUtils.HtmlHelper;

// HTML 转义
var escaped = HtmlHelper.escape('<script>alert("xss")</script>');

// 生成 UUID
var uuid = HtmlHelper.generateUUID();

// 文件大小格式化
var size = HtmlHelper.formatFileSize(1048576); // "1.00 MB"

// 防抖
var debouncedFunc = HtmlHelper.debounce(function() {
    console.log('执行');
}, 300);

// 判断空值
var isEmpty = HtmlHelper.isEmpty(''); // true
```---

### 2. DateHelper - Date and time tool
**File**: `dateHelper.js`

**Function**:
- Date formatting
- Relative time display (just now, N minutes ago, etc.)
- Time difference calculation
- Duration formatting

**Usage Example**:```javascript
var DateHelper = window.PromptRangeUtils.DateHelper;

// 日期格式化
var formatted = DateHelper.formatDate(new Date(), 'yyyy-MM-dd HH:mm:ss');
// "2025-12-15 14:30:00"

// 相对时间
var relative = DateHelper.getRelativeTime(new Date(Date.now() - 3600000));
// "1小时前"

// 时间差（毫秒）
var diff = DateHelper.getTimeDiff('2025-12-15', '2025-12-16');
// 86400000

// 持续时间格式化
var duration = DateHelper.formatDuration(3665000);
// "1小时1分钟5秒"
```---

### 3. NameHelper - Name query tool
**File**: `nameHelper.js`

**Function**:
- Unified name query interface
- ID and name mutual check
- Support custom field names
- Batch name query

**Usage Example**:```javascript
var NameHelper = window.PromptRangeUtils.NameHelper;

// 根据 ID 获取名称
var name = NameHelper.getName(
    [{value: 1, label: '选项1'}, {value: 2, label: '选项2'}],
    1,
    '未知选项'
);
// "选项1"

// 根据 ID 获取完整对象
var option = NameHelper.getOption(
    [{id: 1, name: 'Admin'}, {id: 2, name: 'User'}],
    1,
    'id' // 自定义 ID 字段名
);
// {id: 1, name: 'Admin'}

// 根据名称获取 ID
var id = NameHelper.getIdByName(
    [{value: 1, label: '选项1'}],
    '选项1'
);
// 1

// 批量查询
var names = NameHelper.getNames(
    [{value: 1, label: '选项1'}, {value: 2, label: '选项2'}],
    [1, 2]
);
// ["选项1", "选项2"]
```**Usage in prompt.js**:```javascript
// 已集成到 prompt.js 的 Name 查询方法
getModelName: function(id) {
    return window.PromptRangeUtils.NameHelper.getName(
        this.modelOpt, id, '未知模型'
    );
}
```---

### 4. StorageHelper - Local storage tool
**File**: `storageHelper.js`

**Function**:
- LocalStorage package
- Automatic JSON serialization/deserialization
- Batch operations
- Stored information query

**Usage Example**:```javascript
var StorageHelper = window.PromptRangeUtils.StorageHelper;

// 保存数据（自动 JSON 序列化）
StorageHelper.set('userInfo', {
    id: 1,
    name: 'Admin',
    roles: ['admin', 'user']
});

// 读取数据（自动 JSON 反序列化）
var userInfo = StorageHelper.get('userInfo');
// {id: 1, name: 'Admin', roles: ['admin', 'user']}

// 删除数据
StorageHelper.remove('userInfo');

// 清空所有数据
StorageHelper.clear();

// 检查键是否存在
var exists = StorageHelper.has('userInfo');
// false

// 获取所有键
var keys = StorageHelper.keys();
// ['key1', 'key2', ...]

// 获取存储大小
var size = StorageHelper.size();
// {used: 1024, available: 5242880, total: 5242880}

// 批量操作
StorageHelper.setMultiple({
    'key1': 'value1',
    'key2': {data: 'value2'}
});

var values = StorageHelper.getMultiple(['key1', 'key2']);
// {key1: 'value1', key2: {data: 'value2'}}
```---

### 5. CopyHelper - Clipboard Tool
**File**: `copyHelper.js`

**Function**:
- Copy text to clipboard
- Copy objects (automatic JSON formatting)
- copy array
- Copy HTML content
- Automatic downgrade processing

**Usage Example**:```javascript
var CopyHelper = window.PromptRangeUtils.CopyHelper;

// 复制文本
CopyHelper.copyText('Hello World', function(success) {
    if (success) {
        console.log('复制成功');
    }
});

// 复制对象（格式化为 JSON）
CopyHelper.copyObject({
    name: 'Test',
    value: 123
}, function(success) {
    console.log('复制', success ? '成功' : '失败');
});

// 复制数组（格式化）
CopyHelper.copyArray(['item1', 'item2', 'item3']);

// 复制 HTML 内容（保留格式）
CopyHelper.copyHtml('<strong>Bold Text</strong>');

// 复制 Prompt 结果（业务方法）
CopyHelper.copyPromptResult({
    content: 'Test content',
    score: 8.5
}, false); // false = 原始内容，true = HTML
```---

## 🚀 How to use

### 1. Introduced in HTML

Load utility classes before `prompt.js`:```html
<!-- 先加载第三方库 -->
<script src="/js/PromptRange/lib/axios.min.js"></script>
<script src="/js/PromptRange/axios.js"></script>

<!-- 然后加载工具类 -->
<script src="/js/PromptRange/utils/htmlHelper.js"></script>
<script src="/js/PromptRange/utils/dateHelper.js"></script>
<script src="/js/PromptRange/utils/nameHelper.js"></script>
<script src="/js/PromptRange/utils/storageHelper.js"></script>
<script src="/js/PromptRange/utils/copyHelper.js"></script>

<!-- 最后加载主文件 -->
<script src="/js/PromptRange/prompt.js"></script>
```### 2. Use in Vue```javascript
var app = new Vue({
    el: "#app",
    
    methods: {
        // 使用 DateHelper
        formatDate: function(date) {
            return window.PromptRangeUtils.DateHelper.formatDate(date);
        },
        
        // 使用 NameHelper（已集成）
        getModelName: function(id) {
            return window.PromptRangeUtils.NameHelper.getName(
                this.modelOpt, id, '未知模型'
            );
        },
        
        // 使用 CopyHelper
        copyResult: function(item) {
            window.PromptRangeUtils.CopyHelper.copyText(item.content);
        },
        
        // 使用 StorageHelper
        saveConfig: function() {
            window.PromptRangeUtils.StorageHelper.set('config', this.config);
        }
    }
});
```---

## 🧪 Test

Open the `test-utils.html` file for testing:```
/js/PromptRange/utils/test-utils.html
```Open this file in a browser to interactively test the functionality of all tool classes.

---

## 📝 Notes

### 1. Loading order

The tool class **must** be loaded before `prompt.js`, otherwise an error will be reported.

### 2. Global namespace

All tool classes are mounted under `window.PromptRangeUtils`:```javascript
window.PromptRangeUtils = {
    HtmlHelper: {...},
    DateHelper: {...},
    NameHelper: {...},
    StorageHelper: {...},
    CopyHelper: {...}
};
```### 3. ES5 compatible syntax

The tool class uses traditional JavaScript syntax and is compatible with IE11+:
- Use `var` instead of `const/let`
- Use `function` instead of arrow functions
- Use traditional for loop

### 4. IIFE mode

Each utility class is encapsulated using IIFE to avoid global pollution:```javascript
(function(window) {
    'use strict';
    
    // 工具类定义
    var Helper = {
        method: function() { }
    };
    
    // 挂载到全局
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.Helper = Helper;
    
})(window);
```---

## 🔗 Related documents

- [Refactoring completion report](../../../docs/REFACTOR-COMPLETE.md)
- [Refactor final strategy](../../../docs/refactor-final-strategy.md)
- [ApiHelper fix report](../../../docs/bugfix-apihelper.md)

---

## 📅 Document information

- **Creation date**: 2025-12-15
- **Updated date**: 2025-12-15
- **Version**: 2.0 (after cleaning)
- **Status**: ✅ Completed

---

**Happy use! ** 🎉
