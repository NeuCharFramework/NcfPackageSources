var app = new Vue({
  el: "#app",
  data() {
    return {
      page: {
        page: 1,
        size: 10
      },
      tableLoading: true,
      tableData: [],
      addFormDialogVisible: false,
      addForm: {
        alias: "",
        "vectorId": "",
        "name": "",
        "connectionString": "",
        "vectorDBType": '0',
        "note": "",
      },
      editFormDialogVisible: false,
      editForm: {
        alias: "",
        "vectorId": "",
        "name": "",
        "connectionString": "",
        "vectorDBType": '0',
        "note": "",
        "show": true
      },
      total: 0,
      addRules: {
        alias: [
          { required: true, message: ncfT('AIKernel.Vector.AliasRequired'), trigger: 'change' }
        ],
        vectorDBType: [
          { required: true, message: ncfT('AIKernel.Vector.TypeRequired'), trigger: 'change' }
        ],
        vectorId: [
          { required: true, message: ncfT('AIKernel.Vector.ModelNameRequired'), trigger: 'blur' }
        ],
        name: [
          { required: true, message: ncfT('AIKernel.Vector.NameRequired'), trigger: 'blur' }
        ],
        connectionString: []
      },
      editRules: {
        alias: [
          { required: true, message: ncfT('AIKernel.Vector.AliasRequired'), trigger: 'change' }
        ],
        vectorDBType: [
          { required: true, message: ncfT('AIKernel.Vector.TypeRequired'), trigger: 'change' }
        ],
        vectorId: [
          { required: true, message: ncfT('AIKernel.Vector.ModelNameRequired'), trigger: 'blur' }
        ],
        name: [
          { required: true, message: ncfT('AIKernel.Vector.NameRequired'), trigger: 'blur' }
        ],
        connectionString: []
      }
    }
  },
  mounted() {
    //wait page load  
    setTimeout(async () => {
      await this.init();
    }, 100)
  },
  methods: {
    isInMemoryVectorType(vectorTypeRaw) {
      const code = Number(vectorTypeRaw);
      return code === 0 || code === 17;
    },
    ensureConnectionStringRequirement(formData) {
      if (!formData) {
        return false;
      }

      const connectionString = (formData.connectionString || '').trim();
      formData.connectionString = connectionString;

      if (this.isInMemoryVectorType(formData.vectorDBType)) {
        return true;
      }

      if (!connectionString) {
        this.$message.warning(ncfT('AIKernel.Vector.ConnectionRequired'));
        return false;
      }
      return true;
    },
    async init() {
      await this.getDataList();
    },
    async handleSizeChange(val) {
      this.page.size = val;
      await this.getDataList();
    },
    async handleCurrentChange(val) {
      this.page.page = val;
      await this.getDataList();
    },
    async getDataList() {
      this.tableLoading = true
      await service.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.GetPagedListAsync', {
        "page": this.page.page,
        "size": this.page.size,
      })
        .then(res => {
          console.log(res)
          this.tableData = res.data.data.data;
          this.total = res.data.data.total;
          this.tableLoading = false
        })
    },
    addVector() {
      this.addFormDialogVisible = true;
    },
    addNeuCharModel() {
      this.neuCharFormDialogVisible = true; // 显示对话框  
    },
    async copyInfo(key) {
      let copied = false
      try {
        if (window.isSecureContext && navigator.clipboard && navigator.clipboard.writeText) {
          await navigator.clipboard.writeText(key)
          copied = true
        }
      } catch (error) {
        console.warn('Clipboard API is unavailable, using the compatibility fallback.', error)
      }

      if (!copied) {
        const input = document.createElement('textarea')
        input.setAttribute('readonly', 'readonly')
        input.value = key
        input.style.position = 'fixed'
        input.style.opacity = '0'
        document.body.appendChild(input)
        input.select()
        input.setSelectionRange(0, key.length)
        try {
          copied = document.execCommand('copy')
        } finally {
          input.remove()
        }
      }

      if (copied) {
        this.$message.success(ncfT('AIKernel.Vector.CopySuccess', key.slice(-4)))
      } else {
        this.$message.error(ncfT('AIKernel.Vector.CopyFailed'))
      }
    },
    async addModelSubmit() {
      this.$refs.addForm.validate(async (valid) => {
        if (valid) {
          this.addForm.vectorDBType = parseInt(this.addForm.vectorDBType)
          if (!this.ensureConnectionStringRequirement(this.addForm)) {
            return false;
          }
          await service.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.CreateAsync', {
            ...this.addForm
          }
          ).then(res => {
            this.$message({
              type: res.data.success ? 'success' : 'error',
              message: res.data.success ? ncfT('AIKernel.Vector.AddSuccess') : ncfT('AIKernel.Vector.AddFailed')
            });
            if (res.data.success) {
              this.getDataList()
              this.clearAddForm()
              this.addFormDialogVisible = false;
            }
          })
        } else {
          return false;
        }
      });
    },
    clearAddForm() {
      this.addForm = {
        alias: "",
        "vectorId": "",
        "name": "",
        "connectionString": "",
        "vectorDBType": '0',
        "note": "",
      }
    },
    clearEditForm() {
      this.editForm = {
        alias: "",
        "vectorId": "",
        "name": "",
        "connectionString": "",
        "vectorDBType": '0',
        "note": "",
        "show": true
      }
    },
    async editModelSubmit() {
      this.$refs.editForm.validate(async (valid) => {
        if (valid) {
          this.editForm.vectorDBType = parseInt(this.editForm.vectorDBType)
          if (!this.ensureConnectionStringRequirement(this.editForm)) {
            return false;
          }
          // clear empty value  
          for (const key in this.editForm) {
            if (this.editForm.hasOwnProperty(key)) {
              const element = this.editForm[key];
              if (element === null || element === undefined) {
                delete this.editForm[key]
              }
            }
          }

          await service.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.EditAsync', {
            ...this.editForm
          }).then(res => {
            this.$message({
              type: res.data.success ? 'success' : 'error',
              message: res.data.success ? ncfT('AIKernel.Vector.EditSuccess') : ncfT('AIKernel.Vector.EditFailed')
            });
            if (res.data.success) {
              this.clearEditForm()
              this.getDataList()
              this.editFormDialogVisible = false;
            }
          })
        } else {
          return false;
        }
      });
    },
    dateformatter(date) {
      return new Date(date).toLocaleString()
    },
    editModel(row) {
      this.editFormDialogVisible = true;
      this.editForm = {
        ...row,
        vectorDBType: row.vectorDBType.toString()
      };
    },
    deleteModel(row) {
      this.$confirm(ncfT('AIKernel.Vector.DeleteConfirm', row.alias), ncfT('AIKernel.Vector.DeleteConfirmTitle'), {
        confirmButtonText: ncfT('AIKernel.Vector.Confirm'),
        cancelButtonText: ncfT('AIKernel.Vector.Cancel'),
        type: 'warning'
      }).then(async () => {
        await service.delete('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.DeleteAsync', {
          params: {
            id: row.id
          }
        }).then(async res => {
          this.$message({
            type: res.data.success ? 'success' : 'error',
            message: res.data.success ? ncfT('AIKernel.Vector.DeleteSuccess') : ncfT('AIKernel.Vector.DeleteFailed')
          });
          await this.getDataList().then(() => {
            if (this.tableData.length === 0 && this.page.page > 1) {
              this.page.page--;
              this.getDataList();
            }
          })
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: ncfT('AIKernel.Vector.CancelDelete')
        });
      });
    },
  },
});  
