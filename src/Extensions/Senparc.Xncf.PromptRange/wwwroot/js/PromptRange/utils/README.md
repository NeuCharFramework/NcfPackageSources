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

**因此 `apiHelper.js` 暂不使用**，直接使用项目现有的 `servicePR` 即可：

```javascript
// ✅ 推荐：使用项目现有的 servicePR
servicePR.post('/api/xxx', { data: {...} })
    .then(res => {
        if (res.data.success) {
            // 成功处理
        }
    });

// ❌ 不推荐：使用 apiHelper（功能重复）
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
- 相对时间显示
- 时间差计算
- 持续时间格式化
- 日期判断（今天/昨天）

**使用示例**:
```javascript
var DateHelper = window.PromptRangeUtils.DateHelper;

// 日期格式化
var formatted = DateHelper.formatDate(new Date(), 'YYYY-MM-DD HH:mm:ss');

// 相对时间（如"5分钟前"）
var chatTime = DateHelper.formatChatTime(new Date(Date.now() - 300000));

// 时间差
var diff = DateHelper.getTimeDiff(startDate, endDate);
console.log(diff.hours + '小时');

// 判断是否为今天
var isToday = DateHelper.isToday(new Date());

// 持续时间格式化
var duration = DateHelper.formatDuration(3665000); // "1小时1分钟5秒"
```

---

### 3. NameHelper - 名称查询工具
**文件**: `nameHelper.js`

**功能**:
- 根据 ID 查询名称
- 根据名称查询 ID
- 批量查询
- ID 存在性检查
- 获取完整选项对象

**使用示例**:
```javascript
var NameHelper = window.PromptRangeUtils.NameHelper;

// 数据示例
var options = [
    {value: 1, label: '选项1'},
    {value: 2, label: '选项2'},
    {value: 3, label: '选项3'}
];

// 根据 ID 获取名称
var name = NameHelper.getName(options, 2, '未知'); // "选项2"

// 根据名称获取 ID
var id = NameHelper.getId(options, '选项2'); // 2

// 检查 ID 是否存在
var exists = NameHelper.hasId(options, 2); // true

// 批量获取名称
var names = NameHelper.getNames(options, [1, 2, 3]); // ["选项1", "选项2", "选项3"]

// 获取完整对象
var option = NameHelper.getOption(options, 2); // {value: 2, label: '选项2'}
```

---

### 4. StorageHelper - LocalStorage 工具
**文件**: `storageHelper.js`

**功能**:
- 自动 JSON 序列化/反序列化
- 批量操作
- 存储信息查询
- 可用性检查

**使用示例**:
```javascript
var StorageHelper = window.PromptRangeUtils.StorageHelper;

// 保存数据（自动 JSON 序列化）
StorageHelper.set('user', {name: 'John', age: 30});

// 读取数据（自动反序列化）
var user = StorageHelper.get('user'); // {name: 'John', age: 30}

// 读取数据并提供默认值
var settings = StorageHelper.get('settings', {theme: 'light'});

// 检查键是否存在
var exists = StorageHelper.has('user'); // true

// 删除数据
StorageHelper.remove('user');

// 批量操作
StorageHelper.setMultiple({
    key1: 'value1',
    key2: 'value2'
});

var data = StorageHelper.getMultiple(['key1', 'key2']);

// 获取所有键
var keys = StorageHelper.keys();

// 清空所有数据
StorageHelper.clear();

// 获取存储信息
var info = StorageHelper.getStorageInfo();
console.log('已使用: ' + info.percentage + '%');
```

---

### 5. CopyHelper - 复制功能工具
**文件**: `copyHelper.js`

**功能**:
- 复制文本到剪贴板
- 复制对象（JSON）
- 复制数组
- 复制 HTML
- 自动降级处理

**使用示例**:
```javascript
var CopyHelper = window.PromptRangeUtils.CopyHelper;

// 复制文本
CopyHelper.copyToClipboard('Hello World', '复制成功', '复制失败');

// 复制对象为 JSON
var obj = {name: 'Test', value: 123};
CopyHelper.copyObject(obj); // 复制格式化的 JSON

// 复制数组（用换行符分隔）
var arr = ['项目1', '项目2', '项目3'];
CopyHelper.copyArray(arr, '\n');

// 复制 Prompt 结果（专用方法）
CopyHelper.copyPromptResult(resultItem, false); // false=普通结果, true=原始结果

// 检查复制功能是否可用
var supported = CopyHelper.isSupported(); // true/false
```

---

### 6. ApiHelper - API 请求工具
**文件**: `apiHelper.js`

**依赖**: jQuery, Element UI

**功能**:
- 统一 API 调用
- 自动错误处理
- 自动 Loading 状态管理
- 批量请求

**使用示例**:
```javascript
// 在 Vue 实例中初始化
var ApiHelper = window.PromptRangeUtils.ApiHelper;
this.apiHelper = new ApiHelper('http://api.example.com');

// POST 请求
this.apiHelper.post('/api/user/login', 
    {username: 'admin', password: '123456'},
    {
        onSuccess: function(response) {
            console.log('登录成功', response.data);
        },
        onError: function(error) {
            console.error('登录失败', error);
        },
        loadingState: {key: 'loginLoading', target: this},
        errorMessage: '登录失败，请重试'
    }
);

// GET 请求
this.apiHelper.get('/api/user/list', {
    onSuccess: function(response) {
        this.userList = response.data;
    }.bind(this)
});

// 批量请求
this.apiHelper.batchRequest([
    {url: '/api/data1', method: 'GET'},
    {url: '/api/data2', method: 'GET'}
], function(results) {
    console.log('所有请求完成', results);
});
```

---

## 🚀 使用方法

### 1. 在 HTML 中引入

```html
<!-- 按顺序加载工具类 -->
<script src="/js/PromptRange/utils/htmlHelper.js"></script>
<script src="/js/PromptRange/utils/dateHelper.js"></script>
<script src="/js/PromptRange/utils/nameHelper.js"></script>
<script src="/js/PromptRange/utils/storageHelper.js"></script>
<script src="/js/PromptRange/utils/copyHelper.js"></script>
<script src="/js/PromptRange/utils/apiHelper.js"></script>

<!-- 然后加载主文件 -->
<script src="/js/PromptRange/prompt.js"></script>
```

### 2. 在 Vue 中使用

```javascript
var app = new Vue({
    el: "#app",
    
    data: function() {
        return {
            apiHelper: null
        };
    },
    
    created: function() {
        // 初始化 API Helper
        var ApiHelper = window.PromptRangeUtils.ApiHelper;
        this.apiHelper = new ApiHelper(this.devHost);
    },
    
    methods: {
        // 使用工具类
        formatDate: function(date) {
            return window.PromptRangeUtils.DateHelper.formatDate(date);
        },
        
        getModelName: function(id) {
            return window.PromptRangeUtils.NameHelper.getName(
                this.modelOpt, id, '未知模型'
            );
        },
        
        copyResult: function(item) {
            window.PromptRangeUtils.CopyHelper.copyPromptResult(item);
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

测试页面包含：
- ✓ 所有工具类的功能测试
- ✓ 全局命名空间检查
- ✓ 交互式测试按钮

---

## 📝 命名空间

所有工具类都挂载在 `window.PromptRangeUtils` 命名空间下：

```javascript
window.PromptRangeUtils = {
    HtmlHelper: {...},
    DateHelper: {...},
    NameHelper: {...},
    StorageHelper: {...},
    CopyHelper: {...},
    ApiHelper: function ApiHelper(baseUrl) {...}
};
```

---

## ⚙️ 技术特点

### 1. IIFE 模式
所有工具类使用 IIFE 模式封装，避免全局污染：

```javascript
(function(window) {
    'use strict';
    
    var Helper = {
        // 方法定义
    };
    
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.Helper = Helper;
    
})(window);
```

### 2. ES5 兼容
使用传统 JavaScript 语法，兼容所有浏览器：
- 使用 `var` 而非 `const/let`
- 使用 `function` 而非箭头函数
- 使用 `for` 循环而非 `for...of`

### 3. 无依赖（除 ApiHelper）
大部分工具类无外部依赖，可独立使用。
只有 `ApiHelper` 依赖 jQuery 和 Element UI。

---

## 🔧 维护说明

### 添加新的工具方法

在对应的工具类中添加方法：

```javascript
// 在 HtmlHelper 中添加新方法
var HtmlHelper = {
    // 现有方法...
    
    // 新方法
    newMethod: function(param) {
        // 实现
    }
};
```

### 创建新的工具类

1. 创建新文件 `newHelper.js`
2. 使用 IIFE 模式封装
3. 挂载到 `window.PromptRangeUtils`
4. 在主 HTML 中引入
5. 更新本 README

---

## 📄 许可证

Copyright © Senparc

---

## 📞 联系方式

如有问题，请联系开发团队。

**版本**: 1.0.0  
**最后更新**: 2025-12-15

