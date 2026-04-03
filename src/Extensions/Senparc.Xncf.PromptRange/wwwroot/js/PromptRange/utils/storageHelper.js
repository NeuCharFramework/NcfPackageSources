/**
 * LocalStorage storage auxiliary tool
 * Provide simplified local storage operations and automatically handle JSON serialization
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Storage auxiliary toolset
     * Use object literal mode (no instantiation required)
     */
    var StorageHelper = {
        /**
         * Save data to localStorage
         * Automatic JSON serialization
         * @param {string} key - key name
         * @param {*} value - value (automatically JSON serialized)
         * @returns {boolean} Whether the save was successful
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
         * Read data from localStorage
         * Automatic JSON deserialization
         * @param {string} key - key name
         * @param {*} [defaultValue=null] - Default value (returned when the key does not exist or parsing fails)
         * @returns {*} read value or default value
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
         * Remove data from localStorage
         * @param {string} key - key name
         * @returns {boolean} Whether the removal was successful
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
         * Clear all localStorage data
         * @returns {boolean} Whether the clearing is successful
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
         * Check if the key exists
         * @param {string} key - key name
         * @returns {boolean} whether the key exists
         */
        has: function(key) {
            if (!key) {
                return false;
            }
            return localStorage.getItem(key) !== null;
        },

        /**
         * Get all key names
         * @returns {Array<string>} Array of all key names
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
         * Get the amount of stored data
         * @returns {number} The number of stored data
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
         * Get all data
         * @returns {Object} An object containing all key-value pairs
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
         * Batch set data
         * @param {Object} data - key-value pair object
         * @returns {boolean} Whether all settings are successful
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
         * Get data in batches
         * @param {Array<string>} keys - array of key names
         * @returns {Object} An object containing the requested key-value pairs
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
         * Remove data in batches
         * @param {Array<string>} keys - array of key names
         * @returns {boolean} Whether all removals were successful
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
         * Check if localStorage is available
         * @returns {boolean} whether available
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
         * Get storage space usage
         * @returns {Object} {used: number, total: number, percentage: number}
         */
        getStorageInfo: function() {
            var used = 0;
            var total = 5 * 1024 * 1024; // Assuming 5MB total capacity (may vary between browsers)

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

    // Exposed to the global namespace
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.StorageHelper = StorageHelper;

})(window);

