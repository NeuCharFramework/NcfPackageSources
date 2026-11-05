var validatePass = (rule, value, callback) => {
    if (value === '') {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.passwordRequired) || 'Password is required'));
    } else {
        callback();
    }
};
var validateUser = (rule, value, callback) => {
    if (value === '') {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.usernameRequired) || 'Username is required'));
    } else {
        callback();
    }
};
var validateTenant = (rule, value, callback) => {
    // 租户名称不是必填的，直接通过验证
    callback();
};
var validateCaptcha = (rule, value, callback) => {
    // 仅当服务端标记需要验证码时才必填
    var required = typeof app !== 'undefined' && app && app.captchaRequired;
    if (required && (!value || value.trim() === '')) {
        callback(new Error((window.ncfLoginI18n && window.ncfLoginI18n.captchaRequired) || 'Captcha is required'));
    } else {
        callback();
    }
};
var app = new Vue({
    el: '#app',
    data: {
        ruleForm: {
            user: '',
            pass: '',
            tenant: '',
            captcha: ''
        },
        enableMultiTenant: false,
        captchaRequired: false,
        captchaType: '',
        captchaToken: '',
        captchaImage: '',
        slideCaptcha: null,
        sliderX: 0,
        slideVerified: null,
        slideTip: '',
        slidePieceW: 0,
        slideDisplayWidth: 300,
        rules: {
            user: [
                { validator: validateUser, trigger: 'blur' }
            ],
            pass: [
                { validator: validatePass, trigger: 'blur' }
            ],
            tenant: [
                { validator: validateTenant, trigger: 'blur' }
            ],
            captcha: [
                { validator: validateCaptcha, trigger: 'blur' }
            ]
        }, loading: false
    },
    computed: {
        slidePieceStyle() {
            var scale = this.slideDisplayWidth / 552;
            return {
                left: this.sliderX + 'px',
                width: Math.round((this.slidePieceW || 60) * scale) + 'px'
            };
        }
    },
    mounted() {
        // 检查是否启用多租户
        service.get('/Admin/Login?handler=CheckMultiTenant').then(res => {
            this.enableMultiTenant = res.data.data;
            // 如果不是多租户模式，清空租户输入
            if (!this.enableMultiTenant) {
                this.ruleForm.tenant = '';
            }
        }).catch(error => {
            console.error('检查多租户状态失败:', error);
            this.enableMultiTenant = false;
            this.ruleForm.tenant = '';
        });
    },
    methods: {
        submitForm(formName) {
            this.$refs[formName].validate((valid) => {
                this.loading = true;
                var url = "/Admin/Login?handler=Login";
                let data = {
                    Name: this.ruleForm.user+'',
                    Password: this.ruleForm.pass,
                    Tenant: this.ruleForm.tenant,
                    CaptchaToken: this.captchaRequired ? this.captchaToken : '',
                    CaptchaCode: this.captchaRequired ? (this.ruleForm.captcha || '') : ''
                };
                if (valid) {
                    service.post(url, data, { customAlert: true }).then(res => {
                        if (res.data.success) {
                            const url = this.resizeUrl().ReturnUrl;
                            window.location.href = url ? unescape(url) : '/Admin/index';
                        } else {
                            this.$message.error(res.data.msg || (window.ncfLoginI18n && window.ncfLoginI18n.loginFailed) || 'Login failed');
                            if (res.data.captchaRequired) {
                                this.captchaRequired = true;
                                this.applyCaptchaChallenge(res);
                            }
                            this.ruleForm.captcha = '';
                            this.loading = false;
                        }
                    }).catch(error => {
                        if (error && error.response && error.response.status === 429) {
                            this.$message.warning((window.ncfLoginI18n && window.ncfLoginI18n.rateLimited) || 'Too many requests, please try again later');
                        } else {
                            this.$message.error((window.ncfLoginI18n && window.ncfLoginI18n.loginRetry) || 'Login failed, please try again');
                        }
                        this.loading = false;
                    });
                } else {
                    this.loading = false;
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        refreshCaptcha() {
            var self = this;
            service.get('/Admin/Login?handler=Captcha', { customAlert: true }).then(res => {
                if (res.data && res.data.success) {
                    self.captchaToken = res.data.data.token;
                    self.captchaImage = res.data.data.image;
                    self.ruleForm.captcha = '';
                }
            }).catch(error => {
                console.error('获取验证码失败:', error);
                self.captchaImage = '';
                self.captchaToken = '';
            });
        },
        // ===== 登录安全加强：滑块验证码（Lazy.SlideCaptcha.Core/MIT，服务端含行为轨迹校验） =====
        applyCaptchaChallenge(res) {
            if (res.data.slideCaptcha && res.data.slideCaptcha.id) {
                this.captchaType = 'slide';
                this.slideCaptcha = {
                    id: res.data.slideCaptcha.id,
                    backgroundImage: res.data.slideCaptcha.backgroundImage,
                    sliderImage: res.data.slideCaptcha.sliderImage
                };
                this.captchaToken = '';
                this.captchaImage = '';
                this.resetSlideState();
                this.slideTip = (window.ncfLoginI18n && window.ncfLoginI18n.slideCaptchaDrag) || 'Hold and drag to the gap';
            } else if (res.data.captcha && res.data.captcha.token) {
                this.captchaType = 'text';
                this.captchaToken = res.data.captcha.token;
                this.captchaImage = res.data.captcha.image;
            } else {
                this.captchaType = 'text';
                this.refreshCaptcha();
            }
        },
        resetSlideState() {
            this.sliderX = 0;
            this.slideVerified = null;
        },
        onSlidePieceLoad(e) {
            // 滑块图（整幅 344 高画布，拼图居中）：记录自然宽度以换算显示宽度
            if (e && e.target) {
                this.slidePieceW = e.target.naturalWidth;
            }
        },
        slideMaxX() {
            var scale = this.slideDisplayWidth / 552;
            return Math.max(1, this.slideDisplayWidth - (this.slidePieceW || 60) * scale);
        },
        onSlideDown(e) {
            this._beginSlide(e.clientX);
        },
        onSlideTouchStart(e) {
            if (e.touches && e.touches.length) {
                this._beginSlide(e.touches[0].clientX);
            }
        },
        _beginSlide(clientX) {
            if (this.slideVerified !== null || !this.slideCaptcha) {
                return;
            }
            var bar = this.$el.querySelector('.slide-captcha-bar');
            if (!bar) {
                return;
            }
            var rect = bar.getBoundingClientRect();
            var self = this;
            var startTs = Date.now();
            var tracks = [];
            var max = this.slideMaxX();
            var lastX = 0;
            var dragging = true;

            function trackPoint(x) {
                lastX = x;
                self.sliderX = x;
                tracks.push({ X: Math.round(x), Y: 0, T: Date.now() - startTs });
            }
            function handleMove(x) {
                if (!dragging) {
                    return;
                }
                trackPoint(Math.max(0, Math.min(max, x - rect.left - 14)));
            }
            function handleUp() {
                if (!dragging) {
                    return;
                }
                dragging = false;
                document.removeEventListener('mousemove', handleMove);
                document.removeEventListener('mouseup', handleUp);
                document.removeEventListener('touchmove', handleTouchMove);
                document.removeEventListener('touchend', handleUp);
                trackPoint(lastX);
                if (tracks.length < 2) {
                    self.resetSlideState();
                    return;
                }
                self._finishSlide(startTs, tracks);
            }
            function handleTouchMove(e) {
                if (e.touches && e.touches.length) {
                    handleMove(e.touches[0].clientX);
                }
            }

            document.addEventListener('mousemove', handleMove);
            document.addEventListener('mouseup', handleUp);
            document.addEventListener('touchmove', handleTouchMove);
            document.addEventListener('touchend', handleUp);
            trackPoint(Math.max(0, Math.min(max, clientX - rect.left - 14)));
        },
        _finishSlide(startTs, tracks) {
            var scale = this.slideDisplayWidth / 552;
            var trackPayload = {
                BackgroundImageWidth: this.slideDisplayWidth,
                BackgroundImageHeight: Math.round(this.slideDisplayWidth * 344 / 552),
                SliderImageWidth: Math.round((this.slidePieceW || 60) * scale),
                SliderImageHeight: Math.round(this.slideDisplayWidth * 344 / 552),
                StartTime: new Date(startTs).toISOString(),
                EndTime: new Date().toISOString(),
                Percent: this.sliderX / this.slideMaxX() * 100,
                Tracks: tracks
            };
            this.slideTip = (window.ncfLoginI18n && window.ncfLoginI18n.slideCaptchaChecking) || 'Verifying...';
            this._submitWithSlide(trackPayload);
        },
        _submitWithSlide(trackPayload) {
            var self = this;
            this.loading = true;
            var data = {
                Name: this.ruleForm.user + '',
                Password: this.ruleForm.pass,
                Tenant: this.ruleForm.tenant,
                CaptchaToken: '',
                CaptchaCode: '',
                SlideCaptchaId: this.slideCaptcha.id,
                SlideTrack: JSON.stringify(trackPayload)
            };
            service.post('/Admin/Login?handler=Login', data, { customAlert: true }).then(res => {
                this.loading = false;
                if (res.data.success) {
                    const url = this.resizeUrl().ReturnUrl;
                    window.location.href = url ? unescape(url) : '/Admin/index';
                } else {
                    this.slideVerified = false;
                    this.slideTip = (window.ncfLoginI18n && window.ncfLoginI18n.slideCaptchaFailed) || 'Verification failed, please retry';
                    this.$message.error(res.data.msg || (window.ncfLoginI18n && window.ncfLoginI18n.loginFailed) || 'Login failed');
                    this.applyCaptchaChallenge(res);
                }
            }).catch(error => {
                this.loading = false;
                this.slideVerified = false;
                this.slideTip = (window.ncfLoginI18n && window.ncfLoginI18n.slideCaptchaFailed) || 'Verification failed, please retry';
                if (error && error.response && error.response.status === 429) {
                    this.$message.warning((window.ncfLoginI18n && window.ncfLoginI18n.rateLimited) || 'Too many requests, please try again later');
                } else {
                    this.$message.error((window.ncfLoginI18n && window.ncfLoginI18n.loginRetry) || 'Login failed, please try again');
                }
            });
        },
        checkCaptchaRequired() {
            var name = this.ruleForm.user + '';
            var self = this;
            if (!name) {
                self.captchaRequired = false;
                self.captchaImage = '';
                self.captchaToken = '';
                return;
            }
            service.get('/Admin/Login?handler=CaptchaRequired&name=' + encodeURIComponent(name), { customAlert: true }).then(res => {
                if (res.data && res.data.success) {
                    if (res.data.data && !self.captchaRequired) {
                        self.captchaRequired = true;
                        self.refreshCaptcha();
                    } else if (!res.data.data) {
                        self.captchaRequired = false;
                        self.captchaImage = '';
                        self.captchaToken = '';
                    }
                }
            }).catch(error => {
                console.error('检查验证码状态失败:', error);
            });
        },
        resizeUrl() {//处理剪切url id
            let url = window.location.href;
            let obj = {};
            let reg = /[?&][^?&]+=[^?&]+/g;
            let arr = url.match(reg); // return ["?id=123456","&a=b"]
            if (arr) {
                arr.forEach((item) => {
                    let tempArr = item.substring(1).split('=');
                    let key = tempArr[0];
                    let val = tempArr[1];
                    obj[key] = decodeURIComponent(val);
                });
            }
            return obj;
        }
    }
});