define(function (require) {

    var List = require('../../data/List');
    var SeriesModel = require('../../model/Series');
    var zrUtil = require('zrender/core/util');

    var GaugeSeries = SeriesModel.extend({

        type: 'series.gauge',

        getInitialData: function (option, ecModel) {
            var list = new List(['value'], this);
            var dataOpt = option.data || [];
            if (!zrUtil.isArray(dataOpt)) {
                dataOpt = [dataOpt];
            }
            // Only use the first data item
            list.initData(dataOpt);
            return list;
        },

        defaultOption: {
            zlevel: 0,
            z: 2,
            // Default global center
            center: ['50%', '50%'],
            legendHoverLink: true,
            radius: '75%',
            startAngle: 225,
            endAngle: -45,
            clockwise: true,
            // minimum value
            min: 0,
            // maximum value
            max: 100,
            // Number of segments, default is 10
            splitNumber: 10,
            // Coordinate axis
            axisLine: {
                // Displayed by default, the attribute show controls whether to display or not.
                show: true,
                lineStyle: {       // The attribute lineStyle controls the line style
                    color: [[0.2, '#91c7ae'], [0.8, '#63869e'], [1, '#c23531']],
                    width: 30
                }
            },
            // divider
            splitLine: {
                // Displayed by default, the attribute show controls whether to display or not.
                show: true,
                // The length attribute controls the line length
                length: 30,
                // The attribute lineStyle (see lineStyle for details) controls the line style
                lineStyle: {
                    color: '#eee',
                    width: 2,
                    type: 'solid'
                }
            },
            // Axis small mark
            axisTick: {
                // The attribute show controls whether to display or not. It is not displayed by default.
                show: true,
                // How many segments are divided into each split?
                splitNumber: 5,
                // The length attribute controls the line length
                length: 8,
                // The attribute lineStyle controls the line style
                lineStyle: {
                    color: '#eee',
                    width: 1,
                    type: 'solid'
                }
            },
            axisLabel: {
                show: true,
                // formatter: null,
                textStyle: {       // The remaining attributes use the global text style by default, see TEXTSTYLE for details.
                    color: 'auto'
                }
            },
            pointer: {
                show: true,
                length: '80%',
                width: 8
            },
            itemStyle: {
                normal: {
                    color: 'auto'
                }
            },
            title: {
                show: true,
                // x, y, unit px
                offsetCenter: [0, '-40%'],
                // The remaining attributes use the global text style by default, see TEXTSTYLE for details.
                textStyle: {
                    color: '#333',
                    fontSize: 15
                }
            },
            detail: {
                show: true,
                backgroundColor: 'rgba(0,0,0,0)',
                borderWidth: 0,
                borderColor: '#ccc',
                width: 100,
                height: 40,
                // x, y, unit px
                offsetCenter: [0, '40%'],
                // formatter: null,
                // The remaining attributes use the global text style by default, see TEXTSTYLE for details.
                textStyle: {
                    color: 'auto',
                    fontSize: 30
                }
            }
        }
    });

    return GaugeSeries;
});