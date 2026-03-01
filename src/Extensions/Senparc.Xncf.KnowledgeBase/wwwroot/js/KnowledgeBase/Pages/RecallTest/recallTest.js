new Vue({
  el: "#app",
  data() {
    return {
      knowledgeBaseList: [],
      selectedKnowledgeBaseId: null,
      recallContent: '',
      score: '',
      attributeList: [{ id: 1, name: '类型' }, { id: 2, name: '来源' }],
      tagList: [{ id: 1, name: '重要' }, { id: 2, name: '待审核' }],
      selectedAttrs: [],
      selectedTags: [],
      visible: {
        attrDialog: false,
        attrForm: false,
        tagDialog: false,
        tagForm: false,
        searchSettingsDrawer: false,
        paragraphDetailDialog: false
      },
      paragraphDetailItem: null,
      topK: 5,
      attrForm: { id: null, name: '' },
      tagForm: { id: null, name: '' },
      recallLoading: false,
      recallResults: [],
      recordList: [
        { queryContent: '演示', score:'0', dataSource: 'Retrieval Test', time: '2026-03-01 17:13' },
      ],
      recordPage: 1,
      recordPageSize: 5,
      recordTableHeight: 220,
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
    },
    paragraphDetailChunkName() {
      if (!this.paragraphDetailItem) return '';
      var idx = this.paragraphDetailItem._index != null ? this.paragraphDetailItem._index + 1 : 1;
      return this.paragraphDetailItem.chunkName || ('分段-' + String(idx).padStart(2, '0'));
    },
    paragraphDetailTags() {
      return (this.paragraphDetailItem && this.paragraphDetailItem.tags) ? this.paragraphDetailItem.tags : [];
    },
    recordTotal() {
      return this.recordList.length;
    },
    recordPageList() {
      var start = (this.recordPage - 1) * this.recordPageSize;
      return this.recordList.slice(start, start + this.recordPageSize);
    }
  },
  watch: {
    recordList() {
      var maxPage = Math.max(1, Math.ceil(this.recordTotal / this.recordPageSize));
      if (this.recordPage > maxPage) this.recordPage = maxPage;
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
    openParagraphDetail(item, index) {
      this.paragraphDetailItem = { ...item, _index: index };
      this.visible.paragraphDetailDialog = true;
    },
    handleRecordSizeChange(val) {
      this.recordPageSize = val;
      this.recordPage = 1;
    },
    handleRecordPageChange(val) {
      this.recordPage = val;
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
        var recordScore = '';
        if (body && Array.isArray(body) && body.length > 0) {
          recordScore = body[0].score != null && body[0].score !== '' ? String(body[0].score) : (body[0].Score != null && body[0].Score !== '' ? String(body[0].Score) : '');
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
              score: b.score != null ? b.score : (b.Score != null ? b.Score : ''),
              recallTime: b.recallTime
            };
          });
          var kbName = (that.knowledgeBaseList.find(function (k) { return k.id === kbId; }) || {}).name || 'Retrieval Test';
          that.recordList.unshift({ queryContent: that.recallContent, score: recordScore, dataSource: kbName, time: timeStr });
        } else {
          that.recallResults = [];
          var kbName = (that.knowledgeBaseList.find(function (k) { return k.id === kbId; }) || {}).name || 'Retrieval Test';
          that.recordList.unshift({ queryContent: that.recallContent, score: recordScore, dataSource: kbName, time: timeStr });
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
