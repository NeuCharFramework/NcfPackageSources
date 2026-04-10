var Store = new Vuex.Store({
    state: {
        pageSrc: '1',
        resourceCodes: [],
        navMenu: { // Sidebar data
            navMenuList: [],
            isCollapse:JSON.parse( window.sessionStorage.getItem('isCollapse'))|| false,
            variables: {
                menuBg: '#304156', // Background color
                menuText: '#bfcbd9', // Text color
                menuActiveText: '#409EFF' // Active color
            },
            // Current active menu index
            activeMenu: window.sessionStorage.getItem('activeMenu') || '0'
        }
    },
    mutations: {
        changePageSrc(state, data) {
            state.pageSrc = data;
        },
        saveResourceCodes(state, data) {
            state.resourceCodes = data;
        },
        // Toggle sidebar state
        changeIsCollapse(state,data) {
            state.navMenu.isCollapse = data;
        },
        // Save menu data
        savenavMenuList(state, data) {
            state.navMenu.navMenuList = data;
        }
    }
});
