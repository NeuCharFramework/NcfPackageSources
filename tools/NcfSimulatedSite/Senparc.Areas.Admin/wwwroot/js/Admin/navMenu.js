// Toggle sidebar expand/collapse
Vue.prototype.toggleSideBar = function () {
    Store.commit('changeIsCollapse', !Store.state.navMenu.isCollapse);
    let isCollapse = JSON.parse(window.sessionStorage.getItem('isCollapse')) || false;
    // Fix sidebar state resetting on refresh
    window.sessionStorage.setItem('isCollapse', !isCollapse);
};

// Click menu, highlight active item
Vue.prototype.menuSelect = function (key, keypath) {
    window.sessionStorage.setItem('activeMenu', key);
};

// Sidebar menu data
function getNavMenu() {
    service.get("/Admin/index?handler=MenuResource").then(res => {
        if (res.data.success) {
            var temp = res.data.data.menuList;
            myfunctionMain(temp);
            // Store data
            Store.commit('savenavMenuList', temp);
            // Store button permissions. Usage: add v-has="['admin-add']" directly on DOM element
            window.sessionStorage.setItem('saveResourceCodes', JSON.stringify(res.data.data.resourceCodes));
        }
    });
}

getNavMenu();

// Recursive menu data processing
function myfunctionMain(list) {
    if (!list || list.length === 0) {
        return;
    }
    for (var i in list) {
        let setNavMenuActive = window.sessionStorage.getItem('setNavMenuActive');
        // If there is a nav item to specifically activate (e.g. after install/uninstall)
        if (setNavMenuActive && setNavMenuActive !== "null" && list[i].menuName === setNavMenuActive) {
            window.sessionStorage.setItem('setNavMenuActive', null);
            if (list[i].children.length > 0) {
                window.sessionStorage.setItem('activeMenu', list[i].children[0].id);
            } else {
                window.sessionStorage.setItem('activeMenu', list[i].id);
            }
        }
        list[i].index = list[i].id;
        myfunctionMain(list[i].children);
    }
}
