/**
 * Clipboard helper.
 * Copies text to the clipboard with broad browser compatibility.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Clipboard helper methods.
     * Uses an object-literal pattern and requires no instantiation.
     */
    var CopyHelper = {
        /**
         * Copy text to the clipboard.
         * @param {string} text - Text to copy.
         * @param {string} [successMessage='复制成功'] - Success message.
         * @param {string} [errorMessage='复制失败'] - Error message.
         * @param {boolean} [showMessage=true] - Whether to show a message.
         * @returns {boolean} Whether the copy request was accepted.
         */
        copyToClipboard: function(text, successMessage, errorMessage, showMessage) {
            if (!text) {
                if (showMessage !== false) {
                    this._showMessage(ncfST('没有可复制的内容'), 'warning');
                }
                return false;
            }

            this.copyToClipboardAsync(text, successMessage, errorMessage, showMessage);
            return true;
        },

        /**
         * Copy text asynchronously and return the final result.
         * @returns {Promise<boolean>} Whether copying succeeded.
         */
        copyToClipboardAsync: async function(text, successMessage, errorMessage, showMessage) {
            successMessage = successMessage || ncfST('复制成功');
            errorMessage = errorMessage || ncfST('复制失败');
            showMessage = showMessage !== false;

            if (!text) {
                if (showMessage) {
                    this._showMessage(ncfST('没有可复制的内容'), 'warning');
                }
                return Promise.resolve(false);
            }

            var success = false;

            // Prefer the modern Clipboard API (HTTPS or localhost required).
            if (window.isSecureContext && navigator.clipboard && navigator.clipboard.writeText) {
                try {
                    await navigator.clipboard.writeText(text);
                    success = true;
                } catch (err) {
                    console.warn('Clipboard API failed, using the compatibility fallback:', err);
                }
            }

            if (!success) {
                success = this._fallbackCopy(text);
            }

            if (showMessage) {
                this._showMessage(success ? successMessage : errorMessage, success ? 'success' : 'error');
            }
            return success;
        },

        /**
         * Compatibility fallback using document.execCommand.
         * @param {string} text - Text to copy.
         * @returns {boolean} Whether copying succeeded.
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
         * Copy a Prompt result.
         * @param {Object} item - Prompt result.
         * @param {boolean} [rawResult=false] - Whether to copy the raw result.
         * @returns {boolean} Whether copying succeeded.
         */
        copyPromptResult: function(item, rawResult) {
            if (!item) {
                this._showMessage(ncfST('没有可复制的结果'), 'warning');
                return false;
            }

            var text = rawResult ? item.rawResult : item.result;
            if (!text) {
                this._showMessage(ncfST('结果为空'), 'warning');
                return false;
            }

            return this.copyToClipboard(text, ncfST('复制成功'));
        },

        /**
         * Copy an object as formatted JSON.
         * @param {*} obj - Object to copy.
         * @param {number} [indent=2] - Indentation size.
         * @returns {boolean} Whether copying succeeded.
         */
        copyObject: function(obj, indent) {
            indent = indent !== undefined ? indent : 2;
            
            if (obj === null || obj === undefined) {
                this._showMessage(ncfST('没有可复制的对象'), 'warning');
                return false;
            }

            try {
                var text = JSON.stringify(obj, null, indent);
                return this.copyToClipboard(text, ncfST('JSON 复制成功'));
            } catch (error) {
                console.error('JSON stringify failed:', error);
                this._showMessage(ncfST('对象序列化失败'), 'error');
                return false;
            }
        },

        /**
         * Copy array contents joined by a separator.
         * @param {Array} arr - Array to copy.
         * @param {string} [separator='\n'] - Separator.
         * @returns {boolean} Whether copying succeeded.
         */
        copyArray: function(arr, separator) {
            separator = separator || '\n';
            
            if (!Array.isArray(arr)) {
                this._showMessage(ncfST('不是有效的数组'), 'warning');
                return false;
            }

            if (arr.length === 0) {
                this._showMessage(ncfST('数组为空'), 'warning');
                return false;
            }

            var text = arr.join(separator);
            return this.copyToClipboard(text, ncfST('数组内容复制成功'));
        },

        /**
         * Copy HTML content while preserving formatting.
         * @param {string} html - HTML content.
         * @returns {boolean} Whether copying succeeded.
         */
        copyHtml: function(html) {
            if (!html) {
                this._showMessage(ncfST('没有可复制的 HTML 内容'), 'warning');
                return false;
            }

            // Create a temporary container.
            var container = document.createElement('div');
            container.innerHTML = html;
            container.style.position = 'fixed';
            container.style.left = '-999999px';
            
            document.body.appendChild(container);

            // Select the content.
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
                this._showMessage(ncfST('HTML 内容复制成功'), 'success');
            } else {
                this._showMessage(ncfST('HTML 内容复制失败'), 'error');
            }

            return success;
        },

        /**
         * Show a message through Element UI's Message component.
         * @param {string} message - Message content.
         * @param {string} type - Message type (success/error/warning/info).
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
         * Check whether the copy feature is available.
         * @returns {boolean} Whether copying is available.
         */
        isSupported: function() {
            return !!(navigator.clipboard || document.queryCommandSupported('copy'));
        }
    };

    // Expose the helper through the global namespace.
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.CopyHelper = CopyHelper;

})(window);
