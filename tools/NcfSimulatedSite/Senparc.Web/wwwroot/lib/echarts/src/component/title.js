define(function(require) {

    'use strict';

    var echarts = require('../echarts');
    var graphic = require('../util/graphic');
    var layout = require('../util/layout');

    // Model
    echarts.extendComponentModel({

        type: 'title',

        layoutMode: {type: 'box', ignoreSize: true},

        defaultOption: {
            // One level cascading
            zlevel: 0,
            // Second level stacking
            z: 6,
            show: true,

            text: '',
            // Hyperlink jump
            // link: null,
            // Only supports self | blank
            target: 'blank',
            subtext: '',

            // Hyperlink jump
            // sublink: null,
            // Only supports self | blank
            subtarget: 'blank',

            // 'center' ¦ 'left' ¦ 'right'
            // ¦ {number} (x coordinate, unit px)
            left: 0,
            // 'top' ¦ 'bottom' ¦ 'center'
            // ¦ {number} (y coordinate, unit px)
            top: 0,

            // Align horizontally
            // 'auto' | 'left' | 'right'
            // By default, it is judged whether to align left or right based on the position of x
            //textAlign: null

            backgroundColor: 'rgba(0,0,0,0)',

            // Title border color
            borderColor: '#ccc',

            // Title border line width, unit px, default is 0 (no border)
            borderWidth: 0,

            // Title padding, unit px, default padding in all directions is 5.
            // Accept arrays to set the top, right, bottom and left margins respectively, same as css
            padding: 5,

            // Vertical spacing between main and subtitles, unit px, default is 10,
            itemGap: 10,
            textStyle: {
                fontSize: 18,
                fontWeight: 'bolder',
                // Main title text color
                color: '#333'
            },
            subtextStyle: {
                // Subtitle text color
                color: '#aaa'
            }
        }
    });

    // View
    echarts.extendComponentView({

        type: 'title',

        render: function (titleModel, ecModel, api) {
            this.group.removeAll();

            if (!titleModel.get('show')) {
                return;
            }

            var group = this.group;

            var textStyleModel = titleModel.getModel('textStyle');
            var subtextStyleModel = titleModel.getModel('subtextStyle');

            var textAlign = titleModel.get('textAlign');

            var textEl = new graphic.Text({
                style: {
                    text: titleModel.get('text'),
                    textFont: textStyleModel.getFont(),
                    fill: textStyleModel.getTextColor(),
                    textBaseline: 'top'
                },
                z2: 10
            });

            var textRect = textEl.getBoundingRect();

            var subText = titleModel.get('subtext');
            var subTextEl = new graphic.Text({
                style: {
                    text: subText,
                    textFont: subtextStyleModel.getFont(),
                    fill: subtextStyleModel.getTextColor(),
                    y: textRect.height + titleModel.get('itemGap'),
                    textBaseline: 'top'
                },
                z2: 10
            });

            var link = titleModel.get('link');
            var sublink = titleModel.get('sublink');

            textEl.silent = !link;
            subTextEl.silent = !sublink;

            if (link) {
                textEl.on('click', function () {
                    window.open(link, '_' + titleModel.get('target'));
                });
            }
            if (sublink) {
                subTextEl.on('click', function () {
                    window.open(sublink, '_' + titleModel.get('subtarget'));
                });
            }

            group.add(textEl);
            subText && group.add(subTextEl);
            // If no subText, but add subTextEl, there will be an empty line.

            var groupRect = group.getBoundingRect();
            var layoutOption = titleModel.getBoxLayoutParams();
            layoutOption.width = groupRect.width;
            layoutOption.height = groupRect.height;
            var layoutRect = layout.getLayoutRect(
                layoutOption, {
                    width: api.getWidth(),
                    height: api.getHeight()
                }, titleModel.get('padding')
            );
            // Adjust text align based on position
            if (!textAlign) {
                // Align left if title is on the left. center and right is same
                textAlign = titleModel.get('left') || titleModel.get('right');
                if (textAlign === 'middle') {
                    textAlign = 'center';
                }
                // Adjust layout by text align
                if (textAlign === 'right') {
                    layoutRect.x += layoutRect.width;
                }
                else if (textAlign === 'center') {
                    layoutRect.x += layoutRect.width / 2;
                }
            }
            group.position = [layoutRect.x, layoutRect.y];
            textEl.setStyle('textAlign', textAlign);
            subTextEl.setStyle('textAlign', textAlign);

            // Render background
            // Get groupRect again because textAlign has been changed
            groupRect = group.getBoundingRect();
            var padding = layoutRect.margin;
            var style = titleModel.getItemStyle(['color', 'opacity']);
            style.fill = titleModel.get('backgroundColor');
            var rect = new graphic.Rect({
                shape: {
                    x: groupRect.x - padding[3],
                    y: groupRect.y - padding[0],
                    width: groupRect.width + padding[1] + padding[3],
                    height: groupRect.height + padding[0] + padding[2]
                },
                style: style,
                silent: true
            });
            graphic.subPixelOptimizeRect(rect);

            group.add(rect);
        }
    });
});