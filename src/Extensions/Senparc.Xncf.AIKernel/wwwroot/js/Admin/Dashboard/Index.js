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
      // 添加动画控制变量
      shakeAllModules: false,
      glowUpgradeableModules: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    // 添加鼠标事件监听
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

      //----------------------------------下为chart1--------------------------------------
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
            areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      


          //----------------------------------下为chart2--------------------------------------
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
                      areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance2 = echarts.init(chart2);
          chartInstance2.setOption(chartOption2);

          


        //----------------------------------下为chart3--------------------------------------
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
                    areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
                    data: this.chartData.map(item => item.normalLogCount),
                    color: '#91c7ae'
                },
            ]
        };
        let chartInstance3 = echarts.init(chart3);
        chartInstance3.setOption(chartOption3);

        

          //----------------------------------下为chart4--------------------------------------
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
                      areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance4 = echarts.init(chart4);
          chartInstance4.setOption(chartOption4);


          //----------------------------------下为chart5--------------------------------------
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
                      areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance5 = echarts.init(chart5);
          chartInstance5.setOption(chartOption5);

          

          //----------------------------------下为chart6--------------------------------------
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
                      areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
                      data: this.chartData.map(item => item.normalLogCount),
                      color: '#91c7ae'
                  },
              ]
          };
          let chartInstance6 = echarts.init(chart6);
          chartInstance6.setOption(chartOption6);

          

     




    },
    //XNCF 统计状态  
    async getXncfStat() {
      let xncfStatData = await service.get('/Admin/Index?handler=XncfStat');
      this.xncfStat = xncfStatData.data.data;
    },
    //开放模块数据  
    async getXncfOpening() {
      let xncfOpeningList = await service.get('/Admin/Index?handler=XncfOpening');
      this.xncfOpeningList = xncfOpeningList.data.data;
    },
    //点击打开模块
    navigateTo(uid) {
      window.location.href = '/Admin/XncfModule/Start/?uid=' + uid;
    },
    getOpenDetail(rowIndex, menus) {
      console.log(`rowIndex --- ${JSON.stringify(rowIndex)}`)
      console.log(`menus --- ${JSON.stringify(menus)}`)
      var menuInfo = menus[rowIndex]
      window.location.href = menuInfo.url
    },
    // 添加新方法处理悬停效果
    initializeHoverEffects() {
      // 获取统计项元素 - 修正选择器
      const installedModulesStat = document.querySelector('.xncf-stat-item');
      const updateModulesStat = document.querySelectorAll('.xncf-stat-item')[1];

      // 已安装模块统计项的鼠标事件
      if (installedModulesStat) {
        installedModulesStat.addEventListener('mouseenter', () => {
          this.triggerShakeAnimation();
        });
      }

      // 待更新模块统计项的鼠标事件
      if (updateModulesStat) {
        updateModulesStat.addEventListener('mouseenter', () => {
          this.triggerGlowAnimation();
        });
      }
    },

    // 触发抖动动画
    triggerShakeAnimation() {
      const moduleCards = document.querySelectorAll('#xncf-modules-area .box-card');
      moduleCards.forEach(card => {
        // 添加随机延迟
        const delay = Math.random() * 200; // 0-200ms的随机延迟
        setTimeout(() => {
          card.classList.add('shake-animation');
          // 动画结束后移除类
          setTimeout(() => {
            card.classList.remove('shake-animation');
          }, 800); // 与动画持续时间匹配
        }, delay);
      });
    },

    // 触发发光/淡化动画
    triggerGlowAnimation() {
      const allCards = document.querySelectorAll('#xncf-modules-area .box-card');
      const upgradeableVersions = document.querySelectorAll('#xncf-modules-area .version-upgradeable');

      // 为所有可更新的模块添加发光效果
      upgradeableVersions.forEach(version => {
        const card = version.closest('.box-card');
        if (card) {
          // 添加随机延迟
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('glow-animation');
            setTimeout(() => {
              card.classList.remove('glow-animation');
            }, 1200); // 与动画持续时间匹配
          }, delay);
        }
      });

      // 为不可更新的模块添加淡化效果
      allCards.forEach(card => {
        if (!card.querySelector('.version-upgradeable')) {
          // 添加随机延迟
          const delay = Math.random() * 200;
          setTimeout(() => {
            card.classList.add('fade-animation');
            setTimeout(() => {
              card.classList.remove('fade-animation');
            }, 1200); // 与动画持续时间匹配
          }, delay);
        }
      });
    }
  }
});  
