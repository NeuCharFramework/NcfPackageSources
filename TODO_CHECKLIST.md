# 🎯 Prompt Optimization Implementation Checklist

## 📋 Preparation

### Step 0: Confirm dependency services ⏳

**Information needed from you:**

1. **AI invocation service**
   - [ ] Confirm the service class name used for AI calls in the project.
   - [ ] Confirm the method signature (for example, ChatAsync, CompletionAsync).
   - [ ] Confirm how this service is injected into handlers.

   **Example questions:**
   ```csharp
   // Is it like this?
   public class AIKernelService
   {
       public Task<string> ChatAsync(string systemPrompt, string userMessage, ...);
   }

   // Or like this?
   public class ChatService
   {
       public Task<string> SendAsync(ChatRequest request);
   }
   ```

2. **PromptItem query method**
   - [ ] Confirm whether PromptItemService has a query method by FullVersion.
   - [ ] If not, I can help implement one.

3. **Test environment**
   - [ ] Confirm that an available AI model is configured.
   - [ ] Confirm that the AI API key is configured.

---

## 🔧 Implementation Tasks

### Task 1: Update PromptOptimizationRequestHandler ⏳

**File**: src/Extensions/Senparc.Xncf.PromptRange/Application/EventHandlers/PromptOptimizationRequestHandler.cs

**Current issues:**
- Uses old event format (missing NewPromptContent and Parameters).
- Only simulates optimization; no real AI call.

**Required implementation:**
1. [ ] Add required service dependency injection (AI service, PromptRangeService, etc.).
2. [ ] Implement GetPromptItemByCode() for parsing and querying PromptItem.
3. [ ] Implement BuildOptimizationSystemPrompt().
4. [ ] Implement BuildOptimizationUserInput().
5. [ ] Call AI service for optimization.
6. [ ] Parse returned AI JSON (including optimized content and parameters).
7. [ ] Create new PromptItem.
8. [ ] Publish response event in the correct format.

**I will provide**: a full implementation template (you only need to fill in the AI service invocation part).

---

### Task 2: Update frontend JavaScript ⏳

**File**: src/Extensions/Senparc.Xncf.PromptRange/wwwroot/js/PromptRange/prompt.js

**Method to modify**: executeOptimize() (around line 3010)

**Required implementation:**
1. [ ] Get full prompt information (including parameters).
2. [ ] Build request payload including promptContent and context.
3. [ ] Show more detailed optimization result (including parameter changes).
4. [ ] Refresh prompt list.
5. [ ] Optional: auto-switch to the new prompt.

**I will provide**: complete JavaScript code.

---

### Task 3: Testing and verification ⏳

**Test steps:**
1. [ ] Unit test: AI optimization logic.
2. [ ] Integration test: full workflow.
3. [ ] End-to-end test: click from frontend to result display.

**Acceptance criteria:**
- [ ] Auto-create PromptCatalyzer agent on first invocation.
- [ ] Successfully call AI to optimize prompt content.
- [ ] Successfully optimize parameters (for example, Temperature).
- [ ] Create new PromptItem and return PromptCode.
- [ ] Frontend displays optimization result correctly.

---

## 📊 Progress Tracking

| Task | Status | Completion Time |
|------|------|----------|
| Step 0: Confirm dependencies | ⏳ Waiting | - |
| Task 1: Update handler | ⏳ Not started | - |
| Task 2: Update frontend | ⏳ Not started | - |
| Task 3: Testing & verification | ⏳ Not started | - |

---

## 🚀 Start Action

**Current step**: Step 0 - AI service information is required.

**What you need to do:**
1. Check AI invocation service code in your project.
2. Reply with the following:
   - AI service class name
   - AI service method signature
   - Injection approach

**Example reply:**
```
My AI service is Senparc.AI.Kernel.AIChatService,
method signature is Task<string> ChatAsync(string prompt, ChatOptions options),
injected via constructor.
```

After receiving this info, I will provide a complete implementation immediately.

---

## 📞 Need Help?

If you run into any issue, tell me anytime:
- AI service related questions
- Implementation details
- Test errors
- Any other blockers

I will help you resolve them step by step.
