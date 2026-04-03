define(function (require) {

    require('../../echarts').extendComponentModel({

        type: 'tooltip',

        defaultOption: {
            zlevel: 0,

            z: 8,

            show: true,

            // tooltip body content
            showContent: true,

            // Trigger type, default data trigger, see the figure below, optional: 'item' ¦ 'axis'
            trigger: 'item',

            // Trigger condition, supports 'click' | 'mousemove'
            triggerOn: 'mousemove',

            // Whether to always display content
            alwaysShowContent: false,

            // Location {Array} | {Function}
            // position: null

            // Content formatter: {string} (Template) ¦ {Function}
            // formatter: null

            showDelay: 0,

            // Hidden delay, unit ms
            hideDelay: 100,

            // Animation transformation time, unit s
            transitionDuration: 0.4,

            enterable: false,

            // Prompt background color, default is black with transparency of 0.7
            backgroundColor: 'rgba(50,50,50,0.7)',

            // Prompt border color
            borderColor: '#333',

            // Prompt border rounded corners, unit px, default is 4
            borderRadius: 4,

            // Prompt border line width, unit px, default is 0 (no border)
            borderWidth: 0,

            // Prompt padding in px. The default padding in each direction is 5.
            // Accept arrays to set the top, right, bottom and left margins respectively, same as css
            padding: 5,

            // Extra css text
            extraCssText: '',

            // Axis indicator, axis trigger is valid
            axisPointer: {
                // Default is straight line
                // Optional: 'line' | 'shadow' | 'cross'
                type: 'line',

                // Valid when type is line, specify the axis where the tooltip line is located, optional
                // Optional 'x' | 'y' | 'angle' | 'radius' | 'auto'
                // The default is 'auto', which will select the axis of type cateogry. For double value axes, the Cartesian coordinate system will select the x-axis by default.
                // The polar coordinate system will select the angle axis by default.
                axis: 'auto',

                animation: true,
                animationDurationUpdate: 200,
                animationEasingUpdate: 'exponentialOut',

                // Line indicator style settings
                lineStyle: {
                    color: '#555',
                    width: 1,
                    type: 'solid'
                },

                crossStyle: {
                    color: '#555',
                    width: 1,
                    type: 'dashed',

                    // TODO formatter
                    textStyle: {}
                },

                // Shadow indicator style settings
                shadowStyle: {
                    color: 'rgba(150,150,150,0.3)'
                }
            },
            textStyle: {
                color: '#fff',
                fontSize: 14
            }
        }
    });
});