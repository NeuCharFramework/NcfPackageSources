/**
 * 名称查询辅助工具
 * 提供统一的名称查询功能，避免代码重复
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * 名称查询辅助工具集
     * 使用对象字面量模式（无需实例化）
     */
    var NameHelper = {
        /**
         * 通用的名称查询方法
         * 从选项数组中根据 ID 查找对应的名称
         * @param {Array} options - 选项数组
         * @param {string|number} id - 要查询的 ID
         * @param {string} [defaultName='未知'] - 默认名称（找不到时返回）
         * @param {string} [valueKey='value'] - ID 字段名
         * @param {string} [labelKey='label'] - 名称字段名
         * @returns {string} 查找到的名称或默认名称
         */
        getName: function(options, id, defaultName, valueKey, labelKey) {
            // 设置默认值
            defaultName = defaultName || '未知';
            valueKey = valueKey || 'value';
            labelKey = labelKey || 'label';
            
            // 验证输入
            if (!options || !id) {
                return defaultName;
            }

            // 如果不是数组，返回默认值
            if (!Array.isArray(options)) {
                return defaultName;
            }
            
            // 查找匹配的项
            var item = null;
            for (var i = 0; i < options.length; i++) {
                if (options[i][valueKey] === id) {
                    item = options[i];
                    break;
                }
            }
            
            return item ? item[labelKey] : defaultName;
        },

        /**
         * 创建名称查询器
         * 返回一个绑定了特定选项数组的查询函数
         * @param {Array} options - 选项数组
         * @param {string} [defaultName='未知'] - 默认名称
         * @returns {Function} 查询函数，接受 id 参数，返回对应名称
         */
        createGetter: function(options, defaultName) {
            defaultName = defaultName || '未知';
            var self = this;
            return function(id) {
                return self.getName(options, id, defaultName);
            };
        },

        /**
         * 批量获取名称
         * 根据 ID 数组批量查询名称
         * @param {Array} options - 选项数组
         * @param {Array} ids - ID 数组
         * @param {string} [defaultName='未知'] - 默认名称
         * @param {string} [valueKey='value'] - ID 字段名
         * @param {string} [labelKey='label'] - 名称字段名
         * @returns {Array} 名称数组
         */
        getNames: function(options, ids, defaultName, valueKey, labelKey) {
            if (!Array.isArray(ids)) {
                return [];
            }

            var self = this;
            var names = [];
            for (var i = 0; i < ids.length; i++) {
                names.push(self.getName(options, ids[i], defaultName, valueKey, labelKey));
            }
            return names;
        },

        /**
         * 根据名称查找 ID
         * 与 getName 相反，根据名称查找对应的 ID
         * @param {Array} options - 选项数组
         * @param {string} name - 要查询的名称
         * @param {string|number} [defaultId=null] - 默认 ID（找不到时返回）
         * @param {string} [valueKey='value'] - ID 字段名
         * @param {string} [labelKey='label'] - 名称字段名
         * @returns {string|number|null} 查找到的 ID 或默认 ID
         */
        getId: function(options, name, defaultId, valueKey, labelKey) {
            defaultId = defaultId !== undefined ? defaultId : null;
            valueKey = valueKey || 'value';
            labelKey = labelKey || 'label';
            
            if (!options || !name) {
                return defaultId;
            }

            if (!Array.isArray(options)) {
                return defaultId;
            }
            
            var item = null;
            for (var i = 0; i < options.length; i++) {
                if (options[i][labelKey] === name) {
                    item = options[i];
                    break;
                }
            }
            
            return item ? item[valueKey] : defaultId;
        },

        /**
         * 检查 ID 是否存在
         * @param {Array} options - 选项数组
         * @param {string|number} id - 要检查的 ID
         * @param {string} [valueKey='value'] - ID 字段名
         * @returns {boolean} ID 是否存在
         */
        hasId: function(options, id, valueKey) {
            valueKey = valueKey || 'value';
            
            if (!options || !id) {
                return false;
            }

            if (!Array.isArray(options)) {
                return false;
            }
            
            for (var i = 0; i < options.length; i++) {
                if (options[i][valueKey] === id) {
                    return true;
                }
            }
            
            return false;
        },

        /**
         * 获取完整的选项对象
         * 根据 ID 获取完整的选项对象（不仅仅是名称）
         * @param {Array} options - 选项数组
         * @param {string|number} id - 要查询的 ID
         * @param {string} [valueKey='value'] - ID 字段名
         * @returns {Object|null} 找到的选项对象或 null
         */
        getOption: function(options, id, valueKey) {
            valueKey = valueKey || 'value';
            
            if (!options || !id) {
                return null;
            }

            if (!Array.isArray(options)) {
                return null;
            }
            
            for (var i = 0; i < options.length; i++) {
                if (options[i][valueKey] === id) {
                    return options[i];
                }
            }
            
            return null;
        }
    };

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.NameHelper = NameHelper;

})(window);

