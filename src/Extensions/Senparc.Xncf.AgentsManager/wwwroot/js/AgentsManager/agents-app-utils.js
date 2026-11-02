/*
 * AgentsManager 前端：页面级工具函数（debounce/formatDate 等，供各 agents-app-*.js 运行时调用）。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

/**
 * 节流 防抖
 * @param {Function} func
 * @param {number} wait
 * @param {boolean} immediate
 * @return {*}
 */
function debounce(func, wait, immediate) {
  let timeout, args, context, timestamp, result
  const later = function () {
    // 据上一次触发时间间隔
    const last = +new Date() - timestamp

    // 上次被包装函数被调用时间间隔 last 小于设定时间间隔 wait
    if (last < wait && last > 0) {
      timeout = setTimeout(later, wait - last)
    } else {
      timeout = null
      // 如果设定为immediate===true，因为开始边界已经调用过了此处无需调用
      if (!immediate) {
        result = func.apply(context, args)
        if (!timeout) context = args = null
      }
    }
  }

  return function (...args) {
    context = this
    timestamp = +new Date()
    const callNow = immediate && !timeout
    // 如果延时不存在，重新设定延时
    if (!timeout) timeout = setTimeout(later, wait)
    if (callNow) {
      result = func.apply(context, args)
      context = args = null
    }

    return result
  }
}

/**
* 克隆
* @param {Object} source
* @returns {Object}
*/
function deepClone(source) {
  if (!source && typeof source !== 'object') {
    throw new Error('error arguments', 'deepClone')
  }
  const targetObj = source.constructor === Array ? [] : {}
  Object.keys(source).forEach(keys => {
    if (source[keys] && typeof source[keys] === 'object') {
      targetObj[keys] = deepClone(source[keys])
    } else {
      targetObj[keys] = source[keys]
    }
  })
  return targetObj
}

/**
* 判断值是否 数字
* @param {*} val 需要判断的变量
*/
function isNumber(val) {
  // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
  return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**
* 判断值是否是 空对象
* @param {*} val 需要判断的变量
*/
function isObjEmpty(obj) {
  return Object.keys(obj).length === 0;
}

/**
 * 打开 window窗口
 * @param {Sting} url
 * @param {Sting} title
 * @param {Number} w
 * @param {Number} h
 */
function openWindow(url, title, w, h) {
  // Fixes dual-screen position                            Most browsers       Firefox
  const dualScreenLeft = window.screenLeft !== undefined ? window.screenLeft : screen.left
  const dualScreenTop = window.screenTop !== undefined ? window.screenTop : screen.top

  const width = window.innerWidth ? window.innerWidth : document.documentElement.clientWidth ? document.documentElement.clientWidth : screen.width
  const height = window.innerHeight ? window.innerHeight : document.documentElement.clientHeight ? document.documentElement.clientHeight : screen.height

  const left = ((width / 2) - (w / 2)) + dualScreenLeft
  const top = ((height / 2) - (h / 2)) + dualScreenTop
  const newWindow = window.open(url, title, 'toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no, resizable=yes, copyhistory=no, width=' + w + ', height=' + h + ', top=' + top + ', left=' + left)

  // WKWebView 可能不创建新窗口，此时回退为当前窗口导航。
  if (newWindow && window.focus) {
    newWindow.focus()
  } else if (!newWindow) {
    window.location.assign(url)
  }
}

/**
 * 模拟 a 标签
 * @param {string} url // 原地址
 */
function simulationAELOperation(url = '', name = '') {
  if (!url) return
  const link = document.createElement('a')
  link.style.display = 'none'
  link.href = url
  if (name) link.download = name
  // 不强制 _blank：macOS WKWebView 未实现新窗口委托，强制新窗口会导致点击无响应。
  link.click()
  link.remove()
}

async function copyTextForEmbeddedBrowser(text) {
  if (!text) return false

  if (window.isSecureContext && navigator.clipboard && navigator.clipboard.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch (error) {
      console.warn('Clipboard API is unavailable, using the compatibility fallback.', error)
    }
  }

  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.setAttribute('readonly', 'readonly')
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.focus()
  textarea.select()
  textarea.setSelectionRange(0, text.length)
  try {
    return document.execCommand('copy')
  } catch (error) {
    console.error('Copy fallback failed:', error)
    return false
  } finally {
    textarea.remove()
  }
}

/**
 * 处理接口 query 参数 转换为 string
 * @param {Object} queryObj // 原地址
 */
function getInterfaceQueryStr(queryObj) {
  if (!queryObj) return ''
  // 将对象转换为 URL 参数字符串
  return Object.entries(queryObj)
    .filter(([key, value]) => {
      // 过滤掉空值
      // console.log('value', typeof value)
      if (typeof value === 'string') {
        return value !== ''
      } else if (typeof value === 'object' && value instanceof Array) {
        return value.length > 0
      } else if (typeof value === 'number') {
        return true
      } else if (typeof value === 'boolean') {
        return true
      } else {
        // if(typeof value === 'undefined')
        return false
      }
    })
    .map(
      ([key, value]) => {
        if (Array.isArray(value)) {
          let str = ""
          for (let index in value) {
            str += `${index > 0 ? '&' : ''}${encodeURIComponent(key)}=${encodeURIComponent(value[index])}`
          }
          return str
        }
        return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
      }
    )
    .join('&')
}

/**
 * 日期格式化为 yyyy-MM-dd HH:mm:ss
 * @param {date} dateString
 * @param {string} format
 * @returns {string} - 格式化后的时间
 */
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm:ss') {
  if (!dateString) return ''
  const dateObject = new Date(dateString);

  const year = dateObject.getFullYear();
  const month = String(dateObject.getMonth() + 1).padStart(2, '0'); // 月份从0开始
  const day = String(dateObject.getDate()).padStart(2, '0');
  const hours = String(dateObject.getHours()).padStart(2, '0');
  const minutes = String(dateObject.getMinutes()).padStart(2, '0');
  const seconds = String(dateObject.getSeconds()).padStart(2, '0');

  // 替换格式中的标识符
  return format
    .replace('yyyy', year)
    .replace('MM', month)
    .replace('dd', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds);
};
/**
 * 计算持续时间
 * @param {string} startTime - 开始时间字符串（ISO格式）
 * @param {string} [endTime] - 结束时间字符串（ISO格式），可选
 * @returns {string} - 持续时间字符串，根据差值级别动态显示
 */
function calculateDuration(startTime, endTime) {
  if (!startTime) return ''
  // 将开始时间和结束时间转换为 Date 对象
  const startDate = new Date(startTime);
  const endDate = endTime ? new Date(endTime) : new Date(); // 如果没有结束时间，则使用当前时间

  // 计算时间差（以毫秒为单位）
  const durationInMillis = endDate - startDate;

  // 各个时间单位的毫秒值
  const secondsInMillis = 1000;
  const minutesInMillis = secondsInMillis * 60;
  const hoursInMillis = minutesInMillis * 60;
  const daysInMillis = hoursInMillis * 24;
  const yearsInMillis = daysInMillis * 365; // 假设一年365天

  // 计算各个时间单位
  const years = Math.floor(durationInMillis / yearsInMillis);
  const days = Math.floor((durationInMillis % yearsInMillis) / daysInMillis);
  const hours = Math.floor((durationInMillis % daysInMillis) / hoursInMillis);
  const minutes = Math.floor((durationInMillis % hoursInMillis) / minutesInMillis);
  const seconds = Math.floor((durationInMillis % minutesInMillis) / secondsInMillis);

  // 动态构建输出字符串
  let durationParts = [];
  if (years > 0) durationParts.push(ncfST('{0} 年', years));
  if (days > 0) durationParts.push(ncfST('{0} 天', days));
  if (hours > 0) durationParts.push(ncfST('{0} 小时', hours));
  if (minutes > 0) durationParts.push(ncfST('{0} 分钟', minutes));
  if (seconds > 0 || durationParts.length === 0) durationParts.push(ncfST('{0} 秒', seconds));

  return durationParts.join(' ');
}

// 简单对比 数组是否相等
function arraysEqual(arr1, arr2) {
  return JSON.stringify(arr1) === JSON.stringify(arr2);
}

// prompt 分数处理
function scoreFormatter(score) {
  return score === -1 ? '--' : score.toFixed(1)
}

/**
 * 加载载 模拟json 数据
 */
// function funcMockJson() {
//     return fetch("/json/AgentsManager/data.json")
//         .then((res) => {
//             return res.json();
//         })
// }

function sanitizeTaskHtml(value) {
  const html = String(value ?? '')
  if (typeof DOMPurify !== 'undefined') {
    return DOMPurify.sanitize(html)
  }

  const template = document.createElement('template')
  template.innerHTML = html
  template.content.querySelectorAll('script,style,iframe,object,embed,applet,base,form,meta,link').forEach(node => node.remove())
  template.content.querySelectorAll('*').forEach(element => {
    Array.from(element.attributes).forEach(attribute => {
      const name = attribute.name.toLowerCase()
      if (name.startsWith('on') || name === 'style' || name === 'srcdoc') {
        element.removeAttribute(attribute.name)
        return
      }

      if (['href', 'src', 'action', 'formaction', 'xlink:href'].includes(name)) {
        try {
          const url = new URL(attribute.value, document.baseURI)
          if (url.protocol !== 'http:' && url.protocol !== 'https:' && !attribute.value.trim().startsWith('#')) {
            element.removeAttribute(attribute.name)
          }
        } catch (error) {
          element.removeAttribute(attribute.name)
        }
      }
    })
  })
  return template.innerHTML
}

