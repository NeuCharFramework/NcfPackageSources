/**
 * LocalStorage helper.
 * Simplifies storage operations and handles JSON serialization automatically.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**

     * Storage helper methods.

     * Uses an object-literal pattern and requires no instantiation.

     */
    var StorageHelper = {
        /**

         * Save data to localStorage.

         * @param {string} key - Storage key.

         * @param {*} value - Value to serialize.

         * @returns {boolean} Whether saving succeeded.

         */
        set: function(key, value) {
            if (!key) {
                console.error('StorageHelper.set: key is required');
                return false;
            }

            try {
                var serialized = JSON.stringify(value);
                localStorage.setItem(key, serialized);
                return true;
            } catch (error) {
                console.error('StorageHelper.set error:', error);
                return false;
            }
        },

        /**

         * Read data from localStorage.

         * @param {string} key - Storage key.

         * @param {*} [defaultValue=null] - Value returned when missing or invalid.

         * @returns {*} Stored value or the default.

         */
        get: function(key, defaultValue) {
            if (!key) {
                console.error('StorageHelper.get: key is required');
                return defaultValue !== undefined ? defaultValue : null;
            }

            defaultValue = defaultValue !== undefined ? defaultValue : null;

            try {
                var item = localStorage.getItem(key);
                if (item === null) {
                    return defaultValue;
                }
                return JSON.parse(item);
            } catch (error) {
                console.error('StorageHelper.get error:', error);
                return defaultValue;
            }
        },

        /**

         * Remove data from localStorage.

         * @param {string} key - Storage key.

         * @returns {boolean} Whether removal succeeded.

         */
        remove: function(key) {
            if (!key) {
                console.error('StorageHelper.remove: key is required');
                return false;
            }

            try {
                localStorage.removeItem(key);
                return true;
            } catch (error) {
                console.error('StorageHelper.remove error:', error);
                return false;
            }
        },

        /**

         * Clear all localStorage data.

         * @returns {boolean} Whether clearing succeeded.

         */
        clear: function() {
            try {
                localStorage.clear();
                return true;
            } catch (error) {
                console.error('StorageHelper.clear error:', error);
                return false;
            }
        },

        /**

         * Check whether a key exists.

         * @param {string} key - Storage key.

         * @returns {boolean} Whether the key exists.

         */
        has: function(key) {
            if (!key) {
                return false;
            }
            return localStorage.getItem(key) !== null;
        },

        /**

         * Return all storage keys.

         * @returns {Array<string>} Storage key list.

         */
        keys: function() {
            var keys = [];
            try {
                for (var i = 0; i < localStorage.length; i++) {
                    keys.push(localStorage.key(i));
                }
            } catch (error) {
                console.error('StorageHelper.keys error:', error);
            }
            return keys;
        },

        /**

         * Return the number of stored entries.

         * @returns {number} Entry count.

         */
        length: function() {
            try {
                return localStorage.length;
            } catch (error) {
                console.error('StorageHelper.length error:', error);
                return 0;
            }
        },

        /**

         * Return all stored data.

         * @returns {Object} Object containing all key-value pairs.

         */
        getAll: function() {
            var all = {};
            try {
                for (var i = 0; i < localStorage.length; i++) {
                    var key = localStorage.key(i);
                    all[key] = this.get(key);
                }
            } catch (error) {
                console.error('StorageHelper.getAll error:', error);
            }
            return all;
        },

        /**

         * Set multiple values.

         * @param {Object} data - Key-value object.

         * @returns {boolean} Whether all values were stored.

         */
        setMultiple: function(data) {
            if (!data || typeof data !== 'object') {
                console.error('StorageHelper.setMultiple: data must be an object');
                return false;
            }

            var success = true;
            for (var key in data) {
                if (data.hasOwnProperty(key)) {
                    if (!this.set(key, data[key])) {
                        success = false;
                    }
                }
            }
            return success;
        },

        /**

         * Get multiple values.

         * @param {Array<string>} keys - Key list.

         * @returns {Object} Object containing requested key-value pairs.

         */
        getMultiple: function(keys) {
            if (!Array.isArray(keys)) {
                console.error('StorageHelper.getMultiple: keys must be an array');
                return {};
            }

            var result = {};
            for (var i = 0; i < keys.length; i++) {
                var key = keys[i];
                result[key] = this.get(key);
            }
            return result;
        },

        /**

         * Remove multiple values.

         * @param {Array<string>} keys - Key list.

         * @returns {boolean} Whether all values were removed.

         */
        removeMultiple: function(keys) {
            if (!Array.isArray(keys)) {
                console.error('StorageHelper.removeMultiple: keys must be an array');
                return false;
            }

            var success = true;
            for (var i = 0; i < keys.length; i++) {
                if (!this.remove(keys[i])) {
                    success = false;
                }
            }
            return success;
        },

        /**

         * Check whether localStorage is available.

         * @returns {boolean} Whether localStorage is available.

         */
        isAvailable: function() {
            try {
                var test = '__storage_test__';
                localStorage.setItem(test, test);
                localStorage.removeItem(test);
                return true;
            } catch (error) {
                return false;
            }
        },

        /**

         * Return storage usage information.

         * @returns {Object} {used: number, total: number, percentage: number}

         */
        getStorageInfo: function() {
            var used = 0;
            var total = 5 * 1024 * 1024; // Approximate 5 MB capacity; browsers differ.

            try {
                for (var key in localStorage) {
                    if (localStorage.hasOwnProperty(key)) {
                        used += localStorage[key].length + key.length;
                    }
                }
            } catch (error) {
                console.error('StorageHelper.getStorageInfo error:', error);
            }

            return {
                used: used,
                total: total,
                percentage: ((used / total) * 100).toFixed(2)
            };
        }
    };

    // Expose the helper through the global namespace.
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.StorageHelper = StorageHelper;

})(window);

