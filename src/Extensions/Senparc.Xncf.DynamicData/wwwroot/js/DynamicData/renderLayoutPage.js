/**

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
var app = new Vue({
  el: "#app",
  data() {
    return {
      devHost: 'http://pr-felixj.frp.senparc.com',
      elSize: 'medium', //Legacy comment translated to English.
      layoutName: '', //Legacy comment translated to English.
      layoutMenuActive: 0, //Legacy comment translated to English.
      layoutMenuList: [
        {
          name: ncfST('Frontend.276'),
          isEditName: false,
          layoutList: []
        }
      ],
      layoutComponentsList: [],//Legacy comment translated to English.
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
    //Legacy comment translated to English.
    handleSwitchMenu(index) {
      //Legacy comment translated to English.
      this.layoutMenuActive = index
      //Legacy comment translated to English.
      this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
    },
  }
});
