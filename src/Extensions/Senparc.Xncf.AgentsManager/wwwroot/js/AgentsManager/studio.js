(function (global) {
  const state = global.AgentStudioState;
  const api = (service, method) => '/api/Senparc.Xncf.AgentsManager/' + service
    + '/Xncf.AgentsManager_' + service + '.' + method;

  global.AgentsStudioMixin = {
    data() {
      return {
        studioSearch: '',
        studioShelf: 'agents',
        studioSelection: { type: '', id: null },
        studioSelectedKeys: [],
        studioDraggingKey: null,
        studioDropGroupId: null,
        studioBusy: false,
        studioError: '',
        studioSceneError: '',
        studioComposerOpen: false,
        studioTeamName: '',
        studioAdminId: null,
        studioEntryId: null,
        studioRunGroupId: null,
        studioCreatedTeamId: null,
        studioTaskName: '',
        studioCommand: '',
        studioModelId: 0,
        studioApproval: false,
        studioModels: [],
        studioTaskDetail: null,
        studioHistories: [],
        studioDetailLoading: false,
        studioHistoryMore: false,
        studioDetailVersion: 0,
        studioHistoryRequesting: false
      };
    },
    computed: {
      studioAgents() {
        const search = this.studioSearch.trim().toLowerCase();
        return (this.agentGraphSnapshot.agents || []).filter(agent =>
          !search || [agent.name, agent.description, agent.agentKind].some(value =>
            String(value || '').toLowerCase().includes(search)));
      },
      studioGroups() {
        return this.buildFilteredAgentGraphSnapshot(this.agentGraphSnapshot).groups.filter(group =>
          !this.studioSearch.trim() || group.name.toLowerCase().includes(this.studioSearch.trim().toLowerCase()));
      },
      studioTasks() {
        return state.tasks(this.agentGraphSnapshot, this.agentGraphFilterGroupId,
          this.agentGraphFilterTaskStatuses, this.studioSearch);
      },
      studioSelectedAgents() {
        return (this.agentGraphSnapshot.agents || []).filter(agent => this.studioSelectedKeys.includes(state.key(agent)));
      },
      studioRoleOptions() {
        return this.studioSelectedAgents.filter(state.roleEligible);
      },
      studioSelectedAgent() {
        return this.studioSelection.type === 'agent'
          ? (this.agentGraphSnapshot.agents || []).find(agent => state.key(agent) === this.studioSelection.id) : null;
      },
      studioSelectedGroup() {
        const groupId = this.studioSelection.type === 'group' ? this.studioSelection.id
          : this.studioSelection.type === 'task'
            ? (this.agentGraphSnapshot.tasks || []).find(task => task.id === this.studioSelection.id)?.groupId : null;
        return (this.agentGraphSnapshot.groups || []).find(group => group.id === groupId);
      },
      studioGroupMembers() {
        return this.studioSelectedGroup ? state.members(this.agentGraphSnapshot, this.studioSelectedGroup.id) : [];
      },
      studioGroupTasks() {
        return this.studioSelectedGroup ? state.tasks(this.agentGraphSnapshot, this.studioSelectedGroup.id) : [];
      },
      studioSelectedTask() {
        return this.studioSelection.type === 'task'
          ? (this.agentGraphSnapshot.tasks || []).find(task => task.id === this.studioSelection.id) : null;
      },
      studioAgentGroups() {
        if (!this.studioSelectedAgent) return [];
        const key = state.key(this.studioSelectedAgent);
        const ids = new Set((this.agentGraphSnapshot.links || [])
          .filter(link => link.participantKey === key).map(link => link.groupId));
        return (this.agentGraphSnapshot.groups || []).filter(group => ids.has(group.id));
      }
    },
    watch: {
      agentGraphSnapshot() {
        const available = new Set((this.agentGraphSnapshot.agents || []).map(state.key));
        this.studioSelectedKeys = this.studioSelectedKeys.filter(key => available.has(key));
        if (!this.studioRoleOptions.some(agent => agent.id === this.studioAdminId)) this.studioAdminId = null;
        if (!this.studioRoleOptions.some(agent => agent.id === this.studioEntryId)) this.studioEntryId = null;
        const selection = this.studioSelection;
        const exists = selection.type === 'agent' ? available.has(selection.id)
          : selection.type === 'group' ? (this.agentGraphSnapshot.groups || []).some(group => group.id === selection.id)
            : selection.type === 'task' ? (this.agentGraphSnapshot.tasks || []).some(task => task.id === selection.id) : true;
        if (!exists) this.studioSelect('', null);
        if (this.studioSelectedTask && !this.studioDetailLoading
          && ([0, 1, 2].includes(this.studioSelectedTask.status)
            || !this.studioTaskDetail || this.studioTaskDetail.status !== this.studioSelectedTask.status)) {
          this.studioLoadTask(false);
        }
      },
      studioSelectedKeys() {
        const first = this.studioRoleOptions[0];
        if (!this.studioRoleOptions.some(agent => agent.id === this.studioAdminId)) this.studioAdminId = first?.id || null;
        if (!this.studioRoleOptions.some(agent => agent.id === this.studioEntryId)) this.studioEntryId = first?.id || null;
      }
    },
    methods: {
      st(key) { return ncfT('AgentsManager.Studio.' + key); },
      studioKey(agent) { return state.key(agent); },
      studioStatus(task) {
        return task.humanPendingCount ? this.st('HumanPending') : this.taskStateText[task.status];
      },
      studioSelect(type, id) {
        if (this.studioSelection.type === type && this.studioSelection.id === id) return;
        this.studioDetailVersion++;
        this.studioSelection = { type, id };
        this.studioTaskDetail = null;
        this.studioHistories = [];
        this.studioHistoryMore = false;
        this.studioDetailLoading = false;
        if (type === 'group') {
          this.agentGraph3d?.focusGroup(id);
        } else if (type === 'task') {
          this.agentGraph3d?.focusGroup(this.studioSelectedGroup?.id || null);
          this.studioLoadTask(true);
        } else {
          this.agentGraph3d?.focusGroup(null);
        }
      },
      studioToggleAgent(agent) {
        if (this.studioBusy || this.studioCreatedTeamId) return;
        const key = state.key(agent);
        if (this.studioSelectedKeys.includes(key)) {
          this.studioSelectedKeys = this.studioSelectedKeys.filter(item => item !== key);
          return;
        }
        if (!agent.enable) {
          this.$message.warning(this.st('ParticipantUnavailable'));
          return;
        }
        this.studioSelectedKeys = this.studioSelectedKeys.concat(key);
      },
      studioDragStart(agent, event) {
        if (this.studioBusy || !agent.enable) {
          event?.preventDefault();
          return;
        }
        this.studioDraggingKey = state.key(agent);
        if (event?.dataTransfer) {
          event.dataTransfer.effectAllowed = 'copy';
          event.dataTransfer.setData('application/x-ncf-agent', this.studioDraggingKey);
          event.dataTransfer.setData('text/plain', this.studioDraggingKey);
        }
      },
      studioDragEnd() {
        this.studioDraggingKey = null;
        this.studioDropGroupId = null;
        this.agentGraph3d?.highlightDropGroup(null);
      },
      studioDropToTeam(key = this.studioDraggingKey) {
        const agent = (this.agentGraphSnapshot.agents || []).find(item => state.key(item) === key);
        if (agent && !this.studioSelectedKeys.includes(state.key(agent))) this.studioToggleAgent(agent);
        this.studioDragEnd();
      },
      async studioRequest(service, method, query = {}, body = null, get = false) {
        const url = api(service, method) + '?' + getInterfaceQueryStr(query);
        const response = get ? await serviceAM.get(url, { customAlert: true })
          : await serviceAM.post(url, body, { customAlert: true });
        if (!response?.data?.success) {
          throw new Error(response?.data?.errorMessage || this.st('RequestFailed'));
        }
        return response.data.data;
      },
      studioReportError(error) {
        console.error('Agents studio operation failed', error);
        this.studioError = error.message || this.st('RequestFailed');
        this.$message.error(this.studioError);
      },
      async studioChangeMember(groupId, key, remove = false) {
        this.studioDragEnd();
        if (this.studioBusy) return;
        if (!groupId || !key) {
          this.$message.info(this.st('DropHint'));
          return;
        }
        if (!remove && (this.agentGraphSnapshot.links || []).some(link =>
          link.groupId === groupId && link.participantKey === key)) {
          this.$message.info(this.st('AlreadyMember'));
          return;
        }
        this.studioBusy = true;
        try {
          const message = await this.studioRequest('ChatGroupAppService', 'SetStudioParticipant',
            { groupId, participantKey: key, remove });
          this.$message.success(message);
          await this.refreshAgentGraphSnapshot(true);
          this.studioSelect('group', groupId);
        } catch (error) {
          this.studioReportError(error);
        } finally {
          this.studioBusy = false;
        }
      },
      async studioOpenComposer(groupId = null) {
        this.studioRunGroupId = groupId;
        if (groupId) this.studioCreatedTeamId = null;
        this.studioComposerOpen = true;
        if (!this.studioModels.length) {
          try {
            const response = await serviceAM.post(
              '/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync',
              { pageIndex: 0, pageSize: 0 }, { customAlert: true });
            if (!response?.data?.success) throw new Error(response?.data?.errorMessage || this.st('RequestFailed'));
            this.studioModels = (response.data.data || []).filter(model => Number(model.configModelType) === 2);
          } catch (error) {
            this.studioReportError(error);
          }
        }
      },
      async studioSubmitTeam(run) {
        if (this.studioBusy) return;
        let groupId = this.studioCreatedTeamId || this.studioRunGroupId;
        if (!groupId && (!this.studioTeamName.trim() || !this.studioSelectedKeys.length
          || !this.studioAdminId || !this.studioEntryId)) {
          this.$message.warning(this.st('TeamRequired'));
          return;
        }
        if (run && (!this.studioTaskName.trim() || !this.studioCommand.trim())) {
          this.$message.warning(this.st('TaskRequired'));
          return;
        }
        this.studioBusy = true;
        try {
          let taskId = null;
          if (!groupId) {
            const group = await this.studioRequest('ChatGroupAppService', 'CreateStudioTeam', {}, {
              name: this.studioTeamName.trim(),
              participantKeys: this.studioSelectedKeys,
              adminAgentTemplateId: this.studioAdminId,
              enterAgentTemplateId: this.studioEntryId
            });
            if (!Number.isInteger(group?.id) || group.id <= 0) throw new Error(this.st('RequestFailed'));
            groupId = group.id;
            // Keep the persisted team if scheduling fails; retry must not create another team.
            this.studioCreatedTeamId = groupId;
          }
          if (run) {
            const started = await this.studioRequest('ChatGroupAppService', 'StartStudioTask', { chatGroupId: groupId }, {
              chatGroupId: groupId, name: this.studioTaskName.trim(), promptCommand: this.studioCommand.trim(),
              aiModelId: this.studioModelId, personality: true, chatMaxRound: 20,
              requireHumanApproval: this.studioApproval, humanInTheLoopLevel: this.studioApproval ? 2 : 0
            });
            if (!Number.isInteger(started?.chatTaskId) || started.chatTaskId <= 0) throw new Error(this.st('RequestFailed'));
            taskId = started.chatTaskId;
          }
          this.studioComposerOpen = false;
          this.studioCreatedTeamId = null;
          this.studioSelectedKeys = [];
          this.studioTeamName = '';
          this.studioTaskName = '';
          this.studioCommand = '';
          this.$message.success(this.st(run ? 'TaskQueued' : 'TeamCreated'));
          await this.refreshAgentGraphSnapshot(true);
          this.studioSelect(taskId ? 'task' : 'group', taskId || groupId);
          this.studioShelf = 'tasks';
        } catch (error) {
          this.studioReportError(error);
          await this.refreshAgentGraphSnapshot(true);
        } finally {
          this.studioBusy = false;
        }
      },
      async studioSetEnabled(type, item) {
        if (this.studioBusy) return;
        this.studioBusy = true;
        try {
          const service = type === 'group' ? 'ChatGroupAppService'
            : item.agentKind === 'RemoteA2A' ? 'RemoteAgentAppService' : 'AgentTemplateAppService';
          await this.studioRequest(service, 'Enable', { id: item.id, enable: !item.enable });
          await this.refreshAgentGraphSnapshot(true);
        } catch (error) {
          this.studioReportError(error);
        } finally {
          this.studioBusy = false;
        }
      },
      async studioEditAgent(agent) {
        try {
          if (agent.agentKind === 'RemoteA2A') {
            await this.getRemoteAgentListData();
            const item = this.remoteAgentList.find(item => item.id === agent.id);
            if (!item) throw new Error(this.st('ParticipantUnavailable'));
            this.openRemoteAgentEditor(item);
          } else {
            const item = await this.studioRequest('AgentTemplateAppService', 'GetItemStatus', { id: agent.id }, null, true);
            await this.handleEditDrawerOpenBtn('drawerAgent', item.agentTemplateStatus);
          }
        } catch (error) {
          this.studioReportError(error);
        }
      },
      async studioLoadTask(reset) {
        const id = this.studioSelection.type === 'task' ? this.studioSelection.id : null;
        if (!id || this.studioDetailLoading || this.studioHistoryRequesting) return;
        const version = this.studioDetailVersion;
        this.studioDetailLoading = true;
        try {
          const detail = await this.studioRequest('ChatTaskAppService', 'GetItem', { id }, null, true);
          if (version !== this.studioDetailVersion) return;
          this.studioTaskDetail = detail.chatTaskDto;
          await this.studioLoadHistory(reset, version);
        } catch (error) {
          if (version === this.studioDetailVersion) this.studioReportError(error);
        } finally {
          if (version === this.studioDetailVersion) this.studioDetailLoading = false;
        }
      },
      async studioLoadHistory(reset = false, version = this.studioDetailVersion) {
        if (this.studioHistoryRequesting || this.studioSelection.type !== 'task') return;
        this.studioHistoryRequesting = true;
        try {
          const lastId = reset ? 0 : (this.studioHistories[this.studioHistories.length - 1]?.id || 0);
          const result = await this.studioRequest('ChatGroupHistoryAppService', 'GetList', {
            chatTaskId: this.studioSelection.id, nextHistoryId: lastId, pageIndex: 1, pageSize: 30
          }, null, true);
          if (version !== this.studioDetailVersion) return;
          const histories = (result.chatGroupHistories || []).map(item => ({
            ...item, messageHtml: this.renderSafeMarkdown(item.message)
          }));
          this.studioHistories = reset ? histories : this.studioHistories.concat(histories);
          this.studioHistoryMore = histories.length === 30;
        } catch (error) {
          if (version === this.studioDetailVersion) this.studioReportError(error);
        } finally {
          this.studioHistoryRequesting = false;
        }
      },
      async studioTaskAction(action) {
        const task = this.studioSelectedTask;
        if (!task || this.studioBusy) return;
        if (action === 'stop') {
          try {
            await this.$confirm(this.st('ConfirmStop'), this.st('Task'), {
              confirmButtonText: this.st('Stop'), cancelButtonText: this.st('Cancel'), type: 'warning'
            });
          } catch (error) {
            if (error !== 'cancel' && error !== 'close') this.studioReportError(error);
            return;
          }
        }
        this.studioBusy = true;
        try {
          const message = await this.studioRequest('ChatTaskAppService',
            action === 'stop' ? 'ForceStop' : 'SetArchiveStatus',
            action === 'stop' ? { id: task.id } : { id: task.id, isArchived: true });
          this.$message.success(message);
          await this.refreshAgentGraphSnapshot(true);
        } catch (error) {
          this.studioReportError(error);
        } finally {
          this.studioBusy = false;
        }
      },
      studioResetCamera() { this.agentGraph3d?.resetCamera(); }
    }
  };
})(window);
