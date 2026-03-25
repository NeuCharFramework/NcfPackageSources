var chatApp = new Vue({
  el: '#admin-chat-app',
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
      autoScrollEnabled: true
    };
  },
  mounted() {
    if (window.INITIAL_DATA) {
      this.currentSessionId = window.INITIAL_DATA.sessionId || 0;
      this.currentUserId = window.INITIAL_DATA.currentUserId || 0;
      
      if (window.INITIAL_DATA.initialMessage) {
        this.inputMessage = decodeURIComponent(window.INITIAL_DATA.initialMessage);
      }
    }

    this.loadSessionList();
    
    if (this.currentSessionId > 0) {
      this.loadSessionDetail();
    }
  },
  methods: {
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
        this.$message.error('加载会话列表失败');
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
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('加载会话详情失败:', response.data.errorMessage);
          this.$message.error('加载会话详情失败');
        }
      } catch (error) {
        console.error('加载会话详情异常:', error);
        this.$message.error('加载会话详情失败');
      } finally {
        this.loadingMessages = false;
      }
    },

    async sendMessage() {
      if (!this.inputMessage || this.inputMessage.trim().length === 0) {
        this.$message.warning('请输入消息内容');
        return;
      }

      if (!this.currentSessionId) {
        this.$message.error('请先选择或创建会话');
        return;
      }

      const messageContent = this.inputMessage.trim();
      this.inputMessage = '';
      this.isSending = true;
      this.isAIResponding = true;

      try {
        const requestData = {
          sessionId: this.currentSessionId,
          content: messageContent
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.SendMessageAsync', requestData);
        
        if (response.data && response.data.success && response.data.data) {
          const { userMessage, assistantMessage } = response.data.data;
          
          this.messageList.push(userMessage);
          this.messageList.push(assistantMessage);
          
          await this.loadSessionList();
          
          this.$nextTick(() => {
            this.scrollToBottom();
          });
        } else {
          console.error('发送消息失败:', response.data.errorMessage);
          this.$message.error(response.data.errorMessage || '发送消息失败');
          this.inputMessage = messageContent;
        }
      } catch (error) {
        console.error('发送消息异常:', error);
        this.$message.error('发送消息失败，请稍后重试');
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
          this.$message.success('反馈成功');
        } else {
          this.$message.error('反馈失败');
        }
      } catch (error) {
        console.error('设置反馈异常:', error);
        this.$message.error('反馈失败');
      }
    },

    async switchSession(sessionId) {
      if (this.currentSessionId === sessionId) return;

      this.currentSessionId = sessionId;
      this.messageList = [];
      this.currentSessionModules = [];
      await this.loadSessionDetail();
    },

    async createNewSession() {
      window.location.href = '/Admin/Index';
    },

    async handleSessionCommand(command) {
      if (command.action === 'delete') {
        this.$confirm('确定要删除这个会话吗？', '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        }).then(async () => {
          try {
            const response = await service.delete(`/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.DeleteSessionAsync?sessionId=${command.id}`);
            
            if (response.data && response.data.success) {
              this.$message.success('删除成功');
              
              if (this.currentSessionId === command.id) {
                this.currentSessionId = 0;
                this.messageList = [];
                this.currentSessionTitle = '';
                this.currentSessionModules = [];
              }
              
              await this.loadSessionList();
            } else {
              this.$message.error('删除失败');
            }
          } catch (error) {
            console.error('删除会话异常:', error);
            this.$message.error('删除失败');
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
        0: '我',
        1: 'AI 助手',
        2: '系统'
      };
      return nameMap[roleType] || '未知';
    },

    getMessageIcon(roleType) {
      const iconMap = {
        0: 'fa fa-user',
        1: 'fa fa-robot',
        2: 'fa fa-info-circle'
      };
      return iconMap[roleType] || 'fa fa-user';
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

        if (diffMins < 1) return '刚刚';
        if (diffMins < 60) return `${diffMins}分钟前`;
        if (diffHours < 24) return `${diffHours}小时前`;
        if (diffDays < 7) return `${diffDays}天前`;
        
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
