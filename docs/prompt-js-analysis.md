# Prompt.js 代码分析文档

## 📋 文档概述

**文件路径**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`  
**文件大小**: 7,639 行代码  
**框架**: Vue.js 2.x  
**主要功能**: AI Prompt 测试靶场的前端交互系统

---

## 🎯 核心功能模块

### 1. **应用架构**
这是一个基于 Vue.js 2.x 的单页面应用，采用 MVVM 模式，主要包含以下几个核心模块：

```
prompt.js
├── 数据模型层 (data)
│   ├── 配置管理
│   ├── 表单状态
│   ├── 输出结果
│   └── UI 状态
├── 计算属性层 (computed)
│   ├── 页面加载状态
│   ├── Prompt 对比信息
│   └── 动态变量检测
├── 监听器层 (watch)
│   ├── 版本搜索
│   └── 内容变化监听
└── 方法层 (methods)
    ├── 数据管理 (CRUD)
    ├── API 交互
    ├── UI 交互
    ├── 3D 可视化
    └── 工具方法
```

---

## 📦 数据模型结构 (data)

### 1.1 核心配置数据
```javascript
{
  isAIGrade: true,              // AI 评分开关
  devHost: 'http://...',        // 开发环境主机
  pageChange: false,            // 页面变化标记
  isAvg: true,                  // 是否平均分
}
```

### 1.2 靶场配置 (Prompt Range Configuration)
```javascript
{
  promptField: '',              // 当前选择的靶场
  promptFieldOpt: [],           // 靶场列表选项
  promptOpt: [],                // Prompt 列表选项
  modelOpt: [],                 // AI 模型列表选项
  promptid: '',                 // 选择的靶道 ID
  modelid: '',                  // 选择的模型 ID
  content: '',                  // Prompt 输入内容
  remarks: '',                  // 备注
  numsOfResults: 1,             // 连发次数 (1-10)
}
```

### 1.3 参数设置 (Parameter Configuration)
参数视图配置列表 `parameterViewList` 包含以下 AI 模型参数：

| 参数名 | 字段名 | 范围 | 说明 |
|--------|--------|------|------|
| Top_p | topP | 0-1 (步长0.1) | 控制词的选择范围 |
| Temperature | temperature | 0-2 (步长0.1) | 采样温度 |
| MaxToken | maxToken | 0-∞ | 最大 Token 数 |
| Frequency_penalty | frequencyPenalty | -2-2 (步长0.1) | 惩罚频繁词 |
| Presence_penalty | presencePenalty | -2-2 (步长0.1) | 惩罚已出现词 |
| StopSequences | stopSequences | 字符串 | 终止词序列 |

### 1.4 Prompt 请求参数
```javascript
promptParamForm: {
  prefix: '',                   // 前缀
  suffix: '',                   // 后缀
  variableList: []              // 变量列表
}
```

### 1.5 输出结果管理
```javascript
{
  outputAverageDeci: -1,        // 平均分
  outputMaxDeci: -1,            // 最高分
  outputActive: '',             // 当前选中项
  outputList: [],               // 输出结果列表
  robotScoreLoadingMap: {},     // AI 评分加载状态
  chartData: [],                // 图表数据
  chartInstance: null,          // ECharts 实例
}
```

### 1.6 版本记录管理
```javascript
{
  versionDrawer: false,         // 版本抽屉显隐
  versionSearchVal: '',         // 版本搜索关键词
  versionTreeData: [],          // 版本树数据
  versionTreeProps: {
    children: 'children',
    label: 'label'
  }
}
```

### 1.7 战术模式 (Tactical Mode)
```javascript
tacticalForm: {
  tactics: '重新瞄准',          // 战术类型
  chatMode: '对话模式'          // 对话模式/直接测试
}

// 继续聊天相关
continueChatMode: false,        // 是否处于继续聊天模式
continueChatPromptResultId: null, // 继续聊天的 Result ID
continueChatHistory: [],        // 聊天历史记录
```

### 1.8 3D 导图可视化 (3D Map Visualization)
```javascript
{
  mapDialogVisible: false,      // 导图对话框显隐
  map3dScene: null,             // Three.js 场景
  map3dCamera: null,            // 相机
  map3dRenderer: null,          // 渲染器
  map3dControls: null,          // 相机控制器
  map3dNodes: [],               // 3D 节点数组
  map3dTreeData: null,          // 树状结构数据
  map3dNodeMap: new Map(),      // 节点映射
  map3dAnimationId: null,       // 动画 ID
}
```

### 1.9 表单管理
```javascript
// 靶场表单
fieldForm: { alias: '' }

// 模型表单
modelForm: {
  alias: "",                    // 别名
  modelType: "",                // 模型类型 (OpenAI/AzureOpenAI/NeuCharAI/HuggingFace/FastAPI)
  deploymentName: "",           // 部署名称
  apiVersion: "",               // API 版本
  apiKey: "",                   // API 密钥
  endpoint: "",                 // 端点
  organizationId: "",           // 组织 ID
}

// AI 评分标准表单
aiScoreForm: {
  resultList: [{
    id: 1,
    label: '预期结果',
    value: ''
  }]
}
```

### 1.10 UI 状态管理
```javascript
{
  promptLeftShow: false,        // 左侧区域显隐
  parameterViewShow: false,     // 参数设置显隐
  targetShootLoading: false,    // 打靶 Loading
  dodgersLoading: false,        // 连发 Loading
  
  // 区域宽度控制
  leftAreaWidth: 360,           // 左侧宽度
  centerAreaWidth: 380,         // 中间宽度
  isResizing: false,            // 是否正在拖动
  resizeType: null,             // 拖动类型: 'left' 或 'right'
  
  // 区域最大化
  centerAreaMaximized: false,   // 中间区域最大化
  rightAreaMaximized: false,    // 右侧区域最大化
  
  // Prompt 对比
  compareDialogVisible: false,  // 对比对话框
  comparePromptAId: null,       // 对比 Prompt A ID
  comparePromptBId: null,       // 对比 Prompt B ID
}
```

### 1.11 Plugin 管理
```javascript
{
  uploadPluginVisible: false,   // Plugin 上传对话框
  uploadPluginDropAreaVisible: true, // 上传区域显隐
  uploadPluginDropHover: false, // 拖拽悬停
  uploadPluginData: [],         // Plugin 文件列表
  jsZip: null,                  // JSZip 实例
  expectedPluginVisible: false, // Plugin 导出对话框
  expectedPluginFoem: {
    checkList: [],              // 选择的数据 tree
  },
}
```

---

## 🧮 计算属性 (computed)

### 2.1 `isPageLoading()`
综合判断页面是否处于加载状态，汇总以下 loading 状态：
- `tacticalFormSubmitLoading`
- `modelFormSubmitLoading`
- `aiScoreFormSubmitLoading`
- `targetShootLoading`
- `dodgersLoading`

### 2.2 `availablePrompts()`
返回可用的 Prompt 列表，用于 Prompt 对比对话框。

### 2.3 `comparePromptAInfo()` / `comparePromptBInfo()`
解析对比 Prompt 的显示信息，从 `fullVersion` 字段解析：
- 靶场名称
- 靶道名称
- 战术名称
- 模型名称

格式: `靶场-靶道-战术`

### 2.4 `isSamePrompt()`
判断对比的两个 Prompt 是否为同一个（ID 相同）。

### 2.5 `detectedVariables()`
从 `content` 字段中检测 `{{变量}}` 格式的变量。

---

## 👀 监听器 (watch)

### 3.1 `versionSearchVal(val)`
监听版本搜索输入，触发树节点过滤。

### 3.2 `content(newVal, oldVal)`
监听 Prompt 内容变化：
- 标记页面有变化 (`pageChange = true`)
- 使用防抖（300ms）延迟应用高亮

---

## 🛠️ 核心方法分类 (methods)

### 4.1 初始化与配置类 (20+ 方法)

#### URL 参数处理
- `getTargetRangeIdFromUrl()` - 从 URL 获取靶场 ID
- `setDefaultSelectedOption(targetrangeId)` - 设置默认选项

#### 数据初始化
- `resetConfigurineParam(isPageChange)` - 重置配置参数
- `resetInputPrompt()` - 重置 Prompt 输入
- `loadAreaWidthsFromStorage()` - 从 localStorage 加载区域宽度
- `saveAreaWidthsToStorage()` - 保存区域宽度到 localStorage

---

### 4.2 数据管理类 (CRUD) (30+ 方法)

#### 靶场管理
- `fieldFormSubmitBtn()` - 创建/更新靶场
- `fieldDeleteHandel(e, id)` - 删除靶场
- `renameField(e, item)` - 重命名靶场

#### Prompt 管理
- `promptDeleteHandel(e, id)` - 删除 Prompt
- `promptRemarkSave()` - 保存 Prompt 备注
- `promptNameRest(e, id)` - 重置 Prompt 名称
- `promptNameField(e, item)` - 更新 Prompt 名称
- `promptChangeHandel(val, itemKey, oldVal)` - Prompt 内容变化处理

#### 模型管理
- `modelFormSubmitBtn()` - 提交模型表单
- `deleteModel(item)` - 删除模型

#### Prompt 参数管理
- `addVariableBtn()` - 添加变量
- `deleteVariableBtn(index)` - 删除变量
- `promptParamFormSubmit()` - 提交 Prompt 参数

#### AI 评分标准管理
- `aiScoreFormSubmitBtn()` - 提交 AI 评分标准
- `aiScoreFormAddRow()` - 添加评分项
- `deleteAiScoreBtn(index)` - 删除评分项

---

### 4.3 核心业务逻辑类 (40+ 方法)

#### 打靶与连发
- `clickSendBtn()` - 发送按钮点击（打靶/连发/保存草稿）
- `tacticalFormSubmitBtn()` - 战术表单提交（核心打靶逻辑，~400 行）

#### 评分系统
- `addAlScoring(index)` - 添加 AI 评分
- `manualBtnScoring(index)` - 手动评分
- `cancelManualScore(index)` - 取消手动评分
- `showRatingView(index, scoreType)` - 显示评分视图
- `getFinalScore(item)` - 获取最终分数

#### 版本管理
- `seeVersionRecord()` - 查看版本记录
- `versionRecordEdit(itemData)` - 编辑版本记录
- `versionRecordDelete(itemData)` - 删除版本记录
- `versionRecordGenerateCode(itemData)` - 生成代码
- `versionRecordIsPublic(itemData)` - 设置版本公开状态

#### Prompt 对比
- `openCompareDialog(event, item)` - 打开对比对话框
- `swapComparePrompts()` - 交换对比 Prompt
- `getContentDiffHtml(side)` - 获取内容差异 HTML
- `getVariablesDiffHtml(side)` - 获取变量差异 HTML
- `renderDiffHtml(diff, side)` - 渲染差异 HTML
- `renderInlineDiff(oldText, newText, mode)` - 渲染行内差异

---

### 4.4 3D 可视化类 (30+ 方法)

#### 场景管理
- `openMapDialog()` - 打开 3D 导图对话框
- `initMap3D()` - 初始化 3D 场景（~110 行）
- `clearMap3DScene()` - 清空 3D 场景
- `destroyMap3D()` - 销毁 3D 场景
- `handleMap3DResize()` - 处理 3D 场景大小调整

#### 数据构建
- `buildTreeData()` - 构建树状数据（~100 行）
- `calculateTreeHeight(nodeData)` - 计算树高度
- `countTreeNodes(nodeData)` - 统计节点数
- `calculateScoreStatistics()` - 计算分数统计

#### 渲染与动画
- `renderTreeNodes()` - 渲染树节点（~1260 行，**最大方法**）
- `createConnectionLines()` - 创建连接线（~90 行）
- `updateConnectionLine(nodeData)` - 更新连接线
- `updateAllConnectionLines()` - 更新所有连接线
- `startNodeAnimations()` - 启动节点动画（~90 行）
- `animateMap3D()` - 3D 场景动画循环（~60 行）
- `animateNodesPopOut(parentNodeData, onComplete)` - 节点弹出动画（~230 行）
- `animateNodesSuckIn(parentNodeData, onComplete)` - 节点吸入动画（~100 行）
- `createGradientBackground()` - 创建渐变背景

---

### 4.5 Plugin 管理类 (15+ 方法)

#### 上传
- `enentPluginDrop(e)` - 处理 Plugin 拖放
- `enentPluginInput()` - 处理 Plugin 文件输入
- `getFileFromEntryRecursively(entry)` - 递归获取文件
- `submitUploadPlugins()` - 提交上传 Plugin
- `folderHandlesubmit(formData)` - 处理文件夹提交

#### 导出
- `btnExpectedPlugins()` - 导出 Plugin 按钮
- `exportPluginSelectAll()` - 全选导出项
- `exportPluginInvertSelection()` - 反选导出项
- `exportPluginClearAll()` - 清空导出选择
- `exportPluginToggleExpand()` - 切换展开/折叠
- `treeCheckChange(data, currentCheck, childrenCheck)` - 树节点选中变化

---

### 4.6 UI 交互类 (35+ 方法)

#### 区域控制
- `Amplification(boxClicked)` - 区域放大/缩小
- `foldsidebar()` - 折叠侧边栏
- `startResizeLeft(event)` - 开始调整左侧区域宽度
- `startResizeRight(event)` - 开始调整右侧区域宽度
- `handleResize(event)` - 处理区域拖动
- `stopResize()` - 停止拖动
- `resetAreaWidths(event)` - 重置区域宽度

#### 对话框管理
- `tacticalFormCloseDialog()` - 关闭战术对话框
- `promptParamFormClose()` - 关闭 Prompt 参数对话框
- `modelFormCloseDialog()` - 关闭模型对话框
- `fieldFormCloseDialog()` - 关闭靶场对话框
- `aiScoreFormCloseDialog()` - 关闭 AI 评分对话框
- `uploadPluginCloseDialog()` - 关闭上传 Plugin 对话框
- `expectedPluginCloseDialog()` - 关闭导出 Plugin 对话框
- `versionDrawerClose()` - 关闭版本记录抽屉
- `mapDialogClose()` - 关闭 3D 导图对话框

#### 输出管理
- `outputSelectSwitch(index)` - 切换输出选择
- `scrollToBtm()` - 滚动到底部
- `handleResultScroll(event)` - 处理结果滚动
- `scrollToResult(index)` - 滚动到指定结果
- `getThumbnailStyle(index)` - 获取缩略图样式
- `isResultInView(index)` - 判断结果是否在视口内
- `getViewportStyle()` - 获取视口样式
- `handleOutputAreaMouseMove(event)` - 处理鼠标移动（缩略图）
- `handleOutputAreaMouseLeave()` - 处理鼠标离开

#### 图表管理
- `chartInitialization()` - 初始化图表（~70 行）
- `formatTooltip(val)` - 格式化 Tooltip（~60 行）

#### 编辑器高亮
- `getEditorText()` - 获取编辑器文本
- `generateHighlightHTML(text)` - 生成高亮 HTML（~120 行）
- `applyHighlight()` - 应用高亮（~60 行）
- `applyHighlightWithCaretPos(expectedCaretPos)` - 应用高亮并保持光标位置
- `saveCaretPosition()` - 保存光标位置（~90 行）
- `restoreCaretPosition(offset)` - 恢复光标位置（~90 行）
- `handleEditorInput(e)` - 处理编辑器输入
- `handleKeyDown(e)` - 处理键盘事件
- `handlePaste(e)` - 处理粘贴事件（~120 行）
- `cleanupHighlightBrTags(editor)` - 清理高亮标签中的 `<br>`（~90 行）

---

### 4.7 工具方法类 (15+ 方法)

#### 数据转换
- `convertData(data)` - 转换数据（~60 行）
- `treeArrayFormat(data, child)` - 树数组格式化
- `hasFieldDiff(fieldA, fieldB)` - 判断字段是否有差异
- `formatVariables(variablesJson)` - 格式化变量

#### 格式化
- `formatDate(d)` - 格式化日期
- `formatChatTime(d)` - 格式化聊天时间
- `formatChatContent(content)` - 格式化聊天内容（~100 行）
- `formatTime(timeStr)` - 格式化时间字符串
- `scoreFormatter(score)` - 格式化分数（全局函数）

#### 辅助方法
- `copyInfo(source)` - 复制信息（~40 行）
- `copyPromptResult(item, rawResult)` - 复制 Prompt 结果
- `escapeHtml(text)` - HTML 转义
- `escapeRegex(str)` - 正则转义
- `getUuid()` - 生成 UUID（全局函数）
- `beforeunloadHandler(e)` - 页面卸载前处理（~30 行）

#### 查询方法
- `getTargetRangeName(id)` - 获取靶场名称
- `getTargetLaneName(id)` - 获取靶道名称
- `getTacticalName(id)` - 获取战术名称
- `getModelName(id)` - 获取模型名称
- `querySearch(queryString, cb)` - 自动完成搜索
- `createFilter(queryString)` - 创建过滤器

#### 样式与 UI
- `getScoreBarClass(item)` - 获取分数条 class
- `getScoreBarStyle(item)` - 获取分数条 style
- `getThumbnailTooltip(item)` - 获取缩略图 Tooltip
- `hasScore(score)` - 判断是否有分数
- `checkUseRed(index, item, which)` - 检查是否使用红色（~30 行）

---

## 🔍 代码特征分析

### 5.1 代码规模统计
| 维度 | 数量 |
|------|------|
| 总行数 | 7,639 |
| 数据属性 | ~80 个 |
| 计算属性 | 5 个 |
| 监听器 | 2 个 |
| 方法数量 | 162 个 |
| 最大方法 | `renderTreeNodes()` (~1260 行) |
| 全局函数 | 2 个 (`scoreFormatter`, `getUuid`) |

### 5.2 复杂度热点

#### 🔥 超大型方法（>500 行）
1. **`renderTreeNodes()`** - 1260 行
   - 渲染 3D 树节点
   - 包含大量 Three.js 操作
   - 事件处理逻辑复杂

2. **`tacticalFormSubmitBtn()`** - ~400 行
   - 核心打靶逻辑
   - 处理多种战术模式
   - 包含大量 API 调用和状态管理

#### 🔶 大型方法（200-500 行）
3. **`buildTreeData()`** - ~100 行
4. **`animateNodesPopOut()`** - ~230 行
5. **`chartInitialization()`** - ~70 行（但复杂度高）

#### 🔷 中型方法（100-200 行）
- `initMap3D()` - ~110 行
- `generateHighlightHTML()` - ~120 行
- `handlePaste()` - ~120 行
- `formatChatContent()` - ~100 行

### 5.3 代码重复模式识别

#### 🔁 模式 1: 对话框关闭逻辑
```javascript
// 重复出现的模式
xxxFormCloseDialog() {
    this.xxxFormVisible = false
}
```
**重复次数**: 7+ 处

#### 🔁 模式 2: Name 查询方法
```javascript
// 重复出现的模式
getXxxName(id) {
    if (!this.xxxOpt || !id) return '未知Xxx';
    const item = this.xxxOpt.find(item => item.value === id);
    return item ? item.label : '未知Xxx';
}
```
**重复次数**: 4 处
- `getTargetRangeName`
- `getTargetLaneName`
- `getTacticalName`
- `getModelName`

#### 🔁 模式 3: Loading 状态管理
```javascript
// 重复模式
this.xxxLoading = true
try {
    // 业务逻辑
} finally {
    this.xxxLoading = false
}
```
**重复次数**: 15+ 处

#### 🔁 模式 4: 消息提示
```javascript
// 重复模式
this.$message({
    message: 'xxx',
    type: 'success/error/warning'
});
```
**重复次数**: 50+ 处

#### 🔁 模式 5: API 调用模式
```javascript
// 重复模式
$.ajax({
    url: this.devHost + '/api/xxx',
    type: 'POST/GET',
    data: {...},
    success: (res) => {
        if (res.success) {
            // 成功处理
            this.$message.success(res.msg)
        } else {
            this.$message.error(res.msg)
        }
    },
    error: (err) => {
        this.$message.error('请求失败')
    }
})
```
**重复次数**: 30+ 处

#### 🔁 模式 6: 光标位置保存/恢复
```javascript
// saveCaretPosition() 和 restoreCaretPosition() 
// 包含大量相似的 DOM 操作逻辑
```
**复杂度**: 高，~180 行重复逻辑

#### 🔁 模式 7: 3D 节点动画模式
```javascript
// animateNodesPopOut 和 animateNodesSuckIn
// 包含相似的 GSAP 动画逻辑
```
**复杂度**: 高，~330 行重复逻辑

---

## 🎨 技术栈与依赖

### 核心库
- **Vue.js 2.x** - MVVM 框架
- **Element UI** - UI 组件库
- **jQuery** - DOM 操作和 AJAX
- **ECharts** - 数据可视化
- **Three.js** - 3D 渲染
- **GSAP** - 动画库
- **JSZip** - ZIP 文件处理
- **diff** - 文本差异对比

### API 集成
- AI 模型 API（OpenAI、Azure OpenAI、NeuCharAI、HuggingFace、FastAPI）
- 后端 RESTful API

---

## 📐 架构设计特点

### 优点
✅ **功能丰富**: 覆盖 Prompt 测试的完整工作流  
✅ **交互复杂**: 3D 可视化、拖拽、实时高亮等高级交互  
✅ **状态管理清晰**: 数据模型设计合理  
✅ **可扩展性**: 支持多种 AI 模型和插件系统  

### 缺点
❌ **单文件过大**: 7,639 行代码难以维护  
❌ **方法过长**: 多个方法超过 500 行  
❌ **代码重复**: 存在大量重复模式  
❌ **缺乏模块化**: 所有逻辑集中在一个 Vue 实例  
❌ **耦合度高**: UI 逻辑、业务逻辑、数据管理混杂  
❌ **缺乏注释**: 复杂逻辑缺少必要的说明  

---

## 🔧 重构建议优先级

### 🔴 高优先级（立即执行）

#### 1. 提取公共工具方法
- 创建 `utils/` 目录
- 提取以下工具类：
  - `apiHelper.js` - 统一 API 调用
  - `messageHelper.js` - 统一消息提示
  - `nameHelper.js` - 统一 Name 查询
  - `dateHelper.js` - 日期格式化
  - `copyHelper.js` - 复制功能
  - `validationHelper.js` - 表单验证

**预期减少**: ~500 行

#### 2. 拆分超大方法
- `renderTreeNodes()` 拆分为：
  - `createNodeMesh()` - 创建节点网格
  - `createNodeLabel()` - 创建节点标签
  - `bindNodeEvents()` - 绑定节点事件
  - `updateNodePosition()` - 更新节点位置

**预期减少**: ~800 行

#### 3. 抽取 3D 可视化模块
- 创建独立的 `Map3DManager` 类
- 将所有 `map3d*` 相关方法移入该类
- 通过依赖注入方式与 Vue 实例通信

**预期减少**: ~2000 行

### 🟡 中优先级（近期执行）

#### 4. 业务逻辑模块化
- 创建 `services/` 目录：
  - `PromptService.js` - Prompt 管理
  - `ModelService.js` - 模型管理
  - `FieldService.js` - 靶场管理
  - `VersionService.js` - 版本管理
  - `PluginService.js` - Plugin 管理

**预期减少**: ~1500 行

#### 5. 状态管理优化
- 引入 Vuex 进行状态管理
- 将 `data` 中的复杂状态迁移到 Vuex Store
- 使用 modules 按功能分模块

**预期减少**: ~200 行（逻辑更清晰）

#### 6. 表单管理统一
- 创建 `FormManager` 类
- 统一管理所有表单的：
  - 显隐状态
  - 加载状态
  - 关闭逻辑
  - 提交逻辑

**预期减少**: ~300 行

### 🟢 低优先级（长期优化）

#### 7. 组件化拆分
- 拆分为独立的 Vue 组件：
  - `PromptEditor.vue` - Prompt 编辑器
  - `OutputList.vue` - 输出列表
  - `VersionTree.vue` - 版本树
  - `Map3DViewer.vue` - 3D 查看器
  - `PluginManager.vue` - Plugin 管理器

**预期减少**: ~1000 行（主文件）

#### 8. TypeScript 迁移
- 逐步迁移到 TypeScript
- 增强类型安全

#### 9. 性能优化
- 虚拟滚动优化大列表
- 防抖/节流优化高频操作
- Webpack 代码分割

---

## 📊 重构效果预估

| 重构项 | 行数减少 | 可维护性提升 | 性能提升 |
|--------|----------|--------------|----------|
| 提取工具方法 | ~500 | ⭐⭐⭐ | ⭐ |
| 拆分超大方法 | ~800 | ⭐⭐⭐⭐ | ⭐⭐ |
| 3D 模块抽取 | ~2000 | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| 业务逻辑模块化 | ~1500 | ⭐⭐⭐⭐ | ⭐ |
| 状态管理优化 | ~200 | ⭐⭐⭐⭐ | ⭐⭐ |
| 表单管理统一 | ~300 | ⭐⭐⭐ | ⭐ |
| 组件化拆分 | ~1000 | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **总计** | **~6300** | - | - |

**重构后预期主文件行数**: ~1300 行

---

## 🎯 关键业务流程

### 打靶流程
```
用户输入 Prompt
    ↓
选择靶场/靶道/模型
    ↓
配置参数 (Top_p, Temperature, etc.)
    ↓
点击"打靶"按钮
    ↓
弹出战术选择对话框
    ↓
选择战术 (重新瞄准/继续聊天/等)
    ↓
调用 AI 模型 API
    ↓
接收结果并显示
    ↓
触发 AI 评分 (可选)
    ↓
显示分数和图表
    ↓
保存版本记录
```

### 连发流程
```
配置连发次数 (1-10)
    ↓
点击"连发"按钮
    ↓
循环调用打靶逻辑
    ↓
并行/串行执行多次
    ↓
收集所有结果
    ↓
计算平均分/最高分
    ↓
生成对比图表
```

### 3D 可视化流程
```
点击"查看导图"
    ↓
初始化 Three.js 场景
    ↓
构建树状数据结构
    ↓
渲染节点和连接线
    ↓
应用弹出动画
    ↓
绑定交互事件 (点击/悬停)
    ↓
支持展开/折叠节点
    ↓
实时更新动画
```

---

## 🔐 安全性考虑

### 当前实现
- API Key 存储在前端表单中
- 使用明文传输（依赖 HTTPS）
- 无明显的输入验证

### 建议改进
1. API Key 应该在后端管理，前端不直接暴露
2. 增加输入验证和 XSS 防护
3. 实现请求签名机制
4. 添加速率限制

---

## 📝 总结

这是一个功能强大但代码规模庞大的 Vue.js 应用，主要用于 AI Prompt 测试和可视化。代码质量尚可，但存在明显的可维护性问题：

1. **单文件过大** (7,639 行) - 急需模块化拆分
2. **方法过长** (最长 1,260 行) - 需要重构
3. **代码重复** - 存在大量相似模式
4. **耦合度高** - UI、业务、数据逻辑混杂

**建议优先执行高优先级重构**，重点是：
- 提取公共工具方法
- 拆分超大方法
- 抽取 3D 可视化模块

通过系统性重构，预期可将主文件从 7,639 行减少到 ~1,300 行，大幅提升可维护性和开发效率。

---

**文档生成日期**: 2025-12-15  
**分析工具**: Claude AI (Sonnet 4.5)

