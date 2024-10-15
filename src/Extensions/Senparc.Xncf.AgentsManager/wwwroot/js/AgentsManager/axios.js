/**
 * axios封装
 * 请求拦截、响应拦截、错误统一处理
 */
// 创建一个axios实例
var serviceAM = axios.create({
    timeout:  1000 * 60 * 10  // request timeout
});
// 请求拦截
serviceAM.interceptors.request.use(
    config => {
        if (config.method.toUpperCase() === 'POST') {
            config.headers['RequestVerificationToken'] = window.document.getElementsByName('__RequestVerificationToken')[0].value;
        }
        config.headers['x-requested-with'] = 'XMLHttpRequest';
        return config;
    },
    error => {
        console.log(error); // for debug
        return Promise.reject(error);
    }
);
// 响应拦截器
serviceAM.interceptors.response.use(
    response => {
        //console.log('response', response)
        if (response.status === 200) {
            if (response.request.responseType === 'blob' || response.request.responseType === 'arraybuffer') {
                // console.log('文件流：', response)
                return Promise.resolve(response);
            } else if (response.data.success) {
                return Promise.resolve(response);
            } else {
                // 请求已发出，其他状态
                // 切换隐藏时不给错误提示，直接刷新
                if (response.config.url.includes('HideManager') || response.config.url.includes('ChangeState')) {
                    return;
                }
                if (!response.config.customAlert){
                    app.$message({
                        message: response.data.errorMessage || response.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
                return Promise.resolve(response);
            }
        } else {
            app.$message({
                message: response.data.errorMessage || response.data.data || 'Error',
                type: 'error',
                duration: 5 * 1000
            });
            return Promise.reject(response);
        }
    },
    error => {
        console.log('err' + error);
        if (error.message.includes('401')) {
            app.$message({
                message: '登陆过期，即将跳转到登录页面',
                type: 'error',
                duration: 3 * 1000,
                onClose: function () {
                    window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
                }
            });
            return Promise.reject(error);
        } if (error.message.includes('403')) {
            app.$message({
                message: '您没有访问权限~',
                type: 'error',
                duration: 3 * 1000
            });
            return Promise.reject(error);
        } else if (error.message.includes('302')) {
            return Promise.reject(error);
        }
        app.$message({
            message: error.message,
            type: 'error',
            duration: 5 * 1000
        });
        return Promise.reject(error);
    }
);
