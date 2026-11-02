/*
 * AgentsManager 前端：智能体与 ChatGroup 列表/详情数据加载方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsAgentGroup = {
    // 获取 智能体 数据
    async getAgentListData(listType, page = 0) {
      const queryList = {}
      if (listType === 'agent') {
        this.agentQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.agentQueryList)
      }
      if (listType === 'groupAgent') {
        this.groupAgentQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.groupAgentQueryList)
      }
      // 接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const agentData = data?.data?.list ?? []
            if (listType === 'agent') {
              this.$set(this, 'agentList', agentData)
              const agentDetail = this.agentDetails?.agentTemplateDto ?? {}
              // 获取详情 
              if (agentDetail.id) {
                this.getAgentDetailData(agentDetail.id, agentDetail)
              }
              // 计算 agent列表 需要填充的元素数量
              this.calcAgentFillNum()

              if (this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
                this.refreshAgentGraphSnapshot(this.agentListViewMode === 'three')
              }
            }
            if (listType === 'groupAgent') {
              this.$set(this, 'groupAgentList', agentData)
              // 确保更新数据时 不会清空选中
              this.$nextTick(() => {
                this.isGetGroupAgent = false
              })
              // 组成员table 初始选中
              if (this.visible.drawerGroup && this.groupForm.members.length > 0) {
                // this.toggleSelection()
                this.$nextTick(() => {
                  // this.groupAgentTotal = agentData.length
                  const filterList = agentData.filter(i => {
                    return this.groupForm.members.findIndex(item => item.id === i.id) !== -1
                  })
                  this.toggleSelection(filterList)
                })

              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
            this.isGetGroupAgent = false
          }
        }).catch((err) => {
          console.log('err', err)
          this.isGetGroupAgent = false
        })
    },
    // 获取 智能体详情 
    async getAgentDetailData(id, detail = {}) {
      let taskList = []
      let groupList = []
      if (this.tabsActiveName === 'first') {
        const groupQuery = {
          pageIndex: 0,
          pageSize: 0,
          agentTemplateId: id
        }
        // 获取组列表
        await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(groupQuery)}`, groupQuery)
          .then(res => {
            const data = res?.data ?? {}
            if (data.success) {
              groupList = data?.data?.chatGroupDtoList ?? []
            }
          })
        const taskQuery = {
          pageIndex: 0,
          pageSize: 0,
          agentTemplateId: id
        }
        //  获取任务列表
        await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?${getInterfaceQueryStr(taskQuery)}`, taskQuery)
          .then(res => {
            const data = res?.data ?? {}
            if (data.success) {
              taskList = data?.data?.chatTaskList ?? []
            }
          })
      }
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${id}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const agentDetail = data?.data?.agentTemplateStatus ?? ''
            if (agentDetail) {
              if (agentDetail.agentTemplateDto) {
                const agentTemplateDto = Object.assign({}, detail, agentDetail.agentTemplateDto)
                agentDetail.agentTemplateDto = agentTemplateDto
              }
              if (this.tabsActiveName === 'first') {
                agentDetail.participationGroup = groupList.length
                agentDetail.participationInTasks = taskList.length
              }
            }

            this.$set(this, 'agentDetails', agentDetail)
            // this.agentDetails = agentDetail
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 组 数据
    async getGroupListData(listType, id, page = 0) {
      const queryList = {}
      if (listType === 'group') {
        this.groupQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.groupQueryList)
      }
      if (listType === 'agentGroup') {
        this.agentDetailsGroupQueryList.pageIndex = page ?? 1
        this.agentDetailsGroupQueryList.agentTemplateId = id
        Object.assign(queryList, this.agentDetailsGroupQueryList)
      }
      // debugger
      // 获取agent列表
      let agentAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList')
        .then(res => {
          // debugger
          const data = res?.data ?? {}
          if (data.success) {
            agentAllList = data?.data?.list ?? []
          }
        })
      // 获取任务列表
      let taskAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList')
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            taskAllList = data?.data?.chatTaskList ?? []
            //设置最新的任务信息
            this.groupTaskListLastNew = taskAllList[0]
          }
        })
      // 获取组列表
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          // debugger
          const data = res?.data ?? {}
          if (data.success) {
            const groupData = data?.data?.chatGroupDtoList ?? []
            const handleGroupData = groupData.map(item => {
              const adminAgentTemplateName = agentAllList.find(i => i.id === item.adminAgentTemplateId)?.name ?? ''
              const enterAgentTemplateName = agentAllList.find(i => i.id === item.enterAgentTemplateId)?.name ?? ''
              const numberTasks = taskAllList.filter(i => i.chatGroupId === item.id) || []
              return {
                ...item,
                numberTasks: numberTasks?.length ?? 0,
                adminAgentTemplateName,
                enterAgentTemplateName,
              }
            })
            if (listType === 'group') {
              // this.groupTreeData = [{
              //     id: '0',
              //     name: '全部组',
              //     children: groupData
              // }]
              this.groupSelection = [] // 清空选中
              this.$set(this, 'groupList', handleGroupData)
              const groupDetail = this.groupDetails?.chatGroupDto ?? {}
              // 获取详情 
              if (this.groupShowType === '2' && groupDetail.id) {
                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
              }
            }
            if (listType === 'agentGroup') {
              this.$set(this, 'agentDetailsGroupList', handleGroupData)
              const groupDetail = handleGroupData[this.agentDetailsGroupIndex]
              // 获取详情
              if (groupDetail && groupDetail.id) {
                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 组详情 
    async getGroupDetailData(detailType, id, detail = {}) {
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${id}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const groupDetail = data?.data ?? ''
            if (groupDetail && groupDetail.chatGroupDto) {
              const chatGroupDto = Object.assign({}, detail, groupDetail.chatGroupDto)
              groupDetail.chatGroupDto = chatGroupDto
            }
            if (detailType === 'agentGroup') {
              this.$set(this, 'agentDetailsGroupDetails', groupDetail)
              // 获取任务列表
              this.gettaskListData('agentGroupTask', id)
            }
            if (['group', 'groupTable'].includes(detailType)) {
              this.$set(this, 'groupDetails', groupDetail)
              // 获取任务列表
              this.gettaskListData('groupTask', id)
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
};

