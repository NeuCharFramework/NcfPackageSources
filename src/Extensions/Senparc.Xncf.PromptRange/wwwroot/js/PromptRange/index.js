var app = new Vue({
    el: "#app",
    data: function () {
        return {
            loading: false,
            recentDays: 14,
            topN: 8,
            dashboard: {
                generatedAt: "",
                recentDays: 14,
                overview: {},
                usageTrend: [],
                topPrompts: [],
                topRanges: [],
                topModels: [],
                riskPrompts: [],
                insights: []
            },
            usageTrendChart: null,
            modelUsageChart: null,
            promptPageUid: "C6175B8E-9F79-4053-9523-F8E4AC0C3E18",
            aiKernelUid: "796D12D8-580B-40F3-A6E8-A5D9D2EABB69"
        };
    },
    computed: {
        overviewCards: function () {
            var overview = this.dashboard && this.dashboard.overview ? this.dashboard.overview : {};
            var totalPrompts = Number(overview.totalPrompts || 0);
            var draftPrompts = Number(overview.draftPrompts || 0);
            var draftRate = totalPrompts > 0 ? (draftPrompts * 100 / totalPrompts).toFixed(1) : "0.0";

            return [
                {
                    key: "rangeTotal",
                    label: "靶场总数",
                    value: this.formatInteger(overview.totalRanges || 0),
                    hint: "近7天活跃靶场 " + this.formatInteger(overview.activeRangesLast7Days || 0)
                },
                {
                    key: "promptTotal",
                    label: "靶道总数",
                    value: this.formatInteger(totalPrompts),
                    hint: "草稿 " + this.formatInteger(draftPrompts) + "（" + draftRate + "%）"
                },
                {
                    key: "resultTotal",
                    label: "累计打靶次数",
                    value: this.formatInteger(overview.totalResults || 0),
                    hint: "今日 " + this.formatInteger(overview.resultsToday || 0) + " / 近7天 " + this.formatInteger(overview.resultsLast7Days || 0)
                },
                {
                    key: "score",
                    label: "平均最终得分",
                    value: this.formatScore(overview.avgFinalScore),
                    hint: "评分覆盖率 " + this.formatPercent(overview.scoreCoverageRate || 0)
                },
                {
                    key: "token",
                    label: "累计 Token 消耗",
                    value: this.formatInteger(overview.totalTokens || 0),
                    hint: "近7天活跃靶道 " + this.formatInteger(overview.activePromptsLast7Days || 0)
                },
                {
                    key: "latency",
                    label: "平均响应耗时",
                    value: this.formatMs(overview.avgLatencyMs || 0),
                    hint: "全量打靶结果统计"
                }
            ];
        }
    },
    created: function () {
        this.fetchDashboard(false);
    },
    mounted: function () {
        window.addEventListener("resize", this.handleResize);
    },
    beforeDestroy: function () {
        window.removeEventListener("resize", this.handleResize);
        this.disposeCharts();
    },
    methods: {
        createEmptyDashboard: function () {
            return {
                generatedAt: "",
                recentDays: 14,
                overview: {},
                usageTrend: [],
                topPrompts: [],
                topRanges: [],
                topModels: [],
                riskPrompts: [],
                insights: []
            };
        },
        formatInteger: function (value) {
            var num = Number(value || 0);
            if (isNaN(num)) {
                return "0";
            }
            return num.toLocaleString();
        },
        formatScore: function (value) {
            var num = Number(value);
            if (isNaN(num) || num < 0) {
                return "--";
            }
            return num.toFixed(2);
        },
        formatPercent: function (value) {
            var num = Number(value);
            if (isNaN(num)) {
                return "0.00%";
            }
            return num.toFixed(2) + "%";
        },
        formatMs: function (value) {
            var num = Number(value);
            if (isNaN(num) || num <= 0) {
                return "--";
            }
            return num.toFixed(0) + " ms";
        },
        formatDateTime: function (value) {
            if (!value) {
                return "--";
            }
            var date = new Date(value);
            if (isNaN(date.getTime())) {
                return "--";
            }
            var y = date.getFullYear();
            var m = String(date.getMonth() + 1).padStart(2, "0");
            var d = String(date.getDate()).padStart(2, "0");
            var h = String(date.getHours()).padStart(2, "0");
            var min = String(date.getMinutes()).padStart(2, "0");
            return y + "-" + m + "-" + d + " " + h + ":" + min;
        },
        formatRangeName: function (alias, rangeName) {
            var safeAlias = (alias || "").trim();
            var safeRangeName = rangeName || "";
            if (safeAlias && safeRangeName) {
                return safeAlias + "（" + safeRangeName + "）";
            }
            return safeAlias || safeRangeName || "--";
        },
        formatModelName: function (model) {
            if (!model) {
                return "Unknown";
            }
            if (model.alias) {
                return model.alias;
            }
            if (model.deploymentName) {
                return model.deploymentName;
            }
            return "Model#" + model.modelId;
        },
        clampPercent: function (value) {
            var num = Number(value);
            if (isNaN(num)) {
                return 0;
            }
            return Math.max(0, Math.min(100, num));
        },
        scoreClass: function (score) {
            var num = Number(score);
            if (isNaN(num) || num < 0) {
                return "scoreMuted";
            }
            if (num >= 8) {
                return "scoreGood";
            }
            if (num >= 6) {
                return "scoreMedium";
            }
            return "scoreBad";
        },
        buildPromptUrl: function (rangeId, promptId) {
            var params = new URLSearchParams((window.location.search || "").replace(/^\?/, ""));
            if (!params.get("uid")) {
                params.set("uid", this.promptPageUid);
            }

            var hashParts = [];
            if (rangeId) {
                hashParts.push("rangeId=" + encodeURIComponent(rangeId));
            }
            if (promptId) {
                hashParts.push("promptId=" + encodeURIComponent(promptId));
            }

            var queryString = params.toString();
            var hashString = hashParts.length > 0 ? ("#" + hashParts.join("&")) : "";
            return "/Admin/PromptRange/Prompt" + (queryString ? ("?" + queryString) : "") + hashString;
        },
        goPromptWorkbench: function () {
            window.location.href = this.buildPromptUrl(null, null);
        },
        toAIKernel: function () {
            window.open("/Admin/AIKernel/Index?uid=" + this.aiKernelUid);
        },
        async fetchDashboard(showMessage) {
            this.loading = true;
            try {
                var requester = typeof servicePR !== "undefined" ? servicePR : axios;
                var response = await requester.get(
                    "/api/Senparc.Xncf.PromptRange/StatisticAppService/Xncf.PromptRange_StatisticAppService.GetDashboardAsync",
                    {
                        params: {
                            recentDays: this.recentDays,
                            topN: this.topN
                        }
                    }
                );

                if (response && response.data && response.data.success) {
                    this.dashboard = Object.assign(this.createEmptyDashboard(), response.data.data || {});
                    this.$nextTick(() => {
                        this.renderCharts();
                    });
                    if (showMessage) {
                        this.$message({
                            message: "看板已刷新",
                            type: "success"
                        });
                    }
                }
            } catch (error) {
                console.error("fetchDashboard error:", error);
            } finally {
                this.loading = false;
            }
        },
        renderCharts: function () {
            this.renderUsageTrendChart();
            this.renderModelUsageChart();
        },
        renderUsageTrendChart: function () {
            var trendData = this.dashboard && this.dashboard.usageTrend ? this.dashboard.usageTrend : [];
            var chartEl = document.getElementById("usageTrendChart");
            if (!chartEl || typeof echarts === "undefined") {
                return;
            }

            if (!this.usageTrendChart) {
                this.usageTrendChart = echarts.init(chartEl);
            }

            var xAxis = trendData.map(function (item) {
                return item.dateLabel || item.date || "";
            });
            var usageCount = trendData.map(function (item) {
                return Number(item.resultCount || 0);
            });
            var avgScores = trendData.map(function (item) {
                var score = Number(item.avgScore);
                return score >= 0 ? score : null;
            });

            this.usageTrendChart.setOption({
                color: ["#2f88ff", "#15b685"],
                tooltip: {
                    trigger: "axis"
                },
                legend: {
                    data: ["调用次数", "平均分"]
                },
                grid: {
                    left: 50,
                    right: 45,
                    top: 50,
                    bottom: 35
                },
                xAxis: {
                    type: "category",
                    data: xAxis,
                    boundaryGap: true
                },
                yAxis: [
                    {
                        type: "value",
                        name: "调用次数",
                        minInterval: 1
                    },
                    {
                        type: "value",
                        name: "平均分",
                        min: 0,
                        max: 10
                    }
                ],
                series: [
                    {
                        name: "调用次数",
                        type: "bar",
                        barMaxWidth: 24,
                        data: usageCount
                    },
                    {
                        name: "平均分",
                        type: "line",
                        yAxisIndex: 1,
                        smooth: true,
                        connectNulls: true,
                        data: avgScores
                    }
                ]
            }, true);
        },
        renderModelUsageChart: function () {
            var modelData = this.dashboard && this.dashboard.topModels ? this.dashboard.topModels : [];
            var chartEl = document.getElementById("modelUsageChart");
            if (!chartEl || typeof echarts === "undefined") {
                return;
            }

            if (!this.modelUsageChart) {
                this.modelUsageChart = echarts.init(chartEl);
            }

            var yAxis = modelData.map(this.formatModelName);
            var usageSeries = modelData.map(function (item) {
                return Number(item.usageCount || 0);
            });
            var tokenSeries = modelData.map(function (item) {
                return Number(item.tokenCost || 0);
            });

            this.modelUsageChart.setOption({
                color: ["#4361ee", "#ef476f"],
                tooltip: {
                    trigger: "axis",
                    axisPointer: {
                        type: "shadow"
                    }
                },
                legend: {
                    data: ["调用次数", "Token 消耗"]
                },
                grid: {
                    left: 110,
                    right: 25,
                    top: 50,
                    bottom: 30
                },
                xAxis: [
                    {
                        type: "value",
                        name: "调用次数"
                    },
                    {
                        type: "value",
                        name: "Token",
                        position: "top",
                        splitLine: {
                            show: false
                        }
                    }
                ],
                yAxis: {
                    type: "category",
                    data: yAxis
                },
                series: [
                    {
                        name: "调用次数",
                        type: "bar",
                        data: usageSeries,
                        barMaxWidth: 18
                    },
                    {
                        name: "Token 消耗",
                        type: "bar",
                        xAxisIndex: 1,
                        data: tokenSeries,
                        barMaxWidth: 18
                    }
                ]
            }, true);
        },
        handleResize: function () {
            if (this.usageTrendChart) {
                this.usageTrendChart.resize();
            }
            if (this.modelUsageChart) {
                this.modelUsageChart.resize();
            }
        },
        disposeCharts: function () {
            if (this.usageTrendChart) {
                this.usageTrendChart.dispose();
                this.usageTrendChart = null;
            }
            if (this.modelUsageChart) {
                this.modelUsageChart.dispose();
                this.modelUsageChart = null;
            }
        }
    }
});
