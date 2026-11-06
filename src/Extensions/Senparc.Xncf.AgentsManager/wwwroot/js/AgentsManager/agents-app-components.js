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

// 对话长度导航：长条高度按文本长度分配，彼此相接，弹层贴在右侧供点选。
Vue.component('conversation-length-rail', {
  props: {
    items: { type: Array, default: function () { return [] } },
    listType: { type: String, default: 'studio' }
  },
  data: function () {
    return { hoverIndex: -1, selectedId: null, popTop: 0, closeTimer: null }
  },
  beforeDestroy: function () {
    clearTimeout(this.closeTimer)
  },
  computed: {
    bars: function () {
      const host = this.$parent
      const colors = ['#3d8bfd', '#2faf9a', '#e09a3e', '#7b6fe0', '#d25d88', '#4aa0c4', '#6ea344', '#c47b4a']
      return (this.items || []).filter(function (item) {
        if (!item || item._generating) return false
        if (this.listType === 'studio' || !host || typeof host.historyMatchesMemberFilter !== 'function') return true
        const filters = {
          task: ['taskMemberfilter', 'taskMemberfilterList'],
          agentTask: ['agentTaskMemberfilter', 'agentTaskMemberfilterList'],
          agentGroupTask: ['agentGroupTaskMemberfilter', 'agentGroupTaskMemberfilterList'],
          groupTask: ['groupTaskMemberfilter', 'groupTaskMemberfilterList']
        }[this.listType]
        return !filters || host.historyMatchesMemberFilter(item, host[filters[0]], host[filters[1]])
      }.bind(this)).map(function (item) {
        const text = String(item.message || '').replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim()
        const name = this.speakerName(item)
        let hash = 0
        for (let i = 0; i < name.length; i++) hash = (hash + name.charCodeAt(i)) % colors.length
        return {
          id: item.id,
          item: item,
          name: name,
          excerpt: text.slice(0, 90) || '…',
          weight: Math.max(text.length, 12),
          color: colors[hash]
        }
      }.bind(this))
    },
    active: function () {
      return this.hoverIndex >= 0 ? this.bars[this.hoverIndex] : null
    }
  },
  watch: {
    bars: function () {
      if (this.hoverIndex >= this.bars.length) this.hoverIndex = -1
    }
  },
  methods: {
    speakerName: function (item) {
      const host = this.$parent
      if (this.listType !== 'studio' && host && typeof host.getTaskSenderName === 'function') {
        return host.getTaskSenderName(this.listType, item) || ''
      }
      return item.fromParticipantName
        || (item.fromAgentTemplate && item.fromAgentTemplate.name)
        || (typeof ncfT === 'function' ? ncfT('AgentsManager.Studio.Agent') : 'Agent')
    },
    locateLabel: function () {
      const key = 'AgentsManager.Studio.RailLocate'
      const translated = typeof ncfT === 'function' ? ncfT(key) : ''
      return translated && translated !== key ? translated : '定位到这条'
    },
    cancelClose: function () {
      clearTimeout(this.closeTimer)
    },
    closeSoon: function () {
      const self = this
      clearTimeout(this.closeTimer)
      this.closeTimer = setTimeout(function () { self.hoverIndex = -1 }, 220)
    },
    open: function (index, event) {
      this.cancelClose()
      this.hoverIndex = index
      const bar = event.currentTarget
      const rail = this.$el.getBoundingClientRect()
      const box = bar.getBoundingClientRect()
      const popHeight = 132
      let top = box.top - rail.top
      const maxTop = Math.max(0, rail.height - popHeight)
      top = Math.min(Math.max(0, top), maxTop)
      if (top > box.bottom - rail.top - 12) top = Math.max(0, box.bottom - rail.top - 12)
      this.popTop = top
    },
    choose: function (bar) {
      if (!bar) return
      this.selectedId = bar.id
      this.hoverIndex = this.bars.findIndex(function (item) { return item.id === bar.id })
      const host = this.$el.parentElement
      if (!host) return
      host.querySelectorAll('.is-rail-selected').forEach(function (node) {
        node.classList.remove('is-rail-selected')
      })
      const target = host.querySelector('[data-history-id="' + bar.id + '"]')
      if (!target) return
      target.classList.add('is-rail-selected')
      target.scrollIntoView({ block: 'center', behavior: 'smooth' })
    },
    move: function (event) {
      if (!this.bars.length || (event.key !== 'ArrowDown' && event.key !== 'ArrowUp')) return
      event.preventDefault()
      const next = event.key === 'ArrowDown'
        ? Math.min(this.bars.length - 1, Math.max(0, this.hoverIndex) + 1)
        : Math.max(0, (this.hoverIndex < 0 ? 0 : this.hoverIndex) - 1)
      const button = this.$el.querySelectorAll('.conversation-rail-bar')[next]
      if (button) {
        button.focus()
        this.open(next, { currentTarget: button })
      }
    }
  },
  template: `
    <aside v-if="bars.length" class="conversation-rail" :class="{'is-studio': listType === 'studio'}"
      @mouseleave="closeSoon" @mouseenter="cancelClose" @keydown="move">
      <div class="conversation-rail-track" role="listbox" aria-label="对话">
        <button v-for="(bar, index) in bars" :key="bar.id" type="button"
          class="conversation-rail-bar" role="option"
          :class="{'is-active': index === hoverIndex, 'is-selected': bar.id === selectedId}"
          :style="{ flexGrow: bar.weight, background: bar.color }"
          :aria-selected="bar.id === selectedId" :title="bar.name"
          @mouseenter="open(index, $event)" @focus="open(index, $event)" @click="choose(bar)"></button>
      </div>
      <div v-if="active" class="conversation-rail-pop" :style="{ top: popTop + 'px' }"
        @mouseenter="cancelClose">
        <strong>{{ active.name }}</strong>
        <p>{{ active.excerpt }}</p>
        <button type="button" @click="choose(active)">{{ locateLabel() }}</button>
      </div>
    </aside>
  `
})
