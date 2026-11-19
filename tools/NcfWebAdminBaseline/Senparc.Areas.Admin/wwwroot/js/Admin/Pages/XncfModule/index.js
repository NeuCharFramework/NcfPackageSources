var app = new Vue({
  el: "#app",
  data() {
    return {
      newTableData: [], //Legacy comment translated to English.
      oldTableData: [], //Legacy comment translated to English.
      updatedTableData: [], //Legacy comment translated to English.
      activeTab: 'new',
      selectedUpdatedModules: [],
      isExtend: false, //Legacy comment translated to English.
      handlerText: "",
      handlerTips: "",
      newData: {},
      oldData: {
        state: {
          0: ncfT('Xncf.Close'),
          1: ncfT('Xncf.Open'),
          2: ncfT('Xncf.NewModules'),
          3: ncfT('Xncf.PendingUpdates')
        }
      },
      newTableSearch: '',
      oldTableSearch: '',
      updatedTableSearch: '',
      batchUpdate: {
        loading: false,
        resultVisible: false,
        result: null
      }
    };
  },
  computed: {
    filteredUpdatedTableData() {
      const keyword = this.updatedTableSearch.trim().toLocaleLowerCase();
      if (!keyword) {
        return this.updatedTableData;
      }
      return this.updatedTableData.filter(item =>
        [item.name, item.menuName, item.uid, item.version]
          .some(value => String(value || '').toLocaleLowerCase().includes(keyword)));
    }
  },
  watch: {
    'isExtend': {
      handler: function (val, oldVal) {
        this.handlerText = val ? ncfT('Xncf.EnableManager') : ncfT('Xncf.HideManager');
        this.handlerTips = val ? ncfT('Xncf.EnableManagerConfirm') : ncfT('Xncf.HideManagerConfirm');
      },
      immediate: true
    }
  },
  created: function () {
    this.activeTab = this.getRequestedTab();
    this.getList();
  },
  methods: {
    // get
    async getList() {
      const [oldTableData, newTableData, updatedTableData] = await Promise.all([
        service.get('/Admin/XncfModule/Index?handler=Mofules'),
        service.get('/Admin/XncfModule/Index?handler=UnMofules'),
        service.get('/Admin/XncfModule/Index?handler=UpdatedMofules')
      ]);
      this.oldTableData = oldTableData.data.data.result;
      //Legacy comment translated to English.
      this.isExtend = oldTableData.data.data.hideModuleManager;
      this.newTableData = newTableData.data.data;
      this.updatedTableData = updatedTableData.data.data;
      this.selectedUpdatedModules = [];
      if (this.$refs.updatedModulesTable) {
        this.$refs.updatedModulesTable.clearSelection();
      }
    },
    getRequestedTab() {
      const requestedTab = new URLSearchParams(window.location.search).get('tab');
      return ['new', 'installed', 'updates'].includes(requestedTab) ? requestedTab : 'new';
    },
    handleTabClick(tab) {
      const tabName = tab && tab.name ? tab.name : this.activeTab;
      const url = new URL(window.location.href);
      url.searchParams.set('tab', tabName);
      window.history.replaceState(null, '', url.pathname + url.search + url.hash);
    },
    //Legacy comment translated to English.
    async handleSwitch() {
      await service.post('/Admin/XncfModule/Index?handler=HideManager');
      this.isExtend = !this.isExtend;
      window.location.href = "/Admin/Index";
    },
    //Legacy comment translated to English.
    async handleInstall(index, row) {
      await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${row.uid}`);
      window.sessionStorage.setItem('setNavMenuActive', row.menuName);
      getNavMenu();
      //Legacy comment translated to English.
      setTimeout(function () {
        window.location.href = `/Admin/XncfModule/Start/?uid=${row.uid}`;
      }, 100);
    },
    handleUpdatedSelectionChange(selection) {
      this.selectedUpdatedModules = selection;
    },
    async handleSingleUpdate(row) {
      this.selectedUpdatedModules = [row];
      if (this.$refs.updatedModulesTable) {
        this.$refs.updatedModulesTable.clearSelection();
        this.$refs.updatedModulesTable.toggleRowSelection(row, true);
      }
      await this.confirmBatchUpdate();
    },
    async confirmBatchUpdate() {
      if (this.selectedUpdatedModules.length === 0) {
        this.$message.warning(ncfT('Xncf.BatchUpdate.SelectAtLeastOne'));
        return;
      }

      try {
        await this.$confirm(
          ncfT('Xncf.BatchUpdate.Confirm', this.selectedUpdatedModules.length),
          ncfT('Xncf.BatchUpdate.Title'),
          {
            confirmButtonText: ncfT('Common.确认'),
            cancelButtonText: ncfT('Common.取消'),
            type: 'warning'
          });
      } catch (error) {
        return;
      }

      await this.handleBatchUpdateAndEnable();
    },
    async handleBatchUpdateAndEnable() {
      const uids = this.selectedUpdatedModules.map(item => item.uid);
      this.batchUpdate.loading = true;
      try {
        const response = await service.post(
          '/Admin/XncfModule/Index?handler=BatchUpdateAndEnable',
          { uids: uids },
          { customAlert: true }
        );
        const result = response && response.data ? response.data.data : null;
        if (!result || !Array.isArray(result.items)) {
          throw new Error(ncfT('Xncf.BatchUpdate.NoResult'));
        }

        this.batchUpdate.result = result;
        this.batchUpdate.resultVisible = true;
        try {
          await this.getList();
          getNavMenu();
        } catch (refreshError) {
          console.error(ncfT('Xncf.BatchUpdate.RefreshFailed'), refreshError);
          this.$message.warning(ncfT('Xncf.BatchUpdate.RefreshFailed'));
        }
      } catch (error) {
        console.error(ncfT('Xncf.BatchUpdate.RequestFailed'), error);
        this.$message.error(error.message || ncfT('Xncf.BatchUpdate.RequestFailed'));
      } finally {
        this.batchUpdate.loading = false;
      }
    },
    //Legacy comment translated to English.
    handleHandle(index, row) {
      window.location.href = "/Admin/XncfModule/Start/?uid=" + row.xncfRegister.uid;
    },
    //Legacy comment translated to English.
    handleIndex(index, row) {
      window.location.href = row.xncfRegister.homeUrl;
    }
  }
});
