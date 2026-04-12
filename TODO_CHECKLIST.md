[中文版](TODO_CHECKLIST.cn.md)

# 🎯 Prompt optimization function implementation list

## 📋 Preparation

### Step 0: Confirm dependent services ⏳

**Information required from you:**

1. **AI calling service**
  - Confirm the service class name used to call AI in the project
  - Confirm the calling method signature of the service (such as `ChatAsync`, `CompletionAsync`, etc.)
  - Confirm how to inject the service in Handler
   **Example Question**:
2. **PromptItem query method**
  - Confirm whether `PromptItemService` has a method to query through `FullVersion`
  - If not, I will help you implement one
3. **Test environment**
  - Confirm that there is an available AI Model configuration
  - Confirm that the AI API Key has been configured

---

## 🔧 Implement tasks

### Task 1: Update PromptOptimizationRequestHandler ⏳

**File**: `src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs`

**Current Issue**:

- Use old event format (missing `NewPromptContent` and `Parameters`)
- Just simulation optimization, no real call to AI

**Requires implementation**:

1. [ ] Add necessary service dependency injection (AI service, PromptRangeService, etc.)
2. [ ] Implement the `GetPromptItemByCode()` method to parse and query PromptItem
3. [ ] Implement the `BuildOptimizationSystemPrompt()` method
4. [ ] Implement the `BuildOptimizationUserInput()` method
5. [ ] Call AI service for optimization
6. [ ] Parse the JSON returned by AI (including optimized content and parameters)
7. [ ] Create a new PromptItem
8. [ ] Publish correctly formatted response events

**I will provide**: Complete implementation code template (you need to fill in the AI service calling part)

---

### Task 2: Update front-end JavaScript ⏳

**File**: `src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js`

**Method that needs to be modified**: `executeOptimize()` (about line 3010)

**Requires implementation**:

1. [ ] Get the complete information of the current Prompt (including parameters)
2. [ ] Construct a request object containing `promptContent` and `context`
3. [ ] displays more detailed optimization results (including parameter changes)
4. [ ] Refresh the Prompt list
5. [ ] Optional: Automatically switch to new Prompt

**I will provide**: the complete JavaScript code

---

### Task 3: Testing and Validation ⏳

**Test steps**:

1. [ ] Unit test: test AI optimization logic
2. [ ] Integration testing: testing the complete process
3. [ ] End-to-end testing: from clicking on the front end to seeing the results

**Acceptance Criteria**:

- Automatically create "PromptCatalyzer" Agent when called for the first time
- Ability to call AI to optimize Prompt content
- Ability to optimize parameters (Temperature, etc.)
- Create a new PromptItem and return a PromptCode
- Front-end displays optimization results

---

## 📊 Progress Tracking


| Task | Status | Completion Time |
| ------------------ | ----- | ---- |
| Step 0: Confirm dependencies | ⏳ Waiting | - |
| Task 1: Update Handler | ⏳ To be started | - |
| Task 2: Update front end | ⏳ To be started | - |
| Task 3: Test verification | ⏳ To be started | - |


---

## 🚀 Get started

**Current Step**: Step 0 - You are required to provide information about AI services

**Things to do**:

1. View the code of the AI calling service in your project
2. Reply with the following information:
  - The class name of the AI service
  - Call method signature of AI service
  - How to inject the service

**Sample response**:```
我的 AI 服务是 `Senparc.AI.Kernel.AIChatService`，
调用方法是 `Task<string> ChatAsync(string prompt, ChatOptions options)`，
通过构造函数注入。
```After receiving your information, I will provide you with the complete implementation code immediately!

---

## 📞Need help?

If you encounter any problems, please feel free to let me know:

- Issues related to AI services
- Code implementation questions
- Errors during testing
- any other questions

I will help you solve it step by step!
