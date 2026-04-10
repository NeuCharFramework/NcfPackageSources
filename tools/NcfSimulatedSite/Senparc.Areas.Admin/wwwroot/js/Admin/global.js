// Logout
Vue.prototype.loginout = function () {
    window.location.href = '/Admin/Login?handler=Logout&ReturnUrl=' + escape(window.location.pathname + window.location.search);
};

var ncfI18n = window.ncfI18n || {};

// Format add time etc. 2020 - 06 - 19T09: 41: 51.1905692
function formaTableTime(value) {
    return value ? value.replace('T', '  ').substr(0, 17) : (ncfI18n.noTime || 'No time');
}

/**
 * Copy content
 * @param {any} value The value to copy
 * @param {any} toastText Toast message
 */
function copyToClipboard(value, toastText) {
    $('#clipboardContainer').val(value);
    $('#clipboardContainer').select();
    toastText = toastText || (ncfI18n.copySuccess || 'Copied! Use Ctrl+V to paste.');
    try {
        document.execCommand("copy"); // Execute browser copy command
        base.swal.toast(toastText);
    } finally {
        //Execute Finally
    }
}

// Parse URL query parameters
function resizeUrl() {
    let url = window.location.href;
    let obj = {};
    let reg = /[?&][^?&]+=[^?&]+/g;
    let arr = url.match(reg); // return ["?id=123456","&a=b"]
    if (arr) {
        arr.forEach((item) => {
            let tempArr = item.substring(1).split('=');
            let key = tempArr[0];
            let val = tempArr[1];
            obj[key] = decodeURIComponent(val);
        });
    }
    return obj;
}

/**
 * Round a number to 2 decimal places and format as currency
 * @param {any} num Number value (Number or String)
 * @returns {any} Currency formatted string, e.g. '1,234,567.45'
 */
function formatCurrency(num) {
    num = num.toString().replace(/\$|\,/g, '');
    if (isNaN(num))
        num = "0";
    sign = num === (num = Math.abs(num));
    num = Math.floor(num * 100 + 0.50000000001);
    cents = num % 100;
    num = Math.floor(num / 100).toString();
    if (cents < 10)
        cents = "0" + cents;
    for (var i = 0; i < Math.floor((num.length - (1 + i)) / 3); i++)
        num = num.substring(0, num.length - (4 * i + 3)) + ',' + num.substring(num.length - (4 * i + 3));
    return ((sign ? '' : '-') + num + '.' + cents);
}