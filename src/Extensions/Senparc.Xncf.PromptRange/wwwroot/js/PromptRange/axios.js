/**

 * Axios wrapper.

 * Centralizes request interception, response interception, and error handling.

 */
// Create the Axios instance.
var servicePR = axios.create({
    timeout: 100000 // request timeout
});

function buildPromptRangeErrorMessage(data) {
    if (!data) {
        return 'Error';
    }

    const baseMessage = data.errorMessage || data.data || 'Error';
    if (!data.requestTempId) {
        return baseMessage;
    }

    return `${baseMessage}\nRequestTempId: ${data.requestTempId}`;
}
// Request interceptor.
servicePR.interceptors.request.use(
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
// Response interceptor.
servicePR.interceptors.response.use(
    response => {
        //console.log('response', response)
                // Binary response handling.
        //if (response.request.responseType === 'blob' || response.request.responseType === 'arraybuffer') {
        //    if (response.data.type === 'application/json') {
        //        const reader = new FileReader();
        //        reader.onload = () => {
        //            app.$message({
        //                message: `Error: ${JSON.parse(reader.result).message}！` || 'Error',
        //                type: 'error',
        //                duration: 5 * 1000
        //            });
        //        };
        //        reader.readAsText(res.data);
        //        return Promise.reject(response);
        //    } else {
        //        return Promise.resolve(response);
        //    }
        //} else
        if (response.status === 200) {
            if (response.request.responseType === 'blob' || response.request.responseType === 'arraybuffer') {
                //Legacy comment translated to English.
                return Promise.resolve(response);
            } else if (response.data.success) {
                return Promise.resolve(response);
            } else {
                // The request completed with an application-level error.
                // Hidden state changes are refreshed without showing an alert.
                if (response.config.url.includes('HideManager') || response.config.url.includes('ChangeState')) {
                    return;
                }
                if (!response.config.customAlert){
                    app.$message({
                        message: buildPromptRangeErrorMessage(response.data),
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
                return Promise.resolve(response);
            }
        } else {
            app.$message({
                message: buildPromptRangeErrorMessage(response.data),
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
                message: ncfT('Auth.SessionExpired'),
                type: 'error',
                duration: 3 * 1000,
                onClose: function () {
                    window.location.href = '/Admin/Login?url=' + escape(window.location.pathname + window.location.search);
                }
            });
            return Promise.reject(error);
        } if (error.message.includes('403')) {
            app.$message({
                message: ncfT('Auth.AccessDenied'),
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
