function openAdminPage(url) {
  console.debug('[NCF][AdminTabs] 首页打开', url);
  if (window.ncfOpenAdminPage && window.ncfOpenAdminPage(url)) {
    return;
  }
  window.location.href = url;
}

var app = new Vue({
  el: '#app',
  data() {
    return {
      isExpandAll: true,
      loading: false,
      refreshTable: true,
      xncfStat: {},
      xncfOpeningList: [],
      chartData: [],
      todayLogData: [],
      hostMetrics: {
        warnings: []
      },
      hostMetricsLoading: false,
      hostMetricsError: '',
      hostMetricsTimer: null,
      resourceMonitorError: '',
      resourceMonitorLoading: false,
      resourceMonitorTimer: null,
      resourceMonitorChart: null,
      resourceMonitorRange: 30 * 60 * 1000,
      resourceMonitorZoomed: false,
      resourceMonitorLive: true,
      resourceMonitorStart: 0,
      resourceMonitorEnd: 0,
      agentsOverview: {
        available: false
      },
      agentsOverviewLoading: false,
      agentsOverviewUnavailable: false,
      agentsOverviewTimer: null,
      agentsOverviewUpdatedAt: null,
      //Legacy comment translated to English.
      pivotBoard: null,
      //Legacy comment translated to English.
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    this.fetchTodayLogData();
    this.fetchHostMetrics();
    this.startHostMetricsPolling();
    this.fetchResourceMonitor();
    this.startResourceMonitorPolling();
    this.fetchAgentsOverview();
    this.startAgentsOverviewPolling();
    this.loadPivotBoard();
    //Legacy comment translated to English.
    this.initializeHoverEffects();
  },
  beforeDestroy() {
    if (this.hostMetricsTimer) {
      window.clearInterval(this.hostMetricsTimer);
      this.hostMetricsTimer = null;
    }
    if (this.resourceMonitorTimer) {
      window.clearInterval(this.resourceMonitorTimer);
      this.resourceMonitorTimer = null;
    }
    if (this.resourceMonitorChart) {
      this.resourceMonitorChart.dispose();
      this.resourceMonitorChart = null;
    }
    if (this.agentsOverviewTimer) {
      window.clearInterval(this.agentsOverviewTimer);
      this.agentsOverviewTimer = null;
    }
  },
  methods: {
    startAgentsOverviewPolling() {
      if (this.agentsOverviewTimer) {
        window.clearInterval(this.agentsOverviewTimer);
      }
      this.agentsOverviewTimer = window.setInterval(() => this.fetchAgentsOverview(), 5000);
    },
    async fetchAgentsOverview() {
      if (this.agentsOverviewLoading || this.agentsOverviewUnavailable || document.hidden) {
        return;
      }

      this.agentsOverviewLoading = true;
      try {
        const headers = { 'x-requested-with': 'XMLHttpRequest' };
        if (window.ncfJwtToken) {
          headers.Authorization = 'Bearer ' + window.ncfJwtToken;
        }
        const response = await axios.get(
          '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetDashboardOverview',
          { headers: headers });
        const payload = response && response.data;
        if (!payload || !payload.success || !payload.data) {
          throw new Error('AgentsManager overview is unavailable.');
        }

        this.agentsOverview = Object.assign({ available: true }, payload.data);
        this.agentsOverviewUpdatedAt = new Date();
      } catch (_) {
        //Legacy comment translated to English.
        this.agentsOverviewUnavailable = true;
        this.agentsOverview = { available: false };
      } finally {
        this.agentsOverviewLoading = false;
      }
    },
    disabledCount(total, enabled) {
      return Math.max(0, (Number(total) || 0) - (Number(enabled) || 0));
    },
    agentsTotal() {
      return (Number(this.agentsOverview.localAgentCount) || 0) + (Number(this.agentsOverview.remoteA2AAgentCount) || 0);
    },
    agentsEnabledTotal() {
      return (Number(this.agentsOverview.localAgentEnabledCount) || 0) + (Number(this.agentsOverview.remoteA2AAgentEnabledCount) || 0);
    },
    agentsDisabledTotal() {
      return this.disabledCount(this.agentsTotal(), this.agentsEnabledTotal());
    },
    agentsActiveTotal() {
      return (Number(this.agentsOverview.activeLocalAgentCount) || 0) + (Number(this.agentsOverview.activeRemoteA2AAgentCount) || 0);
    },
    agentsChattingTotal() {
      return (Number(this.agentsOverview.chattingLocalAgentCount) || 0) + (Number(this.agentsOverview.chattingRemoteA2AAgentCount) || 0);
    },
    agentsOverviewUpdatedText() {
      if (!this.agentsOverviewUpdatedAt) {
        return '--';
      }
      return this.agentsOverviewUpdatedAt.toLocaleTimeString();
    },
    navigateToAgentsManager() {
      openAdminPage('/Admin/XncfModule/Start/?uid=D858D7FA-775A-4690-9023-CFB0B3B84994');
    },
    //Legacy comment translated to English.
    //Legacy comment translated to English.
    async loadPivotBoard() {
      try {
        const headers = { 'x-requested-with': 'XMLHttpRequest' };
        if (window.ncfJwtToken) {
          headers.Authorization = 'Bearer ' + window.ncfJwtToken;
        }
        const response = await axios.get(
          '/Admin/NeuCharPivot/Board?handler=ByPageKey&pageKey=admin-home',
          { headers: headers });
        const payload = response && response.data;
        if (!payload || !payload.success || !payload.data) {
          throw new Error('Pivot board is unavailable.');
        }
        const board = payload.data;
        this.pivotBoard = board && Array.isArray(board.blocks) && board.blocks.length ? board : null;
      } catch (_) {
        this.pivotBoard = null;
      }
    },
    pivotBlockModuleLabel(moduleUid) {
      const openingModules = Array.isArray(this.xncfOpeningList) ? this.xncfOpeningList : [];
      const item = openingModules.find(z => z.uid === moduleUid);
      return item ? item.menuName : moduleUid;
    },
    pivotBlockIsMissing(block) {
      return block && block.available === false;
    },
    pivotBlockMissingDescription(block) {
      return `该 Pivot Block 引用的 Function 尚未在数据库中初始化。模块 UID：${block.moduleUid || '-'}；Function Code：${block.functionKey || '-'}。请先在模块的 NeuCharPivot 设置中生成或恢复对应 Function，再回到面板设置重新选择。`;
    },
    pivotBlockSettingsUrl(block) {
      const boardId = this.pivotBoard && this.pivotBoard.id ? this.pivotBoard.id : '';
      return '/Admin/NeuCharPivot/Board?boardId=' + encodeURIComponent(boardId) +
        '&pageKey=admin-home&moduleUid=' + encodeURIComponent(block.moduleUid || '') +
        '&functionKey=' + encodeURIComponent(block.functionKey || '');
    },
    pivotBlockModuleSettingsUrl(block) {
      return '/Admin/XncfModule/Start/?uid=' + encodeURIComponent(block.moduleUid || '') +
        '&functionKey=' + encodeURIComponent(block.functionKey || '') +
        '&action=settings&tab=pivot';
    },
    openPivotBlock(block) {
      if (this.pivotBlockIsMissing(block)) {
        return;
      }
      if (window.NeuCharPivotFunction && typeof window.NeuCharPivotFunction.open === 'function') {
        window.NeuCharPivotFunction.open({
          moduleUid: block.moduleUid,
          functionKey: block.functionKey,
          title: block.title || block.functionName || block.functionKey
        });
      }
    },
    startHostMetricsPolling() {
      if (this.hostMetricsTimer) {
        window.clearInterval(this.hostMetricsTimer);
      }
      this.hostMetricsTimer = window.setInterval(() => this.fetchHostMetrics(), 2000);
    },
    async fetchHostMetrics() {
      if (this.hostMetricsLoading || document.hidden) {
        return;
      }

      this.hostMetricsLoading = true;
      let sample = null;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetHostMetrics');
        const metrics = response && response.data && response.data.data;
        const sampledAt = metrics && Date.parse(metrics.sampledAt);
        if (!metrics || !Number.isFinite(sampledAt)) {
          throw new Error(ncfT('Admin.Home.HostMetricsInvalidResponse'));
        }

        this.hostMetrics = metrics;
        this.hostMetricsError = '';
        sample = {
          t: sampledAt,
          cpu: this.toNullableNumber(metrics.cpuUsagePercent),
          pcpu: this.toNullableNumber(metrics.processCpuUsagePercent),
          mem: this.toNullableNumber(metrics.memoryUsagePercent),
          pmem: this.computeProcessMemoryPercent(metrics),
          rx: this.bytesPerSecondToMbps(metrics.networkReceiveBytesPerSecond),
          tx: this.bytesPerSecondToMbps(metrics.networkSendBytesPerSecond)
        };
      } catch (error) {
        this.hostMetricsError = error && error.message
          ? error.message
          : ncfT('Admin.Home.HostMetricsUnavailable');
        console.warn('Host metrics refresh failed:', error);
      } finally {
        this.hostMetricsLoading = false;
      }
      if (sample) {
        try {
          this.ensureResourceMonitorChart().addLiveSample(sample);
        } catch (error) {
          this.reportResourceMonitorError(error);
        }
      }
    },
    //Legacy comment translated to English.
    computeProcessMemoryPercent(metrics) {
      const processBytes = this.toNullableNumber(metrics.processWorkingSetBytes);
      const totalBytes = this.toNullableNumber(metrics.memoryTotalBytes);
      if (processBytes === null || totalBytes === null || totalBytes <= 0) {
        return null;
      }
      return this.normalizePercent(processBytes / totalBytes * 100);
    },
    startResourceMonitorPolling() {
      if (this.resourceMonitorTimer) {
        window.clearInterval(this.resourceMonitorTimer);
      }
      //Legacy comment translated to English.
      this.resourceMonitorTimer = window.setInterval(() => this.fetchResourceMonitor(), 10000);
    },
    ensureResourceMonitorChart() {
      if (!this.resourceMonitorChart) {
        const chartElement = document.getElementById('resourceMonitorChart');
        if (!chartElement || !window.ResourceMonitorChart) {
          throw new Error(ncfT('Admin.Home.ResourceMonitorUnavailable'));
        }
        this.resourceMonitorChart = window.ResourceMonitorChart.create(chartElement, {
          rangeMs: this.resourceMonitorRange,
          i18n: {
            empty: ncfT('Admin.Home.ResourceMonitorEmpty'),
            cpuHost: ncfT('Admin.Home.HostCpu'),
            cpuWeb: ncfT('Admin.Home.HostProcessCpu'),
            memHost: ncfT('Admin.Home.HostMemory'),
            memWeb: ncfT('Admin.Home.WebProcessMemory'),
            rx: ncfT('Admin.Home.HostNetworkReceive'),
            tx: ncfT('Admin.Home.HostNetworkSend')
          },
          onViewChange: (view) => {
            this.resourceMonitorStart = view.start;
            this.resourceMonitorEnd = view.end;
            this.resourceMonitorLive = view.live;
            this.resourceMonitorZoomed = view.zoomed;
          }
        });
      }
      return this.resourceMonitorChart;
    },
    reportResourceMonitorError(error) {
      this.resourceMonitorError = (error && error.message)
        ? error.message
        : ncfT('Admin.Home.ResourceMonitorUnavailable');
      console.warn('Resource monitor refresh failed:', error);
    },
    async fetchResourceMonitor() {
      if (this.resourceMonitorLoading || document.hidden) {
        return;
      }
      this.resourceMonitorLoading = true;
      try {
        const response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetResourceMonitor');
        const data = response && response.data && response.data.data;
        if (!data || !Number.isFinite(data.now) || !Array.isArray(data.windows) ||
          !data.windows.every(window => window && Number.isFinite(window.resolutionSeconds) &&
            window.resolutionSeconds > 0 && Array.isArray(window.points))) {
          throw new Error(ncfT('Admin.Home.ResourceMonitorUnavailable'));
        }
        this.ensureResourceMonitorChart().update(data);
        this.resourceMonitorError = '';
      } catch (error) {
        this.reportResourceMonitorError(error);
      } finally {
        this.resourceMonitorLoading = false;
      }
    },
    resetResourceMonitorZoom() {
      if (this.resourceMonitorChart) {
        this.resourceMonitorChart.reset();
      }
    },
    selectResourceMonitorRange(range) {
      if (this.resourceMonitorChart) {
        this.resourceMonitorChart.setRange(range);
      }
    },
    returnResourceMonitorToLive() {
      if (this.resourceMonitorChart) {
        this.resourceMonitorChart.returnToLive();
      }
    },
    formatResourceMonitorTime(value) {
      return value ? new Date(value).toLocaleString() : '--';
    },
    toNullableNumber(value) {
      if (value === null || value === undefined || value === '') {
        return null;
      }
      const number = Number(value);
      return Number.isFinite(number) ? number : null;
    },
    normalizePercent(value) {
      const number = this.toNullableNumber(value);
      return number === null ? 0 : Math.max(0, Math.min(100, number));
    },
    barWidth(value) {
      return this.normalizePercent(value).toFixed(3) + '%';
    },
    boundedSubsetPercent(value, maximum) {
      const subset = this.normalizePercent(value);
      const maximumNumber = this.toNullableNumber(maximum);
      if (maximumNumber === null || maximumNumber <= 0) {
        return subset;
      }
      return Math.min(subset, this.normalizePercent(maximumNumber));
    },
    boundedSubsetWidth(value, maximum) {
      return this.boundedSubsetPercent(value, maximum).toFixed(3) + '%';
    },
    processMemoryPercent() {
      const processBytes = this.toNullableNumber(this.hostMetrics.processWorkingSetBytes);
      const totalBytes = this.toNullableNumber(this.hostMetrics.memoryTotalBytes);
      if (processBytes === null || totalBytes === null || totalBytes <= 0) {
        return 0;
      }
      return this.normalizePercent(processBytes / totalBytes * 100);
    },
    formatPercent(value) {
      const number = this.toNullableNumber(value);
      return number === null ? '--' : number.toFixed(1) + '%';
    },
    formatBytes(value) {
      const number = this.toNullableNumber(value);
      if (number === null) {
        return '--';
      }
      if (number <= 0) {
        return '0 B';
      }
      const units = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];
      const unitIndex = Math.min(Math.floor(Math.log(number) / Math.log(1024)), units.length - 1);
      const scaled = number / Math.pow(1024, unitIndex);
      const digits = scaled >= 100 ? 0 : (scaled >= 10 ? 1 : 2);
      return scaled.toFixed(digits) + ' ' + units[unitIndex];
    },
    formatRate(value) {
      const number = this.toNullableNumber(value);
      return number === null ? '--' : this.formatBytes(number) + '/s';
    },
    bytesPerSecondToMbps(value) {
      const number = this.toNullableNumber(value);
      return number === null ? null : Number((number * 8 / 1000000).toFixed(3));
    },
    formatDuration(value) {
      const totalSeconds = Math.max(0, Math.floor(Number(value) || 0));
      const days = Math.floor(totalSeconds / 86400);
      const hours = Math.floor((totalSeconds % 86400) / 3600);
      const minutes = Math.floor((totalSeconds % 3600) / 60);
      return (days > 0 ? days + 'd ' : '') +
        (hours > 0 || days > 0 ? hours + 'h ' : '') +
        minutes + 'm';
    },
    formatSampleTime(value) {
      const date = new Date(value);
      return Number.isNaN(date.getTime()) ? '--' : date.toLocaleTimeString();
    },
    async fetchChartData() {
      try {
        let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetLogs');
        if (response.data && response.data.data && response.data.data.logs) {
          this.chartData = response.data.data.logs;
          this.initChart();
        } else {
          console.error('Invalid API response:', response);
        }
      } catch (error) {
        console.error('Error fetching chart data:', error);
      }
    },
    async fetchTodayLogData() { //Legacy comment translated to English.
      try {
        let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetTodayLog');
        if (response.data && response.data.data && response.data.data.items) {
          this.todayLogData = response.data.data.items;
          this.todayDate = response.data.data.date;
          this.initChart(); //Legacy comment translated to English.
        } else {
          console.error('Invalid API response:', response);
        }
      } catch (error) {
        console.error('Error fetching today log data:', error);
      }
    },
    initChart() {
      let chart1 = document.getElementById('firstChart');
      let chartOption1 = {
        title: {
          text: ncfT('Admin.Home.LogStats'),
          subtext: ncfT('Admin.Home.Last14Days')
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date)
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} ' + ncfT('Admin.Home.LogCountUnit')
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow'
          }
        },
          legend: {
          data: [ncfT('Admin.Home.NormalLogs'), ncfT('Admin.Home.ErrorLogs')]
        },
        series: [
          {
            name: ncfT('Admin.Home.NormalLogs'),
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
          {
            name: ncfT('Admin.Home.ErrorLogs'),
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#d48265' }, //Legacy comment translated to English.
            data: this.chartData.map(item => item.exceptionLogCount),
            color: '#d48265'
          }
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      //Legacy comment translated to English.
      chartInstance1.on('click', params => {
        if (params.componentType === 'series') {
          let date = params.name;
          openAdminPage(`/Admin/SenparcTrace/DateLog?date=${date}`);
        }
      });

      //Legacy comment translated to English.
      let todayLogData = this.todayLogData.map(item => ({
        name: item.senparcTraceType,
        value: item.count
      }));

      let chart2 = document.getElementById('secondChart');
      let chartOption2 = {
        title: {
          text: ncfT('Admin.Home.TodayLogStats'),
          subtext: ncfT('Admin.Home.DynamicData'),
          left: 'center'
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b}: {c} ({d}%)'
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          data: this.todayLogData.map(item => item.senparcTraceType) //Legacy comment translated to English.
        },
        series: [{
          name: ncfT('Admin.Home.LogType'),
          type: 'pie',
          radius: '50%',
          data: todayLogData,
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: 'rgba(0, 0, 0, 0.5)'
            }
          }
        }]
      };
      let chartInstance2 = echarts.init(chart2);
      chartInstance2.setOption(chartOption2);
      //Legacy comment translated to English.
      chartInstance2.on('click', params => {
        if (params.componentType === 'series') {
          openAdminPage(`/Admin/SenparcTrace/DateLog?date=${this.todayDate}`);
        }
      });

    },
    //XNCF statisticsstatus  
    async getXncfStat() {
      let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
      this.xncfStat = xncfStatData.data.data;
    },
    //Legacy comment translated to English.
    async getXncfOpening() {
      let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
      const data = xncfOpeningList && xncfOpeningList.data && xncfOpeningList.data.data;
      this.xncfOpeningList = Array.isArray(data) ? data : [];
    },
    //Legacy comment translated to English.
    navigateTo(uid) {
      openAdminPage('/Admin/XncfModule/Start/?uid=' + uid);
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      openAdminPage(menuInfo.url)
    },
    //Legacy comment translated to English.
    initializeHoverEffects() {
      //Legacy comment translated to English.
      const installedModulesStat = document.querySelector('.xncf-stat-item');
      const updateModulesStat = document.querySelectorAll('.xncf-stat-item')[1];

      //Legacy comment translated to English.
      if (installedModulesStat) {
        installedModulesStat.addEventListener('mouseenter', () => {
          this.triggerShakeAnimation();
        });
      }

      //Legacy comment translated to English.
      if (updateModulesStat) {
        updateModulesStat.addEventListener('mouseenter', () => {
          this.triggerGlowAnimation();
        });
      }
    },

    //Legacy comment translated to English.
    triggerShakeAnimation() {
      const moduleCards = document.querySelectorAll('#xncf-modules-area .box-card');
      moduleCards.forEach(card => {
        //Legacy comment translated to English.
        const delay = Math.random() * 200; //Legacy comment translated to English.
        setTimeout(() => {
          card.classList.add('shake-animation');
          //Legacy comment translated to English.
          setTimeout(() => {
            card.classList.remove('shake-animation');
          }, 800); //Legacy comment translated to English.
        }, delay);
      });
    },

    //Legacy comment translated to English.
    triggerGlowAnimation() {
      const allCards = document.querySelectorAll('#xncf-modules-area .box-card');
      const upgradeableVersions = document.querySelectorAll('#xncf-modules-area .version-upgradeable');

      //Legacy comment translated to English.
      upgradeableVersions.forEach(version => {
        const card = version.closest('.box-card');
        if (card) {
          //Legacy comment translated to English.
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('glow-animation');
            setTimeout(() => {
              card.classList.remove('glow-animation');
            }, 1200); //Legacy comment translated to English.
          }, delay);
        }
      });

      //Legacy comment translated to English.
      allCards.forEach(card => {
        if (!card.querySelector('.version-upgradeable')) {
          //Legacy comment translated to English.
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('fade-animation');
            setTimeout(() => {
              card.classList.remove('fade-animation');
            }, 1200); //Legacy comment translated to English.
          }, delay);
        }
      });
    }
  }
});  
