//Legacy comment translated to English.
Vue.prototype.toggleSideBar = function () {
    Store.commit('changeIsCollapse', !Store.state.navMenu.isCollapse);
    let isCollapse = JSON.parse(window.sessionStorage.getItem('isCollapse')) || false;
    //Legacy comment translated to English.
    window.sessionStorage.setItem('isCollapse', !isCollapse);
};

//Legacy comment translated to English.
Vue.prototype.menuSelect = function (key, keypath) {
    if (document.body.classList.contains('ncf-admin-embedded')) {
        return;
    }
    window.sessionStorage.setItem('activeMenu', key);
    //Legacy comment translated to English.
    saveOpenedMenus((keypath || []).slice(0, -1), true);
};

//Legacy comment translated to English.
Vue.prototype.menuOpen = function (key, keypath) {
    saveOpenedMenus(keypath || [key], true);
};

//Legacy comment translated to English.
Vue.prototype.menuClose = function (key) {
    saveOpenedMenus([key], false);
};

function saveOpenedMenus(menuIndexes, isOpened) {
    var openedMenus = Store.state.navMenu.openedMenus.slice();

    (menuIndexes || []).forEach(function (menuIndex) {
        var position = openedMenus.indexOf(menuIndex);
        if (isOpened && position < 0) {
            openedMenus.push(menuIndex);
        } else if (!isOpened && position >= 0) {
            openedMenus.splice(position, 1);
        }
    });

    Store.commit('changeOpenedMenus', openedMenus);
    try {
        window.sessionStorage.setItem(NCF_ADMIN_NAV_STORAGE_KEYS.openedMenus, JSON.stringify(openedMenus));
    } catch (e) {
        //Legacy comment translated to English.
    }
}

function restoreNavMenuScroll() {
    var scrollWrap = document.querySelector('.el-aside-index .scrollbar-wrapper');
    if (!scrollWrap) {
        return;
    }

    var storedScrollTop = 0;
    try {
        storedScrollTop = Number(window.sessionStorage.getItem(NCF_ADMIN_NAV_STORAGE_KEYS.scrollTop));
    } catch (e) {
        storedScrollTop = 0;
    }

    if (Number.isFinite(storedScrollTop) && storedScrollTop > 0) {
        scrollWrap.scrollTop = storedScrollTop;
    }

    if (scrollWrap._ncfNavScrollPersistenceBound) {
        return;
    }

    var saveScrollTop = function () {
        try {
            window.sessionStorage.setItem(NCF_ADMIN_NAV_STORAGE_KEYS.scrollTop, String(scrollWrap.scrollTop));
        } catch (e) {
            //Legacy comment translated to English.
        }
    };

    scrollWrap.addEventListener('scroll', saveScrollTop, { passive: true });
    window.addEventListener('pagehide', saveScrollTop);
    scrollWrap._ncfNavScrollPersistenceBound = true;
}

//Legacy comment translated to English.
function getNavMenu() {
    service.get("/Admin/index?handler=MenuResource").then(res => {
        if (res.data.success) {
            var temp = res.data.data.menuList;
            myfunctionMain(temp);
            //Legacy comment translated to English.
            Store.commit('savenavMenuList', temp);
            //Legacy comment translated to English.
            Vue.nextTick(function () {
                if (typeof window.ncfSyncAdminTabsWithMenu === 'function') {
                    window.ncfSyncAdminTabsWithMenu(temp);
                }
                if (window.requestAnimationFrame) {
                    window.requestAnimationFrame(restoreNavMenuScroll);
                } else {
                    window.setTimeout(restoreNavMenuScroll, 0);
                }
            });
            //Legacy comment translated to English.
            window.sessionStorage.setItem('saveResourceCodes', JSON.stringify(res.data.data.resourceCodes));
        }
    });
}

getNavMenu();

//Legacy comment translated to English.
//Legacy comment translated to English.
//Legacy comment translated to English.
var ncfNavSearchAutocompleteToken = 'ncf-nav-search-' + Math.random().toString(36).slice(2);
Vue.prototype.ncfNavSearchAutocomplete = ncfNavSearchAutocompleteToken;

Vue.prototype.unlockNavMenuSearch = function (event) {
    var target = event && event.target;
    var root = target && target.closest ? target.closest('.admin-nav-search') : document.querySelector('.admin-nav-search');
    var input = root ? root.querySelector('.el-input__inner') : null;
    if (input) {
        input.readOnly = false;
        input.removeAttribute('name');
    }
    if (Store.state.navMenu.searchInputLocked) {
        Store.commit('unlockNavMenuSearch');
    }
};

function isNavMenuSearchInput(target) {
    return !!(target && target.closest && target.classList && target.classList.contains('el-input__inner') && target.closest('.admin-nav-search'));
}

function markNavMenuSearchUserEdited(input) {
    input._ncfUserEdited = true;
}

function logNavMenuSearchAutofill(input, message) {
    if (!input || input._ncfAutofillLogged || !window.console || !console.debug) {
        return;
    }
    input._ncfAutofillLogged = true;
    console.debug(message);
}

function clearNavMenuSearchAutofill(input) {
    if (!input || input._ncfUserEdited) {
        return;
    }
    input.removeAttribute('name');
    if (!String(Store.state.navMenu.searchKeyword || '') && input.value) {
        input.value = '';
        logNavMenuSearchAutofill(input, '[NCF] 已忽略菜单搜索框的浏览器自动填充');
    }
}

//Legacy comment translated to English.
document.addEventListener('keydown', function (event) {
    if (isNavMenuSearchInput(event.target)) {
        markNavMenuSearchUserEdited(event.target);
    }
}, true);
document.addEventListener('paste', function (event) {
    if (isNavMenuSearchInput(event.target)) {
        markNavMenuSearchUserEdited(event.target);
    }
}, true);
document.addEventListener('drop', function (event) {
    if (isNavMenuSearchInput(event.target)) {
        markNavMenuSearchUserEdited(event.target);
    }
}, true);
document.addEventListener('input', function (event) {
    var input = event.target;
    if (!isNavMenuSearchInput(input) || input._ncfUserEdited) {
        return;
    }
    event.stopImmediatePropagation();
    input.value = '';
    logNavMenuSearchAutofill(input, '[NCF] 已拦截菜单搜索框的浏览器自动填充');
}, true);
document.addEventListener('animationstart', function (event) {
    if (event.animationName === 'ncf-nav-search-autofill-start') {
        clearNavMenuSearchAutofill(event.target);
    }
}, true);

function bindNavMenuSearchInput(input) {
    if (!input || input._ncfNavSearchGuardBound) {
        return;
    }
    input._ncfNavSearchGuardBound = true;
    input.removeAttribute('name');
    input.setAttribute('autocomplete', ncfNavSearchAutocompleteToken);
}

function watchNavMenuSearchAutofill() {
    var ticks = 0;
    var observed = false;

    var scan = function () {
        var inputs = document.querySelectorAll('.admin-nav-search .el-input__inner');
        for (var i = 0; i < inputs.length; i++) {
            bindNavMenuSearchInput(inputs[i]);
            clearNavMenuSearchAutofill(inputs[i]);
        }
        if (!observed && inputs.length && window.MutationObserver) {
            var box = document.querySelector('.admin-nav-search');
            if (box) {
                observed = true;
                var observer = new MutationObserver(function () {
                    scan();
                });
                observer.observe(box, {
                    subtree: true,
                    childList: true,
                    attributes: true,
                    attributeFilter: ['name', 'value']
                });
            }
        }
    };

    var start = function () {
        scan();
        //Legacy comment translated to English.
        var timer = window.setInterval(function () {
            scan();
            ticks++;
            if (ticks >= 40) {
                window.clearInterval(timer);
            }
        }, 250);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', start);
    } else {
        start();
    }
}

watchNavMenuSearchAutofill();

//Legacy comment translated to English.
function myfunctionMain(list) {
    if (!list || list.length === 0) {
        return;
    }
    //if (!list && list.length === 0) {
    //    return;
    //}
    for (var i in list) {
        let setNavMenuActive = window.sessionStorage.getItem('setNavMenuActive');
        //Legacy comment translated to English.
        if (!document.body.classList.contains('ncf-admin-embedded') && setNavMenuActive && setNavMenuActive !== "null" && list[i].menuName === setNavMenuActive) {
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
