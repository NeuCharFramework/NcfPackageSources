/**
 * HTML helper.
 * Provides HTML escaping, UUID generation, debounce, and throttle utilities.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * HTML helper methods.
     * Uses an object-literal pattern and requires no instantiation.
     */
    var HtmlHelper = {
        /**
         * Escape HTML.
         * @param {string} text - Text to escape.
         * @returns {string} HTML-escaped text.
         */
        escape: function(text) {
            if (!text) return '';
            var div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },

        /**
         * Escape regular-expression metacharacters.
         * @param {string} str - String to escape.
         * @returns {string} Escaped string.
         */
        escapeRegex: function(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },

        /**
         * Generate an RFC 4122 UUID v4.
         * @returns {string} UUID string.
         */
        generateUUID: function() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                var r = (Math.random() * 16) | 0;
                var v = c === 'x' ? r : (r & 0x3) | 0x8;
                return v.toString(16);
            });
        },

        /**
         * Format a file size.
         * @param {number} bytes - Number of bytes.
         * @returns {string} Human-readable size such as "1.23 MB".
         */
        formatFileSize: function(bytes) {
            if (bytes === 0) return '0 B';
            var k = 1024;
            var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
            var i = Math.floor(Math.log(bytes) / Math.log(k));
            return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i];
        },

        /**
         * Debounce a function.
         * @param {Function} func - Function to debounce.
         * @param {number} wait - Delay in milliseconds.
         * @returns {Function} Debounced function.
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
         * Throttle a function.
         * @param {Function} func - Function to throttle.
         * @param {number} limit - Limit in milliseconds.
         * @returns {Function} Throttled function.
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
         * Deep-clone an object.
         * @param {*} obj - Object to clone.
         * @returns {*} Cloned value.
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
         * Read a URL query parameter.
         * @param {string} name - Parameter name.
         * @param {string} [url] - URL; defaults to the current page URL.
         * @returns {string|null} Parameter value, or null when absent.
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
         * Check whether a value is empty.
         * @param {*} value - Value to check.
         * @returns {boolean} Whether the value is empty.
         */
        isEmpty: function(value) {
            if (value === null || value === undefined) return true;
            if (typeof value === 'string' && value.trim() === '') return true;
            if (Array.isArray(value) && value.length === 0) return true;
            if (typeof value === 'object' && Object.keys(value).length === 0) return true;
            return false;
        }
    };

    // Expose the helper through the global namespace.
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.HtmlHelper = HtmlHelper;

})(window);

