define(function(require) {

    'use strict';

    var createListFromArray = require('../helper/createListFromArray');
    var SeriesModel = require('../../model/Series');

    return SeriesModel.extend({

        type: 'series.line',

        dependencies: ['grid', 'polar'],

        getInitialData: function (option, ecModel) {
            return createListFromArray(option.data, this, ecModel);
        },

        defaultOption: {
            zlevel: 0,                  // One level cascading
            z: 2,                       // Second level stacking
            coordinateSystem: 'cartesian2d',
            legendHoverLink: true,

            hoverAnimation: true,
            // stack: null
            xAxisIndex: 0,
            yAxisIndex: 0,

            polarIndex: 0,

            // If clip the overflow value
            clipOverflow: true,

            label: {
                normal: {
                    position: 'top'
                }
            },
            // itemStyle: {
            //     normal: {},
            //     emphasis: {}
            // },
            lineStyle: {
                normal: {
                    width: 2,
                    type: 'solid'
                }
            },
            // areaStyle: {},

            smooth: false,
            smoothMonotone: null,
            // Inflection point graph type
            symbol: 'emptyCircle',
            // Inflection point graphic size
            symbolSize: 4,
            // Inflection point graphic rotation control
            symbolRotate: null,

            // Whether to display the symbol, it will only be displayed when the tooltip hovers
            showSymbol: true,
            // By default, only the main axis is displayed in the logo graphic (with the main axis label interval hidden strategy)
            showAllSymbol: false,

            // Whether to connect breakpoints
            connectNulls: false,

            // Data filtering, 'average', 'max', 'min', 'sum'
            sampling: 'none',

            animationEasing: 'linear'
        }
    });
});