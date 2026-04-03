/**
 * Date and time auxiliary tools
 * Provides date formatting, relative time display and other functions
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     *Date auxiliary tool set
     * Use object literal mode (no instantiation required)
     */
    var DateHelper = {
        /**
         * Format date
         * Format a date object, string or timestamp into a string in the specified format
         * @param {Date|string|number} date - date object, string or timestamp
         * @param {string} [format='YYYY-MM-DD HH:mm:ss'] - format string
         * @returns {string} Formatted date string
         * 
         *Supported format tags:
         * - YYYY: four-digit year
         * - MM: two digit month (01-12)
         * - DD: two digit date (01-31)
         * - HH: two digit hours (00-23)
         * - mm: two digit minutes (00-59)
         * - ss: two digit seconds (00-59)
         */
        formatDate: function(date, format) {
            format = format || 'YYYY-MM-DD HH:mm:ss';
            var d = new Date(date);
            
            // Check if the date is valid
            if (isNaN(d.getTime())) {
                return '';
            }

            // Auxiliary function: zero padding
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
         * Format chat time (relative time)
         * Convert the time into a description relative to the current time (such as "just", "5 minutes ago")
         * @param {Date|string|number} date - date object, string or timestamp
         * @returns {string} relative time description
         */
        formatChatTime: function(date) {
            var now = new Date();
            var d = new Date(date);
            var diff = now - d;

            var minute = 60 * 1000;
            var hour = 60 * minute;
            var day = 24 * hour;

            if (diff < minute) {
                return '刚刚';
            } else if (diff < hour) {
                return Math.floor(diff / minute) + '分钟前';
            } else if (diff < day) {
                return Math.floor(diff / hour) + '小时前';
            } else if (diff < 2 * day) {
                return '昨天 ' + this.formatDate(d, 'HH:mm');
            } else if (diff < 7 * day) {
                return Math.floor(diff / day) + '天前';
            } else {
                return this.formatDate(d, 'YYYY-MM-DD HH:mm');
            }
        },

        /**
         * Format time string
         * Extract and format ISO 8601 time string
         * @param {string} timeStr - time string
         * @returns {string} Formatted time string
         */
        formatTime: function(timeStr) {
            if (!timeStr) return '';
            var match = timeStr.match(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
            return match ? this.formatDate(match[0]) : timeStr;
        },

        /**
         * Get timestamp
         * Returns the timestamp of the current time (milliseconds)
         * @returns {number} timestamp
         */
        getTimestamp: function() {
            return new Date().getTime();
        },

        /**
         * Calculate time difference
         * Calculate the difference between two times
         * @param {Date|string|number} startDate - start time
         * @param {Date|string|number} endDate - end time
         * @returns {Object} time difference object {days, hours, minutes, seconds, milliseconds}
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
         * Determine whether it is today
         * @param {Date|string|number} date - the date to be judged
         * @returns {boolean} whether it is today
         */
        isToday: function(date) {
            var d = new Date(date);
            var today = new Date();
            return d.getFullYear() === today.getFullYear() &&
                   d.getMonth() === today.getMonth() &&
                   d.getDate() === today.getDate();
        },

        /**
         * Determine whether it is yesterday
         * @param {Date|string|number} date - the date to be judged
         * @returns {boolean} whether it is yesterday
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
         * Get the number of days in the month
         * @param {number} year - year
         * @param {number} month - month (1-12)
         * @returns {number} The number of days in the month
         */
        getDaysInMonth: function(year, month) {
            return new Date(year, month, 0).getDate();
        },

        /**
         *Format duration
         * Convert milliseconds to a readable duration format
         * @param {number} milliseconds - number of milliseconds
         * @returns {string} formatted duration (such as "2 hours and 30 minutes")
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
            if (days > 0) parts.push(days + '天');
            if (hours > 0) parts.push(hours + '小时');
            if (minutes > 0) parts.push(minutes + '分钟');
            if (seconds > 0 && days === 0) parts.push(seconds + '秒');

            return parts.length > 0 ? parts.join('') : '0秒';
        }
    };

    // Exposed to the global namespace
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.DateHelper = DateHelper;

})(window);

