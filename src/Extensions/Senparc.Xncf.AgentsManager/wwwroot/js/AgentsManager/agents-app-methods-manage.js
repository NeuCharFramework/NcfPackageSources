/*
 * AgentsManager 前端：智能体/组/任务的查看、筛选、启停、删除与参与者管理方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsManage = {
    // 查看全部智能体 列表 
    handleAgentViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: null }))
        return
      }
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = '' // 清空索引
      this.agentDetails = '' // 清空详情数据
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
    // 查看 智能体
    handleAgentView(item, index, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: item.id }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      this.scrollbarAgentIndex = item.id ?? ''
      // 重置 数据
      this.resetAgentDetailsQuery()
      // 获取智能体 详情
      this.getAgentDetailData(item.id, item)
      // 重置 组获取智能体query
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', item.id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', item.id)
      }
      this.syncHashRoute({ tab: 'first', agentId: item.id })
    },
    // 重置 智能体详情下的组和任务数据
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
    // 切换 智能体详情 tabs 页面
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
    // 智能体 状态 切换
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
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
    // 组启用 / 停用
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



    // 侧边 组tree 组件 节点 筛选
    filterGroupTreeNode(value, data) {
      if (!value) return true;
      return data.name.indexOf(value) !== -1;
    },
    // 侧边 组tree 组件 节点 点击
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
          this.groupDetails = '' // 清空组详情
        }
        if (this.groupShowType === '2') {
          this.groupDetails = deepClone(data)
        }
        if (this.groupShowType === '3') {
          this.groupDetails = deepClone(data)
        }
      }
    },
    // 组 查看详情
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
    // 组 查看全部 列表 
    handleGroupViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'second' })
        return
      }
      this.clearHistoryTimer()
      this.groupShowType = '1'
      // 清空组详情
      this.scrollbarGroupIndex = '' // 清空索引
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
    // 组 查看列表 详情 
    handleGroupView(clickType, item, index = 0, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && (clickType === 'group' || clickType === 'groupTable')) {
        this.navigateByHash({ tab: 'second', groupId: item.id })
        return
      }
      this.clearHistoryTimer()
      // 智能体下时 查看组详情
      if (clickType === 'agentGroup') {
        // 切换展示类型
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupIndex = index ?? 0
        // 清空组详情
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
      // 组大类时 查看组详情
      if (clickType === 'group' || clickType === 'groupTable') {
        // 切换展示类型
        this.groupShowType = '2'
        // if (clickType === 'groupTable') {
        //     const { pageIndex, pageSize } = this.groupQueryList
        //     this.scrollbarGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
        // } else {
        //     this.scrollbarGroupIndex = index ?? 0
        // }
        this.scrollbarGroupIndex = item.id ?? ''
        // 清空组详情
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
    // 组 新增|编辑 智能体table 切换table 选中
    toggleSelection(rows) {
      if (rows) {
        rows.forEach(row => {
          this.$refs?.groupAgentTable?.toggleRowSelection(row);
        });
      } else {
        this.$refs?.groupAgentTable?.clearSelection();
      }
    },
    // 组 新增|编辑 智能体table 选中变化
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
    // 组 新增|编辑 智能体 成员取消选中
    groupMembersCancel(item, index) {
      this.groupForm.members.splice(index, 1);
      const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
      if (findIndex !== -1) {
        this.toggleSelection([this.groupAgentList[findIndex]])
      }
    },
    // 组列表选中变化 (批量删除)
    handleGroupSelectionChange(val) {
      this.groupSelection = val
    },
    // 组 删除
    handleGroupDelete(optype, row) {
      console.log('handleGroupDelete:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
              // 重新获取数据
              this.getGroupListData('group')
            } else {
              // 查看全部组
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
    // 组批量删除
    handleGroupDeleteBatch() {
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      const selectedIds = (this.groupSelection || []).map(item => item.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要删除的组')
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.DeleteBatch'
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
            // 重新获取数据
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


    // 任务 查看全部 列表 
    handleTaskViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'third' })
        return
      }
      this.clearHistoryTimer()
      this.scrollbarTaskIndex = ''
      // 清空详情数据
      this.taskDetails = ''
      this.taskSelection = []
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
      this.syncHashRoute({ tab: 'third', taskId: null })
    },
    // 查看 任务详情
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
        // 清空详情数据
        this.agentDetailsTaskDetails = ''
        this.agentDetailsTaskHistoryList = []
        this.agentDetailsTaskMemberList = []
        this.agentTaskMemberfilter = ''
        this.agentTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '2'
        // 清空详情数据
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentDetailsGroupTaskMemberList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'groupTask') {
        this.groupShowType = '3'
        // 清空详情数据
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
        // 清空详情数据
        this.taskDetails = ''
        this.taskHistoryList = []
        this.taskMemberList = []
        this.taskMemberfilter = ''
        this.taskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'third', taskId: item.id })
      }
    },
    // 返回组详情页面
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
        this.groupShowType = '2' // 组件详情
        this.syncHashRoute({ tab: 'second', taskId: null })
        // const item = this.groupList[this.scrollbarGroupIndex]
        // this.getGroupDetailData('groupTable', item.id,this.groupDetails)
      }
    },
    // 智能体-组 任务列表table选中变化 (批量启动和删除)
    handleAgentGroupTaskSelectionChange(val) {
      this.agentGroupTaskSelection = val
    },

    // 组 任务列表table选中变化 (批量启动和删除)
    handleGroupTaskSelectionChange(val) {
      this.groupTaskSelection = val
    },
    // 任务列表选择 
    handleTaskSelectionChange(val) {
      this.taskSelection = val
    },
    // 查看智能体参数 列表
    async viewAgentParameters(optype, item) {
      let baseList = []
      if (optype === 'task') {
        // 从任务行数据获取所在组成员列表
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
      // 填充状态与历史输出后再打开弹窗
      this.agentParameterList = await this.buildAgentParameterList(baseList)
      // 先清空再开弹窗，确保 el-tabs 在 pane 渲染完成后按正确类型激活第一个 tab
      this.agentParameterTabsValue = ''
      this.visible.dialogAgentParameter = true
      this.$nextTick(() => {
        this.agentParameterTabsValue = '0'
      })
    },
    // 从参数弹窗直接复用现有 Agent 编辑表单，保留当前对话上下文
    async openAgentParameterEditor(item) {
      if (!item || item.agentKind === 'RemoteA2A' || !item.id) {
        return
      }
      await this.handleEditDrawerOpenBtn('drawerAgent', item)
    },
    // Agent 编辑保存后同步当前参数弹窗，避免关闭抽屉后仍显示旧名称、描述或参数
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
        // 基础信息已经同步；状态接口失败时保留原有参数展示
        console.warn('refreshAgentParameterItem: refresh status failed for agent', savedAgent.id, e)
      }
    },
    // 构建智能体参数列表：为基础 DTO 列表补充 promptItemDto / aiModelDto / promptRangeDto 及历史输出
    async buildAgentParameterList(baseList) {
      const result = []
      for (const agent of baseList) {
        const enriched = Object.assign({}, agent, { outputList: [] })
        if (agent.agentKind === 'RemoteA2A') {
          result.push(enriched)
          continue
        }
        // 获取智能体运行状态（含 promptItemDto / aiModelDto / promptRangeDto）
        // 使用 serviceAM 并设置 customAlert，由拦截器静默处理错误
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
        // 获取历史输出列表（PromptRange 结果）
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
    // 再次执行 (即再次启动)
    handleTaskAgain(optype, item = {}) {
      let startData = item ?? {}
      const chatGroupId = this.getGroupStartChatGroupId(startData)
      if (chatGroupId) {
        startData = Object.assign({}, startData, { chatGroupId })
      }
      // this.groupStartForm.groupName = item.name
      this.handleEditDrawerOpenBtn('drawerTaskStart', startData)
    },
    // 任务删除
    handleTaskDelet(optype, row) {
      console.log('handleTaskDelet:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
              // 获取任务列表
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
    // 组-任务批量启动(任务) agentGroupTaskBatch groupTaskBatch
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
      // 操作确认 提示
      this.$confirm('确认批量启动数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
    // 组-任务批量删除(任务) agentGroupTaskBatch groupTaskBatch
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
      // 操作确认 提示
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
              // 获取任务列表
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
    // 查看任务描述
    viewTaskDescription(item) {
      this.describeContent = item?.promptCommand ?? ''
      this.taskDescriptionDetails = item || null
      this.visible.dialogTaskDescription = true
    },
    taskHumanInTheLoopLevelText(value) {
      return {
        0: 'L0 全自动',
        1: 'L1 风险分层',
        2: 'L2 工具审批',
        3: 'L3 Human 参与者 + 工具审批'
      }[Number(value)] || `未知（${value}）`
    },
    taskToolPermissionText(value) {
      return {
        0: '继承 HIL 等级',
        1: '自动执行',
        2: '执行前审批',
        3: '禁止使用'
      }[Number(value)] || `未知（${value}）`
    },
    taskHumanParticipantStatusText(task) {
      if (!task?.executionPolicyCaptured) {
        return '历史任务未记录'
      }
      return task.includeHumanParticipant ? '已加入本任务' : '本任务未启用'
    },
    taskDescriptionCopyText() {
      const detail = this.taskDescriptionDetails || {}
      if (!detail.executionPolicyCaptured) {
        return this.describeContent
      }

      return [
        this.describeContent,
        '',
        '--- 执行策略 ---',
        `HIL 等级：${this.taskHumanInTheLoopLevelText(detail.humanInTheLoopLevel)}`,
        `插件工具权限：${this.taskToolPermissionText(detail.pluginToolPermission)}`,
        `MCP 工具权限：${this.taskToolPermissionText(detail.mcpToolPermission)}`,
        `Human 参与者：${detail.includeHumanParticipant ? '包含' : '跳过'}`,
        `最大对话轮数：${Number(detail.chatMaxRound || 0)}`,
        `个性化参数：${detail.isPersonality ? '启用' : '关闭'}`,
        `兼容强制审批：${detail.requireHumanApproval ? '启用' : '关闭'}`
      ].join('\n')
    },
    // 任务描述复制
    taskDescriptionCopy() {
      // 复制文本
      this.copyText('4', this.taskDescriptionCopyText()).then(() => {
        this.handleElVisibleClose('dialogTaskDescription')
      })
    },
    // 任务评价
    taskEvaluation(item) {
      Object.assign(this.evaluationForm, item)
      this.visible.dialogTaskEvaluation = true
    },
    // input 数值类型处理
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
    // 任务成员列表筛选 
    handleTaskFilterChange(val, listType) {
      if (listType === 'agentGroupTask') {
        // 智能体 组 任务
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.agentGroupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'groupTask') {
        // 组 任务
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.groupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'agentTask') {
        // 智能体 任务
        const filterList = this.agentDetailsTaskMemberList.filter(item => item.name.includes(val))
        this.agentTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'task') {
        // 任务
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

    // el-scrollbar 触底滚动 到底部
    scrollbarDown(refName, istouchBottom = false, isFirst = false) {
      if (!refName) return
      const scrollbar = this.$refs[refName];
      if (!scrollbar) return
      if (istouchBottom) {
        const scrollTop = scrollbar.wrap.scrollTop; // 当前滚动的顶部
        const scrollHeight = scrollbar.wrap.scrollHeight; // 内容总高度
        const clientHeight = scrollbar.wrap.clientHeight; // 可视区域高度
        // scrollTop, scrollHeight, clientHeight
        if (scrollHeight !== clientHeight && (scrollTop + clientHeight + 30 >= scrollHeight || isFirst)) {
          // 滚动到底部
          scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
        }
      } else {
        // 滚动到底部
        scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
      }
    },
    // 获取发送人名称
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
      // 智能体 组 任务
      if (taskType === 'agentGroupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // 组 任务
      if (taskType === 'groupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // 智能体 任务
      if (taskType === 'agentTask') {
        const fintItem = this.agentDetailsTaskMemberList.find(item => this.getParticipantKey(item) === participantKey)
          || this.agentDetailsTaskMemberList.find(item => item.agentKind !== 'RemoteA2A' && item.id === formId)
        return fintItem ?? {}
      }

      // 任务
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
        // 靶场:rangeId   靶道:promptId（hash 路由）
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
          // 展示详情数据
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
            dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
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
    // 处理靶道 和 靶场展示名称
    handlePromptShowName(showType, item) {
      let resultText = ''
      if (showType === '1') {
        // 靶道
        const itemData = item?.promptRangeDto ?? ''
        if (itemData) {
          resultText = `${itemData.alias}(${itemData.rangeName})`
        }
      } else if (showType === '2') {
        // 靶场
        const itemData = item?.promptItemDto ?? ''
        if (itemData) {
          const avg = scoreFormatter(itemData.evalAvgScore)
          const max = scoreFormatter(itemData.evalMaxScore)
          resultText = `${itemData.nickName || '未设置'} | ${itemData.fullVersion} | 平均分：${avg} | 最高分：${max} ${itemData.isDraft ? '(草稿)' : ''}`
        }
      }
      return resultText ?? ''
    },
    // 复制 task 任务描述
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

