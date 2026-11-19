var NCF_ADMIN_NAV_STORAGE_KEYS = {
    openedMenus: 'ncf.admin.navMenu.openedMenus',
    scrollTop: 'ncf.admin.navMenu.scrollTop'
};

function getStoredNavMenuOpenedMenus() {
    try {
        var openedMenus = JSON.parse(window.sessionStorage.getItem(NCF_ADMIN_NAV_STORAGE_KEYS.openedMenus));
        return Array.isArray(openedMenus) ? openedMenus : [];
    } catch (e) {
        return [];
    }
}

//Legacy comment translated to English.
function filterNavMenuTree(list, keyword) {
    if (!list || list.length === 0) {
        return [];
    }
    var kw = String(keyword || '').trim().toLowerCase();
    if (!kw) {
        return list;
    }
    var result = [];
    list.forEach(function (node) {
        var children = node.children || [];
        var selfMatched = String(node.menuName || '').toLowerCase().indexOf(kw) >= 0;
        var keptChildren = selfMatched ? children : filterNavMenuTree(children, kw);
        if (selfMatched || keptChildren.length > 0) {
            result.push(Object.assign({}, node, { children: keptChildren }));
        }
    });
    return result;
}

//Legacy comment translated to English.
function collectNavMenuSubmenuIndexes(list) {
    var indexes = [];
    (list || []).forEach(function (node) {
        var children = node.children || [];
        if (children.length > 0) {
            indexes.push(node.index !== undefined && node.index !== null ? node.index : node.id);
            indexes = indexes.concat(collectNavMenuSubmenuIndexes(children));
        }
    });
    return indexes;
}

var Store = new Vuex.Store({
    state: {
        pageSrc: '1',
        resourceCodes: [],
        adminTabs: {
            enabled: false,
            shellId: '',
            activeId: '',
            tabs: [],
            canScrollLeft: false,
            canScrollRight: false
        },
        navMenu: { //Legacy comment translated to English.
            navMenuList: [],
            searchKeyword: '',
            //Legacy comment translated to English.
            searchInputLocked: true,
            isCollapse:JSON.parse( window.sessionStorage.getItem('isCollapse'))|| false,
            openedMenus: getStoredNavMenuOpenedMenus(),
            variables: {
                menuBg: '#304156', //Legacy comment translated to English.
                menuText: '#bfcbd9', //Legacy comment translated to English.
                menuActiveText: '#409EFF' //Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            activeMenu: window.sessionStorage.getItem('activeMenu') || '0'
        }
    },
    getters: {
        //Legacy comment translated to English.
        filteredNavMenuList: function (state) {
            return filterNavMenuTree(state.navMenu.navMenuList, state.navMenu.searchKeyword);
        },
        //Legacy comment translated to English.
        effectiveNavMenuOpeneds: function (state) {
            var keyword = String(state.navMenu.searchKeyword || '').trim();
            if (!keyword) {
                return state.navMenu.openedMenus;
            }
            var searched = collectNavMenuSubmenuIndexes(filterNavMenuTree(state.navMenu.navMenuList, keyword));
            //Legacy comment translated to English.
            return state.navMenu.openedMenus.concat(
                searched.filter(function (index) {
                    return state.navMenu.openedMenus.indexOf(index) < 0;
                })
            );
        },
        isNavMenuSearching: function (state) {
            return String(state.navMenu.searchKeyword || '').trim().length > 0;
        },
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        adminIframeTabs: function (state) {
            if (!state.adminTabs || !state.adminTabs.enabled) {
                return [];
            }
            return state.adminTabs.tabs.filter(function (tab) {
                return !tab.isShell;
            });
        },
        navMenuRenderKey: function (state) {
            var keyword = String(state.navMenu.searchKeyword || '').trim();
            if (!keyword) {
                return 'nav-menu-normal';
            }
            return 'nav-menu-searching-' + collectNavMenuSubmenuIndexes(
                filterNavMenuTree(state.navMenu.navMenuList, keyword)).join('|');
        }
    },
    mutations: {
        changePageSrc(state, data) {
            state.pageSrc = data;
        },
        saveResourceCodes(state, data) {
            state.resourceCodes = data;
        },
        //Legacy comment translated to English.
        changeIsCollapse(state,data) {
            state.navMenu.isCollapse = data;
        },
        //Legacy comment translated to English.
        changeOpenedMenus(state, data) {
            state.navMenu.openedMenus = data;
        },
        //Legacy comment translated to English.
        savenavMenuList(state, data) {
            state.navMenu.navMenuList = data;
        },
        //Legacy comment translated to English.
        setNavMenuSearchKeyword(state, data) {
            state.navMenu.searchKeyword = data || '';
        },
        //Legacy comment translated to English.
        unlockNavMenuSearch(state) {
            state.navMenu.searchInputLocked = false;
        }
    }
});
