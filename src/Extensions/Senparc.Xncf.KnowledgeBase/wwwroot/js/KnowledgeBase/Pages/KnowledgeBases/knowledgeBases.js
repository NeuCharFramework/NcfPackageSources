
new Vue({
  el: "#app",
  data() {
    var validateCode = (rule, value, callback) => {
      callback();
    };
    return {
      defaultMSG: null,
      editorData: '',
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
        { title: "训练模型Id", istrue: false },
        { title: "向量数据库Id", istrue: false },
        { title: "对话模型Id", istrue: false },
        { title: "名称", istrue: true }
      ],
      checkBoxGroup: [
        "训练模型Id", "向量数据库Id", "对话模型Id", "名称"
      ],
      checkedColumns: [],

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
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: ''
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
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: ''
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
      }
    }
  },
  created: function () {
    let that = this
    that.getList();
    that.getEmbeddingModelList();
    that.getVectorDBList();
    that.getChatModelList();

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
          id: '', embeddingModelId: '', vectorDBId: '', chatModelId: '', name: ''
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
      let that = this
      // 上传成功
      that.fileList = fileList;
      log('上传成功', fileList.length, 2);
      log('res', res, 2);

      if (res.code == 200) {
        that.$notify({
          title: '成功',
          message: '恭喜你，上传成功',
          type: 'success'
        });
        that.dialog.data.cover = res.data;
      }
      else {
        that.$notify.error({
          title: '失败',
          message: '上传失败，请重新上传'
        });
      }
    },
    uploadError() {
      let that = this
      //that.refs.upload.clearFiles();
      that.$notify.error({
        title: '失败',
        message: '上传失败，请重新上传'
      });
    },
    async getEmbeddingModelList() {
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

      await service.get(`/Admin/KnowledgeBases/Index?handler=KnowledgeBases&pageIndex=${pageIndex}&pageSize=${pageSize}&keyword=${keyword}&orderField=${orderField}`).then(res => {// 使用 map 转换为目标格式的对象数组
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

        that.tableData = res.data.data.list;
        that.paginationQuery.total = res.data.data.totalCount;
      });
    },
    async getCategoryList() {
      let that = this
      //获取分类列表数据
      await service.get('/Admin/KnowledgeBases/Index?handler=KnowledgeBasesCategory').then(res => {
        that.categoryData = res.data.data.list;
        log('categoryData', res, 2);
      });
    },
    // 编辑 // 新增知识库管理 // 增加下一级
    handleEdit(index, row, flag) {
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
      let { id, embeddingModelId, vectorDBId, chatModelId, name } = row;
      that.dialog.data = {
        id, embeddingModelId, vectorDBId, chatModelId, name
      };
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
      //that.recursionFunc(row, this.categoryData, x);
      //that.dialog.data.categoryId = x;

      if (flag === 'edit') {
        that.dialog.title = '编辑知识库管理';
      }
    },
    // 设置父级菜单默认显示 递归
    recursionFunc(row, source, dest) {
      if (row.categoryId === null) {
        return;
      }
      for (let i in source) {
        let ele = source[i];
        if (row.categoryId === ele.id) {
          this.recursionFunc(ele, this.categoryData, dest);
          dest.push(ele.id);
        }
        else {
          this.recursionFunc(row, ele.children, dest);
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
            EmbeddingModelId: that.dialog.data.embeddingModelId,
            VectorDBId: that.dialog.data.vectorDBId,
            ChatModelId: that.dialog.data.chatModelId,
            Name: that.dialog.data.name
          };
          console.log('add-' + JSON.stringify(data));
          service.post("/Admin/KnowledgeBases/Edit?handler=Save", data).then(res => {
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
      service.post("/Admin/KnowledgeBases/edit?handler=Delete", ids).then(res => {
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
    }
  }
}); 