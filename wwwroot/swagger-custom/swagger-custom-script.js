/**
 * Swagger UI 自定义脚本
 * 用于处理认证 token 的自动填充和管理
 */

(function () {
    // Cookie 操作辅助函数
    function getCookie(name) {
        const value = `; ${document.cookie}`;
        const parts = value.split(`; ${name}=`);
        if (parts.length === 2) return parts.pop().split(';').shift();
        return null;
    }

    // Token 管理
    const tokenManager = {
        // 获取 token 的来源优先级：localStorage > sessionStorage > cookie
        getToken: function () {
            return localStorage.getItem('auth_token') 
                || sessionStorage.getItem('auth_token')
                || getCookie('auth_token');
        },

        // 设置 token
        setToken: function (token) {
            if (token) {
                localStorage.setItem('auth_token', token);
            }
        },

        // 清除 token
        clearToken: function () {
            localStorage.removeItem('auth_token');
            sessionStorage.removeItem('auth_token');
            // 清除 cookie 中的 token
            document.cookie = 'auth_token=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;';
        }
    };

    // 初始化 Swagger 授权
    function initializeSwaggerAuth() {
        const token = tokenManager.getToken();
        if (token) {
            // 设置 Bearer token
            const bearerAuth = new SwaggerClient.ApiKeyAuthorization(
                'Authorization',
                'Bearer ' + token,
                'header'
            );
            window.swaggerUi.api.clientAuthorizations.add('Bearer', bearerAuth);
        }
    }

    // 监听 DOM 加载完成
    document.addEventListener('DOMContentLoaded', function () {
        // 等待 SwaggerUI 完全加载
        const initializationInterval = setInterval(function () {
            if (window.swaggerUi && window.swaggerUi.api) {
                clearInterval(initializationInterval);
                initializeSwaggerAuth();

                // 添加自定义授权按钮样式
                const authButton = document.querySelector('.authorize');
                if (authButton) {
                    authButton.style.backgroundColor = '#49cc90';
                    authButton.style.color = '#fff';
                    authButton.style.border = 'none';
                    authButton.style.padding = '5px 15px';
                    authButton.style.borderRadius = '4px';
                }
            }
        }, 100);
    });

    // 监听存储变化
    window.addEventListener('storage', function (e) {
        if (e.key === 'auth_token') {
            initializeSwaggerAuth();
        }
    });

    // 导出工具函数供外部使用
    window.swaggerCustom = {
        tokenManager: tokenManager,
        reinitializeAuth: initializeSwaggerAuth
    };
})();
