var ncfI18n = window.ncfI18n || {};

var app = new Vue({
    el: "#app",
    data() {
        return {
            data: [], // Data
            tooltip: {
                "IAreaRegister": ncfI18n.tooltipWebpage || 'Webpage',
                "IXncfDatabase": ncfI18n.tooltipDatabase || 'Database',
                "IXncfMiddleware": ncfI18n.tooltipMiddleware || 'Middleware',
                "IXncfRazorRuntimeCompilation": ncfI18n.tooltipThread || 'Thread'
            },
            state: {
                'String': ncfI18n.stateText || 'Text',
                'Int32': ncfI18n.stateNumber || 'Number',
                'Int64': ncfI18n.stateNumber || 'Number',
                'DateTime': ncfI18n.stateDate || 'Date',
                'String[]': ncfI18n.stateOptions || 'Options'
            },
            xNcfModules_State: {
                0: ncfI18n.moduleStateClosed || 'Closed',
                1: ncfI18n.moduleStateOpen || 'Open',
                2: ncfI18n.moduleStateNewPending || 'New - Pending Review',
                3: ncfI18n.moduleStateUpdatePending || 'Update - Pending Review'
            },
            // Execution dialog
            run: {
                data: {},
                visible: false,
                loading: false
            },
            runData: {
                // Bound data
            },
            runResult: {
                visible: false,
                tit: '',
                tip: '',
                msg: '',
                tempId: '',
                hasLog: false
            },
            // View thread
            thread: {
                visible: false
            }
        };
    },
    created() {
        this.getList();
    },
    methods: {
        async  getList() {
            const uid = resizeUrl().uid;
            const res = await service.get(`/Admin/XncfModule/Start?handler=Detail&uid=${uid}`);
            this.data = res.data.data;
            this.data.xncfRegister.interfaces = this.data.xncfRegister.interfaces.splice(1);
            window.document.title = this.data.xncfModule.menuName;
        },


        // Open homepage
        openUrl(url, flag) {
            // Return if closed
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfI18n.notice || 'Notice',
                    message: ncfI18n.pleaseEnableFirst || 'Please enable before executing',
                    type: 'warning'
                });
                return;
            }
            window.location.href = url;
        },
        // Open execution
        openRun(item, flag) {
            // Return if closed
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: ncfI18n.notice || 'Notice',
                    message: ncfI18n.pleaseEnableFirst || 'Please enable before executing',
                    type: 'warning'
                });
                return;
            }
            this.run.data = item;
            this.runData = {};
            this.run.data.value.map(res => {
                // Dynamic model binding generation
                // Default selection assignment
                // Multi-select
                if (res.parameterType === 2 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = [];
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected) {
                            this.runData[res.name].value.push(ele.value);
                        }
                    });
                }
                // Dropdown value
                if (res.parameterType === 1 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = '';
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected) {
                            this.runData[res.name].value = ele.value;
                        }
                    });
                    // If no default, use the first one
                    if (this.runData[res.name].value.length === 0) {
                        this.runData[res.name].value = res.selectionList.items[0].value;
                    }
                }
                // Input field
                if (res.parameterType === 0 || res.parameterType === 3) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value || '';
                }
            });
            this.runData = Object.assign({}, this.runData);
            //  runData array structure
            //  When sending to API, convert dropdown single-select to array
            //{
            //   // parameterType === 2 Multi-select
            //    Modules: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 1 Dropdown single-select
            //    ReferenceType: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 0 Input
            //    SourcePath: {
            //        item: {},
            //        value: ''
            //    }
            //};
            this.run.visible = true;
        },
        // Execute
        async handleRun() {
            // Physical path validation
            if (this.runData.hasOwnProperty('SourcePath') && this.runData.SourcePath.length < 1) {
                this.$notify({
                    title: ncfI18n.warning || 'Warning',
                    message: ncfI18n.pleaseEnterSourcePath || 'Please enter the source code physical path',
                    type: 'warning'
                });
                return;
            }

            // Set loading state
            this.run.loading = true;

            try {
                let xncfFunctionParams = {};
                for (var i in this.runData) {
                    // Multi-select
                    if (this.runData[i].item.parameterType === 2) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfI18n.notice || 'Notice',
                                message: this.runData[i].item.title + '  ' + (ncfI18n.isRequired || 'is required'),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = {};
                            xncfFunctionParams[i].SelectedValues = [];
                            xncfFunctionParams[i].SelectedValues = this.runData[i].value;

                        }
                    }
                    // Dropdown value is string but API expects array
                    if (this.runData[i].item.parameterType === 1) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfI18n.notice || 'Notice',
                                message: this.runData[i].item.title + '  ' + (ncfI18n.isRequired || 'is required'),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = {};
                            xncfFunctionParams[i].SelectedValues = [];
                            xncfFunctionParams[i].SelectedValues[0] = this.runData[i].value;
                        }
                    }
                    // Input field
                    if (this.runData[i].item.parameterType === 0 || this.runData[i].item.parameterType === 3) {
                        if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                            this.$notify({
                                title: ncfI18n.notice || 'Notice',
                                message: this.runData[i].item.title + '  ' + (ncfI18n.isRequired || 'is required'),
                                type: 'warning'
                            });
                            return;
                        } else {
                            xncfFunctionParams[i] = this.runData[i].value;
                        }
                    }
                }
                const data = {
                    xncfUid: this.data.xncfModule.uid,
                    xncfFunctionName: this.run.data.key.name,
                    xncfFunctionParams: JSON.stringify(xncfFunctionParams)
                };

                const res = await service.post(`/Admin/XncfModule/Start?handler=RunFunction`, data, { customAlert: true });

                this.runResult.tempId = res.data.tempId;
                if ((res.data.log || '').length > 0 && (res.data.tempId || '').length > 0) {
                    this.runResult.hasLog = true;
                }

                const msg = DOMPurify.sanitize(res.data.msg);

                if (!res.data.success) {
                    this.runResult.tit = ncfI18n.errorEncountered || 'Error encountered';
                    this.runResult.tip = ncfI18n.errorInfo || 'Error info';
                    this.runResult.msg = (msg || DOMPurify.sanitize(res.data.exception)).replace(/&lt;br \/&gt;/g, '<br />').replace('\r\n', '<br />').replace('\n', '<br />').replace('\r', '<br />');
                    this.runResult.visible = true;
                    return;
                }
                if (msg && (msg.indexOf('http://') === 0 || msg.indexOf('https://') === 0)) {
                    this.runResult.tit = ncfI18n.executionSuccess || 'Execution successful';
                    this.runResult.tip = ncfI18n.receivedUrlClickBelow || 'Received a URL, click below to open<br />(This link is provided by a third party, please be careful):';
                    this.runResult.msg = '<i class="fa fa-external-link"></i> <a href="' + msg + '" target="_blank">' + msg + '</a>';
                }
                else {
                    this.runResult.tit = ncfI18n.executionSuccess || 'Execution successful';
                    this.runResult.tip = ncfI18n.returnInfo || 'Return info';
                    this.runResult.msg = msg.replace(/&lt;br \/&gt;/g, '<br />').replace('\r\n', '<br />').replace('\n', '<br />').replace('\r','<br />');
                }
                // Open execution result dialog
                this.runResult.visible = true;
                this.getList();
            } catch (error) {
                console.error('Execution error:', error);
                this.$notify({
                    title: ncfI18n.error || 'Error',
                    message: ncfI18n.executionError || 'An error occurred during execution',
                    type: 'error'
                });
            } finally {
                // Cancel loading state regardless of success or failure
                this.run.loading = false;
            }
        },
        // Close and open
        async updataState(state) {
            const id = this.data.xncfModule.id;
            const res = await service.get(`/Admin/XncfModule/Start?handler=ChangeState&id=${id}&tostate=${state}`);
            window.location.reload();
        },
        // Update version
        async  updataVersion() {
            const uid = resizeUrl().uid;
            await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${uid}`);
            window.location.reload();
        },
        // Delete
        async handleDelete() {
            const id = this.data.xncfModule.id;
            const res = await service.post(`/Admin/XncfModule/Start?handler=Delete&id=${id}`);
            window.sessionStorage.setItem('setNavMenuActive', '模块管理');
            getNavMenu();
            setTimeout(function () {
                window.location.href = '/Admin/XncfModule/Index';
            }, 100);
        }
    }
});
