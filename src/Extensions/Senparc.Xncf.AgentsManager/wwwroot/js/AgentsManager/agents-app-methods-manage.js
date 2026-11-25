/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

var AgentsAppMethodsManage = {
    //Legacy comment translated to English.
    handleAgentViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: null }))
        return
      }
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = '' //Legacy comment translated to English.
      this.agentDetails = '' // clear detail data
      this.getAgentListData('agent')
      if (this.agentListViewMode === 'three') {
        this.$nextTick(() => {
          this.ensureAgentGraph3d()
          this.refreshAgentGraphSnapshot(true)
          this.startAgentGraphPolling()
        })
      }
      this.syncHashRoute({ tab: 'first', agentId: null })
    },
    //Legacy comment translated to English.
    handleAgentView(item, index, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: item.id }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      this.scrollbarAgentIndex = item.id ?? ''
      //Legacy comment translated to English.
      this.resetAgentDetailsQuery()
      //Legacy comment translated to English.
      this.getAgentDetailData(item.id, item)
      //Legacy comment translated to English.
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', item.id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', item.id)
      }
      this.syncHashRoute({ tab: 'first', agentId: item.id })
    },
    //Legacy comment translated to English.
    resetAgentDetailsQuery() {
      this.invalidateTaskDetailRequest('agentGroupTask')
      this.invalidateTaskDetailRequest('agentTask')
      this.agentDetailsTabsActiveName = 'first'
      this.agentDetailsGroupList = []
      this.agentDetailsGroupShowType = '1'
      this.agentDetailsGroupIndex = 0
      this.agentDetailsGroupDetails = ''
      this.agentGroupTaskSelection = []
      this.agentDetailsGroupTaskList = []
      this.agentDetailsGroupTaskHistoryList = []
      this.agentGroupTaskMemberfilter = ''
      this.agentGroupTaskMemberfilterList = []
      this.agentDetailsGroupDetailsTaskDetails = ''
      this.agentDetailsTaskIndex = 0
      this.agentDetailsTaskList = []
      this.agentDetailsTaskDetails = ''
      this.agentDetailsTaskHistoryList = []
      this.agentDetailsTaskMemberList = []
      this.agentTaskMemberfilter = ''
      this.agentTaskMemberfilterList = []
      this.$set(this, 'agentDetailsGroupTaskQueryList', this.$options.data().agentDetailsGroupTaskQueryList)
      this.$set(this, 'agentDetailsGroupQueryList', this.$options.data().agentDetailsGroupQueryList)
      this.$set(this, 'agentDetailsTaskQueryList', this.$options.data().agentDetailsTaskQueryList)
    },
    //Legacy comment translated to English.
    handleAgentDetailsTabsClick(tab, event) {
      this.clearHistoryTimer()
      let id = ''
      if (this.agentDetails) {
        id = this.agentDetails.agentTemplateDto ? this.agentDetails.agentTemplateDto.id : this.agentDetails.id
      }
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', id)
      }
    },
    //Legacy comment translated to English.
    setAgentState(stateType, item) {
      if (!stateType || !item) return
      let messageText = ''
      let serviceURL = ''
      if (stateType === 'stop') {
        let itemData = item.agentTemplateDto || item
        serviceURL = `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.Enable?id=${itemData.id}&enable=${!itemData.enable}`

        if (itemData.enable) {
          messageText = `<div>${ncfST('Frontend.AgentsManager.DisableAgentValue', itemData.name)}</div><div>${ncfST('Frontend.AgentsManager.ThisAgentWillNotTakePartInLaterNewTasks')}</div>`
        } else {
          messageText = `<div>${ncfST('Frontend.AgentsManager.EnableAgentValue', itemData.name)}</div>`
        }
      }
      if (stateType === 'delete') {
        messageText = `<div>${ncfST('Frontend.AgentsManager.LeaveGroupValue', item.name)}</div><div>${ncfST('Frontend.AgentsManager.AfterRemovalTaskRecordsYouPreviouslyTookPartInWillNoLonger')}</div>`
      }
      if (!serviceURL) return
      this.$confirm(messageText, ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            if (stateType === 'stop') {
              // const agentMapStr = {
              //     'drawerAgent': 'agent',
              //     'dialogGroupAgent': 'groupAgent'
              // }
              this.getAgentListData('agent')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })

      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },
    //Legacy comment translated to English.
    async setGroupState(item) {
      const group = item?.chatGroupDto || item
      if (!group || !group.id) return

      const willEnable = !group.enable
      const messageText = willEnable
        ? `<div>${ncfST('Frontend.AgentsManager.ConfirmEnablingGroupValue', group.name)}</div><div>${ncfST('Frontend.AgentsManager.AfterEnablingNewTasksCanBeCreatedAndTheGroupCanBe')}</div>`
        : `<div>${ncfST('Frontend.AgentsManager.ConfirmDisablingGroupValue', group.name)}</div><div>${ncfST('Frontend.AgentsManager.AfterDisablingNoNewTasksWillBeCreatedAndTheGroupCannot')}</div>`

      try {
        await this.$confirm(messageText, ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
          dangerouslyUseHTMLString: true,
          confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
          cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
          type: 'warning'
        })

        const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Enable?id=${group.id}&enable=${willEnable}`
        const res = await serviceAM.post(serviceURL)
        if (!res?.data?.success) {
          throw new Error(res?.data?.errorMessage || res?.data?.data || ncfST('Frontend.AgentsManager.OperationFailed'))
        }

        this.$message({
          type: 'success',
          message: ncfST('Frontend.AgentsManager.GroupValueHasBeenValue', group.name, ncfST(willEnable ? 'Frontend.AgentsManager.EnabledSupplemental' : 'Frontend.AgentsManager.DisabledSupplemental'))
        })
        if (this.tabsActiveName === 'second') {
          await this.getGroupListData('group')
        } else if (this.agentDetails?.agentTemplateDto?.id) {
          await this.getGroupListData('agentGroup', this.agentDetails.agentTemplateDto.id)
        }
        await this.refreshAgentGraphSnapshot(this.agentListViewMode === 'three')
      } catch (error) {
        if (error === 'cancel' || error === 'close') {
          this.$message({ type: 'info', message: ncfST('Frontend.AgentsManager.OperationCanceled') })
          return
        }
        this.$message({
          message: error?.message || ncfST('Frontend.AgentsManager.OperationFailed'),
          type: 'error',
          duration: 5 * 1000
        })
      }
    },

    handleAgentDelete(item, { closeEditor = false } = {}) {
      const itemData = item?.agentTemplateDto || item
      if (!itemData || !itemData.id) return

      const groupQuery = {
        agentTemplateId: 0,
        pageIndex: 0,
        pageSize: 0,
        filter: ''
      }
      const memberGroupQuery = {
        agentTemplateId: itemData.id,
        pageIndex: 0,
        pageSize: 0,
        filter: ''
      }

      const serviceURL = `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.Delete?id=${itemData.id}`

      Promise.all([
        serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(groupQuery)}`, groupQuery),
        serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(memberGroupQuery)}`, memberGroupQuery)
      ]).then(([allGroupRes, memberGroupRes]) => {
        const allGroupList = allGroupRes?.data?.data?.chatGroupDtoList ?? []
        const memberGroupList = memberGroupRes?.data?.data?.chatGroupDtoList ?? []

        const adminGroups = allGroupList.filter(group => group.adminAgentTemplateId === itemData.id).map(group => group.name)
        const enterGroups = allGroupList.filter(group => group.enterAgentTemplateId === itemData.id).map(group => group.name)

        if (adminGroups.length || enterGroups.length) {
          const blockedMessage = [
            `<div>${ncfST('Frontend.AgentsManager.AgentValueCannotBeDeletedRightNow', itemData.name)}</div>`,
            adminGroups.length ? `<div style="margin-top:6px;">${ncfST('Frontend.AgentsManager.GroupsAsOwnerValue', adminGroups.join(', '))}</div>` : '',
            enterGroups.length ? `<div style="margin-top:6px;">${ncfST('Frontend.AgentsManager.GroupsAsContactValue', enterGroups.join(', '))}</div>` : '',
            `<div style="margin-top:8px;color:#E6A23C;">${ncfST('Frontend.AgentsManager.ReplaceTheOwnerContactInTheCorrespondingGroupBeforeDeleting')}</div>`
          ].join('')

          this.$alert(blockedMessage, ncfST('Frontend.AgentsManager.DeletionBlocked'), {
            dangerouslyUseHTMLString: true,
            confirmButtonText: ncfST('Frontend.AgentsManager.GotIt')
          })
          return
        }

        const memberGroups = memberGroupList.map(group => group.name)
        const previewMessage = [
          `<div>${ncfST('Frontend.AgentsManager.DeleteAgentValue', itemData.name)}</div>`,
          memberGroups.length
            ? `<div style="margin-top:6px;">${ncfST('Frontend.AgentsManager.GroupsTheMemberWillLeaveValue', memberGroups.join(', '))}</div>`
            : `<div style="margin-top:6px;">${ncfST('Frontend.AgentsManager.ThisAgentIsNotCurrentlyAMemberOfAnyGroup')}</div>`,
          `<div style="margin-top:8px;">${ncfST('Frontend.AgentsManager.MessageHistoryRelatedToThisAgentWillAlsoBeDeletedAndCannot')}</div>`
        ].join('')

        this.$confirm(previewMessage, ncfST('Frontend.AgentsManager.ConfirmAction'), {
          dangerouslyUseHTMLString: true,
          confirmButtonText: ncfST('Frontend.AgentsManager.DeleteAgent'),
          cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
          type: 'warning'
        }).then(() => {
          serviceAM.post(serviceURL).then(res => {
            if (res.data.success) {
              this.$message({
                type: 'success',
                message: ncfST('Frontend.AgentsManager.DeletedSuccessfullyEmphasis')
              })
              if (closeEditor) {
                this.visible.drawerAgent = false
              }
              this.handleAgentViewAll()
            } else {
              app.$message({
                message: res.data.errorMessage || res.data.data || 'Error',
                type: 'error',
                duration: 5 * 1000
              })
            }
          })
        }).catch(() => {
          this.$message({
            type: 'info',
            message: ncfST('Frontend.AgentsManager.OperationCanceled')
          })
        })
      }).catch(() => {
        app.$message({
          message: ncfST('Frontend.AgentsManager.DeletionPreCheckFailedPleaseTryAgainLater'),
          type: 'error',
          duration: 5 * 1000
        })
      })
    },



    //Legacy comment translated to English.
    filterGroupTreeNode(value, data) {
      if (!value) return true;
      return data.name.indexOf(value) !== -1;
    },
    //Legacy comment translated to English.
    handleGroupTreeNodeClick(node, data, clickType) {
      // console.log('handleGroupTreeNodeClick', node, data)
      if (clickType === 'agentGroup') {
        this.agentDetailsGroupShowType = node.level.toString()
        if (this.agentDetailsGroupShowType === '1') {
          this.agentDetailsGroupDetails = deepClone(data)
        }
        if (this.agentDetailsGroupShowType === '2') {
          this.agentDetailsGroupDetails = deepClone(data)
        }
      }
      if (clickType === 'group') {
        this.groupShowType = node.level.toString()
        if (this.groupShowType === '1') {
          this.groupDetails = '' //Legacy comment translated to English.
        }
        if (this.groupShowType === '2') {
          this.groupDetails = deepClone(data)
        }
        if (this.groupShowType === '3') {
          this.groupDetails = deepClone(data)
        }
      }
    },
    //Legacy comment translated to English.
    handleGroupDetail(row, clickType) {
      // console.log('handleGroupDetail', row)
      if (clickType === 'agentGroup') {
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupDetails = deepClone(row)
      }
      if (clickType === 'group') {
        this.groupShowType = '2'
        this.groupDetails = deepClone(row)
      }
    },
    //Legacy comment translated to English.
    handleGroupViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'second' })
        return
      }
      this.clearHistoryTimer()
      this.groupShowType = '1'
      //Legacy comment translated to English.
      this.scrollbarGroupIndex = '' //Legacy comment translated to English.
      this.groupDetails = ''
      this.groupTaskSelection = []
      this.groupTaskList = []
      this.groupTaskDetails = ''
      this.groupTaskHistoryList = []
      this.groupTaskMemberList = []
      this.groupSelection = []
      this.groupTaskMemberfilter = ''
      this.groupTaskMemberfilterList = []
      this.getGroupListData('group')
      this.syncHashRoute({ tab: 'second', groupId: null, taskId: null })
    },
    //Legacy comment translated to English.
    handleGroupView(clickType, item, index = 0, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && (clickType === 'group' || clickType === 'groupTable')) {
        this.navigateByHash({ tab: 'second', groupId: item.id })
        return
      }
      this.clearHistoryTimer()
      //Legacy comment translated to English.
      if (clickType === 'agentGroup') {
        //Legacy comment translated to English.
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupIndex = index ?? 0
        //Legacy comment translated to English.
        this.agentDetailsGroupDetails = ''
        this.agentGroupTaskSelection = []
        this.agentDetailsGroupTaskList = []
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentDetailsGroupTaskMemberList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getGroupDetailData(clickType, item.id, item)
      }
      //Legacy comment translated to English.
      if (clickType === 'group' || clickType === 'groupTable') {
        //Legacy comment translated to English.
        this.groupShowType = '2'
        // if (clickType === 'groupTable') {
        //     const { pageIndex, pageSize } = this.groupQueryList
        //     this.scrollbarGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
        // } else {
        //     this.scrollbarGroupIndex = index ?? 0
        // }
        this.scrollbarGroupIndex = item.id ?? ''
        //Legacy comment translated to English.
        this.groupDetails = ''
        this.groupTaskSelection = []
        this.groupTaskList = []
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
        this.groupTaskMemberList = []
        this.groupTaskMemberfilter = ''
        this.groupTaskMemberfilterList = []
        this.getGroupDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'second', groupId: item.id, taskId: null })
      }
    },
    //Legacy comment translated to English.
    toggleSelection(rows) {
      if (rows) {
        rows.forEach(row => {
          this.$refs?.groupAgentTable?.toggleRowSelection(row);
        });
      } else {
        this.$refs?.groupAgentTable?.clearSelection();
      }
    },
    //Legacy comment translated to English.
    handleSelectionChange(val) {
      if (!this.isGetGroupAgent) {
        const selectedIds = new Set(val.map((i) => i.id))
        const spliceList = this.groupAgentList.filter(
          (item) => !selectedIds.has(item.id)
        )
        const pushList = this.groupAgentList.filter((item) =>
          selectedIds.has(item.id)
        )
        pushList.forEach((item) => {
          const index = this.groupForm.members.findIndex(
            (i) => i.id === item.id
          )
          if (index === -1) {
            this.groupForm.members.push(item)
          } else {
            this.groupForm.members.splice(index, 1, item)
          }
        })

        spliceList.forEach((item) => {
          const index = this.groupForm.members.findIndex(
            (i) => i.id === item.id
          )
          if (index !== -1) {
            this.groupForm.members.splice(index, 1)
          }
        })
      }

    },
    //Legacy comment translated to English.
    groupMembersCancel(item, index) {
      this.groupForm.members.splice(index, 1);
      const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
      if (findIndex !== -1) {
        this.toggleSelection([this.groupAgentList[findIndex]])
      }
    },
    //Legacy comment translated to English.
    handleGroupSelectionChange(val) {
      this.groupSelection = val
    },
    // group delete
    handleGroupDelete(optype, row) {
      console.log('handleGroupDelete:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm(ncfST('Frontend.AgentsManager.DeleteThisData'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            if (optype === 'groupTable') {
              // reload data
              this.getGroupListData('group')
            } else {
              //Legacy comment translated to English.
              this.handleGroupViewAll()
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },
    //Legacy comment translated to English.
    handleGroupDeleteBatch() {
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      const selectedIds = (this.groupSelection || []).map(item => item.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning(ncfST('Frontend.AgentsManager.SelectGroupsToDeleteFirst'))
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.DeleteBatch'
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm(ncfST('Frontend.AgentsManager.DeleteTheSelectedDataInBatch'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            // reload data
            this.getGroupListData('group')
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },


    //Legacy comment translated to English.
    handleTaskViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'third' })
        return
      }
      this.clearHistoryTimer()
      this.invalidateTaskDetailRequest('task')
      this.scrollbarTaskIndex = ''
      // clear detail data
      this.taskDetails = ''
      this.taskSelection = []
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
      this.syncHashRoute({ tab: 'third', taskId: null })
    },
    //Legacy comment translated to English.
    handleTaskView(clickType, item = {}, index = 0, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'groupTask') {
        this.navigateByHash({ tab: 'second', groupId: item.chatGroupId || this.scrollbarGroupIndex || null, taskId: item.id })
        return
      }
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'task') {
        this.navigateByHash({ tab: 'third', taskId: item.id })
        return
      }
      this.clearHistoryTimer()
      if (clickType === 'agentTask') {
        this.invalidateTaskDetailRequest(clickType)
        this.agentDetailsTaskIndex = index ?? ''
        // clear detail data
        this.agentDetailsTaskDetails = ''
        this.agentDetailsTaskHistoryList = []
        this.agentDetailsTaskMemberList = []
        this.agentTaskMemberfilter = ''
        this.agentTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'agentGroupTask') {
        this.invalidateTaskDetailRequest(clickType)
        this.agentDetailsGroupShowType = '2'
        // clear detail data
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentDetailsGroupTaskMemberList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'groupTask') {
        this.invalidateTaskDetailRequest(clickType)
        this.groupShowType = '3'
        // clear detail data
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
        this.groupTaskMemberList = []
        this.groupTaskMemberfilter = ''
        this.groupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'second', groupId: item.chatGroupId || this.scrollbarGroupIndex || null, taskId: item.id })
      }
      if (clickType === 'task') {
        this.invalidateTaskDetailRequest(clickType)
        this.scrollbarTaskIndex = index ?? ''
        // clear detail data
        this.taskDetails = ''
        this.taskHistoryList = []
        this.taskMemberList = []
        this.taskMemberfilter = ''
        this.taskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'third', taskId: item.id })
      }
    },
    canForceStopTask(row = {}) {
      const status = Number(row?.status)
      return status === 0 || status === 1 || status === 2
    },
    handleTaskAction(action, optype, row = {}, index = 0) {
      switch (action) {
        case 'parameters':
          return this.viewAgentParameters(optype, row)
        case 'force-stop':
          return this.handleTaskForceStop(optype, row)
        case 'delete':
          return this.handleTaskDelet(optype, row)
        default:
          return undefined
      }
    },
    handleGroupAction(action, row = {}, index = 0) {
      switch (action) {
        case 'start':
          return this.handleElVisibleOpenBtn('drawerGroupStart', row)
        case 'toggle':
          return this.setGroupState(row)
        case 'delete':
          return this.handleGroupDelete('groupTable', row)
        default:
          return undefined
      }
    },
    handleRemoteAgentAction(action, row = {}) {
      switch (action) {
        case 'test':
          return this.testRemoteAgent(row)
        case 'toggle':
          return this.setRemoteAgentEnable(row, !row.enable)
        case 'delete':
          return this.deleteRemoteAgent(row)
        default:
          return undefined
      }
    },
    //Legacy comment translated to English.
    returnGroup(clickType, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'groupTask') {
        const groupId = this.groupDetails?.chatGroupDto?.id || this.scrollbarGroupIndex || null
        this.navigateByHash({ tab: 'second', groupId: groupId })
        return
      }
      this.clearHistoryTimer()
      if (clickType === 'agentGroupTask') {
        this.invalidateTaskDetailRequest(clickType)
        this.agentDetailsGroupShowType = '1'
        // const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
        // this.getGroupDetailData('agentGroup', item.id,this.agentDetailsGroupDetails)

      }
      if (clickType === 'groupTask') {
        this.invalidateTaskDetailRequest(clickType)
        this.groupShowType = '2' //Legacy comment translated to English.
        this.syncHashRoute({ tab: 'second', taskId: null })
        // const item = this.groupList[this.scrollbarGroupIndex]
        // this.getGroupDetailData('groupTable', item.id,this.groupDetails)
      }
    },
    //Legacy comment translated to English.
    handleAgentGroupTaskSelectionChange(val) {
      this.agentGroupTaskSelection = val
    },

    //Legacy comment translated to English.
    handleGroupTaskSelectionChange(val) {
      this.groupTaskSelection = val
    },
    //Legacy comment translated to English.
    handleTaskSelectionChange(val) {
      this.taskSelection = val
    },
    //Legacy comment translated to English.
    async viewAgentParameters(optype, item) {
      let baseList = []
      if (optype === 'task') {
        //Legacy comment translated to English.
        if (item && item.chatGroupId) {
          await this.getTaskMemberListData('task', item.chatGroupId)
        }
        baseList = this.taskMemberList ?? []
      } else if (optype === 'taskDetail') {
        baseList = this.taskMemberList ?? []
      } else if (optype === 'agentTask') {
        baseList = this.agentDetailsTaskMemberList ?? []
      } else if (optype === 'agentGroupTaskAdmin') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        let agentDtoList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
        let adminAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(adminAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTaskEnter') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        let agentDtoList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
        let enterAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(enterAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTask') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        baseList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
      } else if (optype === 'groupTaskAdmin') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        let agentDtoList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
        let adminAgentId = this.groupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(adminAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTaskEnter') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        let agentDtoList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
        let enterAgentId = this.groupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(enterAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTask') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        baseList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
      }
      //Legacy comment translated to English.
      this.agentParameterList = await this.buildAgentParameterList(baseList)
      //Legacy comment translated to English.
      this.agentParameterTabsValue = ''
      this.visible.dialogAgentParameter = true
      this.$nextTick(() => {
        this.agentParameterTabsValue = '0'
      })
    },
    //Legacy comment translated to English.
    async openAgentParameterEditor(item) {
      if (!item || item.agentKind === 'RemoteA2A' || !item.id) {
        return
      }
      await this.handleEditDrawerOpenBtn('drawerAgent', item)
    },
    //Legacy comment translated to English.
    async refreshAgentParameterItem(savedAgent) {
      if (!this.visible.dialogAgentParameter || !savedAgent?.id) {
        return
      }

      const index = this.agentParameterList.findIndex(item => item.id === savedAgent.id)
      if (index < 0) {
        return
      }

      const current = Object.assign({}, this.agentParameterList[index], savedAgent)
      this.$set(this.agentParameterList, index, current)
      try {
        const refreshed = await this.buildAgentParameterList([current])
        if (refreshed[0]) {
          this.$set(this.agentParameterList, index, refreshed[0])
        }
      } catch (e) {
        //Legacy comment translated to English.
        console.warn('refreshAgentParameterItem: refresh status failed for agent', savedAgent.id, e)
      }
    },
    //Legacy comment translated to English.
    async buildAgentParameterList(baseList) {
      const result = []
      for (const agent of baseList) {
        const enriched = Object.assign({}, agent, { outputList: [] })
        if (agent.agentKind === 'RemoteA2A') {
          result.push(enriched)
          continue
        }
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        try {
          const res = await serviceAM.get(
            `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${agent.id}`,
            { customAlert: true }
          )
          const data = res?.data ?? {}
          if (data.success) {
            const status = data?.data?.agentTemplateStatus ?? null
            if (status) {
              enriched.promptItemDto = status.promptItemDto || null
              enriched.promptRangeDto = status.promptRangeDto || null
              enriched.aiModelDto = status.aiModelDto || null
            }
          }
        } catch (e) {
          console.warn('buildAgentParameterList: GetItemStatus failed for agent', agent.id, e)
        }
        //Legacy comment translated to English.
        if (enriched.promptItemDto && enriched.promptItemDto.id) {
          try {
            const res = await serviceAM.get(
              `/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${enriched.promptItemDto.id}`,
              { customAlert: true }
            )
            const data = res?.data ?? {}
            if (data.success) {
              const promptResults = data?.data?.promptResults ?? []
              enriched.outputList = promptResults.map(oitem => {
                oitem.addTime = oitem.addTime ? formatDate(oitem.addTime) : ''
                oitem.resultStringHtml = this.renderSafeMarkdown(oitem.resultString || '')
                return oitem
              })
            }
          } catch (e) {
            console.warn('buildAgentParameterList: GetByItemId failed for promptItem', enriched.promptItemDto.id, e)
          }
        }
        result.push(enriched)
      }
      return result
    },
    //Legacy comment translated to English.
    handleTaskAgain(optype, item = {}) {
      let startData = item ?? {}
      const chatGroupId = this.getGroupStartChatGroupId(startData)
      if (chatGroupId) {
        startData = Object.assign({}, startData, { chatGroupId })
      }
      // this.groupStartForm.groupName = item.name
      this.handleEditDrawerOpenBtn('drawerTaskStart', startData)
    },
    // taskdelete
    handleTaskDelet(optype, row) {
      console.log('handleTaskDelet:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm(ncfST('Frontend.AgentsManager.DeleteThisData'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            let groupDetail = {}
            if (optype === 'agentGroupTask') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
            } else if (optype === 'groupTask') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
            } else {
              this.gettaskListData(optype)
            }
            if (groupDetail.id) {
              // gettask list
              this.gettaskListData(optype, groupDetail.id)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },
    //Legacy comment translated to English.
    handleTaskStartBatch(opType, item) {
      let selectedRows = []
      let refreshListType = 'task'
      let refreshGroupId = 0

      if (opType === 'agentGroupTaskBatch') {
        selectedRows = this.agentGroupTaskSelection
        refreshListType = 'agentGroupTask'
        refreshGroupId = Number(this.agentDetailsGroupDetails?.chatGroupDto?.id || 0)
      } else if (opType === 'groupTaskBatch') {
        selectedRows = this.groupTaskSelection
        refreshListType = 'groupTask'
        refreshGroupId = Number(this.groupDetails?.chatGroupDto?.id || 0)
      } else if (opType === 'taskBatch') {
        selectedRows = this.taskSelection
      }

      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning(ncfST('Frontend.AgentsManager.SelectTasksToStartFirst'))
        return
      }

      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.StartBatch'
      // operation confirmation prompt
      this.$confirm(ncfST('Frontend.AgentsManager.StartTheSelectedDataInBatch'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: res?.data?.data || ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            if (refreshListType === 'task') {
              const refreshOptions = this.buildTaskRefreshOptions('task', {
                preferLatest: true
              }, 'drawerTaskStart')
              this.gettaskListData('task', '', 0, refreshOptions)
            } else if (refreshGroupId > 0) {
              const refreshOptions = this.buildTaskRefreshOptions(refreshListType, {
                preferLatest: true,
                focusChatGroupId: refreshGroupId
              }, 'drawerTaskStart')
              this.gettaskListData(refreshListType, refreshGroupId, 0, refreshOptions)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },
    //Legacy comment translated to English.
    handleTaskDeleteBatch(opType, item) {
      let selectedRows = []
      if (opType === 'agentGroupTaskBatch') {
        // item.chatGroupDto.id this.agentDetails.agentTemplateDto.id
        console.log('agentGroupTaskBatch:', this.agentGroupTaskSelection);
        selectedRows = this.agentGroupTaskSelection
      } else if (opType === 'groupTaskBatch') {
        // item.chatGroupDto.id
        console.log('groupTaskBatch:', this.groupTaskSelection);
        selectedRows = this.groupTaskSelection
      } else if (opType === 'taskBatch') {
        console.log('taskSelection:', this.taskSelection);
        selectedRows = this.taskSelection
      }
      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning(ncfST('Frontend.AgentsManager.SelectTasksToDeleteFirst'))
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.DeleteBatch'
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm(ncfST('Frontend.AgentsManager.DeleteTheSelectedDataInBatch'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            });
            let groupDetail = {}, groupType = ''
            if (opType === 'agentGroupTaskBatch') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
              groupType = 'agentGroupTask' //'agentGroup'
            } else if (opType === 'groupTaskBatch') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
              groupType = 'groupTask' //'group'
            }
            if (groupDetail.id) {
              // gettask list
              this.gettaskListData(groupType, groupDetail.id)
              // this.getGroupDetailData(groupType, groupDetail.id, groupDetail)
            } else {
              this.gettaskListData('task')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        });
      });
    },

    handleTaskForceStop(optype, row) {
      if (!row || !row.id) return
      const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStop?id=${row.id}`
      this.$confirm(ncfST('Frontend.AgentsManager.ForceStopThisTask'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true,
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel')
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            })
            let groupDetail = {}
            if (optype === 'agentGroupTask') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
            } else if (optype === 'groupTask') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
            } else {
              this.gettaskListData('task')
            }
            if (groupDetail.id) {
              this.gettaskListData(optype, groupDetail.id)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        })
      })
    },

    handleTaskForceStopBatch(opType, item) {
      let selectedRows = []
      if (opType === 'agentGroupTaskBatch') {
        selectedRows = this.agentGroupTaskSelection
      } else if (opType === 'groupTaskBatch') {
        selectedRows = this.groupTaskSelection
      } else if (opType === 'taskBatch') {
        selectedRows = this.taskSelection
      }
      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning(ncfST('Frontend.AgentsManager.SelectTasksToStopFirst'))
        return
      }
      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStopBatch'
      this.$confirm(ncfST('Frontend.AgentsManager.ForceStopTheSelectedTasks'), ncfST('Frontend.AgentsManager.ConfirmActionAlternate'), {
        dangerouslyUseHTMLString: true,
        confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
        cancelButtonText: ncfST('Frontend.AgentsManager.Cancel')
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: ncfST('Frontend.AgentsManager.OperationSucceeded')
            })
            let groupDetail = {}, groupType = ''
            if (opType === 'agentGroupTaskBatch') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
              groupType = 'agentGroupTask'
            } else if (opType === 'groupTaskBatch') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
              groupType = 'groupTask'
            }
            if (groupDetail.id) {
              this.gettaskListData(groupType, groupDetail.id)
            } else {
              this.gettaskListData('task')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfST('Frontend.AgentsManager.OperationCanceled')
        })
      })
    },
    //Legacy comment translated to English.
    viewTaskDescription(item) {
      this.describeContent = item?.promptCommand ?? ''
      this.taskDescriptionDetails = item || null
      this.visible.dialogTaskDescription = true
    },
    taskHumanInTheLoopLevelText(value) {
      return {
        0: ncfST('Frontend.AgentsManager.LZeroFullyAutomaticPluginsMCPExecuteAutomatically'),
        1: ncfST('Frontend.AgentsManager.LOneRiskBasedApprovalMCPDefaultApproval'),
        2: ncfST('Frontend.AgentsManager.LTwoToolApprovalConfirmEveryToolFirst'),
        3: ncfST('Frontend.AgentsManager.LThreeHumanParticipantToolApproval')
      }[Number(value)] || ncfST('Frontend.AgentsManager.NoneAlternate')
    },
    taskToolPermissionText(value) {
      return {
        0: ncfST('Frontend.AgentsManager.InheritHILLevel'),
        1: ncfST('Frontend.AgentsManager.AutomaticExecution'),
        2: ncfST('Frontend.AgentsManager.ApproveBeforeExecution'),
        3: ncfST('Frontend.AgentsManager.Prohibited')
      }[Number(value)] || ncfST('Frontend.AgentsManager.NoneAlternate')
    },
    taskHumanParticipantStatusText(task) {
      if (!task?.executionPolicyCaptured) {
        return ncfST('Frontend.AgentsManager.ThisTaskWasCreatedBeforePolicyRecordingWasEnabledItsHistoricalHIL')
      }
      return task.includeHumanParticipant ? ncfST('Frontend.AgentsManager.IncludedInThisTask') : ncfST('Frontend.AgentsManager.NotEnabledForThisTask')
    },
    taskDescriptionCopyText() {
      const detail = this.taskDescriptionDetails || {}
      if (!detail.executionPolicyCaptured) {
        return this.describeContent
      }

      return [
        this.describeContent,
        '',
        `--- ${ncfST('Frontend.AgentsManager.RunPolicy')} ---`,
        `${ncfST('Frontend.AgentsManager.HILLevel')}：${this.taskHumanInTheLoopLevelText(detail.humanInTheLoopLevel)}`,
        `${ncfST('Frontend.AgentsManager.PluginToolPermissions')}：${this.taskToolPermissionText(detail.pluginToolPermission)}`,
        `${ncfST('Frontend.AgentsManager.MCPToolPermissions')}：${this.taskToolPermissionText(detail.mcpToolPermission)}`,
        `${ncfST('Frontend.AgentsManager.HumanParticipant')}：${detail.includeHumanParticipant ? ncfST('Frontend.AgentsManager.RunAgentWithPersonalizedParameters') : ncfST('Frontend.AgentsManager.Disable')}`,
        `${ncfST('Frontend.AgentsManager.MaximumChatRounds')}：${Number(detail.chatMaxRound || 0)}`,
        `${ncfST('Frontend.AgentsManager.PersonalizedParameters')}：${detail.isPersonality ? ncfST('Frontend.AgentsManager.Enable') : ncfST('Frontend.AgentsManager.Disable')}`,
        `${ncfST('Frontend.AgentsManager.CompatibleForcedApproval')}：${detail.requireHumanApproval ? ncfST('Frontend.AgentsManager.Enable') : ncfST('Frontend.AgentsManager.Disable')}`
      ].join('\n')
    },
    //Legacy comment translated to English.
    taskDescriptionCopy() {
      //Legacy comment translated to English.
      this.copyText('4', this.taskDescriptionCopyText()).then(() => {
        this.handleElVisibleClose('dialogTaskDescription')
      })
    },
    // task evaluation
    taskEvaluation(item) {
      Object.assign(this.evaluationForm, item)
      this.visible.dialogTaskEvaluation = true
    },
    //Legacy comment translated to English.
    handleInputNum(val, form) {
      if (form) {
        const sliderStep = 0.1
        let _val = val.replace(/[^\d]/g, '')
        //floor
        _val = Math.round(_val / sliderStep) * sliderStep
        if (form.includes('.')) {
          const formArr = form.split('.')
          // const formArrLen = formArr.length
          this.$set(this[formArr[0]], formArr[1], _val)
        } else {
          this.$set(this, form, _val)
        }
      }
    },
    //Legacy comment translated to English.
    handleTaskFilterChange(val, listType) {
      if (listType === 'agentGroupTask') {
        // agent group task
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.agentGroupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'groupTask') {
        // group task
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.groupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'agentTask') {
        // agent task
        const filterList = this.agentDetailsTaskMemberList.filter(item => item.name.includes(val))
        this.agentTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'task') {
        // task
        const filterList = this.taskMemberList.filter(item => item.name.includes(val))
        this.taskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
        console.log('handleTaskFilterChange', this.taskMemberfilterList);
      }
    },

    getTaskHistoryListForParticipantInfo(taskType) {
      const historyByType = {
        task: this.taskHistoryList,
        agentTask: this.agentDetailsTaskHistoryList,
        agentGroupTask: this.agentDetailsGroupTaskHistoryList,
        groupTask: this.groupTaskHistoryList,
      }
      return Array.isArray(historyByType[taskType]) ? historyByType[taskType] : []
    },

    getParticipantQuickInfo(taskType, participant) {
      const participantKey = this.getParticipantKey(participant)
      const relatedHistory = this.getTaskHistoryListForParticipantInfo(taskType)
        .filter(item => this.getParticipantKey(item) === participantKey)
      const usage = this.buildTaskHistoryUsageSummary(relatedHistory)
      const isHuman = participant?.isHuman === true
        || participant?.agentKind === 'Human'
        || participantKey.startsWith('human:')
      const hasReportedTokenUsage = relatedHistory.some(item => {
        return ['promptTokens', 'completionTokens', 'totalTokens']
          .some(field => item?.[field] !== null && item?.[field] !== undefined)
      })
      const isRemote = participant?.agentKind === 'RemoteA2A'
      const enabled = participant?.enable !== false
      const statusText = isRemote
        ? this.remoteParticipantAvailabilityText(participant)
        : (enabled ? ncfST('Frontend.AgentsManager.Enabled') : ncfST('Frontend.AgentsManager.Disabled'))
      const statusType = isRemote
        ? this.remoteParticipantAvailabilityType(participant)
        : (enabled ? 'success' : 'info')
      const currentUsageText = isHuman
        ? (usage.messageCount === 0
          ? ncfST('Frontend.AgentsManager.NoContent')
          : `${ncfST('Frontend.AgentsManager.TotalMessages')}: ${this.formatUsageCount(usage.messageCount)}`)
        : (usage.messageCount === 0
          ? ncfST('Frontend.AgentsManager.NoContent')
          : (hasReportedTokenUsage
            ? `${ncfST('Frontend.AgentsManager.Messages')}: ${this.formatUsageCount(usage.messageCount)} · ${ncfST('Frontend.AgentsManager.TotalToken')}: ${this.formatUsageCount(usage.totalTokens)}`
            : `${ncfST('Frontend.AgentsManager.Messages')}: ${this.formatUsageCount(usage.messageCount)} · ${ncfST('Frontend.AgentsManager.TotalToken')}`))
      const totalTokens = Number(participant?.totalTokens || 0)

      return {
        kindText: isHuman ? ncfST('Frontend.AgentsManager.HumanParticipant') : (isRemote ? ncfST('Frontend.AgentsManager.RemoteATwoA') : ncfST('Frontend.AgentsManager.Local')),
        kindType: isHuman ? 'info' : (isRemote ? 'warning' : 'primary'),
        statusText,
        statusType,
        description: participant?.description || ncfST('Frontend.AgentsManager.NoDescription'),
        currentUsageText,
        responseTimeText: isHuman
          ? ncfST('Frontend.AgentsManager.NoneAlternate')
          : this.formatResponseMilliseconds(usage.averageResponseMilliseconds, ncfST('Frontend.AgentsManager.NoContent')),
        totalUsageText: isHuman
          ? ncfST('Frontend.AgentsManager.HumanDoesNotCallAModelThisBindingPreservesTheSystemAgentAlternate')
          : (totalTokens > 0
            ? `${this.formatUsageCount(totalTokens)} Token`
            : ncfST('Frontend.AgentsManager.NoContent')),
        activityText: this.formatActivityTime(participant?.lastActiveTime || participant?.lastHealthCheckAt),
        healthMessage: isRemote ? (participant?.lastHealthCheckMessage || ncfST('Frontend.AgentsManager.ConnectionCheck')) : '',
        isRemote,
        canOpenEditor: !isHuman && Number.isInteger(Number(participant?.id)) && Number(participant.id) > 0,
      }
    },

    buildParticipantEditorUrl(participant) {
      const id = Number(participant?.id || 0)
      if (!Number.isInteger(id) || id <= 0 || participant?.isHuman === true || participant?.agentKind === 'Human') {
        return ''
      }

      if (participant?.agentKind === 'RemoteA2A') {
        return `/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId=${id}`
      }

      return `/Admin/AgentsManager/Index#tab=first&view=edit&agentId=${id}`
    },

    openParticipantAgentEditor(participant) {
      const url = this.buildParticipantEditorUrl(participant)
      if (!url) {
        return
      }

      const participantType = participant?.agentKind === 'RemoteA2A' ? 'RemoteA2A' : 'Local'
      const targetName = `NcfAgentsManager_${participantType}_${participant.id}`
      const openedWindow = typeof window.open === 'function'
        ? window.open(url, targetName)
        : null
      if (openedWindow) {
        openedWindow.focus?.()
        return
      }

      window.location?.assign?.(url)
    },

    getGroupParticipantList(groupDetail) {
      const localAgents = groupDetail?.agentTemplateDtoList ?? []
      const roleAgents = groupDetail?.roleAgentTemplateDtoList ?? []
      const remoteMembers = groupDetail?.remoteMemberDtoList ?? []
      const localParticipantMap = new Map()
      const localParticipants = localAgents.map(agent => {
        const participant = Object.assign({}, agent, {
          participantKey: agent.isHuman ? `human:${agent.id}` : `local:${agent.id}`,
          agentKind: agent.isHuman ? 'Human' : 'Local',
          roles: []
        })
        localParticipantMap.set(agent.id, participant)
        return participant
      })

      roleAgents.forEach(role => {
        const agent = role?.agentTemplateDto
        const roleName = String(role?.roleName || '').trim()
        if (!agent?.id) return

        let participant = localParticipantMap.get(agent.id)
        if (!participant) {
          participant = Object.assign({}, agent, {
            participantKey: `local:${agent.id}`,
            agentKind: 'Local',
            roles: []
          })
          localParticipantMap.set(agent.id, participant)
          localParticipants.push(participant)
        }
        if (roleName && !participant.roles.includes(roleName)) {
          participant.roles.push(roleName)
        }
      })

      const remoteAgents = remoteMembers
        .map(member => {
          const remote = member?.remoteAgentDto
          if (!remote || !remote.id) return null
          return Object.assign({}, remote, {
            participantKey: `remote:${remote.id}`,
            agentKind: 'RemoteA2A',
            avastar: null,
            enable: !!member.enable && !!remote.enable,
            connectionStatus: remote.connectionStatus
          })
        })
        .filter(Boolean)

      return localParticipants.concat(remoteAgents)
    },

    getTaskParticipantList(taskType) {
      if (taskType === 'agentGroupTask' && this.agentDetailsGroupTaskMemberList.length) {
        return this.agentDetailsGroupTaskMemberList
      }
      if (taskType === 'groupTask' && this.groupTaskMemberList.length) {
        return this.groupTaskMemberList
      }
      if (taskType === 'agentGroupTask') {
        return this.getGroupParticipantList(this.agentDetailsGroupDetails)
      }
      if (taskType === 'groupTask') {
        return this.getGroupParticipantList(this.groupDetails)
      }
      return []
    },

    getParticipantKey(participantOrHistory) {
      if (!participantOrHistory) return ''
      if (participantOrHistory.fromParticipantKey) return participantOrHistory.fromParticipantKey
      if (participantOrHistory.participantKey) return participantOrHistory.participantKey
      if (participantOrHistory.fromAgentTemplateId !== undefined && participantOrHistory.fromAgentTemplateId !== null) {
        return `local:${participantOrHistory.fromAgentTemplateId}`
      }
      return participantOrHistory.id !== undefined && participantOrHistory.id !== null
        ? `local:${participantOrHistory.id}`
        : ''
    },

    historyMatchesMemberFilter(historyItem, filterText, matchedParticipantKeys) {
      return !filterText || matchedParticipantKeys.includes(this.getParticipantKey(historyItem))
    },

    //Legacy comment translated to English.
    scrollbarDown(refName, istouchBottom = false, isFirst = false) {
      if (!refName) return
      const scrollbar = this.$refs[refName];
      if (!scrollbar) return
      if (istouchBottom) {
        const scrollTop = scrollbar.wrap.scrollTop; //Legacy comment translated to English.
        const scrollHeight = scrollbar.wrap.scrollHeight; //Legacy comment translated to English.
        const clientHeight = scrollbar.wrap.clientHeight; //Legacy comment translated to English.
        // scrollTop, scrollHeight, clientHeight
        if (scrollHeight !== clientHeight && (scrollTop + clientHeight + 30 >= scrollHeight || isFirst)) {
          //Legacy comment translated to English.
          scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
        }
      } else {
        //Legacy comment translated to English.
        scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
      }
    },
    //Legacy comment translated to English.
    getTaskSenderName(taskType, historyItem) {
      const sender = this.getTaskSenderInfo(taskType, historyItem)
      if (sender && sender.name) {
        return sender.name
      }
      return historyItem?.fromParticipantName || historyItem?._streamAgentName || (historyItem?._generating ? 'Generating...' : '')
    },
    getTaskSenderInfo(taskType, participantOrHistory) {
      const participantKey = this.getParticipantKey(participantOrHistory)
      const formId = participantOrHistory?.fromAgentTemplateId ?? participantOrHistory?.id ?? participantOrHistory
      // agent group task
      if (taskType === 'agentGroupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // group task
      if (taskType === 'groupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // agent task
      if (taskType === 'agentTask') {
        const fintItem = this.agentDetailsTaskMemberList.find(item => this.getParticipantKey(item) === participantKey)
          || this.agentDetailsTaskMemberList.find(item => item.agentKind !== 'RemoteA2A' && item.id === formId)
        return fintItem ?? {}
      }

      // task
      if (taskType === 'task') {
        const fintItem = this.taskMemberList.find(item => this.getParticipantKey(item) === participantKey)
          || this.taskMemberList.find(item => item.agentKind !== 'RemoteA2A' && item.id === formId)
        return fintItem ?? {}
      }

      return {}
    },
    jumpPromptRange(urlType, item) {
      let url = ''
      if (urlType === 'promptRange') {
        //Legacy comment translated to English.
        const rangeId = item?.promptRange?.id ?? ''
        const promptId = item?.id ?? ''
        if (rangeId && promptId) {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18#rangeId=${rangeId}&promptId=${promptId}`
        } else {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        }
      }
      if (urlType === 'model') {
        url = `/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69`
      }
      if (urlType === 'modelParameter') {
        // url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        if (item) {
          //Legacy comment translated to English.
          this.$confirm(`<div class="df">
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Top_p:</span>
                        <span>${item.topP}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Temperature:</span>
                        <span>${item.temperature}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>MaxToken:</span>
                        <span>${item.maxToken}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Frequeny_penalty:</span>
                        <span>${item.frequencyPenalty}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Presence_penalty:</span>
                        <span>${item.presencePenalty}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>StopSequences:</span>
                        <span>${item.stopSequences}</span>
                    </div>
    </div>`, ncfST('Frontend.AgentsManager.ModelParameters'), {
            dangerouslyUseHTMLString: true, //Legacy comment translated to English.
            confirmButtonText: ncfST('Frontend.AgentsManager.ConfirmAlternate'),
            cancelButtonText: ncfST('Frontend.AgentsManager.Cancel'),
            showCancelButton: false,
            // type: 'warning'
          }).then(() => { }).catch(() => { });
        }
      }
      if (!url) return
      simulationAELOperation(url)
      // openWindow(url)
    },
    //Legacy comment translated to English.
    handlePromptShowName(showType, item) {
      let resultText = ''
      if (showType === '1') {
        //Legacy comment translated to English.
        const itemData = item?.promptRangeDto ?? ''
        if (itemData) {
          resultText = `${itemData.alias}(${itemData.rangeName})`
        }
      } else if (showType === '2') {
        // range
        const itemData = item?.promptItemDto ?? ''
        if (itemData) {
          const avg = scoreFormatter(itemData.evalAvgScore)
          const max = scoreFormatter(itemData.evalMaxScore)
          resultText = `${itemData.nickName || ncfST('Frontend.AgentsManager.NotSet')} | ${itemData.fullVersion} | ${ncfST('Frontend.AgentsManager.AverageScore')} ${avg} | ${ncfST('Frontend.AgentsManager.HighestScore')} ${max} ${itemData.isDraft ? ncfST('Frontend.AgentsManager.Draft') : ''}`
        }
      }
      return resultText ?? ''
    },
    //Legacy comment translated to English.
    async copyText(opType, item) {
      let text = ''
      if (opType === '1') {
        text = item?.message ?? ''
      } else if (opType === '2') {
        text = item?.messageHtml ?? ''
      } else if (opType === '3') {
        text = item?.promptCommand ?? ''
      } else if (opType === '4') {
        text = item ?? ''
      }

      const copied = await copyTextForEmbeddedBrowser(text)
      this.$message[copied ? 'success' : 'error'](copied ? ncfST('Frontend.AgentsManager.CopiedSuccessfully') : ncfST('Frontend.AgentsManager.CopyFailed'))
      return copied
    },
};

