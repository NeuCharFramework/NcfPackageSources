var ncfI18n = window.ncfI18n || {};

var app = new Vue({
  el: "#app",
  data() {
    return {
      newTableData: [], // New module data
      oldTableData: [], // Installed modules
      updatedTableData: [], // Modules pending update
      isExtend: false, // Whether to toggle state
      handlerText: "",
      handlerTips: "",
      newData: {},
      oldData: {
        state: {
          0: ncfI18n.moduleStateClosed || 'Closed',
          1: ncfI18n.moduleStateOpen || 'Open',
          2: ncfI18n.moduleStateNewPending || 'New - Pending Review',
          3: ncfI18n.moduleStateUpdatePending || 'Update - Pending Review'
        }
      },
      newTableSearch: '',
      oldTableSearch: ''
    };
  },
  watch: {
    'isExtend': {
      handler: function (val, oldVal) {
        this.handlerText = val
          ? (ncfI18n.enableExtModuleMode || 'Enable [Extension Module] management mode')
          : (ncfI18n.switchToPublishMode || 'Switch to publish mode, hide [Extension Module] management unit');
        this.handlerTips = val
          ? (ncfI18n.enableExtModuleConfirm || 'After enabling [Extension Module] management, all extension modules will be displayed under the [Extension Module] submenu. Are you sure?')
          : (ncfI18n.hideExtModuleConfirm || 'After hiding [Extension Module] management, all extension modules will be shown as top-level menu items. To re-enable, visit this page [/Admin/XncfModule] directly. Are you sure?');
      },
      immediate: true
    }
  },
  created: function () {
    this.getList();
  },
  methods: {
    // Get data
    async getList() {
      const oldTableData = await service.get('/Admin/XncfModule/Index?handler=Mofules');
      this.oldTableData = oldTableData.data.data.result;
      // Whether to toggle state
      this.isExtend = oldTableData.data.data.hideModuleManager;
      const newTableData = await service.get('/Admin/XncfModule/Index?handler=UnMofules');
      this.newTableData = newTableData.data.data;

      const updatedTableData = await service.get('/Admin/XncfModule/Index?handler=UpdatedMofules');
      this.updatedTableData = updatedTableData.data.data;
    },
    // Toggle state
    async handleSwitch() {
      await service.post('/Admin/XncfModule/Index?handler=HideManager');
      this.isExtend = !this.isExtend;
      window.location.href = "/Admin/Index";
    },
    // Install
    async handleInstall(index, row) {
      await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${row.uid}`);
      window.sessionStorage.setItem('setNavMenuActive', row.menuName);
      getNavMenu();
      // Navigate to module detail
      setTimeout(function () {
        window.location.href = `/Admin/XncfModule/Start/?uid=${row.uid}`;
      }, 100);
    },
    // Manage
    handleHandle(index, row) {
      window.location.href = "/Admin/XncfModule/Start/?uid=" + row.xncfRegister.uid;
    },
    // Homepage
    handleIndex(index, row) {
      window.location.href = row.xncfRegister.homeUrl;
    }
  }
});
