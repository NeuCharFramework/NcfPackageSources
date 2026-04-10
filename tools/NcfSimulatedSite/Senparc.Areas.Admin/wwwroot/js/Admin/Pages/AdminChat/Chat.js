var ncfI18n = window.ncfI18n || {};

var chatApp = new Vue({
  el: '#app',
  mixins: [window.ChatLauncherMixin],
  data() {
    return {
      currentSessionId: 0,
      currentSessionTitle: '',
      currentSessionModules: [],
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

    this.loadSessionList();

    if (this.currentSessionId > 0) {
      this.loadSessionDetail();
    }
  },
  methods: {
    handleChatInputKeydown(event) {
      // Keep consistent with homepage: Ctrl+Enter (Windows/Linux) or Cmd+Enter (Mac) to send.
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.sendMessage();
      }
      // Plain Enter keeps line break behavior.
    },

    async loadSessionList() {
      this.loadingSessions = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.GetSessionListAsync?pageIndex=1&pageSize=50');

        if (response.data && response.data.success && response.data.data) {
          this.sessionList = response.data.data.sessions || [];
        } else {
          console.error('Failed to load session list:', response.data.errorMessage);
        }
      } catch (error) {
        console.error('Error loading session list:', error);
        this.$message.error(ncfI18n.loadSessionListFailed || 'Failed to load session list');
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
          this.clearMessageSelection();
          this.isManageMode = false;

          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('Failed to load session detail:', response.data.errorMessage);
          this.$message.error(ncfI18n.loadSessionDetailFailed || 'Failed to load session detail');
        }
      } catch (error) {
        console.error('Error loading session detail:', error);
        this.$message.error(ncfI18n.loadSessionDetailFailed || 'Failed to load session detail');
      } finally {
        this.loadingMessages = false;
      }
    },

    async sendMessage() {
      if (!this.inputMessage || this.inputMessage.trim().length === 0) {
        this.$message.warning(ncfI18n.pleaseEnterMessage || 'Please enter a message');
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error(ncfI18n.pleaseSelectOrCreateSession || 'Please select or create a session');
        return;
      }

      const messageContent = this.inputMessage.trim();
      this.inputMessage = '';
      this.isSending = true;
      this.isAIResponding = true;

      // Optimistic rendering: show user message immediately to avoid blank during API call.
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
          content: messageContent
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessageAsync', requestData);

        if (response.data && response.data.success && response.data.data) {
          const { userMessage, assistantMessage } = response.data.data;

                  const tempIndex = this.messageList.findIndex((item) => item.id === tempMessageId);
                  if (tempIndex >= 0) {
                    // Replace temporary message with server message for accurate time and ID.
                    this.messageList.splice(tempIndex, 1, userMessage || tempUserMessage);
                  }

                  if (assistantMessage) {
                    this.messageList.push(assistantMessage);
                  }

          await this.loadSessionList();

          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('Failed to send message:', response.data.errorMessage);
          this.$message.error(response.data.errorMessage || (ncfI18n.sendMessageFailed || 'Failed to send message'));
          this.messageList = this.messageList.filter((item) => item.id !== tempMessageId);
          this.inputMessage = messageContent;
        }
      } catch (error) {
        console.error('Error sending message:', error);
        this.$message.error(ncfI18n.sendMessageFailedRetry || 'Failed to send message, please try again');
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
          this.$message.success(ncfI18n.feedbackSuccess || 'Feedback submitted');
        } else {
          this.$message.error(ncfI18n.feedbackFailed || 'Feedback failed');
        }
      } catch (error) {
        console.error('Error setting feedback:', error);
        this.$message.error(ncfI18n.feedbackFailed || 'Feedback failed');
      }
    },

    async switchSession(sessionId) {
      if (this.currentSessionId === sessionId) return;

      this.currentSessionId = sessionId;
      this.messageList = [];
      this.currentSessionModules = [];
      this.clearMessageSelection();
      this.isManageMode = false;
      await this.loadSessionDetail();
    },

    async createNewSession() {
      this.currentSessionId = 0;
      this.currentSessionTitle = '';
      this.currentSessionModules = [];
      this.messageList = [];
      this.inputMessage = '';
      this.chatInputText = '';
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
        this.$message.warning(ncfI18n.pleaseSelectMessagesToCopy || 'Please select messages to copy');
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

        this.$message.success((ncfI18n.copiedNMessages || 'Copied {0} message(s)').replace('{0}', selectedMessages.length));
      } catch (error) {
        console.error('Failed to copy messages:', error);
        this.$message.error(ncfI18n.copyFailedManual || 'Copy failed, please copy manually');
      }
    },

    async deleteSelectedMessages() {
      const selectedIds = this.selectedMessageIds
        .map((id) => parseInt(id, 10))
        .filter((id) => Number.isInteger(id) && id > 0);

      if (selectedIds.length === 0) {
        this.$message.warning(ncfI18n.pleaseSelectMessagesToDelete || 'Please select messages to delete');
        return;
      }

      try {
        await this.$confirm(
          (ncfI18n.confirmDeleteNMessages || 'Are you sure you want to delete the selected {0} message(s)? This action cannot be undone.').replace('{0}', selectedIds.length),
          ncfI18n.deleteConfirm || 'Delete Confirmation', {
          confirmButtonText: ncfI18n.deleteBtn || 'Delete',
          cancelButtonText: ncfI18n.cancel || 'Cancel',
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
          this.$message.success(response.data.data || (ncfI18n.deleteSuccess || 'Deleted successfully'));
        } else {
          this.$message.error((response.data && response.data.errorMessage) || (ncfI18n.deleteFailed || 'Delete failed'));
        }
      } catch (error) {
        console.error('Error batch deleting messages:', error);
        this.$message.error(ncfI18n.deleteFailedRetry || 'Delete failed, please try again');
      }
    },

    async handleSessionCommand(command) {
      if (command.action === 'delete') {
        this.$confirm(ncfI18n.confirmDeleteSession || 'Are you sure you want to delete this session?', ncfI18n.notice || 'Notice', {
          confirmButtonText: ncfI18n.confirm || 'OK',
          cancelButtonText: ncfI18n.cancel || 'Cancel',
          type: 'warning'
        }).then(async () => {
          try {
            const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSessionAsync?sessionId=${command.id}`);

            if (response.data && response.data.success) {
              this.$message.success(ncfI18n.deleteSuccess || 'Deleted successfully');

              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.messageList = [];
                this.currentSessionTitle = '';
                this.currentSessionModules = [];
              }

              await this.loadSessionList();
            } else {
              this.$message.error(ncfI18n.deleteFailed || 'Delete failed');
            }
          } catch (error) {
            console.error('Error deleting session:', error);
            this.$message.error(ncfI18n.deleteFailed || 'Delete failed');
          }
        }).catch(() => {
          console.log('Delete cancelled');
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
        0: ncfI18n.chatRoleMe || 'Me',
        1: ncfI18n.chatRoleAI || 'AI Assistant',
        2: ncfI18n.chatRoleSystem || 'System'
      };
      return nameMap[roleType] || (ncfI18n.chatRoleUnknown || 'Unknown');
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
        name: (module && (module.displayName || module.menuName || module.moduleName)) || (matched && matched.name) || uid || (ncfI18n.unnamedModule || 'Unnamed module'),
        icon: (matched && matched.icon) || 'fa fa-cube',
        description: (module && module.moduleDescription) || (matched && matched.description) || (ncfI18n.noDescription || 'No description'),
        version: (module && module.moduleVersion) || (matched && matched.version) || '',
        menus: (matched && matched.menus) || [],
        functions: (matched && matched.functions) || []
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

        if (diffMins < 1) return ncfI18n.justNow || 'Just now';
        if (diffMins < 60) return (ncfI18n.minutesAgo || '{0} min ago').replace('{0}', diffMins);
        if (diffHours < 24) return (ncfI18n.hoursAgo || '{0} hr ago').replace('{0}', diffHours);
        if (diffDays < 7) return (ncfI18n.daysAgo || '{0} day(s) ago').replace('{0}', diffDays);

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
        console.error('Time formatting failed:', error);
        return dateTimeStr;
      }
    },

    formatMessageContent(content) {
      if (!content) return '';

      let formatted = content
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#x27;')
        .replace(/\//g, '&#x2F;');

      formatted = formatted
        .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
        .replace(/\*(.+?)\*/g, '<em>$1</em>')
        .replace(/`(.+?)`/g, '<code>$1</code>')
        .replace(/\n/g, '<br/>');

      return formatted;
    }
  }
});
