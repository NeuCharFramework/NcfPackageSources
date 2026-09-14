var chatApp = new Vue({
  el: '#app',
  mixins: [window.ChatLauncherMixin],
  data() {
    return {
      currentSessionId: 0,
      currentSessionTitle: '',
      currentSessionModules: [],
      currentSessionWorkflows: [],
      currentSessionAiModelId: 0,
      chatMode: 'harness',
      trajectoryDialogVisible: false,
      trajectoryLoading: false,
      trajectoryDetailLoading: false,
      trajectoryList: [],
      selectedTrajectory: null,
      selectedTrajectoryEvents: [],
      trajectorySearchText: '',
      replayIndex: -1,
      replayPlaying: false,
      replayTimer: null,
      aiModelOptions: [],
      aiKernelAvailable: false,
      loadingAiModelOptions: false,
      sessionList: [],
      messageList: [],
      inputMessage: '',
      loadingSessions: false,
      loadingMessages: false,
      isSending: false,
      isAIResponding: false,
      currentUserId: 0,
      autoScrollEnabled: true,
      isManageMode: false,
      selectedMessageIds: []
    };
  },
  computed: {
    replayEvent() {
      return this.replayIndex >= 0 ? this.selectedTrajectoryEvents[this.replayIndex] || null : null;
    }
  },
  mounted() {
    if (window.INITIAL_DATA) {
      this.currentSessionId = window.INITIAL_DATA.sessionId || 0;
      this.currentUserId = window.INITIAL_DATA.currentUserId || 0;
      
      if (window.INITIAL_DATA.initialMessage) {
        const initialMessage = decodeURIComponent(window.INITIAL_DATA.initialMessage);
        if (this.currentSessionId > 0) {
          this.inputMessage = initialMessage;
        } else {
          this.chatInputText = initialMessage;
        }
      }
    }

    this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
    this.launcherAiModelId = this.currentSessionAiModelId;

    this.loadAiModelOptions();
    this.loadSessionList();
    
    if (this.currentSessionId > 0) {
      this.loadSessionDetail();
      this.loadTrajectoryList();
    }
  },
  methods: {
    async loadAiModelOptions() {
      this.loadingAiModelOptions = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetAiModelOptionsAsync');

        if (response.data && response.data.success && response.data.data) {
          this.aiKernelAvailable = !!response.data.data.aiKernelAvailable;
          this.aiModelOptions = response.data.data.models || [];
        } else {
          this.aiModelOptions = [{ id: 0, name: ncfT('AdminChat.FallbackModelName'), description: ncfT('AdminChat.FallbackModelDescription'), isDefault: true }];
        }
      } catch (error) {
        console.error('加载 AI 模型失败:', error);
        this.aiModelOptions = [{ id: 0, name: ncfT('AdminChat.FallbackModelName'), description: ncfT('AdminChat.FallbackModelDescription'), isDefault: true }];
      } finally {
        this.loadingAiModelOptions = false;
      }
    },

    handleCurrentSessionAiModelChange(value) {
      this.currentSessionAiModelId = this.normalizeAiModelId(value);
      this.setSessionAiModelId(this.currentSessionId, this.currentSessionAiModelId);
    },

    harnessStepsTitle(steps) {
      const label = (typeof ncfT === 'function' && ncfT('AdminChat.HarnessSteps')) || 'Harness';
      return label + ' · ' + (steps ? steps.length : 0);
    },

    trajectoryStatusLabel(status) {
      return {
        0: '执行中',
        1: '等待审批',
        2: '已完成',
        3: '失败',
        4: '已取消'
      }[status] || '未知状态';
    },

    formatTrajectoryPayload(payload) {
      if (!payload) return '';
      try {
        return JSON.stringify(JSON.parse(payload), null, 2);
      } catch (error) {
        return payload;
      }
    },

    async loadTrajectoryList() {
      if (!this.currentSessionId) return;
      this.trajectoryLoading = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionTrajectoriesAsync?sessionId=${this.currentSessionId}`);
        if (response.data && response.data.success && response.data.data) {
          this.trajectoryList = response.data.data.trajectories || [];
        }
      } catch (error) {
        console.error('加载 Harness 轨迹失败:', error);
      } finally {
        this.trajectoryLoading = false;
      }
    },

    openTrajectoryDialog() {
      this.trajectoryDialogVisible = true;
      this.loadTrajectoryList();
    },

    async openTrajectory(trajectoryId) {
      if (!trajectoryId) return;
      this.trajectoryDialogVisible = true;
      this.trajectoryDetailLoading = true;
      try {
        const query = this.trajectorySearchText ? `&query=${encodeURIComponent(this.trajectorySearchText)}` : '';
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetTrajectoryAsync?trajectoryId=${trajectoryId}${query}`);
        if (response.data && response.data.success && response.data.data) {
          this.selectedTrajectory = response.data.data.trajectory;
          this.selectedTrajectoryEvents = response.data.data.events || [];
          this.stopTrajectoryReplay();
        }
      } catch (error) {
        console.error('加载 Harness 轨迹详情失败:', error);
        this.$message.error('轨迹加载失败');
      } finally {
        this.trajectoryDetailLoading = false;
      }
    },

    async searchTrajectories() {
      if (!this.trajectorySearchText || !this.trajectorySearchText.trim()) {
        await this.loadTrajectoryList();
        return;
      }
      this.trajectoryLoading = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SearchTrajectoriesAsync?query=${encodeURIComponent(this.trajectorySearchText.trim())}`);
        if (response.data && response.data.success && response.data.data) {
          this.trajectoryList = response.data.data.trajectories || [];
          if (this.selectedTrajectory) {
            await this.openTrajectory(this.selectedTrajectory.id);
          }
        }
      } catch (error) {
        console.error('检索 Harness 轨迹失败:', error);
      } finally {
        this.trajectoryLoading = false;
      }
    },

    async resumeTrajectory(trajectoryId) {
      const prompt = await this.askTrajectoryInstruction('恢复 Harness 任务', '继续执行上次尚未完成的任务');
      if (prompt === null) return;
      await this.runTrajectoryAction('resume', { trajectoryId, instruction: prompt });
    },

    async forkTrajectory(trajectoryId) {
      const trajectory = this.trajectoryList.find(item => item.id === trajectoryId) || this.selectedTrajectory;
      const prompt = await this.askTrajectoryInstruction('分叉 Harness 任务', '从当前轨迹最后一步创建一个新分支');
      if (prompt === null) return;
      await this.runTrajectoryAction('fork', {
        trajectoryId,
        forkFromSequence: trajectory ? trajectory.lastSequence : 0,
        instruction: prompt
      });
    },

    resumeSelectedTrajectory() {
      return this.selectedTrajectory ? this.resumeTrajectory(this.selectedTrajectory.id) : null;
    },

    forkSelectedTrajectory() {
      return this.selectedTrajectory ? this.forkTrajectory(this.selectedTrajectory.id) : null;
    },

    toggleTrajectoryReplay() {
      if (this.replayPlaying) {
        this.stopTrajectoryReplay();
      } else {
        this.startTrajectoryReplay();
      }
    },

    startTrajectoryReplay() {
      if (!this.selectedTrajectoryEvents.length) return;
      this.stopTrajectoryReplay();
      this.replayIndex = 0;
      this.replayPlaying = true;
      this.replayTimer = window.setInterval(() => {
        if (this.replayIndex >= this.selectedTrajectoryEvents.length - 1) {
          this.stopTrajectoryReplay();
          return;
        }
        this.replayIndex += 1;
      }, 650);
    },

    stopTrajectoryReplay() {
      if (this.replayTimer) {
        window.clearInterval(this.replayTimer);
        this.replayTimer = null;
      }
      this.replayPlaying = false;
    },

    askTrajectoryInstruction(title, defaultValue) {
      return this.$prompt('可以直接确认默认指令，也可以补充新的执行要求。', title, {
        confirmButtonText: '执行',
        cancelButtonText: '取消',
        inputValue: defaultValue,
        inputType: 'textarea'
      }).then(({ value }) => value).catch(() => null);
    },

    async runTrajectoryAction(action, payload) {
      this.isSending = true;
      this.isAIResponding = true;
      try {
        const method = action === 'fork' ? 'ForkTrajectoryAsync' : 'ResumeTrajectoryAsync';
        const response = await service.post(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.${method}`, payload);
        if (!(response.data && response.data.success && response.data.data)) {
          throw new Error(response.data && response.data.errorMessage ? response.data.errorMessage : '任务执行失败');
        }
        const data = response.data.data;
        const harness = data.harness || {};
        if (data.userMessage) this.messageList.push(data.userMessage);
        if (data.assistantMessage) {
          data.assistantMessage.trajectoryId = harness.trajectoryId || 0;
          data.assistantMessage.trajectoryEvents = harness.trajectoryEvents || [];
          data.assistantMessage.pendingApprovals = harness.pendingApprovals || [];
          this.messageList.push(data.assistantMessage);
        }
        await this.loadSessionList();
        await this.loadTrajectoryList();
        if (harness.trajectoryId) await this.openTrajectory(harness.trajectoryId);
        this.$message.success(action === 'fork' ? '已创建新的任务分支' : '已继续执行任务');
      } catch (error) {
        console.error('Harness 轨迹操作失败:', error);
        this.$message.error(error.message || '任务执行失败');
      } finally {
        this.isSending = false;
        this.isAIResponding = false;
      }
    },

    async respondApproval(trajectoryId, approval, approved) {
      this.isSending = true;
      this.isAIResponding = true;
      try {
        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.RespondTrajectoryApprovalAsync', {
          trajectoryId,
          requestId: approval.requestId,
          toolCallId: approval.toolCallId,
          toolName: approval.toolName,
          argumentsJson: approval.argumentsJson || '{}',
          approved,
          reason: approved ? '管理员允许执行' : '管理员拒绝执行'
        });
        if (!(response.data && response.data.success && response.data.data)) {
          throw new Error(response.data && response.data.errorMessage ? response.data.errorMessage : '审批处理失败');
        }
        const data = response.data.data;
        const harness = data.harness || {};
        if (data.assistantMessage) {
          data.assistantMessage.trajectoryId = harness.trajectoryId || trajectoryId;
          data.assistantMessage.trajectoryEvents = harness.trajectoryEvents || [];
          data.assistantMessage.pendingApprovals = harness.pendingApprovals || [];
          this.messageList.push(data.assistantMessage);
        }
        await this.loadTrajectoryList();
        await this.openTrajectory(harness.trajectoryId || trajectoryId);
      } catch (error) {
        console.error('处理 Harness 审批失败:', error);
        this.$message.error(error.message || '审批处理失败');
      } finally {
        this.isSending = false;
        this.isAIResponding = false;
      }
    },

    handleChatInputKeydown(event) {
      // 保持与首页一致：Ctrl+Enter (Windows/Linux) 或 Cmd+Enter (Mac) 发送。
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.sendMessage();
      }
      // 普通 Enter 保留换行行为。
    },

    async loadSessionList() {
      this.loadingSessions = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionListAsync?pageIndex=1&pageSize=50');
        
        if (response.data && response.data.success && response.data.data) {
          this.sessionList = response.data.data.sessions || [];
        } else {
          console.error('加载会话列表失败:', response.data.errorMessage);
        }
      } catch (error) {
        console.error('加载会话列表异常:', error);
        this.$message.error(ncfT('AdminChat.LoadSessionFailed'));
      } finally {
        this.loadingSessions = false;
      }
    },

    async loadSessionDetail() {
      if (!this.currentSessionId) return;

      this.loadingMessages = true;
      try {
        const response = await service.get(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionDetailAsync?sessionId=${this.currentSessionId}`);
        
        if (response.data && response.data.success && response.data.data) {
          const session = response.data.data.session;
          this.currentSessionTitle = session.title;
          this.messageList = session.messages || [];
          this.currentSessionModules = session.modules || [];
          this.currentSessionWorkflows = session.workflows || [];
          this.currentSessionAiModelId = this.getSessionAiModelId(this.currentSessionId);
          this.clearMessageSelection();
          this.isManageMode = false;
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('加载会话详情失败:', response.data.errorMessage);
          this.$message.error(ncfT('AdminChat.LoadDetailFailed'));
        }
      } catch (error) {
        console.error('加载会话详情异常:', error);
        this.$message.error(ncfT('AdminChat.LoadDetailFailed'));
      } finally {
        this.loadingMessages = false;
      }
    },

    async sendMessage() {
      if (!this.inputMessage || this.inputMessage.trim().length === 0) {
        this.$message.warning(ncfT('AdminChat.InputRequired'));
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error(ncfT('AdminChat.SessionRequired'));
        return;
      }

      const messageContent = this.inputMessage.trim();
      this.inputMessage = '';
      this.isSending = true;
      this.isAIResponding = true;

      // 乐观渲染：立即显示“我”的消息，避免等待接口返回期间出现空白。
      const tempMessageId = `temp-${Date.now()}`;
      const tempUserMessage = {
        id: tempMessageId,
        roleType: 0,
        content: messageContent,
        addTime: new Date().toISOString(),
        userFeedback: 0
      };
      this.messageList.push(tempUserMessage);
      this.$nextTick(() => {
        this.scrollToBottom();
      });

      try {
        const requestData = {
          sessionId: this.currentSessionId,
          aiModelId: this.normalizeAiModelId(this.currentSessionAiModelId),
          content: messageContent,
          mode: this.chatMode === 'harness' ? 1 : 0
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessageAsync', requestData);
        
        if (response.data && response.data.success && response.data.data) {
          const { userMessage, assistantMessage } = response.data.data;
          const harnessSteps = response.data.data.harnessSteps || null;
          const trajectoryId = response.data.data.trajectoryId || 0;
          const trajectoryEvents = response.data.data.trajectoryEvents || [];
          const pendingApprovals = response.data.data.pendingApprovals || [];

                  const tempIndex = this.messageList.findIndex((item) => item.id === tempMessageId);
                  if (tempIndex >= 0) {
                    // 用服务端正式消息替换临时消息，确保时间、ID等数据准确。
                    this.messageList.splice(tempIndex, 1, userMessage || tempUserMessage);
                  }

                  if (assistantMessage) {
                    assistantMessage.harnessSteps = harnessSteps;
                    assistantMessage.trajectoryId = trajectoryId;
                    assistantMessage.trajectoryEvents = trajectoryEvents;
                    assistantMessage.pendingApprovals = pendingApprovals;
                    this.messageList.push(assistantMessage);
                  }
          
          await this.loadSessionList();
          await this.loadTrajectoryList();
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('发送消息失败:', response.data.errorMessage);
          this.$message.error(response.data.errorMessage || ncfT('AdminChat.SendFailed'));
          this.messageList = this.messageList.filter((item) => item.id !== tempMessageId);
          this.inputMessage = messageContent;
        }
      } catch (error) {
        console.error('发送消息异常:', error);
        this.$message.error(ncfT('AdminChat.SendFailedRetry'));
        this.messageList = this.messageList.filter((item) => item.id !== tempMessageId);
        this.inputMessage = messageContent;
      } finally {
        this.isSending = false;
        this.isAIResponding = false;
      }
    },

    async setFeedback(messageId, feedbackType) {
      try {
        const response = await service.put(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SetMessageFeedbackAsync?messageId=${messageId}&feedback=${feedbackType}`);
        
        if (response.data && response.data.success) {
          const message = this.messageList.find(m => m.id === messageId);
          if (message) {
            message.userFeedback = feedbackType;
          }
          this.$message.success(ncfT('AdminChat.SetFeedbackSuccess'));
        } else {
          this.$message.error(ncfT('AdminChat.FeedbackFailed'));
        }
      } catch (error) {
        console.error('设置反馈异常:', error);
        this.$message.error(ncfT('AdminChat.FeedbackFailed'));
      }
    },

    async switchSession(sessionId) {
      if (this.currentSessionId === sessionId) return;

      this.currentSessionId = sessionId;
      this.currentSessionAiModelId = this.getSessionAiModelId(sessionId);
      this.messageList = [];
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.clearMessageSelection();
      this.isManageMode = false;
      await this.loadSessionDetail();
      await this.loadTrajectoryList();
    },

    async createNewSession() {
      this.launcherAiModelId = this.currentSessionAiModelId || this.launcherAiModelId || 0;
      this.currentSessionId = 0;
      this.currentSessionTitle = '';
      this.currentSessionModules = [];
      this.currentSessionWorkflows = [];
      this.messageList = [];
      this.inputMessage = '';
      this.chatInputText = '';
      this.trajectoryList = [];
      this.selectedTrajectory = null;
      this.selectedTrajectoryEvents = [];
      this.clearMessageSelection();
      this.isManageMode = false;
    },

    toggleManageMode() {
      this.isManageMode = !this.isManageMode;
      if (!this.isManageMode) {
        this.clearMessageSelection();
      }
    },

    isMessageSelected(messageId) {
      return this.selectedMessageIds.includes(String(messageId));
    },

    toggleMessageSelection(messageId) {
      const key = String(messageId);
      const index = this.selectedMessageIds.indexOf(key);
      if (index >= 0) {
        this.selectedMessageIds.splice(index, 1);
      } else {
        this.selectedMessageIds.push(key);
      }
    },

    toggleSelectAllMessages(checked) {
      if (!checked) {
        this.selectedMessageIds = [];
        return;
      }

      this.selectedMessageIds = this.messageList.map((m) => String(m.id));
    },

    clearMessageSelection() {
      this.selectedMessageIds = [];
    },

    async copySelectedMessages() {
      const selectedSet = new Set(this.selectedMessageIds);
      const selectedMessages = this.messageList.filter((m) => selectedSet.has(String(m.id)));
      if (selectedMessages.length === 0) {
        this.$message.warning(ncfT('AdminChat.SelectMessages'));
        return;
      }

      const plainText = selectedMessages
        .map((m) => `[${this.getRoleTypeName(m.roleType)}] ${m.content || ''}`)
        .join('\n\n');

      try {
        if (navigator.clipboard && navigator.clipboard.writeText) {
          await navigator.clipboard.writeText(plainText);
        } else {
          const textarea = document.createElement('textarea');
          textarea.value = plainText;
          textarea.style.position = 'fixed';
          textarea.style.opacity = '0';
          document.body.appendChild(textarea);
          textarea.select();
          document.execCommand('copy');
          document.body.removeChild(textarea);
        }

        this.$message.success(ncfT('AdminChat.CopyCount', selectedMessages.length));
      } catch (error) {
        console.error('复制消息失败:', error);
        this.$message.error(ncfT('AdminChat.CopyFailed'));
      }
    },

    async deleteSelectedMessages() {
      const selectedIds = this.selectedMessageIds
        .map((id) => parseInt(id, 10))
        .filter((id) => Number.isInteger(id) && id > 0);

      if (selectedIds.length === 0) {
        this.$message.warning(ncfT('AdminChat.SelectMessages'));
        return;
      }

      try {
        await this.$confirm(ncfT('AdminChat.DeleteMessagesConfirm', selectedIds.length), ncfT('AdminChat.DeleteConfirmTitle'), {
          confirmButtonText: ncfT('Common.删除'),
          cancelButtonText: ncfT('Common.取消'),
          type: 'warning'
        });
      } catch (error) {
        return;
      }

      try {
        const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteMessagesAsync?sessionId=${this.currentSessionId}&messageIds=${selectedIds.join(',')}`);

        if (response.data && response.data.success) {
          const selectedSet = new Set(selectedIds.map((id) => String(id)));
          this.messageList = this.messageList.filter((m) => !selectedSet.has(String(m.id)));
          this.clearMessageSelection();
          this.$message.success(response.data.data || ncfT('AdminChat.DeleteMessagesSuccess'));
        } else {
          this.$message.error((response.data && response.data.errorMessage) || ncfT('AdminChat.DeleteFailed'));
        }
      } catch (error) {
        console.error('批量删除消息异常:', error);
        this.$message.error(ncfT('AdminChat.DeleteFailedRetry'));
      }
    },

    async handleSessionCommand(command) {
      if (command.action === 'delete') {
        this.$confirm(ncfT('AdminChat.DeleteConfirm'), ncfT('AdminChat.DeleteConfirmTitle'), {
          confirmButtonText: ncfT('Common.确认'),
          cancelButtonText: ncfT('Common.取消'),
          type: 'warning'
        }).then(async () => {
          try {
            const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSessionAsync?sessionId=${command.id}`);
            
            if (response.data && response.data.success) {
              this.$message.success(ncfT('AdminChat.DeleteSessionSuccess'));
              
              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.messageList = [];
                this.currentSessionTitle = '';
                this.currentSessionModules = [];
                this.currentSessionWorkflows = [];
              }
              
              await this.loadSessionList();
            } else {
              this.$message.error(ncfT('AdminChat.DeleteFailed'));
            }
          } catch (error) {
            console.error('删除会话异常:', error);
            this.$message.error(ncfT('AdminChat.DeleteFailed'));
          }
        }).catch(() => {
          console.log('取消删除');
        });
      }
    },

    scrollToBottom() {
      if (!this.autoScrollEnabled) return;
      
      const container = this.$refs.messagesContainer;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
    },

    getRoleTypeClass(roleType) {
      const roleMap = {
        0: 'user',
        1: 'assistant',
        2: 'system'
      };
      return roleMap[roleType] || 'user';
    },

    getRoleTypeName(roleType) {
      const nameMap = {
        0: ncfT('AdminChat.RoleMe'),
        1: ncfT('AdminChat.RoleAssistant'),
        2: ncfT('AdminChat.RoleSystem')
      };
      return nameMap[roleType] || ncfT('AdminChat.RoleUnknown');
    },

    getMessageIcon(roleType) {
      const iconMap = {
        0: 'fa fa-user',
        1: 'fa fa-robot',
        2: 'fa fa-info-circle'
      };
      return iconMap[roleType] || 'fa fa-user';
    },

    getModuleUid(module) {
      return (module && (module.xncfModuleUid || module.uid)) || '';
    },

    resolveModuleDetail(module) {
      const uid = this.getModuleUid(module);
      const matched = this.availableModules.find((item) => item.uid === uid) || null;

      return {
        uid,
        name: (module && (module.displayName || module.menuName || module.moduleName)) || (matched && matched.name) || uid || ncfT('AdminChat.UnknownModule'),
        icon: (matched && matched.icon) || 'fa fa-cube',
        description: (module && module.moduleDescription) || (matched && matched.description) || ncfT('Admin.Home.NoDescription'),
        version: (module && module.moduleVersion) || (matched && matched.version) || '',
        menus: (matched && matched.menus) || [],
        functions: (matched && matched.functions) || []
      };
    },

    resolveWorkflowDetail(workflow) {
      return {
        id: workflow && workflow.workflowId,
        name: workflow && workflow.workflowName ? workflow.workflowName : `Workflow #${workflow && workflow.workflowId}`,
        description: workflow && workflow.workflowDescription ? workflow.workflowDescription : '',
        parameters: workflow && workflow.parameters ? workflow.parameters : []
      };
    },

    formatTime(dateTimeStr) {
      if (!dateTimeStr) return '';
      
      try {
        const date = new Date(dateTimeStr);
        const now = new Date();
        const diffMs = now - date;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return ncfT('AdminChat.JustNow');
        if (diffMins < 60) return ncfT('AdminChat.MinutesAgo', diffMins);
        if (diffHours < 24) return ncfT('AdminChat.HoursAgo', diffHours);
        if (diffDays < 7) return ncfT('AdminChat.DaysAgo', diffDays);
        
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        
        if (year === now.getFullYear()) {
          return `${month}-${day} ${hours}:${minutes}`;
        } else {
          return `${year}-${month}-${day} ${hours}:${minutes}`;
        }
      } catch (error) {
        console.error('时间格式化失败:', error);
        return dateTimeStr;
      }
    },

    formatMessageContent(content) {
      if (!content) return '';

      const rendered = marked.parse(content);
      return typeof DOMPurify === 'undefined' ? '' : DOMPurify.sanitize(rendered);
    }
  }
});
