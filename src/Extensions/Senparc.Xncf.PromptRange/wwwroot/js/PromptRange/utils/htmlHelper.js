/**
 * HTML operation assistance tool
 * Provides common functions such as HTML escaping, UUID generation, anti-shake throttling, etc.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     *HTML auxiliary toolset
     * Use object literal mode (no instantiation required)
     */
    var HtmlHelper = {
        /**
         * HTML escape
         * Convert special characters in text to HTML entities
         * @param {string} text - the text to escape
         * @returns {string} escaped text
         */
        escape: function(text) {
            if (!text) return '';
            var div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },

        /**
         * Regular expression escape
         * Escape special characters in regular expressions
         * @param {string} str - the string to escape
         * @returns {string} escaped string
         */
        escapeRegex: function(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },

        /**
         * Generate UUID (v4)
         * Generate UUID compliant with RFC4122 standard
         * @returns {string} UUID string
         */
        generateUUID: function() {
            return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
                var r = (Math.random() * 16) | 0;
                var v = c === 'x' ? r : (r & 0x3) | 0x8;
                return v.toString(16);
            });
        },

        /**
         * Format file size
         * Convert byte count to human-readable format
         * @param {number} bytes - number of bytes
         * @returns {string} formatted file size (such as "1.23 MB")
         */
        formatFileSize: function(bytes) {
            if (bytes === 0) return '0 B';
            var k = 1024;
            var sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
            var i = Math.floor(Math.log(bytes) / Math.log(k));
            return (bytes / Math.pow(k, i)).toFixed(2) + ' ' + sizes[i];
        },

        /**
         * Anti-shake function
         * Delay execution function, if called again during the delay period, retime
         * @param {Function} func - the function to be anti-shake
         * @param {number} wait - delay time (milliseconds)
         * @returns {Function} function after anti-shake
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
         * Throttle function
         * Limit the function to be executed only once within a specified time
         * @param {Function} func - the function to throttle
         * @param {number} limit - time limit (milliseconds)
         * @returns {Function} The function after throttling
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
         * Deep clone object
         * Create a deep copy of the object
         * @param {*} obj - the object to be cloned
         * @returns {*} cloned object
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
         * Get URL query parameters
         * Extract the specified query parameter value from the URL
         * @param {string} name - parameter name
         * @param {string} [url] - URL string, defaults to the current page URL
         * @returns {string|null} parameter value, if it does not exist, return null
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
         * Determine whether it is a null value
         * Check whether the value is null, undefined, empty string, empty array or empty object
         * @param {*} value - the value to check
         * @returns {boolean} whether it is empty
         */
        isEmpty: function(value) {
            if (value === null || value === undefined) return true;
            if (typeof value === 'string' && value.trim() === '') return true;
            if (Array.isArray(value) && value.length === 0) return true;
            if (typeof value === 'object' && Object.keys(value).length === 0) return true;
            return false;
        }
    };

    // Exposed to the global namespace
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.HtmlHelper = HtmlHelper;

})(window);

