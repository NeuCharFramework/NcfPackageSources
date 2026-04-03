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
      // Add animation control variables
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
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
      initChart() {

      //----------------------------------The following is chart1-------------------------------------------------
      let chart1 = document.getElementById('firstChart');
      let chartOption1 = {
        title: {
              text: '全部会话数',
          subtext: '近 7 天'
        },
        xAxis: {
          type: 'category',
          data: this.chartData.map(item => item.date)
        },
        yAxis: {
          type: 'value',
          axisLabel: {
            formatter: '{value} 次'
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
                name: '全部会话数',
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#91c7ae' }, // Add area fill color  
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      


          //----------------------------------The following is chart2--------------------------------------------------
          let chart2 = document.getElementById('secondChart');
          let chartOption2 = {
              title: {
                  text: '活跃用户数',
                  subtext: '近 7 天'
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} 人'
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
                      name: '活跃用户数',
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, // Add area fill color  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance2 = echarts.init(chart2);
          chartInstance2.setOption(chartOption2);

          


        //----------------------------------The following is chart3--------------------------------------------------
        let chart3 = document.getElementById('Chart3');
        let chartOption3 = {
            title: {
                text: '平均会话互动数',
                subtext: '近 7 天'
            },
            xAxis: {
                type: 'category',
                data: this.chartData.map(item => item.date)
            },
            yAxis: {
                type: 'value',
                axisLabel: {
                    formatter: '{value} 次'
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
                    name: '平均会话互动数',
                    type: 'line',
                    stack: '总量',
                    areaStyle: { color: '#91c7ae' }, // Add area fill color  
                    data: this.chartData.map(item => item.normalLogCount),
                    color: '#91c7ae'
                },
            ]
        };
        let chartInstance3 = echarts.init(chart3);
        chartInstance3.setOption(chartOption3);

        

          //----------------------------------The following is chart4-------------------------------------------------
          let chart4 = document.getElementById('Chart4');
          let chartOption4 = {
              title: {
                  text: 'Token 输出速度（Token/秒）',
                  subtext: '近 7 天'
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
                      name: 'Token 输出速度',
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, // Add area fill color  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance4 = echarts.init(chart4);
          chartInstance4.setOption(chartOption4);


          //----------------------------------The following is chart5--------------------------------------------------
          let chart5 = document.getElementById('Chart5');
          let chartOption5 = {
              title: {
                  text: '用户满意度',
                  subtext: '近 7 天'
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} 次'
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
                      name: '用户满意度',
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, // Add area fill color  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance5 = echarts.init(chart5);
          chartInstance5.setOption(chartOption5);

          

          //----------------------------------The following is chart6--------------------------------------------------
          let chart6 = document.getElementById('Chart6');
          let chartOption6 = {
              title: {
                  text: '全部消息数',
                  subtext: '近 7 天'
              },
              xAxis: {
                  type: 'category',
                  data: this.chartData.map(item => item.date)
              },
              yAxis: {
                  type: 'value',
                  axisLabel: {
                      formatter: '{value} 次'
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
                      name: '全部消息数',
                      type: 'line',
                      stack: '总量',
                      areaStyle: { color: '#91c7ae' }, // Add area fill color  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance6 = echarts.init(chart6);
          chartInstance6.setOption(chartOption6);

          

     




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
