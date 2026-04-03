define(function(require) {

    var List = require('../../data/List');
    var zrUtil = require('zrender/core/util');
    var SeriesModel = require('../../model/Series');

    return SeriesModel.extend({

        type: 'series.parallel',

        dependencies: ['parallel'],

        getInitialData: function (option, ecModel) {
            var parallelModel = ecModel.getComponent(
                'parallel', this.get('parallelIndex')
            );
            var dimensions = parallelModel.dimensions;
            var parallelAxisIndices = parallelModel.parallelAxisIndex;

            var rawData = option.data;

            var dimensionsInfo = zrUtil.map(dimensions, function (dim, index) {
                var axisModel = ecModel.getComponent(
                    'parallelAxis', parallelAxisIndices[index]
                );
                if (axisModel.get('type') === 'category') {
                    translateCategoryValue(axisModel, dim, rawData);
                    return {name: dim, type: 'ordinal'};
                }
                else {
                    return dim;
                }
            });

            var list = new List(dimensionsInfo, this);
            list.initData(rawData);

            return list;
        },

        /**
         * User can get data raw indices on 'axisAreaSelected' event received.
         *
         * @public
         * @param {string} activeState 'active' or 'inactive' or 'normal'
         * @return {Array.<number>} Raw indices
         */
        getRawIndicesByActiveState: function (activeState) {
            var coordSys = this.coordinateSystem;
            var data = this.getData();
            var indices = [];

            coordSys.eachActiveState(data, function (theActiveState, dataIndex) {
                if (activeState === theActiveState) {
                    indices.push(data.getRawIndex(dataIndex));
                }
            });

            return indices;
        },

        defaultOption: {
            zlevel: 0,                  // One level cascading
            z: 2,                       // Second level stacking

            coordinateSystem: 'parallel',
            parallelIndex: 0,

            // FIXME is not available yet
            label: {
                normal: {
                    show: false
                    // formatter: label text formatter, same as Tooltip.formatter, does not support asynchronous callback
                    // position: The default is adaptive, the horizontal layout is 'top', the vertical layout is 'right', optional
                    //           'inside'|'left'|'right'|'top'|'bottom'
                    // textStyle: null // Use global text style by default, see TEXTSTYLE for details
                },
                emphasis: {
                    show: false
                    // formatter: label text formatter, same as Tooltip.formatter, does not support asynchronous callback
                    // position: The default is adaptive, the horizontal layout is 'top', the vertical layout is 'right', optional
                    //           'inside'|'left'|'right'|'top'|'bottom'
                    // textStyle: null // Use global text style by default, see TEXTSTYLE for details
                }
            },

            inactiveOpacity: 0.05,
            activeOpacity: 1,

            lineStyle: {
                normal: {
                    width: 2,
                    opacity: 0.45,
                    type: 'solid'
                }
            },
            // smooth: false

            animationEasing: 'linear'
        }
    });

    function translateCategoryValue(axisModel, dim, rawData) {
        var axisData = axisModel.get('data');
        var numberDim = +dim.replace('dim', '');

        if (axisData && axisData.length) {
            zrUtil.each(rawData, function (dataItem) {
                if (!dataItem) {
                    return;
                }
                var index = zrUtil.indexOf(axisData, dataItem[numberDim]);
                dataItem[numberDim] = index >= 0 ? index : NaN;
            });
        }
        // FIXME
        // If axis data is not set, it should be calculated automatically or prompted.
    }
});