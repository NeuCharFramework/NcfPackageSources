/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

var AgentsAppMethodsPluginMcp = {
    openFunctionBindingDrawer() {
      this.visible.drawerFunctionBindings = true
      this.functionBindingSearch = ''
      this.functionBindingTab = 'function'
      this.loadFunctionBindingCatalog()
    },
    async loadFunctionBindingCatalog() {
      this.functionBindingLoading = true
      try {
        const agentId = Number(this.agentForm?.id || 0)
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetFunctionBindingCatalog?agentId=${agentId}`)
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || '读取 Function 与 Workflow 列表失败')
          return
        }

        const catalog = data.data || {}
        this.$set(this, 'functionBindingCatalog', {
          functions: Array.isArray(catalog.functions) ? catalog.functions : [],
          plugins: Array.isArray(catalog.plugins) ? catalog.plugins : [],
          workflows: Array.isArray(catalog.workflows) ? catalog.workflows : [],
          currentBindings: Array.isArray(catalog.currentBindings) ? catalog.currentBindings : []
        })
        if ((!this.agentForm.functionBindings || this.agentForm.functionBindings.length === 0)
          && this.functionBindingCatalog.currentBindings.length > 0) {
          this.$set(this.agentForm, 'functionBindings', this.functionBindingCatalog.currentBindings)
          this.syncLegacyFunctionCallNames()
        }
      } catch (error) {
        console.error('读取 Function 绑定目录失败:', error)
        this.$message.error(error?.message || '读取 Function 与 Workflow 列表失败')
      } finally {
        this.functionBindingLoading = false
      }
    },
    normalizeFunctionBinding(binding) {
      if (!binding) return null
      const kind = String(binding.kind || binding.Kind || 'plugin').toLowerCase()
      const key = String(binding.key || binding.Key || '').trim()
      if (!key) return null
      return {
        kind,
        key,
        name: binding.name || binding.Name || key,
        description: binding.description || binding.Description || '',
        moduleUid: binding.moduleUid || binding.ModuleUid || '',
        functionKey: binding.functionKey || binding.FunctionKey || '',
        workflowId: binding.workflowId || binding.WorkflowId || (kind === 'workflow' ? Number(key) || null : null)
      }
    },
    isFunctionBindingSelected(option) {
      const normalized = this.normalizeFunctionBinding(option)
      if (!normalized) return false
      return (this.agentForm.functionBindings || []).some(item => {
        const current = this.normalizeFunctionBinding(item)
        return current && current.kind === normalized.kind && current.key.toLowerCase() === normalized.key.toLowerCase()
      })
    },
    toggleFunctionBinding(option, selected) {
      const normalized = this.normalizeFunctionBinding(option)
      if (!normalized) return
      const current = (this.agentForm.functionBindings || [])
        .map(item => this.normalizeFunctionBinding(item))
        .filter(Boolean)
      const index = current.findIndex(item => item.kind === normalized.kind
        && item.key.toLowerCase() === normalized.key.toLowerCase())
      if (selected && index < 0) {
        current.push(normalized)
      } else if (!selected && index >= 0) {
        current.splice(index, 1)
      }
      this.$set(this.agentForm, 'functionBindings', current)
      this.syncLegacyFunctionCallNames()
    },
    removeFunctionBinding(binding) {
      this.toggleFunctionBinding(binding, false)
    },
    syncLegacyFunctionCallNames() {
      const names = (this.agentForm.functionBindings || [])
        .map(item => this.normalizeFunctionBinding(item))
        .filter(item => item?.kind === 'plugin')
        .map(item => item.key)
      this.$set(this.agentForm, 'functionCallNames', [...new Set(names)].join(','))
    },
    //Legacy comment translated to English.
    displayedAvatars(list, limit = 5) {
      if (Array.isArray(list)) {
        return list?.slice(0, limit) ?? [];
      }
      return []
    },
    //Legacy comment translated to English.
    exceededCount(list, limit = 5) {
      if (Array.isArray(list)) {
        return list.length > limit ? list.length - limit : 0;
      }
      return 0
    },
    //Legacy comment translated to English.
    showFunctionCallInput() {
      this.functionCallInputVisible = true;
      this.$nextTick(_ => {
        this.$refs.functionCallInput.$refs.input.focus();
      });
    },

    //Legacy comment translated to English.
    handleFunctionCallInputConfirm() {
      const inputValue = this.functionCallInputValue;
      if (inputValue) {
        if (!this.agentForm.functionCallNames) {
          this.agentForm.functionCallNames = inputValue;
          this.functionCallTags = [inputValue];
        } else {
          const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
          if (!currentNames.includes(inputValue)) {
            this.agentForm.functionCallNames = [...currentNames, inputValue].join(',');
            this.functionCallTags = [...currentNames, inputValue];
          }
        }
      }
      this.functionCallInputVisible = false;
      this.functionCallInputValue = '';
    },

    //Legacy comment translated to English.
    handleFunctionCallClose(tag) {
      const currentNames = this.getFunctionCallNamesList();
      const index = currentNames.indexOf(tag);
      if (index > -1) {
        currentNames.splice(index, 1);
        this.agentForm.functionCallNames = currentNames.join(',');
      }
      this.functionCallTags = currentNames;
    },

    //Legacy comment translated to English.
    getFunctionCallNamesList() {
      return this.agentForm.functionCallNames
        ? this.agentForm.functionCallNames.split(',').filter(x => x)
        : [];
    },

    //Legacy comment translated to English.
    handleAutoAttachXncfChange(val) {
      if (val) {
        //Legacy comment translated to English.
        const currentNames = this.getFunctionCallNamesList();
        const allNames = [...new Set([...currentNames, ...this.pluginTypes])];
        this.agentForm.functionCallNames = allNames.join(',');
      } else {
        //Legacy comment translated to English.
        const currentNames = this.getFunctionCallNamesList();
        const manualNames = currentNames.filter(name => !this.pluginTypes.includes(name));
        this.agentForm.functionCallNames = manualNames.join(',');
      }
    },
    
    //Legacy comment translated to English.
    async testMcpEndpoint(name, endpoint) {
      //Legacy comment translated to English.
      this.$set(endpoint, 'testing', true);
      
      try {
        const response = await axios.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.TestMcpConnection', {
          params: {
            endpointName: name,
            endpointUrl: endpoint.url
          }
        });
        
        //Legacy comment translated to English.
        console.log('MCP测试响应数据:', response);
        
        //Legacy comment translated to English.
        if (response.data && response.data.success) {
          //Legacy comment translated to English.
          let tools = [];
          let status = 200;
          
          //Legacy comment translated to English.
          console.log('API返回数据结构:', JSON.stringify(response.data, null, 2));
          
          //Legacy comment translated to English.
          if (response.data.data) {
            console.log('data字段:', response.data.data);
            
            //Legacy comment translated to English.
            if (response.data.data.tools) {
              console.log('从data.tools获取工具列表');
              tools = response.data.data.tools;
              status = response.data.data.status || 200;
            } 
            //Legacy comment translated to English.
            else if (Array.isArray(response.data.data)) {
              console.log('data直接是工具列表');
              tools = response.data.data;
            }
          }
          
          console.log('提取的工具列表:', tools);
          
          //Legacy comment translated to English.
          if (!Array.isArray(tools)) {
            console.warn('工具列表不是数组，将转换为空数组');
            tools = [];
          }
          
          //Legacy comment translated to English.
          tools = tools.map(tool => ({
            name: tool.name || '未命名工具', 
            description: tool.description || '无描述',
            parameters: Array.isArray(tool.parameters) ? tool.parameters : []
          }));
          
          console.log('处理后的工具列表:', tools);
          
          //Legacy comment translated to English.
          if (!endpoint.testResult) {
            this.$set(endpoint, 'testResult', {});
          }
          
          // settingsresultproperty
          this.$set(endpoint.testResult, 'success', true);
          this.$set(endpoint.testResult, 'tools', tools);
          this.$set(endpoint.testResult, 'status', status);
          
          console.log('更新后的endpoint对象:', JSON.parse(JSON.stringify(endpoint)));
          
          this.$message.success('连接测试成功');
          
          //Legacy comment translated to English.
          if (tools && tools.length > 0) {
            this.showMcpToolsDialog(endpoint);
          }
        } else {
          const testResult = {
            success: false,
            message: response.data.errorMessage || '未知错误'
          };
          this.$set(endpoint, 'testResult', testResult);
          this.$message.error('连接测试失败: ' + testResult.message);
        }
      } catch (error) {
        console.error('测试MCP连接出错:', error);
        const testResult = {
          success: false,
          message: error.message || '未知错误'
        };
        this.$set(endpoint, 'testResult', testResult);
        this.$message.error('连接测试出错: ' + testResult.message);
      } finally {
        //Legacy comment translated to English.
        this.$set(endpoint, 'testing', false);
      }
    },
    
    //Legacy comment translated to English.
    async getPluginTypes() {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetPluginTypes');
        if (res?.data?.success) {
          this.pluginTypes = res.data.data || [];
        }
      } catch (error) {
        console.error('获取插件类型失败:', error);
        this.$message.error('获取插件类型失败');
      }
    },

    getKnowledgeBaseBindingInfo(knowledgeBaseId) {
      const id = Number(knowledgeBaseId || 0)
      if (!Number.isInteger(id) || id <= 0) {
        return { text: '未绑定', type: 'info' }
      }
      if (!this.knowledgeBaseOptionsLoaded) {
        return { text: '正在读取状态', type: 'info' }
      }
      const knowledgeBase = (this.knowledgeBaseOptions || []).find(item => Number(item.id) === id)
      if (!knowledgeBase) {
        return { text: '知识库不可用', type: 'danger' }
      }
      if (knowledgeBase.embeddingStatus === 'legacy') {
        return { text: '旧版向量化，待发布', type: 'warning' }
      }
      return knowledgeBase.isEmbedded
        ? { text: '可检索', type: 'success' }
        : { text: '待向量化', type: 'warning' }
    },

    getKnowledgeBaseOptionLabel(knowledgeBase) {
      if (!knowledgeBase) return ''
      if (knowledgeBase.embeddingStatus === 'legacy') {
        return `${knowledgeBase.name}（已向量化，待重新发布）`
      }
      return knowledgeBase.isEmbedded
        ? knowledgeBase.name
        : `${knowledgeBase.name}（未向量化）`
    },

    buildKnowledgeBaseUrl(knowledgeBaseId, focus = 'materials') {
      const id = Number(knowledgeBaseId || 0)
      if (!Number.isInteger(id) || id <= 0) return ''

      const url = new URL('/Admin/KnowledgeBase/Index', window.location.origin)
      url.searchParams.set('knowledgeBaseId', String(id))
      url.searchParams.set('focus', focus === 'materials' ? 'materials' : 'edit')
      return url.pathname + url.search
    },

    openKnowledgeBase(knowledgeBaseId, focus = 'materials') {
      const url = this.buildKnowledgeBaseUrl(knowledgeBaseId, focus)
      if (!url) {
        this.$message.warning('请先选择有效的知识库。')
        return
      }

      const targetName = `NcfKnowledgeBase_${knowledgeBaseId}`
      const openedWindow = window.open(url, targetName)
      if (openedWindow) {
        openedWindow.focus()
        return
      }

      this.$confirm(
        '当前环境不能打开新窗口。为保留尚未保存的 Agent 修改，请先保存；确认后将在当前页面跳转到知识库。',
        '打开知识库',
        {
          confirmButtonText: '仍要跳转',
          cancelButtonText: '取消',
          type: 'warning'
        })
        .then(() => window.location.assign(url))
        .catch(() => {})
    },

    async refreshKnowledgeBaseOptions() {
      await this.getKnowledgeBaseOptions(true)
    },

    async getKnowledgeBaseOptions(showFeedback = false) {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetKnowledgeBaseOptions')
        if (res?.data?.success) {
          this.knowledgeBaseOptions = Array.isArray(res.data.data) ? res.data.data : []
          if (showFeedback) {
            this.$message.success('知识库状态已刷新')
          }
        } else if (showFeedback) {
          this.$message.error(res?.data?.errorMessage || '知识库状态刷新失败')
        }
      } catch (error) {
        console.error('获取知识库列表失败:', error)
        this.knowledgeBaseOptions = []
        if (showFeedback) {
          this.$message.error('知识库状态刷新失败')
        }
      } finally {
        this.knowledgeBaseOptionsLoaded = true
      }
    },

    //Legacy comment translated to English.
    handleAddPluginType(pluginType) {
      if (!this.agentForm.functionCallNames) {
        this.agentForm.functionCallNames = pluginType;
        this.functionCallTags = [pluginType];
      } else {
        //Legacy comment translated to English.
        const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
        if (!currentNames.includes(pluginType)) {
          //Legacy comment translated to English.
          this.agentForm.functionCallNames = [...currentNames, pluginType].join(',');
          this.functionCallTags = [...currentNames, pluginType];
        }
      }
    },
    // McpEndpoints relatedmethod
    
    //Legacy comment translated to English.
    showMcpEndpointInput() {
      this.mcpEndpointInputVisible = true;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
      this.$nextTick(() => {
        if (this.$refs.mcpEndpointNameInput) {
          this.$refs.mcpEndpointNameInput.$refs.input.focus();
        }
      });
    },
    
    // edit Endpoint
    handleMcpEndpointEdit(name, endpoint) {
      this.mcpEndpointInputVisible = true;
      this.mcpEndpointNameValue = name;
      this.mcpEndpointUrlValue = endpoint.url;
      this.mcpEndpointEditMode = true;
      this.mcpEndpointOriginalName = name;
      
      this.$nextTick(() => {
        if (this.$refs.mcpEndpointNameInput) {
          this.$refs.mcpEndpointNameInput.$refs.input.focus();
        }
      });
    },
    
    //Legacy comment translated to English.
    cancelMcpEndpointInput() {
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    //Legacy comment translated to English.
    handleMcpEndpointInputConfirm() {
      const name = this.mcpEndpointNameValue.trim();
      const url = this.mcpEndpointUrlValue.trim();
      
      if (!name || !url) {
        this.$message.warning('名称和URL不能为空');
        return;
      }
      
      let endpoints = {};
      try {
        if (this.agentForm.mcpEndpoints) {
          endpoints = JSON.parse(this.agentForm.mcpEndpoints);
        }
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        endpoints = {};
      }
      
      if (this.mcpEndpointEditMode) {
        //Legacy comment translated to English.
        if (this.mcpEndpointOriginalName !== name) {
          delete endpoints[this.mcpEndpointOriginalName];
        }
      }
      
      //Legacy comment translated to English.
      endpoints[name] = { url };
      this.agentForm.mcpEndpoints = JSON.stringify(endpoints);
      
      //Legacy comment translated to English.
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    //Legacy comment translated to English.
    openMcpSelectDrawer() {
      this.visible.drawerMcpSelect = true;
      this.mcpSelectSearch = '';
      this.loadMcpEndpointOptions();
    },
    
    //Legacy comment translated to English.
    async loadMcpEndpointOptions() {
      this.mcpSelectLoading = true;
      this.mcpModuleAvailable = false;
      this.mcpSelectMessage = '';
      this.mcpEndpointOptions = [];
      try {
        const response = await serviceAM.get(
          '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetMcpEndpointOptions');
        const data = response && response.data ? response.data : {};
        if (!data.success) {
          this.mcpSelectMessage = data.errorMessage || '获取 MCP 列表失败';
          return;
        }
        const result = data.data || {};
        this.mcpModuleAvailable = !!result.mcpModuleAvailable;
        this.mcpSelectMessage = result.message || '';
        this.mcpEndpointOptions = Array.isArray(result.options) ? result.options : [];
      } catch (error) {
        console.error('获取 MCP 列表失败:', error);
        this.mcpSelectMessage = (error && error.message) || '获取 MCP 列表失败';
      } finally {
        this.mcpSelectLoading = false;
      }
    },
    
    //Legacy comment translated to English.
    isMcpOptionSelected(option) {
      if (!option) return false;
      const endpoints = this.parsedMcpEndpoints || {};
      const name = String(option.name || '').trim();
      if (name && endpoints[name]) return true;
      const url = String(option.endpoint || '').trim();
      if (!url) return false;
      return Object.keys(endpoints).some(key => {
        const value = endpoints[key] || {};
        return String(value.url || value.endpoint || '').trim() === url;
      });
    },
    
    //Legacy comment translated to English.
    toggleMcpSelect(option, selected) {
      if (!option) return;
      const name = String(option.name || '').trim();
      const url = String(option.endpoint || '').trim();
      const endpoints = this.parsedMcpEndpoints || {};
      
      if (selected) {
        if (!name || !url) {
          this.$message.warning('该端点缺少名称或 URL，无法添加');
          return;
        }
        endpoints[name] = { url };
        this.agentForm.mcpEndpoints = JSON.stringify(endpoints);
        this.$message.success('已添加 MCP 端点：' + name);
        return;
      }
      
      const keysToRemove = Object.keys(endpoints).filter(key => {
        if (name && key === name) return true;
        if (url) {
          const value = endpoints[key] || {};
          if (String(value.url || value.endpoint || '').trim() === url) return true;
        }
        return false;
      });
      if (!keysToRemove.length) return;
      keysToRemove.forEach(key => delete endpoints[key]);
      this.agentForm.mcpEndpoints = Object.keys(endpoints).length > 0
        ? JSON.stringify(endpoints)
        : '';
    },
    
    // delete Endpoint
    handleMcpEndpointRemove(name) {
      let endpoints = {};
      try {
        if (this.agentForm.mcpEndpoints) {
          endpoints = JSON.parse(this.agentForm.mcpEndpoints);
        }
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        return;
      }
      
      //Legacy comment translated to English.
      if (endpoints[name]) {
        delete endpoints[name];
        this.agentForm.mcpEndpoints = Object.keys(endpoints).length > 0 
          ? JSON.stringify(endpoints) 
          : '';
      }
    },
    
    //Legacy comment translated to English.
    showMcpToolsDialog(endpoint) {
      console.log('调用showMcpToolsDialog函数', endpoint);
      
      //Legacy comment translated to English.
      if (!endpoint) {
        console.error('endpoint参数为空');
        this.$message.warning('endpoint参数为空');
        return;
      }
      
      console.log('endpoint.testResult:', endpoint.testResult);
      
      if (endpoint && endpoint.testResult && endpoint.testResult.tools) {
        //Legacy comment translated to English.
        const tools = [...endpoint.testResult.tools];
        console.log('显示工具列表:', tools);
        console.log('工具列表数量:', tools.length);
        
        //Legacy comment translated to English.
        this.currentMcpTools = tools;
        this.visible.dialogMcpTools = true;
        
        console.log('设置currentMcpTools:', this.currentMcpTools);
        console.log('设置dialogMcpTools为可见');
        
        //Legacy comment translated to English.
        setTimeout(() => {
          if (!document.querySelector('.el-dialog__wrapper[aria-label="MCP 工具列表"]')) {
            console.warn('对话框未显示，使用备用弹窗');
            this.showMcpToolsAlert(tools);
          }
        }, 500);
      } else {
        console.warn('没有可用的工具信息', endpoint);
        this.$message.warning('没有可用的工具信息');
      }
    },

    //Legacy comment translated to English.
    showMcpToolsAlert(tools) {
      if (!tools || !tools.length) {
        this.$message.warning('没有可用的工具信息');
        return;
      }
      
      this.$alert(
        `<div>
          <h3>工具列表 (${tools.length}个)</h3>
          <ul style="padding-left: 20px; text-align: left;">
            ${tools.map(tool => 
              `<li style="margin-bottom: 10px;">
                <div style="font-weight: bold; color: #409EFF;">${tool.name}</div>
                <div style="margin: 5px 0; color: #606266;">${tool.description || '无描述'}</div>
                ${tool.parameters && tool.parameters.length > 0 ? 
                  `<div style="margin-top: 5px;">
                    <div style="font-weight: bold;">参数:</div>
                    <ul style="padding-left: 20px;">
                      ${tool.parameters.map(param => 
                        `<li><span style="color: #409EFF;">${param.name}</span>: ${param.description || ''}</li>`
                      ).join('')}
                    </ul>
                  </div>` : 
                  '<div>无参数</div>'
                }
              </li>`
            ).join('')}
          </ul>
        </div>`,
        'MCP工具列表',
        {
          dangerouslyUseHTMLString: true,
          closeOnClickModal: true,
          closeOnPressEscape: true,
          confirmButtonText: '关闭'
        }
      );
    },
};

