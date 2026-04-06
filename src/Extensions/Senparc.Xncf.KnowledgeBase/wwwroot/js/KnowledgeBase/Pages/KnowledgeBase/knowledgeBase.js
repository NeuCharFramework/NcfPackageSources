new Vue({
  el: "#app",
  data() {
    var validateCode = (rule, value, callback) => {
      callback();
    };
    return {
      defaultMSG: null,
      editorData: '',
      elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
      // 显隐 visible
      visible: {
        drawerGroup: false, // 组 新增|编辑
        dialogFile: false,   // 配置抽屉内「新建文件」上传弹框
        embeddingProgress: false,  // 向量化进度弹窗
        embeddingResult: false,    // 向量化结果展示弹窗
      },
      embeddingProgressPercent: 0,
      embeddingProgressStatus: '',   // '' | success | exception
      embeddingProgressText: '正在准备...',
      embeddingResultText: '',
      _embeddingTimer: null,
      configUploadFileList: [], // 新建文件弹框内的上传列表（仅展示用，关闭时清空）
      form:
      {
        content: ''
      },
      config:
      {
        initialFrameHeight: 500
      },
      //分页参数
      paginationQuery:
      {
        total: 5
      },
      //分页接口传参
      listQuery: {
        pageIndex: 1,
        pageSize: 10,
        keyword: '',
        orderField: ''
      },
      selectDefaultEmbeddingModel: [],
      embeddingModelData: [],
      selectDefaultVectorDB: [],
      vectorDBData: [],
      selectDefaultChatModel: [],
      chatModelData: [],
      page: {
        page: 1,
        size: 10,
        modelCount: 999
      },
      filterTableHeader: {
        embeddingModelId: [],
        vectorDBId: [],
        chatModelId: [],
        name: []
      },
      colData: [
        { title: "Embedding模型Id", istrue: false },
        { title: "向量数据库Id", istrue: false },
        { title: "对话模型Id", istrue: false },
        { title: "名称", istrue: true },
        { title: "内容", istrue: true },
      ],
      checkBoxGroup: [
        "Embedding模型Id", "向量数据库Id", "对话模型Id", "名称", "内容"
      ],
      checkedColumns: [],
      contentTypeData: [
        { value: 1, label: '输入' },
        { value: 2, label: '文件' },
        { value: 3, label: '采集外部数据' }
      ],
      keyword: '',
      multipleSelection: '',
      radio: '',
      props: { multiple: true },
      // 表格数据
      tableData: [],
      uid: '',
      fileList: [],
      sizeForm: {
        name: '',
        region: '',
        date1: '',
        date2: '',
        delivery: false,
        type: [],
        resource: '',
        desc: ''
      },
      size: 'mini',
      dialogImageUrl: '',
      dialogVisible: false,
      dialog:
      {
        title: '新增知识库管理',
        visible: false,
        data:
        {
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: '', content: ''
        },
        rules:
        {
          name:
            [
              { required: true, message: "知识库管理名称为必填项", trigger: "blur" }
            ]
        },
        updateLoading: false,
        disabled: false,
        checkStrictly: true // 是否严格的遵守父子节点不互相关联
      },
      detailDialog:
      {
        title: '知识库管理详情',
        visible: false,
        data:
        {
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: '', content: ''
        },
        rules:
        {
          name:
            [
              { required: true, message: "知识库管理名称为必填项", trigger: "blur" }
            ]
        },
        updateLoading: false,
        disabled: false,
        checkStrictly: true // 是否严格的遵守父子节点不互相关联
      },
      // 组 新增|编辑（内容类型：1=输入 2=文件 3=采集外部数据，默认文件）
      groupForm: {
        contentType: 2, // 内容类型，默认「文件」
        files: [], // 文件列表
        content: '', // 内容
        knowledgeBasesId: '' //知识库ID
      },
      groupFormRules: {
        name: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        members: [
          { required: true, message: '请填写', trigger: 'change' },
        ],
        adminAgentTemplateId: [
          { required: true, message: '请填写', trigger: 'change' },
        ],
        enterAgentTemplateId: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        // description: [
        //     { required: true, message: '请填写', trigger: 'blur' },
        // ],
      },
      // 组 新增|编辑 智能体
      groupAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      groupAgentList: [], // 组新增时的智能体列表
      // 配置页文件列表查询与分页
      fileQueryList: {
        pageIndex: 1,
        pageSize: 10,
        filter: '', // 文件名称筛选
      },
      fileListTotal: 0, // 文件列表总条数，用于分页
      fileList: [], // 配置页文件列表
      fileNamesToSelect: [], // 打开配置时待选中的文件名（从 KnowledgeBaseItem 回显用）
    }
  },
  created: function () {
    let that = this
    that.getList();
    that.getEmbeddingModelList();
    that.getVectorDBList();
    that.getChatModelList();
    //debugger
    // 获取文件数据
    that.getFileListData('file');

    //TODO:初始化设置选中的字段
    that.checkedColumns = that.colData.filter(item => item.istrue).map(item => item.title);
    console.log(`that.checkedColumns --- ${JSON.stringify(that.checkedColumns)}`)
  },
  watch:
  {
    'dialog.visible': function (val, old) {
      // 关闭dialog，清空
      if (!val) {
        this.dialog.data = {
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: '', content: ''
        };
        this.dialog.updateLoading = false;
        this.dialog.disabled = false;
      }
    },
    'checkedColumns': function (val) {
      //debugger
      console.log(val);
      let arr = this.checkBoxGroup.filter(i => !val.includes(i));
      this.colData.filter(i => {
        if (arr.indexOf(i.title) != -1) {
          i.istrue = false;
        } else {
          i.istrue = true;
        }
      });
      this.reload = Math.random()
    }
  },
  methods:
  {
    handleChange(value) {
      console.log(value);
    },
    handleRemove(file, fileList) {
      log(file, fileList, 2);
    },
    handlePictureCardPreview(file) {
      let that = this
      that.dialogImageUrl = file.url;
      that.dialogVisible = true;
    },
    uploadSuccess(res, file, fileList) {
      let that = this;
      // 上传成功
      that.fileList = fileList;
      //debugger
      if (res.stateCode == 0) {
        that.$notify({
          title: '成功',
          message: '恭喜你，上传成功',
          type: 'success'
        });
        // 清空文件列表（关键）
        that.$refs.uploadRef.clearFiles();
      } else {
        that.$notify.error({
          title: '失败',
          message: '上传失败，请重新上传'
        });
      }
    },
    uploadError() {
      let that = this;
      that.$notify.error({
        title: '失败',
        message: '上传失败，请重新上传'
      });
    },
    // 配置抽屉「新建文件」弹框：上传成功后将文件写入 FileManage，并加入当前已选列表、刷新文件列表并勾选新文件
    async configUploadSuccess(res, file, fileList) {
      const that = this;
      const ok = (res && (res.stateCode === 0 || res.success === true));
      const fileId = res && res.data != null ? res.data : null;
      if (ok && fileId != null) {
        const id = typeof fileId === 'number' ? fileId : parseInt(fileId, 10);
        if (!isNaN(id)) {
          that.groupForm.files = that.groupForm.files || [];
          that.groupForm.files.push({ id: id, name: file.name || file.fileName || '' });
        }
        await that.getFileListData('file');
        that.$nextTick(() => {
          const row = that.fileList.find(i => i.id === id);
          if (row) that.toggleSelection(that.groupForm.files.map(f => that.fileList.find(i => i.id === f.id)).filter(Boolean));
        });
        that.$notify({ title: '成功', message: '文件已上传至文件管理', type: 'success' });
      } else {
        that.$notify.error({ title: '失败', message: (res && res.errorMessage) || '上传失败，请重试' });
      }
      that.$nextTick(() => {
        if (that.$refs.configUploadRef) that.$refs.configUploadRef.clearFiles();
      });
    },
    configUploadError() {
      this.$notify.error({ title: '失败', message: '上传失败，请重新上传' });
    },
    handleDialogFileClose() {
      this.configUploadFileList = [];
      if (this.$refs.configUploadRef) this.$refs.configUploadRef.clearFiles();
    },
    async getEmbeddingModelList() {
      //debugger
      let that = this
      let param = {
        page: that.page.page,
        size: that.page.modelCount,
      }
      await axios.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetPagedListAsync', param)
        .then(res => {
          console.log(res)
          that.embeddingModelData = res.data.data.data;
        })

    },
    async getVectorDBList() {
      let that = this
      let param = {
        page: that.page.page,
        size: that.page.modelCount,
      }
      await axios.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.GetPagedListAsync', param)
        .then(res => {
          console.log(res)
          that.vectorDBData = res.data.data.data;
        })
    },
    async getChatModelList() {
      let that = this
      let param = {
        page: that.page.page,
        size: that.page.modelCount,
      }
      await axios.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetPagedListAsync', param)
        .then(res => {
          console.log(res)
          that.chatModelData = res.data.data.data;
        })
    },
    // 获取 文件 数据（支持分页与筛选）
    async getFileListData(listType, page) {
      const that = this
      if (listType === 'file') {
        if (page != null) that.fileQueryList.pageIndex = page
      }
      const queryList = {}
      if (listType === 'file') {
        Object.assign(queryList, that.fileQueryList)
      }
      await axios.get(`/api/Senparc.Xncf.FileManager/FileTemplateAppService/Xncf.FileManager_FileTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const payload = data?.data ?? {}
            const fileData = payload.list ?? []
            if (listType === 'file') {
              that.$set(that, 'fileList', fileData)
              that.fileListTotal = payload.totalCount ?? 0
              that.$nextTick(() => {
                if (that.visible.drawerGroup && that.groupForm.files.length > 0) {
                  const filterList = fileData.filter(i => that.groupForm.files.some(item => item.id === i.id))
                  that.toggleSelection(filterList)
                }
              })
            }
          } else {
            app.$message({ message: data.errorMessage || data.data || 'Error', type: 'error', duration: 5 * 1000 })
          }
        }).catch((err) => {
          console.log('err', err)
        })
    },
    // 配置页文件列表：页码变化（首页/上一页/下一页/尾页/跳转）
    handleConfigFilePageChange(page) {
      this.fileQueryList.pageIndex = page
      this.getFileListData('file')
    },
    // 配置页文件列表：每页条数变化
    handleConfigFileSizeChange(size) {
      this.fileQueryList.pageSize = size
      this.fileQueryList.pageIndex = 1
      this.getFileListData('file')
    },
    // 打开配置时：根据当前知识库拉取 KnowledgeBaseItem 关联项，回填文件选中或内容
    async loadConfigKnowledgeBaseItems(knowledgeBaseId) {
      const that = this
      if (!knowledgeBaseId) {
        that.getFileListData('file')
        return
      }
      const url = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseItemAppService/Xncf.KnowledgeBase_KnowledgeBaseItemAppService.GetListByKnowledgeBaseId'
      try {
        const res = await axios.get(`${url}?knowledgeBaseId=${knowledgeBaseId}`)
        const data = res?.data ?? {}
        const list = (data.success && data.data) ? (Array.isArray(data.data) ? data.data : []) : []
        if (list.length === 0) {
          that.getFileListData('file')
          return
        }
        const hasFileType = list.some(i => i.contentType === 100 || i.contentType === 200 || i.contentType === 300 || i.contentType === 400)
        if (hasFileType) {
          const fileNames = [...new Set(list.filter(i => i.fileName).map(i => i.fileName))]
          that.groupForm.contentType = 2
          that.groupForm.files = []
          that.fileNamesToSelect = fileNames
          that.fileQueryList.pageSize = 9999
          that.fileQueryList.pageIndex = 1
          await that.getFileListData('file')
          that.$nextTick(() => {
            if (that.fileNamesToSelect.length && that.fileList.length) {
              const matched = that.fileList.filter(f => that.fileNamesToSelect.includes(f.fileName))
              that.groupForm.files = matched.map(f => ({ id: f.id, name: f.fileName }))
              that.toggleSelection(matched)
            }
            that.fileNamesToSelect = []
            that.fileQueryList.pageSize = 10
            that.getFileListData('file')
          })
        } else {
          that.groupForm.contentType = (list.length && list[0].contentType === 0) ? 1 : 1
          that.groupForm.content = list.map(i => i.content).filter(Boolean).join('\n\n') || ''
          that.groupForm.files = []
          that.getFileListData('file')
        }
      } catch (e) {
        console.error(e)
        that.getFileListData('file')
      }
    },
    // 获取列表
    async getList() {
      let that = this
      //that.uid = resizeUrl().uid
      let { pageIndex, pageSize, keyword, orderField } = that.listQuery;
      if (orderField == '' || orderField == undefined) {
        orderField = 'AddTime Desc';
      }
      if (that.keyword != '' && that.keyword != undefined) {
        keyword = that.keyword;
      }
      debugger
      await service.get(`/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=${pageIndex}&pageSize=${pageSize}&keyword=${keyword}&orderField=${orderField}`).then(res => {// 使用 map 转换为目标格式的对象数组
        that.filterTableHeader.embeddingModelId = res.data.data.list.map(z => ({
          text: z.embeddingModelId,
          value: z.embeddingModelId
        }));
        that.filterTableHeader.vectorDBId = res.data.data.list.map(z => ({
          text: z.vectorDBId,
          value: z.vectorDBId
        }));
        that.filterTableHeader.chatModelId = res.data.data.list.map(z => ({
          text: z.chatModelId,
          value: z.chatModelId
        }));
        that.filterTableHeader.name = res.data.data.list.map(z => ({
          text: z.name,
          value: z.name
        }));
        that.filterTableHeader.content = res.data.data.list.map(z => ({
          text: z.content,
          value: z.content
        }));

        that.tableData = res.data.data.list;
        that.paginationQuery.total = res.data.data.totalCount;
      });
    },
    async getCategoryList() {
      let that = this
      //获取分类列表数据
      await service.get('/Admin/KnowledgeBase/Index?handler=KnowledgeBasesCategory').then(res => {
        that.categoryData = res.data.data.list;
        log('categoryData', res, 2);
      });
    },
    // 编辑 // 新增知识库管理（文件在配置中上传，此处不再使用文件列表）
    handleEdit(index, row, flag) {
      let that = this;
      that.dialog.visible = false;
      that.$nextTick(() => {
        that.dialog.visible = true;
      });

      if (flag === 'add') {
        // 新增 - 初始化空数据
        that.dialog.title = '新增知识库管理';
        that.dialog.data = {
          id: 0,
          embeddingModelId: 0,
          vectorDBId: 0,
          chatModelId: 0,
          name: '',
          content: ''
        };
        that.selectDefaultEmbeddingModel = [];
        that.selectDefaultVectorDB = [];
        that.selectDefaultChatModel = [];
        that.dialogImageUrl = '';
        return;
      }

      // 编辑 - 使用现有数据
      let { id, embeddingModelId, vectorDBId, chatModelId, name, content } = row;
      that.dialog.data = {
        id: id || 0,
        embeddingModelId: embeddingModelId || 0,
        vectorDBId: vectorDBId || 0,
        chatModelId: chatModelId || 0,
        name: name || '',
        content: content || ''
      };

      // 设置下拉框默认值
      if (that.dialog.data.embeddingModelId) {
        that.selectDefaultEmbeddingModel = [parseInt(that.dialog.data.embeddingModelId)];
      }
      if (that.dialog.data.vectorDBId) {
        that.selectDefaultVectorDB = [parseInt(that.dialog.data.vectorDBId)];
      }
      if (that.dialog.data.chatModelId) {
        that.selectDefaultChatModel = [parseInt(that.dialog.data.chatModelId)];
      }

      if (flag === 'edit') {
        that.dialog.title = '编辑知识库管理';
      }
    },
    // 设置父级菜单默认显示 递归
    recursionFunc(row, source, dest) {
      if (row.chatModelId === null) {
        return;
      }
      for (let i in source) {
        let ele = source[i];
        if (row.chatModelId === ele.id) {
          this.recursionFunc(ele, this.chatModelData, dest);
          dest.push(ele.id);
        }
        else {
          this.recursionFunc(row, ele.children, dest);
        }
      }
    },
    // 保存 submitForm 数据（配置抽屉确认：内容类型为文件时导入选中文件，否则不校验文件）
    async saveSubmitFormData(saveType, serviceForm = {}) {
      const that = this;
      if (saveType === 'drawerGroup') {
        const contentType = serviceForm.contentType;
        const isFileType = contentType === 2; // 2=文件

        if (isFileType) {
          const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseAppService/Xncf.KnowledgeBase_KnowledgeBaseAppService.ImportFilesToKnowledgeBase';
          const selectedFiles = serviceForm.files || [];
          const fileIds = selectedFiles.map(f => (typeof f.id === 'number' ? f.id : parseInt(f.id, 10))).filter(id => !isNaN(id));

          if (fileIds.length === 0) {
            that.$notify({ title: '提示', message: '请至少选择一个文件后再保存', type: 'warning', duration: 2000 });
            return;
          }

          const requestData = {
            knowledgeBaseId: parseInt(serviceForm.knowledgeBasesId, 10),
            fileIds: fileIds
          };

          try {
            const res = await service.post(serviceURL, requestData);
            const success = res && (res.data === true || (res.data && (res.data.success === true || res.data.data === true)));
            if (success) {
              that.$notify({ title: '成功', message: '文件已关联到知识库并写入条目', type: 'success', duration: 2000 });
              that.visible.drawerGroup = false;
              that.getList();
            } else {
              that.$notify({ title: '失败', message: (res && res.message) || (res && res.data && res.data.errorMessage) || '文件导入失败', type: 'error', duration: 3000 });
            }
          } catch (err) {
            console.error('Request Error:', err);
            that.$notify({ title: '错误', message: '文件导入失败: ' + (err.message || err), type: 'error', duration: 3000 });
          }
        } else {
          // 内容类型为「输入」或「采集外部数据」时，不校验文件，直接关闭
          that.$notify({ title: '成功', message: '已保存', type: 'success', duration: 2000 });
          that.visible.drawerGroup = false;
        }
      }
    },
    selectEmbeddingModel() {
      let that = this
      for (let i = 0; i < that.embeddingModelData.length; i++) {
        if (that.selectDefaultEmbeddingModel[0] == that.embeddingModelData[i].id) {
          that.dialog.data.embeddingModelId = that.embeddingModelData[i].id;
          //that.dialog.data.columnName = that.devicesData[i].name;
        }
      }
    },
    selectVectorDB() {
      let that = this
      for (let i = 0; i < that.vectorDBData.length; i++) {
        if (that.selectDefaultVectorDB[0] == that.vectorDBData[i].id) {
          that.dialog.data.vectorDBId = that.vectorDBData[i].id;
          //that.dialog.data.columnName = that.devicesData[i].name;
        }
      }
    },
    selectChatModel() {
      let that = this
      for (let i = 0; i < that.chatModelData.length; i++) {
        if (that.selectDefaultChatModel[0] == that.chatModelData[i].id) {
          that.dialog.data.chatModelId = that.chatModelData[i].id;
          //that.dialog.data.columnName = that.devicesData[i].name;
        }
      }
    },
    // 更新新增、编辑（文件改为在「配置」中上传并关联，此处不再传文件）
    updateData() {
      let that = this;
      that.dialog.updateLoading = true;
      that.$refs['dataForm'].validate(valid => {
        if (valid) {
          let data = {
            id: that.dialog.data.id || 0,
            embeddingModelId: parseInt(that.dialog.data.embeddingModelId) || 0,
            vectorDBId: parseInt(that.dialog.data.vectorDBId) || 0,
            chatModelId: parseInt(that.dialog.data.chatModelId) || 0,
            name: that.dialog.data.name,
            content: that.dialog.data.content || '',
            NcfFileIds: []
          };
          console.log('保存知识库数据：' + JSON.stringify(data));
          service.post("/Admin/KnowledgeBase/Edit?handler=Save", data).then(res => {
            console.log('保存响应：', res);
            debugger
            // res.data 是后端返回的对象：{success: true, data: true, msg: "保存成功"}
            if (res.data && res.data.data.success && res.data.data.data === true) {
              that.getList();
              that.$notify({
                title: "成功",
                message: res.data.msg || "知识库保存成功",
                type: "success",
                duration: 2000
              });
              that.dialog.visible = false;
              that.dialog.updateLoading = false;
            } else {
              that.$notify({
                title: "失败",
                message: (res.data && res.data.msg) || "保存失败，请检查数据",
                type: "error",
                duration: 3000
              });
              that.dialog.updateLoading = false;
            }
          }).catch(err => {
            console.error('保存错误：', err);
            that.$notify({
              title: "错误",
              message: "保存出错：" + (err.message || err),
              type: "error",
              duration: 3000
            });
            that.dialog.updateLoading = false;
          });
        }
      });
    },
    // 删除
    handleDelete(index, row) {
      let that = this
      let ids = [row.id];
      service.post("/Admin/KnowledgeBase/edit?handler=Delete", ids).then(res => {
        if (res.data.success) {
          that.getList();
          that.$notify({
            title: "Success",
            message: "删除成功",
            type: "success",
            duration: 2000
          });
        }
      });
    },
    handleSelectionChange(val) {
      let that = this
      that.multipleSelection = val;
      console.log(`that.multipleSelection----${JSON.stringify(that.multipleSelection)}`);
      if (that.multipleSelection.length == 1) {
        //that.newsId = that.multipleSelection[0].id;
        //that.dialogVote.data.newsId = that.multipleSelection[0].id;
        //console.log(`that.newsId----${that.newsId}`);
      }
    },
    // 配置抽屉内「文件列表」表格勾选变化：同步到 groupForm.files，保存时用于关联知识库
    handleConfigFileSelectionChange(val) {
      this.groupForm.files = (val || []).map(r => ({
        id: r.id,
        name: r.fileName != null ? r.fileName : (r.name || '')
      }));
    },
    handleDbClick(row, column, event) {
      let that = this
      //that.multipleSelection = val;
      console.log(`row----${JSON.stringify(row)}`);
      console.log(`column----${JSON.stringify(column)}`);
      console.log(`event----${JSON.stringify(event)}`);
      //if (that.multipleSelection.length == 1) {
      //  //that.newsId = that.multipleSelection[0].id;
      //  //that.dialogVote.data.newsId = that.multipleSelection[0].id;
      //  //console.log(`that.newsId----${that.newsId}`);
      //}
      that.detailDialog.visible = true;
    },
    // 筛选输入变化
    handleFilterChange(value, filterType) {
      console.log('handleFilterChange', filterType, value)
      if (filterType === 'groupAgent') {
        this.groupAgentQueryList.filter = value
        this.getAgentListData('groupAgent', 1)
      }
    },
    // 配置页文件列表：按文件名称搜索（调用接口过滤，并回到第一页）
    handleConfigFileSearch() {
      this.fileQueryList.pageIndex = 1
      this.getFileListData('file')
    },
    // 配置页文件列表：删除文件（删除数据库与物理文件，并刷新列表与已选）
    handleConfigFileDelete(row) {
      const that = this
      that.$confirm(`确认删除文件「${row.fileName || row.name}」吗？删除后数据库与物理文件均会移除。`, '删除确认', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        that.doDeleteFileById(row.id).then(ok => {
          if (ok) {
            that.groupForm.files = (that.groupForm.files || []).filter(f => f.id !== row.id)
            that.getFileListData('file')
          }
        })
      }).catch(() => { })
    },
    // 配置页文件列表：批量删除（删除当前选中的多行，并同步数据库与物理文件）
    handleConfigFileBatchDelete() {
      const that = this
      const selected = (that.groupForm.files || []).slice()
      if (selected.length === 0) {
        that.$message.warning('请先勾选需要删除的文件')
        return
      }
      that.$confirm(`确认删除已选中的 ${selected.length} 个文件吗？删除后数据库与物理文件均会移除。`, '批量删除确认', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        const deleteUrl = '/api/Senparc.Xncf.FileManager/FileTemplateAppService/Xncf.FileManager_FileTemplateAppService.DeleteFile'
        const promises = selected.map(f => service.post(deleteUrl, { id: f.id }).then(res => {
          const ok = res && (res.data === true || (res.data && (res.data.success === true || res.data.data === true)))
          return !!ok
        }).catch(() => false))
        Promise.all(promises).then(results => {
          const successCount = results.filter(Boolean).length
          that.groupForm.files = []
          that.getFileListData('file')
          that.$notify({
            title: successCount === selected.length ? '成功' : '部分完成',
            message: `已删除 ${successCount}/${selected.length} 个文件`,
            type: successCount === selected.length ? 'success' : 'warning'
          })
        })
      }).catch(() => { })
    },
    doDeleteFileById(id) {
      const url = '/api/Senparc.Xncf.FileManager/FileTemplateAppService/Xncf.FileManager_FileTemplateAppService.DeleteFile'
      return service.post(url, { id: id }).then(res => {
        const ok = res && (res.data === true || (res.data && (res.data.success === true || res.data.data === true)))
        if (ok) this.$notify({ title: '成功', message: '文件已删除', type: 'success' })
        else this.$notify({ title: '失败', message: (res && res.data && res.data.errorMessage) || '删除失败', type: 'error' })
        return ok
      }).catch(err => {
        this.$notify({ title: '错误', message: '删除失败: ' + (err.message || err), type: 'error' })
        return false
      })
    },
    getCurrentRow(row) {
      let that = this
      //获取选中数据
      //that.templateSelection = row;
      that.multipleSelection = row;
      log('multipleSelection', row, 2)
      //that.newsId = that.multipleSelection.id;
      //that.dialogVote.data.newsId = that.multipleSelection.id;
    },
    filterTag(value, row) {
      return row.tag === value;
    },
    filterHandler(value, row, column) {
      const property = column['property'];
      return row[property] === value;
    },
    handleSearch() {
      let that = this;
      that.listQuery.keyword = that.keyword || '';
      that.listQuery.pageIndex = 1;
      that.getList();
    },
    resetCondition() {
      let that = this;
      that.keyword = '';
      that.listQuery.keyword = '';
      that.listQuery.pageIndex = 1;
      that.getList();
    },
    handlePageSizeChange(val) {
      this.listQuery.pageSize = val;
      this.listQuery.pageIndex = 1;
      this.getList();
    },
    handlePageChange(val) {
      this.listQuery.pageIndex = val;
      this.getList();
    },
    setRecommendFormat(row, column, cellValue, index) {
      if (cellValue) {
        return "Y";
      }
      return "N";
    },
    setBodyFormat(row, column, cellValue, index) {
      if (cellValue == undefined) {
        return '-';
      }
      else {
        return cellValue.substring(0, 16);
      }
    },

    setContentFormat(row, column, cellValue, index) {
      if (cellValue == undefined) {
        return '-';
      }
      else {
        return cellValue.replace(/<[^>]+>/gim, '').replace(/\[(\w+)[^\]]*](.*?)\[\/\1]/g, '$2 ').substring(0, 16);
      }
    },
    handleClick() {

    },
    onSubmit() {
      console.log('submit!');
    },
    handleEmbeddingBtn(btnType, item) {
      const that = this;
      if (btnType === 'embedding') {
        this.$confirm(`确认对知识库 "${item.name}" 进行向量化处理吗？`, '向量化确认', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        }).then(() => {
          that.embeddingProgressPercent = 0;
          that.embeddingProgressStatus = '';
          that.embeddingProgressText = '正在准备...';
          that.visible.embeddingProgress = true;

          var progressVal = 0;
          that._embeddingTimer = setInterval(function () {
            progressVal += Math.random() * 8 + 4;
            if (progressVal > 90) progressVal = 90;
            that.embeddingProgressPercent = Math.floor(progressVal);
            that.embeddingProgressText = '正在向量化处理中... ' + that.embeddingProgressPercent + '%';
          }, 400);

          const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseAppService/Xncf.KnowledgeBase_KnowledgeBaseAppService.EmbeddingKnowledgeBase';
          const dataTemp = { id: item?.id ?? '' };

          service.post(serviceURL, dataTemp).then(res => {
            if (that._embeddingTimer) {
              clearInterval(that._embeddingTimer);
              that._embeddingTimer = null;
            }
            that.embeddingProgressPercent = 100;
            that.embeddingProgressStatus = 'success';
            that.embeddingProgressText = '向量化完成';

            var body = res && res.data;
            var success = body && (body.success === true);
            var resultMessage = (body && body.data != null) ? (typeof body.data === 'string' ? body.data : (body.data.data != null ? body.data.data : '')) : '';
            if (success && resultMessage) {
              setTimeout(function () {
                that.visible.embeddingProgress = false;
                that.embeddingResultText = resultMessage;
                that.visible.embeddingResult = true;
              }, 400);
            } else if (success) {
              setTimeout(function () {
                that.visible.embeddingProgress = false;
                that.embeddingResultText = '知识库「' + (item.name || '') + '」向量化已完成。';
                that.visible.embeddingResult = true;
              }, 400);
            } else {
              that.embeddingProgressStatus = 'exception';
              that.embeddingProgressText = (res && res.errorMessage) || (res && res.message) || '向量化失败';
              setTimeout(function () {
                that.visible.embeddingProgress = false;
                that.$notify({ title: '向量化失败', message: that.embeddingProgressText, type: 'error', duration: 5000 });
              }, 800);
            }
          }).catch(err => {
            if (that._embeddingTimer) {
              clearInterval(that._embeddingTimer);
              that._embeddingTimer = null;
            }
            that.embeddingProgressStatus = 'exception';
            that.embeddingProgressPercent = Math.max(that.embeddingProgressPercent, 50);
            that.embeddingProgressText = '处理出错：' + (err.message || err);
            setTimeout(function () {
              that.visible.embeddingProgress = false;
              that.$notify({
                title: '错误',
                message: err.message || '向量化处理出错，请检查配置',
                type: 'error',
                duration: 5000
              });
            }, 800);
          });
        }).catch(function () { });
      }
    },
    // Dailog|抽屉 打开 按钮
    handleElVisibleOpenBtn(btnType, item) {
      const that = this
      let visibleKey = btnType
      if (btnType === 'drawerGroup') {
        visibleKey = 'drawerGroup'
        that.groupForm.knowledgeBasesId = item?.id ?? ''
        that.fileQueryList.pageIndex = 1
        that.fileNamesToSelect = []
        that.visible[visibleKey] = true
        that.loadConfigKnowledgeBaseItems(item?.id)
        return
      }
      if (btnType === 'dialogFile') {
        visibleKey = 'dialogFile'
      }
      this.visible[visibleKey] = true
    },
    // Dailog|抽屉 关闭 按钮
    handleElVisibleClose(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      this.$confirm('确认关闭？')
        .then(_ => {
          let refName = '', formName = ''
          // 组
          if (btnType === 'drawerGroup') {
            refName = 'groupELForm'
            formName = 'groupForm'
            this.visible[btnType] = false

            //// 重置 组获取智能体query
            //this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
            //this.groupAgentList = []
          }

          if (formName) {
            this.$set(this, `${formName}`, this.$options.data()[formName])
            // Object.assign(this[formName],this.$options.data()[formName])
          }
          if (refName) {
            this.$refs[refName].resetFields();
          }
          this.$nextTick(() => {
            this.visible[btnType] = false
          })
          // 清理 Function Calls 数据
          if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
            this.functionCallTags = []
            this.functionCallInputVisible = false
            this.functionCallInputValue = ''
          }
        })
        .catch(_ => { });
    },
    // Dailog|抽屉 提交 按钮
    handleElVisibleSubmit(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      let refName = '', formName = ''
      // 组
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        formName = 'groupForm'
      }
      if (!refName) return
      this.$refs[refName].validate((valid) => {
        if (valid) {
          //debugger
          const submitForm = this[formName] ?? {}
          //提交数据给后端
          this.saveSubmitFormData(btnType, submitForm)
          //debugger
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    // 配置抽屉「已选择」文件列表中移除某一项，并同步表格勾选
    groupMembersCancel(item, index) {
      this.groupForm.files.splice(index, 1);
      this.$nextTick(() => {
        const tbl = this.$refs.groupAgentTable;
        if (!tbl) return;
        tbl.clearSelection();
        this.groupForm.files.forEach(f => {
          const row = this.fileList.find(i => i.id === f.id);
          if (row) tbl.toggleRowSelection(row, true);
        });
      });
    },
    // 编辑 Dailog|抽屉 按钮 
    async handleEditDrawerOpenBtn(btnType, item) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      //console.log('handleEditDrawerOpenBtn', btnType, item);
      let formName = ''
      // 智能体
      if (['dialogGroupAgent'].includes(btnType)) {
        formName = 'agentForm'
      }

      if (formName) {
        if (btnType === 'drawerAgent' && item) {
          console.log('item', item);
          // 创建一个新的对象来存储表单数据
          const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };
          console.log('formData', formData);

          // 确保 functionCallNames 被正确初始化
          this.functionCallTags = formData.functionCallNames ? formData.functionCallNames.split(',').filter(Boolean) : [];

          // 将数据赋值给表单
          Object.assign(this[formName], formData);

          // 打印日志以便调试
          console.log('Loaded form data:', formData);
          console.log('functionCallTags:', this.functionCallTags);

        } else if (btnType === 'drawerGroup') {
          if (item.chatGroupDto) {
            Object.assign(this[formName], {
              ...item.chatGroupDto,
              members: item.agentTemplateDtoList || item.chatGroupMembers || []
            })
          } else {
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${item.id}`)
              .then(res => {
                const data = res?.data ?? {}
                if (data.success) {
                  const groupDetail = data?.data ?? {}
                  Object.assign(this[formName], {
                    ...groupDetail.chatGroupDto,
                    members: groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || []
                  })
                }
              })
          }
          // // 获取 全部智能体数据
          // this.getAgentListData('groupAgent')
        } else if (btnType === 'drawerTaskStart') {
          Object.assign(this[formName], {
            ...item
            // groupName: item?.name ?? ''
          })
        } else {
          Object.assign(this[formName], item)
        }
        // 回显 表单值
        // this.$set(this, `${formName}`, deepClone(item))
        // 打开 抽屉
        this.handleElVisibleOpenBtn(btnType)
      }
    },
    // 组 新增|编辑 智能体table 切换table 选中
    toggleSelection(rows) {
      if (rows) {
        rows.forEach(row => {
          this.$refs?.groupAgentTable?.toggleRowSelection(row);
        });
      } else {
        this.$refs?.groupAgentTable?.clearSelection();
      }
    },
  }
});



/**
* 处理接口 query 参数 转换为 string
* @param {Object} queryObj // 原地址
*/
function getInterfaceQueryStr(queryObj) {
  if (!queryObj) return ''
  // 将对象转换为 URL 参数字符串
  return Object.entries(queryObj)
    .filter(([key, value]) => {
      // 过滤掉空值
      // console.log('value', typeof value)
      if (typeof value === 'string') {
        return value !== ''
      } else if (typeof value === 'object' && value instanceof Array) {
        return value.length > 0
      } else if (typeof value === 'number') {
        return true
      } else {
        // if(typeof value === 'undefined')
        return false
      }
    })
    .map(
      ([key, value]) => {
        if (Array.isArray(value)) {
          let str = ""
          for (let index in value) {
            str += `${index > 0 ? '&' : ''}${encodeURIComponent(key)}=${encodeURIComponent(value[index])}`
          }
          return str
        }
        return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
      }
    )
    .join('&')
}