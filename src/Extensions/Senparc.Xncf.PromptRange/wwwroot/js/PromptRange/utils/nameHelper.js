/**
 * Name lookup helper.
 * Provides consistent name lookup without duplicated code.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**

     * Name lookup methods.

     * Uses an object-literal pattern and requires no instantiation.

     */
    var NameHelper = {
        /**

         * Find a name by ID.

         * @param {Array} options - Option list.

         * @param {string|number} id - ID to find.

         * @param {string} [defaultName='Unknown'] - Fallback name.

         * @param {string} [valueKey='value'] - ID field.

         * @param {string} [labelKey='label'] - Name field.

         * @returns {string} Matching or fallback name.

         */
        getName: function(options, id, defaultName, valueKey, labelKey) {
            // Apply defaults.
            defaultName = defaultName || ncfST('未知');
            valueKey = valueKey || 'value';
            labelKey = labelKey || 'label';
            
            // Validate input.
            if (!options || !id) {
                return defaultName;
            }

            // Non-array input cannot be searched.
            if (!Array.isArray(options)) {
                return defaultName;
            }
            
            // Find the matching option.
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

         * Create a bound name lookup function.

         * @param {Array} options - Option list.

         * @param {string} [defaultName='Unknown'] - Fallback name.

         * @returns {Function} Lookup function accepting an ID.

         */
        createGetter: function(options, defaultName) {
            defaultName = defaultName || ncfST('未知');
            var self = this;
            return function(id) {
                return self.getName(options, id, defaultName);
            };
        },

        /**

         * Get names for a list of IDs.

         * @param {Array} options - Option list.

         * @param {Array} ids - ID list.

         * @param {string} [defaultName='Unknown'] - Fallback name.

         * @param {string} [valueKey='value'] - ID field.

         * @param {string} [labelKey='label'] - Name field.

         * @returns {Array} Name array.

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

         * Find an ID by name.

         * @param {Array} options - Option list.

         * @param {string} name - Name to find.

         * @param {string|number} [defaultId=null] - Fallback ID.

         * @param {string} [valueKey='value'] - ID field.

         * @param {string} [labelKey='label'] - Name field.

         * @returns {string|number|null} Matching ID or fallback ID.

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

         * Check whether an ID exists.

         * @param {Array} options - Option list.

         * @param {string|number} id - ID to check.

         * @param {string} [valueKey='value'] - ID field.

         * @returns {boolean} Whether the ID exists.

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

         * Return the complete option for an ID.

         * @param {Array} options - Option list.

         * @param {string|number} id - ID to find.

         * @param {string} [valueKey='value'] - ID field.

         * @returns {Object|null} Matching option or null.

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

    // Expose the helper through the global namespace.
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.NameHelper = NameHelper;

})(window);

