var app = new Vue({
    el: '#app',
    data: {
        isExpanded: false,
        installStarted: false,
        installConfirmationVisible: false,
        optionsModelList: [],
        preferredCulture: (window.ncfInstallPage && window.ncfInstallPage.currentCulture) || 'zh-CN',
        installOptions: {
            systemName: "",
            adminUserName: "",
            dbConnectionString: "",
            needModelList: null,
            preferredCulture: (window.ncfInstallPage && window.ncfInstallPage.currentCulture) || 'zh-CN'
        }
    },
    methods: {
        toggleInfo() {
            this.isExpanded = !this.isExpanded;
        },
        switchLanguage(culture) {
            if (!culture || culture === this.preferredCulture) {
                return;
            }
            if (typeof ncfSetInstallCultureCookie === 'function') {
                ncfSetInstallCultureCookie(culture);
            }
            window.location.reload();
        },
        initOptions() {
            axios.get("/Install/Index?handler=DefaultOptions")
                .then(response => {
                    var data = response.data.result.data;
                    this.installOptions.systemName = data.systemName;
                    console.log('[NCF Installer] Loaded default install options.', data);
                    this.installOptions.adminUserName = data.adminUserName;
                    this.installOptions.dbConnectionString = data.dbConnectionString;
                    this.optionsModelList = data.needModelList || [];
                    this.installOptions.needModelList = this.optionsModelList
                        .filter(item => item.selectedByDefault)
                        .map(item => item.uid);
                    this.installOptions.preferredCulture = this.preferredCulture;
                })
                .catch(error => {
                    console.error((window.ncfInstallI18n && window.ncfInstallI18n.loadOptionsFailed) || 'Failed to load installation options:', error);
                });
        },
        submit() {
            if (this.installStarted) {
                this.$message({
                    message: (window.ncfInstallI18n && window.ncfInstallI18n.started) || 'Installation is already running, please wait.',
                    type: 'info',
                    showClose: true
                });
                return false;
            }

            if (this.installConfirmationVisible) {
                return false;
            }

            var i18n = window.ncfInstallI18n || {};
            this.installConfirmationVisible = true;

            this.$confirm(
                i18n.startConfirmDetail || 'This page will expire after installation.',
                i18n.startConfirm || 'Start installation?',
                {
                    confirmButtonText: i18n.confirmButtonText || 'Install now',
                    cancelButtonText: i18n.cancelButtonText || 'Cancel',
                    type: 'warning',
                    closeOnClickModal: false,
                    distinguishCancelAndClose: true
                })
                .then(() => {
                    this.installConfirmationVisible = false;
                    this.startInstallation();
                })
                .catch(action => {
                    this.installConfirmationVisible = false;
                    if (action !== 'cancel' && action !== 'close') {
                        console.error('Failed to display the installation confirmation:', action);
                    }
                });

            return false;
        },
        startInstallation() {
            if (this.installStarted) {
                return false;
            }

            this.installStarted = true;
            this.installOptions.preferredCulture = this.preferredCulture;
            console.log('[NCF Installer] Starting install with culture:', this.installOptions.preferredCulture);

            axios.post("/Install/Index", this.installOptions, {
                headers: {
                    RequestVerificationToken: document.querySelector('input[name="__RequestVerificationToken"]').value
                },
            }).then(res => {
                document.getElementById('app').innerHTML = res.data;
            }).catch(error => {
                this.installStarted = false;
                document.getElementById('app').innerHTML = ((window.ncfInstallI18n && window.ncfInstallI18n.failedPrefix) || 'Install failed:') + error;
            });
            return true;
        }
    },
    mounted() {
        this.initOptions();
    }
})