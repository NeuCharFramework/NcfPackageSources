/**
 * 复制功能辅助工具
 * 提供复制文本到剪贴板的功能，兼容多种浏览器
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Copy 辅助工具集
     * 使用对象字面量模式（无需实例化）
     */
    var CopyHelper = {
        /**
         * 复制文本到剪贴板
         * 优先使用现代 Clipboard API，降级使用传统方法
         * @param {string} text - 要复制的文本
         * @param {string} [successMessage='复制成功'] - 成功提示消息
         * @param {string} [errorMessage='复制失败'] - 失败提示消息
         * @param {boolean} [showMessage=true] - 是否显示提示消息
         * @returns {boolean} 是否复制成功
         */
        copyToClipboard: function(text, successMessage, errorMessage, showMessage) {
            successMessage = successMessage || '复制成功';
            errorMessage = errorMessage || '复制失败';
            showMessage = showMessage !== false;

            if (!text) {
                if (showMessage) {
                    this._showMessage('没有可复制的内容', 'warning');
                }
                return false;
            }

            var self = this;
            var success = false;

            // 方法1: 使用现代 Clipboard API (需要 HTTPS 或 localhost)
            if (navigator.clipboard && window.isSecureContext) {
                navigator.clipboard.writeText(text).then(
                    function() {
                        success = true;
                        if (showMessage) {
                            self._showMessage(successMessage, 'success');
                        }
                    },
                    function(err) {
                        console.error('Clipboard API failed:', err);
                        // 降级到传统方法
                        success = self._fallbackCopy(text);
                        if (showMessage) {
                            self._showMessage(success ? successMessage : errorMessage, success ? 'success' : 'error');
                        }
                    }
                );
                return true;
            } else {
                // 方法2: 使用传统方法
                success = this._fallbackCopy(text);
                if (showMessage) {
                    this._showMessage(success ? successMessage : errorMessage, success ? 'success' : 'error');
                }
                return success;
            }
        },

        /**
         * 降级复制方法（使用 document.execCommand）
         * @param {string} text - 要复制的文本
         * @returns {boolean} 是否复制成功
         * @private
         */
        _fallbackCopy: function(text) {
            var textarea = document.createElement('textarea');
            textarea.value = text;
            textarea.style.position = 'fixed';
            textarea.style.top = '0';
            textarea.style.left = '0';
            textarea.style.width = '1px';
            textarea.style.height = '1px';
            textarea.style.padding = '0';
            textarea.style.border = 'none';
            textarea.style.outline = 'none';
            textarea.style.boxShadow = 'none';
            textarea.style.background = 'transparent';
            textarea.style.opacity = '0';
            
            document.body.appendChild(textarea);
            textarea.focus();
            textarea.select();

            var success = false;
            try {
                success = document.execCommand('copy');
            } catch (err) {
                console.error('execCommand failed:', err);
                success = false;
            }

            document.body.removeChild(textarea);
            return success;
        },

        /**
         * 复制 Prompt 结果
         * 专门用于复制 Prompt 结果的便捷方法
         * @param {Object} item - Prompt 结果对象
         * @param {boolean} [rawResult=false] - 是否复制原始结果
         * @returns {boolean} 是否复制成功
         */
        copyPromptResult: function(item, rawResult) {
            if (!item) {
                this._showMessage('没有可复制的结果', 'warning');
                return false;
            }

            var text = rawResult ? item.rawResult : item.result;
            if (!text) {
                this._showMessage('结果为空', 'warning');
                return false;
            }

            return this.copyToClipboard(text, '复制成功');
        },

        /**
         * 复制对象为 JSON 字符串
         * 将对象序列化为格式化的 JSON 后复制
         * @param {*} obj - 要复制的对象
         * @param {number} [indent=2] - 缩进空格数
         * @returns {boolean} 是否复制成功
         */
        copyObject: function(obj, indent) {
            indent = indent !== undefined ? indent : 2;
            
            if (obj === null || obj === undefined) {
                this._showMessage('没有可复制的对象', 'warning');
                return false;
            }

            try {
                var text = JSON.stringify(obj, null, indent);
                return this.copyToClipboard(text, 'JSON 复制成功');
            } catch (error) {
                console.error('JSON stringify failed:', error);
                this._showMessage('对象序列化失败', 'error');
                return false;
            }
        },

        /**
         * 复制数组内容
         * 将数组元素用换行符连接后复制
         * @param {Array} arr - 要复制的数组
         * @param {string} [separator='\n'] - 分隔符
         * @returns {boolean} 是否复制成功
         */
        copyArray: function(arr, separator) {
            separator = separator || '\n';
            
            if (!Array.isArray(arr)) {
                this._showMessage('不是有效的数组', 'warning');
                return false;
            }

            if (arr.length === 0) {
                this._showMessage('数组为空', 'warning');
                return false;
            }

            var text = arr.join(separator);
            return this.copyToClipboard(text, '数组内容复制成功');
        },

        /**
         * 复制 HTML 内容（保留格式）
         * @param {string} html - HTML 内容
         * @returns {boolean} 是否复制成功
         */
        copyHtml: function(html) {
            if (!html) {
                this._showMessage('没有可复制的 HTML 内容', 'warning');
                return false;
            }

            // 创建临时容器
            var container = document.createElement('div');
            container.innerHTML = html;
            container.style.position = 'fixed';
            container.style.left = '-999999px';
            
            document.body.appendChild(container);

            // 选中内容
            var range = document.createRange();
            range.selectNode(container);
            window.getSelection().removeAllRanges();
            window.getSelection().addRange(range);

            var success = false;
            try {
                success = document.execCommand('copy');
            } catch (err) {
                console.error('Copy HTML failed:', err);
            }

            window.getSelection().removeAllRanges();
            document.body.removeChild(container);

            if (success) {
                this._showMessage('HTML 内容复制成功', 'success');
            } else {
                this._showMessage('HTML 内容复制失败', 'error');
            }

            return success;
        },

        /**
         * 显示消息提示
         * 使用 Element UI 的 Message 组件
         * @param {string} message - 消息内容
         * @param {string} type - 消息类型 (success/error/warning/info)
         * @private
         */
        _showMessage: function(message, type) {
            if (window.app && window.app.$message) {
                window.app.$message({
                    message: message,
                    type: type,
                    duration: 2000
                });
            } else if (typeof console !== 'undefined') {
                console[type === 'error' ? 'error' : 'log'](message);
            }
        },

        /**
         * 检查复制功能是否可用
         * @returns {boolean} 是否可用
         */
        isSupported: function() {
            return !!(navigator.clipboard || document.queryCommandSupported('copy'));
        }
    };

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.CopyHelper = CopyHelper;

})(window);

