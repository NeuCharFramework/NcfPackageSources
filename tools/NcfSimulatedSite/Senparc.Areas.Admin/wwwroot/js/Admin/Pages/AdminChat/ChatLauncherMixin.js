window.ChatLauncherMixin = {
  data() {
    return {
      chatInputText: '',
      selectedModules: [],
      moduleSelectorVisible: false,
      moduleSearchKeyword: '',
      availableModules: [],
      selectedModuleUids: [],
      loadingModuleOptions: false,
      isCreatingSession: false
    };
  },
  computed: {
    filteredAvailableModules() {
      const keyword = (this.moduleSearchKeyword || '').trim().toLowerCase();
      if (!keyword) {
        return this.availableModules;
      }

      return this.availableModules.filter((item) => {
        return (
          (item.name || '').toLowerCase().includes(keyword) ||
          (item.description || '').toLowerCase().includes(keyword) ||
          (item.uid || '').toLowerCase().includes(keyword)
        );
      });
    },
    isAllFilteredSelected() {
      if (this.filteredAvailableModules.length === 0) {
        return false;
      }

      return this.filteredAvailableModules.every((item) => this.selectedModuleUids.includes(item.uid));
    },
    moduleSelectionIndeterminate() {
      if (this.filteredAvailableModules.length === 0) {
        return false;
      }

      const selectedCount = this.filteredAvailableModules.filter((item) => this.selectedModuleUids.includes(item.uid)).length;
      return selectedCount > 0 && selectedCount < this.filteredAvailableModules.length;
    }
  },
  methods: {
    handleLauncherInputKeydown(event) {
      if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        this.startChatSession();
      }
    },

    normalizeModuleItem(item) {
      return {
        uid: item.uid,
        name: item.menuName || item.name || '未命名模块',
        icon: item.icon || 'fa fa-cube',
        description: item.description || '暂无描述',
        version: item.version || '',
        menus: item.menus || []
      };
    },

    async ensureModuleOptionsLoaded(forceReload) {
      if (!forceReload && this.availableModules.length > 0) {
        return;
      }

      this.loadingModuleOptions = true;
      try {
        const response = await service.get('/Admin/Index?handler=XncfOpening');
        const moduleList = (response.data && response.data.data) ? response.data.data : [];
        this.availableModules = moduleList.map((item) => this.normalizeModuleItem(item));
      } catch (error) {
        console.error('加载模块列表失败:', error);
        this.$message.error('加载模块列表失败，请稍后重试');
      } finally {
        this.loadingModuleOptions = false;
      }
    },

    async openModuleSelector() {
      await this.ensureModuleOptionsLoaded(false);
      this.selectedModuleUids = this.selectedModules.map((item) => item.uid);
      this.moduleSelectorVisible = true;
    },

    toggleModuleSelection(uid) {
      const index = this.selectedModuleUids.indexOf(uid);
      if (index >= 0) {
        this.selectedModuleUids.splice(index, 1);
      } else {
        this.selectedModuleUids.push(uid);
      }
    },

    handleSelectAllFiltered(checked) {
      const filteredUids = this.filteredAvailableModules.map((item) => item.uid);
      if (checked) {
        const merged = new Set(this.selectedModuleUids.concat(filteredUids));
        this.selectedModuleUids = Array.from(merged);
      } else {
        this.selectedModuleUids = this.selectedModuleUids.filter((uid) => !filteredUids.includes(uid));
      }
    },

    applyModuleSelection() {
      const uidSet = new Set(this.selectedModuleUids);
      this.selectedModules = this.availableModules.filter((item) => uidSet.has(item.uid));
      this.moduleSelectorVisible = false;
    },

    clearModuleSelectionInDialog() {
      this.selectedModuleUids = [];
    },

    removeModule(uid) {
      this.selectedModules = this.selectedModules.filter((item) => item.uid !== uid);
    },

    clearSelectedModules() {
      this.selectedModules = [];
    },

    async startChatSession() {
      if (!this.chatInputText || this.chatInputText.trim().length === 0) {
        this.$message.warning('请输入对话内容');
        return;
      }

      this.isCreatingSession = true;
      try {
        const requestData = {
          initialMessage: this.chatInputText.trim(),
          moduleUids: this.selectedModules.map((item) => item.uid)
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync', requestData);
        if (response.data && response.data.success && response.data.data) {
          const sessionId = response.data.data.sessionId;
          window.location.href = '/Admin/AdminChat/Chat?sessionId=' + sessionId;
          return;
        }

        this.$message.error((response.data && response.data.errorMessage) || '创建会话失败');
      } catch (error) {
        console.error('创建会话失败:', error);
        this.$message.error('创建会话失败，请稍后重试');
      } finally {
        this.isCreatingSession = false;
      }
    }
  }
};
