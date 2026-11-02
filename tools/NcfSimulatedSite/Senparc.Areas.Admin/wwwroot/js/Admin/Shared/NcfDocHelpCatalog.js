(function (window) {
    'use strict';

    // Only publish entries after both locale URLs (and their anchors) are deployed.
    window.NcfDocHelpCatalog = {
        topics: {
            'admin.home': { path: 'start/start-develop/admin-background.html' },
            'modules.manage': { path: 'start/start-develop/admin-module-manage.html' },
            'modules.publish': {
                path: 'start/start-develop/admin-module-manage.html',
                anchors: { zh: '切换-发布-状态', en: 'switch-publish-status' }
            },
            'mcp.home': { path: 'MCP/home/index.html' }
        },
        pages: {
            '/admin': 'admin.home',
            '/admin/index': 'admin.home',
            '/admin/xncfmodule': 'modules.manage',
            '/admin/xncfmodule/index': 'modules.manage',
            '/admin/mcp': 'mcp.home',
            '/admin/mcp/index': 'mcp.home'
        }
    };
})(window);
