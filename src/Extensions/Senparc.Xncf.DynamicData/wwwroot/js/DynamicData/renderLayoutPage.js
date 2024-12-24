/**
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
var app = new Vue({
  el: "#app",
  data() {
    return {
      devHost: 'http://pr-felixj.frp.senparc.com',
      elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
      layoutName: '', // 布局名称
      layoutMenuActive: 0, // 默认选中的菜单
      layoutMenuList: [
        {
          name: '菜单1',
          isEditName: false,
          layoutList: []
        }
      ],
      layoutComponentsList: [],// 布局组件区域列表
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
    // 切换菜单
    handleSwitchMenu(index) {
      // 当前选中的菜单
      this.layoutMenuActive = index
      // 布局组件区域列表
      this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
    },
  }
});
