define(function(require) {

    'use strict';

    var List = require('../../data/List');
    var zrUtil = require('zrender/core/util');
    var modelUtil = require('../../util/model');
    var completeDimensions = require('../../data/helper/completeDimensions');

    var dataSelectableMixin = require('../../component/helper/selectableMixin');

    var PieSeries = require('../../echarts').extendSeriesModel({

        type: 'series.pie',

        // Overwrite
        init: function (option) {
            PieSeries.superApply(this, 'init', arguments);

            // Enable legend selection for each data item
            // Use a function instead of direct access because data reference may changed
            this.legendDataProvider = function () {
                return this._dataBeforeProcessed;
            };

            this.updateSelectedMap(option.data);

            this._defaultLabelLine(option);
        },

        // Overwrite
        mergeOption: function (newOption) {
            PieSeries.superCall(this, 'mergeOption', newOption);
            this.updateSelectedMap(this.option.data);
        },

        getInitialData: function (option, ecModel) {
            var dimensions = completeDimensions(['value'], option.data);
            var list = new List(dimensions, this);
            list.initData(option.data);
            return list;
        },

        // Overwrite
        getDataParams: function (dataIndex) {
            var data = this._data;
            var params = PieSeries.superCall(this, 'getDataParams', dataIndex);
            var sum = data.getSum('value');
            // FIXME toFixed?
            //
            // Percent is 0 if sum is 0
            params.percent = !sum ? 0 : +(data.get('value', dataIndex) / sum * 100).toFixed(2);

            params.$vars.push('percent');
            return params;
        },

        _defaultLabelLine: function (option) {
            // Extend labelLine emphasis
            modelUtil.defaultEmphasis(option.labelLine, ['show']);

            var labelLineNormalOpt = option.labelLine.normal;
            var labelLineEmphasisOpt = option.labelLine.emphasis;
            // Not show label line if `label.normal.show = false`
            labelLineNormalOpt.show = labelLineNormalOpt.show
                && option.label.normal.show;
            labelLineEmphasisOpt.show = labelLineEmphasisOpt.show
                && option.label.emphasis.show;
        },

        defaultOption: {
            zlevel: 0,
            z: 2,
            legendHoverLink: true,

            hoverAnimation: true,
            // Default global center
            center: ['50%', '50%'],
            radius: [0, '75%'],
            // Default clockwise
            clockwise: true,
            startAngle: 90,
            // Minimum angle changed to 0
            minAngle: 0,
            // Selected is the sector offset
            selectedOffset: 10,

            // If use strategy to avoid label overlapping
            avoidLabelOverlap: true,
            // Select mode, off by default, single, multiple optional
            // selectedMode: false,
            // Nightingale rose pattern, 'radius' | 'area'
            // roseType: null,

            label: {
                normal: {
                    // If rotate around circle
                    rotate: false,
                    show: true,
                    // 'outer', 'inside', 'center'
                    position: 'outer'
                    // formatter: label text formatter, same as Tooltip.formatter, does not support asynchronous callback
                    // textStyle: null // Use global text style by default, see TEXTSTYLE for details
                    // distance: valid when position is inner, it is the proportional coefficient between the distance from the label position to the center of the circle and the circle radius (the donut diagram is the sum of the inner and outer radii)
                },
                emphasis: {}
            },
            // Enabled when label.normal.position is 'outer'
            labelLine: {
                normal: {
                    show: true,
                    // The length of the first of the two segments of the guide line
                    length: 15,
                    // The length of the second of the two segments of the guide line
                    length2: 15,
                    smooth: false,
                    lineStyle: {
                        // color: different,
                        width: 1,
                        type: 'solid'
                    }
                }
            },
            itemStyle: {
                normal: {
                    // color: different,
                    borderColor: 'rgba(0,0,0,0)',
                    borderWidth: 1
                },
                emphasis: {
                    // color: different,
                    borderColor: 'rgba(0,0,0,0)',
                    borderWidth: 1
                }
            },

            animationEasing: 'cubicOut',

            data: []
        }
    });

    zrUtil.mixin(PieSeries, dataSelectableMixin);

    return PieSeries;
});