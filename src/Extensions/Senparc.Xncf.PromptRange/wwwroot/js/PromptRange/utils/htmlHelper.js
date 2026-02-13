/**
 * HTML 操作辅助工具
 * 提供 HTML 转义、UUID 生成、防抖节流等通用功能
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * HTML 辅助工具集
     * 使用对象字面量模式（无需实例化）
     */
    var HtmlHelper = {
        /**
         * HTML 转义
         * 将文本中的特殊字符转换为 HTML 实体
         * @param {string} text - 要转义的文本
         * @returns {string} 转义后的文本
         */
        escape: function(text) {
            if (!text) return '';
            var div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },

        /**
         * 正则表达式转义
         * 转义正则表达式中的特殊字符
         * @param {string} str - 要转义的字符串
         * @returns {string} 转义后的字符串
         */
        escapeRegex: function(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },

        /**
         * 生成 UUID (v4)
         * 生成符合 RFC4122 标准的 UUID
         * @returns {string} UUID 字符串
         */
        generateUUID: function() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                var r = (Math.random() * 16) | 0;
                var v = c === 'x' ? r : (r & 0x3) | 0x8;
                return v.toString(16);
            });
        },

        /**
         * 格式化文件大小
         * 将字节数转换为人类可读的格式
         * @param {number} bytes - 字节数
         * @returns {string} 格式化后的文件大小（如 "1.23 MB"）
         */
        formatFileSize: function(bytes) {
            if (bytes === 0) return '0 B';
            var k = 1024;
            var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
            var i = Math.floor(Math.log(bytes) / Math.log(k));
            return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i];
        },

        /**
         * 防抖函数
         * 延迟执行函数，如果在延迟期间再次调用，则重新计时
         * @param {Function} func - 要防抖的函数
         * @param {number} wait - 延迟时间（毫秒）
         * @returns {Function} 防抖后的函数
         */
        debounce: function(func, wait) {
            var timeout;
            return function() {
                var context = this;
                var args = arguments;
                clearTimeout(timeout);
                timeout = setTimeout(function() {
                    func.apply(context, args);
                }, wait);
            };
        },

        /**
         * 节流函数
         * 限制函数在指定时间内只能执行一次
         * @param {Function} func - 要节流的函数
         * @param {number} limit - 时间限制（毫秒）
         * @returns {Function} 节流后的函数
         */
        throttle: function(func, limit) {
            var inThrottle;
            return function() {
                var context = this;
                var args = arguments;
                if (!inThrottle) {
                    func.apply(context, args);
                    inThrottle = true;
                    setTimeout(function() {
                        inThrottle = false;
                    }, limit);
                }
            };
        },

        /**
         * 深度克隆对象
         * 创建对象的深拷贝
         * @param {*} obj - 要克隆的对象
         * @returns {*} 克隆后的对象
         */
        deepClone: function(obj) {
            if (obj === null || typeof obj !== 'object') {
                return obj;
            }

            if (obj instanceof Date) {
                return new Date(obj.getTime());
            }

            if (obj instanceof Array) {
                var cloneArray = [];
                for (var i = 0; i < obj.length; i++) {
                    cloneArray[i] = this.deepClone(obj[i]);
                }
                return cloneArray;
            }

            if (obj instanceof Object) {
                var cloneObj = {};
                for (var key in obj) {
                    if (obj.hasOwnProperty(key)) {
                        cloneObj[key] = this.deepClone(obj[key]);
                    }
                }
                return cloneObj;
            }
        },

        /**
         * 获取 URL 查询参数
         * 从 URL 中提取指定的查询参数值
         * @param {string} name - 参数名
         * @param {string} [url] - URL 字符串，默认为当前页面 URL
         * @returns {string|null} 参数值，不存在则返回 null
         */
        getQueryParam: function(name, url) {
            url = url || window.location.href;
            name = name.replace(/[\[\]]/g, '\\$&');
            var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)');
            var results = regex.exec(url);
            if (!results) return null;
            if (!results[2]) return '';
            return decodeURIComponent(results[2].replace(/\+/g, ' '));
        },

        /**
         * 判断是否为空值
         * 检查值是否为 null、undefined、空字符串、空数组或空对象
         * @param {*} value - 要检查的值
         * @returns {boolean} 是否为空
         */
        isEmpty: function(value) {
            if (value === null || value === undefined) return true;
            if (typeof value === 'string' && value.trim() === '') return true;
            if (Array.isArray(value) && value.length === 0) return true;
            if (typeof value === 'object' && Object.keys(value).length === 0) return true;
            return false;
        }
    };

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.HtmlHelper = HtmlHelper;

})(window);

