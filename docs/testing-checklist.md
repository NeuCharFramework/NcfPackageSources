# 工具类集成测试清单

## ✅ 当前测试状态

**日期**: 2025-12-15  
**分支**: `refactor/prompt-js-modularization`  
**测试环境**: 浏览器开发者工具

---

## 🔍 测试项目

### 1. 工具类加载测试

#### 1.1 全局命名空间检查
**在浏览器控制台执行**:
```javascript
// 检查全局命名空间是否存在
console.log('PromptRangeUtils:', window.PromptRangeUtils);

// 应该输出：
// {
//   HtmlHelper: {...},
//   DateHelper: {...},
//   NameHelper: {...},
//   StorageHelper: {...},
//   CopyHelper: {...}
// }
```

**预期结果**: ✅ 所有工具类都应该存在

#### 1.2 各工具类功能检查
```javascript
// HtmlHelper
console.log('UUID:', window.PromptRangeUtils.HtmlHelper.generateUUID());

// DateHelper
console.log('Date:', window.PromptRangeUtils.DateHelper.formatDate(new Date()));

// NameHelper
console.log('NameHelper:', typeof window.PromptRangeUtils.NameHelper.getName);

// StorageHelper
window.PromptRangeUtils.StorageHelper.set('test', 'value');
console.log('Storage:', window.PromptRangeUtils.StorageHelper.get('test'));

// CopyHelper
console.log('CopyHelper:', typeof window.PromptRangeUtils.CopyHelper.copyText);
```

**预期结果**: ✅ 所有方法都应该正常工作

---

### 2. Name 查询方法测试

#### 2.1 测试 getTargetRangeName
```javascript
// 选择一个靶场，然后查看其名称
app.getTargetRangeName(app.promptField);
```

**预期结果**: ✅ 返回正确的靶场名称（不是 '未知靶场'）

#### 2.2 测试 getTargetLaneName
```javascript
// 选择一个靶道，然后查看其名称
app.getTargetLaneName(app.promptid);
```

**预期结果**: ✅ 返回正确的靶道名称（可能包含 nickName）

#### 2.3 测试 getModelName
```javascript
// 选择一个模型，然后查看其名称
app.getModelName(app.modelid);
```

**预期结果**: ✅ 返回正确的模型名称

---

### 3. 页面功能测试

#### 3.1 选择靶场
- [ ] 打开页面
- [ ] 点击"选择靶场"下拉框
- [ ] 选择一个靶场
- [ ] 观察是否有错误

**预期结果**: ✅ 无错误，靶场正常切换

#### 3.2 选择靶道
- [ ] 在已选择靶场的情况下
- [ ] 点击"选择靶道"下拉框
- [ ] 选择一个靶道
- [ ] 观察靶道名称是否正确显示

**预期结果**: ✅ 无错误，靶道信息正确显示

#### 3.3 选择模型
- [ ] 点击"选择模型"下拉框
- [ ] 选择一个模型
- [ ] 观察模型名称是否正确

**预期结果**: ✅ 无错误，模型名称正确

#### 3.4 打开对比对话框
- [ ] 点击靶道选项中的"对比"按钮
- [ ] 对话框应该正常打开
- [ ] 观察是否有 `resetField` 错误

**预期结果**: ✅ 无错误，对话框正常打开

---

### 4. API 请求测试

#### 4.1 检查 servicePR 是否可用
```javascript
console.log('servicePR:', window.servicePR);
console.log('axios:', window.axios);
```

**预期结果**: ✅ servicePR 应该存在且为 axios 实例

#### 4.2 测试一个简单的请求
在操作页面时（如删除、新增），观察：
- [ ] 请求是否正常发送
- [ ] 错误消息是否正确显示
- [ ] 成功消息是否正确显示

**预期结果**: ✅ API 请求正常，消息提示正常

---

### 5. 控制台错误检查

#### 5.1 页面加载时
**检查项**:
- [ ] 无 "ApiHelper requires jQuery" 错误
- [ ] 无其他 JavaScript 错误
- [ ] 所有脚本正常加载

**预期结果**: ✅ 无错误

#### 5.2 操作过程中
**操作**:
- [ ] 切换靶场
- [ ] 切换靶道
- [ ] 切换模型
- [ ] 打开/关闭对话框
- [ ] 输入 Prompt

**预期结果**: ✅ 无错误（Element UI 的 resetField 警告可忽略）

---

## 🐛 已知问题

### 1. Element UI resetField 错误
**错误信息**:
```
TypeError: Cannot read properties of undefined (reading 'indexOf')
    at a.resetField (element.js:1:369631)
```

**原因**: 
- Element UI 的已知问题
- 通常在表单字段定义变化时出现
- 不影响功能

**解决方案**: 
- 可以忽略（不影响使用）
- 或在关闭对话框时不调用 `resetFields()`

**优先级**: 低（不影响核心功能）

---

## 📊 测试结果记录

### 测试环境
- **浏览器**: _____________
- **操作系统**: _____________
- **测试时间**: _____________

### 测试结果

| 测试项 | 状态 | 备注 |
|--------|------|------|
| 工具类加载 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| Name 查询方法 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| 靶场切换 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| 靶道切换 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| 模型切换 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| 对话框操作 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| API 请求 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |
| 控制台无错误 | ⬜ 未测试 / ✅ 通过 / ❌ 失败 | |

### 发现的问题

1. **问题描述**: ___________________________
   - **严重程度**: 低 / 中 / 高 / 阻断
   - **复现步骤**: ___________________________
   - **错误信息**: ___________________________

2. **问题描述**: ___________________________
   - **严重程度**: 低 / 中 / 高 / 阻断
   - **复现步骤**: ___________________________
   - **错误信息**: ___________________________

---

## 🎯 下一步行动

### 如果测试通过
- [ ] 继续阶段二的其他工作（日期、复制、Storage）
- [ ] 更新文档
- [ ] 提交代码

### 如果发现问题
- [ ] 记录问题详情
- [ ] 分析问题原因
- [ ] 修复问题
- [ ] 重新测试

---

## 📝 测试建议

### 推荐的测试顺序
1. **先测基础** - 工具类加载和全局命名空间
2. **再测集成** - Name 查询方法
3. **最后测功能** - 完整的页面操作流程

### 测试技巧
1. **打开开发者工具** - F12 或右键"检查"
2. **查看控制台** - 观察是否有错误
3. **查看网络** - 观察 API 请求是否正常
4. **使用断点** - 在关键方法处设置断点调试

---

**测试清单版本**: 1.0  
**最后更新**: 2025-12-15

