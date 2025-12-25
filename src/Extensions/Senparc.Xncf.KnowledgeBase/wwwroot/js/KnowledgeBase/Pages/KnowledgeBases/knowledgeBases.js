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
      },
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
        pageSize: 20,
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
        size: 10
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
      // 组 新增|编辑
      groupForm: {
        contentType: '', // 内容类型
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
      // 组 新增|编辑 智能体
      fileQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      fileList: [], // 组新增时的智能体列表
    }
  },
  created: function () {
    let that = this
    that.getList();
    that.getEmbeddingModelList();
    that.getVectorDBList();
    that.getChatModelList();
    debugger
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
      if (res.code == 200) {
        that.$notify({
          title: '成功',
          message: '恭喜你，上传成功',
          type: 'success'
        });
        that.dialog.data.cover = res.data;
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
    async getEmbeddingModelList() {
      debugger
      let that = this
      let param = {
        page: that.page.page,
        size: that.page.size,
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
        size: that.page.size,
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
        size: that.page.size,
      }
      await axios.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetPagedListAsync', param)
        .then(res => {
          console.log(res)
          that.chatModelData = res.data.data.data;
      })
    },
    // 获取 文件 数据
    async getFileListData(listType, page = 0) {
      const queryList = {}
      if (listType === 'file') {
        this.fileQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.fileQueryList)
      }
      // 接口对接
      await axios.get(`/api/Senparc.Xncf.FileManager/FileTemplateAppService/Xncf.FileManager_FileTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
        .then(res => {
          debugger
          const data = res?.data ?? {}
          if (data.success) {
            const fileData = data?.data?.list ?? []
            if (listType === 'file') {
              this.$set(this, 'fileList', fileData)
              // 确保更新数据时 不会清空选中
              this.$nextTick(() => {
                this.isGetGroupAgent = false
              })
              // 组成员table 初始选中
              if (this.visible.drawerGroup && this.groupForm.files.length > 0) {
                // this.toggleSelection()
                this.$nextTick(() => {
                  // this.groupAgentTotal = agentData.length
                  const filterList = fileData.filter(i => {
                    return this.groupForm.files.findIndex(item => item.id === i.id) !== -1
                  })
                  this.toggleSelection(filterList)
                })

              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
            this.isGetGroupAgent = false
          }
        }).catch((err) => {
          console.log('err', err)
          this.isGetGroupAgent = false
        })
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
    // 编辑 // 新增知识库管理 // 增加下一级
    handleEdit(index, row, flag) {
      debugger
      let that = this
      that.dialog.visible = true;
      //获取分类列表数据
      //that.getCategoryList();
      if (flag === 'add') {
        // 新增
        that.dialog.title = '新增知识库管理';
        that.dialogImageUrl = '';
        //that.$refs['bodyEditor'].editor.setData('');
        return;
      }
      // 编辑
      let { id, embeddingModelId, vectorDBId, chatModelId, name,content } = row;
      that.dialog.data = {
        id, embeddingModelId, vectorDBId, chatModelId, name,content
      };
      if (that.dialog.data.embeddingModelId != undefined) {
        that.selectDefaultEmbeddingModel[0] = parseInt(that.dialog.data.embeddingModelId);
      }
      if (that.dialog.data.vectorDBId != undefined) {
        that.selectDefaultVectorDB[0] = parseInt(that.dialog.data.vectorDBId);
      }
      if (that.dialog.data.chatModelId != undefined) {
        that.selectDefaultChatModel[0] = parseInt(that.dialog.data.chatModelId);
      }
      //if (cover != '' && cover != undefined)
      //{
      //    that.dialogImageUrl = cover;
      //}
      //if (body != '' && body != undefined)
      //{
      //    that.editorData = this.$refs['bodyEditor'].editor.setData(body);
      //}
      //if (that.editorData == '')
      //{
      //    that.editorData = this.$refs['bodyEditor'].editor.setData(body);
      //}
      // dialog中父级菜单 做递归显示
      //let x = [];
      //that.recursionFunc(row, that.chatModelData, x);
      //that.dialog.data.chatModelId = x;

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
    // 保存 submitForm 数据
    async saveSubmitFormData(saveType, serviceForm = {}) {
      debugger
      let serviceURL = ''
      // 组 新增|编辑
      if (saveType === 'drawerGroup') {
        // 调用新的批量导入文件 API
        serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBasesAppService/Xncf.KnowledgeBase_KnowledgeBasesAppService.ImportFilesToKnowledgeBase'
        
        // 从表单中提取选中的文件 ID 列表
        const selectedFiles = serviceForm.files || [];
        const fileIds = selectedFiles.map(file => file.id);
        
        // 构建请求数据
        const requestData = {
          knowledgeBaseId: serviceForm.knowledgeBasesId,
          fileIds: fileIds
        };
        
        try {
          service.post(serviceURL, requestData).then(res => {
            debugger
            that.$notify({
              title: "Success",
              message: "文件导入成功",
              type: "success",
              duration: 2000
            });
            that.visible.drawerGroup = false;
          });
        } catch (err) {
          console.error('Request Error:', err);
          that.$notify({
            title: "Error",
            message: "文件导入失败: " + err.message,
            type: "error",
            duration: 3000
          });
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
    // 更新新增、编辑
    updateData() {
      let that = this
      that.dialog.updateLoading = true;
      that.$refs['dataForm'].validate(valid => {
        //that.editorData = that.$refs['bodyEditor'].editor.getData()
        //that.dialog.data.body = that.$refs['bodyEditor'].editor.getData();
        // 表单校验
        if (valid) {
          that.dialog.updateLoading = true;
          let data = {
            Id: that.dialog.data.id,
            EmbeddingModelId: that.dialog.data.embeddingModelId.toString(),
            VectorDBId: that.dialog.data.vectorDBId.toString(),
            ChatModelId: that.dialog.data.chatModelId.toString(),
            Name: that.dialog.data.name,
            Content: that.dialog.data.content
          };
          console.log('add-' + JSON.stringify(data));
          service.post("/Admin/KnowledgeBase/Edit?handler=Save", data).then(res => {
            debugger
            if (res.data.success) {
              that.getList();
              that.$notify({
                title: "Success",
                message: "成功",
                type: "success",
                duration: 2000
              });
              that.dialog.visible = false;
            }
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
      let that = this
      that.getList();
    },
    resetCondition() {
      let that = this
      that.keyword = '';
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
      debugger
      if (btnType === 'embedding') {
        //开始向量化数据
        serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBasesAppService/Xncf.KnowledgeBase_KnowledgeBasesAppService.EmbeddingKnowledgeBase'
        let dataTemp = {
          id:item?.id ?? ''
        }

        if (!serviceURL) return
        try {
          service.post(serviceURL, dataTemp).then(res => {
            debugger
            that.$notify({
              title: "Success",
              message: "成功",
              type: "success",
              duration: 2000
            });
            that.visible.drawerGroup = false;
          });
        } catch (err) {
          console.error('Request Error:', err);
          this.isGetGroupAgent = false
        }
      }
    },
    // Dailog|抽屉 打开 按钮
    handleElVisibleOpenBtn(btnType,item) {
      let visibleKey = btnType
      if (btnType === 'drawerGroup') {
        //this.getAgentListData('groupAgent')
        visibleKey = 'drawerGroup'
        //设置
        this.groupForm.knowledgeBasesId = item?.id ?? ''
      }
      //新建文件
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
          debugger
          const submitForm = this[formName] ?? {}
          //提交数据给后端
          this.saveSubmitFormData(btnType, submitForm)
          debugger
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    // 组 新增|编辑 智能体 成员取消选中
    groupMembersCancel(item, index) {
      this.groupForm.members.splice(index, 1);
      const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
      if (findIndex !== -1) {
        this.toggleSelection([this.groupAgentList[findIndex]])
      }
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