/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

var app = new Vue({
  mixins: [AgentsStudioMixin],
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
    //Legacy comment translated to English.
    this.getPluginTypes();
    this.getKnowledgeBaseOptions();
  },
  mounted() {
    this.chatTaskPreviewMode = new URLSearchParams(window.location.search || '').get('preview') === '1';
    document.body.classList.toggle('agents-manager-chat-preview', this.chatTaskPreviewMode);
    this.tabsActiveName = "first";
    this.agentForm.systemMessageType = "2";
    this.getPluginTypes();

    // agent
    if (this.tabsActiveName === 'first') {
      this.getAgentListData('agent')
    }
    // group
    if (this.tabsActiveName === 'second') {
      this.getGroupListData('group')
    }
    // task
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
    document.body.classList.remove('agents-manager-chat-preview');
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
