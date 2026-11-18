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
          messageText = `<div>是否确认将${itemData.name}智能体停用？</div><div>此智能体将不会参与后续新任务</div>`
        } else {
          messageText = `<div>是否确认将${itemData.name}智能体启用？</div>`
        }
      }
      if (stateType === 'delete') {
        messageText = `<div>是否确认从${item.name}组退出？</div><div>移除出后将无法看到之前参与的任务记录</div>`
      }
      if (!serviceURL) return
      this.$confirm(messageText, '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
        });
      });
    },
    //Legacy comment translated to English.
    async setGroupState(item) {
      const group = item?.chatGroupDto || item
      if (!group || !group.id) return

      const willEnable = !group.enable
      const actionText = willEnable ? '启用' : '停用'
      const messageText = willEnable
        ? `<div>是否确认启用组“${group.name}”？</div><div>启用后可再次创建新任务，并可作为 Workflow 节点使用。</div>`
        : `<div>是否确认停用组“${group.name}”？</div><div>停用后不会创建新任务或作为 Workflow 节点使用；已经启动的任务不会被中断。</div>`

      try {
        await this.$confirm(messageText, '操作确认', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        })

        const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Enable?id=${group.id}&enable=${willEnable}`
        const res = await serviceAM.post(serviceURL)
        if (!res?.data?.success) {
          throw new Error(res?.data?.errorMessage || res?.data?.data || '操作失败')
        }

        this.$message({ type: 'success', message: `组已${actionText}` })
        if (this.tabsActiveName === 'second') {
          await this.getGroupListData('group')
        } else if (this.agentDetails?.agentTemplateDto?.id) {
          await this.getGroupListData('agentGroup', this.agentDetails.agentTemplateDto.id)
        }
        await this.refreshAgentGraphSnapshot(this.agentListViewMode === 'three')
      } catch (error) {
        if (error === 'cancel' || error === 'close') {
          this.$message({ type: 'info', message: '已取消操作' })
          return
        }
        this.$message({
          message: error?.message || '操作失败',
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
            `<div>智能体「${itemData.name}」当前不可删除。</div>`,
            adminGroups.length ? `<div style="margin-top:6px;">作为群主的组：${adminGroups.join('、')}</div>` : '',
            enterGroups.length ? `<div style="margin-top:6px;">作为对接人的组：${enterGroups.join('、')}</div>` : '',
            '<div style="margin-top:8px;color:#E6A23C;">请先在对应组中替换群主/对接人后再删除。</div>'
          ].join('')

          this.$alert(blockedMessage, '删除受阻', {
            dangerouslyUseHTMLString: true,
            confirmButtonText: '我知道了'
          })
          return
        }

        const memberGroups = memberGroupList.map(group => group.name)
        const previewMessage = [
          `<div>确认删除智能体「${itemData.name}」吗？</div>`,
          memberGroups.length
            ? `<div style="margin-top:6px;">将移出成员组：${memberGroups.join('、')}</div>`
            : '<div style="margin-top:6px;">该智能体当前不在任何组成员中。</div>',
          '<div style="margin-top:8px;">同时会删除与该智能体相关的历史消息记录，且不可恢复。</div>'
        ].join('')

        this.$confirm(previewMessage, '删除智能体确认', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '删除智能体',
          cancelButtonText: '取消',
          type: 'warning'
        }).then(() => {
          serviceAM.post(serviceURL).then(res => {
            if (res.data.success) {
              this.$message({
                type: 'success',
                message: '删除成功!'
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
            message: '已取消操作'
          })
        })
      }).catch(() => {
        app.$message({
          message: '删除预检查失败，请稍后重试',
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
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
        });
      });
    },
    //Legacy comment translated to English.
    handleGroupDeleteBatch() {
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      const selectedIds = (this.groupSelection || []).map(item => item.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要删除的组')
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.DeleteBatch'
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
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
        this.agentDetailsGroupShowType = '1'
        // const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
        // this.getGroupDetailData('agentGroup', item.id,this.agentDetailsGroupDetails)

      }
      if (clickType === 'groupTask') {
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
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
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
        this.$message.warning('请先选择要启动的任务')
        return
      }

      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.StartBatch'
      // operation confirmation prompt
      this.$confirm('确认批量启动数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: res?.data?.data || '操作成功!'
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
          message: '已取消操作'
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
        this.$message.warning('请先选择要删除的任务')
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.DeleteBatch'
      if (!serviceURL) return
      // operation confirmation prompt
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, //Legacy comment translated to English.
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
        });
      });
    },

    handleTaskForceStop(optype, row) {
      if (!row || !row.id) return
      const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStop?id=${row.id}`
      this.$confirm('确认强制停止该任务吗？', '操作确认', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
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
        this.$message.warning('请先选择要停止的任务')
        return
      }
      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStopBatch'
      this.$confirm('确认批量强制停止所选任务吗？', '操作确认', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
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
          message: '已取消操作'
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
        0: ncfST('Frontend.345'),
        1: ncfST('Frontend.346'),
        2: ncfST('Frontend.347'),
        3: ncfST('Frontend.348')
      }[Number(value)] || ncfST('Frontend.555')
    },
    taskToolPermissionText(value) {
      return {
        0: ncfST('Frontend.468'),
        1: ncfST('Frontend.470'),
        2: ncfST('Frontend.412'),
        3: ncfST('Frontend.455')
      }[Number(value)] || ncfST('Frontend.555')
    },
    taskHumanParticipantStatusText(task) {
      if (!task?.executionPolicyCaptured) {
        return ncfST('Frontend.444')
      }
      return task.includeHumanParticipant ? ncfST('Frontend.578') : ncfST('Frontend.579')
    },
    taskDescriptionCopyText() {
      const detail = this.taskDescriptionDetails || {}
      if (!detail.executionPolicyCaptured) {
        return this.describeContent
      }

      return [
        this.describeContent,
        '',
        `--- ${ncfST('Frontend.440')} ---`,
        `${ncfST('Frontend.341')}：${this.taskHumanInTheLoopLevelText(detail.humanInTheLoopLevel)}`,
        `${ncfST('Frontend.419')}：${this.taskToolPermissionText(detail.pluginToolPermission)}`,
        `${ncfST('Frontend.349')}：${this.taskToolPermissionText(detail.mcpToolPermission)}`,
        `${ncfST('Frontend.343')}：${detail.includeHumanParticipant ? ncfST('Frontend.140') : ncfST('Frontend.501')}`,
        `${ncfST('Frontend.432')}：${Number(detail.chatMaxRound || 0)}`,
        `${ncfST('Frontend.354')}：${detail.isPersonality ? ncfST('Frontend.502') : ncfST('Frontend.501')}`,
        `${ncfST('Frontend.376')}：${detail.requireHumanApproval ? ncfST('Frontend.502') : ncfST('Frontend.501')}`
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
        : (enabled ? '已启用' : '已停用')
      const statusType = isRemote
        ? this.remoteParticipantAvailabilityType(participant)
        : (enabled ? 'success' : 'info')
      const currentUsageText = isHuman
        ? (usage.messageCount === 0
          ? '尚无已提交输入'
          : `已输入 ${this.formatUsageCount(usage.messageCount)} 条文本`)
        : (usage.messageCount === 0
          ? '尚无已完成回复'
          : (hasReportedTokenUsage
            ? `${this.formatUsageCount(usage.messageCount)} 条回复 · ${this.formatUsageCount(usage.totalTokens)} Token`
            : `${this.formatUsageCount(usage.messageCount)} 条回复 · Token 未由远端反馈`))
      const totalTokens = Number(participant?.totalTokens || 0)

      return {
        kindText: isHuman ? 'Human 参与者' : (isRemote ? '远程 A2A' : '本地 Agent'),
        kindType: isHuman ? 'info' : (isRemote ? 'warning' : 'primary'),
        statusText,
        statusType,
        description: participant?.description || '暂无简介',
        currentUsageText,
        responseTimeText: isHuman
          ? '不适用'
          : this.formatResponseMilliseconds(usage.averageResponseMilliseconds, '暂无响应时长'),
        totalUsageText: isHuman
          ? '不产生模型 Token'
          : (totalTokens > 0
            ? `${this.formatUsageCount(totalTokens)} Token`
            : '暂无累计数据'),
        activityText: this.formatActivityTime(participant?.lastActiveTime || participant?.lastHealthCheckAt),
        healthMessage: isRemote ? (participant?.lastHealthCheckMessage || '尚未执行连接检测') : '',
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
    </div>`, '模型参数', {
            dangerouslyUseHTMLString: true, //Legacy comment translated to English.
            confirmButtonText: '确定',
            cancelButtonText: '取消',
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
          resultText = `${itemData.nickName || '未设置'} | ${itemData.fullVersion} | 平均分：${avg} | 最高分：${max} ${itemData.isDraft ? '(草稿)' : ''}`
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
      this.$message[copied ? 'success' : 'error'](copied ? '复制成功' : '复制失败')
      return copied
    },
};

