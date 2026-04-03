define(function (require) {

    var zrUtil = require('zrender/core/util');

    var defaultOption = {
        show: true,
        zlevel: 0,                  // One level cascading
        z: 0,                       // Second level stacking
        // reverse axis
        inverse: false,
        // Axis name, default is empty
        name: '',
        // Axis name position, supports 'start' | 'middle' | 'end'
        nameLocation: 'end',
        // Axis text style, default to global style
        nameTextStyle: {},
        // Distance between text and axis
        nameGap: 15,
        // Whether mouse events can be triggered
        silent: true,
        // Coordinate axis
        axisLine: {
            // Displayed by default, the attribute show controls whether to display or not.
            show: true,
            onZero: true,
            // The attribute lineStyle controls the line style
            lineStyle: {
                color: '#333',
                width: 1,
                type: 'solid'
            }
        },
        // Axis small mark
        axisTick: {
            // The attribute show controls whether to display or not. It is displayed by default.
            show: true,
            // Control whether the small mark is in the grid
            inside: false,
            // The length attribute controls the line length
            length: 5,
            // The attribute lineStyle controls the line style
            lineStyle: {
                color: '#333',
                width: 1
            }
        },
        // Axis text label, see axis.axisLabel for details
        axisLabel: {
            show: true,
            // Control whether the text label is in the grid
            inside: false,
            rotate: 0,
            margin: 8,
            // formatter: null,
            // The remaining attributes use the global text style by default, see TEXTSTYLE for details.
            textStyle: {
                color: '#333',
                fontSize: 12
            }
        },
        // divider
        splitLine: {
            // Displayed by default, the attribute show controls whether to display or not.
            show: true,
            // The attribute lineStyle (see lineStyle for details) controls the line style
            lineStyle: {
                color: ['#ccc'],
                width: 1,
                type: 'solid'
            }
        },
        // separate areas
        splitArea: {
            // Not displayed by default, the attribute show controls whether to display it or not.
            show: false,
            // The attribute areaStyle (see areaStyle for details) controls the area style.
            areaStyle: {
                color: ['rgba(250,250,250,0.3)','rgba(200,200,200,0.3)']
            }
        }
    };

    var categoryAxis = zrUtil.merge({
        // Blank strategy at the beginning and end of the category
        boundaryGap: true,
        // Axis small mark
        axisTick: {
            interval: 'auto'
        },
        // Axis text label, see axis.axisLabel for details
        axisLabel: {
            interval: 'auto'
        }
    }, defaultOption);

    var valueAxis = zrUtil.defaults({
        // Numeric starting and ending blank strategy
        boundaryGap: [0, 0],
        // Minimum value, set to 'dataMin' to calculate the minimum value from the data
        // min: null,
        // Maximum value, set to 'dataMax' to calculate the maximum value from the data
        // max: null,
        // Readonly prop, specifies start value of the range when using data zoom.
        // rangeStart: null
        // Readonly prop, specifies end value of the range when using data zoom.
        // rangeEnd: null
        // Break away from the 0 value ratio, zoom in and focus to the final _min, _max interval
        // scale: false,
        // Number of segments, default is 5
        splitNumber: 5
        // Minimum interval
        // minInterval: null
    }, defaultOption);

    // FIXME
    var timeAxis = zrUtil.defaults({
        scale: true,
        min: 'dataMin',
        max: 'dataMax'
    }, valueAxis);
    var logAxis = zrUtil.defaults({}, valueAxis);
    logAxis.scale = true;

    return {
        categoryAxis: categoryAxis,
        valueAxis: valueAxis,
        timeAxis: timeAxis,
        logAxis: logAxis
    };
});