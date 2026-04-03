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
      // Add animation control variables
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    this.fetchTodayLogData();
    // Add mouse event listener
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
    async fetchTodayLogData() { // Added method to obtain today's log data  
      try {
        let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetTodayLog');
        if (response.data && response.data.data && response.data.data.items) {
          this.todayLogData = response.data.data.items;
          this.todayDate = response.data.data.date;
          this.initChart(); // Make sure the chart updates after getting the data  
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
          text: '日志统计',
          subtext: '近 14 天'
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date)
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} 条'
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow'
          }
        },
        legend: {
          data: ['常规日志', '异常日志']
        },
        series: [
          {
            name: '常规日志',
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#91c7ae' }, // Add area fill color  
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
          {
            name: '异常日志',
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#d48265' }, // Add area fill color  
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

      // Prepare today’s log data  
      let todayLogData = this.todayLogData.map(item => ({
        name: item.senparcTraceType,
        value: item.count
      }));

      let chart2 = document.getElementById('secondChart');
      let chartOption2 = {
        title: {
          text: '今日日志统计',
          subtext: '动态数据',
          left: 'center'
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b}: {c} ({d}%)'
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          data: this.todayLogData.map(item => item.senparcTraceType) // Automatically output all categories  
        },
        series: [{
          name: '日志类型',
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
    //XNCF statistics status  
    async getXncfStat() {
      let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
      this.xncfStat = xncfStatData.data.data;
    },
    //Open module data
    async getXncfOpening() {
      let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
      this.xncfOpeningList = xncfOpeningList.data.data;
    },
    //Click to open module
    navigateTo(uid) {
      window.location.href = '/Admin/XncfModule/Start/?uid=' + uid;
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      window.location.href = menuInfo.url
    },
    // Add new method to handle hover effects
    initializeHoverEffects() {
      // Get statistic element - fix selector
      const installedModulesStat = document.querySelector('.xncf-stat-item');
      const updateModulesStat = document.querySelectorAll('.xncf-stat-item')[1];

      // Mouse events for installed module statistics
      if (installedModulesStat) {
        installedModulesStat.addEventListener('mouseenter', () => {
          this.triggerShakeAnimation();
        });
      }

      // Mouse events for module statistical items to be updated
      if (updateModulesStat) {
        updateModulesStat.addEventListener('mouseenter', () => {
          this.triggerGlowAnimation();
        });
      }
    },

    // Trigger jitter animation
    triggerShakeAnimation() {
      const moduleCards = document.querySelectorAll('#xncf-modules-area .box-card');
      moduleCards.forEach(card => {
        // add random delay
        const delay = Math.random() * 200; // 0-200ms random delay
        setTimeout(() => {
          card.classList.add('shake-animation');
          // Remove class after animation ends
          setTimeout(() => {
            card.classList.remove('shake-animation');
          }, 800); // Match animation duration
        }, delay);
      });
    },

    // Trigger glow/fade animation
    triggerGlowAnimation() {
      const allCards = document.querySelectorAll('#xncf-modules-area .box-card');
      const upgradeableVersions = document.querySelectorAll('#xncf-modules-area .version-upgradeable');

      // Added glow effect to all updatable modules
      upgradeableVersions.forEach(version => {
        const card = version.closest('.box-card');
        if (card) {
          // add random delay
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('glow-animation');
            setTimeout(() => {
              card.classList.remove('glow-animation');
            }, 1200); // Match animation duration
          }, delay);
        }
      });

      // Add a fade effect to non-updatable modules
      allCards.forEach(card => {
        if (!card.querySelector('.version-upgradeable')) {
          // add random delay
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('fade-animation');
            setTimeout(() => {
              card.classList.remove('fade-animation');
            }, 1200); // Match animation duration
          }, delay);
        }
      });
    }
  }
});  
