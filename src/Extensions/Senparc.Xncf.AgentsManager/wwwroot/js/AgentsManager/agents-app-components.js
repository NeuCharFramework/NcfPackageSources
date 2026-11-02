/*
 * AgentsManager 前端：participant-quick-action / task-html-renderer / load-more-select 子组件。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

// 任务右侧成员列表：名称始终完整展示，点击后查看与当前任务关联的摘要。
Vue.component('participant-quick-action', {
  props: {
    participant: {
      type: Object,
      required: true
    },
    quickInfo: {
      type: Object,
      required: true
    },
    testing: {
      type: Boolean,
      default: false
    }
  },
  methods: {
    avatarUrl() {
      return this.participant?.avastar || '/images/AgentsManager/avatar/avatar1.png'
    },
    testRemote() {
      this.$emit('test-remote')
    },
    editAgent() {
      this.$emit('edit-agent')
    }
  },
  template: `
    <el-popover placement="left-start" :width="320" trigger="click" popper-class="participant-quick-info-popover">
      <section class="participant-quick-info">
        <div class="participant-quick-info__header">
          <img :src="avatarUrl()" alt="" class="participant-quick-info__avatar">
          <div>
            <div class="participant-quick-info__name">{{ participant.name }}</div>
            <div class="participant-quick-info__tags">
              <el-tag :type="quickInfo.kindType" size="mini">{{ quickInfo.kindText }}</el-tag>
              <el-tag :type="quickInfo.statusType" size="mini">{{ quickInfo.statusText }}</el-tag>
            </div>
          </div>
        </div>
        <div class="participant-quick-info__description">{{ quickInfo.description }}</div>
        <div class="participant-quick-info__metrics">
          <div><span>当前任务用量</span><strong>{{ quickInfo.currentUsageText }}</strong></div>
          <div><span>平均响应</span><strong>{{ quickInfo.responseTimeText }}</strong></div>
          <div><span>累计 Token</span><strong>{{ quickInfo.totalUsageText }}</strong></div>
          <div><span>最近活动</span><strong>{{ quickInfo.activityText }}</strong></div>
        </div>
        <div v-if="quickInfo.isRemote" class="participant-quick-info__health" :title="quickInfo.healthMessage">
          <span>连接检测</span>
          <span>{{ quickInfo.healthMessage }}</span>
        </div>
        <div class="participant-quick-info__footer">
          <span>用量仅统计当前任务中已加载的记录</span>
          <el-button v-if="quickInfo.canOpenEditor" type="text" size="mini" icon="el-icon-top-right" title="在新窗口中编辑 Agent" @click.stop="editAgent">编辑</el-button>
          <el-button v-if="quickInfo.isRemote" type="text" size="mini" :loading="testing" @click.stop="testRemote">测试连接</el-button>
        </div>
      </section>
      <button slot="reference" type="button" class="taskmain-member-action" :title="'查看 ' + participant.name + ' 的信息'">
        <span class="taskmain-member-action__avatar">
          <img :src="avatarUrl()" alt="">
        </span>
        <span class="taskmain-member-action__content">
          <span class="taskmain-member-action__name">{{ participant.name }}</span>
          <span class="taskmain-member-action__meta">
            <el-tag :type="quickInfo.kindType" size="mini">{{ quickInfo.kindText }}</el-tag>
            <el-tag :type="quickInfo.statusType" size="mini">{{ quickInfo.statusText }}</el-tag>
          </span>
        </span>
        <i class="el-icon-arrow-right taskmain-member-action__arrow" aria-hidden="true"></i>
      </button>
    </el-popover>
  `
})

// task-html-renderer 渲染任务对话记录的内容
Vue.component('task-html-renderer', {
  props: ['content'],
  render(createElement) {
    return createElement('div', {
      class: 'taskrecord-listWrap-item-content', // 使用 CSS 类
      domProps: {
        innerHTML: sanitizeTaskHtml(this.content)
      }
    });
  }
});

// 注册一个全局自定义指令 v-el-select-loadmore
Vue.directive('el-select-loadmore', {
  bind(el, binding, vnode) {
    // 获取element-ui定义好的scroll盒子
    const SELECTWRAP_DOM = el.querySelector('.el-select-dropdown .el-select-dropdown__wrap')
    SELECTWRAP_DOM.addEventListener('scroll', function () {
      /**
      * scrollHeight 获取元素内容高度(只读)
      * scrollTop 获取或者设置元素的偏移值,常用于, 计算滚动条的位置, 当一个元素的容器没有产生垂直方向的滚动条, 那它的scrollTop的值默认为0.
      * clientHeight 读取元素的可见高度(只读)
      * 如果元素滚动到底, 下面等式返回true, 没有则返回false:
      * ele.scrollHeight - ele.scrollTop === ele.clientHeight;
      */
      const condition = this.scrollHeight - this.scrollTop <= this.clientHeight
      if (condition) {
        binding.value()
      }
    })
  }
})

// load-more-select 组件
Vue.component('load-more-select', {
  // v-el-select-loadmore="interestsLoadmore" filterable remote collapse-tags reserve-keyword :remote-method="remoteMethod" @focus="remoteMethod('',true)" @visible-change="reverseArrow"
  template: `<div :class="[direction === 'horizontal' ? 'df-wn flex-ac flex-js' : '']" style="width:100%;gap:10px;">
        <el-select ref="elSelectLoadMore" v-model="selectVal"  :disabled="disabled" :loading="interesLoading" :placeholder="placeholder" filterable :multiple="multipleChoice" clearable style="width:100%" @change="handleChange">
    <el-option v-for="(item,index) in interestsOptions" :key="item.value" :label="item.label" :value="item.value">
      <template v-if="serviceType === 'model'">
        <span>{{item.label}}</span>
        <span style="float:right;display:flex;gap:4px;">
          <el-tag size="mini" effect="plain">{{modelTypeLabel(item.configModelType)}}</el-tag>
          <el-tag size="mini" type="info" effect="plain">{{modelPlatformLabel(item.aiPlatform)}}</el-tag>
          <el-tag v-if="item.deploymentName || item.modelId" size="mini" type="warning" effect="plain">{{item.deploymentName || item.modelId}}</el-tag>
        </span>
      </template>
      <template v-else>{{item.label}}</template>
    </el-option></el-select>
    <template v-if="direction==='horizontal'">
        <i class="cursorPointer fas fa-redo" title="刷新" @click="refreshManagementList" />
    </template>
    <template v-else>
        <el-button size="mini" @click="refreshManagementList" :loading="interesLoading">刷新</el-button>
        <el-button v-if="serviceType === 'systemMessage'" type="primary" size="mini" @click="jumpPromptRange('promptRange')">管理PromptRange</el-button>
        <el-button v-if="serviceType === 'model'" type="primary" size="mini" @click="jumpPromptRange('model')">管理模型</el-button>
    </template>
    
    </div>`,
  props: {
    // eslint-disable-next-line vue/require-prop-types
    value: {
      // type: [Array, String, Number],
      required: true
    },
    placeholder: {
      type: String,
      default: ''
    },
    multipleChoice: {
      type: Boolean,
      default: false
    },
    serviceType: {
      type: String,
      default: '' // 默认使用公共 
    },
    misiptvId: {
      type: [String, Number],
      default: ''
    },
    disabled: {
      type: Boolean,
      default: false
    },
    direction: {
      type: String,
      default: 'horizontal' // 横向/竖向  horizontal/vertical
    },
    chatOnly: {
      type: Boolean,
      default: false
    }
  },
  data: function () {
    return {
      optionVisible: false,
      interestsOptions: [], //  接口返回数据
      interesLoading: false,
      currentPageSize: 0,
      listQuery: {
        pageIndex: 0,
        pageSize: 0,
        // key: '',
        filter: ''
      }
    }
  },
  computed: {
    selectVal: {
      get() {
        if (this.multipleChoice) {
          return [...this.value]
        } else {
          return this.value ?? ''
        }
      },
      set(val) {
        if (this.multipleChoice) {
          this.$emit('input', [...val])
        } else {
          this.$emit('input', val)
        }
      }
    }
  },
  watch: {
    // serviceType: {
    //     handler(val = '') {
    //         this.listQuery.key = val
    //     },
    //     immediate: true
    // }
  },
  mounted() {
    // 找到dom
    // const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
    //     '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
    // )
    // // 对dom新增class
    // rulesDom?.classList.add('el-icon-arrow-up')
    this.refreshManagementList()
  },
  methods: {
    modelTypeLabel(type) {
      return Number(type) === 2 ? 'Chat' : `类型 ${type ?? '未知'}`
    },
    modelPlatformLabel(platform) {
      const labels = { 1: 'OpenAI', 2: 'Azure OpenAI', 3: 'Hugging Face', 4: 'NeuCharAI' }
      return labels[Number(platform)] || `平台 ${platform ?? '未知'}`
    },
    jumpPromptRange(urlType) {
      let url = ''
      if (urlType === 'promptRange') {
        const selectedPromptCode = typeof this.selectVal === 'string'
          ? this.selectVal.trim()
          : ''
        if (selectedPromptCode) {
          url = `/Admin/PromptRange/Prompt?handler=Resolve&uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18&promptCode=${encodeURIComponent(selectedPromptCode)}`
        } else {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        }
      }
      if (urlType === 'model') {
        url = `/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69`
      }
      if (!url) return
      simulationAELOperation(url)
      // openWindow(url)
    },
    reverseArrow(flag) {
      this.optionVisible = flag
      // 找到dom
      const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
        '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
      )
      if (flag) {
        rulesDom.classList.add('is-reverse') // 对dom新增class
      } else {
        rulesDom.classList.remove('is-reverse') // 对dom新增class
      }
    },
    handleChange(e) {
      if (this.multipleChoice) {
        const filterItem = this.interestsOptions.filter((item) => {
          return e.includes(item.value)
        })
        this.$emit('change', filterItem)
      } else {
        const fintItem = this.interestsOptions.find((item) => item.value === e)
        this.$emit('change', fintItem)
      }
    },
    // 远程搜索
    remoteMethod(query, isfocus) {
      // console.log(query, 8888, this.optionVisible,isfocus)
      if (this.optionVisible && isfocus) return
      this.listQuery.filter = query ?? ''
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption() // 请求接口
    },
    interestsLoadmore() {
      setTimeout(() => {
        this.listQuery.pageIndex = this.listQuery.pageIndex + 1
        if (this.listQuery.pageSize > this.currentPageSize) {
          this.listQuery.pageIndex = this.listQuery.pageIndex - 1
          return
        }
        this.managementListOption()
      }, 1000)
    },
    // 刷新接口
    refreshManagementList() {
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption()
    },
    // 调用接口
    managementListOption() {
      // console.log('managementListOption',this.serviceType);
      this.interesLoading = true // 本地搜索 调用
      if (this.serviceType === 'agent') {
        serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList?${getInterfaceQueryStr(this.listQuery)}`)
          .then(res => {
            // console.log('this.serviceType === agent', res);
            const data = res?.data ?? {}
            if (data.success) {
              const agentData = data?.data?.list ?? []
              const listData = agentData.map(item => {
                return {
                  ...item,
                  label: item.name,
                  value: item.id,
                  disabled: false
                }
              })
              this.interesLoading = false
              this.currentPageSize = listData?.length ?? 0
              //this.interestsOptions = this.interestsOptions.concat(listData)
              this.interestsOptions = listData
              // [...this.interestsOptions, ...listData]
              // console.log(this.interestsOptions, 888)
            } else {
              app.$message({
                message: data.errorMessage || data.data || 'Error',
                type: 'error',
                duration: 5 * 1000
              })
            }
          })
      } else if (this.serviceType === 'model') {
        serviceAM.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', this.listQuery).then(res => {
          // console.log('this.serviceType === model', res);
          const data = res?.data ?? {}
          if (data.success) {
            //console.log('getModelOptData:', res.data)
            const modelData = (data?.data ?? [])
              .filter(item => !this.chatOnly || Number(item.configModelType) === 2)
            const listData = modelData.map(item => {
              return {
                ...item,
                label: item.alias,
                value: item.id,
                disabled: false
              }
            })
            this.interesLoading = false
            this.currentPageSize = listData?.length ?? 0
            this.interestsOptions = this.interestsOptions.concat(listData)
            // [...this.interestsOptions, ...listData]
            // console.log(this.interestsOptions, 888)
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      } else if (this.serviceType === 'systemMessage') {
        // /api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetPromptRangeTree
        // /api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.GetPromptRangeTree
        serviceAM.get('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.GetPromptRangeTree', this.listQuery).then(res => {
          // console.log('this.serviceType === systemMessage', res);
          const data = res?.data ?? {}
          if (data.success) {
            //console.log('getModelOptData:', res.data)
            const promptRangeData = data?.data ?? []
            const listData = promptRangeData.map(item => {
              return {
                ...item,
                label: item.text,
                disabled: false
              }
            })
            this.interesLoading = false
            this.currentPageSize = listData?.length ?? 0
            this.interestsOptions = this.interestsOptions.concat(listData)
            this.$emit('options-loaded', this.interestsOptions)
            // [...this.interestsOptions, ...listData]
            // console.log(this.interestsOptions, 888)
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }

    }
  },
})
