/**
 * LocalStorage 存储辅助工具
 * 提供简化的本地存储操作，自动处理 JSON 序列化
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Storage 辅助工具集
     * 使用对象字面量模式（无需实例化）
     */
    var StorageHelper = {
        /**
         * 保存数据到 localStorage
         * 自动进行 JSON 序列化
         * @param {string} key - 键名
         * @param {*} value - 值（会自动 JSON 序列化）
         * @returns {boolean} 是否保存成功
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
         * 从 localStorage 读取数据
         * 自动进行 JSON 反序列化
         * @param {string} key - 键名
         * @param {*} [defaultValue=null] - 默认值（键不存在或解析失败时返回）
         * @returns {*} 读取到的值或默认值
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
         * 从 localStorage 移除数据
         * @param {string} key - 键名
         * @returns {boolean} 是否移除成功
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
         * 清空所有 localStorage 数据
         * @returns {boolean} 是否清空成功
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
         * 检查键是否存在
         * @param {string} key - 键名
         * @returns {boolean} 键是否存在
         */
        has: function(key) {
            if (!key) {
                return false;
            }
            return localStorage.getItem(key) !== null;
        },

        /**
         * 获取所有键名
         * @returns {Array<string>} 所有键名的数组
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
         * 获取存储数据的数量
         * @returns {number} 存储数据的数量
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
         * 获取所有数据
         * @returns {Object} 包含所有键值对的对象
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
         * 批量设置数据
         * @param {Object} data - 键值对对象
         * @returns {boolean} 是否全部设置成功
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
         * 批量获取数据
         * @param {Array<string>} keys - 键名数组
         * @returns {Object} 包含请求的键值对的对象
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
         * 批量移除数据
         * @param {Array<string>} keys - 键名数组
         * @returns {boolean} 是否全部移除成功
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
         * 检查 localStorage 是否可用
         * @returns {boolean} 是否可用
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
         * 获取存储空间使用情况
         * @returns {Object} {used: number, total: number, percentage: number}
         */
        getStorageInfo: function() {
            var used = 0;
            var total = 5 * 1024 * 1024; // 假设 5MB 总容量（不同浏览器可能不同）

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

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.StorageHelper = StorageHelper;

})(window);

