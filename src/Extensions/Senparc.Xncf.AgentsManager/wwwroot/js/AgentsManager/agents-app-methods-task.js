/*
 * AgentsManager 前端：ChatTask 列表/详情/历史记录、SSE 流式输出与 Human-in-the-Loop 方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsTask = {
    // 获取 任务 数据
    async gettaskListData(listType, id, page = 0, options = {}) {
      const opts = options || {}
      const preferLatest = !!opts.preferLatest
      const focusChatGroupIdRaw = opts.focusChatGroupId
      const hasFocusChatGroupId = focusChatGroupIdRaw !== undefined
        && focusChatGroupIdRaw !== null
        && focusChatGroupIdRaw !== ''
      const focusChatGroupId = hasFocusChatGroupId ? Number(focusChatGroupIdRaw) : null
      const minTaskIdExclusive = Number(opts.minTaskIdExclusive || 0)
      const hasMinTaskIdExclusive = Number.isFinite(minTaskIdExclusive) && minTaskIdExclusive > 0
      const retryOnMiss = !!opts.retryOnMiss

      const getScopedTaskList = (list) => {
        if (!Array.isArray(list) || list.length === 0) return []
        if (!preferLatest) return list
        if (!hasFocusChatGroupId) return list

        const scopedList = list.filter(task => Number(task.chatGroupId) === focusChatGroupId)
        return scopedList.length > 0 ? scopedList : list
      }

      const hasLatestCandidate = (list) => {
        const candidateList = getScopedTaskList(list)
        if (!hasMinTaskIdExclusive) return candidateList.length > 0
        return candidateList.some(task => Number(task?.id || 0) > minTaskIdExclusive)
      }

      const pickTaskForView = (list, currentTaskId) => {
        if (!Array.isArray(list) || list.length === 0) return null

        if (preferLatest) {
          const candidateList = getScopedTaskList(list)
          const filteredList = hasMinTaskIdExclusive
            ? candidateList.filter(task => Number(task?.id || 0) > minTaskIdExclusive)
            : candidateList
          const sortList = filteredList.length > 0 ? filteredList : candidateList
          return sortList
            .slice()
            .sort((a, b) => {
              const idA = Number(a?.id || 0)
              const idB = Number(b?.id || 0)
              if (idA !== idB) return idB - idA

              const timeA = new Date(a?.startTime || a?.addTime || 0).getTime()
              const timeB = new Date(b?.startTime || b?.addTime || 0).getTime()
              return timeB - timeA
            })[0] || null
        }

        if (currentTaskId !== undefined && currentTaskId !== null && currentTaskId !== '') {
          const matched = list.find(task => String(task.id) === String(currentTaskId))
          if (matched) return matched
        }
        return list[0]
      }

      const queryList = {}
      // 任务
      if (listType === 'task') {
        this.taskQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.taskQueryList)
        queryList.archiveScope = this.getTaskArchiveScopeCode()
      }
      // 智能体 任务
      if (listType === 'agentTask') {
        this.agentDetailsTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsTaskQueryList.agentTemplateId = id
        Object.assign(queryList, this.agentDetailsTaskQueryList)
      }
      // 智能体 组 任务
      if (listType === 'agentGroupTask') {
        this.agentDetailsGroupTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsGroupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.agentDetailsGroupTaskQueryList)
      }
      // 组 任务
      if (listType === 'groupTask') {
        this.groupTaskQueryList.pageIndex = page ?? 1
        this.groupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.groupTaskQueryList)
      }
      let modelList = []
      // 获取模型列表
      await serviceAM.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', {
        pageIndex: 0,
        pageSize: 0
      }).then(res => {
        // console.log('this.serviceType === model', res);
        const data = res?.data ?? {}
        if (data.success) {
          //console.log('getModelOptData:', res.data)
          modelList = data?.data ?? []
        } else {
          app.$message({
            message: data.errorMessage || data.data || 'Error',
            type: 'error',
            duration: 5 * 1000
          })
        }
      })
      //  接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const taskData = data?.data?.chatTaskList ?? []
            const handleTaskData = taskData.map(item => {
              const modelName = modelList.find(i => i.id === item.aiModelId)?.alias ?? ''
              return {
                ...item,
                modelName
              }
            })

            const needRetryLatestTask = preferLatest && retryOnMiss
            if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
              this.scheduleTaskListRetry(listType, id, page, opts)
            } else {
              this.clearTaskListRetryTimer(listType)
            }

            // 任务
            if (listType === 'task') {
              this.$set(this, 'taskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.taskDetails?.id)
                if (taskDetail) {
                  if (preferLatest) {
                    const latestIndex = handleTaskData.findIndex(task => String(task.id) === String(taskDetail.id))
                    this.scrollbarTaskIndex = latestIndex > -1 ? latestIndex : 0
                  }
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 智能体 任务
            if (listType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.agentDetailsTaskDetails?.id)
                if (taskDetail) {
                  if (preferLatest) {
                    const latestIndex = handleTaskData.findIndex(task => String(task.id) === String(taskDetail.id))
                    this.agentDetailsTaskIndex = latestIndex > -1 ? latestIndex : 0
                    this.agentDetailsTabsActiveName = 'second'
                  }
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 智能体 组 任务
            if (listType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              if (preferLatest && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.agentDetailsGroupDetailsTaskDetails?.id)
                if (taskDetail) {
                  this.agentDetailsGroupShowType = '2'
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 组 任务
            if (listType === 'groupTask') {
              this.$set(this, 'groupTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              if (preferLatest && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.groupTaskDetails?.id)
                if (taskDetail) {
                  this.groupShowType = '3'
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
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
    getTaskListByType(listType) {
      if (listType === 'task') return this.taskList || []
      if (listType === 'agentTask') return this.agentDetailsTaskList || []
      if (listType === 'agentGroupTask') return this.agentDetailsGroupTaskList || []
      if (listType === 'groupTask') return this.groupTaskList || []
      return []
    },
    getCurrentTaskDetailByType(listType) {
      if (listType === 'task') return this.taskDetails || null
      if (listType === 'agentTask') return this.agentDetailsTaskDetails || null
      if (listType === 'agentGroupTask') return this.agentDetailsGroupDetailsTaskDetails || null
      if (listType === 'groupTask') return this.groupTaskDetails || null
      return null
    },
    setCurrentTaskDetailByType(listType, detail) {
      if (listType === 'task') this.$set(this, 'taskDetails', detail)
      if (listType === 'agentTask') this.$set(this, 'agentDetailsTaskDetails', detail)
      if (listType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupDetailsTaskDetails', detail)
      if (listType === 'groupTask') this.$set(this, 'groupTaskDetails', detail)
    },
    setCurrentTaskStatusByType(listType, chatTaskId, status) {
      const nextStatus = Number(status)
      if (!Number.isFinite(nextStatus)) return

      const currentDetail = this.getCurrentTaskDetailByType(listType)
      const taskId = Number(chatTaskId || currentDetail?.id || 0)

      if (currentDetail && taskId > 0 && Number(currentDetail.id || 0) === taskId) {
        if (Number(currentDetail.status) !== nextStatus) {
          this.setCurrentTaskDetailByType(listType, Object.assign({}, currentDetail, { status: nextStatus }))
        }
      }

      const list = this.getTaskListByType(listType)
      if (!Array.isArray(list) || taskId <= 0) return

      const listIndex = list.findIndex(item => Number(item?.id || 0) === taskId)
      if (listIndex < 0) return

      const currentItem = list[listIndex] || {}
      if (Number(currentItem.status) === nextStatus) return

      this.$set(list, listIndex, Object.assign({}, currentItem, { status: nextStatus }))
    },
    getMaxTaskIdByType(listType) {
      const list = this.getTaskListByType(listType)
      if (!Array.isArray(list) || list.length === 0) return 0
      return list.reduce((maxId, item) => {
        const currentId = Number(item?.id || 0)
        return currentId > maxId ? currentId : maxId
      }, 0)
    },
    buildTaskRefreshOptions(listType, baseOptions = {}, saveType = '') {
      const options = Object.assign({}, baseOptions)
      const isStartTaskSave = ['drawerTaskStart', 'drawerGroupStart'].includes(saveType)
      if (!isStartTaskSave) {
        return options
      }

      options.retryOnMiss = true
      options.retryAttempt = 0
      options.maxRetry = 20
      options.retryDelayMs = 300

      const detailId = Number(this.getCurrentTaskDetailByType(listType)?.id || 0)
      const maxId = this.getMaxTaskIdByType(listType)
      const baselineTaskId = Math.max(detailId, maxId)

      if (baselineTaskId > 0) {
        options.minTaskIdExclusive = baselineTaskId
      }

      return options
    },
    clearTaskListRetryTimer(listType) {
      if (!listType) return
      const timer = this.taskListRetryTimer[listType]
      if (timer) {
        clearTimeout(timer)
      }
      this.$delete(this.taskListRetryTimer, listType)
    },
    clearTaskListRetryTimers() {
      Object.keys(this.taskListRetryTimer || {}).forEach((key) => {
        this.clearTaskListRetryTimer(key)
      })
    },
    scheduleTaskListRetry(listType, id, page, options = {}) {
      if (!listType) return
      const attempt = Number(options.retryAttempt || 0)
      const maxRetry = Number(options.maxRetry || 0)
      if (attempt >= maxRetry) {
        this.clearTaskListRetryTimer(listType)
        return
      }

      const retryDelayMs = Math.max(120, Number(options.retryDelayMs || 300))
      this.clearTaskListRetryTimer(listType)
      this.taskListRetryTimer[listType] = setTimeout(() => {
        const nextOptions = Object.assign({}, options, {
          retryAttempt: attempt + 1
        })
        this.gettaskListData(listType, id, page, nextOptions)
      }, retryDelayMs)
    },
    // 获取 任务详情 
    async getTaskDetailData(detailType, id, detail = {}, detailsOn = false) {
      //TODO:
      if (id == undefined) {
        app.$message({
          message: '当前还没有可执行的任务',
          type: 'error',
          duration: 5 * 1000
        })
        return
      }
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${id}`)
        .then(async res => {
          const data = res?.data ?? {}
          if (data.success) {
            // 不是仅详情时 清除轮询
            if (!detailsOn) {
              this.clearHistoryTimer()
            }

            if (!detailsOn) {
              if (detailType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskMemberList', [])
              if (detailType === 'groupTask') this.$set(this, 'groupTaskMemberList', [])
            }

            let taskDetail = data?.data?.chatTaskDto ?? ''
            if (taskDetail) {
              taskDetail = Object.assign({}, detail, taskDetail)
            }
            // 智能体 组 任务
            if (detailType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupDetailsTaskDetails', taskDetail)
            }
            // 组 任务
            if (detailType === 'groupTask') {
              this.$set(this, 'groupTaskDetails', taskDetail)
            }
            // 智能体 任务
            if (detailType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskDetails', taskDetail)
            }
            // 任务
            if (detailType === 'task') {
              this.$set(this, 'taskDetails', taskDetail)
            }

            if (!detailsOn && taskDetail) {
              if (detailType === 'task') this.$set(this, 'taskHistoryList', [])
              if (detailType === 'agentTask') this.$set(this, 'agentDetailsTaskHistoryList', [])
              if (detailType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskHistoryList', [])
              if (detailType === 'groupTask') this.$set(this, 'groupTaskHistoryList', [])

              // 打开任务详情时检测一次远程 A2A 成员；后续仅由用户手动触发重测。
              const taskMemberList = await this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
              await this.autoTestRemoteParticipants(taskMemberList)
              // 首次获取历史 + 开启实时流
              this.getTaskRecordListData(detailType, taskDetail.id, '', true)
              this.startTaskHistoryStream(detailType, taskDetail.id, taskDetail.status)
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
    // 获取 任务历史记录
    async getTaskRecordListData(recordType, chatTaskId, nextHistoryId, isFirst = false) {
      const queryList = {
        chatTaskId,
        nextHistoryId
      }
      //  接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const chatGroupHistories = data?.data?.chatGroupHistories ?? []
            const historiesData = chatGroupHistories.map(item => {
              //使用 MarkDown 格式，对输出结果进行展示
              item.messageHtml = this.renderSafeMarkdown(item.message);
              return item
            })
            if (historiesData.length > 0) {
              this.clearTaskGeneratingPlaceholder(recordType)
            }
            // 任务
            if (recordType === 'task') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('task'), isFirst)
              let historyList = this.taskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.taskHistoryList.concat(historiesData);
                  this.$set(this, 'taskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.taskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'taskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('task', latestId)
              })
            }
            // 智能体 任务
            if (recordType === 'agentTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('agentTask'), isFirst)
              let historyList = this.agentDetailsTaskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.agentDetailsTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'agentDetailsTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('agentTask', latestId)
              })
            }
            // 智能体 组 任务
            if (recordType === 'agentGroupTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('agentGroupTask'), isFirst)
              let historyList = this.agentDetailsGroupTaskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.agentDetailsGroupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsGroupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('agentGroupTask', latestId)
              })
            }
            // 组 任务
            if (recordType === 'groupTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('groupTask'), isFirst)
              let historyList = this.groupTaskHistoryList || []
              if (nextHistoryId) {
                if (historiesData.length > 0) {
                  historyList = this.groupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'groupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.groupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'groupTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('groupTask', latestId)
              })
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
    // 获取 任务 成员列表
    async getTaskMemberListData(memberType, chatGroupld) {
      try {
        const res = await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${chatGroupld}`)
        const data = res?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载任务成员失败')
        }

        const groupDetail = data?.data ?? {}
        const taskMemberList = this.getGroupParticipantList(groupDetail)

        // 任务详情页的成员面板使用组详情状态渲染。启动任务后，任务详情请求可能
        // 先于组详情请求完成；用这里取得的完整组数据回填对应状态，避免右侧面板
        // 因为竞态暂时没有 groupDetails 而显示为空，重新进入页面才恢复。
        const mergeTaskGroupDetail = (currentDetail) => {
          const current = currentDetail || {}
          return Object.assign({}, current, groupDetail, {
            chatGroupDto: Object.assign({}, current.chatGroupDto || {}, groupDetail.chatGroupDto || {})
          })
        }

        if (memberType === 'groupTask') {
          this.$set(this, 'groupDetails', mergeTaskGroupDetail(this.groupDetails))
        }
        if (memberType === 'agentGroupTask') {
          this.$set(this, 'agentDetailsGroupDetails', mergeTaskGroupDetail(this.agentDetailsGroupDetails))
          this.$set(this, 'agentDetailsGroupTaskMemberList', taskMemberList)
        }
        if (memberType === 'groupTask') {
          this.$set(this, 'groupTaskMemberList', taskMemberList)
        }

        // 任务
        if (memberType === 'task') {
          this.$set(this, 'taskMemberList', taskMemberList)
        }
        // 智能体 任务
        if (memberType === 'agentTask') {
          this.$set(this, 'agentDetailsTaskMemberList', taskMemberList)
        }
        return taskMemberList
      } catch (err) {
        app.$message({
          message: err?.message || '加载任务成员失败',
          type: 'error',
          duration: 5 * 1000
        })
        return []
      }
    },
    openUsageAnalytics(detailType, taskDetail) {
      if (!taskDetail || !taskDetail.id) return

      this.usageAnalyticsTaskId = taskDetail.id
      this.usageAnalyticsTaskName = taskDetail.name || `Task-${taskDetail.id}`
      this.usageAnalyticsVisible = true

      let members = []
      if (detailType === 'task') members = this.taskMemberList || []
      if (detailType === 'agentTask') members = this.agentDetailsTaskMemberList || []
      if (['groupTask', 'agentGroupTask'].includes(detailType)) {
        members = this.getTaskParticipantList(detailType)
      }

      this.usageAnalyticsAgentOptions = members.map(item => ({
        id: item.id,
        name: item.name
      }))

      this.loadUsageAnalytics()
    },
    resetUsageAnalyticsFilters() {
      this.usageAnalyticsDateRange = []
      this.usageAnalyticsAgentId = ''
      this.loadUsageAnalytics()
    },
    async loadUsageAnalytics() {
      if (!this.usageAnalyticsTaskId) return
      this.usageAnalyticsLoading = true

      const query = {
        chatTaskId: this.usageAnalyticsTaskId
      }
      if (this.usageAnalyticsAgentId) {
        query.agentTemplateId = this.usageAnalyticsAgentId
      }
      if (this.usageAnalyticsDateRange && this.usageAnalyticsDateRange.length === 2) {
        query.startTime = this.usageAnalyticsDateRange[0]
        query.endTime = this.usageAnalyticsDateRange[1]
      }

      try {
        const res = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetUsageAnalytics?${getInterfaceQueryStr(query)}`)
        const data = res?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || '获取统计数据失败')
          return
        }

        const payload = data.data || {}
        this.usageAnalyticsData = {
          overview: payload.overview || this.$options.data().usageAnalyticsData.overview,
          roundStats: payload.roundStats || [],
          agentStats: payload.agentStats || [],
          timelineStats: payload.timelineStats || []
        }
      } catch (e) {
        console.error(e)
        this.$message.error('获取统计数据失败')
      } finally {
        this.usageAnalyticsLoading = false
      }
    },
    getDefaultTaskUsageSummary() {
      return {
        messageCount: 0,
        promptTokens: 0,
        completionTokens: 0,
        totalTokens: 0,
        averageResponseMilliseconds: 0,
        maxResponseMilliseconds: 0,
      }
    },
    buildTaskHistoryUsageSummary(historyList = []) {
      const summary = this.getDefaultTaskUsageSummary()
      if (!Array.isArray(historyList) || historyList.length === 0) {
        return summary
      }

      let responseCount = 0
      let responseTotalMs = 0

      historyList.forEach((item) => {
        if (!item || item._generating) {
          return
        }

        summary.messageCount += 1

        const promptTokens = Number(item.promptTokens || 0) || 0
        const completionTokens = Number(item.completionTokens || 0) || 0
        const itemTotalTokens = Number(item.totalTokens || 0) || 0
        const totalTokens = itemTotalTokens > 0 ? itemTotalTokens : (promptTokens + completionTokens)

        summary.promptTokens += promptTokens
        summary.completionTokens += completionTokens
        summary.totalTokens += totalTokens

        const responseMilliseconds = Number(item.responseMilliseconds || 0) || 0
        if (responseMilliseconds > 0) {
          responseCount += 1
          responseTotalMs += responseMilliseconds
          if (responseMilliseconds > summary.maxResponseMilliseconds) {
            summary.maxResponseMilliseconds = responseMilliseconds
          }
        }
      })

      if (responseCount > 0) {
        summary.averageResponseMilliseconds = Math.round(responseTotalMs / responseCount)
      }

      return summary
    },
    formatUsageCount(value) {
      const numeric = Number(value || 0)
      if (!Number.isFinite(numeric)) return '0'
      return numeric.toLocaleString('en-US')
    },
    formatActivityTime(value) {
      return value ? formatDate(value) : '暂无'
    },
    formatResponseMilliseconds(milliseconds, emptyText = '--') {
      const numeric = Number(milliseconds || 0)
      if (!Number.isFinite(numeric) || numeric <= 0) {
        return emptyText
      }

      const rounded = Math.round(numeric)
      if (rounded < 1000) {
        return `${rounded}ms`
      }

      const seconds = Math.floor(rounded / 1000)
      const remainMilliseconds = rounded % 1000
      return `${seconds}s${remainMilliseconds}ms`
    },
    formatTaskHistoryUsage(history) {
      const promptTokens = Number(history?.promptTokens || 0) || 0
      const completionTokens = Number(history?.completionTokens || 0) || 0
      const totalTokens = Number(history?.totalTokens || 0) || (promptTokens + completionTokens)
      const responseMs = history?.responseMilliseconds || 0
      const roundText = history?.roundIndex ? `R${history.roundIndex} - ` : ''
      return `${roundText}Token: ${totalTokens}${responseMs > 0 ? ` · ${this.formatResponseMilliseconds(responseMs, '')}` : ''}`
    },
    getTaskArchiveScopeCode(scope = this.taskArchiveScope) {
      const scopeMap = {
        active: 0,
        archived: 1,
        all: 2
      }
      return scopeMap[scope] ?? 0
    },
    setTaskArchiveScope(scope) {
      if (!scope || this.taskArchiveScope === scope) return
      this.taskArchiveScope = scope
      this.clearHistoryTimer()
      this.scrollbarTaskIndex = ''
      this.taskDetails = ''
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
      this.syncHashRoute({ tab: 'third', taskId: null })
    },
    async handleTaskArchiveToggle(item) {
      if (!item || !item.id) return
      const nextArchived = !item.isArchived
      const actionText = nextArchived ? ncfST('归档') : ncfST('取消归档')
      this.taskArchiveSavingId = item.id
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.SetArchiveStatus?id=${item.id}&isArchived=${nextArchived}`
        )
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || ncfST('{0}失败', actionText))
          return
        }

        this.$message.success(ncfST('{0}成功', actionText))
        const currentTaskId = Number(this.taskDetails?.id || 0)
        if (currentTaskId === Number(item.id) && this.taskArchiveScope !== 'all' && this.taskArchiveScope !== (nextArchived ? 'archived' : 'active')) {
          this.scrollbarTaskIndex = ''
          this.taskDetails = ''
          this.taskHistoryList = []
          this.taskMemberList = []
          this.taskMemberfilter = ''
          this.taskMemberfilterList = []
        }
        this.gettaskListData('task')
      } catch (error) {
        this.$message.error(ncfST('{0}失败：{1}', actionText, error?.message || ncfST('未知错误')))
      } finally {
        this.taskArchiveSavingId = 0
      }
    },
    getGroupStartChatGroupId(serviceForm = {}) {
      const candidates = [
        serviceForm?.chatGroupId,
        serviceForm?.chatGroupDto?.id,
        serviceForm?.groupId,
        this.groupStartForm?.chatGroupId,
        this.groupTaskDetails?.chatGroupId,
        this.groupDetails?.chatGroupDto?.id,
        this.scrollbarGroupIndex,
        this.groupTaskQueryList?.chatGroupId,
        this.agentDetailsGroupDetailsTaskDetails?.chatGroupId,
        this.agentDetailsGroupDetails?.chatGroupDto?.id,
        this.agentDetailsGroupTaskQueryList?.chatGroupId,
        this.agentDetailsTaskDetails?.chatGroupId,
        this.taskDetails?.chatGroupId,
        typeof window !== 'undefined'
          ? window.location.hash.match(/(?:#|&)groupId=(\d+)/)?.[1]
          : ''
      ]

      for (const candidate of candidates) {
        const id = Number(candidate)
        if (Number.isInteger(id) && id > 0) {
          return id
        }
      }

      return 0
    },
    // 保存 submitForm 数据
    async saveSubmitFormData(saveType, serviceForm = {}) {
      //debugger
      let serviceURL = ''
      // agent 新增|编辑
      if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
        // 确保 serviceForm 是正确的对象
        serviceForm = serviceForm || {};

        serviceForm.functionBindings = (serviceForm.functionBindings || this.agentForm.functionBindings || [])
          .map(item => this.normalizeFunctionBinding(item))
          .filter(Boolean)
        serviceForm.functionCallNames = serviceForm.functionBindings
          .filter(item => item.kind === 'plugin')
          .map(item => item.key)
          .join(',')

        // 打印日志以便调试
        console.log('Submitting serviceForm:', serviceForm);
        console.log('functionCallTags:', this.functionCallTags);
        console.log('functionCallNames:', serviceForm.functionCallNames);

        serviceURL = '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.SetItem'
        if (saveType === 'dialogGroupAgent') {
          this.isGetGroupAgent = true
        }
      }
      // 组 新增|编辑
      if (saveType === 'drawerGroup') {
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.SetChatGroup'
        const memberAgentTemplateIds = (serviceForm.members || [])
          .filter(item => item && item.isHuman !== true)
          .map(item => item.id)
        const remoteAgentIds = (serviceForm.remoteMembers || []).map(item => item.id)
        serviceForm.memberAgentTemplateIds = memberAgentTemplateIds
        serviceForm.remoteAgentIds = remoteAgentIds
        serviceURL += `?${getInterfaceQueryStr({ memberAgentTemplateIds, remoteAgentIds, includeHumanParticipant: !!serviceForm.includeHumanParticipant })}`
      }
      // 组启动（运行任务） ['drawerGroupStart', 'drawerTaskStart'].includes(btnType)
      if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
        const chatGroupId = this.getGroupStartChatGroupId(serviceForm)
        if (!chatGroupId) {
          app.$message({
            message: '未找到有效的聊天组，请重新从聊天组或任务详情打开启动窗口。',
            type: 'error',
            duration: 5 * 1000
          })
          return
        }

        // 启动窗口可能由任务详情打开；此时表单对象只携带了部分字段，
        // 统一把当前上下文解析出的 Group ID 写回请求，避免后台收到 0。
        serviceForm.chatGroupId = chatGroupId
        if (serviceForm.requireHumanApproval && Number(serviceForm.humanInTheLoopLevel || 0) === 0) {
          serviceForm.humanInTheLoopLevel = 2
        }
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup'
        // RunGroup 将聊天组 ID 同时作为查询参数和正文属性提交。查询参数由动态 ApiBind
        // 控制器显式绑定，即使旧脚本或宿主环境异常处理了正文属性，后台仍能可靠取得 Group ID。
        serviceURL += `?${getInterfaceQueryStr({ chatGroupId })}`
      }
      if (saveType === 'dialogTaskEvaluation') {
        serviceURL = ''
      }
      if (!serviceURL) return
      try {
        const response = await serviceAM.post(serviceURL, serviceForm)
        if (response.data.success) {
          let refName = '', formName = ''
          // 智能体
          if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            refName = 'agentELForm'
            formName = 'agentForm'
          }
          // 组
          if (saveType === 'drawerGroup') {
            refName = 'groupELForm'
            formName = 'groupForm'
            // 重置 组获取智能体query
            this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
            this.$set(this, 'groupRemoteAgentQueryList', this.$options.data().groupRemoteAgentQueryList)
            this.groupRemoteAgentList = []
          }
          // 组 启动
          if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            refName = 'groupStartELForm'
            formName = 'groupStartForm'
          }
          // 任务评价
          if (saveType === 'dialogTaskEvaluation') {
            refName = 'evaluationELForm'
            formName = 'evaluationForm'
          }
          if (formName) {
            this.$set(this, `${formName}`, this.$options.data()[formName])
            // Object.assign(this[formName],this.$options.data()[formName] )
          }
          if (refName) {
            this.$refs[refName].resetFields();
          }
          if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            this.agentSystemMessageTypeDetectionPending = false
          }
          delete this.editorFormInitialSnapshots[this.getEditorVisibleKey(saveType)]
          this.$nextTick(() => {
            this.visible[this.getEditorVisibleKey(saveType)] = false
          })
          // 重新获取数据
          if (['drawerGroup', 'drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            console.log('#***#', this.tabsActiveName, this.agentDetails);
            const isStartTaskSave = ['drawerTaskStart', 'drawerGroupStart'].includes(saveType)

            if (this.tabsActiveName === 'first') {
              // agentTemplateStatus
              if (this.agentDetails) {
                const id = this.agentDetails.agentTemplateDto ? this.agentDetails.agentTemplateDto.id : this.agentDetails.id
                if (this.agentDetailsTabsActiveName === 'first') {
                  if (isStartTaskSave) {
                    const focusChatGroupId = serviceForm?.chatGroupId
                      || this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
                      || this.agentDetailsGroupDetails?.chatGroupDto?.id
                      || ''
                    if (focusChatGroupId) {
                      const refreshOptions = this.buildTaskRefreshOptions('agentGroupTask', {
                        preferLatest: true,
                        focusChatGroupId
                      }, saveType)
                      this.gettaskListData('agentGroupTask', focusChatGroupId, 0, refreshOptions)
                    } else {
                      this.getGroupListData('agentGroup', id)
                    }
                  } else {
                    this.getGroupListData('agentGroup', id)
                  }
                } else {
                  const refreshOptions = this.buildTaskRefreshOptions('agentTask', {
                    preferLatest: isStartTaskSave,
                    focusChatGroupId: serviceForm?.chatGroupId || ''
                  }, saveType)
                  this.gettaskListData('agentTask', id, 0, refreshOptions)
                }
              }
            } else if (this.tabsActiveName === 'second') {
              if (isStartTaskSave) {
                const focusChatGroupId = serviceForm?.chatGroupId
                  || this.groupTaskDetails?.chatGroupId
                  || this.groupDetails?.chatGroupDto?.id
                  || ''
                if (focusChatGroupId) {
                  const refreshOptions = this.buildTaskRefreshOptions('groupTask', {
                    preferLatest: true,
                    focusChatGroupId
                  }, saveType)
                  this.gettaskListData('groupTask', focusChatGroupId, 0, refreshOptions)
                } else {
                  this.getGroupListData('group')
                }
              } else {
                this.getGroupListData('group')
              }
            } else {
              const refreshOptions = this.buildTaskRefreshOptions('task', {
                preferLatest: isStartTaskSave,
                focusChatGroupId: serviceForm?.chatGroupId || ''
              }, saveType)
              this.gettaskListData('task', '', 0, refreshOptions)
            }
          } else if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            const agentMapStr = {
              'drawerAgent': 'agent',
              'dialogGroupAgent': 'groupAgent'
            }
            this.getAgentListData(agentMapStr[saveType])
            if (saveType === 'drawerAgent') {
              await this.refreshAgentParameterItem(response.data?.data)
            }
          } else if (saveType === 'dialogTaskEvaluation') {
            // 重新获取任务详情 
            let detail = {}
            if (this.tabsActiveName === 'first') {
              if (this.agentDetailsTabsActiveName === 'first') {
                detail.serviceType = 'agentGroupTask'
                detail.id = this.agentDetailsGroupDetailsTaskDetails?.id ?? ''
              } else if (this.agentDetailsTabsActiveName === 'second') {
                detail.serviceType = 'agentTask'
                detail.id = this.agentDetailsTaskDetails?.id ?? ''
              }
            } else if (this.tabsActiveName === 'second') {
              detail.serviceType = 'groupTask'
              detail.id = this.groupTaskDetails?.id ?? ''
            } else {
              detail.serviceType = 'task'
              detail.id = this.taskDetails?.id ?? ''
            }
            if (detail.id) {
              // detailType, id, detail = {} true
              this.getTaskDetailData(detail.serviceType, detail.id, detail, true)
            }
          }

          if (['drawerGroup', 'drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            this.refreshQuickJumpTaskOptions()
          }
        } else {
          console.error('API Error:', response.data);
          app.$message({
            message: response.data.errorMessage || response.data.data || 'Error',
            type: 'error',
            duration: 5 * 1000
          })
          this.isGetGroupAgent = false
        }
      } catch (err) {
        console.error('Request Error:', err);
        this.isGetGroupAgent = false
      }
    },
    // 轮询获取 task 历史对话记录
    pollGetTaskHistoryData(listType, fun, id) {
      if (!listType || !fun) return
      fun(listType, id, '', true)
      const interval = () => {
        if (this.historyTimer[listType]) clearTimeout(this.historyTimer[listType])
        this.historyTimer[listType] = setTimeout(() => {
          const nextHistoryId = this.getLatestPersistedHistoryId(listType)
          // 执行代码块
          fun(listType, id, nextHistoryId || '')
          interval()
        }, 1000 * 5)
        // console.log('pollGetTaskHistoryData', this.historyTimer[listType]);
      }
      interval()
    },
    getHistoryListByType(listType) {
      if (listType === 'task') return this.taskHistoryList || []
      if (listType === 'agentTask') return this.agentDetailsTaskHistoryList || []
      if (listType === 'agentGroupTask') return this.agentDetailsGroupTaskHistoryList || []
      if (listType === 'groupTask') return this.groupTaskHistoryList || []
      return []
    },
    shouldShowTaskGenerating(status) {
      return [1, 2].includes(Number(status))
    },
    shouldSubscribeTaskStream(status) {
      return [0, 1, 2].includes(Number(status))
    },
    buildTaskGeneratingItem(listType, chatTaskId) {
      const nowIso = new Date().toISOString()
      return {
        id: `${listType}:generating:${chatTaskId || 'unknown'}`,
        fromAgentTemplateId: 0,
        addTime: nowIso,
        message: 'Generating...',
        messageHtml: this.renderSafeMarkdown('Generating...'),
        promptTokens: 0,
        completionTokens: 0,
        totalTokens: 0,
        responseMilliseconds: 0,
        roundIndex: 0,
        _streaming: true,
        _generating: true,
        _streamAgentName: 'Generating...'
      }
    },
    ensureTaskGeneratingPlaceholder(listType, chatTaskId) {
      if (!listType) return
      const historyList = this.getHistoryListByType(listType).slice()
      const existedIndex = historyList.findIndex(item => item && item._generating)
      if (existedIndex > -1) {
        return historyList[existedIndex]
      }

      const placeholder = this.buildTaskGeneratingItem(listType, chatTaskId)
      historyList.push(placeholder)
      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        this.scrollHistoryToItemBottom(listType, placeholder.id)
      })
      return placeholder
    },
    clearTaskGeneratingPlaceholder(listType) {
      if (!listType) return
      const historyList = this.getHistoryListByType(listType)
      if (!Array.isArray(historyList) || historyList.length === 0) return

      const filtered = historyList.filter(item => !item || item._generating !== true)
      if (filtered.length !== historyList.length) {
        this.setHistoryListByType(listType, filtered)
      }
    },
    getLatestPersistedHistoryId(listType) {
      const historyList = this.getHistoryListByType(listType)
      if (!Array.isArray(historyList) || historyList.length === 0) return 0

      for (let index = historyList.length - 1; index >= 0; index--) {
        const item = historyList[index]
        const historyId = Number(item?.id || 0)
        if (Number.isFinite(historyId) && historyId > 0) {
          return historyId
        }
      }
      return 0
    },
    pullTaskHistoryAfterStreamClosed(listType, chatTaskId) {
      if (!listType || !chatTaskId) return
      const nextHistoryId = this.getLatestPersistedHistoryId(listType)
      this.getTaskRecordListData(listType, chatTaskId, nextHistoryId || '', false)
    },
    setHistoryListByType(listType, list) {
      if (listType === 'task') this.$set(this, 'taskHistoryList', list)
      if (listType === 'agentTask') this.$set(this, 'agentDetailsTaskHistoryList', list)
      if (listType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskHistoryList', list)
      if (listType === 'groupTask') this.$set(this, 'groupTaskHistoryList', list)
    },
    getHistoryScrollbarRef(listType) {
      const scrollbarMap = {
        task: 'taskHistoryScrollbar',
        agentTask: 'agentDetailsTaskHistoryScrollbar',
        agentGroupTask: 'agentDetailsGroupTaskHistoryScrollbar',
        groupTask: 'groupTaskHistoryScrollbar'
      }
      return scrollbarMap[listType] || ''
    },
    isHistoryNearBottom(refName, isFirst = false) {
      if (isFirst) return true
      if (!refName) return true
      const scrollbar = this.$refs[refName]
      if (!scrollbar || !scrollbar.wrap) return true
      const wrap = scrollbar.wrap
      const scrollTop = wrap.scrollTop
      const scrollHeight = wrap.scrollHeight
      const clientHeight = wrap.clientHeight
      if (scrollHeight <= clientHeight) return true
      return scrollTop + clientHeight + 30 >= scrollHeight
    },
    scrollHistoryToItemBottom(listType, historyId, behavior = 'auto') {
      const refName = this.getHistoryScrollbarRef(listType)
      if (!refName) return
      const scrollbar = this.$refs[refName]
      if (!scrollbar || !scrollbar.wrap) return

      const wrap = scrollbar.wrap
      if (historyId === undefined || historyId === null || historyId === '') {
        wrap.scrollTop = wrap.scrollHeight
        return
      }

      const findTarget = () => {
        const historyItems = wrap.querySelectorAll('.taskrecord-listWrap-item[data-history-id]')
        for (let index = 0; index < historyItems.length; index++) {
          const item = historyItems[index]
          if (String(item.getAttribute('data-history-id')) === String(historyId)) {
            return item
          }
        }
        return null
      }

      let target = findTarget()
      const scrollWrapToTargetBottom = (targetItem) => {
        if (!targetItem) return
        const targetBottom = targetItem.offsetTop + targetItem.offsetHeight
        const nextTop = Math.max(0, targetBottom - wrap.clientHeight)
        if (behavior === 'smooth' && typeof wrap.scrollTo === 'function') {
          wrap.scrollTo({ top: nextTop, behavior: 'smooth' })
        } else {
          wrap.scrollTop = nextTop
        }
      }
      if (target) {
        scrollWrapToTargetBottom(target)
        return
      }

      requestAnimationFrame(() => {
        target = findTarget()
        if (target) {
          scrollWrapToTargetBottom(target)
        }
      })
    },
    startTaskHistoryStream(listType, chatTaskId, taskStatus) {
      if (!listType || !chatTaskId) {
        return
      }

      this.closeTaskHistoryStream(listType)

      this.setCurrentTaskStatusByType(listType, chatTaskId, taskStatus)
      if (!this.shouldSubscribeTaskStream(taskStatus)) {
        this.clearTaskGeneratingPlaceholder(listType)
        return
      }
      if (this.shouldShowTaskGenerating(taskStatus)) {
        this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
      }

      if (typeof EventSource === 'undefined') {
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
        return
      }

      const streamUrl = `/api/Senparc.Xncf.AgentsManager/ChatTaskStream/Subscribe?chatTaskId=${chatTaskId}&replayBuffered=false&_ts=${Date.now()}`
      const source = new EventSource(streamUrl, { withCredentials: true })
      this.historyStream[listType] = source
      this.loadPendingHumanRequests(chatTaskId)
      this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)

      const rearmSilentTimer = () => {
        if (this.historyStream[listType] !== source) return
        this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)
      }

      const onChunk = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.upsertTaskStreamChunk(listType, event)
        rearmSilentTimer()
      }
      const onMessage = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.flushTaskStreamMessage(listType, event)
        rearmSilentTimer()
      }
      const onStatus = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        const payload = this.safeParseStreamEvent(event)
        if (!payload || !payload.text) {
          rearmSilentTimer()
          return
        }

        const statusText = String(payload.text).toLowerCase().trim()
        const statusCodeMap = {
          chatting: 1,
          paused: 2,
          finished: 3,
          completed: 3,
          done: 3,
          cancelled: 4,
          canceled: 4,
          failed: 5,
          error: 5
        }
        const nextStatus = statusCodeMap[statusText]
        if (Number.isFinite(nextStatus)) {
          this.setCurrentTaskStatusByType(listType, chatTaskId, nextStatus)
        }

        if ([3, 4, 5].includes(Number(nextStatus))) {
          this.closeTaskHistoryStream(listType)
          this.clearTaskGeneratingPlaceholder(listType)
          this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
          return
        }

        if (this.shouldShowTaskGenerating(nextStatus)) {
          this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
        } else {
          this.clearTaskGeneratingPlaceholder(listType)
        }
        rearmSilentTimer()
      }

      const onHumanRequest = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        const payload = this.safeParseStreamEvent(event)
        if (payload) {
          this.setCurrentTaskStatusByType(listType, chatTaskId, 2)
          this.handleHumanApprovalRequest(payload)
        }
        rearmSilentTimer()
      }
      const onHumanResolved = (event) => {
        if (this.historyStream[listType] !== source) return
        const payload = this.safeParseStreamEvent(event)
        const requestId = this.getHumanRequestId(payload)
        if (requestId) {
          this.removeResolvedHumanRequest(requestId)
        }
        rearmSilentTimer()
      }

      source.addEventListener('chunk', onChunk)
      source.addEventListener('message', onMessage)
      source.addEventListener('status', onStatus)
      source.addEventListener('humanRequest', onHumanRequest)
      source.addEventListener('humanResolved', onHumanResolved)

      source.onerror = () => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.closeTaskHistoryStream(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
      }
    },
    getHumanRequestId(payload) {
      return String(payload?.humanRequestId || payload?.requestId || '')
    },
    formatToolApprovalArguments(rawArguments) {
      let value = rawArguments
      if (value === undefined || value === null || String(value).trim() === '') {
        return '（未提供参数）'
      }

      for (let index = 0; index < 2 && typeof value === 'string'; index++) {
        const text = value.trim()
        if (!text || !['{', '[', '"'].includes(text[0])) break
        try {
          value = JSON.parse(text)
        } catch (_) {
          break
        }
      }

      if (typeof value === 'string') {
        return value
      }

      try {
        return JSON.stringify(value, null, 2)
      } catch (_) {
        return String(value)
      }
    },
    showNextToolApproval() {
      if (this.toolApprovalRequest || this.toolApprovalQueue.length === 0) return
      const request = this.toolApprovalQueue.shift()
      this.toolApprovalRequest = request
      this.toolApprovalArgumentText = this.formatToolApprovalArguments(
        request?.humanToolArguments ?? request?.toolArguments)
      this.toolApprovalDialogVisible = true
    },
    async handleHumanApprovalRequest(payload) {
      const requestId = this.getHumanRequestId(payload)
      if (!requestId || this.humanApprovalRequests[requestId]) {
        return
      }

      this.$set(this.humanApprovalRequests, requestId, true)
      if (String(payload?.humanRequestType || payload?.requestType || '').toLowerCase() === 'humanturn') {
        this.humanReplyRequest = payload
        this.humanReplyText = ''
        this.humanReplyDialogVisible = true
        return
      }

      this.toolApprovalQueue.push({
        ...payload,
        requestId
      })
      this.showNextToolApproval()
    },
    async resolveToolApproval(approved) {
      const request = this.toolApprovalRequest
      const requestId = this.getHumanRequestId(request)
      if (!requestId || this.toolApprovalSubmitting) return

      this.toolApprovalSubmitting = true
      try {
        const reason = approved ? '用户确认' : '用户拒绝'
        const query = getInterfaceQueryStr({ requestId, approved, reason })
        const response = await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ResolveHumanRequest?${query}`)
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || '人工审批提交失败')
          return
        }
        this.$message.success(approved ? '已批准工具调用，任务继续执行' : '已拒绝工具调用，任务继续处理')
        this.toolApprovalDialogVisible = false
        this.toolApprovalRequest = null
        this.toolApprovalArgumentText = ''
        this.$delete(this.humanApprovalRequests, requestId)
        this.$nextTick(() => this.showNextToolApproval())
      } catch (error) {
        this.$message.error(error?.message || '人工审批提交失败')
      } finally {
        this.toolApprovalSubmitting = false
      }
    },
    deferToolApproval() {
      const requestId = this.getHumanRequestId(this.toolApprovalRequest)
      this.toolApprovalDialogVisible = false
      this.toolApprovalRequest = null
      this.toolApprovalArgumentText = ''
      if (requestId) {
        this.$delete(this.humanApprovalRequests, requestId)
      }
      this.$nextTick(() => this.showNextToolApproval())
    },
    removeResolvedHumanRequest(requestId) {
      if (!requestId) return
      this.toolApprovalQueue = this.toolApprovalQueue
        .filter(item => this.getHumanRequestId(item) !== requestId)
      if (this.getHumanRequestId(this.toolApprovalRequest) === requestId) {
        this.toolApprovalDialogVisible = false
        this.toolApprovalRequest = null
        this.toolApprovalArgumentText = ''
        this.$nextTick(() => this.showNextToolApproval())
      }
      this.$delete(this.humanApprovalRequests, requestId)
    },
    async submitHumanReply() {
      const requestId = String(this.humanReplyRequest?.humanRequestId || this.humanReplyRequest?.requestId || '')
      const input = String(this.humanReplyText || '').trim()
      if (!requestId || !input) {
        this.$message.warning('请输入 Human 回复')
        return
      }

      this.humanReplySubmitting = true
      try {
        const response = await serviceAM.post(
          '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.SendHumanMessage',
          { requestId, input })
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || 'Human 回复提交失败')
          return
        }

        const submittedRequestId = requestId
        this.humanReplyDialogVisible = false
        this.humanReplyRequest = null
        this.humanReplyText = ''
        this.$delete(this.humanApprovalRequests, submittedRequestId)
        this.$message.success('Human 回复已提交')
      } catch (error) {
        this.$message.error(error?.message || 'Human 回复提交失败')
      } finally {
        this.humanReplySubmitting = false
      }
    },
    closeHumanReplyDialog() {
      const requestId = String(this.humanReplyRequest?.humanRequestId || this.humanReplyRequest?.requestId || '')
      this.humanReplyDialogVisible = false
      this.humanReplyRequest = null
      this.humanReplyText = ''
      if (requestId) {
        this.$delete(this.humanApprovalRequests, requestId)
      }
    },
    async loadPendingHumanRequests(chatTaskId) {
      if (!chatTaskId) return
      try {
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetHumanRequests?chatTaskId=${encodeURIComponent(chatTaskId)}`,
          { customAlert: true }
        )
        const pendingRequests = response?.data?.data || []
        pendingRequests.forEach(request => this.handleHumanApprovalRequest(request))
      } catch (error) {
        console.warn('load pending human approval requests failed', chatTaskId, error)
      }
    },
    clearTaskHistoryStreamSilentTimer(listType) {
      const timer = this.historyStreamSilentTimer[listType]
      if (timer) {
        clearTimeout(timer)
      }
      this.$delete(this.historyStreamSilentTimer, listType)
    },
    resetTaskHistoryStreamSilentTimer(listType, chatTaskId) {
      this.clearTaskHistoryStreamSilentTimer(listType)
      this.historyStreamSilentTimer[listType] = setTimeout(async () => {
        const source = this.historyStream[listType]
        if (!source) return

        try {
          const statusRes = await serviceAM.get(
            `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${chatTaskId}`,
            { customAlert: true }
          )
          const taskStatus = Number(statusRes?.data?.data?.chatTaskDto?.status)
          this.setCurrentTaskStatusByType(listType, chatTaskId, taskStatus)
          if ([3, 4, 5].includes(taskStatus)) {
            this.closeTaskHistoryStream(listType)
            this.clearTaskGeneratingPlaceholder(listType)
            this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
            return
          }

          if (this.shouldShowTaskGenerating(taskStatus)) {
            this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
          } else {
            this.clearTaskGeneratingPlaceholder(listType)
          }
        } catch (e) {
          console.warn('stream silent fallback status check failed', listType, chatTaskId, e)
        }

        // 任务仍在运行但暂无流事件，继续观察，避免长时间 pending 无更新。
        this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)
      }, 4000)
    },
    safeParseStreamEvent(event) {
      if (!event || !event.data) return null
      try {
        return JSON.parse(event.data)
      } catch (e) {
        console.error('stream parse error', e, event.data)
        return null
      }
    },
    upsertTaskStreamChunk(listType, event) {
      const payload = this.safeParseStreamEvent(event)
      if (!payload || !payload.responseId) return

      const draftKey = `${listType}:${payload.responseId}`
      const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef(listType), false)
      const historyList = this.getHistoryListByType(listType).filter(item => !item || item._generating !== true).slice()
      const existedIndex = historyList.findIndex(item => item.id === draftKey)
      const agentInfo = this.getTaskSenderInfo(listType, payload) || {}
      const oldMessage = existedIndex > -1 ? (historyList[existedIndex].message || '') : ''
      const mergedMessage = `${oldMessage}${payload.text || ''}`

      const draftItem = {
        id: draftKey,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        fromParticipantKey: payload.fromParticipantKey || '',
        fromParticipantKind: payload.fromParticipantKind || '',
        fromParticipantName: payload.fromAgentName || '',
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message: mergedMessage,
        messageHtml: this.renderSafeMarkdown(mergedMessage || ''),
        promptTokens: payload.promptTokens || 0,
        completionTokens: payload.completionTokens || 0,
        totalTokens: payload.totalTokens || 0,
        responseMilliseconds: payload.responseMilliseconds || 0,
        roundIndex: payload.roundIndex || 0,
        _streaming: true,
        _streamAgentName: payload.fromAgentName || agentInfo.name || '',
      }

      if (existedIndex > -1) {
        historyList.splice(existedIndex, 1, draftItem)
      } else {
        historyList.push(draftItem)
      }

      this.historyStreamingDrafts[draftKey] = draftItem
      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        if (!shouldAutoFollow) return
        this.scrollHistoryToItemBottom(listType, draftItem.id)
      })
    },
    flushTaskStreamMessage(listType, event) {
      const payload = this.safeParseStreamEvent(event)
      if (!payload) return

      const draftKey = payload.responseId ? `${listType}:${payload.responseId}` : ''
      const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef(listType), false)
      const historyList = this.getHistoryListByType(listType).filter(item => !item || item._generating !== true).slice()
      if (draftKey) {
        const draftIndex = historyList.findIndex(item => item.id === draftKey)
        if (draftIndex > -1) {
          historyList.splice(draftIndex, 1)
        }
        delete this.historyStreamingDrafts[draftKey]
      }

      const message = payload.text || ''
      const historyId = Number(payload.historyId || 0)
      const existedFinalIndex = historyId > 0
        ? historyList.findIndex(item => Number(item?.id || 0) === historyId)
        : -1
      if (existedFinalIndex > -1) {
        const existedFinal = historyList[existedFinalIndex] || {}
        const mergedFinal = {
          ...existedFinal,
          fromAgentTemplateId: payload.fromAgentTemplateId || existedFinal.fromAgentTemplateId || 0,
          fromParticipantKey: payload.fromParticipantKey || existedFinal.fromParticipantKey || '',
          fromParticipantKind: payload.fromParticipantKind || existedFinal.fromParticipantKind || '',
          fromParticipantName: payload.fromAgentName || existedFinal.fromParticipantName || '',
          addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : (existedFinal.addTime || new Date().toISOString()),
          message: message || existedFinal.message || '',
          messageHtml: this.renderSafeMarkdown(message || existedFinal.message || ''),
          promptTokens: payload.promptTokens || existedFinal.promptTokens || 0,
          completionTokens: payload.completionTokens || existedFinal.completionTokens || 0,
          totalTokens: payload.totalTokens || existedFinal.totalTokens || 0,
          responseMilliseconds: payload.responseMilliseconds || existedFinal.responseMilliseconds || 0,
          roundIndex: payload.roundIndex || existedFinal.roundIndex || 0
        }
        historyList.splice(existedFinalIndex, 1, mergedFinal)
        this.setHistoryListByType(listType, historyList)
        this.$nextTick(() => {
          if (!shouldAutoFollow) return
          this.scrollHistoryToItemBottom(listType, mergedFinal.id)
        })
        return
      }

      const finalItem = {
        id: payload.historyId || `${draftKey || 'msg'}:${Date.now()}`,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        fromParticipantKey: payload.fromParticipantKey || '',
        fromParticipantKind: payload.fromParticipantKind || '',
        fromParticipantName: payload.fromAgentName || '',
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message,
        messageHtml: this.renderSafeMarkdown(message),
        promptTokens: payload.promptTokens || 0,
        completionTokens: payload.completionTokens || 0,
        totalTokens: payload.totalTokens || 0,
        responseMilliseconds: payload.responseMilliseconds || 0,
        roundIndex: payload.roundIndex || 0
      }
      historyList.push(finalItem)

      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        if (!shouldAutoFollow) return
        this.scrollHistoryToItemBottom(listType, finalItem.id)
      })

      // 每轮 message 落地后立即补一个 Generating 占位，确保下一轮也有可见彩虹提示。
      if (this.historyStream[listType]) {
        this.ensureTaskGeneratingPlaceholder(listType, payload.chatTaskId || '')
      }
    },
    closeTaskHistoryStream(listType) {
      this.clearTaskHistoryStreamSilentTimer(listType)
      const source = this.historyStream[listType]
      if (source) {
        source.close()
      }
      this.$delete(this.historyStream, listType)
      this.clearTaskGeneratingPlaceholder(listType)
    },
    clearTaskHistoryStreams() {
      Object.keys(this.historyStream || {}).forEach((key) => {
        this.closeTaskHistoryStream(key)
      })
      Object.keys(this.historyStreamSilentTimer || {}).forEach((key) => {
        this.clearTaskHistoryStreamSilentTimer(key)
      })
      this.historyStreamingDrafts = {}
    },
    // 清除 获取历史对话记录 的轮询
    clearHistoryTimer() {
      for (const key in this.historyTimer) {
        if (Object.prototype.hasOwnProperty.call(this.historyTimer, key)) {
          const element = this.historyTimer[key];
          // console.log('clearHistoryTimer', element);
          if (element) {
            clearTimeout(element)
          }
        }
      }
      this.clearTaskListRetryTimers()
      this.clearTaskHistoryStreams()
    },

    // 编辑 Dailog|抽屉 按钮 
    async handleEditDrawerOpenBtn(btnType, item) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      //console.log('handleEditDrawerOpenBtn', btnType, item);
      let formName = ''
      // 智能体
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        formName = 'agentForm'
      }
      // 组
      if (btnType === 'drawerGroup') {
        formName = 'groupForm'
      }
      // 组 启动 
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        formName = 'groupStartForm'
      }
      // 任务 评价
      if (btnType === 'dialogTaskEvaluation') {
        formName = 'evaluationForm'
      }
      if (formName) {
          if (btnType === 'drawerAgent' && item) {
          console.log('item', item);
          // 创建一个新的对象来存储表单数据
          const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };
          console.log('formData', formData);

          // FunctionBindings 是新契约；没有该字段时回退到旧版插件类名列表。
          const loadedBindings = Array.isArray(formData.functionBindings)
            ? formData.functionBindings
            : (formData.functionCallNames
              ? formData.functionCallNames.split(',').filter(Boolean).map(name => ({
                kind: 'plugin',
                key: name,
                name
              }))
              : [])
          this.$set(this.agentForm, 'functionBindings', loadedBindings.map(item => this.normalizeFunctionBinding(item)).filter(Boolean))
          this.syncLegacyFunctionCallNames()
          this.functionCallTags = this.agentForm.functionCallNames ? this.agentForm.functionCallNames.split(',').filter(Boolean) : [];

          // 将数据赋值给表单
          Object.assign(this[formName], formData);

          // 打印日志以便调试
          console.log('Loaded form data:', formData);
          console.log('functionCallTags:', this.functionCallTags);

          } else if (btnType === 'drawerGroup') {
            // 列表/详情对象字段并不完全一致；始终由专用接口取得本地和远程成员，
            // 这样编辑既有 Group 时不会误清除已配置的 A2A 成员。
            const groupId = item.chatGroupDto ? item.chatGroupDto.id : item.id
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${groupId}`)
              .then(res => {
                const data = res?.data ?? {}
                if (data.success) {
                  const groupDetail = data?.data ?? {}
                  Object.assign(this[formName], {
                    ...groupDetail.chatGroupDto,
                    members: groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || [],
                    includeHumanParticipant: (groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || [])
                      .some(member => member && member.isHuman === true),
                    remoteMembers: (groupDetail.remoteMemberDtoList || []).map(member => member.remoteAgentDto || member)
                  })
                }
              })
          // // 获取 全部智能体数据
          // this.getAgentListData('groupAgent')
        } else if (btnType === 'drawerTaskStart') {
          Object.assign(this[formName], {
            ...item
            // groupName: item?.name ?? ''
          })
        } else {
          Object.assign(this[formName], item)
        }
        // 回显 表单值
        // this.$set(this, `${formName}`, deepClone(item))
        if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)
          && (item?.agentTemplateDto?.id || item?.id)) {
          // systemMessageType 不是持久化字段。先挂载“自选”控件并完成候选项加载，
          // 再根据实际是否存在该 PromptCode 判断类型，避免慢网络下被过早判定为“手动”。
          this.agentSystemMessageTypeDetectionPending = true
          this.$set(this.agentForm, 'systemMessageType', '1')
        }
        // 打开 抽屉
        this.handleElVisibleOpenBtn(btnType)
      }
    },
};

