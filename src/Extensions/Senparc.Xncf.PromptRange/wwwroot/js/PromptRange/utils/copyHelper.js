/**
 * Copy function auxiliary tool
 * Provides the function of copying text to the clipboard, compatible with multiple browsers
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     *Copy auxiliary toolset
     * Use object literal mode (no instantiation required)
     */
    var CopyHelper = {
        /**
         * Copy text to clipboard
         * Prioritize using modern Clipboard API, downgrading to traditional methods
         * @param {string} text - the text to copy
         * @param {string} [successMessage='Copy successfully'] - Success message
         * @param {string} [errorMessage='Copy failed'] - Failure message
         * @param {boolean} [showMessage=true] - whether to display prompt messages
         * @returns {boolean} Whether the copy was successful
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

            // Method 1: Use the modern Clipboard API (requires HTTPS or localhost)
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
                        // Downgrade to traditional methods
                        success = self._fallbackCopy(text);
                        if (showMessage) {
                            self._showMessage(success ? successMessage : errorMessage, success ? 'success' : 'error');
                        }
                    }
                );
                return true;
            } else {
                // Method 2: Use traditional methods
                success = this._fallbackCopy(text);
                if (showMessage) {
                    this._showMessage(success ? successMessage : errorMessage, success ? 'success' : 'error');
                }
                return success;
            }
        },

        /**
         * Downgrade copy method (using document.execCommand)
         * @param {string} text - the text to copy
         * @returns {boolean} Whether the copy was successful
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
         * Copy Prompt results
         * Convenience method specifically used to copy Prompt results
         * @param {Object} item - Prompt result object
         * @param {boolean} [rawResult=false] - whether to copy the original result
         * @returns {boolean} Whether the copy was successful
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
         * Copy object as JSON string
         * Copy the object after serializing it to formatted JSON
         * @param {*} obj - the object to be copied
         * @param {number} [indent=2] - Number of indent spaces
         * @returns {boolean} Whether the copy was successful
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
         * Copy the contents of the array
         * Copy the array elements after concatenating them with newline characters
         * @param {Array} arr - the array to copy
         * @param {string} [separator='\n'] - separator
         * @returns {boolean} Whether the copy was successful
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
         * Copy HTML content (preserve formatting)
         * @param {string} html - HTML content
         * @returns {boolean} Whether the copy was successful
         */
        copyHtml: function(html) {
            if (!html) {
                this._showMessage('没有可复制的 HTML 内容', 'warning');
                return false;
            }

            // Create temporary container
            var container = document.createElement('div');
            container.innerHTML = html;
            container.style.position = 'fixed';
            container.style.left = '-999999px';
            
            document.body.appendChild(container);

            // Selected content
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
         * Display message prompt
         * Use the Message component of Element UI
         * @param {string} message - message content
         * @param {string} type - message type (success/error/warning/info)
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
         * Check if the copy function is available
         * @returns {boolean} whether available
         */
        isSupported: function() {
            return !!(navigator.clipboard || document.queryCommandSupported('copy'));
        }
    };

    // Exposed to the global namespace
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.CopyHelper = CopyHelper;

})(window);

