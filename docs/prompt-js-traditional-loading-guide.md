# Prompt.js 传统加载方式指南

## 📋 概述

由于 `prompt.js` 是直接通过 `<script>` 标签在浏览器中加载的传统 JavaScript 文件，**不经过任何构建工具编译**，因此我们需要使用传统的模块化模式来组织代码。

---

## 🚫 不能使用的特性

### ❌ ES6 模块系统
```javascript
// ❌ 不能使用
import { ApiHelper } from './utils/apiHelper.js';
export class Map3DManager { }
export default function() { }
```

### ❌ 现代构建工具特性
```javascript
// ❌ 不能使用
require('./utils/apiHelper.js');
const helper = await import('./helper.js');
```

### ❌ npm 包管理
```javascript
// ❌ 不能使用
import Vue from 'vue';
import * as THREE from 'three';
```

---

## ✅ 推荐的模块化模式

### 模式 1: IIFE (立即执行函数表达式) + 全局命名空间

这是最推荐的模式，既避免全局污染，又保持良好的封装性。

```javascript
// utils/apiHelper.js
(function(window) {
    'use strict';
    
    // 构造函数
    function ApiHelper(baseUrl) {
        this.baseUrl = baseUrl || '';
    }
    
    // 原型方法
    ApiHelper.prototype.request = function(options) {
        // 实现
    };
    
    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.ApiHelper = ApiHelper;
    
})(window);
```

**优点**:
- ✅ 避免全局变量污染
- ✅ 清晰的命名空间
- ✅ 良好的封装性
- ✅ 兼容所有浏览器

---

### 模式 2: 命名空间对象

用于工具函数集合，无需实例化。

```javascript
// utils/nameHelper.js
(function(window) {
    'use strict';
    
    var NameHelper = {
        getName: function(options, id, defaultName) {
            // 实现
        },
        
        createGetter: function(options, defaultName) {
            // 实现
        }
    };
    
    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.NameHelper = NameHelper;
    
})(window);
```

**优点**:
- ✅ 更简洁（无需 new）
- ✅ 适合纯函数工具
- ✅ 无状态，线程安全

---

## 📦 全局命名空间设计

### 推荐的命名空间结构

```javascript
window.PromptRangeUtils = {
    // 工具类
    ApiHelper: function ApiHelper(baseUrl) { },
    NameHelper: { getName: function() { } },
    DateHelper: { formatDate: function() { } },
    CopyHelper: { copyToClipboard: function() { } },
    StorageHelper: { get: function() { }, set: function() { } },
    HtmlHelper: { escape: function() { }, generateUUID: function() { } }
};

window.PromptRangeModules = {
    // 大型模块
    Map3DManager: function Map3DManager(container, options) { }
};
```

### 为什么分两个命名空间？

1. **PromptRangeUtils** - 小型工具类和辅助函数
   - 通用性强，可能被其他项目复用
   - 无状态或状态简单
   - 行数较少（< 200 行）

2. **PromptRangeModules** - 大型功能模块
   - 项目特定的业务逻辑
   - 有复杂状态管理
   - 行数较多（> 500 行）

---

## 📄 HTML 加载顺序

### 完整的加载顺序示例

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Prompt Range</title>
    
    <!-- 1. 第三方库（CDN或本地） -->
    <script src="https://cdn.jsdelivr.net/npm/vue@2.6.14/dist/vue.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/element-ui@2.15.9/lib/index.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/jquery@3.6.0/dist/jquery.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/echarts@5.4.0/dist/echarts.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/three@0.150.0/build/three.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/three@0.150.0/examples/js/controls/OrbitControls.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/gsap@3.12.0/dist/gsap.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/jszip@3.10.0/dist/jszip.min.js"></script>
    
    <!-- 2. 工具类（按依赖顺序） -->
    <script src="/js/PromptRange/utils/htmlHelper.js"></script>
    <script src="/js/PromptRange/utils/storageHelper.js"></script>
    <script src="/js/PromptRange/utils/dateHelper.js"></script>
    <script src="/js/PromptRange/utils/copyHelper.js"></script>
    <script src="/js/PromptRange/utils/nameHelper.js"></script>
    <script src="/js/PromptRange/utils/apiHelper.js"></script>
    
    <!-- 3. 大型模块 -->
    <script src="/js/PromptRange/modules/Map3DManager.js"></script>
    
    <!-- 4. 主文件（最后加载） -->
    <script src="/js/PromptRange/prompt.js"></script>
</head>
<body>
    <div id="app">
        <!-- Vue 模板 -->
    </div>
</body>
</html>
```

### ⚠️ 加载顺序规则

1. **第三方库在最前** - Vue, jQuery, Three.js 等
2. **工具类在中间** - 按依赖关系排序
3. **业务模块其次** - Map3DManager 等
4. **主文件在最后** - prompt.js

---

## 🔧 在主文件中使用模块

### 在 prompt.js 中使用工具类

```javascript
var app = new Vue({
    el: "#app",
    
    data: function() {
        return {
            apiHelper: null,
            map3dManager: null
        };
    },
    
    created: function() {
        // 初始化工具类实例
        var ApiHelper = window.PromptRangeUtils.ApiHelper;
        this.apiHelper = new ApiHelper(this.devHost);
    },
    
    methods: {
        // 使用工具类（直接调用静态方法）
        formatDate: function(date) {
            return window.PromptRangeUtils.DateHelper.formatDate(date);
        },
        
        // 使用工具类（调用实例方法）
        deleteModel: function(item) {
            var self = this;
            this.apiHelper.post('/Admin/PromptRange/DeleteModel', 
                { id: item.value },
                {
                    onSuccess: function() {
                        self.getModelListData();
                    }
                }
            );
        },
        
        // 使用大型模块
        initMap3D: function() {
            var Map3DManager = window.PromptRangeModules.Map3DManager;
            this.map3dManager = new Map3DManager(
                this.$refs.map3dContainer,
                { backgroundColor: 0x0a0e27 }
            );
            
            this.map3dManager.init();
        }
    }
});
```

---

## 🎯 代码风格建议

### 1. 使用严格模式

```javascript
(function(window) {
    'use strict';  // ✅ 总是使用严格模式
    
    // 代码
    
})(window);
```

### 2. 避免使用 ES6+ 语法

```javascript
// ❌ 避免使用
const helper = { name: 'test' };
let count = 0;
const getName = () => this.name;
const data = { ...oldData, newField: 1 };
const [a, b] = array;

// ✅ 使用传统语法
var helper = { name: 'test' };
var count = 0;
var getName = function() { return this.name; };
var data = Object.assign({}, oldData, { newField: 1 });
var a = array[0];
var b = array[1];
```

### 3. 保存 this 引用

```javascript
// ✅ 正确的方式
ApiHelper.prototype.request = function(options) {
    var self = this;  // 保存 this 引用
    
    $.ajax({
        success: function(response) {
            self._showMessage(response.msg, 'success');  // 使用 self
        }
    });
};
```

### 4. 使用构造函数模式（需要实例）

```javascript
function ApiHelper(baseUrl) {
    this.baseUrl = baseUrl || '';
}

ApiHelper.prototype.request = function(options) {
    // 实现
};

// 使用
var helper = new ApiHelper('http://api.example.com');
helper.request({ url: '/test' });
```

### 5. 使用对象字面量（无需实例）

```javascript
var NameHelper = {
    getName: function(options, id) {
        // 实现
    }
};

// 使用
NameHelper.getName(list, 123);
```

---

## 🔍 兼容性检查

### 在模块中检查依赖

```javascript
(function(window) {
    'use strict';
    
    // 检查必需的全局依赖
    if (typeof THREE === 'undefined') {
        console.error('Map3DManager requires THREE.js');
        return;
    }
    
    if (typeof gsap === 'undefined') {
        console.error('Map3DManager requires GSAP');
        return;
    }
    
    // 实现代码
    function Map3DManager(container, options) {
        // ...
    }
    
    // 暴露到全局
    window.PromptRangeModules = window.PromptRangeModules || {};
    window.PromptRangeModules.Map3DManager = Map3DManager;
    
})(window);
```

---

## 🛠️ 调试技巧

### 1. 检查命名空间是否正确加载

在浏览器控制台中：

```javascript
// 检查工具类是否加载
console.log(window.PromptRangeUtils);
// 应该输出: {ApiHelper: ƒ, NameHelper: {…}, DateHelper: {…}, ...}

// 检查模块是否加载
console.log(window.PromptRangeModules);
// 应该输出: {Map3DManager: ƒ}

// 测试工具类
var helper = new window.PromptRangeUtils.ApiHelper('http://test.com');
console.log(helper.baseUrl);  // 应该输出: "http://test.com"
```

### 2. 检查加载顺序

如果某个模块未定义，检查：
1. 文件是否存在
2. HTML 中的加载顺序是否正确
3. 文件路径是否正确
4. 是否有 JavaScript 错误（打开开发者工具查看）

### 3. 使用 debugger

```javascript
ApiHelper.prototype.request = function(options) {
    debugger;  // 浏览器会在这里暂停
    var self = this;
    // ...
};
```

---

## 📝 完整示例

### utils/apiHelper.js

```javascript
/**
 * API 请求辅助工具
 * 依赖: jQuery (全局 $)
 */
(function(window) {
    'use strict';
    
    /**
     * API Helper 构造函数
     * @param {string} baseUrl - API 基础URL
     */
    function ApiHelper(baseUrl) {
        this.baseUrl = baseUrl || '';
    }
    
    /**
     * 发送 API 请求
     * @param {Object} options - 请求选项
     */
    ApiHelper.prototype.request = function(options) {
        var self = this;
        var url = options.url || '';
        var method = options.method || 'POST';
        var data = options.data || {};
        var onSuccess = options.onSuccess;
        var onError = options.onError;
        var loadingState = options.loadingState;
        var errorMessage = options.errorMessage || '请求失败';
        
        // 设置 loading
        if (loadingState) {
            loadingState.target[loadingState.key] = true;
        }
        
        $.ajax({
            url: this.baseUrl + url,
            type: method,
            data: JSON.stringify(data),
            contentType: 'application/json',
            dataType: 'json',
            success: function(response) {
                if (response.success) {
                    if (response.msg) {
                        self._showMessage(response.msg, 'success');
                    }
                    if (onSuccess) {
                        onSuccess(response);
                    }
                } else {
                    self._showMessage(response.msg || errorMessage, 'error');
                    if (onError) {
                        onError(response);
                    }
                }
            },
            error: function(error) {
                self._showMessage(errorMessage, 'error');
                if (onError) {
                    onError(error);
                }
            },
            complete: function() {
                if (loadingState) {
                    loadingState.target[loadingState.key] = false;
                }
            }
        });
    };
    
    /**
     * POST 请求快捷方法
     */
    ApiHelper.prototype.post = function(url, data, options) {
        options = options || {};
        options.url = url;
        options.method = 'POST';
        options.data = data;
        return this.request(options);
    };
    
    /**
     * 显示消息
     * @private
     */
    ApiHelper.prototype._showMessage = function(message, type) {
        if (window.app && window.app.$message) {
            window.app.$message({
                message: message,
                type: type
            });
        }
    };
    
    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.ApiHelper = ApiHelper;
    
})(window);
```

### 在 prompt.js 中使用

```javascript
var app = new Vue({
    el: "#app",
    
    data: function() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            apiHelper: null,
            modelOpt: []
        };
    },
    
    created: function() {
        // 初始化 API Helper
        var ApiHelper = window.PromptRangeUtils.ApiHelper;
        this.apiHelper = new ApiHelper(this.devHost);
        
        // 加载初始数据
        this.getModelListData();
    },
    
    methods: {
        getModelListData: function() {
            var self = this;
            this.apiHelper.post('/Admin/PromptRange/GetModelList', 
                {},
                {
                    onSuccess: function(response) {
                        self.modelOpt = response.data || [];
                    },
                    errorMessage: '加载模型列表失败'
                }
            );
        },
        
        deleteModel: function(item) {
            var self = this;
            this.$confirm('确定删除该模型吗？', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(function() {
                self.apiHelper.post('/Admin/PromptRange/DeleteModel', 
                    { id: item.value },
                    {
                        onSuccess: function() {
                            self.getModelListData();
                        },
                        errorMessage: '删除失败'
                    }
                );
            }).catch(function() {
                // 用户取消
            });
        }
    }
});
```

---

## ✅ 最佳实践总结

### Do's ✅

1. **使用 IIFE 包装所有模块**
2. **使用全局命名空间组织代码**
3. **检查依赖是否存在**
4. **使用严格模式 `'use strict'`**
5. **保存 this 引用 (`var self = this`)**
6. **使用传统 JavaScript 语法 (ES5)**
7. **明确的文件加载顺序**

### Don'ts ❌

1. **不要使用 ES6 模块 (import/export)**
2. **不要使用箭头函数（影响 this 绑定）**
3. **不要使用 const/let（兼容性）**
4. **不要直接污染全局作用域**
5. **不要假设依赖已加载（要检查）**
6. **不要使用构建工具特性**

---

## 🎓 学习资源

### 理解 IIFE 模式
- [MDN: IIFE](https://developer.mozilla.org/en-US/docs/Glossary/IIFE)
- [Understanding JavaScript Function Invocation and "this"](https://yehudakatz.com/2011/08/11/understanding-javascript-function-invocation-and-this/)

### 命名空间模式
- [JavaScript Patterns: Namespace](https://www.oreilly.com/library/view/learning-javascript-design/9781449334840/ch13s15.html)
- [Modular JavaScript](https://addyosmani.com/resources/essentialjsdesignpatterns/book/#modularjavascript)

---

**文档版本**: 1.0  
**创建日期**: 2025-12-15  
**适用场景**: 传统浏览器端 JavaScript，无构建工具

