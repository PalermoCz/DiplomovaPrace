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

    function resetZoomInstance(chart) {
        if (!chart || chart.isDisposed()) {
            return;
        }

        chart.setOption({
            dataZoom: [
                { start: 0, end: 100 },
                { start: 0, end: 100 }
            ]
        });
    }

    function downloadDataUrl(dataUrl, fileName) {
        if (!dataUrl) {
            return;
        }

        var link = document.createElement('a');
        link.href = dataUrl;
        link.download = (fileName || 'chart-snapshot') + '.png';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    function getOverlayPalette(index, pinned) {
        var pinnedPalette = [
            { line: '#f97316', fill: '#f97316' },
            { line: '#10b981', fill: '#10b981' },
            { line: '#8b5cf6', fill: '#8b5cf6' },
            { line: '#e11d48', fill: '#e11d48' },
            { line: '#f59e0b', fill: '#f59e0b' }
        ];
        var previewPalette = [
            { line: '#fb923c', fill: '#fb923c' },
            { line: '#34d399', fill: '#34d399' },
            { line: '#a78bfa', fill: '#a78bfa' },
            { line: '#fb7185', fill: '#fb7185' },
            { line: '#fbbf24', fill: '#fbbf24' }
        ];
        var palette = pinned ? pinnedPalette : previewPalette;
        return palette[index % palette.length];
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

        var overlaySeriesModels = Array.isArray(model.overlaySeries)
            ? model.overlaySeries.map(function (overlay) {
                return {
                    seriesName: overlay && overlay.seriesName ? overlay.seriesName : 'Contributor overlay',
                    pinned: !!(overlay && overlay.pinned),
                    points: Array.isArray(overlay && overlay.points)
                        ? overlay.points.map(function (point) {
                            return [point.timestampUtc, point.value];
                        })
                        : []
                };
            }).filter(function (overlay) {
                return overlay.points.length > 0;
            })
            : [];

        var hasBaseline = baselineSeriesData.length > 0;
        var hasOverlay = overlaySeriesModels.length > 0;
        var hasLegend = hasBaseline || hasOverlay;

        var pointCount = Math.max(actualSeriesData.length, baselineSeriesData.length);
        overlaySeriesModels.forEach(function (overlay) {
            pointCount = Math.max(pointCount, overlay.points.length);
        });
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
                width: denseSeries ? 2.1 : 2.6,
                color: '#0284c7'
            },
            areaStyle: {
                opacity: denseSeries ? 0.025 : 0.055,
                color: '#38bdf8'
            }
        }];

        overlaySeriesModels.forEach(function (overlay, overlayIndex) {
            var colors = getOverlayPalette(overlayIndex, overlay.pinned);
            series.push({
                type: 'line',
                name: overlay.seriesName || 'Contributor overlay',
                data: overlay.points,
                showSymbol: false,
                smooth: false,
                sampling: useSampling ? 'lttb' : undefined,
                progressive: 1000,
                progressiveThreshold: 2000,
                lineStyle: {
                    width: overlay.pinned ? (denseSeries ? 2.1 : 2.4) : (denseSeries ? 1.6 : 1.9),
                    type: overlay.pinned ? 'solid' : 'dashed',
                    color: colors.line,
                    opacity: overlay.pinned ? 0.98 : 0.9
                },
                itemStyle: {
                    color: colors.fill
                }
            });
        });

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
            legend: hasLegend
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
                left: 50,
                right: 16,
                top: model.compact ? 24 : 34,
                bottom: model.compact ? 54 : 68,
                containLabel: false
            },
            tooltip: {
                trigger: 'axis',
                confine: true,
                axisPointer: { type: 'line' },
                backgroundColor: 'rgba(15, 23, 42, 0.94)',
                borderWidth: 0,
                textStyle: {
                    color: '#f8fafc'
                },
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
                    height: model.compact ? 14 : 18,
                    bottom: model.compact ? 14 : 22,
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
                    lineStyle: { color: 'rgba(148, 163, 184, 0.28)' }
                }
            },
            series: series
        }, true);

        chart.off('dblclick');
        chart.on('dblclick', function () {
            resetZoomInstance(chart);
        });

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

    function renderTemperatureLoadScatter(containerId, model) {
        var chart = getInstance(containerId);
        if (!chart || !model || !Array.isArray(model.points)) {
            return;
        }

        var seriesData = model.points
            .map(function (point) {
                return [point.outdoorTemperatureC, point.loadValue, point.timestampUtc];
            })
            .filter(function (point) {
                return Number.isFinite(point[0]) && Number.isFinite(point[1]);
            });

        if (seriesData.length === 0) {
            chart.clear();
            return;
        }

        var denseSeries = seriesData.length >= 400;

        chart.setOption({
            animation: seriesData.length < 1200,
            grid: {
                left: 58,
                right: 20,
                top: 24,
                bottom: 42,
                containLabel: false
            },
            tooltip: {
                trigger: 'item',
                confine: true,
                formatter: function (param) {
                    if (!param || !Array.isArray(param.value)) {
                        return '';
                    }

                    var temperature = Number(param.value[0]);
                    var loadValue = Number(param.value[1]);
                    var timestampUtc = param.value[2];
                    var temperatureText = Number.isFinite(temperature)
                        ? temperature.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                        : '-';
                    var loadText = Number.isFinite(loadValue)
                        ? loadValue.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                        : '-';
                    var unitSuffix = model.unit ? ' ' + model.unit : '';
                    var lines = [];

                    if (timestampUtc) {
                        lines.push(toTooltipDate(timestampUtc));
                    }

                    lines.push('Ta: ' + temperatureText + ' C');
                    lines.push((param.marker || '') + (model.title || 'Load') + ': ' + loadText + unitSuffix);
                    return lines.join('<br/>');
                }
            },
            xAxis: {
                type: 'value',
                name: model.xAxisLabel || 'Outdoor temperature Ta (C)',
                nameGap: 24,
                axisLabel: {
                    margin: 10
                },
                splitLine: {
                    lineStyle: { color: '#e2e8f0' }
                }
            },
            yAxis: {
                type: 'value',
                name: model.yAxisLabel || 'Load',
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
                    type: 'scatter',
                    name: model.title || 'Temperature vs load',
                    data: seriesData,
                    symbolSize: denseSeries ? 6 : 8,
                    itemStyle: {
                        color: '#0ea5e9',
                        opacity: denseSeries ? 0.72 : 0.84
                    },
                    emphasis: {
                        itemStyle: {
                            color: '#0369a1'
                        }
                    }
                }
            ]
        }, true);

        chart.resize();
    }

    function resetZoom(containerId) {
        var chart = getInstance(containerId);
        resetZoomInstance(chart);
    }

    function exportPng(containerId, fileName) {
        var chart = getInstance(containerId);
        if (!chart || chart.isDisposed()) {
            return;
        }

        downloadDataUrl(chart.getDataURL({
            type: 'png',
            pixelRatio: 2,
            backgroundColor: '#ffffff'
        }), fileName);
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
        renderTemperatureLoadScatter: renderTemperatureLoadScatter,
        resetZoom: resetZoom,
        exportPng: exportPng,
        dispose: dispose,
        resize: resize
    };
})();
