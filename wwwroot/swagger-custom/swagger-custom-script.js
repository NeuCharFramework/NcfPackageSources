/**
 * Swagger UI 自定义脚本
 * 用于处理认证 token 的自动填充和管理
 * 支持 Cookie 认证和 JWT Bearer Token 认证
 */

(function () {
    // Cookie 操作辅助函数
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // 认证管理器
    const authManager = {
        // 获取 JWT token 的来源优先级：localStorage > sessionStorage > cookie
        getJwtToken: function () {
            return localStorage.getItem('auth_token') 
                || sessionStorage.getItem('auth_token')
                || getCookie('auth_token');
        },

        // 检查是否已通过 Cookie 认证
        isCookieAuthenticated: function () {
            // 检查 .AspNetCore.Identity.Application cookie 或其他认证 cookie
            return getCookie('.AspNetCore.Identity.Application') !== null 
                || getCookie(SiteConfig.NcfAdminAuthorizeScheme) !== null;
        },

        // 设置 JWT token
        setJwtToken: function (token) {
            if (token) {
                localStorage.setItem('auth_token', token);
                this.applyJwtAuth();
            }
        },

        // 清除 JWT token
        clearJwtToken: function () {
            localStorage.removeItem('auth_token');
            sessionStorage.removeItem('auth_token');
            document.cookie = 'auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
            this.removeJwtAuth();
        },

        // 应用 JWT 认证
        applyJwtAuth: function () {
            const token = this.getJwtToken();
            if (token && window.swaggerUi && window.swaggerUi.api) {
                const bearerAuth = new SwaggerClient.ApiKeyAuthorization(
                    'Authorization',
                    'Bearer ' + token,
                    'header'
                );
                window.swaggerUi.api.clientAuthorizations.add('Bearer', bearerAuth);
            }
        },

        // 移除 JWT 认证
        removeJwtAuth: function () {
            if (window.swaggerUi && window.swaggerUi.api) {
                window.swaggerUi.api.clientAuthorizations.remove('Bearer');
            }
        },

        // 检查认证状态并应用适当的认证方式
        checkAndApplyAuth: function () {
            // 如果有 JWT token，应用 JWT 认证
            if (this.getJwtToken()) {
                this.applyJwtAuth();
            }
            
            // 如果已通过 Cookie 认证，不需要额外操作
            // ASP.NET Core 会自动处理 Cookie 认证
        }
    };

    // 初始化 UI
    function initializeUI() {
        // 添加自定义授权按钮样式
        const authButton = document.querySelector('.authorize');
        if (authButton) {
            authButton.style.backgroundColor = '#49cc90';
            authButton.style.color = '#fff';
            authButton.style.border = 'none';
            authButton.style.padding = '5px 15px';
            authButton.style.borderRadius = '4px';
        }

        // 添加认证状态指示器
        const header = document.querySelector('.swagger-ui .topbar');
        if (header) {
            const statusDiv = document.createElement('div');
            statusDiv.id = 'auth-status';
            statusDiv.style.marginRight = '1em';
            statusDiv.style.color = '#fff';
            header.appendChild(statusDiv);
            updateAuthStatus();
        }
    }

    // 更新认证状态显示
    function updateAuthStatus() {
        const statusDiv = document.getElementById('auth-status');
        if (!statusDiv) return;

        const isCookieAuth = authManager.isCookieAuthenticated();
        const hasJwtToken = !!authManager.getJwtToken();

        let status = [];
        if (isCookieAuth) status.push('Cookie认证');
        if (hasJwtToken) status.push('JWT认证');

        statusDiv.textContent = status.length ? `认证方式: ${status.join(', ')}` : '未认证';
        statusDiv.style.color = status.length ? '#4CAF50' : '#FFA500';
    }

    // 监听 DOM 加载完成
    document.addEventListener('DOMContentLoaded', function () {
        // 等待 SwaggerUI 完全加载
        const initializationInterval = setInterval(function () {
            if (window.swaggerUi && window.swaggerUi.api) {
                clearInterval(initializationInterval);
                authManager.checkAndApplyAuth();
                initializeUI();
            }
        }, 100);
    });

    // 监听存储变化
    window.addEventListener('storage', function (e) {
        if (e.key === 'auth_token') {
            authManager.checkAndApplyAuth();
            updateAuthStatus();
        }
    });

    // 导出工具函数供外部使用
    window.swaggerCustom = {
        authManager: authManager,
        reinitializeAuth: function () {
            authManager.checkAndApplyAuth();
            updateAuthStatus();
        }
    };
})();
