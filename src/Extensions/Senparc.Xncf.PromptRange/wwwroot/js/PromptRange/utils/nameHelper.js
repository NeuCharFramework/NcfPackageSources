/**
 * Name query auxiliary tool
 * Provide a unified name query function to avoid code duplication
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Name query auxiliary tool set
     * Use object literal mode (no instantiation required)
     */
    var NameHelper = {
        /**
         * Universal name query method
         * Find the corresponding name based on ID from the options array
         * @param {Array} options - array of options
         * @param {string|number} id - the ID to be queried
         * @param {string} [defaultName='Unknown'] - Default name (returned if not found)
         * @param {string} [valueKey='value'] - ID field name
         * @param {string} [labelKey='label'] - Name field name
         * @returns {string} The found name or the default name
         */
        getName: function(options, id, defaultName, valueKey, labelKey) {
            // Set default value
            defaultName = defaultName || '未知';
            valueKey = valueKey || 'value';
            labelKey = labelKey || 'label';
            
            // Validate input
            if (!options || !id) {
                return defaultName;
            }

            // If not an array, returns the default value
            if (!Array.isArray(options)) {
                return defaultName;
            }
            
            // Find matching items
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
         * Create name query
         * Returns a query function bound to a specific option array
         * @param {Array} options - array of options
         * @param {string} [defaultName='Unknown'] - default name
         * @returns {Function} query function, accepts the id parameter and returns the corresponding name
         */
        createGetter: function(options, defaultName) {
            defaultName = defaultName || '未知';
            var self = this;
            return function(id) {
                return self.getName(options, id, defaultName);
            };
        },

        /**
         * Get names in batches
         * Query names in batches based on ID array
         * @param {Array} options - array of options
         * @param {Array} ids - ID array
         * @param {string} [defaultName='Unknown'] - default name
         * @param {string} [valueKey='value'] - ID field name
         * @param {string} [labelKey='label'] - Name field name
         * @returns {Array} name array
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
         * Find ID based on name
         * Contrary to getName, find the corresponding ID based on the name
         * @param {Array} options - array of options
         * @param {string} name - the name to be queried
         * @param {string|number} [defaultId=null] - Default ID (returned if not found)
         * @param {string} [valueKey='value'] - ID field name
         * @param {string} [labelKey='label'] - Name field name
         * @returns {string|number|null} The found ID or default ID
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
         * Check if ID exists
         * @param {Array} options - array of options
         * @param {string|number} id - the ID to check
         * @param {string} [valueKey='value'] - ID field name
         * @returns {boolean} Whether the ID exists
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
         * Get the complete options object
         * Get the complete option object based on ID (not just the name)
         * @param {Array} options - array of options
         * @param {string|number} id - the ID to be queried
         * @param {string} [valueKey='value'] - ID field name
         * @returns {Object|null} the option object found or null
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

    // Exposed to the global namespace
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.NameHelper = NameHelper;

})(window);

