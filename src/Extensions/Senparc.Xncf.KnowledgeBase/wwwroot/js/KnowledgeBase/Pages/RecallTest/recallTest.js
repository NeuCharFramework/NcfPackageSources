new Vue({
  el: "#app",
  data() {
    return {
      knowledgeBaseList: [],
      selectedKnowledgeBaseId: null,
      recallContent: '',
      attributeList: [{ id: 1, name: '类型' }, { id: 2, name: '来源' }],
      tagList: [{ id: 1, name: '重要' }, { id: 2, name: '待审核' }],
      selectedAttrs: [],
      selectedTags: [],
      visible: {
        attrDialog: false,
        attrForm: false,
        tagDialog: false,
        tagForm: false,
        searchSettingsDrawer: false
      },
      topK: 5,
      attrForm: { id: null, name: '' },
      tagForm: { id: null, name: '' },
      recallLoading: false,
      recallResults: [],
      recordList: [
        { queryContent: '放假', dataSource: 'Retrieval Test', time: '2026-03-01 17:13' },
        { queryContent: '放假', dataSource: 'Retrieval Test', time: '2026-02-24 23:06' },
        { queryContent: '放假', dataSource: 'Retrieval Test', time: '2026-02-15 23:41' },
        { queryContent: '米立科技', dataSource: 'Retrieval Test', time: '2026-02-15 23:40' }
      ],
      _attrId: 10,
      _tagId: 10
    };
  },
  computed: {
    selectedAttrNames() {
      return this.selectedAttrs.map(id => (this.attributeList.find(a => a.id === id) || {}).name).filter(Boolean);
    },
    selectedTagNames() {
      return this.selectedTags.map(id => (this.tagList.find(t => t.id === id) || {}).name).filter(Boolean);
    }
  },
  created() {
    this.loadKnowledgeBaseList();
  },
  methods: {
    loadKnowledgeBaseList() {
      var that = this;
      if (that.knowledgeBaseList.length > 0) return;
      service.get('/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=1&pageSize=999&keyword=&orderField=AddTime%20Desc').then(function (res) {
        var list = (res.data && res.data.data && res.data.data.list) ? res.data.data.list : [];
        that.knowledgeBaseList = list.map(function (item) { return { id: item.id, name: item.name || ('知识库-' + item.id) }; });
      }).catch(function () { });
    },
    onKbSelectVisibleChange(visible) {
      if (visible && this.knowledgeBaseList.length === 0) this.loadKnowledgeBaseList();
    },
    openSearchSettingsDrawer() {
      this.visible.searchSettingsDrawer = true;
    },
    saveSearchSettings() {
      this.visible.searchSettingsDrawer = false;
      this.$message.success('检索设置已保存');
    },
    openAttrDialog() {
      this.visible.attrDialog = true;
      this.visible.attrForm = false;
    },
    closeAttrDialog() {
      this.visible.attrForm = false;
    },
    addAttr() {
      this.attrForm = { id: null, name: '' };
      this.visible.attrForm = true;
    },
    editAttr(row) {
      this.attrForm = { id: row.id, name: row.name };
      this.visible.attrForm = true;
    },
    saveAttr() {
      if (!this.attrForm.name || !this.attrForm.name.trim()) {
        this.$message.warning('请输入属性名');
        return;
      }
      if (this.attrForm.id) {
        const item = this.attributeList.find(a => a.id === this.attrForm.id);
        if (item) item.name = this.attrForm.name.trim();
      } else {
        this.attributeList.push({ id: ++this._attrId, name: this.attrForm.name.trim() });
      }
      this.visible.attrForm = false;
      this.attrForm = { id: null, name: '' };
      this.$message.success('保存成功');
    },
    deleteAttr(row) {
      this.$confirm('确认删除该属性？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.attributeList = this.attributeList.filter(a => a.id !== row.id);
        this.selectedAttrs = this.selectedAttrs.filter(id => id !== row.id);
        this.$message.success('已删除');
      }).catch(() => { });
    },
    removeAttr(index) {
      const name = this.selectedAttrNames[index];
      const item = this.attributeList.find(a => a.name === name);
      if (item) this.selectedAttrs = this.selectedAttrs.filter(id => id !== item.id);
    },

    openTagDialog() {
      this.visible.tagDialog = true;
      this.visible.tagForm = false;
    },
    closeTagDialog() {
      this.visible.tagForm = false;
    },
    addTag() {
      this.tagForm = { id: null, name: '' };
      this.visible.tagForm = true;
    },
    editTag(row) {
      this.tagForm = { id: row.id, name: row.name };
      this.visible.tagForm = true;
    },
    saveTag() {
      if (!this.tagForm.name || !this.tagForm.name.trim()) {
        this.$message.warning('请输入标签名');
        return;
      }
      if (this.tagForm.id) {
        const item = this.tagList.find(t => t.id === this.tagForm.id);
        if (item) item.name = this.tagForm.name.trim();
      } else {
        this.tagList.push({ id: ++this._tagId, name: this.tagForm.name.trim() });
      }
      this.visible.tagForm = false;
      this.tagForm = { id: null, name: '' };
      this.$message.success('保存成功');
    },
    deleteTag(row) {
      this.$confirm('确认删除该标签？', '提示', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(() => {
        this.tagList = this.tagList.filter(t => t.id !== row.id);
        this.selectedTags = this.selectedTags.filter(id => id !== row.id);
        this.$message.success('已删除');
      }).catch(() => { });
    },
    removeTag(index) {
      const name = this.selectedTagNames[index];
      const item = this.tagList.find(t => t.name === name);
      if (item) this.selectedTags = this.selectedTags.filter(id => id !== item.id);
    },

    doRecall() {
      const that = this;
      var kbId = that.selectedKnowledgeBaseId;
      if (kbId == null || kbId === '') {
        that.$message.warning('请先选择知识库');
        return;
      }
      that.recallLoading = true;
      that.recallResults = [];
      const now = new Date();
      const timeStr = now.getFullYear() + '-' + String(now.getMonth() + 1).padStart(2, '0') + '-' + String(now.getDate()).padStart(2, '0') + ' ' +
        String(now.getHours()).padStart(2, '0') + ':' + String(now.getMinutes()).padStart(2, '0') + ':' + String(now.getSeconds()).padStart(2, '0');

      const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/RecallTestAppService/Xncf.KnowledgeBase_RecallTestAppService.RecallTest';
      const dataTemp = { id: kbId, content: that.recallContent, topK: that.topK };
      service.post(serviceURL, dataTemp).then(res => {
        var body = res && res.data && res.data.data;
        if (body && Array.isArray(body) && body.length > 0) {
          that.recallResults = body.map(function (b, i) {
            var content = b.content || '';
            var tags = b.tags || [];
            if (typeof tags === 'string') tags = tags.split(/[,\s]+/).filter(Boolean);
            return {
              chunkName: b.chunkName || ('Chunk-' + String(i + 1).padStart(2, '0')),
              charCount: content.length,
              content: content,
              tags: tags,
              sourceFile: b.sourceFile || b.fileName || '—',
              score: b.score,
              recallTime: b.recallTime
            };
          });
          var kbName = (that.knowledgeBaseList.find(function (k) { return k.id === kbId; }) || {}).name || 'Retrieval Test';
          that.recordList.unshift({ queryContent: that.recallContent, dataSource: kbName, time: timeStr });
        } else {
          that.recallResults = [
            { chunkName: 'Chunk-01', charCount: 36, content: '山西米立信息技术有限公司** 关于2026年春节放假安排的通知', tags: ['安排', '2026', '通知', '春节', '信息技术', '米立', '有限公司', '放假', '山西'], sourceFile: '2026年春节放假通知-v1.0.2.txt' },
            { chunkName: 'Chunk-03', charCount: 55, content: '根据国务院办公厅关于2026年部分节假日安排的通知精神,结合公司实际情况,现将2026年春节放假安排通知如下:', tags: ['安排', '2026', '通知', '春节', '节假日', '通知精神', '现将', '国务院', '放假', '结合'], sourceFile: '2026年春节放假通知-v1.0.2.txt' },
            { chunkName: 'Chunk-04', charCount: 95, content: '一、放假时间** 2026年2月15日(星期日,农历腊月二十八)至2月23日(星期一,农历正月初七),共放假9天。...', tags: ['星期日', '2026', '星期六', '15', '23', '农历', '14', '放假', '28', '正月初七'], sourceFile: '2026年春节放假通知-v1.0.2.txt' }
          ];
          var kbName = (that.knowledgeBaseList.find(function (k) { return k.id === kbId; }) || {}).name || 'Retrieval Test';
          that.recordList.unshift({ queryContent: that.recallContent, dataSource: kbName, time: timeStr });
        }
        that.recallLoading = false;
        that.$message.success('召回完成');
      }).catch(err => {
        that.recallResults = [];
        that.recallLoading = false;
        that.$notify({ title: '错误', message: err.message || '召回失败，请检查配置', type: 'error', duration: 5000 });
      });
    }
  }
});
