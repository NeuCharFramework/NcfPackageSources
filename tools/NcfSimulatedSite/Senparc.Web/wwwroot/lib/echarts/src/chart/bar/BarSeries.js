define(function(require) {

    'use strict';

    var SeriesModel = require('../../model/Series');
    var createListFromArray = require('../helper/createListFromArray');

    return SeriesModel.extend({

        type: 'series.bar',

        dependencies: ['grid', 'polar'],

        getInitialData: function (option, ecModel) {
            return createListFromArray(option.data, this, ecModel);
        },

        getMarkerPosition: function (value) {
            var coordSys = this.coordinateSystem;
            if (coordSys) {
                var pt = coordSys.dataToPoint(value);
                var data = this.getData();
                var offset = data.getLayout('offset');
                var size = data.getLayout('size');
                var offsetIndex = coordSys.getBaseAxis().isHorizontal() ? 0 : 1;
                pt[offsetIndex] += offset + size / 2;
                return pt;
            }
            return [NaN, NaN];
        },

        defaultOption: {
            zlevel: 0,                  // One level cascading
            z: 2,                       // Second level stacking
            coordinateSystem: 'cartesian2d',
            legendHoverLink: true,
            // stack: null

            // Cartesian coordinate system
            xAxisIndex: 0,
            yAxisIndex: 0,

            // Minimum height changed to 0
            barMinHeight: 0,

            // barMaxWidth: null,
            // Default adaptive
            // barWidth: null,
            // The distance between columns, the default is 30% of the column width, and can be set to a fixed value
            // barGap: '30%',
            // Column distance between categories, the default is 20% of the category spacing, and can be set to a fixed value
            // barCategoryGap: '20%',
            // label: {
            //     normal: {
            //         show: false
            //     }
            // },
            itemStyle: {
                normal: {
                    // color: 'different',
                    // Column edge
                    barBorderColor: '#fff',
                    // Column edge line width, unit px, default is 1
                    barBorderWidth: 0
                },
                emphasis: {
                    // color: 'different',
                    // Column edge
                    barBorderColor: '#fff',
                    // Column edge line width, unit px, default is 1
                    barBorderWidth: 0
                }
            }
        }
    });
});