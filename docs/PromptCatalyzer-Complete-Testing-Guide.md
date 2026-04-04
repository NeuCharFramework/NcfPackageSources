# PromptCatalyzer - A Complete Guide to Functional Testing

## 🎯 Contents of this update

According to user reminders, the following functions of PromptCatalyzer have been improved:

### ✅ Function completed
1. **ChatGroup automatically created**: created during initialization`PromptCatalyzer-OptimizationGroup`
2. **Role Settings**: Admin and Enter are both set to the same Agent (PromptCatalyzer)
3. **ChatTask Record**: Create a task record for each optimization for easy tracking
4. **Real AI Optimization**: Call Semantic Kernel for Prompt optimization (not simulation)
5. **AI generated mark**: All AI generated prompts are marked in the Note field`🤖AI-Generated`
6. **Parameter optimization**: AI will optimize parameters such as Temperature, TopP, and MaxTokens according to user needs
7. **Predicted score**: AI will predict the optimized performance score (0-1 range)

### 📝 Add/modify files
1. **New**:`src/Extensions/Senparc.Xncf.AgentsManager/Application/EventHandlers/PromptOptimizationChatTaskHandler.cs`
- Contains 2 EventHandlers
2. **Refactor**:`src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`
- Changed from simulation optimization to real AI optimization
3. **Improvement**:`src/Extensions/Senparc.Xncf.AgentsManager/Domain/Services/PromptOptimizationService.cs`
- Add ChatGroup creation logic (step 4/4)
4. **New**:`docs/PromptCatalyzer-ChatGroup-Integration.md`
- Complete architecture and process documentation

---

## 🧪 Preparation before test

### 1. Clean old data (recommended)

If PromptCatalyzer related data has been created before, it is recommended to clean it first:

```sql
-- 清理旧的 ChatTask
DELETE FROM Senparc_AgentsManager_ChatTask 
WHERE ChatGroupId IN (
    SELECT Id FROM Senparc_AgentsManager_ChatGroup 
    WHERE Name = 'PromptCatalyzer-OptimizationGroup'
);

-- 清理旧的 ChatGroup
DELETE FROM Senparc_AgentsManager_ChatGroup 
WHERE Name = 'PromptCatalyzer-OptimizationGroup';

-- 清理旧的 Agent
DELETE FROM Senparc_AgentsManager_AgentTemplate 
WHERE Name = 'PromptCatalyzer';

-- 清理旧的 PromptItem
DELETE FROM Senparc_PromptRange_PromptItem 
WHERE RangeId IN (
    SELECT Id FROM Senparc_PromptRange_PromptRange 
    WHERE NickName = 'PromptCatalyzer'
);

-- 清理旧的 PromptRange
DELETE FROM Senparc_PromptRange_PromptRange 
WHERE NickName = 'PromptCatalyzer';
```

### 2. Restart the application

```bash
# 停止当前应用（Ctrl+C 或 Command+C）
# 重新启动
cd tools/NcfSimulatedSite/Senparc.Web
dotnet run
```

### 3. Force refresh the browser

- **Windows**: `Ctrl + Shift + R`or`Ctrl + F5`
- **Mac**: `Command + Shift + R`or`Command + Option + R`

---

## 📋 Complete testing process

### Part One: Initialization Test

#### Step 1: Open the PromptRange page
1. Visit:`http://localhost:5000/Admin/PromptRange/Prompt`
2. Confirm that the page loads normally and there are no JavaScript errors.

#### Step 2: Select a Prompt
1. Select any prompt in the prompt list
2. Confirm that the details panel on the right is displayed normally

#### Step 3: Trigger initialization
1. Click the "Optimize" button
2. **Expected results**: Pop-up initialization dialog box
- Title: "Initializing PromptCatalyzer"
- Display: drop-down box for selecting AI Model
- Display: Number of Models (for example: "Found 17 available AI Models")

#### Step 4: Select Model and initialize
1. Select a Chat type Model in the drop-down box
2. Click the "Start Initialization" button
3. **Expected results**: Display Loading status: "Initializing, please wait..."

#### Step 5: Observe the console log
In the backend console, you should see the following logs (in order):

```
========== EnsureInitializedAsync 开始 ==========
请求的 ModelId: 1
【步骤1/4】检查 PromptCatalyzer Agent 是否已存在...
【步骤2/4】Agent 不存在，开始初始化流程...
  发布 PromptInitRequestEvent: RequestId=xxxx, ModelId=1
  等待 PromptInitResponseEvent（最长2分钟）...

========== PromptInitRequestHandler 开始 ==========
【步骤1/4】检查 PromptRange 是否存在...
  ✅ PromptRange 已存在: RangeId=xxx, NickName=PromptCatalyzer
【步骤2/4】确定 AI Model...
  使用用户指定的 Model ID: 1
  ✅ Model 验证通过: gpt-4o (ID: 1)
【步骤3/4】检查 PromptItem 是否存在于 Range xxx...
  PromptItem 不存在，开始创建...
【步骤4/4】创建 PromptItem...
  ✅ PromptItem 创建成功: ItemId=xxx, PromptCode=2026.03.24.1-T1-A1

  ✅ 收到 PromptInitResponseEvent: Success=True, PromptCode=2026.03.24.1-T1-A1
【步骤3/4】创建 PromptCatalyzer Agent...
  ✅ Agent 创建成功！AgentId: xxx, PromptCode: 2026.03.24.1-T1-A1
【步骤4/4】创建 ChatGroup...
  ✅ ChatGroup 创建成功！GroupId: xxx, Name: PromptCatalyzer-OptimizationGroup
========== EnsureInitializedAsync 完成 ==========
```

#### Step 6: Verify front-end
1. Loading status disappears
2. A success message pops up: "✅ Initialization successful!"
3. The dialog box closes automatically

#### Step 7: Verify database

```sql
-- 1. 检查 PromptRange 是否创建
SELECT * FROM Senparc_PromptRange_PromptRange 
WHERE NickName = 'PromptCatalyzer';

-- 2. 检查 PromptItem 是否创建（应该有 Note='AI-Catalyzer'）
SELECT Id, NickName, FullVersion AS PromptCode, Note, AddTime 
FROM Senparc_PromptRange_PromptItem 
WHERE RangeId = (SELECT Id FROM Senparc_PromptRange_PromptRange WHERE NickName = 'PromptCatalyzer');

-- 3. 检查 Agent 是否创建
SELECT Id, Name, SystemMessage AS PromptCode, Enable, Description 
FROM Senparc_AgentsManager_AgentTemplate 
WHERE Name = 'PromptCatalyzer';

-- 4. 检查 ChatGroup 是否创建（重点验证 Admin 和 Enter 是同一个 Agent）
SELECT 
    Id, 
    Name, 
    Enable, 
    State,
    AdminAgentTemplateId, 
    EnterAgentTemplateId,
    CASE 
        WHEN AdminAgentTemplateId = EnterAgentTemplateId THEN '✅ 相同 Agent'
        ELSE '❌ 不同 Agent'
    END AS RoleCheck
FROM Senparc_AgentsManager_ChatGroup 
WHERE Name = 'PromptCatalyzer-OptimizationGroup';
```

**Expected results**:
- PromptRange: 1 record
- PromptItem: 1 record (Note='AI-Catalyzer')
-AgentTemplate: 1 record
- ChatGroup: 1 record,`AdminAgentTemplateId = EnterAgentTemplateId`

---

### Part 2: Optimizing Functional Testing

#### Step 1: Click "Optimize" again
1. Select any prompt on the PromptRange page
2. Click the "Optimize" button
3. **Expected results**: Open the optimization dialog box directly (the initialization dialog box will no longer be displayed)
- Title: "Optimization Prompt"
- Display: current Prompt information
- Display: Input box allows users to enter optimization requirements

#### Step 2: Enter optimization requirements
Enter your requirements in the input box, for example:
```
让这个 Prompt 更加专业和详细，增强逻辑性和清晰度
```

#### Step 3: Start optimizing
1. Click the "Start Optimization" button
2. **Expected results**: Display Loading status: "Optimizing, please wait..."

#### Step 4: Observe the console log (key!)

**[Parallel Log 1] PromptOptimizationChatTaskHandler**:
```
========== PromptOptimizationChatTaskHandler 开始 ==========
  RequestId: xxxx
【步骤1/3】查找 PromptCatalyzer ChatGroup...
  ✅ 找到 ChatGroup: 1, Name: PromptCatalyzer-OptimizationGroup
【步骤2/3】获取 Agent 信息...
  ✅ Agent 信息：AgentId=1, AIModelId=1
【步骤3/3】创建 ChatTask...
  ✅ ChatTask 创建成功！TaskId: xxx, Name: Prompt优化-2026.03.24.1-T1-A1
========== PromptOptimizationChatTaskHandler 完成 ==========
```

**[Parallel Log 2] PromptOptimizationRequestHandler**:
```
========== PromptOptimizationRequestHandler 开始 ==========
  RequestId: xxxx
  Target PromptCode: 2026.03.24.1-T1-A1
  UserRequirement: 让这个 Prompt 更加专业和详细，增强逻辑性和清晰度
【步骤1/5】获取原始 PromptItem...
  ✅ 找到 PromptItem: xxx, Content Length: 500
【步骤2/5】调用 AI 进行 Prompt 优化...
  调用 AI Kernel 进行优化...
  ✅ AI 返回结果（前200字符）: {"optimizedContent": "...
【步骤3/5】解析 AI 优化结果...
  ✅ 解析完成：Score=0.92, Temperature=0.7, TopP=0.85
【步骤4/5】创建新版本 PromptItem...
  ✅ 新 PromptItem 创建成功！NewPromptCode: 2026.03.24.1-T1-A2
【步骤5/5】发布优化响应...
========== PromptOptimizationRequestHandler 完成 ==========
```

**[Follow-up log] PromptOptimizationTaskCompletionHandler**:
```
========== PromptOptimizationTaskCompletionHandler 开始 ==========
  RequestId: xxxx, Success: True
  找到对应的 ChatTask: xxx, Name: Prompt优化-2026.03.24.1-T1-A1
  ✅ ChatTask 状态已更新: Finished
========== PromptOptimizationTaskCompletionHandler 完成 ==========
```

#### Step 5: Verify front-end display
After Loading disappears, it should show:

```
✅ 优化成功！

📊 预测评分: 0.92
📝 新 Prompt 版本: 2026.03.24.1-T1-A2

📋 优化后的参数:
  • Temperature: 0.7 → 0.7
  • TopP: 0.9 → 0.85
  • MaxTokens: 2000 → 2000

💡 优化说明: [AI 返回的优化理由]
```

#### Step 6: Verify Prompt list refresh
1. The dialog box closes automatically
2. Prompt list automatically refreshed
3. The newly created Prompt should be automatically selected
4. **Note**: The Note for the new Prompt should be displayed`🤖AI-Generated`mark

#### Step 7: Verify database

```sql
-- 1. 检查 ChatTask 是否创建
SELECT 
    CT.Id, 
    CT.Name, 
    CT.Status, 
    CT.PromptCommand AS UserRequirement, 
    CT.AddTime, 
    CT.StartTime, 
    CT.EndTime,
    CG.Name AS GroupName,
    DATEDIFF(second, CT.StartTime, CT.EndTime) AS DurationSeconds
FROM Senparc_AgentsManager_ChatTask CT
JOIN Senparc_AgentsManager_ChatGroup CG ON CT.ChatGroupId = CG.Id
WHERE CG.Name = 'PromptCatalyzer-OptimizationGroup'
ORDER BY CT.AddTime DESC;

-- 预期结果：
-- - Name: Prompt优化-[PromptCode]
-- - Status: Finished (3)
-- - PromptCommand: 用户输入的优化需求

-- 2. 检查新创建的 PromptItem（AI 生成标记）
SELECT 
    PI.Id,
    PI.NickName,
    PI.FullVersion AS PromptCode,
    PI.Note,
    PI.Temperature,
    PI.TopP,
    PI.MaxToken AS MaxTokens,
    PI.FrequencyPenalty,
    PI.PresencePenalty,
    PI.IsDraft,
    PI.AddTime,
    SUBSTRING(PI.Content, 1, 100) AS ContentPreview
FROM Senparc_PromptRange_PromptItem PI
WHERE PI.Note = '🤖AI-Generated'
ORDER BY PI.AddTime DESC;

-- 预期结果：
-- - Note: 🤖AI-Generated
-- - IsDraft: 0 (false)
-- - Content: AI 优化后的内容
-- - Temperature/TopP/MaxToken: AI 建议的新参数

-- 3. 检查 PromptRange 的版本树
SELECT 
    Id,
    NickName,
    FullVersion AS PromptCode,
    Note,
    Temperature,
    TopP,
    AddTime
FROM Senparc_PromptRange_PromptItem
WHERE RangeId = (SELECT Id FROM Senparc_PromptRange_PromptRange WHERE NickName = 'PromptCatalyzer')
ORDER BY AddTime ASC;

-- 预期结果：应该有多个版本，最新的版本带有 🤖AI-Generated 标记
```

---

## 🔍 Detailed verification items

### A. Initialization process verification

#### ✅ Agent created correctly
```sql
SELECT 
    Id, 
    Name, 
    Enable,
    SystemMessage AS StoredPromptCode,
    PromptCode,
    Description,
    FunctionCallNames,
    AddTime
FROM Senparc_AgentsManager_AgentTemplate 
WHERE Name = 'PromptCatalyzer';
```

**expected**:
- `Enable`: `true`
- `SystemMessage`: equal to`PromptCode`(For example "2026.03.24.1-T1-A1")
- `Description`: "AI Agent that automatically optimizes Prompt content and parameters (Temperature, etc.)"
- `FunctionCallNames`: `NULL`(function-calling is not currently used)

#### ✅ ChatGroup role is set correctly
```sql
SELECT 
    CG.Id, 
    CG.Name, 
    CG.AdminAgentTemplateId, 
    CG.EnterAgentTemplateId,
    AT1.Name AS AdminAgentName,
    AT2.Name AS EnterAgentName,
    CASE 
        WHEN CG.AdminAgentTemplateId = CG.EnterAgentTemplateId THEN '✅ 相同'
        ELSE '❌ 不同'
    END AS RoleCheck
FROM Senparc_AgentsManager_ChatGroup CG
LEFT JOIN Senparc_AgentsManager_AgentTemplate AT1 ON CG.AdminAgentTemplateId = AT1.Id
LEFT JOIN Senparc_AgentsManager_AgentTemplate AT2 ON CG.EnterAgentTemplateId = AT2.Id
WHERE CG.Name = 'PromptCatalyzer-OptimizationGroup';
```

**expected**:
- `AdminAgentTemplateId` = `EnterAgentTemplateId`
- `AdminAgentName` = `EnterAgentName` = "PromptCatalyzer"
- `RoleCheck`: "✅ Same"

### B. Optimize process verification

#### ✅ ChatTask created and completed correctly
```sql
SELECT 
    CT.Id, 
    CT.Name, 
    CT.ChatGroupId,
    CT.AiModelId,
    CT.Status,
    CT.PromptCommand,
    CT.Description,
    CT.AddTime,
    CT.StartTime,
    CT.EndTime,
    CASE CT.Status
        WHEN 0 THEN 'Waiting'
        WHEN 1 THEN 'Chatting'
        WHEN 2 THEN 'Paused'
        WHEN 3 THEN 'Finished'
        WHEN 4 THEN 'Cancelled'
    END AS StatusName
FROM Senparc_AgentsManager_ChatTask CT
WHERE CT.Name LIKE 'Prompt优化-%'
ORDER BY CT.AddTime DESC;
```

**expected**:
- `Status`: `3` (Finished)
- `PromptCommand`: Optimization requirements input by users
- `ChatGroupId`: points to PromptCatalyzer ChatGroup

#### ✅ The Prompt tag generated by AI is correct
```sql
SELECT 
    PI.Id,
    PI.FullVersion AS PromptCode,
    PI.Note,
    PI.Temperature,
    PI.TopP,
    PI.MaxToken,
    PI.IsDraft,
    PI.AddTime,
    LEN(PI.Content) AS ContentLength
FROM Senparc_PromptRange_PromptItem PI
WHERE PI.Note = '🤖AI-Generated'
ORDER BY PI.AddTime DESC;
```

**expected**:
- `Note`: `🤖AI-Generated`
- `IsDraft`: `0` (false)
- Parameters (Temperature, TopP, MaxToken) may be different from the original version

#### ✅ Version tree integrity
```sql
-- 查看 PromptCatalyzer Range 的完整版本树
SELECT 
    PI.Id,
    PI.FullVersion AS PromptCode,
    PI.Note,
    PI.Temperature,
    PI.TopP,
    PI.IsDraft,
    PI.AddTime,
    CASE 
        WHEN PI.Note = '🤖AI-Generated' THEN '🤖 AI 生成'
        WHEN PI.Note = 'AI-Catalyzer' THEN '⚙️ 系统初始化'
        ELSE '👤 手动创建'
    END AS Source
FROM Senparc_PromptRange_PromptItem PI
WHERE PI.RangeId = (SELECT Id FROM Senparc_PromptRange_PromptRange WHERE NickName = 'PromptCatalyzer')
ORDER BY PI.AddTime ASC;
```

**expected**:
- First version: Note='AI-Catalyzer' (created during initialization)
- Subsequent versions: Note='🤖AI-Generated' (AI optimized generation)

---

## 🎬 Complete test scenario

### Scenario 1: First use (initialization + optimization)
1. ✅ Open the PromptRange page
2. ✅ Choose a Prompt
3. ✅ Click "Optimize" → Display initialization dialog box
4. ✅ Select Model → Click "Start Initialization" → Initialization successful
5. ✅ Click "Optimize" again → directly open the optimization dialog box
6. ✅ Enter the requirements → click "Start Optimization" → Optimization successful
7. ✅ Verify database: ChatGroup, ChatTask, and new PromptItem are all correct

### Scenario 2: Multiple optimization (iteration)
1. ✅ Choose a Prompt
2. ✅ Click "Optimize" → Enter requirements → Optimization successful
3. ✅ Select the newly generated Prompt (marked with 🤖)
4. ✅ Click "Optimize" again → Enter new requirements → Optimization successful
5. ✅ Verification database: multiple ChatTask records, multiple AI-generated PromptItems

### Scenario 3: Optimization of different prompts
1. ✅ Select Prompt A → Optimize → Success
2. ✅ Select Prompt B → Optimize → Success
3. ✅ Authentication database: two different ChatTasks, two different new PromptItems

---

## 🚨 FAQ Troubleshooting

### Q1: The initialization dialog box does not pop up
**Possible reasons**:
- PromptCatalyzer Agent already exists
- examine:`SELECT * FROM Senparc_AgentsManager_AgentTemplate WHERE Name = 'PromptCatalyzer'`
- **Solution**: Delete old data and retest

### Q2: Initialization has been stuck in "Initializing"
**Possible reasons**:
- EventHandler is not registered
- PromptInitResponseHandler is not triggered
**Troubleshooting**:
1. Check the startup log: whether there is "EventBus Scan Assembly" output
2. Check whether the scanned`Senparc.Xncf.PromptRange`and`Senparc.Xncf.AgentsManager`
3. Restart the application and make sure`AddSenparcEventBus()`exist`StartWebEngine()`call after

### Q3: 404 error returned during optimization
**Possible reasons**:
- `PromptOptimizationController`Not registered
**Troubleshooting**:
1. Confirm that the file exists:`src/Extensions/Senparc.Xncf.AgentsManager/OHS/Remote/Controllers/PromptOptimizationController.cs`
2. Restart the application
3. Check routing:`http://localhost:5000/api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService/OptimizeAsync`

### Q4: ChatTask not created
**Possible reasons**:
- PromptOptimizationChatTaskHandler is not registered
- ChatGroup does not exist
**Troubleshooting**:
1. Check the ChatGroup:`SELECT * FROM Senparc_AgentsManager_ChatGroup WHERE Name = 'PromptCatalyzer-OptimizationGroup'`
2. If it does not exist, delete the PromptCatalyzer Agent and reinitialize it.
3. Check whether there is "PromptOptimizationChatTaskHandler" related output in the console log

### Q5: The AI ​​optimization content is incorrect or the parameters have not changed
**Possible reasons**:
- AI Model is not configured properly
- Optimize Prompt design issues
**Troubleshooting**:
1. Check the raw JSON returned by AI (the first 200 characters are in the console log)
2. Try different optimization requirements descriptions
3. Try changing to a different AI Model

### Q6: The Note of the new PromptItem is not "🤖AI-Generated"
**Possible reasons**:
- PromptOptimizationRequestHandler code problem
**Troubleshooting**:
1. Check the code:`PromptOptimizationRequestHandler.cs`Line 162
2. Should be:`Note = "🤖AI-Generated"`

---

## 📊 Performance and concurrency testing (optional)

### Concurrency optimization test
1. Open multiple browser tabs
2. Click the "Optimize" button at the same time
3. Observe whether data conflicts or errors occur
4. Verify that each request has a corresponding ChatTask and new PromptItem

**expected**:
- ✅ EventBus supports concurrency and will not block
- ✅ TaskCompletionSource matches responses exactly by RequestId
- ⚠️ ChatTask status updates are matched by name, and there may be a very small probability of mismatching

---

## 🎯 Summary of success indicators

### Initialization successful (4✅)
- ✅ PromptRange created
- ✅ PromptItem created (Note='AI-Catalyzer')
- ✅ AgentTemplate created (Name='PromptCatalyzer')
- ✅ ChatGroup creation (Admin = Enter = PromptCatalyzer Agent)

### Optimization successful (5 ✅)
- ✅ ChatTask created (Status: Chatting → Finished)
- ✅ The AI ​​call is successful (the console has an AI return log)
- ✅ New PromptItem created (Note='🤖AI-Generated')
- ✅ The front end displays optimization results (new version number, ratings, parameter changes)
- ✅ Prompt list automatically refreshes and switches to new version

---

## 📞 Feedback after test completion

Please give feedback after testing:
1. **Was initialization successful**?
- ChatGroup created?
- Are Admin and Enter the same Agent?
2. **Was the optimization successful**?
- ChatTask created?
- Is the new PromptItem marked 🤖AI-Generated?
- Is the content returned by the AI ​​reasonable?
3. **Console log**:
- Do you see the full step log (1/4 to 4/4, 1/5 to 5/5)?
- Are there any errors or warnings?
4. **Database Verification**:
-Does the data in each table meet expectations?
- Are the foreign key relationships correct?

---

## 🔧 Function-Calling extension description (optional)

### Current architecture
- ✅ PromptCatalyzer Agent`FunctionCallNames`set to`null`
- ✅ Optimized logic by`PromptOptimizationRequestHandler`deal with
- ✅ Clear process, good performance, easy to debug

### Optional extensions (future)
If you need to let the Agent invoke optimization capabilities independently, you can:

**1. Modify initialization logic**:
```csharp
// PromptOptimizationService.cs
var newAgent = new AgentTemplate(
    name: "PromptCatalyzer",
    // ...
    functionCallNames: "PromptCatalyzerPlugin",  // 添加 Plugin
    // ...
);
```

**2. Agent can be called through function-calling**:
```csharp
// Agent 自主决策
"I need to optimize this prompt, let me call OptimizePrompt function..."
// 调用 PromptCatalyzerPlugin.OptimizePrompt()
```

**3. Implementation scenario**:
- Autonomous iterative optimization: Agent decides whether to continue optimization based on scores
- Intelligent batch optimization: Agent automatically optimizes multiple prompts
-Multi-Agent collaboration: Multiple Agents are each responsible for different aspects of optimization

**Notice**:
- Function-calling will add a layer of LLM calls, which may increase latency
- Need to ensure that the Plugin is correctly registered in AIPluginHub
- Need to test the stability of function-calling

---

## 📁 Related documents

- 📄 [PromptCatalyzer-ChatGroup-Integration.md](./PromptCatalyzer-ChatGroup-Integration.md) - Complete architecture and process
- 📄 [PromptCatalyzer-Complete-Fix-Summary.md](./PromptCatalyzer-Complete-Fix-Summary.md) - Summary of previous fixes
- 📄 [EventBus-Handler-Registration-Fix.md](./EventBus-Handler-Registration-Fix.md) - EventBus registration fix

---

## ✨ Special instructions

### The purpose of AI generated tags
1. **UI distinction**: Users can clearly know which prompts are generated by AI
2. **Audit Trail**: Convenient to count the effect and frequency of use of AI optimization
3. **Data Analysis**: You can compare the effects of AI-generated and manually created prompts
4. **Version Management**: Clear version source marking

### Value of ChatGroup and ChatTask
1. **Task Tracking**: Each optimization has a complete task record
2. **Audit Log**: Record the user’s optimization needs and execution time
3. **Performance Monitoring**: You can count the average time spent on optimization tasks
4. **Scalability**: Laying the foundation for future multi-Agent collaboration optimization

---

## 🎉 Test success flag

Functionality is fully functional when all of the following items are complete:

- ✅ 4 database records (PromptRange, PromptItem, Agent, ChatGroup) were created during initialization
- ✅ ChatGroup's Admin and Enter both point to the same Agent
- ✅ ChatTask records created during optimization
- ✅ ChatTask status changes from Chatting to Finished
- ✅ New PromptItem created, labeled 🤖AI-Generated
- ✅ AI returned reasonable optimization content and parameter suggestions
- ✅ The front end correctly displays the optimization results (new version number, rating, parameter changes)
- ✅ Console log shows all steps in full

Once testing is complete, please provide feedback, specifically:
1. Are there any errors or exceptions?
2. Is the content optimized by AI reasonable?
3. Are there any performance issues (such as optimization taking too long)?
