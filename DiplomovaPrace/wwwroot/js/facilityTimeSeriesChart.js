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

        var seriesData = model.points.map(function (point) {
            return [point.timestampUtc, point.value];
        });

        var pointCount = seriesData.length;
        var firstTimestamp = pointCount > 0 ? new Date(seriesData[0][0]).getTime() : 0;
        var lastTimestamp = pointCount > 1 ? new Date(seriesData[pointCount - 1][0]).getTime() : firstTimestamp;
        var rangeMs = Math.max(0, lastTimestamp - firstTimestamp);
        var denseSeries = pointCount >= 500;
        var useSampling = pointCount >= 300;
        var splitNumber = rangeMs > 30 * 24 * 60 * 60 * 1000
            ? 6
            : (rangeMs > 7 * 24 * 60 * 60 * 1000 ? 8 : 10);

        chart.setOption({
            animation: pointCount < 1200,
            grid: {
                left: 54,
                right: 22,
                top: 24,
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

                    var point = params[0];
                    var value = Number(point.value[1]);
                    var formattedValue = Number.isFinite(value)
                        ? value.toLocaleString('cs-CZ', { maximumFractionDigits: 2 })
                        : '-';

                    return toTooltipDate(point.value[0]) + '<br/>' + formattedValue + ' ' + (model.unit || '');
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
            series: [{
                type: 'line',
                name: model.title || 'Časová řada',
                data: seriesData,
                showSymbol: false,
                smooth: false,
                sampling: useSampling ? 'lttb' : undefined,
                progressive: 1000,
                progressiveThreshold: 2000,
                lineStyle: {
                    width: denseSeries ? 1.6 : 2,
                    color: '#0ea5e9'
                },
                areaStyle: {
                    opacity: denseSeries ? 0.04 : 0.08,
                    color: '#0ea5e9'
                }
            }]
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
        dispose: dispose,
        resize: resize
    };
})();
