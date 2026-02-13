# PromptRange Utils - 工具类库

## 📚 概述

这是 PromptRange 项目的工具类库，提供了一系列可复用的辅助功能，采用传统的 IIFE (立即执行函数) 模式，无需构建工具即可直接在浏览器中使用。

所有工具类都挂载在全局命名空间 `window.PromptRangeUtils` 下。

---

## ⚠️ 重要说明

### 关于 API 请求

**项目已有完善的 axios 封装** (`servicePR`)，包含：
- ✅ 请求/响应拦截器
- ✅ 自动错误处理和消息提示
- ✅ Token 自动注入
- ✅ 401/403 自动跳转

**直接使用项目现有的 `servicePR` 即可**：

```javascript
// ✅ 推荐：使用项目现有的 servicePR
servicePR.post('/api/xxx', { data: {...} })
    .then(res => {
        if (res.data.success) {
            // 成功处理
        }
    });
```

---

## 📦 工具类列表

### 1. HtmlHelper - HTML 操作工具
**文件**: `htmlHelper.js`

**功能**:
- HTML 转义
- 正则表达式转义
- UUID 生成
- 文件大小格式化
- 防抖/节流函数
- 深度克隆
- URL 参数获取
- 空值判断

**使用示例**:
```javascript
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
```

---

### 2. DateHelper - 日期时间工具
**文件**: `dateHelper.js`

**功能**:
- 日期格式化
- 相对时间显示（刚刚、N分钟前等）
- 时间差计算
- 持续时间格式化

**使用示例**:
```javascript
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
```

---

### 3. NameHelper - 名称查询工具
**文件**: `nameHelper.js`

**功能**:
- 统一的名称查询接口
- ID 与名称互查
- 支持自定义字段名
- 批量名称查询

**使用示例**:
```javascript
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
```

**在 prompt.js 中的使用**:
```javascript
// 已集成到 prompt.js 的 Name 查询方法
getModelName: function(id) {
    return window.PromptRangeUtils.NameHelper.getName(
        this.modelOpt, id, '未知模型'
    );
}
```

---

### 4. StorageHelper - 本地存储工具
**文件**: `storageHelper.js`

**功能**:
- LocalStorage 封装
- 自动 JSON 序列化/反序列化
- 批量操作
- 存储信息查询

**使用示例**:
```javascript
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
```

---

### 5. CopyHelper - 剪贴板工具
**文件**: `copyHelper.js`

**功能**:
- 复制文本到剪贴板
- 复制对象（自动 JSON 格式化）
- 复制数组
- 复制 HTML 内容
- 自动降级处理

**使用示例**:
```javascript
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
```

---

## 🚀 使用方法

### 1. 在 HTML 中引入

在 `prompt.js` 之前加载工具类：

```html
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
```

### 2. 在 Vue 中使用

```javascript
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
```

---

## 🧪 测试

打开 `test-utils.html` 文件进行测试：

```
/js/PromptRange/utils/test-utils.html
```

在浏览器中打开该文件，可以交互式测试所有工具类的功能。

---

## 📝 注意事项

### 1. 加载顺序

工具类**必须**在 `prompt.js` 之前加载，否则会报错。

### 2. 全局命名空间

所有工具类都挂载在 `window.PromptRangeUtils` 下：

```javascript
window.PromptRangeUtils = {
    HtmlHelper: {...},
    DateHelper: {...},
    NameHelper: {...},
    StorageHelper: {...},
    CopyHelper: {...}
};
```

### 3. ES5 兼容语法

工具类使用传统 JavaScript 语法，兼容 IE11+：
- 使用 `var` 而非 `const/let`
- 使用 `function` 而非箭头函数
- 使用传统 for 循环

### 4. IIFE 模式

每个工具类都使用 IIFE 封装，避免全局污染：

```javascript
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
```

---

## 🔗 相关文档

- [重构完成报告](../../../docs/REFACTOR-COMPLETE.md)
- [重构最终策略](../../../docs/refactor-final-strategy.md)
- [ApiHelper 修复报告](../../../docs/bugfix-apihelper.md)

---

## 📅 文档信息

- **创建日期**: 2025-12-15
- **更新日期**: 2025-12-15
- **版本**: 2.0 (清理后)
- **状态**: ✅ 完成

---

**祝使用愉快！** 🎉
