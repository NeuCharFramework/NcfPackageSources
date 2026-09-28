/*
 * AgentsManager 前端：ChatGroup 启动（参与者/提及）与各类编辑抽屉表单方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsGroupStartEditor = {
    buildGroupStartParticipants(groupDetail) {
      const participants = this.getGroupParticipantList(groupDetail)
        .filter(participant => participant && participant.name)
        .map(participant => Object.assign({}, participant, { roles: [] }))
      const participantByKey = new Map(participants.map(participant => [participant.participantKey, participant]))
      const chatGroupDto = groupDetail?.chatGroupDto || groupDetail || {}
      const fallbackRoleAgents = [
        {
          roleName: '群主',
          agentTemplateDto: {
            id: chatGroupDto.adminAgentTemplateId,
            name: chatGroupDto.adminAgentTemplateName
          }
        },
        {
          roleName: '对接人',
          agentTemplateDto: {
            id: chatGroupDto.enterAgentTemplateId,
            name: chatGroupDto.enterAgentTemplateName
          }
        }
      ]
      const roleAgents = (groupDetail?.roleAgentTemplateDtoList || []).concat(fallbackRoleAgents)

      roleAgents.forEach(role => {
        const agent = role?.agentTemplateDto
        const roleName = (role?.roleName || '').trim()
        if (!agent?.id || !agent?.name || !roleName) return

        const participantKey = `local:${agent.id}`
        let participant = participantByKey.get(participantKey)
        if (!participant) {
          participant = Object.assign({}, agent, {
            participantKey,
            agentKind: 'Local',
            roles: []
          })
          participants.push(participant)
          participantByKey.set(participantKey, participant)
        }
        if (!participant.roles.includes(roleName)) {
          participant.roles.push(roleName)
        }
      })

      return participants
    },
    async loadGroupStartParticipants(chatGroupId) {
      const requestedGroupId = Number(chatGroupId || 0)
      if (!requestedGroupId) return

      this.groupStartParticipantLoading = true
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${requestedGroupId}`)
        const data = response?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载群组成员失败')
        }
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.groupStartParticipants = this.buildGroupStartParticipants(data.data || {})
          if (!this.groupStartHumanParticipantTouched) {
            this.groupStartForm.includeHumanParticipant = this.groupStartParticipants
              .some(participant => participant.agentKind === 'Human')
          }
          await this.autoTestRemoteParticipants(this.groupStartParticipants)
        }
      } catch (error) {
        console.warn('loadGroupStartParticipants failed', error)
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.$message.warning('无法刷新完整组员列表，当前仅显示已加载的组员')
        }
      } finally {
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.groupStartParticipantLoading = false
        }
      }
    },
    markGroupStartHumanParticipantTouched() {
      this.groupStartHumanParticipantTouched = true
    },
    getGroupStartPromptTextarea() {
      const input = this.$refs?.groupStartPromptCommand
      return input?.$refs?.textarea
        || input?.textarea
        || input?.$el?.querySelector?.('textarea')
        || null
    },
    rememberGroupStartPromptCaret(event) {
      const textarea = event?.target?.tagName === 'TEXTAREA'
        ? event.target
        : this.getGroupStartPromptTextarea()
      if (!textarea) return

      this.groupStartPromptCaretStart = Number.isInteger(textarea.selectionStart)
        ? textarea.selectionStart
        : (this.groupStartForm.promptCommand || '').length
      this.groupStartPromptCaretEnd = Number.isInteger(textarea.selectionEnd)
        ? textarea.selectionEnd
        : this.groupStartPromptCaretStart
    },
    insertGroupStartMention(participant) {
      const participantName = (participant?.name || '').trim()
      if (!participantName) return

      const textarea = this.getGroupStartPromptTextarea()
      const promptCommand = this.groupStartForm.promptCommand || ''
      const start = Number.isInteger(textarea?.selectionStart)
        ? textarea.selectionStart
        : Math.min(this.groupStartPromptCaretStart, promptCommand.length)
      const end = Number.isInteger(textarea?.selectionEnd)
        ? textarea.selectionEnd
        : Math.min(Math.max(start, this.groupStartPromptCaretEnd), promptCommand.length)
      const before = promptCommand.slice(0, start)
      const after = promptCommand.slice(end)
      const prefix = before && !/\s$/.test(before) ? ' ' : ''
      const suffix = after && !/^\s/.test(after) ? ' ' : ''
      const mention = `${prefix}@${participantName}${suffix}`
      const caretPosition = before.length + mention.length

      this.groupStartForm.promptCommand = `${before}${mention}${after}`
      this.groupStartPromptCaretStart = caretPosition
      this.groupStartPromptCaretEnd = caretPosition
      this.$nextTick(() => {
        const currentTextarea = this.getGroupStartPromptTextarea()
        currentTextarea?.focus?.()
        currentTextarea?.setSelectionRange?.(caretPosition, caretPosition)
      })
    },
    getEditorFormName(btnType) {
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) return 'agentForm'
      if (btnType === 'drawerGroup') return 'groupForm'
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) return 'groupStartForm'
      if (btnType === 'dialogTaskEvaluation') return 'evaluationForm'
      return ''
    },
    getEditorVisibleKey(btnType) {
      return btnType === 'drawerTaskStart' ? 'drawerGroupStart' : btnType
    },
    normalizeEditorSnapshotValue(value, fieldName = '') {
      if (Array.isArray(value)) {
        const normalized = value.map(item => this.normalizeEditorSnapshotValue(item))
        // Group 成员的选择顺序不影响实际保存结果，避免控件回填时造成伪变更。
        if (['members', 'remoteMembers'].includes(fieldName)) {
          return normalized.sort((left, right) => {
            const leftKey = `${left?.id ?? ''}:${left?.name ?? ''}`
            const rightKey = `${right?.id ?? ''}:${right?.name ?? ''}`
            return leftKey.localeCompare(rightKey)
          })
        }
        return normalized
      }
      if (value && typeof value === 'object') {
        return Object.keys(value)
          .sort()
          .reduce((result, key) => {
            // 这两个字段在提交时由成员列表派生，不属于用户编辑内容。
            if (['memberAgentTemplateIds', 'remoteAgentIds'].includes(key)) return result
            result[key] = this.normalizeEditorSnapshotValue(value[key], key)
            return result
          }, {})
      }
      return typeof value === 'undefined' ? null : value
    },
    buildEditorFormSnapshot(btnType) {
      const formName = this.getEditorFormName(btnType)
      if (!formName) return ''
      const snapshot = { form: this[formName] || {} }
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        snapshot.functionCallTags = this.functionCallTags || []
      }
      return JSON.stringify(this.normalizeEditorSnapshotValue(snapshot))
    },
    captureEditorFormSnapshot(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      if (!this.visible[visibleKey]) return
      this.$set(this.editorFormInitialSnapshots, visibleKey, this.buildEditorFormSnapshot(btnType))
    },
    isEditorFormDirty(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      const original = this.editorFormInitialSnapshots[visibleKey]
      // 未取得初始快照时保守处理，避免误丢弃刚刚编辑的内容。
      return !original || original !== this.buildEditorFormSnapshot(btnType)
    },
    closeEditorForm(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      const formName = this.getEditorFormName(btnType)
      let refName = ''

      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
      }
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
        this.groupAgentList = []
        this.$set(this, 'groupRemoteAgentQueryList', this.$options.data().groupRemoteAgentQueryList)
        this.groupRemoteAgentList = []
      }
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        this.groupStartParticipants = []
        this.groupStartParticipantLoading = false
        this.groupStartHumanParticipantTouched = false
        this.groupStartPromptCaretStart = 0
        this.groupStartPromptCaretEnd = 0
      }
      if (btnType === 'dialogTaskEvaluation') {
        refName = 'evaluationELForm'
      }

      if (formName) {
        this.$set(this, formName, this.$options.data()[formName])
      }
      this.$refs[refName]?.resetFields?.()
      delete this.editorFormInitialSnapshots[visibleKey]
      this.$nextTick(() => {
        this.visible[visibleKey] = false
      })

      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        this.functionCallTags = []
        this.functionCallInputVisible = false
        this.functionCallInputValue = ''
        this.agentAutoAttachXncf = false
        this.agentSystemMessageTypeDetectionPending = false
      }
    },
    // Dailog|抽屉 打开 按钮
    async handleElVisibleOpenBtn(btnType, formData) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      // console.log('通用新增按钮:', btnType);
      let visibleKey = btnType
      // 组 启动
      if (btnType === 'drawerGroupStart') {
        // 详情: formData.chatGroupDto 列表: formData
        const chatGroup = formData?.chatGroupDto || formData || {}
        const chatGroupId = chatGroup.id || chatGroup.chatGroupId || formData?.chatGroupId || ''
        this.groupStartForm.groupName = chatGroup.name || ''
        this.groupStartForm.name = chatGroup.name ? `${chatGroup.name}1` : ''
        this.groupStartForm.chatGroupId = chatGroupId
        this.groupStartParticipants = this.buildGroupStartParticipants(formData)
        this.groupStartHumanParticipantTouched = false
        this.groupStartForm.includeHumanParticipant = this.groupStartParticipants.some(participant => participant.agentKind === 'Human')
        this.groupStartPromptCaretStart = 0
        this.groupStartPromptCaretEnd = 0
        this.visible[visibleKey] = true
        await this.loadGroupStartParticipants(this.groupStartForm.chatGroupId)
        this.$nextTick(() => this.captureEditorFormSnapshot(visibleKey))
        return
      }
      if (btnType === 'drawerTaskStart') {
        visibleKey = 'drawerGroupStart'
        const chatGroupId = this.getGroupStartChatGroupId(this.groupStartForm)
        if (!this.groupStartForm.groupName && chatGroupId) {
          const groupDetail = this.groupDetails?.chatGroupDto
            || this.agentDetailsGroupDetails?.chatGroupDto
            || {}
          this.groupStartForm.groupName = groupDetail.name || ''
        }
      }
      let initialSnapshotLoader = null
      if (btnType === 'drawerGroup') {
        // 成员选择控件会在列表到达后回填；待回填完成再建立快照，避免误判为用户修改。
        initialSnapshotLoader = Promise.all([
          this.getAgentListData('groupAgent'),
          this.getRemoteAgentListData('groupRemoteAgent')
        ]).catch(() => { })
      }
      this.visible[visibleKey] = true
      if (initialSnapshotLoader) {
        await initialSnapshotLoader
      }
      this.$nextTick(() => this.captureEditorFormSnapshot(visibleKey))
    },
    // Dailog|抽屉 关闭 按钮
    handleElVisibleClose(btnType) {
      if (btnType === 'dialogAgentParameter') {
        // 清空数据
        this.agentParameterList = []
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      } else if (btnType === 'dialogTaskDescription') {
        // 清空数据
        this.describeContent = ''
        this.taskDescriptionDetails = null
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      }
      if (!this.getEditorFormName(btnType)) return

      // 没有任何表单修改时直接关闭；仅在可能丢弃用户输入时询问。
      if (!this.isEditorFormDirty(btnType)) {
        this.closeEditorForm(btnType)
        return
      }

      this.$confirm('当前修改尚未保存，确认关闭？')
        .then(_ => this.closeEditorForm(btnType))
        .catch(_ => { });
    },
    // Dailog|抽屉 提交 按钮
    handleElVisibleSubmit(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      let refName = '', formName = ''
      // 智能体 
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
        formName = 'agentForm'
      }
      // 组
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        formName = 'groupForm'
      }
      // 组 启动
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        formName = 'groupStartForm'
      }
      // 任务评价
      if (btnType === 'dialogTaskEvaluation') {
        refName = 'evaluationELForm'
        formName = 'evaluationForm'
      }
      if (!refName) return
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)
        && !this.validateAgentModelBindingForm()) {
        return
      }
      this.$refs[refName].validate(async (valid) => {
        if (valid) {
          const submitForm = this[formName] ?? {}
          //提交数据给后端
          await this.saveSubmitFormData(btnType, submitForm)
          // “再次执行/启动任务”的跳转交给 saveSubmitFormData 中刷新后的定位逻辑，
          // 避免这里用旧缓存任务二次覆盖到历史任务。
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    // 表单 单条校验
    handleFormValidateField(refFormEL, formName, propName, item) {
      // this[formName][propName] = item
      this.$set(this[formName], `${propName}`, item)
      this.$refs[refFormEL]?.validateField(propName, () => { })
    },

    // 识别事件
    handleIdentify(e) {

      //debugger
      let bRes = this.findDest(e)
      if (bRes) {
        console.log('命中')
        //自动选出PromptRange（不做处理）

      } else {
        console.log('未命中')
        //TODO:默认成为新的提示词，zai

      }
      console.log('识别事件', e);
    },

    handleSystemMessageTypeChange(type) {
      // 用户显式切换时，应以用户的选择为准，不再让异步加载结果覆盖它。
      this.agentSystemMessageTypeDetectionPending = false
      if (String(type) === '2') {
        this.$set(this.agentForm, 'modelBinding', 2)
      }
    },

    handleAgentModelBindingChange(value) {
      if (Number(value) !== 2) {
        this.$set(this.agentForm, 'aiModelId', null)
      }
    },

    validateAgentModelBindingForm() {
      const isManualPrompt = String(this.agentForm?.systemMessageType || '') === '2'
      const binding = Number(this.agentForm?.modelBinding ?? 0)
      const aiModelId = Number(this.agentForm?.aiModelId || 0)
      if (isManualPrompt && binding !== 2) {
        this.$message.error('手动 Prompt 没有 PromptRange 模型可继承，请选择“手动选择 AIModel”。')
        return false
      }
      if (binding === 2 && !aiModelId) {
        this.$message.error('手动选择 AIModel 时必须选择一个 Chat 类型模型。')
        return false
      }
      return true
    },

    handleSystemMessageOptionsLoaded(options) {
      if (!this.agentSystemMessageTypeDetectionPending) {
        return
      }

      const systemMessage = typeof this.agentForm?.systemMessage === 'string'
        ? this.agentForm.systemMessage.trim()
        : String(this.agentForm?.systemMessage ?? '').trim()
      const selectedFromPromptRange = (options || []).some(option =>
        String(option?.value ?? '').trim() === systemMessage)

      this.agentSystemMessageTypeDetectionPending = false
      this.$set(this.agentForm, 'systemMessageType', selectedFromPromptRange ? '1' : '2')
    },

    // 切换 tabs 页面
    handleTabsClick(tab, event) {
      if (!this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: this.tabsActiveName }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      // 智能体
      if (this.tabsActiveName === 'first') {
        this.getAgentListData('agent')
        if (this.agentListViewMode === 'three' && this.scrollbarAgentIndex === '') {
          this.$nextTick(() => {
            this.ensureAgentGraph3d()
            this.refreshAgentGraphSnapshot(true)
            this.startAgentGraphPolling()
          })
        }
      }
      // 组
      if (this.tabsActiveName === 'second') {
        this.getGroupListData('group')
      }
      // 任务
      if (this.tabsActiveName === 'third') {
        this.gettaskListData('task')
      }
      this.syncHashRoute()
    },

    // 筛选输入变化
    handleFilterChange(value, filterType) {
      console.log('handleFilterChange', filterType, value)
      if (filterType === 'agent') {

        this.agentQueryList.filter = value
        this.getAgentListData('agent', 1)
        if (this.agentListViewMode === 'three' && this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
          this.refreshAgentGraphSnapshot(true)
        }
      }
      if (filterType === 'groupAgent') {
        this.groupAgentQueryList.filter = value
        this.getAgentListData('groupAgent', 1)
      }
      if (filterType === 'group') {
        this.groupQueryList.filter = value
        this.getGroupListData('group', 1)
      }
      if (filterType === 'agentGroup') {
        this.agentDetailsGroupQueryList.filter = value
        this.getGroupListData('agentGroup', 1)
      }
      if (filterType === 'agentTask') {
        this.agentDetailsTaskQueryList.filter = value
        this.gettaskListData('agentTask', 1)
      }
      if (filterType === 'task') {
        this.taskQueryList.filter = value
        this.gettaskListData('task', 1)
      }
    },
    // 筛选条件事件 agent  group task
    handleFilterCriteria(filterType, fieldType) {
      // 智能体
      if (filterType === 'agent') {
        if (fieldType === 'timeSort') {
          this.agentQueryList.timeSort = !this.agentQueryList.timeSort
        } else {
          this.agentFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.agentQueryList[item.value] = true
            } else {
              this.agentQueryList[item.value] = item.checked
            }
          })
        }
        // this.agentFCPVisible = !this.agentFCPVisible
        // to do 调用接口
      }
      // 组
      if (filterType === 'group') {
        if (fieldType === 'timeSort') {
          this.groupQueryList.timeSort = !this.groupQueryList.timeSort
        } else {
          this.groupFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.groupQueryList[item.value] = true
            } else {
              this.groupQueryList[item.value] = item.checked
            }
          })
        }
        // this.groupFCPVisible = !this.groupFCPVisible
        // to do 调用接口
      }
      // 任务
      if (filterType === 'task') {
        if (fieldType === 'timeSort') {
          this.taskQueryList.timeSort = !this.taskQueryList.timeSort
        } else {
          this.taskFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.taskQueryList[item.value] = true
            } else {
              this.taskQueryList[item.value] = item.checked
            }
          })
        }
        // this.taskFCPVisible = !this.taskFCPVisible
        // to do 调用接口
      }
    },



};

