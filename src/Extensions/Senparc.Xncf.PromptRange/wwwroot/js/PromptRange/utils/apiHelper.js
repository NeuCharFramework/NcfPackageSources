/**
 * API 请求辅助工具
 * 统一管理 API 调用、错误处理、Loading 状态等
 * 依赖: axios (servicePR 实例), Element UI (用于消息提示)
 * 
 * @version 2.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    // 检查依赖 - 使用 axios 而非 jQuery
    if (typeof window.servicePR === 'undefined' && typeof window.axios === 'undefined') {
        console.error('ApiHelper requires axios (servicePR instance is preferred)');
        return;
    }

    // 使用项目的 servicePR 实例，如果没有则使用全局 axios
    var axiosInstance = window.servicePR || window.axios;

    /**
     * API Helper 构造函数
     * @param {string} baseUrl - API 基础 URL
     * @param {Object} vueInstance - Vue 实例（用于显示消息）
     */
    function ApiHelper(baseUrl, vueInstance) {
        this.baseUrl = baseUrl || '';
        this.vueInstance = vueInstance || window.app; // 默认使用全局 app
    }

    /**
     * 发送 API 请求（通用方法）
     * @param {Object} options - 请求选项
     * @param {string} options.url - API 路径（相对于 baseUrl）
     * @param {string} [options.method='post'] - 请求方法 (get/post/put/delete)
     * @param {Object} [options.data={}] - 请求数据
     * @param {Function} [options.onSuccess] - 成功回调函数
     * @param {Function} [options.onError] - 失败回调函数
     * @param {Object} [options.loadingState] - Loading 状态对象 {key: 'loadingKey', target: vueInstance}
     * @param {string} [options.successMessage] - 成功提示消息（null 则使用服务器返回的消息）
     * @param {string} [options.errorMessage='请求失败'] - 失败提示消息
     * @param {boolean} [options.showSuccessMessage=true] - 是否显示成功消息
     * @param {boolean} [options.showErrorMessage=true] - 是否显示错误消息
     */
    ApiHelper.prototype.request = function(options) {
        var self = this;
        var url = this.baseUrl + (options.url || '');
        var method = (options.method || 'post').toLowerCase();
        var data = options.data || {};
        var onSuccess = options.onSuccess;
        var onError = options.onError;
        var loadingState = options.loadingState;
        var successMessage = options.successMessage;
        var errorMessage = options.errorMessage || '请求失败';
        var showSuccessMessage = options.showSuccessMessage !== false;
        var showErrorMessage = options.showErrorMessage !== false;
        var vueInstance = options.vueInstance || this.vueInstance;

        // 设置 loading 状态
        if (loadingState && loadingState.target && loadingState.key) {
            loadingState.target[loadingState.key] = true;
        }

        // 构建 axios 请求配置
        var axiosConfig = {
            url: url,
            method: method,
            data: method === 'post' || method === 'put' ? { data: data } : undefined,
            params: method === 'get' || method === 'delete' ? data : undefined
        };

        // 发送请求
        axiosInstance(axiosConfig)
            .then(function(response) {
                // 关闭 loading
                if (loadingState && loadingState.target && loadingState.key) {
                    loadingState.target[loadingState.key] = false;
                }

                // 检查响应
                if (response && response.data && response.data.success) {
                    // 显示成功消息
                    if (showSuccessMessage && vueInstance && vueInstance.$message) {
                        var msgText = successMessage !== undefined 
                            ? successMessage 
                            : (response.data.msg || response.data.message || '操作成功');
                        if (msgText) {
                            vueInstance.$message.success(msgText);
                        }
                    }

                    // 调用成功回调
                    if (typeof onSuccess === 'function') {
                        onSuccess(response.data);
                    }
                } else {
                    // 请求成功但业务失败
                    var errMsg = response.data.errorMessage || response.data.msg || errorMessage;
                    
                    if (showErrorMessage && vueInstance && vueInstance.$message) {
                        vueInstance.$message.error(errMsg);
                    }

                    if (typeof onError === 'function') {
                        onError(response.data);
                    }
                }
            })
            .catch(function(error) {
                // 关闭 loading
                if (loadingState && loadingState.target && loadingState.key) {
                    loadingState.target[loadingState.key] = false;
                }

                // 显示错误消息
                if (showErrorMessage && vueInstance && vueInstance.$message) {
                    var errMsg = error.message || errorMessage;
                    vueInstance.$message.error(errMsg);
                }

                // 调用错误回调
                if (typeof onError === 'function') {
                    onError(error);
                }

                console.error('API request failed:', error);
            });
    };

    /**
     * POST 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} data - 请求数据
     * @param {Function} onSuccess - 成功回调
     * @param {Function} onError - 失败回调
     * @param {Object} options - 其他选项
     */
    ApiHelper.prototype.post = function(url, data, onSuccess, onError, options) {
        var opts = options || {};
        opts.url = url;
        opts.method = 'post';
        opts.data = data;
        opts.onSuccess = onSuccess;
        opts.onError = onError;
        this.request(opts);
    };

    /**
     * GET 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} params - 查询参数
     * @param {Function} onSuccess - 成功回调
     * @param {Function} onError - 失败回调
     * @param {Object} options - 其他选项
     */
    ApiHelper.prototype.get = function(url, params, onSuccess, onError, options) {
        var opts = options || {};
        opts.url = url;
        opts.method = 'get';
        opts.data = params;
        opts.onSuccess = onSuccess;
        opts.onError = onError;
        this.request(opts);
    };

    /**
     * DELETE 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} data - 请求数据
     * @param {Function} onSuccess - 成功回调
     * @param {Function} onError - 失败回调
     * @param {Object} options - 其他选项
     */
    ApiHelper.prototype.delete = function(url, data, onSuccess, onError, options) {
        var opts = options || {};
        opts.url = url;
        opts.method = 'delete';
        opts.data = data;
        opts.onSuccess = onSuccess;
        opts.onError = onError;
        this.request(opts);
    };

    /**
     * 带确认对话框的删除请求
     * @param {Object} options - 选项
     * @param {string} options.url - API 路径
     * @param {Object} options.data - 请求数据
     * @param {Function} options.onSuccess - 成功回调
     * @param {string} [options.confirmMessage='确认删除吗?'] - 确认消息
     * @param {string} [options.confirmTitle='提示'] - 确认标题
     */
    ApiHelper.prototype.deleteWithConfirm = function(options) {
        var self = this;
        var vueInstance = options.vueInstance || this.vueInstance;
        var confirmMessage = options.confirmMessage || '确认删除吗?';
        var confirmTitle = options.confirmTitle || '提示';

        if (!vueInstance || !vueInstance.$confirm) {
            console.error('Vue instance with $confirm method is required');
            return;
        }

        vueInstance.$confirm(confirmMessage, confirmTitle, {
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            type: 'warning'
        }).then(function() {
            self.delete(
                options.url,
                options.data,
                options.onSuccess,
                options.onError,
                {
                    loadingState: options.loadingState,
                    successMessage: options.successMessage || '删除成功',
                    errorMessage: options.errorMessage || '删除失败'
                }
            );
        }).catch(function() {
            // 用户取消
        });
    };

    /**
     * 批量请求
     * @param {Array} requests - 请求数组，每个元素是 request 方法的 options
     * @param {Function} onAllSuccess - 全部成功回调
     * @param {Function} onAnyError - 任意失败回调
     */
    ApiHelper.prototype.batchRequest = function(requests, onAllSuccess, onAnyError) {
        var self = this;
        var promises = [];

        for (var i = 0; i < requests.length; i++) {
            var opts = requests[i];
            var url = this.baseUrl + (opts.url || '');
            var method = (opts.method || 'post').toLowerCase();
            var data = opts.data || {};

            var axiosConfig = {
                url: url,
                method: method,
                data: method === 'post' || method === 'put' ? { data: data } : undefined,
                params: method === 'get' || method === 'delete' ? data : undefined
            };

            promises.push(axiosInstance(axiosConfig));
        }

        Promise.all(promises)
            .then(function(responses) {
                if (typeof onAllSuccess === 'function') {
                    var results = [];
                    for (var i = 0; i < responses.length; i++) {
                        results.push(responses[i].data);
                    }
                    onAllSuccess(results);
                }
            })
            .catch(function(error) {
                if (typeof onAnyError === 'function') {
                    onAnyError(error);
                }
            });
    };

    // 暴露到全局命名空间
    if (typeof window.PromptRangeUtils === 'undefined') {
        window.PromptRangeUtils = {};
    }
    window.PromptRangeUtils.ApiHelper = ApiHelper;

})(window);
