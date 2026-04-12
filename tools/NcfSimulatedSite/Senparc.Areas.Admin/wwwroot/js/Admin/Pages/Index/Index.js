var ncfI18n = window.ncfI18n || {};

var app = new Vue({
  el: '#app',
  mixins: [window.ChatLauncherMixin],
  data() {
    return {
      isExpandAll: true,
      loading: false,
      refreshTable: true,
      xncfStat: {},
      xncfOpeningList: {},
      chartData: [],
      todayLogData: [],
      // Animation control variables
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    this.fetchTodayLogData();
    // Add mouse event listeners
    this.initializeHoverEffects();
  },
  methods: {
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
    async fetchTodayLogData() {
      try {
        let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetTodayLog');
        if (response.data && response.data.data && response.data.data.items) {
          this.todayLogData = response.data.data.items;
          this.todayDate = response.data.data.date;
          this.initChart();
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
          text: ncfI18n.logStatistics || 'Log Statistics',
          subtext: ncfI18n.last14Days || 'Last 14 days',
          textStyle: { fontSize: 13 },
          left: 5
        },
        grid: {
          top: 65,
          left: 48,
          right: 12,
          bottom: 56,
          containLabel: true
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date),
          axisLabel: {
            fontSize: 10,
            interval: 0,
            rotate: 32,
            formatter: function (value) {
              if (value && String(value).length === 8 && /^\d{8}$/.test(String(value))) {
                var s = String(value);
                return s.slice(0, 4) + '-' + s.slice(4, 6) + '-' + s.slice(6, 8);
              }
              return value;
            }
          }
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            fontSize: 11
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow'
          }
        },
        legend: {
          data: [ncfI18n.normalLogs || 'Normal Logs', ncfI18n.exceptionLogs || 'Exception Logs'],
          top: 0,
          right: 0,
          textStyle: { fontSize: 11 }
        },
        series: [
          {
            name: ncfI18n.normalLogs || 'Normal Logs',
            type: 'line',
            stack: 'total',
            areaStyle: { color: '#91c7ae' },
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
          {
            name: ncfI18n.exceptionLogs || 'Exception Logs',
            type: 'line',
            stack: 'total',
            areaStyle: { color: '#d48265' },
            data: this.chartData.map(item => item.exceptionLogCount),
            color: '#d48265'
          }
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      // Add click event listener
      chartInstance1.on('click', params => {
        if (params.componentType === 'series') {
          let date = params.name;
          window.location.href = `/Admin/SenparcTrace/DateLog?date=${date}`;
        }
      });

      // Prepare today's log data
      let todayLogData = this.todayLogData.map(item => ({
        name: item.senparcTraceType,
        value: item.count
      }));

      let chart2 = document.getElementById('secondChart');
      let chartOption2 = {
        title: {
          text: ncfI18n.todayLogStatistics || 'Today Log Statistics',
          subtext: ncfI18n.dynamicData || 'Dynamic data',
          left: 'center',
          textStyle: { fontSize: 14 }
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b}: {c} ({d}%)'
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          data: this.todayLogData.map(item => item.senparcTraceType)
        },
        series: [{
          name: ncfI18n.logType || 'Log Type',
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
      // Add click event listener
      chartInstance2.on('click', params => {
        if (params.componentType === 'series') {
          window.location.href = `/Admin/SenparcTrace/DateLog?date=${this.todayDate}`;
        }
      });

    },
    // XNCF statistics
    async getXncfStat() {
      let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
      this.xncfStat = xncfStatData.data.data;
    },
    // Open module data
    async getXncfOpening() {
      let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
      this.xncfOpeningList = xncfOpeningList.data.data;
    },
    // Click to open module
    navigateTo(uid) {
      window.location.href = '/Admin/XncfModule/Start/?uid=' + uid;
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      window.location.href = menuInfo.url
    },
    // Handle hover effects
    initializeHoverEffects() {
      // Get stat item elements
      const installedModulesStat = document.querySelector('.xncf-stat-item');
      const updateModulesStat = document.querySelectorAll('.xncf-stat-item')[1];

      // Installed modules stat item mouse events
      if (installedModulesStat) {
        installedModulesStat.addEventListener('mouseenter', () => {
          this.triggerShakeAnimation();
        });
      }

      // Pending update modules stat item mouse events
      if (updateModulesStat) {
        updateModulesStat.addEventListener('mouseenter', () => {
          this.triggerGlowAnimation();
        });
      }
    },

    // Trigger shake animation
    triggerShakeAnimation() {
      const moduleCards = document.querySelectorAll('#xncf-modules-area .box-card');
      moduleCards.forEach(card => {
        // Add random delay
        const delay = Math.random() * 200;
        setTimeout(() => {
          card.classList.add('shake-animation');
          // Remove class after animation ends
          setTimeout(() => {
            card.classList.remove('shake-animation');
          }, 800);
        }, delay);
      });
    },

    // Trigger glow/fade animation
    triggerGlowAnimation() {
      const allCards = document.querySelectorAll('#xncf-modules-area .box-card');
      const upgradeableVersions = document.querySelectorAll('#xncf-modules-area .version-upgradeable');

      // Add glow effect to all upgradeable modules
      upgradeableVersions.forEach(version => {
        const card = version.closest('.box-card');
        if (card) {
          // Add random delay
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('glow-animation');
            setTimeout(() => {
              card.classList.remove('glow-animation');
            }, 1200);
          }, delay);
        }
      });

      // Add fade effect to non-upgradeable modules
      allCards.forEach(card => {
        if (!card.querySelector('.version-upgradeable')) {
          // Add random delay
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('fade-animation');
            setTimeout(() => {
              card.classList.remove('fade-animation');
            }, 1200);
          }, delay);
        }
      });
    }
  }
});
