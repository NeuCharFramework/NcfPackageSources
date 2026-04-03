// Toggle menu bar expansion
Vue.prototype.toggleSideBar = function () {
    Store.commit('changeIsCollapse', !Store.state.navMenu.isCollapse);
    let isCollapse = JSON.parse(window.sessionStorage.getItem('isCollapse')) || false;
    // Solve the problem of refreshing menu status recovery
    window.sessionStorage.setItem('isCollapse', !isCollapse);
};

// Click on the menu to highlight
Vue.prototype.menuSelect = function (key, keypath) {
    window.sessionStorage.setItem('activeMenu', key);
};

// Menu bar data
function getNavMenu() {
    service.get("/Admin/index?handler=MenuResource").then(res => {
        if (res.data.success) {
            var temp = res.data.data.menuList;
            myfunctionMain(temp);
            // Save data
            Store.commit('savenavMenuList', temp);
            // Save the button permissions and use: v-has=" ['admin-add']" directly on the dom
            window.sessionStorage.setItem('saveResourceCodes', JSON.stringify(res.data.data.resourceCodes));
        }
    });
}

getNavMenu();

// Menu bar data recursion
function myfunctionMain(list) {
    if (!list || list.length === 0) {
        return;
    }
    //if (!list && list.length === 0) {
    //    return;
    //}
    for (var i in list) {
        let setNavMenuActive = window.sessionStorage.getItem('setNavMenuActive');
        // If there is navigation that needs to be set up separately (such as activating specific navigation after installation or uninstallation)
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