function permissionJudge(value) {
    let list = JSON.parse(window.sessionStorage.getItem('saveResourceCodes'));
    if (!value.length > 0) {
        return true;
    }
    for (let item of list) {
        if (value.indexOf(item.resourceCode) > -1) {
            return true;
        }
    }
    return false;
}
//Legacy comment translated to English.
Vue.directive('has', {
    //Legacy comment translated to English.
    inserted: function (el, binding) {
        if (!permissionJudge(binding.value)) {
            el.parentNode.removeChild(el);
        }
    }
});