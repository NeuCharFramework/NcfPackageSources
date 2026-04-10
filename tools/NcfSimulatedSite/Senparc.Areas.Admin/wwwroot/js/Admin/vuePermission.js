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
// Register a global custom directive `v-has`
Vue.directive('has', {
    // Triggered when the bound element is inserted into the DOM
    inserted: function (el, binding) {
        if (!permissionJudge(binding.value)) {
            el.parentNode.removeChild(el);
        }
    }
});
