var app = new Vue({
  el: "#app",
  data() {
    return {
      newTableData: [], // 新模块数据
      oldTableData: [], // 已安装模块
      updatedTableData: [], // 待更新模块
      isExtend: false, //是否切换状态
      handlerText: "",
      handlerTips: "",
      newData: {},
      oldData: {
        state: {
          0: ncfT('Xncf.State.Closed'),
          1: ncfT('Xncf.State.Open'),
          2: ncfT('Xncf.State.PendingAdd'),
          3: ncfT('Xncf.State.PendingUpdate')
        }
      },
      newTableSearch: '',
      oldTableSearch: '',
      batchUpdate: {
        visible: false,
        enableAfterUpdate: true,
        loading: false,
        resultVisible: false,
        result: null
      }
    };
  },
  watch: {
    'isExtend': {
      handler: function (val, oldVal) {
        this.handlerText = val ? ncfT('Xncf.EnableManagerMode') : ncfT('Xncf.EnablePublishMode');
        this.handlerTips = val ? ncfT('Xncf.EnableManagerModeConfirm') : ncfT('Xncf.EnablePublishModeConfirm');
      },
      immediate: true
    }
  },
  created: function () {
    this.getList();
  },
  methods: {
    // 获取
    async getList() {
      const oldTableData = await service.get('/Admin/XncfModule/Index?handler=Mofules');
      this.oldTableData = oldTableData.data.data.result;
      // 是否切换状态
      this.isExtend = oldTableData.data.data.hideModuleManager;
      const newTableData = await service.get('/Admin/XncfModule/Index?handler=UnMofules');
      this.newTableData = newTableData.data.data;

      const updatedTableData = await service.get('/Admin/XncfModule/Index?handler=UpdatedMofules');
      this.updatedTableData = updatedTableData.data.data;
    },
    // 切换状态
    async handleSwitch() {
      await service.post('/Admin/XncfModule/Index?handler=HideManager');
      this.isExtend = !this.isExtend;
      window.location.href = "/Admin/Index";
    },
    // 安装
    async handleInstall(index, row) {
      await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${row.uid}`);
      window.sessionStorage.setItem('setNavMenuActive', row.menuName);
      getNavMenu();
      // 跳转到模块详情
      setTimeout(function () {
        window.location.href = `/Admin/XncfModule/Start/?uid=${row.uid}`;
      }, 100);
    },
    // 打开批量更新选项
    openBatchUpdate() {
      if (this.updatedTableData.length === 0) {
        this.$message.info(ncfT('Xncf.NoPendingUpdates'));
        return;
      }
      this.batchUpdate.visible = true;
    },
    // 后台逐一更新全部待更新模块
    async handleBatchUpdate() {
      this.batchUpdate.loading = true;
      try {
        const response = await service.post(
          '/Admin/XncfModule/Index?handler=BatchUpdate',
          { enableAfterUpdate: this.batchUpdate.enableAfterUpdate },
          { customAlert: true }
        );
        const result = response && response.data ? response.data.data : null;
        if (!result) {
          throw new Error(ncfT('Xncf.BatchUpdate.NoResult'));
        }

        this.batchUpdate.visible = false;
        this.batchUpdate.result = result;
        this.batchUpdate.resultVisible = true;
        await this.getList();
        getNavMenu();
      } catch (error) {
        console.error(ncfT('Xncf.BatchUpdateFailed'), error);
        this.$message.error(error.message || ncfT('Xncf.BatchUpdateFailed'));
      } finally {
        this.batchUpdate.loading = false;
      }
    },
    // 操作
    handleHandle(index, row) {
      window.location.href = "/Admin/XncfModule/Start/?uid=" + row.xncfRegister.uid;
    },
    // 主页
    handleIndex(index, row) {
      window.location.href = row.xncfRegister.homeUrl;
    }
  }
});
