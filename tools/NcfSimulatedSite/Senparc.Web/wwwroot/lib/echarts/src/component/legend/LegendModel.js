define(function(require) {

    'use strict';

    var zrUtil = require('zrender/core/util');
    var Model = require('../../model/Model');

    var LegendModel = require('../../echarts').extendComponentModel({

        type: 'legend',

        dependencies: ['series'],

        layoutMode: {
            type: 'box',
            ignoreSize: true
        },

        init: function (option, parentModel, ecModel) {
            this.mergeDefaultAndTheme(option, ecModel);

            option.selected = option.selected || {};

            this._updateData(ecModel);

            var legendData = this._data;
            // If has any selected in option.selected
            var selectedMap = this.option.selected;
            // If selectedMode is single, try to select one
            if (legendData[0] && this.get('selectedMode') === 'single') {
                var hasSelected = false;
                for (var name in selectedMap) {
                    if (selectedMap[name]) {
                        this.select(name);
                        hasSelected = true;
                    }
                }
                // Try select the first if selectedMode is single
                !hasSelected && this.select(legendData[0].get('name'));
            }
        },

        mergeOption: function (option) {
            LegendModel.superCall(this, 'mergeOption', option);

            this._updateData(this.ecModel);
        },

        _updateData: function (ecModel) {
            var legendData = zrUtil.map(this.get('data') || [], function (dataItem) {
                if (typeof dataItem === 'string') {
                    dataItem = {
                        name: dataItem
                    };
                }
                return new Model(dataItem, this, this.ecModel);
            }, this);
            this._data = legendData;

            var availableNames = zrUtil.map(ecModel.getSeries(), function (series) {
                return series.name;
            });
            ecModel.eachSeries(function (seriesModel) {
                if (seriesModel.legendDataProvider) {
                    var data = seriesModel.legendDataProvider();
                    availableNames = availableNames.concat(data.mapArray(data.getName));
                }
            });
            /**
             * @type {Array.<string>}
             * @private
             */
            this._availableNames = availableNames;
        },

        /**
         * @return {Array.<module:echarts/model/Model>}
         */
        getData: function () {
            return this._data;
        },

        /**
         * @param {string} name
         */
        select: function (name) {
            var selected = this.option.selected;
            var selectedMode = this.get('selectedMode');
            if (selectedMode === 'single') {
                var data = this._data;
                zrUtil.each(data, function (dataItem) {
                    selected[dataItem.get('name')] = false;
                });
            }
            selected[name] = true;
        },

        /**
         * @param {string} name
         */
        unSelect: function (name) {
            if (this.get('selectedMode') !== 'single') {
                this.option.selected[name] = false;
            }
        },

        /**
         * @param {string} name
         */
        toggleSelected: function (name) {
            var selected = this.option.selected;
            // Default is true
            if (!(name in selected)) {
                selected[name] = true;
            }
            this[selected[name] ? 'unSelect' : 'select'](name);
        },

        /**
         * @param {string} name
         */
        isSelected: function (name) {
            var selected = this.option.selected;
            return !((name in selected) && !selected[name])
                && zrUtil.indexOf(this._availableNames, name) >= 0;
        },

        defaultOption: {
            // One level cascading
            zlevel: 0,
            // Second level stacking
            z: 4,
            show: true,

            // Layout mode, the default is horizontal layout, optional:
            // 'horizontal' | 'vertical'
            orient: 'horizontal',

            left: 'center',
            // right: 'center',

            top: 'top',
            // bottom: 'top',

            // Align horizontally
            // 'auto' | 'left' | 'right'
            // The default is 'auto', which determines whether to align left or right based on the position of x
            align: 'auto',

            backgroundColor: 'rgba(0,0,0,0)',
            // Legend border color
            borderColor: '#ccc',
            // Legend border line width, unit px, default is 0 (no border)
            borderWidth: 0,
            // Legend padding, unit px, default padding in all directions is 5.
            // Accept arrays to set the top, right, bottom and left margins respectively, same as css
            padding: 5,
            // The interval between each item, in px, the default is 10,
            // Horizontal spacing is used for horizontal layout, and vertical spacing is used for vertical layout.
            itemGap: 10,
            // Legend graphic width
            itemWidth: 25,
            // Legend graphic height
            itemHeight: 14,
            textStyle: {
                // Legend text color
                color: '#333'
            },
            // formatter: '',
            // Select mode, the legend switch is turned on by default
            selectedMode: true
            // Configure the default selected state, which can be used with the LEGEND.SELECTED event for dynamic data loading
            // selected: null,
            // Legend content (see legend.data for details, each item in the array represents an item
            // data: [],
        }
    });

    return LegendModel;
});