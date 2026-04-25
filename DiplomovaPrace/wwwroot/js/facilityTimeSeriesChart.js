window.facilityTimeSeriesChart = (function () {
    'use strict';

    var _resizeObservers = new Map();

    function getInstance(containerId) {
        var element = document.getElementById(containerId);
        if (!element || !window.echarts) {
            return null;
        }

        var instance = window.echarts.getInstanceByDom(element) || window.echarts.init(element, null, { renderer: 'canvas' });
        ensureResizeObserver(containerId, element, instance);
        return instance;
    }

    function ensureResizeObserver(containerId, element, chart) {
        if (!window.ResizeObserver || _resizeObservers.has(containerId)) {
            return;
        }

        var rafToken = 0;
        var observer = new window.ResizeObserver(function () {
            if (rafToken) {
                window.cancelAnimationFrame(rafToken);
            }

            rafToken = window.requestAnimationFrame(function () {
                rafToken = 0;
                if (!chart || chart.isDisposed()) {
                    return;
                }

                chart.resize();
            });
        });

        observer.observe(element);
        _resizeObservers.set(containerId, observer);
    }

    function toTooltipDate(isoValue) {
        var date = new Date(isoValue);
        return date.toLocaleString('cs-CZ', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function toAxisDate(isoValue) {
        var date = new Date(isoValue);
        return date.toLocaleString('cs-CZ', {
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function toAxisDateByRange(isoValue, rangeMs) {
        var date = new Date(isoValue);

        if (rangeMs > 180 * 24 * 60 * 60 * 1000) {
            return date.toLocaleString('cs-CZ', {
                month: '2-digit',
                year: 'numeric'
            });
        }

        if (rangeMs > 40 * 24 * 60 * 60 * 1000) {
            return date.toLocaleString('cs-CZ', {
                day: '2-digit',
                month: '2-digit'
            });
        }

        if (rangeMs > 2 * 24 * 60 * 60 * 1000) {
            return date.toLocaleString('cs-CZ', {
                day: '2-digit',
                month: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            });
        }

        return toAxisDate(isoValue);
    }

    function render(containerId, model) {
        var chart = getInstance(containerId);
        if (!chart || !model || !Array.isArray(model.points)) {
            return;
        }

        var actualSeriesData = model.points.map(function (point) {
            return [point.timestampUtc, point.value];
        });

        var baselineSeriesData = Array.isArray(model.baselinePoints)
            ? model.baselinePoints.map(function (point) {
                return [point.timestampUtc, point.value];
            })
            : [];

        var hasBaseline = baselineSeriesData.length > 0;

        var pointCount = Math.max(actualSeriesData.length, baselineSeriesData.length);
        var firstTimestamp = pointCount > 0 ? new Date(actualSeriesData[0][0]).getTime() : 0;
        var lastTimestamp = pointCount > 1 ? new Date(actualSeriesData[actualSeriesData.length - 1][0]).getTime() : firstTimestamp;
        var rangeMs = Math.max(0, lastTimestamp - firstTimestamp);
        var denseSeries = pointCount >= 500;
        var useSampling = pointCount >= 300;
        var splitNumber = rangeMs > 30 * 24 * 60 * 60 * 1000
            ? 6
            : (rangeMs > 7 * 24 * 60 * 60 * 1000 ? 8 : 10);

        var series = [{
            type: 'line',
            name: model.actualSeriesName || model.title || 'Aktuální výkon',
            data: actualSeriesData,
            showSymbol: false,
            smooth: false,
            sampling: useSampling ? 'lttb' : undefined,
            progressive: 1000,
            progressiveThreshold: 2000,
            lineStyle: {
                width: denseSeries ? 1.8 : 2.2,
                color: '#0ea5e9'
            },
            areaStyle: {
                opacity: denseSeries ? 0.04 : 0.08,
                color: '#0ea5e9'
            }
        }];

        if (hasBaseline) {
            series.push({
                type: 'line',
                name: model.baselineSeriesName || 'Baseline reference',
                data: baselineSeriesData,
                showSymbol: false,
                smooth: false,
                sampling: useSampling ? 'lttb' : undefined,
                progressive: 1000,
                progressiveThreshold: 2000,
                lineStyle: {
                    width: denseSeries ? 1.2 : 1.6,
                    type: 'dashed',
                    color: '#64748b'
                },
                itemStyle: {
                    color: '#64748b'
                }
            });
        }

        chart.setOption({
            animation: pointCount < 1200,
            legend: hasBaseline
                ? {
                    top: 0,
                    right: 0,
                    textStyle: {
                        fontSize: 11,
                        color: '#334155'
                    }
                }
                : undefined,
            grid: {
                left: 54,
                right: 22,
                top: hasBaseline ? 36 : 24,
                bottom: 74,
                containLabel: false
            },
            tooltip: {
                trigger: 'axis',
                confine: true,
                axisPointer: { type: 'line' },
                formatter: function (params) {
                    if (!params || params.length === 0) {
                        return '';
                    }

                    var lines = [toTooltipDate(params[0].value[0])];

                    params.forEach(function (point) {
                        var value = Number(point.value[1]);
                        var formattedValue = Number.isFinite(value)
                            ? value.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                            : '-';

                        lines.push((point.marker || '') + (point.seriesName || '') + ': ' + formattedValue + ' ' + (model.unit || ''));
                    });

                    return lines.join('<br/>');
                }
            },
            dataZoom: [
                {
                    type: 'inside',
                    xAxisIndex: 0,
                    filterMode: 'none',
                    throttle: 80,
                    zoomOnMouseWheel: true,
                    moveOnMouseMove: true,
                    moveOnMouseWheel: true
                },
                {
                    type: 'slider',
                    xAxisIndex: 0,
                    height: 18,
                    bottom: 26,
                    showDetail: false,
                    brushSelect: false,
                    filterMode: 'none',
                    borderColor: '#cbd5e1',
                    fillerColor: 'rgba(14, 165, 233, 0.15)',
                    handleSize: '85%'
                }
            ],
            xAxis: {
                type: 'time',
                boundaryGap: false,
                splitNumber: splitNumber,
                min: 'dataMin',
                max: 'dataMax',
                axisTick: {
                    alignWithLabel: true
                },
                axisLabel: {
                    hideOverlap: true,
                    margin: 10,
                    formatter: function (value) {
                        return toAxisDateByRange(value, rangeMs);
                    }
                }
            },
            yAxis: {
                type: 'value',
                name: model.yAxisLabel || '',
                nameGap: 28,
                axisLabel: {
                    margin: 10
                },
                splitLine: {
                    lineStyle: { color: '#e2e8f0' }
                }
            },
            series: series
        }, true);

        chart.resize();
    }

    function renderCompare(containerId, model) {
        var chart = getInstance(containerId);
        if (!chart || !model || !Array.isArray(model.series)) {
            return;
        }

        var palette = ['#0ea5e9', '#f97316', '#16a34a', '#ef4444', '#14b8a6', '#0891b2'];
        var preparedSeries = model.series
            .map(function (series) {
                var points = Array.isArray(series.points)
                    ? series.points.map(function (point) {
                        return [point.timestampUtc, point.value];
                    })
                    : [];

                return {
                    name: series.name || series.nodeKey || 'Série',
                    isPrimary: !!series.isPrimary,
                    data: points
                };
            })
            .filter(function (series) {
                return series.data.length > 0;
            });

        if (preparedSeries.length === 0) {
            chart.clear();
            return;
        }

        var pointCount = preparedSeries.reduce(function (acc, series) {
            return Math.max(acc, series.data.length);
        }, 0);

        var firstTimestamp = Number.POSITIVE_INFINITY;
        var lastTimestamp = Number.NEGATIVE_INFINITY;

        preparedSeries.forEach(function (series) {
            var first = new Date(series.data[0][0]).getTime();
            var last = new Date(series.data[series.data.length - 1][0]).getTime();
            if (first < firstTimestamp) {
                firstTimestamp = first;
            }
            if (last > lastTimestamp) {
                lastTimestamp = last;
            }
        });

        if (!Number.isFinite(firstTimestamp) || !Number.isFinite(lastTimestamp)) {
            chart.clear();
            return;
        }

        var rangeMs = Math.max(0, lastTimestamp - firstTimestamp);
        var denseSeries = pointCount >= 500;
        var useSampling = pointCount >= 300;
        var splitNumber = rangeMs > 30 * 24 * 60 * 60 * 1000
            ? 6
            : (rangeMs > 7 * 24 * 60 * 60 * 1000 ? 8 : 10);

        var series = preparedSeries.map(function (item, index) {
            var color = palette[index % palette.length];
            var lineWidth = item.isPrimary
                ? (denseSeries ? 2.2 : 2.6)
                : (denseSeries ? 1.4 : 1.8);

            return {
                type: 'line',
                name: item.name,
                data: item.data,
                showSymbol: false,
                smooth: false,
                sampling: useSampling ? 'lttb' : undefined,
                progressive: 1000,
                progressiveThreshold: 2000,
                lineStyle: {
                    width: lineWidth,
                    color: color,
                    opacity: item.isPrimary ? 1 : 0.85
                },
                itemStyle: {
                    color: color
                },
                areaStyle: item.isPrimary
                    ? {
                        opacity: denseSeries ? 0.03 : 0.06,
                        color: color
                    }
                    : undefined
            };
        });

        chart.setOption({
            animation: pointCount < 1200,
            legend: {
                top: 0,
                right: 0,
                textStyle: {
                    fontSize: 11,
                    color: '#334155'
                }
            },
            grid: {
                left: 54,
                right: 22,
                top: 36,
                bottom: 74,
                containLabel: false
            },
            tooltip: {
                trigger: 'axis',
                confine: true,
                axisPointer: { type: 'line' },
                formatter: function (params) {
                    if (!params || params.length === 0) {
                        return '';
                    }

                    var lines = [toTooltipDate(params[0].value[0])];

                    params.forEach(function (point) {
                        var value = Number(point.value[1]);
                        var formattedValue = Number.isFinite(value)
                            ? value.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                            : '-';

                        lines.push((point.marker || '') + (point.seriesName || '') + ': ' + formattedValue + ' ' + (model.unit || ''));
                    });

                    return lines.join('<br/>');
                }
            },
            dataZoom: [
                {
                    type: 'inside',
                    xAxisIndex: 0,
                    filterMode: 'none',
                    throttle: 80,
                    zoomOnMouseWheel: true,
                    moveOnMouseMove: true,
                    moveOnMouseWheel: true
                },
                {
                    type: 'slider',
                    xAxisIndex: 0,
                    height: 18,
                    bottom: 26,
                    showDetail: false,
                    brushSelect: false,
                    filterMode: 'none',
                    borderColor: '#cbd5e1',
                    fillerColor: 'rgba(14, 165, 233, 0.15)',
                    handleSize: '85%'
                }
            ],
            xAxis: {
                type: 'time',
                boundaryGap: false,
                splitNumber: splitNumber,
                min: 'dataMin',
                max: 'dataMax',
                axisTick: {
                    alignWithLabel: true
                },
                axisLabel: {
                    hideOverlap: true,
                    margin: 10,
                    formatter: function (value) {
                        return toAxisDateByRange(value, rangeMs);
                    }
                }
            },
            yAxis: {
                type: 'value',
                name: model.yAxisLabel || '',
                nameGap: 28,
                axisLabel: {
                    margin: 10
                },
                splitLine: {
                    lineStyle: { color: '#e2e8f0' }
                }
            },
            series: series
        }, true);

        chart.resize();
    }

    function renderLoadDuration(containerId, model) {
        var chart = getInstance(containerId);
        if (!chart || !model || !Array.isArray(model.points)) {
            return;
        }

        var seriesData = model.points
            .map(function (point) {
                return [point.durationPercent, point.demandKw];
            })
            .filter(function (point) {
                return Number.isFinite(point[0]) && Number.isFinite(point[1]);
            })
            .sort(function (a, b) {
                return a[0] - b[0];
            });

        if (seriesData.length === 0) {
            chart.clear();
            return;
        }

        var denseSeries = seriesData.length >= 500;
        var useSampling = seriesData.length >= 300;

        chart.setOption({
            animation: seriesData.length < 1200,
            grid: {
                left: 54,
                right: 20,
                top: 24,
                bottom: 38,
                containLabel: false
            },
            tooltip: {
                trigger: 'axis',
                confine: true,
                axisPointer: { type: 'line' },
                formatter: function (params) {
                    if (!params || params.length === 0) {
                        return '';
                    }

                    var point = params[0];
                    var duration = Number(point.value[0]);
                    var demand = Number(point.value[1]);
                    var durationText = Number.isFinite(duration)
                        ? duration.toLocaleString('cs-CZ', { maximumFractionDigits: 1 })
                        : '-';
                    var demandText = Number.isFinite(demand)
                        ? demand.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                        : '-';

                    return 'Duration: ' + durationText + '%<br/>' + (point.marker || '') + (point.seriesName || 'Demand') + ': ' + demandText + ' ' + (model.unit || 'kW');
                }
            },
            xAxis: {
                type: 'value',
                min: 0,
                max: 100,
                name: 'Duration (%)',
                nameGap: 24,
                axisLabel: {
                    margin: 10,
                    formatter: function (value) {
                        return value + '%';
                    }
                },
                splitLine: {
                    lineStyle: { color: '#e2e8f0' }
                }
            },
            yAxis: {
                type: 'value',
                name: model.yAxisLabel || 'Demand (kW)',
                nameGap: 28,
                axisLabel: {
                    margin: 10
                },
                splitLine: {
                    lineStyle: { color: '#e2e8f0' }
                }
            },
            series: [
                {
                    type: 'line',
                    name: model.title || 'Load duration curve',
                    data: seriesData,
                    showSymbol: false,
                    smooth: false,
                    sampling: useSampling ? 'lttb' : undefined,
                    progressive: 1000,
                    progressiveThreshold: 2000,
                    lineStyle: {
                        width: denseSeries ? 1.8 : 2.2,
                        color: '#0ea5e9'
                    },
                    areaStyle: {
                        opacity: denseSeries ? 0.04 : 0.08,
                        color: '#0ea5e9'
                    }
                }
            ]
        }, true);

        chart.resize();
    }

    function dispose(containerId) {
        var element = document.getElementById(containerId);

        var observer = _resizeObservers.get(containerId);
        if (observer) {
            observer.disconnect();
            _resizeObservers.delete(containerId);
        }

        if (!element || !window.echarts) {
            return;
        }

        var chart = window.echarts.getInstanceByDom(element);
        if (chart) {
            chart.dispose();
        }
    }

    function resize(containerId) {
        var element = document.getElementById(containerId);
        if (!element || !window.echarts) {
            return;
        }

        var chart = window.echarts.getInstanceByDom(element);
        if (chart) {
            chart.resize();
        }
    }

    return {
        render: render,
        renderCompare: renderCompare,
        renderLoadDuration: renderLoadDuration,
        dispose: dispose,
        resize: resize
    };
})();
