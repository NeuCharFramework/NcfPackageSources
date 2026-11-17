var app = new Vue({
  el: '#app',
  data() {
    return {
      isExpandAll: true,
      loading: false,
      refreshTable: true,
      xncfStat: {},
      xncfOpeningList: {},
      chartData: [],
      todayLogData: [],
      //Legacy comment translated to English.
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    //Legacy comment translated to English.
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
      initChart() {

      //----------------------------------bottomforchart1--------------------------------------
      let chart1 = document.getElementById('firstChart');
      let chartOption1 = {
        title: {
              text: ncfT('AIKernel.Dashboard.TotalSessions'),
          subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date)
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} ' + ncfT('AIKernel.Dashboard.TimesUnit')
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'shadow'
          }
        },
        
        series: [
          {
                name: ncfT('AIKernel.Dashboard.TotalSessions'),
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      


          //----------------------------------bottomforchart2--------------------------------------
          let chart2 = document.getElementById('secondChart');
          let chartOption2 = {
              title: {
                  text: ncfT('AIKernel.Dashboard.ActiveUsers'),
                  subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} ' + ncfT('AIKernel.Dashboard.PeopleUnit')
                  }
              },
              tooltip: {
                  trigger: 'axis',
                  axisPointer: {
                      type: 'shadow'
                  }
              },

              series: [
                  {
                      name: ncfT('AIKernel.Dashboard.ActiveUsers'),
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance2 = echarts.init(chart2);
          chartInstance2.setOption(chartOption2);

          


        //----------------------------------bottomforchart3--------------------------------------
        let chart3 = document.getElementById('Chart3');
        let chartOption3 = {
            title: {
                text: ncfT('AIKernel.Dashboard.AverageInteractions'),
                subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
            },
            xAxis: {
                type: 'category',
                data: this.chartData.map(item => item.date)
            },
            yAxis: {
                type: 'value',
                axisLabel: {
                    formatter: '{value} ' + ncfT('AIKernel.Dashboard.TimesUnit')
                }
            },
            tooltip: {
                trigger: 'axis',
                axisPointer: {
                    type: 'shadow'
                }
            },

            series: [
                {
                    name: ncfT('AIKernel.Dashboard.AverageInteractions'),
                    type: 'line',
                    stack: '总量',
                    areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
                    data: this.chartData.map(item => item.normalLogCount),
                    color: '#91c7ae'
                },
            ]
        };
        let chartInstance3 = echarts.init(chart3);
        chartInstance3.setOption(chartOption3);

        

          //----------------------------------bottomforchart4--------------------------------------
          let chart4 = document.getElementById('Chart4');
          let chartOption4 = {
              title: {
                  text: ncfT('AIKernel.Dashboard.TokenSpeedWithUnit'),
                  subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} '
                  }
              },
              tooltip: {
                  trigger: 'axis',
                  axisPointer: {
                      type: 'shadow'
                  }
              },

              series: [
                  {
                      name: ncfT('AIKernel.Dashboard.TokenSpeed'),
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance4 = echarts.init(chart4);
          chartInstance4.setOption(chartOption4);


          //----------------------------------bottomforchart5--------------------------------------
          let chart5 = document.getElementById('Chart5');
          let chartOption5 = {
              title: {
                  text: ncfT('AIKernel.Dashboard.UserSatisfaction'),
                  subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} ' + ncfT('AIKernel.Dashboard.TimesUnit')
                  }
              },
              tooltip: {
                  trigger: 'axis',
                  axisPointer: {
                      type: 'shadow'
                  }
              },

              series: [
                  {
                      name: ncfT('AIKernel.Dashboard.UserSatisfaction'),
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance5 = echarts.init(chart5);
          chartInstance5.setOption(chartOption5);

          

          //----------------------------------bottomforchart6--------------------------------------
          let chart6 = document.getElementById('Chart6');
          let chartOption6 = {
              title: {
                  text: ncfT('AIKernel.Dashboard.TotalMessages'),
                  subtext: ncfT('AIKernel.Dashboard.LastSevenDays')
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} ' + ncfT('AIKernel.Dashboard.TimesUnit')
                  }
              },
              tooltip: {
                  trigger: 'axis',
                  axisPointer: {
                      type: 'shadow'
                  }
              },

              series: [
                  {
                      name: ncfT('AIKernel.Dashboard.TotalMessages'),
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, //Legacy comment translated to English.
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance6 = echarts.init(chart6);
          chartInstance6.setOption(chartOption6);

          

     




    },
    //XNCF statisticsstatus  
    async getXncfStat() {
      let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
      this.xncfStat = xncfStatData.data.data;
    },
    //Legacy comment translated to English.
    async getXncfOpening() {
      let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
      this.xncfOpeningList = xncfOpeningList.data.data;
    },
    //Legacy comment translated to English.
    navigateTo(uid) {
      window.location.href = '/Admin/XncfModule/Start/?uid=' + uid;
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      window.location.href = menuInfo.url
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
