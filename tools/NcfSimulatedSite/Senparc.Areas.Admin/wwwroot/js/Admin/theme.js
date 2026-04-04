/**
 * Admin 主题管理系统
 * 支持：浅色模式、深色模式、跟随系统
 */

// 主题配置
const ThemeConfig = {
    light: {
        name: 'light',
        body: {
            backgroundColor: '#ffffff',
            color: '#303133'
        },
        elMain: {
            backgroundColor: '#e3e3e3'
        },
        elHeader: {
            backgroundColor: '#ffffff',
            color: '#303133',
            borderBottom: '1px solid #e6e6e6'
        },
        elAside: {
            backgroundColor: '#304156'
        },
        menuText: '#bfcbd9',
        menuActiveBg: '#409EFF',
        link: '#5A738E'
    },
    dark: {
        name: 'dark',
        body: {
            backgroundColor: '#1a1a1a',
            color: '#e0e0e0'
        },
        elMain: {
            backgroundColor: '#1e1e1e'
        },
        elHeader: {
            backgroundColor: '#2d2d2d',
            color: '#e0e0e0',
            borderBottom: '1px solid #404040'
        },
        elAside: {
            backgroundColor: '#1e1e1e'
        },
        menuText: '#a0a8b8',
        menuActiveBg: '#409EFF',
        link: '#73a8d4'
    }
};

/**
 * 获取系统主题偏好（浏览器暗黑模式设置）
 */
function getSystemTheme() {
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }
    return 'light';
}

/**
 * 获取当前应该应用的主题
 */
function getCurrentTheme() {
    const mode = Store.state.themeMode;
    if (mode === 'auto') {
        return getSystemTheme();
    }
    return mode;
}

/**
 * 应用主题
 */
function applyTheme(mode) {
    // 如果传入的是 mode，则获取实际的主题
    const actualTheme = mode === 'auto' ? getSystemTheme() : mode;
    const config = ThemeConfig[actualTheme];
    
    if (!config) return;

    // 应用到 body
    document.body.style.backgroundColor = config.body.backgroundColor;
    document.body.style.color = config.body.color;

    // 应用到 el-main
    const elMain = document.querySelector('.el-main');
    if (elMain) {
        elMain.style.backgroundColor = config.elMain.backgroundColor;
    }

    // 应用到 el-header
    const elHeader = document.querySelector('.el-header');
    if (elHeader) {
        elHeader.style.backgroundColor = config.elHeader.backgroundColor;
        elHeader.style.color = config.elHeader.color;
        elHeader.style.borderBottom = config.elHeader.borderBottom;
    }

    // 应用到 el-aside
    const elAside = document.querySelector('.el-aside-index');
    if (elAside) {
        elAside.style.backgroundColor = config.elAside.backgroundColor;
    }

    // 应用到 breadcrumb 和其他文本元素
    const breadcrumbs = document.querySelectorAll('.el-breadcrumb__inner, .el-breadcrumb__inner a');
    breadcrumbs.forEach(el => {
        if (actualTheme === 'dark') {
            el.style.color = '#c0c4cc';
        } else {
            el.style.color = '#303133';
        }
    });

    // 应用到链接
    const links = document.querySelectorAll('a');
    links.forEach(link => {
        link.style.color = config.link;
    });

    // 应用到 footer
    const footer = document.querySelector('.footer');
    if (footer) {
        footer.style.color = config.link;
    }

    // 应用 CSS 类用于主题识别
    document.documentElement.setAttribute('data-theme', actualTheme);
    document.body.setAttribute('data-theme', actualTheme);

    // 如果有 Element UI 暗黑模式主题文件，切换它
    switchElementUITheme(actualTheme);
}

/**
 * 切换 Element UI 暗黑模式 CSS（如果有的话）
 */
function switchElementUITheme(theme) {
    // 这里可以动态加载不同的 Element UI 主题 CSS
    // 目前 Element UI 2.13.2 的暗黑模式支持有限
    // 可以通过 CSS 变量或新增 CSS 文件来实现

    // 创建或更新 Element UI 暗黑模式 CSS
    let styleId = 'element-ui-dark-mode';
    let styleElement = document.getElementById(styleId);

    if (theme === 'dark') {
        if (!styleElement) {
            styleElement = document.createElement('style');
            styleElement.id = styleId;
            styleElement.innerHTML = getElementUIDarkCSS();
            document.head.appendChild(styleElement);
        }
    } else {
        if (styleElement) {
            styleElement.remove();
        }
    }
}

/**
 * 获取 Element UI 暗黑模式 CSS
 */
function getElementUIDarkCSS() {
    return `
        /* Element UI 暗黑模式 */
        [data-theme="dark"] .el-input__inner,
        [data-theme="dark"] .el-select,
        [data-theme="dark"] .el-table {
            background-color: #2d2d2d;
            color: #e0e0e0;
            border-color: #404040;
        }

        [data-theme="dark"] .el-input__inner {
            background-color: #363636;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-input__inner:focus {
            border-color: #409EFF;
            background-color: #363636;
        }

        [data-theme="dark"] .el-input__inner::placeholder {
            color: #909399;
        }

        [data-theme="dark"] .el-table td, 
        [data-theme="dark"] .el-table th {
            border-color: #404040;
        }

        [data-theme="dark"] .el-table {
            background-color: #2d2d2d;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-table.el-table--enable-row-hover .el-table__body tr:hover > td {
            background-color: #363636;
        }

        [data-theme="dark"] .el-table__header th {
            background-color: #363636;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-dialog {
            background-color: #2d2d2d;
        }

        [data-theme="dark"] .el-dialog__header {
            border-bottom: 1px solid #404040;
        }

        [data-theme="dark"] .el-dialog__title {
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-dialog__close {
            color: #a8a9ad;
        }

        [data-theme="dark"] .el-form-item__label {
            color: #c0c4cc;
        }

        [data-theme="dark"] .el-select-dropdown {
            background-color: #2d2d2d;
        }

        [data-theme="dark"] .el-select-dropdown__item {
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-select-dropdown__item:hover {
            background-color: #363636;
        }

        [data-theme="dark"] .el-select-dropdown__item.selected {
            color: #409EFF;
        }

        [data-theme="dark"] .el-dropdown-menu {
            background-color: #2d2d2d;
            border: 1px solid #404040;
        }

        [data-theme="dark"] .el-dropdown-menu__item {
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-dropdown-menu__item:hover {
            background-color: #363636;
        }

        [data-theme="dark"] .el-button {
            border-color: #404040;
            color: #e0e0e0;
            background-color: #2d2d2d;
        }

        [data-theme="dark"] .el-button.is-plain {
            border-color: #404040;
            background-color: transparent;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-pagination {
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-pagination .btn-prev,
        [data-theme="dark"] .el-pagination .btn-next,
        [data-theme="dark"] .el-pagination .el-pager li {
            background-color: #2d2d2d;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-pagination .btn-prev:hover,
        [data-theme="dark"] .el-pagination .btn-next:hover,
        [data-theme="dark"] .el-pagination .el-pager li:hover {
            color: #409EFF;
        }

        [data-theme="dark"] .el-tooltip__popper {
            background-color: #424243;
            border-color: #404040;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-tooltip__popper[x-placement^="top"] .popper__arrow::after {
            border-top-color: #424243;
        }

        [data-theme="dark"] .el-popover {
            background-color: #2d2d2d;
            border: 1px solid #404040;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-tag {
            background-color: #363636;
            border-color: #404040;
            color: #e0e0e0;
        }

        [data-theme="dark"] .el-message {
            background-color: #2d2d2d;
            border: 1px solid #404040;
            color: #e0e0e0;
        }

        [data-theme="dark"] .header-language-select,
        [data-theme="dark"] .header-theme-select {
            background-color: #2d2d2d;
            color: #e0e0e0;
            border-color: #404040;
        }
    `;
}

/**
 * 初始化主题（在页面加载时调用）
 */
function initTheme() {
    const mode = Store.state.themeMode;
    applyTheme(mode);

    // 初始化 select 元素的值
    const themeSelect = document.getElementById('ncf-theme');
    if (themeSelect) {
        themeSelect.value = mode;
    }

    // 监听系统主题改变（当设置为跟随系统时）
    if (window.matchMedia) {
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
            if (Store.state.themeMode === 'auto') {
                applyTheme('auto');
            }
        });
    }
}

/**
 * 切换主题（从 UI 调用）
 */
function ncfSwitchTheme(mode) {
    Store.commit('setThemeMode', mode);
    
    // 更新 select 元素
    const themeSelect = document.getElementById('ncf-theme');
    if (themeSelect) {
        themeSelect.value = mode;
    }
}

// 初始化
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initTheme);
} else {
    initTheme();
}
