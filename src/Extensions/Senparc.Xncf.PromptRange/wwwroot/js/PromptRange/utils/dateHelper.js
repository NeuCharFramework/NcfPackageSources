/**
 * 日期时间辅助工具
 * 提供日期格式化、相对时间显示等功能
 * 
 * @version 1.0.0
 * @author Senparc
 */
(function(window) {
    'use strict';
    
    /**
     * 日期辅助工具集
     * 使用对象字面量模式（无需实例化）
     */
    var DateHelper = {
        /**
         * 格式化日期
         * 将日期对象、字符串或时间戳格式化为指定格式的字符串
         * @param {Date|string|number} date - 日期对象、字符串或时间戳
         * @param {string} [format='YYYY-MM-DD HH:mm:ss'] - 格式字符串
         * @returns {string} 格式化后的日期字符串
         * 
         * 支持的格式标记:
         * - YYYY: 四位年份
         * - MM: 两位月份 (01-12)
         * - DD: 两位日期 (01-31)
         * - HH: 两位小时 (00-23)
         * - mm: 两位分钟 (00-59)
         * - ss: 两位秒 (00-59)
         */
        formatDate: function(date, format) {
            format = format || 'YYYY-MM-DD HH:mm:ss';
            var d = new Date(date);
            
            // 检查日期是否有效
            if (isNaN(d.getTime())) {
                return '';
            }

            // 辅助函数：补零
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
         * 格式化聊天时间（相对时间）
         * 将时间转换为相对当前时间的描述（如"刚刚"、"5分钟前"）
         * @param {Date|string|number} date - 日期对象、字符串或时间戳
         * @returns {string} 相对时间描述
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
         * 格式化时间字符串
         * 提取并格式化 ISO 8601 时间字符串
         * @param {string} timeStr - 时间字符串
         * @returns {string} 格式化后的时间字符串
         */
        formatTime: function(timeStr) {
            if (!timeStr) return '';
            var match = timeStr.match(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/);
            return match ? this.formatDate(match[0]) : timeStr;
        },

        /**
         * 获取时间戳
         * 返回当前时间的时间戳（毫秒）
         * @returns {number} 时间戳
         */
        getTimestamp: function() {
            return new Date().getTime();
        },

        /**
         * 计算时间差
         * 计算两个时间之间的差值
         * @param {Date|string|number} startDate - 开始时间
         * @param {Date|string|number} endDate - 结束时间
         * @returns {Object} 时间差对象 {days, hours, minutes, seconds, milliseconds}
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
         * 判断是否为今天
         * @param {Date|string|number} date - 要判断的日期
         * @returns {boolean} 是否为今天
         */
        isToday: function(date) {
            var d = new Date(date);
            var today = new Date();
            return d.getFullYear() === today.getFullYear() &&
                   d.getMonth() === today.getMonth() &&
                   d.getDate() === today.getDate();
        },

        /**
         * 判断是否为昨天
         * @param {Date|string|number} date - 要判断的日期
         * @returns {boolean} 是否为昨天
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
         * 获取月份的天数
         * @param {number} year - 年份
         * @param {number} month - 月份 (1-12)
         * @returns {number} 该月的天数
         */
        getDaysInMonth: function(year, month) {
            return new Date(year, month, 0).getDate();
        },

        /**
         * 格式化持续时间
         * 将毫秒数转换为可读的持续时间格式
         * @param {number} milliseconds - 毫秒数
         * @returns {string} 格式化后的持续时间（如 "2小时30分钟"）
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

    // 暴露到全局命名空间
    window.PromptRangeUtils = window.PromptRangeUtils || {};
    window.PromptRangeUtils.DateHelper = DateHelper;

})(window);

