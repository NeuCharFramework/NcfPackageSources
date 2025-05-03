var validatePass = (rule, value, callback) => {
    if (value === '') {
        callback(new Error('请输入密码'));
    } else {
        callback();
    }
};
var validateUser = (rule, value, callback) => {
    if (value === '') {
        callback(new Error('请输入用户名'));
    } else {
        callback();
    }
};
var app = new Vue({
    el: '#app',
    data: {
        ruleForm: {
            user: '',
            pass: '',
            tenant: ''
        },
        enableMultiTenant: false,
        rules: {
            user: [
                { validator: validateUser, trigger: 'blur' }
            ],
            pass: [
                { validator: validatePass, trigger: 'blur' }
            ]
        },
        loading: false,
        //分页参数
        paginationQuery: {
            total: 0
        },
        //分页接口传参
        listQuery: {
            pageIndex: 1,
            pageSize: 10
        },
        tableData: []
    },
    mounted() {
        // 检查是否启用多租户
        service.get('/Admin/Login?handler=CheckMultiTenant').then(res => {
            this.enableMultiTenant = res.data;
        });
        this.getList();
    },
    methods: {
        // 获取数据
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/Login?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                if (res.data.success) {
                    this.tableData = res.data.data.list;
                    this.paginationQuery.total = res.data.data.totalCount;
                }
            });
        },
        submitForm(formName) {
            this.$refs[formName].validate((valid) => {
                this.loading = true;
                var url = "/Admin/Login?handler=Login";
                let data = {
                    Name: this.ruleForm.user+'',
                    Password: this.ruleForm.pass,
                    Tenant: this.ruleForm.tenant
                };
                if (valid) {
                    service.post(url, data).then(res => {
                        if (res.data.success) {
                            const url = this.resizeUrl().ReturnUrl;
                            window.location.href = url ? unescape(url) : '/Admin/index';
                        } else {
                            console.log(res.data);
                        }
                    });
                    this.loading = false;
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        resizeUrl() {//处理剪切url id
            let url = window.location.href;
            let obj = {};
            let reg = /[?&][^?&]+=[^?&]+/g;
            let arr = url.match(reg); // return ["?id=123456","&a=b"]
            if (arr) {
                arr.forEach((item) => {
                    let tempArr = item.substring(1).split('=');
                    let key = tempArr[0];
                    let val = tempArr[1];
                    obj[key] = decodeURIComponent(val);
                });
            }
            return obj;
        }
    }
});