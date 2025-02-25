﻿var app = new Vue({
    el: "#app",
    data() {
        var validateCode = (rule, value, callback) => {
            if (this.dialog.data.menuType === 3) {
                if (!value) {
                    callback(new Error('当类型是按钮类型时此项必填'));
                } else {
                    callback();
                }
            } else {
                callback();
            }
        };
        return {
            // 表格数据
            tableData: [],
            dialog: {
                title: '新增菜单',
                visible: false,
                data: {
                    id: '', menuName: '', parentId: [], url: '', icon: '', sort: '', visible: true,
                    resourceCode: '', isLocked: false, menuType: ''
                },
                rules: {
                    menuName: [
                        { required: true, message: "菜单名称为必填项", trigger: "blur" }
                    ],
                    menuType: [{ required: true, message: "类型为必选项", trigger: "blur" }],
                    resourceCode: [{ validator: validateCode, trigger: "blur" }]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true // 是否严格的遵守父子节点不互相关联	
            },
            dialogIcon: {
                visible: false,
                elementIcons: [
                    'fa-adjust',
                    'fa-anchor',
                    'fa-archive',
                    'fa-area-chart',
                    'fa-arrows',
                    'fa-arrows-h',
                    'fa-arrows-v',
                    'fa-asterisk',
                    'fa-at',
                    'fa-automobile',
                    'fa-ban',
                    'fa-bank',
                    'fa-bar-chart',
                    'fa-bar-chart-o',
                    'fa-barcode',
                    'fa-bars',
                    'fa-beer',
                    'fa-bell',
                    'fa-bell-o',
                    'fa-bell-slash',
                    'fa-bell-slash-o',
                    'fa-bicycle',
                    'fa-binoculars',
                    'fa-birthday-cake',
                    'fa-bolt',
                    'fa-bomb',
                    'fa-book',
                    'fa-bookmark',
                    'fa-bookmark-o',
                    'fa-briefcase',
                    'fa-bug',
                    'fa-building',
                    'fa-building-o',
                    'fa-bullhorn',
                    'fa-bullseye',
                    'fa-bus',
                    'fa-cab',
                    'fa-calculator',
                    'fa-calendar',
                    'fa-calendar-o',
                    'fa-camera',
                    'fa-camera-retro',
                    'fa-car',
                    'fa-caret-square-o-down',
                    'fa-caret-square-o-left',
                    'fa-caret-square-o-right',
                    'fa-caret-square-o-up',
                    'fa-cc',
                    'fa-certificate',
                    'fa-check',
                    'fa-check-circle',
                    'fa-check-circle-o',
                    'fa-check-square',
                    'fa-check-square-o',
                    'fa-child',
                    'fa-circle',
                    'fa-circle-o',
                    'fa-circle-o-notch',
                    'fa-circle-thin',
                    'fa-clock-o',
                    'fa-close',
                    'fa-cloud',
                    'fa-cloud-download',
                    'fa-cloud-upload',
                    'fa-code',
                    'fa-code-fork',
                    'fa-coffee',
                    'fa-cog',
                    'fa-cogs',
                    'fa-comment',
                    'fa-comment-o',
                    'fa-comments',
                    'fa-comments-o',
                    'fa-compass',
                    'fa-copyright',
                    'fa-credit-card',
                    'fa-crop',
                    'fa-crosshairs',
                    'fa-cube',
                    'fa-cubes',
                    'fa-cutlery',
                    'fa-dashboard',
                    'fa-database',
                    'fa-desktop',
                    'fa-dot-circle-o',
                    'fa-download',
                    'fa-edit',
                    'fa-ellipsis-h',
                    'fa-ellipsis-v',
                    'fa-envelope',
                    'fa-envelope-o',
                    'fa-envelope-square',
                    'fa-eraser',
                    'fa-exchange',
                    'fa-exclamation',
                    'fa-exclamation-circle',
                    'fa-exclamation-triangle',
                    'fa-external-link',
                    'fa-external-link-square',
                    'fa-eye',
                    'fa-eye-slash',
                    'fa-eyedropper',
                    'fa-fax',
                    'fa-female',
                    'fa-fighter-jet',
                    'fa-file-archive-o',
                    'fa-file-audio-o',
                    'fa-file-code-o',
                    'fa-file-excel-o',
                    'fa-file-image-o',
                    'fa-file-movie-o',
                    'fa-file-pdf-o',
                    'fa-file-photo-o',
                    'fa-file-picture-o',
                    'fa-file-powerpoint-o',
                    'fa-file-sound-o',
                    'fa-file-video-o',
                    'fa-file-word-o',
                    'fa-file-zip-o',
                    'fa-film',
                    'fa-filter',
                    'fa-fire',
                    'fa-fire-extinguisher',
                    'fa-flag',
                    'fa-flag-checkered',
                    'fa-flag-o',
                    'fa-flash',
                    'fa-flask',
                    'fa-folder',
                    'fa-folder-o',
                    'fa-folder-open',
                    'fa-folder-open-o',
                    'fa-frown-o',
                    'fa-futbol-o',
                    'fa-gamepad',
                    'fa-gavel',
                    'fa-gear',
                    'fa-gears',
                    'fa-gift',
                    'fa-glass',
                    'fa-globe',
                    'fa-graduation-cap',
                    'fa-group',
                    'fa-hdd-o',
                    'fa-headphones',
                    'fa-heart',
                    'fa-heart-o',
                    'fa-history',
                    'fa-home',
                    'fa-image',
                    'fa-inbox',
                    'fa-info',
                    'fa-info-circle',
                    'fa-institution',
                    'fa-key',
                    'fa-keyboard-o',
                    'fa-language',
                    'fa-laptop',
                    'fa-leaf',
                    'fa-legal',
                    'fa-lemon-o',
                    'fa-level-down',
                    'fa-level-up',
                    'fa-life-bouy',
                    'fa-life-buoy',
                    'fa-life-ring',
                    'fa-life-saver',
                    'fa-lightbulb-o',
                    'fa-line-chart',
                    'fa-location-arrow',
                    'fa-lock',
                    'fa-magic',
                    'fa-magnet',
                    'fa-mail-forward',
                    'fa-mail-reply',
                    'fa-mail-reply-all',
                    'fa-male',
                    'fa-map-marker',
                    'fa-meh-o',
                    'fa-microphone',
                    'fa-microphone-slash',
                    'fa-minus',
                    'fa-minus-circle',
                    'fa-minus-square',
                    'fa-minus-square-o',
                    'fa-mobile',
                    'fa-mobile-phone',
                    'fa-money',
                    'fa-moon-o',
                    'fa-mortar-board',
                    'fa-music',
                    'fa-navicon',
                    'fa-newspaper-o',
                    'fa-paint-brush',
                    'fa-paper-plane',
                    'fa-paper-plane-o',
                    'fa-paw',
                    'fa-pencil',
                    'fa-pencil-square',
                    'fa-pencil-square-o',
                    'fa-phone',
                    'fa-phone-square',
                    'fa-photo',
                    'fa-picture-o',
                    'fa-pie-chart',
                    'fa-plane',
                    'fa-plug',
                    'fa-plus',
                    'fa-plus-circle',
                    'fa-plus-square',
                    'fa-plus-square-o',
                    'fa-power-off',
                    'fa-print',
                    'fa-puzzle-piece',
                    'fa-qrcode',
                    'fa-question',
                    'fa-question-circle',
                    'fa-quote-left',
                    'fa-quote-right',
                    'fa-random',
                    'fa-recycle',
                    'fa-refresh',
                    'fa-remove',
                    'fa-reorder',
                    'fa-reply',
                    'fa-reply-all',
                    'fa-retweet',
                    'fa-road',
                    'fa-rocket',
                    'fa-rss',
                    'fa-rss-square',
                    'fa-search',
                    'fa-search-minus',
                    'fa-search-plus',
                    'fa-send',
                    'fa-send-o',
                    'fa-share',
                    'fa-share-alt',
                    'fa-share-alt-square',
                    'fa-share-square',
                    'fa-share-square-o',
                    'fa-shield',
                    'fa-shopping-cart',
                    'fa-sign-in',
                    'fa-sign-out',
                    'fa-signal',
                    'fa-sitemap',
                    'fa-sliders',
                    'fa-smile-o',
                    'fa-soccer-ball-o',
                    'fa-sort',
                    'fa-sort-alpha-asc',
                    'fa-sort-alpha-desc',
                    'fa-sort-amount-asc',
                    'fa-sort-amount-desc',
                    'fa-sort-asc',
                    'fa-sort-desc',
                    'fa-sort-down',
                    'fa-sort-numeric-asc',
                    'fa-sort-numeric-desc',
                    'fa-sort-up',
                    'fa-space-shuttle',
                    'fa-spinner',
                    'fa-spoon',
                    'fa-square',
                    'fa-square-o',
                    'fa-star',
                    'fa-star-half',
                    'fa-star-half-empty',
                    'fa-star-half-full',
                    'fa-star-half-o',
                    'fa-star-o',
                    'fa-suitcase',
                    'fa-sun-o',
                    'fa-support',
                    'fa-tablet',
                    'fa-tachometer',
                    'fa-tag',
                    'fa-tags',
                    'fa-tasks',
                    'fa-taxi',
                    'fa-terminal',
                    'fa-thumb-tack',
                    'fa-thumbs-down',
                    'fa-thumbs-o-down',
                    'fa-thumbs-o-up',
                    'fa-thumbs-up',
                    'fa-ticket',
                    'fa-times',
                    'fa-times-circle',
                    'fa-times-circle-o',
                    'fa-tint',
                    'fa-toggle-down',
                    'fa-toggle-left',
                    'fa-toggle-off',
                    'fa-toggle-on',
                    'fa-toggle-right',
                    'fa-toggle-up',
                    'fa-trash',
                    'fa-trash-o',
                    'fa-tree',
                    'fa-trophy',
                    'fa-truck',
                    'fa-tty',
                    'fa-umbrella',
                    'fa-university',
                    'fa-unlock',
                    'fa-unlock-alt',
                    'fa-unsorted',
                    'fa-upload',
                    'fa-user',
                    'fa-users',
                    'fa-video-camera',
                    'fa-volume-down',
                    'fa-volume-off',
                    'fa-volume-up',
                    'fa-warning',
                    'fa-wheelchair',
                    'fa-wifi',
                    'fa-wrench',
                    'fa-file',
                    'fa-file-archive-o',
                    'fa-file-audio-o',
                    'fa-file-code-o',
                    'fa-file-excel-o',
                    'fa-file-image-o',
                    'fa-file-movie-o',
                    'fa-file-o',
                    'fa-file-pdf-o',
                    'fa-file-photo-o',
                    'fa-file-picture-o',
                    'fa-file-powerpoint-o',
                    'fa-file-sound-o',
                    'fa-file-text',
                    'fa-file-text-o',
                    'fa-file-video-o',
                    'fa-file-word-o',
                    'fa-file-zip-o',
                    'fa-circle-o-notch',
                    'fa-cog',
                    'fa-gear',
                    'fa-refresh',
                    'fa-spinner',
                    'fa-check-square',
                    'fa-check-square-o',
                    'fa-circle',
                    'fa-circle-o',
                    'fa-dot-circle-o',
                    'fa-minus-square',
                    'fa-minus-square-o',
                    'fa-plus-square',
                    'fa-plus-square-o',
                    'fa-square',
                    'fa-square-o',
                    'fa-cc-amex',
                    'fa-cc-discover',
                    'fa-cc-mastercard',
                    'fa-cc-paypal',
                    'fa-cc-stripe',
                    'fa-cc-visa',
                    'fa-credit-card',
                    'fa-google-wallet',
                    'fa-paypal',
                    'fa-area-chart',
                    'fa-bar-chart',
                    'fa-bar-chart-o',
                    'fa-line-chart',
                    'fa-pie-chart',
                    'fa-bitcoin',
                    'fa-btc',
                    'fa-cny',
                    'fa-dollar',
                    'fa-eur',
                    'fa-euro',
                    'fa-gbp',
                    'fa-ils',
                    'fa-inr',
                    'fa-jpy',
                    'fa-krw',
                    'fa-money',
                    'fa-rmb',
                    'fa-rouble',
                    'fa-rub',
                    'fa-ruble',
                    'fa-rupee',
                    'fa-shekel',
                    'fa-sheqel',
                    'fa-try',
                    'fa-turkish-lira',
                    'fa-usd',
                    'fa-won',
                    'fa-yen',
                    'fa-align-center',
                    'fa-align-justify',
                    'fa-align-left',
                    'fa-align-right',
                    'fa-bold',
                    'fa-chain',
                    'fa-chain-broken',
                    'fa-clipboard',
                    'fa-columns',
                    'fa-copy',
                    'fa-cut',
                    'fa-dedent',
                    'fa-eraser',
                    'fa-file',
                    'fa-file-o',
                    'fa-file-text',
                    'fa-file-text-o',
                    'fa-files-o',
                    'fa-floppy-o',
                    'fa-font',
                    'fa-header',
                    'fa-indent',
                    'fa-italic',
                    'fa-link',
                    'fa-list',
                    'fa-list-alt',
                    'fa-list-ol',
                    'fa-list-ul',
                    'fa-outdent',
                    'fa-paperclip',
                    'fa-paragraph',
                    'fa-paste',
                    'fa-repeat',
                    'fa-rotate-left',
                    'fa-rotate-right',
                    'fa-save',
                    'fa-scissors',
                    'fa-strikethrough',
                    'fa-subscript',
                    'fa-superscript',
                    'fa-table',
                    'fa-text-height',
                    'fa-text-width',
                    'fa-th',
                    'fa-th-large',
                    'fa-th-list',
                    'fa-underline',
                    'fa-undo',
                    'fa-unlink',
                    'fa-angle-double-down',
                    'fa-angle-double-left',
                    'fa-angle-double-right',
                    'fa-angle-double-up',
                    'fa-angle-down',
                    'fa-angle-left',
                    'fa-angle-right',
                    'fa-angle-up',
                    'fa-arrow-circle-down',
                    'fa-arrow-circle-left',
                    'fa-arrow-circle-o-down',
                    'fa-arrow-circle-o-left',
                    'fa-arrow-circle-o-right',
                    'fa-arrow-circle-o-up',
                    'fa-arrow-circle-right',
                    'fa-arrow-circle-up',
                    'fa-arrow-down',
                    'fa-arrow-left',
                    'fa-arrow-right',
                    'fa-arrow-up',
                    'fa-arrows',
                    'fa-arrows-alt',
                    'fa-arrows-h',
                    'fa-arrows-v',
                    'fa-caret-down',
                    'fa-caret-left',
                    'fa-caret-right',
                    'fa-caret-square-o-down',
                    'fa-caret-square-o-left',
                    'fa-caret-square-o-right',
                    'fa-caret-square-o-up',
                    'fa-caret-up',
                    'fa-chevron-circle-down',
                    'fa-chevron-circle-left',
                    'fa-chevron-circle-right',
                    'fa-chevron-circle-up',
                    'fa-chevron-down',
                    'fa-chevron-left',
                    'fa-chevron-right',
                    'fa-chevron-up',
                    'fa-hand-o-down',
                    'fa-hand-o-left',
                    'fa-hand-o-right',
                    'fa-hand-o-up',
                    'fa-long-arrow-down',
                    'fa-long-arrow-left',
                    'fa-long-arrow-right',
                    'fa-long-arrow-up',
                    'fa-toggle-down',
                    'fa-toggle-left',
                    'fa-toggle-right',
                    'fa-toggle-up',
                    'fa-arrows-alt',
                    'fa-backward',
                    'fa-compress',
                    'fa-eject',
                    'fa-expand',
                    'fa-fast-backward',
                    'fa-fast-forward',
                    'fa-forward',
                    'fa-pause',
                    'fa-play',
                    'fa-play-circle',
                    'fa-play-circle-o',
                    'fa-step-backward',
                    'fa-step-forward',
                    'fa-stop',
                    'fa-youtube-play',
                    'fa-adn',
                    'fa-android',
                    'fa-angellist',
                    'fa-apple',
                    'fa-behance',
                    'fa-behance-square',
                    'fa-bitbucket',
                    'fa-bitbucket-square',
                    'fa-bitcoin',
                    'fa-btc',
                    'fa-cc-amex',
                    'fa-cc-discover',
                    'fa-cc-mastercard',
                    'fa-cc-paypal',
                    'fa-cc-stripe',
                    'fa-cc-visa',
                    'fa-codepen',
                    'fa-css3',
                    'fa-delicious',
                    'fa-deviantart',
                    'fa-digg',
                    'fa-dribbble',
                    'fa-dropbox',
                    'fa-drupal',
                    'fa-empire',
                    'fa-facebook',
                    'fa-facebook-square',
                    'fa-flickr',
                    'fa-foursquare',
                    'fa-ge',
                    'fa-git',
                    'fa-git-square',
                    'fa-github',
                    'fa-github-alt',
                    'fa-github-square',
                    'fa-gittip',
                    'fa-google',
                    'fa-google-plus',
                    'fa-google-plus-square',
                    'fa-google-wallet',
                    'fa-hacker-news',
                    'fa-html5',
                    'fa-instagram',
                    'fa-ioxhost',
                    'fa-joomla',
                    'fa-jsfiddle',
                    'fa-lastfm',
                    'fa-lastfm-square',
                    'fa-linkedin',
                    'fa-linkedin-square',
                    'fa-linux',
                    'fa-maxcdn',
                    'fa-meanpath',
                    'fa-openid',
                    'fa-pagelines',
                    'fa-paypal',
                    'fa-pied-piper',
                    'fa-pied-piper-alt',
                    'fa-pinterest',
                    'fa-pinterest-square',
                    'fa-qq',
                    'fa-ra',
                    'fa-rebel',
                    'fa-reddit',
                    'fa-reddit-square',
                    'fa-renren',
                    'fa-share-alt',
                    'fa-share-alt-square',
                    'fa-skype',
                    'fa-slack',
                    'fa-slideshare',
                    'fa-soundcloud',
                    'fa-spotify',
                    'fa-stack-exchange',
                    'fa-stack-overflow',
                    'fa-steam',
                    'fa-steam-square',
                    'fa-stumbleupon',
                    'fa-stumbleupon-circle',
                    'fa-tencent-weibo',
                    'fa-trello',
                    'fa-tumblr',
                    'fa-tumblr-square',
                    'fa-twitch',
                    'fa-twitter',
                    'fa-twitter-square',
                    'fa-vimeo-square',
                    'fa-vine',
                    'fa-vk',
                    'fa-wechat',
                    'fa-weibo',
                    'fa-weixin',
                    'fa-windows',
                    'fa-wordpress',
                    'fa-xing',
                    'fa-xing-square',
                    'fa-yahoo',
                    'fa-yelp',
                    'fa-youtube',
                    'fa-youtube-play',
                    'fa-youtube-square',
                    'fa-ambulance',
                    'fa-h-square',
                    'fa-hospital-o',
                    'fa-medkit',
                    'fa-plus-square',
                    'fa-stethoscope',
                    'fa-user-md',
                    'fa-wheelchair'
                ]
            }
        };
    },
    created: function () {
        this.getList();
    },
    mounted() {
        window.addEventListener('scroll', this.handleScroll);
        window.addEventListener('resize', this.handleResize);
        this.$nextTick(() => {
            this.initStickyParents();
        });
    },
    beforeDestroy() {
        window.removeEventListener('scroll', this.handleScroll);
        window.removeEventListener('resize', this.handleResize);
    },
    watch: {
        'dialog.visible': function (val, old) {
            // 关闭dialog，清空
            if (!val) {
                this.dialog.data = {
                    id: '', menuName: '', parentId: [], url: '', icon: '', sort: '', visible: false,
                    resourceCode: '', isLocked: false, menuType: ''
                };
                this.dialog.updateLoading = false;
                this.dialog.disabled = false;
            }
        }
    },
    methods: {
        // 选取图标
        pickIcon(item) {
            this.dialogIcon.visible = false;
            this.dialog.data.icon = item;
        },
        // 更新授权
        async  auUpdateData() {
            this.au.updateLoading = true;
            const checkNodes = this.$refs.tree.getCheckedNodes(false, true);
            let array = [];
            checkNodes.map((ele) => {
                array.push({
                    PermissionId: ele.id,
                    roleId: this.au.temp.id,
                    isMenu: ele.isMenu,
                    roleCode: ele.resourceCode
                });
            });
            const respnseData = await service.post('/Admin/Role/Permission', array);
            if (respnseData.data.success) {
                this.getList();
                this.$notify({
                    title: "Success",
                    message: "授权成功",
                    type: "success",
                    duration: 2000
                });
                this.au.visible = false;
                this.au.updateLoading = false;
            }
        },
        // 获取所有菜单
        async  getList() {
            const a = await service.get('/Admin/Menu/Edit?handler=Menu');
            const b = a.data.data;
            let allMenu = [];
            this.ddd(b, null, allMenu);
            this.tableData = allMenu;
            
            // 数据加载完成后初始化固定效果
            this.$nextTick(() => {
                this.initStickyParents();
            });
        },
        // 数据处理
        ddd(source, parentId, dest) {
            var array = source.filter(_ => _.parentId === parentId);
            for (var i in array) {
                var ele = array[i];
                ele.children = [];
                dest.unshift(ele);
                this.ddd(source, ele.id, ele.children);
            }
        },
        // 编辑 // 新增菜单 // 增加下一级
        handleEdit(index, row, flag) {
            this.dialog.visible = true;
            if (flag === 'add') {
                // 新增
                this.dialog.title = '新增菜单';
                return;
            }
            // 编辑
            let { id, menuName, parentId, url, icon, sort, visible,
                resourceCode, isLocked, menuType } = row;
            this.dialog.data = {
                id, menuName, parentId: [parentId], url, icon, sort, visible,
                resourceCode, isLocked, menuType
            };
            // dialog中父级菜单 做递归显示
            let x = [];
            this.recursionFunc(row, this.tableData, x);
            this.dialog.data.parentId = x;
            //////////////////////////////

            if (flag === 'edit') {
                this.dialog.title = '编辑菜单';
                if (row.isLocked) {
                    this.dialog.disabled = true;
                }
            } else if (flag === 'addNext') {
                this.dialog.data.id = '';
                this.dialog.title = '增加下一级菜单';
                this.dialog.data.menuName = '';
                this.dialog.data.parentId.push(row.id);
            }
        },
        // 设置父级菜单默认显示 递归
        recursionFunc(row, source, dest) {
            if (row.parentId === null) {
                return;
            }
            for (let i in source) {
                let ele = source[i];
                if (row.parentId === ele.id) {
                    this.recursionFunc(ele, this.tableData, dest);
                    dest.push(ele.id);
                } else {
                    this.recursionFunc(row, ele.children, dest);
                }
            }
        },
        // 更新新增、编辑
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // 表单校验
                if (valid) {
                    this.dialog.updateLoading = true;
                    let data = {
                        Id: this.dialog.data.id,
                        MenuName: this.dialog.data.menuName,
                        ParentId: this.dialog.data.parentId[this.dialog.data.parentId.length - 1],
                        Url: this.dialog.data.url,
                        Icon: this.dialog.data.icon.includes("fa ") ? this.dialog.data.icon : "fa " + this.dialog.data.icon,
                        Sort: this.dialog.data.sort * 1,
                        Visible: this.dialog.data.visible,
                        ResourceCode: this.dialog.data.resourceCode,
                        IsLocked: this.dialog.data.isLocked,
                        MenuType: this.dialog.data.menuType
                    };
                    service.post("/Admin/Menu/Edit", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: "成功",
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                        }
                    });
                }
            });
        },
        // 删除
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/Menu/edit?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: "Success",
                        message: "删除成功",
                        type: "success",
                        duration: 2000
                    });
                }
            });
        },
        // 初始化父节点固定效果
        initStickyParents() {
            const rows = document.querySelectorAll('.el-table__row');
            const table = document.querySelector('.el-table');
            
            rows.forEach(row => {
                const expandIcon = row.querySelector('.el-table__expand-icon');
                if (expandIcon && !expandIcon.classList.contains('el-table__expand-icon--leaf')) {
                    row.classList.add('sticky-parent');
                    
                    // 创建克隆行并保持列宽
                    const clone = row.cloneNode(true);
                    clone.classList.add('sticky-clone');
                    clone.style.display = 'none';
                    
                    // 复制每列的宽度
                    const originalCells = row.querySelectorAll('td');
                    const cloneCells = clone.querySelectorAll('td');
                    originalCells.forEach((cell, index) => {
                        const width = window.getComputedStyle(cell).width;
                        cloneCells[index].style.width = width;
                        cloneCells[index].style.minWidth = width;
                        cloneCells[index].style.maxWidth = width;
                    });
                    
                    // 设置克隆行的总宽度
                    clone.style.width = window.getComputedStyle(row).width;
                    
                    row.parentNode.insertBefore(clone, row.nextSibling);
                }
            });
        },
        // 处理滚动事件
        handleScroll() {
            const stickyRows = document.querySelectorAll('.sticky-parent');
            const headerHeight = document.querySelector('.el-table__header-wrapper').offsetHeight;
            const table = document.querySelector('.el-table');
            const tableRect = table.getBoundingClientRect();

            stickyRows.forEach(row => {
                const clone = row.nextElementSibling;
                if (!clone || !clone.classList.contains('sticky-clone')) return;

                const rect = row.getBoundingClientRect();
                
                if (rect.top <= headerHeight) {
                    clone.style.display = 'table-row';
                    clone.style.position = 'fixed';
                    clone.style.top = `${headerHeight}px`;
                    clone.style.left = `${tableRect.left}px`;
                    
                    // 确保克隆行的列宽与原行保持一致
                    const originalCells = row.querySelectorAll('td');
                    const cloneCells = clone.querySelectorAll('td');
                    originalCells.forEach((cell, index) => {
                        const width = window.getComputedStyle(cell).width;
                        cloneCells[index].style.width = width;
                    });
                } else {
                    clone.style.display = 'none';
                }
            });
        },
        // 在窗口大小改变时重新计算列宽
        handleResize() {
            this.initStickyParents();
        }
    }

});