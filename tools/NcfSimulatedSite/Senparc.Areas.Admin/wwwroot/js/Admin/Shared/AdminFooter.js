(function (window) {
    'use strict';

    const maximumConsoleEntries = 200;
    const consoleEntries = [];
    const consoleListeners = new Set();
    let consoleSequence = 0;

    function formatConsoleValue(value) {
        if (value instanceof Error) {
            return value.stack || value.message;
        }
        if (typeof value === 'string') {
            return value;
        }
        try {
            return JSON.stringify(value);
        } catch (_) {
            return String(value);
        }
    }

    function publishConsoleEntry(level, values) {
        const entry = {
            id: ++consoleSequence,
            level: level,
            time: new Date().toLocaleTimeString(),
            message: Array.from(values).map(formatConsoleValue).join(' ')
        };
        consoleEntries.push(entry);
        if (consoleEntries.length > maximumConsoleEntries) {
            consoleEntries.splice(0, consoleEntries.length - maximumConsoleEntries);
        }
        consoleListeners.forEach(listener => listener(consoleEntries.slice()));
    }

    ['log', 'info', 'warn', 'error'].forEach(level => {
        const original = window.console[level].bind(window.console);
        window.console[level] = function () {
            original.apply(window.console, arguments);
            publishConsoleEntry(level, arguments);
        };
    });

    window.NcfAdminConsole = {
        subscribe(listener) {
            consoleListeners.add(listener);
            listener(consoleEntries.slice());
            return function () { consoleListeners.delete(listener); };
        },
        clear() {
            consoleEntries.splice(0, consoleEntries.length);
            consoleListeners.forEach(listener => listener([]));
        }
    };

    const initialState = window.NCF_ADMIN_FOOTER_INITIAL_STATE || {};

    const footerMixin = {
        data() {
            const parsedServerTime = Date.parse(initialState.serverTime || '');
            return {
                footerAiDialogVisible: false,
                footerAiUrl: '/Admin/AdminChat/Chat?embedded=1',
                consoleDialogVisible: false,
                consoleEntries: [],
                synchroDrawerVisible: false,
                synchroProviders: [],
                synchroLoading: false,
                serverTimeBaseMs: Number.isFinite(parsedServerTime) ? parsedServerTime : Date.now(),
                serverTimeMeasuredAtMs: Date.now(),
                footerClockTick: 0,
                footerConsoleUnsubscribe: null,
                footerClockTimer: null,
                footerPollTimer: null,
                footerEventSource: null
            };
        },
        computed: {
            serverTimeText() {
                this.footerClockTick;
                const elapsed = Date.now() - this.serverTimeMeasuredAtMs;
                return new Date(this.serverTimeBaseMs + elapsed).toLocaleString('zh-CN', {
                    hour12: false,
                    month: '2-digit',
                    day: '2-digit',
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
            },
            synchroTotalCount() {
                return this.synchroProviders
                    .filter(provider => provider.enabled)
                    .reduce((providerTotal, provider) => providerTotal + (provider.items || [])
                        .reduce((itemTotal, item) => itemTotal + Math.max(0, Number(item.count) || 0), 0), 0);
            }
        },
        mounted() {
            if (this.$root !== this) {
                return;
            }

            this.footerConsoleUnsubscribe = window.NcfAdminConsole.subscribe(entries => {
                this.consoleEntries = entries;
            });

            if (initialState.embedded) {
                return;
            }

            this.footerClockTimer = window.setInterval(() => { this.footerClockTick += 1; }, 1000);
            this.refreshFooterState();
            this.startSynchroEventStream();
            this.footerPollTimer = window.setInterval(() => this.refreshFooterState(), 30000);
        },
        beforeDestroy() {
            if (this.$root !== this) {
                return;
            }

            if (this.footerConsoleUnsubscribe) {
                this.footerConsoleUnsubscribe();
            }
            window.clearInterval(this.footerClockTimer);
            window.clearInterval(this.footerPollTimer);
            if (this.footerEventSource) {
                this.footerEventSource.close();
            }
        },
        methods: {
            openFooterAi() {
                this.footerAiUrl = '/Admin/AdminChat/Chat?embedded=1&footer=' + Date.now();
                this.footerAiDialogVisible = true;
            },
            openFullAdminChat() {
                window.location.href = '/Admin/AdminChat/Chat';
            },
            clearFooterConsole() {
                window.NcfAdminConsole.clear();
            },
            synchroPreferenceKey() {
                return 'ncf.admin.synchro.providers.' + (initialState.account || 'admin');
            },
            readSynchroPreferences() {
                try {
                    const value = JSON.parse(window.localStorage.getItem(this.synchroPreferenceKey()) || '{}');
                    return value && typeof value === 'object' ? value : {};
                } catch (_) {
                    return {};
                }
            },
            saveSynchroPreferences() {
                const preferences = {};
                this.synchroProviders.forEach(provider => {
                    preferences[provider.providerId] = provider.enabled !== false;
                });
                window.localStorage.setItem(this.synchroPreferenceKey(), JSON.stringify(preferences));
            },
            applySynchroProviders(providers) {
                const preferences = this.readSynchroPreferences();
                this.synchroProviders = (providers || []).map(provider => Object.assign({}, provider, {
                    enabled: Object.prototype.hasOwnProperty.call(preferences, provider.providerId)
                        ? preferences[provider.providerId]
                        : provider.defaultVisible !== false
                }));
            },
            async refreshFooterState() {
                this.synchroLoading = true;
                try {
                    const response = await service.get('/api/Senparc.Areas.Admin/synchro/state');
                    const responseBody = response && response.data ? response.data : {};
                    const state = responseBody.data && responseBody.data.serverTime ? responseBody.data : responseBody;
                    const serverTime = Date.parse(state.serverTime || '');
                    if (Number.isFinite(serverTime)) {
                        this.serverTimeBaseMs = serverTime;
                        this.serverTimeMeasuredAtMs = Date.now();
                    }
                    this.applySynchroProviders(state.providers || []);
                } catch (error) {
                    console.warn('Synchro 状态刷新失败:', error);
                } finally {
                    this.synchroLoading = false;
                }
            },
            startSynchroEventStream() {
                if (typeof window.EventSource === 'undefined') {
                    return;
                }
                this.footerEventSource = new EventSource('/api/Senparc.Areas.Admin/synchro/events');
                this.footerEventSource.addEventListener('synchro-changed', () => this.refreshFooterState());
            }
        }
    };

    window.NcfAdminFooterMixin = footerMixin;
    if (window.Vue) {
        window.Vue.mixin(footerMixin);
    }
})(window);
