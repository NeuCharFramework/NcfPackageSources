define(function (require) {

    var List = require('../../data/List');
    var SeriesModel = require('../../model/Series');
    var zrUtil = require('zrender/core/util');
    var completeDimensions = require('../../data/helper/completeDimensions');

    var formatUtil = require('../../util/format');
    var encodeHTML = formatUtil.encodeHTML;
    var addCommas = formatUtil.addCommas;

    var dataSelectableMixin = require('../../component/helper/selectableMixin');

    var geoCreator = require('../../coord/geo/geoCreator');

    var MapSeries = SeriesModel.extend({

        type: 'series.map',

        /**
         * Only first map series of same mapType will drawMap
         * @type {boolean}
         */
        needsDrawMap: false,

        /**
         * Group of all map series with same mapType
         * @type {boolean}
         */
        seriesGroup: [],

        init: function (option) {

            option = this._fillOption(option, option.map);
            this.option = option;

            MapSeries.superApply(this, 'init', arguments);

            this.updateSelectedMap(option.data);
        },

        getInitialData: function (option) {
            var dimensions = completeDimensions(['value'], option.data || []);

            var list = new List(dimensions, this);

            list.initData(option.data);

            return list;
        },

        mergeOption: function (newOption) {
            if (newOption.data) {
                newOption = this._fillOption(newOption, this.option.map);
            }

            MapSeries.superCall(this, 'mergeOption', newOption);

            this.updateSelectedMap(this.option.data);
        },

        _fillOption: function (option, mapName) {
            // Shallow clone
            option = zrUtil.extend({}, option);

            option.data = geoCreator.getFilledRegions(option.data, mapName);

            return option;
        },

        getRawValue: function (dataIndex) {
            // Use value stored in data instead because it is calculated from multiple series
            // FIXME Provide all value of multiple series ?
            return this._data.get('value', dataIndex);
        },

        /**
         * Get model of region
         * @param  {string} name
         * @return {module:echarts/model/Model}
         */
        getRegionModel: function (regionName) {
            var data = this.getData();
            return data.getItemModel(data.indexOfName(regionName));
        },

        /**
         * Map tooltip formatter
         *
         * @param {number} dataIndex
         */
        formatTooltip: function (dataIndex) {
            var data = this._data;
            var formattedValue = addCommas(this.getRawValue(dataIndex));
            var name = data.getName(dataIndex);

            var seriesGroup = this.seriesGroup;
            var seriesNames = [];
            for (var i = 0; i < seriesGroup.length; i++) {
                if (!isNaN(seriesGroup[i].getRawValue(dataIndex))) {
                    seriesNames.push(
                        encodeHTML(seriesGroup[i].name)
                    );
                }
            }

            return seriesNames.join(', ') + '<br />'
                + name + ' : ' + formattedValue;
        },

        defaultOption: {
            // One level cascading
            zlevel: 0,
            // Second level stacking
            z: 2,
            coordinateSystem: 'geo',
            // The maps of each province are temporarily in Chinese.
            map: 'china',

            // 'center' | 'left' | 'right' | 'x%' | {number}
            left: 'center',
            // 'center' | 'top' | 'bottom' | 'x%' | {number}
            top: 'center',
            // right
            // bottom
            // width:
            // height // adaptive

            // Numeric merging method, default sum, optional:
            // 'sum' | 'average' | 'max' | 'min'
            // mapValueCalculation: 'sum',
            // Decimal precision of map numerical calculation results
            // mapValuePrecision: 0,
            // Displays the legend color identification (the small dot of the series identification), which is valid when the legend is turned on.
            showLegendSymbol: true,
            // Select mode, off by default, single, multiple optional
            // selectedMode: false,
            dataRangeHoverLink: true,
            // Whether to enable zoom and roaming modes
            // roam: false,

            // Default on center of map
            center: null,

            zoom: 1,

            scaleLimit: null,

            label: {
                normal: {
                    show: false,
                    textStyle: {
                        color: '#000'
                    }
                },
                emphasis: {
                    show: true,
                    textStyle: {
                        color: 'rgb(100,0,0)'
                    }
                }
            },
            // scaleLimit: null,
            itemStyle: {
                normal: {
                    // color: different,
                    borderWidth: 0.5,
                    borderColor: '#444',
                    areaColor: '#eee'
                },
                // Also selected style
                emphasis: {
                    areaColor: 'rgba(255,215, 0, 0.8)'
                }
            }
        },

        setZoom: function (zoom) {
            this.option.zoom = zoom;
        },

        setCenter: function (center) {
            this.option.center = center;
        }
    });

    zrUtil.mixin(MapSeries, dataSelectableMixin);

    return MapSeries;
});