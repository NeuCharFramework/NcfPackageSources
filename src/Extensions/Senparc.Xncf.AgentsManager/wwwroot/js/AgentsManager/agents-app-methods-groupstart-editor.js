/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
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
          roleName: ncfST('Frontend.132'),
          agentTemplateDto: {
            id: chatGroupDto.adminAgentTemplateId,
            name: chatGroupDto.adminAgentTemplateName
          }
        },
        {
          roleName: ncfST('Frontend.133'),
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
          throw new Error(data.errorMessage || data.data || ncfST('Frontend.525'))
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
          this.$message.warning(ncfST('Frontend.526'))
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
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
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
      //Legacy comment translated to English.
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
    //Legacy comment translated to English.
    async handleElVisibleOpenBtn(btnType, formData) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      //Legacy comment translated to English.
      let visibleKey = btnType
      // group start
      if (btnType === 'drawerGroupStart') {
        //Legacy comment translated to English.
        const chatGroup = formData?.chatGroupDto || formData || {}
        const chatGroupId = chatGroup.id || chatGroup.chatGroupId || formData?.chatGroupId || ''
        this.groupStartForm.groupName = chatGroup.name || ''
        this.groupStartForm.name = chatGroup.name ? `${chatGroup.name}1` : ''
        this.groupStartForm.chatGroupId = chatGroupId
        this.groupStartParticipants = this.buildGroupStartParticipants(formData)
        this.groupStartHumanParticipantTouched = false
        this.groupStartForm.includeHumanParticipant = this.groupStartParticipants.some(participant => participant.agentKind === 'Human')
        this.groupStartForm.limitConnectors = false
        this.groupStartForm.connectorInstanceIds = []
        this.groupStartPromptCaretStart = 0
        this.groupStartPromptCaretEnd = 0
        this.visible[visibleKey] = true
        await this.loadGroupStartParticipants(this.groupStartForm.chatGroupId)
        this.loadConnectorOptions()
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
        //Legacy comment translated to English.
        initialSnapshotLoader = Promise.all([
          this.getAgentListData('groupAgent'),
          this.getRemoteAgentListData('groupRemoteAgent')
        ]).catch(() => { })
      }
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        this.agentForm.connectorInstanceIds = []
        this.loadConnectorOptions()
      }
      if (btnType === 'drawerTaskStart') {
        this.groupStartForm.limitConnectors = false
        this.groupStartForm.connectorInstanceIds = []
        this.loadConnectorOptions()
      }
      this.visible[visibleKey] = true
      if (initialSnapshotLoader) {
        await initialSnapshotLoader
      }
      this.$nextTick(() => this.captureEditorFormSnapshot(visibleKey))
    },
    //Legacy comment translated to English.
    handleElVisibleClose(btnType) {
      if (btnType === 'dialogAgentParameter') {
        //Legacy comment translated to English.
        this.agentParameterList = []
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      } else if (btnType === 'dialogTaskDescription') {
        //Legacy comment translated to English.
        this.describeContent = ''
        this.taskDescriptionDetails = null
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      }
      if (!this.getEditorFormName(btnType)) return

      //Legacy comment translated to English.
      if (!this.isEditorFormDirty(btnType)) {
        this.closeEditorForm(btnType)
        return
      }

      this.$confirm(ncfST('Frontend.527'), ncfST('Frontend.528'))
        .then(_ => this.closeEditorForm(btnType))
        .catch(_ => { });
    },
    //Legacy comment translated to English.
    handleElVisibleSubmit(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      let refName = '', formName = ''
      // agent 
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
        formName = 'agentForm'
      }
      // group
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        formName = 'groupForm'
      }
      // group start
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        formName = 'groupStartForm'
      }
      // task evaluation
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
          //Legacy comment translated to English.
          await this.saveSubmitFormData(btnType, submitForm)
          //Legacy comment translated to English.
          //Legacy comment translated to English.
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    //Legacy comment translated to English.
    handleFormValidateField(refFormEL, formName, propName, item) {
      // this[formName][propName] = item
      this.$set(this[formName], `${propName}`, item)
      this.$refs[refFormEL]?.validateField(propName, () => { })
    },

    //Legacy comment translated to English.
    handleIdentify(e) {

      //debugger
      let bRes = this.findDest(e)
      if (bRes) {
        console.log('Matched')
        //Legacy comment translated to English.

      } else {
        console.log('Not matched')
        //Legacy comment translated to English.

      }
      console.log('Recognized event', e);
    },

    handleSystemMessageTypeChange(type) {
      //Legacy comment translated to English.
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
        this.$message.error(ncfST('Frontend.410'))
        return false
      }
      if (binding === 2 && !aiModelId) {
        this.$message.error(ncfST('Frontend.609'))
        return false
      }
      return true
    },

    async convertAgentPromptToPromptRange() {
      const agent = this.agentForm || {}
      const agentTemplateId = Number(agent.id || 0)
      const aiModelId = Number(agent.aiModelId || 0)
      const promptContent = String(agent.systemMessage || '').trim()
      if (!agentTemplateId || agent.isHuman || String(agent.systemMessageType) !== '2' || !promptContent) {
        this.$message.error(ncfST('Frontend.610'))
        return
      }
      if (!this.validateAgentModelBindingForm()) {
        return
      }

      const defaultTestInput = `${ncfST('Frontend.361')} ${agent.name || ncfST('Frontend.005')}.`
      let testInput
      try {
        const inputResult = await this.$prompt(ncfST('Frontend.620'), ncfST('Frontend.621'), {
          inputType: 'textarea',
          inputValue: defaultTestInput,
          inputValidator: value => !!String(value || '').trim(),
          inputErrorMessage: ncfST('Frontend.622')
        })
        testInput = String(inputResult.value || '').trim()
      } catch (_) {
        return
      }

      const defaultExpectedResult = `${ncfST('Frontend.623', agent.name || ncfST('Frontend.005'))} ${agent.description || ncfST('Frontend.624')}`
      let expectedResult
      try {
        const expectedResultInput = await this.$prompt(ncfST('Frontend.625'), ncfST('Frontend.626'), {
          inputType: 'textarea',
          inputValue: defaultExpectedResult,
          inputValidator: value => !!String(value || '').trim(),
          inputErrorMessage: ncfST('Frontend.627')
        })
        expectedResult = String(expectedResultInput.value || '').trim()
      } catch (_) {
        return
      }

      this.agentPromptConversionLoading = true
      try {
        const response = await serviceAM.post(
          '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.ConvertPlainPromptToPromptRange',
          {
            agentTemplateId,
            aiModelId,
            promptContent,
            testInput,
            expectedResult
          })
        const apiResponse = response?.data || {}
        if (!apiResponse.success) {
          throw new Error(apiResponse.message || apiResponse.errorMessage || apiResponse.error || ncfST('Frontend.628'))
        }

        const result = apiResponse.data || {}
        this.$set(this.agentForm, 'systemMessage', result.promptCode)
        this.$set(this.agentForm, 'systemMessageType', '1')
        this.$set(this.agentForm, 'modelBinding', 0)
        this.$set(this.agentForm, 'aiModelId', null)
        await this.getAgentListData('agent')

        const resultPreview = String(result.resultString || '').slice(0, 600)
        this.$alert(
          ncfST('Frontend.631', result.promptCode, result.promptItemVersion, result.score, resultPreview),
          ncfST('Frontend.629'),
          { confirmButtonText: ncfST('Frontend.192'), customClass: 'agent-prompt-conversion-result' })
      } catch (error) {
        const message = error?.response?.data?.message || error?.message || ncfST('Frontend.628')
        this.$message.error(message)
      } finally {
        this.agentPromptConversionLoading = false
      }
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

    //Legacy comment translated to English.
    handleTabsClick(tab, event) {
      if (!this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: this.tabsActiveName }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      // agent
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
      // group
      if (this.tabsActiveName === 'second') {
        this.getGroupListData('group')
      }
      // task
      if (this.tabsActiveName === 'third') {
        this.gettaskListData('task')
      }
      this.syncHashRoute()
    },

    //Legacy comment translated to English.
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
    //Legacy comment translated to English.
    handleFilterCriteria(filterType, fieldType) {
      // agent
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
        // to do invokeinterface
      }
      // group
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
        // to do invokeinterface
      }
      // task
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
        // to do invokeinterface
      }
    },



};

