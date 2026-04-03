/**
 * @param {Function} func
 * @param {number} wait
 * @param {boolean} immediate
 * @return {*}
 */
function debounce(func, wait, immediate) {
  let timeout, args, context, timestamp, result
  const later = function () {
    // According to the last trigger time interval
    const last = +new Date() - timestamp

    // The time interval between the last time the wrapped function was called last is less than the set time interval wait
    if (last < wait && last > 0) {
      timeout = setTimeout(later, wait - last)
    } else {
      timeout = null
      // If set to immediate===true, there is no need to call here because the starting boundary has already been called.
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
    // If the delay does not exist, reset the delay
    if (!timeout) timeout = setTimeout(later, wait)
    if (callNow) {
      result = func.apply(context, args)
      context = args = null
    }

    return result
  }
}

/**
* This is just a simple version of deep copy
* Has a lot of edge cases bug
* If you want to use a perfect deep copy, use lodash's _.cloneDeep
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
* Determine whether the value is a number
* @param {*} val variable to be judged
*/
function isNumber(val) {
  // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
  return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**
* Determine whether the value is an empty object
* @param {*} val variable to be judged
*/
function isObjEmpty(obj) {
  return Object.keys(obj).length === 0;
}
var app = new Vue({
  el: "#app",
  data() {
    return {
      devHost: 'http://pr-felixj.frp.senparc.com',
      elSize: 'medium', // el component size defaults to empty medium, small, mini
      layoutName: '', // layout name
      layoutMenuActive: 0, // Default selected menu
      layoutMenuList: [
        {
          name: '菜单1',
          isEditName: false,
          layoutList: []
        }
      ],
      layoutComponentsList: [],// Layout component area list
      positionClassOpt: {
        'top': 'flex-jc',
        'top-start': 'flex-js',
        'top-end': 'flex-je',
        'bottom': 'flex-jc',
        'bottom-start': 'flex-js',
        'bottom-end': 'flex-je'
      },
      // currentPage total
    };
  },
  computed: {
  },
  watch: {},
  created() {
  },
  mounted() {

  },
  beforeDestroy() {

  },
  methods: {
    // Toggle menu
    handleSwitchMenu(index) {
      // Currently selected menu
      this.layoutMenuActive = index
      // Layout component area list
      this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
    },
  }
});
