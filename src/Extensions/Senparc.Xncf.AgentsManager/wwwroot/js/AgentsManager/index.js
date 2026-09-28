/*
 * AgentsManager 前端：Vue 根实例组装（数据/计算/方法片段由 agents-app-*.js 提供，加载顺序见 three-loader.js）。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var app = new Vue({
  el: "#app",
  filters: {
    showFormatDate(value) {
      if (!value) return ''
      return formatDate(value)
    },
    showAvatar(val) {
      return val || '/images/AgentsManager/avatar/avatar1.png'
    }
  },
  data() {
    return Object.assign({}, AgentsAppData);
  },
  computed: AgentsAppComputed,
  watch: {},
  created() {
    // 在组件创建时获取插件类型列表
    this.getPluginTypes();
    this.getKnowledgeBaseOptions();
  },
  mounted() {
    this.tabsActiveName = "first";
    this.agentForm.systemMessageType = "2";
    this.getPluginTypes();

    // 智能体
    if (this.tabsActiveName === 'first') {
      this.getAgentListData('agent')
    }
    // 组
    if (this.tabsActiveName === 'second') {
      this.getGroupListData('group')
    }
    // 任务
    if (this.tabsActiveName === 'third') {
      this.gettaskListData('task')
    }

    this.hashChangeHandler = () => {
      this.applyHashRoute()
    }
    window.addEventListener('hashchange', this.hashChangeHandler)
    this.refreshQuickJumpTaskOptions()
    this.$nextTick(() => {
      this.applyHashRoute()
    })

  },
  beforeDestroy() {
    this.clearHistoryTimer()
    this.stopAgentGraphPolling()
    this.destroyAgentGraph3d()
    if (this.hashChangeHandler) {
      window.removeEventListener('hashchange', this.hashChangeHandler)
      this.hashChangeHandler = null
    }
  },
  methods: Object.assign(
    {},
    AgentsAppMethodsCore,
    AgentsAppMethodsRemoteAgent,
    AgentsAppMethodsAgentGroup,
    AgentsAppMethodsTask,
    AgentsAppMethodsGroupStartEditor,
    AgentsAppMethodsManage,
    AgentsAppMethodsPluginMcp,
  )
});
