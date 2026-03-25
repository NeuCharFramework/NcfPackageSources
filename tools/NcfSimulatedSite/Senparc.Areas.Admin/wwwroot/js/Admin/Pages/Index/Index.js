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
      glowUpgradeableModules: false,
      // AI 对话入口相关
      chatInputText: '',
      isDragOver: false,
      selectedModules: [],
      isCreatingSession: false
    };
  },
  mounted() {
    this.getXncfStat();
    this.getXncfOpening();
    this.fetchChartData();
    this.fetchTodayLogData();
    // 添加鼠标事件监听
    this.initializeHoverEffects();
    // 初始化模块拖拽
    this.initializeModuleDrag();
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
    async fetchTodayLogData() { // 新增获取今日日志数据的方法  
      try {
        let response = await service.get('/api/Senparc.Areas.Admin/StatAppService/Areas.Admin_StatAppService.GetTodayLog');
        if (response.data && response.data.data && response.data.data.items) {
          this.todayLogData = response.data.data.items;
          this.todayDate = response.data.data.date;
          this.initChart(); // 确保图表在获取到数据后更新  
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
            areaStyle: { color: '#91c7ae' }, // 添加区域填充颜色  
            data: this.chartData.map(item => item.normalLogCount),
            color: '#91c7ae'
          },
          {
            name: '异常日志',
            type: 'line',
            stack: '总量',
            areaStyle: { color: '#d48265' }, // 添加区域填充颜色  
            data: this.chartData.map(item => item.exceptionLogCount),
            color: '#d48265'
          }
        ]
      };
      let chartInstance1 = echarts.init(chart1);
      chartInstance1.setOption(chartOption1);

      // 添加点击事件监听器  
      chartInstance1.on('click', params => {
        if (params.componentType === 'series') {
          let date = params.name;
          window.location.href = `/Admin/SenparcTrace/DateLog?date=${date}`;
        }
      });

      // 准备今日日志数据  
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
          data: this.todayLogData.map(item => item.senparcTraceType) // 自动输出所有类别  
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
      // 添加点击事件监听器    
      chartInstance2.on('click', params => {
        if (params.componentType === 'series') {
          window.location.href = `/Admin/SenparcTrace/DateLog?date=${this.todayDate}`;
        }
      });

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
    },

    // AI 对话入口相关方法

    async startChatSession() {
      if (!this.chatInputText || this.chatInputText.trim().length === 0) {
        this.$message.warning('请输入对话内容');
        return;
      }

      this.isCreatingSession = true;

      try {
        const requestData = {
          initialMessage: this.chatInputText.trim(),
          moduleUids: this.selectedModules.map(m => m.uid)
        };

        const response = await service.post('/api/Senparc.Areas.Admin/AdminChatAppService/Areas.Admin_AdminChatAppService.CreateSessionAsync', requestData);

        if (response.data && response.data.success && response.data.data) {
          const sessionId = response.data.data.sessionId;
          const moduleUidsQuery = this.selectedModules.length > 0 
            ? '&moduleUids=' + this.selectedModules.map(m => m.uid).join(',')
            : '';
          
          window.location.href = `/Admin/AdminChat/Chat?sessionId=${sessionId}${moduleUidsQuery}`;
        } else {
          this.$message.error(response.data.errorMessage || '创建会话失败');
        }
      } catch (error) {
        console.error('创建会话失败:', error);
        this.$message.error('创建会话失败，请稍后重试');
      } finally {
        this.isCreatingSession = false;
      }
    },

    handleModuleDrop(event) {
      this.isDragOver = false;
      
      try {
        const moduleDataStr = event.dataTransfer.getData('application/json');
        if (!moduleDataStr) {
          console.warn('未获取到模块数据');
          return;
        }

        const moduleData = JSON.parse(moduleDataStr);
        
        if (!moduleData.uid) {
          console.warn('模块数据不完整:', moduleData);
          return;
        }

        const exists = this.selectedModules.some(m => m.uid === moduleData.uid);
        if (!exists) {
          this.selectedModules.push({
            uid: moduleData.uid,
            name: moduleData.name || '未知模块',
            icon: moduleData.icon || 'fa fa-cube',
            version: moduleData.version || ''
          });
          this.$message.success(`已添加模块: ${moduleData.name}`);
        } else {
          this.$message.info('该模块已添加');
        }
      } catch (error) {
        console.error('处理模块拖放失败:', error);
        this.$message.error('添加模块失败');
      }
    },

    handleDragOver(event) {
      this.isDragOver = true;
    },

    handleDragLeave(event) {
      if (event.target.classList.contains('chat-module-drop-zone')) {
        this.isDragOver = false;
      }
    },

    removeModule(uid) {
      this.selectedModules = this.selectedModules.filter(m => m.uid !== uid);
    },

    clearSelectedModules() {
      this.selectedModules = [];
    },

    initializeModuleDrag() {
      this.$nextTick(() => {
        const moduleCards = document.querySelectorAll('#xncf-modules-area .xncf-item');
        
        moduleCards.forEach((card) => {
          card.setAttribute('draggable', 'true');
          card.style.cursor = 'move';
          
          card.addEventListener('dragstart', (event) => {
            const cardElement = event.currentTarget;
            const linkElement = cardElement.querySelector('a[href*="uid="]');
            const headerElement = cardElement.querySelector('.el-card__header span:first-child');
            const iconElement = cardElement.querySelector('.icon');
            const versionElement = cardElement.querySelector('.version');
            const descElement = cardElement.querySelector('.description');

            const moduleData = {
              uid: linkElement?.href?.match(/uid=([^&]+)/)?.[1] || '',
              name: headerElement?.textContent?.trim() || '未知模块',
              icon: iconElement?.className || 'fa fa-cube',
              version: versionElement?.textContent?.trim() || '',
              description: descElement?.textContent?.trim() || ''
            };

            event.dataTransfer.setData('application/json', JSON.stringify(moduleData));
            event.dataTransfer.effectAllowed = 'copy';
            
            cardElement.style.opacity = '0.5';
            cardElement.classList.add('dragging');
            
            const dropZone = document.querySelector('.chat-module-drop-zone');
            if (dropZone) {
              dropZone.classList.add('highlight');
            }
          });
          
          card.addEventListener('dragend', (event) => {
            event.currentTarget.style.opacity = '1';
            event.currentTarget.classList.remove('dragging');
            
            const dropZone = document.querySelector('.chat-module-drop-zone');
            if (dropZone) {
              dropZone.classList.remove('highlight');
            }
          });
        });
      });
    }
  }
});  
