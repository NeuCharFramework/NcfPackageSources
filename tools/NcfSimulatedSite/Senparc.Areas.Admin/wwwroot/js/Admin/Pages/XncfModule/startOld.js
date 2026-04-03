var app = new Vue({
    el: "#app",
    data() {
        return {
            data: [], // data
            tooltip: {
                "IAreaRegister": '网页',
                "IXncfDatabase": '数据库',
                "IXncfMiddleware": '中间件',
                "IXncfRazorRuntimeCompilation": '线程'
            },
            state: {
                'String': '文本',
                'Int32': '数字',
                'Int64': '数字',
                'DateTime': '日期',
                'String[]': '选项'
            },
            xNcfModules_State: {
                0: '关闭',
                1: '开放',
                2: '新增待审核',
                3: '更新待审核'
            },
            // Execute pop-up window
            run: {
                data: {},
                visible: false
            },
            runData: {
                // Bind data
            },
            runResult: {
                visible: false,
                tit: '',
                tip: '',
                msg: '',
                tempId: '',
                hasLog: false
            },
            //View thread 
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


        // Open home page
        openUrl(url, flag) {
            // Return to closed state
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: '提示',
                    message: '请开启后执行',
                    type: 'warning'
                });
                return;
            }
            window.location.href = url;
        },
        // open execution
        openRun(item, flag) {
            // Return to closed state
            flag = flag + '';
            if (flag !== '1') {
                this.$notify({
                    title: '提示',
                    message: '请开启后执行',
                    type: 'warning'
                });
                return;
            }
            this.run.data = item;
            this.runData = {};
            this.run.data.value.map(res => {
                // Dynamic model binding generation
                // Default selection assignment
                // Multiple choice
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
                // drop-down box value
                if (res.parameterType === 1 && res.selectionList.items) {
                    this.runData[res.name] = {};
                    this.runData[res.name].value = '';
                    this.runData[res.name].item = res;
                    res.selectionList.items.map(ele => {
                        if (ele.defaultSelected) {
                            this.runData[res.name].value = ele.value;
                        }
                    });
                    // If not, default to the first one
                    if (this.runData[res.name].value.length === 0) {
                        this.runData[res.name].value = res.selectionList.items[0].value;
                    }
                }
                // Input box
                if (res.parameterType === 0) {
                    this.runData[res.name] = {};
                    this.runData[res.name].item = res;
                    this.runData[res.name].value = res.value || '';
                }
            });
            this.runData = Object.assign({}, this.runData);
            //  this.runData array structure
            //When transmitting through the interface, convert the drop-down radio selection into an array
            //{
            //   // parameterType === 2 multiple selection
            //    Modules: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 1 drop-down radio selection
            //    ReferenceType: {
            //        item: {},
            //        value: []
            //    },
            //   // parameterType === 0 input
            //    SourcePath: {
            //        item: {},
            //        value: ''
            //    }
            //};
            this.run.visible = true;
        },
        // implement
        async handleRun() {
            // Physical path verification
            if (this.runData.hasOwnProperty('SourcePath') && this.runData.SourcePath.length < 1) {
                this.$notify({
                    title: '警告',
                    message: '请填写源码物理路径',
                    type: 'warning'
                });
                return;
            }
            // Close the execution pop-up window
            // this.run.visible = false;
            let xncfFunctionParams = {};
            for (var i in this.runData) {
                // Multiple choice
                if (this.runData[i].item.parameterType === 2) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必选项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = {};
                        xncfFunctionParams[i].SelectedValues = [];
                        xncfFunctionParams[i].SelectedValues = this.runData[i].value;

                    }
                }
                // The value of the drop-down box is a string, but the interface requires an array.
                if (this.runData[i].item.parameterType === 1) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必填项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = {};
                        xncfFunctionParams[i].SelectedValues = [];
                        xncfFunctionParams[i].SelectedValues[0] = this.runData[i].value;
                    }
                }
                // Input box
                if (this.runData[i].item.parameterType === 0) {
                    if (this.runData[i].item.isRequired && this.runData[i].value.length === 0) {
                        this.$notify({
                            title: '提示',
                            message: this.runData[i].item.title + '  为必填项',
                            type: 'warning'
                        });
                        return;
                    } else {
                        xncfFunctionParams[i] = this.runData[i].value;
                    }
                }
            }
            const data = {
                xncfUid: this.data.xncfModule.uid, xncfFunctionName: this.run.data.key.name, xncfFunctionParams: JSON.stringify(xncfFunctionParams)
            };
            const res = await service.post(`/Admin/XncfModule/Start?handler=RunFunction`, data);
            this.runResult.tempId = res.data.tempId;
            if ((res.data.log || '').length > 0 && (res.data.tempId || '').length > 0) {
                this.runResult.hasLog = true;
            }
            if (!res.data.success) {
                this.runResult.tit = '遇到错误';
                this.runResult.tip = '错误信息';
                this.runResult.msg = res.data.msg;
                return;
            }
            if (res.data.msg && (res.data.msg.indexOf('http://') !== -1 || res.data.msg.indexOf('https://') !== -1)) {
                this.runResult.tit = '执行成功';
                this.runResult.tip = '收到网址，点击下方打开<br />（此链接由第三方提供，请注意安全）：';
                this.runResult.msg = '<i class="fa fa-external-link"></i> <a href="' + res.data.msg + '" target="_blank">' + res.data.msg + '</a>';
            }
            else {
                this.runResult.tit = '执行成功';
                this.runResult.tip = '返回信息';
                this.runResult.msg = res.data.msg;
            }
            // Open the execution result pop-up window
            this.runResult.visible = true;
            this.getList();
        },
        // off and on
        async updataState(state) {
            const id = this.data.xncfModule.id;
            const res = await service.get(`/Admin/XncfModule/Start?handler=ChangeState&id=${id}&tostate=${state}`);
            window.location.reload();
        },
        // Updated version
        async  updataVersion() {
            const uid = resizeUrl().uid;
            await service.get(`/Admin/XncfModule/Index?handler=ScanAjax&uid=${uid}`);
            window.location.reload();
        },
        // delete
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
