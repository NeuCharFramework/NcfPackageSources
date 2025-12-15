/**
 * API 请求辅助工具
 * 统一管理 API 调用、错误处理、Loading 状态等
 * 依赖: jQuery (全局 $), Element UI (用于消息提示)
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    // 检查依赖
    if (typeof $ === 'undefined') {
        console.error('ApiHelper requires jQuery');
        return;
    }

    /**
     * API Helper 构造函数
     * @param {string} baseUrl - API 基础 URL
     */
    function ApiHelper(baseUrl) {
        this.baseUrl = baseUrl || '';
    }

    /**
     * 发送 API 请求
     * @param {Object} options - 请求选项
     * @param {string} options.url - API 路径（相对于 baseUrl）
     * @param {string} [options.method='POST'] - 请求方法 (GET/POST/PUT/DELETE)
     * @param {Object} [options.data={}] - 请求数据
     * @param {Function} [options.onSuccess] - 成功回调函数
     * @param {Function} [options.onError] - 失败回调函数
     * @param {Object} [options.loadingState] - Loading 状态对象 {key: 'loadingKey', target: vueInstance}
     * @param {string} [options.successMessage] - 成功提示消息（为 null 则使用服务器返回的消息）
     * @param {string} [options.errorMessage='请求失败'] - 失败提示消息
     * @param {boolean} [options.showSuccessMessage=true] - 是否显示成功消息
     * @param {boolean} [options.showErrorMessage=true] - 是否显示错误消息
     */
    ApiHelper.prototype.request = function(options) {
        var self = this;
        var url = options.url || '';
        var method = options.method || 'POST';
        var data = options.data || {};
        var onSuccess = options.onSuccess;
        var onError = options.onError;
        var loadingState = options.loadingState;
        var successMessage = options.successMessage;
        var errorMessage = options.errorMessage || '请求失败';
        var showSuccessMessage = options.showSuccessMessage !== false;
        var showErrorMessage = options.showErrorMessage !== false;
        
        // 设置 loading 状态
        if (loadingState) {
            loadingState.target[loadingState.key] = true;
        }
        
        // 发送请求
        $.ajax({
            url: this.baseUrl + url,
            type: method,
            data: JSON.stringify(data),
            contentType: 'application/json',
            dataType: 'json',
            success: function(response) {
                if (response.success) {
                    // 显示成功消息
                    if (showSuccessMessage) {
                        if (successMessage) {
                            self._showMessage(successMessage, 'success');
                        } else if (response.msg) {
                            self._showMessage(response.msg, 'success');
                        }
                    }
                    
                    // 执行成功回调
                    if (onSuccess) {
                        onSuccess(response);
                    }
                } else {
                    // 显示错误消息
                    if (showErrorMessage) {
                        self._showMessage(response.msg || errorMessage, 'error');
                    }
                    
                    // 执行错误回调
                    if (onError) {
                        onError(response);
                    }
                }
            },
            error: function(xhr, status, error) {
                // 显示错误消息
                if (showErrorMessage) {
                    self._showMessage(errorMessage, 'error');
                }
                
                // 执行错误回调
                if (onError) {
                    onError({
                        success: false,
                        msg: errorMessage,
                        xhr: xhr,
                        status: status,
                        error: error
                    });
                }
            },
            complete: function() {
                // 清除 loading 状态
                if (loadingState) {
                    loadingState.target[loadingState.key] = false;
                }
            }
        });
    };

    /**
     * GET 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} [options] - 其他选项
     */
    ApiHelper.prototype.get = function(url, options) {
        options = options || {};
        options.url = url;
        options.method = 'GET';
        return this.request(options);
    };

    /**
     * POST 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} data - 请求数据
     * @param {Object} [options] - 其他选项
     */
    ApiHelper.prototype.post = function(url, data, options) {
        options = options || {};
        options.url = url;
        options.method = 'POST';
        options.data = data;
        return this.request(options);
    };

    /**
     * PUT 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} data - 请求数据
     * @param {Object} [options] - 其他选项
     */
    ApiHelper.prototype.put = function(url, data, options) {
        options = options || {};
        options.url = url;
        options.method = 'PUT';
        options.data = data;
        return this.request(options);
    };

    /**
     * DELETE 请求快捷方法
     * @param {string} url - API 路径
     * @param {Object} [options] - 其他选项
     */
    ApiHelper.prototype.delete = function(url, options) {
        options = options || {};
        options.url = url;
        options.method = 'DELETE';
        return this.request(options);
    };

    /**
     * 显示消息提示
     * 使用 Element UI 的 Message 组件
     * @param {string} message - 消息内容
     * @param {string} type - 消息类型 (success/error/warning/info)
     * @private
     */
    ApiHelper.prototype._showMessage = function(message, type) {
        // 依赖全局的 Element UI 消息组件
        if (window.app && window.app.$message) {
            window.app.$message({
                message: message,
                type: type,
                duration: 3000
            });
        } else if (typeof console !== 'undefined') {
            // 降级到 console
            console[type === 'error' ? 'error' : 'log'](message);
        }
    };

    /**
     * 批量请求
     * 并行发送多个请求，等待所有请求完成
     * @param {Array} requests - 请求配置数组
     * @param {Function} onComplete - 所有请求完成后的回调
     * @param {Function} onError - 任一请求失败时的回调
     */
    ApiHelper.prototype.batchRequest = function(requests, onComplete, onError) {
        var self = this;
        var results = [];
        var completed = 0;
        var hasError = false;

        if (!requests || requests.length === 0) {
            if (onComplete) {
                onComplete([]);
            }
            return;
        }

        for (var i = 0; i < requests.length; i++) {
            (function(index, req) {
                var originalOnSuccess = req.onSuccess;
                var originalOnError = req.onError;

                req.onSuccess = function(response) {
                    results[index] = response;
                    completed++;

                    if (originalOnSuccess) {
                        originalOnSuccess(response);
                    }

                    if (completed === requests.length && !hasError && onComplete) {
                        onComplete(results);
                    }
                };

                req.onError = function(error) {
                    hasError = true;
                    results[index] = error;

                    if (originalOnError) {
                        originalOnError(error);
                    }

                    if (onError) {
                        onError(error, results);
                    }
                };

                self.request(req);
            })(i, requests[i]);
        }
    };

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.ApiHelper = ApiHelper;

})(window);

