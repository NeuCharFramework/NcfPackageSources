/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

/**

 * Legacy comment translated to English.

 * @param {Function} func

 * @param {number} wait

 * @param {boolean} immediate

 * @return {*}

 */
function debounce(func, wait, immediate) {
  let timeout, args, context, timestamp, result
  const later = function () {
    //Legacy comment translated to English.
    const last = +new Date() - timestamp

    //Legacy comment translated to English.
    if (last < wait && last > 0) {
      timeout = setTimeout(later, wait - last)
    } else {
      timeout = null
      //Legacy comment translated to English.
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
    //Legacy comment translated to English.
    if (!timeout) timeout = setTimeout(later, wait)
    if (callNow) {
      result = func.apply(context, args)
      context = args = null
    }

    return result
  }
}

/**

* Legacy comment translated to English.

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

* Legacy comment translated to English.

* English documentation comment.

*/
function isNumber(val) {
  // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
  return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**

* Legacy comment translated to English.

* English documentation comment.

*/
function isObjEmpty(obj) {
  return Object.keys(obj).length === 0;
}

/**

 * Legacy comment translated to English.

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

  //Legacy comment translated to English.
  if (newWindow && window.focus) {
    newWindow.focus()
  } else if (!newWindow) {
    window.location.assign(url)
  }
}

/**

 * Legacy comment translated to English.

 * English documentation comment.

 */
function simulationAELOperation(url = '', name = '') {
  if (!url) return
  const link = document.createElement('a')
  link.style.display = 'none'
  link.href = url
  if (name) link.download = name
  //Legacy comment translated to English.
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

 * Legacy comment translated to English.

 * English documentation comment.

 */
function getInterfaceQueryStr(queryObj) {
  if (!queryObj) return ''
  //Legacy comment translated to English.
  return Object.entries(queryObj)
    .filter(([key, value]) => {
      //Legacy comment translated to English.
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

 * Legacy comment translated to English.

 * @param {date} dateString

 * @param {string} format

 * English documentation comment.

 */
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm:ss') {
  if (!dateString) return ''
  const dateObject = new Date(dateString);

  const year = dateObject.getFullYear();
  const month = String(dateObject.getMonth() + 1).padStart(2, '0'); //Legacy comment translated to English.
  const day = String(dateObject.getDate()).padStart(2, '0');
  const hours = String(dateObject.getHours()).padStart(2, '0');
  const minutes = String(dateObject.getMinutes()).padStart(2, '0');
  const seconds = String(dateObject.getSeconds()).padStart(2, '0');

  //Legacy comment translated to English.
  return format
    .replace('yyyy', year)
    .replace('MM', month)
    .replace('dd', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds);
};
/**

 * Legacy comment translated to English.

 * English documentation comment.

 * English documentation comment.

 * English documentation comment.

 */
function calculateDuration(startTime, endTime) {
  if (!startTime) return ''
  //Legacy comment translated to English.
  const startDate = new Date(startTime);
  const endDate = endTime ? new Date(endTime) : new Date(); //Legacy comment translated to English.

  //Legacy comment translated to English.
  const durationInMillis = endDate - startDate;

  //Legacy comment translated to English.
  const secondsInMillis = 1000;
  const minutesInMillis = secondsInMillis * 60;
  const hoursInMillis = minutesInMillis * 60;
  const daysInMillis = hoursInMillis * 24;
  const yearsInMillis = daysInMillis * 365; //Legacy comment translated to English.

  //Legacy comment translated to English.
  const years = Math.floor(durationInMillis / yearsInMillis);
  const days = Math.floor((durationInMillis % yearsInMillis) / daysInMillis);
  const hours = Math.floor((durationInMillis % daysInMillis) / hoursInMillis);
  const minutes = Math.floor((durationInMillis % hoursInMillis) / minutesInMillis);
  const seconds = Math.floor((durationInMillis % minutesInMillis) / secondsInMillis);

  //Legacy comment translated to English.
  let durationParts = [];
  if (years > 0) durationParts.push(ncfST('{0} 年', years));
  if (days > 0) durationParts.push(ncfST('{0} 天', days));
  if (hours > 0) durationParts.push(ncfST('{0} 小时', hours));
  if (minutes > 0) durationParts.push(ncfST('{0} 分钟', minutes));
  if (seconds > 0 || durationParts.length === 0) durationParts.push(ncfST('{0} 秒', seconds));

  return durationParts.join(' ');
}

//Legacy comment translated to English.
function arraysEqual(arr1, arr2) {
  return JSON.stringify(arr1) === JSON.stringify(arr2);
}

//Legacy comment translated to English.
function scoreFormatter(score) {
  return score === -1 ? '--' : score.toFixed(1)
}

/**

 * Legacy comment translated to English.

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

