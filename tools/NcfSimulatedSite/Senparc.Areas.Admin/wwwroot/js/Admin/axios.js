/**
 * Axios wrapper
 * Request interceptor, response interceptor, unified error handling
 */
var ncfI18n = window.ncfI18n || {};

// Create an axios instance
var service = axios.create({
    timeout: 1000000 // request timeout
});
// Request interceptor
service.interceptors.request.use(
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
// Response interceptor
service.interceptors.response.use(
    response => {
        if (response.status === 200) {
            if (response.data.success) {
                return Promise.resolve(response);
            } else {
                // Request sent but non-success status
                // No error prompt on hide/state toggle, just refresh
                if (response.config.url.includes('HideManager') || response.config.url.includes('ChangeState')) {
                    return;
                }
                if (!response.config.customAlert){
                    app.$message({
                        message: response.data.msg||response.data.exception|| 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
                return Promise.resolve(response);
            }
        } else {
            app.$message({
                message: response.msg || 'Error',
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
                message: ncfI18n.sessionExpired || 'Session expired, redirecting to login page',
                type: 'error',
                duration: 3 * 1000,
                onClose: function () {
                    window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
                }
            });
            return Promise.reject(error);
        } if (error.message.includes('403')) {
            app.$message({
                message: ncfI18n.noPermission || 'You do not have access permission',
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
