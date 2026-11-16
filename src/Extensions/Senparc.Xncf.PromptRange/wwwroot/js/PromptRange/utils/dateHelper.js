/**
 * Date and time helper.
 * Provides date formatting and relative-time display.
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * Date helper methods.
     * Uses an object-literal pattern and requires no instantiation.
     */
    var DateHelper = {
        /**
         * Format a date.
         * @param {Date|string|number} date - Date object, string, or timestamp.
         * @param {string} [format='YYYY-MM-DD HH:mm:ss'] - Format string.
         * @returns {string} Formatted date.
         * 
         * Supported tokens:
         * - YYYY: four-digit year
         * - MM: two-digit month (01-12)
         * - DD: two-digit day (01-31)
         * - HH: two-digit hour (00-23)
         * - mm: two-digit minute (00-59)
         * - ss: two-digit second (00-59)
         */
        formatDate: function(date, format) {
            format = format || 'YYYY-MM-DD HH:mm:ss';
            var d = new Date(date);
            
            // Reject invalid dates.
            if (isNaN(d.getTime())) {
                return '';
            }

            // Pad a number with a leading zero.
            function pad(num) {
                return num < 10 ? '0' + num : '' + num;
            }

            var map = {
                'YYYY': d.getFullYear(),
                'MM': pad(d.getMonth() + 1),
                'DD': pad(d.getDate()),
                'HH': pad(d.getHours()),
                'mm': pad(d.getMinutes()),
                'ss': pad(d.getSeconds())
            };

            return format.replace(/YYYY|MM|DD|HH|mm|ss/g, function(match) {
                return map[match];
            });
        },

        /**
         * Format a chat timestamp as relative time.
         * @param {Date|string|number} date - Date object, string, or timestamp.
         * @returns {string} Relative time description.
         */
        formatChatTime: function(date) {
            var now = new Date();
            var d = new Date(date);
            var diff = now - d;

            var minute = 60 * 1000;
            var hour = 60 * minute;
            var day = 24 * hour;

            if (diff < minute) {
                return ncfST('刚刚');
            } else if (diff < hour) {
                return ncfST('{0}分钟前', Math.floor(diff / minute));
            } else if (diff < day) {
                return ncfST('{0}小时前', Math.floor(diff / hour));
            } else if (diff < 2 * day) {
                return ncfST('昨天 {0}', this.formatDate(d, 'HH:mm'));
            } else if (diff < 7 * day) {
                return ncfST('{0}天前', Math.floor(diff / day));
            } else {
                return this.formatDate(d, 'YYYY-MM-DD HH:mm');
            }
        },

        /**
         * Format an ISO 8601 time string.
         * @param {string} timeStr - Time string.
         * @returns {string} Formatted time string.
         */
        formatTime: function(timeStr) {
            if (!timeStr) return '';
            var match = timeStr.match(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
            return match ? this.formatDate(match[0]) : timeStr;
        },

        /**
         * Return the current timestamp in milliseconds.
         * @returns {number} Timestamp in milliseconds.
         */
        getTimestamp: function() {
            return new Date().getTime();
        },

        /**
         * Calculate the difference between two dates.
         * @param {Date|string|number} startDate - Start date.
         * @param {Date|string|number} endDate - End date.
         * @returns {Object} Difference object: {days, hours, minutes, seconds, milliseconds}.
         */
        getTimeDiff: function(startDate, endDate) {
            var start = new Date(startDate);
            var end = new Date(endDate);
            var diff = Math.abs(end - start);

            return {
                milliseconds: diff,
                seconds: Math.floor(diff / 1000),
                minutes: Math.floor(diff / (1000 * 60)),
                hours: Math.floor(diff / (1000 * 60 * 60)),
                days: Math.floor(diff / (1000 * 60 * 60 * 24))
            };
        },

        /**
         * Check whether a date is today.
         * @param {Date|string|number} date - Date to check.
         * @returns {boolean} Whether the date is today.
         */
        isToday: function(date) {
            var d = new Date(date);
            var today = new Date();
            return d.getFullYear() === today.getFullYear() &&
                   d.getMonth() === today.getMonth() &&
                   d.getDate() === today.getDate();
        },

        /**
         * Check whether a date is yesterday.
         * @param {Date|string|number} date - Date to check.
         * @returns {boolean} Whether the date is yesterday.
         */
        isYesterday: function(date) {
            var d = new Date(date);
            var yesterday = new Date();
            yesterday.setDate(yesterday.getDate() - 1);
            return d.getFullYear() === yesterday.getFullYear() &&
                   d.getMonth() === yesterday.getMonth() &&
                   d.getDate() === yesterday.getDate();
        },

        /**
         * Return the number of days in a month.
         * @param {number} year - Year.
         * @param {number} month - Month (1-12).
         * @returns {number} Number of days.
         */
        getDaysInMonth: function(year, month) {
            return new Date(year, month, 0).getDate();
        },

        /**
         * Format a duration.
         * @param {number} milliseconds - Duration in milliseconds.
         * @returns {string} Readable duration such as "2 hours 30 minutes".
         */
        formatDuration: function(milliseconds) {
            var seconds = Math.floor(milliseconds / 1000);
            var minutes = Math.floor(seconds / 60);
            var hours = Math.floor(minutes / 60);
            var days = Math.floor(hours / 24);

            seconds = seconds % 60;
            minutes = minutes % 60;
            hours = hours % 24;

            var parts = [];
            if (days > 0) parts.push(ncfST('{0}天', days));
            if (hours > 0) parts.push(ncfST('{0}小时', hours));
            if (minutes > 0) parts.push(ncfST('{0}分钟', minutes));
            if (seconds > 0 && days === 0) parts.push(ncfST('{0}秒', seconds));

            return parts.length > 0 ? parts.join('') : ncfST('0秒');
        }
    };

    // Expose the helper through the global namespace.
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.DateHelper = DateHelper;

})(window);

