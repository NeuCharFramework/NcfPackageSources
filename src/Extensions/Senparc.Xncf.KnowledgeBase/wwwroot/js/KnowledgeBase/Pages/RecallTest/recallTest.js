(function () {
  const knowledgeBaseListUrl = '/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=1&pageSize=200&keyword=&orderField=AddTime%20Desc';
  const recallUrl = '/api/Senparc.Xncf.KnowledgeBase/RecallTestAppService/Xncf.KnowledgeBase_RecallTestAppService.RecallTest';

  function unwrap(response) {
    const body = response && response.data;
    return body && Object.prototype.hasOwnProperty.call(body, 'data') ? body.data : body;
  }

  function errorMessage(error) {
    return ncfST('Frontend.278');
  }

  new Vue({
    el: '#app',
    data: function () {
      return {
        knowledgeBaseList: [],
        selectedKnowledgeBaseId: null,
        recallContent: '',
        topK: 5,
        recallLoading: false,
        recallResults: [],
        lastElapsedMilliseconds: null,
        recordList: [],
        recordPage: 1,
        recordPageSize: 5,
        paragraphDetailItem: null,
        visible: {
          searchSettingsDrawer: false,
          paragraphDetailDialog: false
        }
      };
    },
    computed: {
      selectedKnowledgeBase: function () {
        return this.knowledgeBaseList.find(item => Number(item.id) === Number(this.selectedKnowledgeBaseId)) || null;
      },
      recordTotal: function () { return this.recordList.length; },
      recordPageList: function () {
        const start = (this.recordPage - 1) * this.recordPageSize;
        return this.recordList.slice(start, start + this.recordPageSize);
      }
    },
    watch: {
      recordList: function () {
        const maxPage = Math.max(1, Math.ceil(this.recordTotal / this.recordPageSize));
        if (this.recordPage > maxPage) this.recordPage = maxPage;
      }
    },
    created: function () {
      this.loadKnowledgeBaseList();
    },
    methods: {
      loadKnowledgeBaseList: async function () {
        try {
          const payload = unwrap(await service.get(knowledgeBaseListUrl)) || {};
          const list = Array.isArray(payload.list) ? payload.list : [];
          this.knowledgeBaseList = list.map(function (item) {
            const ready = !!(item.vectorCollectionName && item.embeddedTime);
            return {
              id: item.id,
              name: item.name || (ncfST('Frontend.234') + '-' + item.id),
              ready: ready,
              label: (item.name || (ncfST('Frontend.234') + '-' + item.id)) + (ready ? '' : '（' + ncfST('Frontend.220') + '）')
            };
          });
        } catch (error) {
          this.$message.error(ncfST('Frontend.279', errorMessage(error)));
        }
      },
      formatScore: function (score) {
        const value = Number(score);
        return Number.isFinite(value) ? value.toFixed(4) : '—';
      },
      handleRecordSizeChange: function (value) {
        this.recordPageSize = value;
        this.recordPage = 1;
      },
      handleRecordPageChange: function (value) {
        this.recordPage = value;
      },
      openParagraphDetail: function (item) {
        this.paragraphDetailItem = item;
        this.visible.paragraphDetailDialog = true;
      },
      doRecall: async function () {
        const knowledgeBase = this.selectedKnowledgeBase;
        const content = (this.recallContent || '').trim();
        if (!knowledgeBase) {
          this.$message.warning(ncfST('Frontend.103'));
          return;
        }
        if (!knowledgeBase.ready) {
          this.$message.warning(ncfST('Frontend.280'));
          return;
        }
        if (!content) {
          this.$message.warning(ncfST('Frontend.281'));
          return;
        }

        this.recallLoading = true;
        this.recallResults = [];
        this.lastElapsedMilliseconds = null;
        try {
          const response = await service.post(recallUrl, {
            id: Number(knowledgeBase.id),
            content: content,
            topK: this.topK
          });
          const results = unwrap(response);
          if (!Array.isArray(results)) throw new Error(ncfST('Frontend.282'));

          this.recallResults = results.map(function (item, index) {
            return Object.assign({}, item, {
              rank: item.rank || index + 1,
              content: item.content || '',
              sourceName: item.sourceName || '',
              sourceLink: item.sourceLink || ''
            });
          });
          this.lastElapsedMilliseconds = this.recallResults.length ? this.recallResults[0].elapsedMilliseconds : null;
          const scores = this.recallResults.map(item => Number(item.score)).filter(Number.isFinite);
          this.recordList.unshift({
            queryContent: content,
            knowledgeBaseName: knowledgeBase.name,
            resultCount: this.recallResults.length,
            highestScore: scores.length ? Math.max.apply(null, scores) : null,
            time: new Date().toLocaleString(document.documentElement.lang || undefined, { hour12: false })
          });
          this.recordPage = 1;
          this.$message.success(this.recallResults.length ? ncfST('Frontend.283') : ncfST('Frontend.284'));
        } catch (error) {
          this.$notify({ title: ncfST('Frontend.285'), message: errorMessage(error), type: 'error', duration: 6000 });
        } finally {
          this.recallLoading = false;
        }
      }
    }
  });
})();
