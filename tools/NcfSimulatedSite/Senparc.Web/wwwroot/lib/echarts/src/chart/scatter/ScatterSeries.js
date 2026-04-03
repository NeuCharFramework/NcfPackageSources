define(function (require) {

    'use strict';

    var createListFromArray = require('../helper/createListFromArray');
    var SeriesModel = require('../../model/Series');

    return SeriesModel.extend({

        type: 'series.scatter',

        dependencies: ['grid', 'polar'],

        getInitialData: function (option, ecModel) {
            var list = createListFromArray(option.data, this, ecModel);
            return list;
        },

        defaultOption: {
            coordinateSystem: 'cartesian2d',
            zlevel: 0,
            z: 2,
            legendHoverLink: true,

            hoverAnimation: true,
            // Cartesian coordinate system
            xAxisIndex: 0,
            yAxisIndex: 0,

            // Polar coordinate system
            polarIndex: 0,

            // Geo coordinate system
            geoIndex: 0,

            // symbol: null, // graphic type
            symbolSize: 10,          // Graphic size, half-width (radius) parameter, when the graphic is a direction or diamond, the total width is symbolSize * 2
            // symbolRotate: null, // Graphic rotation control

            large: false,
            // Available when large is true
            largeThreshold: 2000,

            // label: {
                // normal: {
                    // show: false
                    // distance: 5,
                    // formatter: label text formatter, same as Tooltip.formatter, does not support asynchronous callback
                    // position: The default is adaptive, the horizontal layout is 'top', the vertical layout is 'right', optional
                    //           'inside'|'left'|'right'|'top'|'bottom'
                    // textStyle: null // Use global text style by default, see TEXTSTYLE for details
            //     }
            // },
            itemStyle: {
                normal: {
                    opacity: 0.8
                    // color: various
                }
            }
        }
    });
});