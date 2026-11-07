(function (global) {
    'use strict';

    function parseJson(value, fallback) {
        try {
            return value ? JSON.parse(value) : fallback;
        } catch (error) {
            console.warn('NeuCharWorkflow JSON parse failed:', error);
            return fallback;
        }
    }

    function unwrap(response) {
        if (!response || !response.data) {
            return null;
        }
        return Object.prototype.hasOwnProperty.call(response.data, 'data')
            ? response.data.data
            : response.data;
    }

    function normalizeParameterSchema(parameters) {
        return Array.isArray(parameters) ? parameters.map((parameter, index) => {
            const normalized = parameter && typeof parameter === 'object' ? { ...parameter } : {};

            // parameterSchemaJson is a JSON string nested in the Designer API response.
            // Older servers serialized that string with .NET's PascalCase defaults, while the
            // Vue UI consumes camelCase. Normalize both contracts before deciding metadata is
            // missing: a real `Name` must never turn into the draft-only `parameter_1` fallback.
            const aliases = {
                name: ['Name'],
                title: ['Title'],
                description: ['Description'],
                required: ['Required'],
                parameterType: ['ParameterType'],
                systemType: ['SystemType'],
                maxLength: ['MaxLength'],
                filterable: ['Filterable'],
                allowCreate: ['AllowCreate'],
                hasSyntheticName: ['HasSyntheticName'],
                metadataError: ['MetadataError'],
                defaultValue: ['DefaultValue'],
                options: ['Options'],
                allowMultiple: ['AllowMultiple'],
                maxFiles: ['MaxFiles'],
                accept: ['Accept']
            };
            Object.keys(aliases).forEach(key => {
                if (Object.prototype.hasOwnProperty.call(normalized, key)) return;
                const legacyKey = aliases[key].find(candidate =>
                    Object.prototype.hasOwnProperty.call(normalized, candidate));
                if (legacyKey) normalized[key] = normalized[legacyKey];
            });
            if (Array.isArray(normalized.options)) {
                normalized.options = normalized.options.map(option => {
                    const result = option && typeof option === 'object' ? { ...option } : {};
                    [['value', 'Value'], ['text', 'Text'], ['note', 'Note'], ['defaultSelected', 'DefaultSelected']]
                        .forEach(([key, legacyKey]) => {
                            if (!Object.prototype.hasOwnProperty.call(result, key) &&
                                Object.prototype.hasOwnProperty.call(result, legacyKey)) {
                                result[key] = result[legacyKey];
                            }
                        });
                    return result;
                });
            }
            const name = String(normalized.name || '').trim();
            if (name) {
                normalized.name = name;
                return normalized;
            }

            // Older or faulty XNCF metadata can omit Name. Keep the parameter operable in a
            // draft with a deterministic key, but mark it so the runtime can refuse execution.
            normalized.name = `parameter_${index + 1}`;
            normalized.title = String(normalized.title || '').trim() || `参数 ${index + 1}`;
            normalized.hasSyntheticName = true;
            normalized.metadataError = normalized.metadataError ||
                'Function 参数元数据缺少字段名；当前仅可保存草稿，修复或更新模块后才能运行。';
            return normalized;
        }) : [];
    }

    function createParameterValues(fn) {
        const schema = normalizeParameterSchema(parseJson(fn.parameterSchemaJson, []));
        const defaults = parseJson(fn.defaultParametersJson, {});
        const values = {};
        schema.forEach(parameter => {
            const defaultKey = Object.keys(defaults).find(key =>
                String(key).toLowerCase() === String(parameter.name).toLowerCase());
            const value = defaultKey
                ? defaults[defaultKey]
                : parameter.defaultValue;
            values[parameter.name] = coerceParameterValue(parameter, value);
        });
        return values;
    }

    function coerceParameterValue(parameter, value) {
        if (!parameter) {
            return value;
        }
        if (parameter.parameterType === 2) {
            if (!Array.isArray(value)) {
                value = typeof value === 'string'
                    ? value.split(/[;,，；\n\r|]+/).map(item => item.trim()).filter(Boolean)
                    : [];
            }
            if (value.length === 0 && Array.isArray(parameter.options)) {
                const selected = parameter.options
                    .filter(option => option && option.defaultSelected)
                    .map(option => option.value);
                if (selected.length) value = selected;
            }
            return value;
        }
        if (parameter.parameterType === 1 &&
            (value === null || typeof value === 'undefined' || value === '')) {
            const options = Array.isArray(parameter.options) ? parameter.options : [];
            const selected = options.find(option => option && option.defaultSelected);
            const only = parameter.required && options.length === 1 ? options[0] : null;
            return selected ? selected.value : (only ? only.value : '');
        }
        if (parameter.parameterType === 4) {
            return value === true || value === 'true' || value === 'True';
        }
        if (parameter.parameterType === 6) {
            return parameter.allowMultiple ? (Array.isArray(value) ? value : []) : (value || '');
        }
        return value === null || typeof value === 'undefined' ? '' : value;
    }

    function defaultParameterValue(parameter) {
        return coerceParameterValue(parameter, parameter ? parameter.defaultValue : '');
    }

    function firstMissingRequired(fn, values) {
        return normalizeParameterSchema(parseJson(fn.parameterSchemaJson, [])).find(parameter => {
            if (!parameter.required) {
                return false;
            }
            const value = values[parameter.name];
            return value === null || typeof value === 'undefined' || value === '' ||
                (Array.isArray(value) && value.length === 0);
        });
    }

    function sanitizeHtml(value) {
        if (!global.DOMPurify || typeof global.DOMPurify.sanitize !== 'function') {
            return '';
        }

        return global.DOMPurify.sanitize(String(value == null ? '' : value), {
            ALLOWED_TAGS: [
                'a', 'b', 'blockquote', 'br', 'code', 'div', 'em',
                'h1', 'h2', 'h3', 'h4', 'h5', 'h6', 'hr', 'i',
                'li', 'mark', 'ol', 'p', 'pre', 'small', 'span',
                'strong', 'sub', 'sup', 'table', 'tbody', 'td',
                'th', 'thead', 'tr', 'u', 'ul'
            ],
            ALLOWED_ATTR: ['href', 'title'],
            ALLOW_ARIA_ATTR: false,
            ALLOW_DATA_ATTR: false,
            ALLOWED_URI_REGEXP: /^https?:\/\/[^\s<>"']+$/i
        });
    }

    global.NeuCharWorkflowUi = Object.freeze({
        parseJson,
        unwrap,
        normalizeParameterSchema,
        createParameterValues,
        defaultParameterValue,
        firstMissingRequired,
        sanitizeHtml
    });
})(window);
