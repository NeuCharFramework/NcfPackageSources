/*----------------------------------------------------------------
 * ResourceMonitor.js
 * Admin 首页“资源监控”图表引擎：
 * - 数据来自后台记录器（每 10 秒采样，CO2NET 缓存最多保留 24 小时，不写数据库）
 * - 非线性时间轴：越靠左时间跨度越大，越靠右越精细（右端接近秒级）
 *   默认 24h 视图分三段：[0,40%) 6h 前→24h 前（5 分钟档），
 *   [40%,70%) 30 分钟→6h（1 分钟档），[70%,100%) 最近 30 分钟（10 秒档）
 * - 交互：按住鼠标横向拖拽可框选时间范围放大；双击或“重置”回到 24 小时
 * 依赖：全局 echarts（3.x）、ncfT 由调用方通过 options.i18n 传入文案
 *----------------------------------------------------------------*/
(function (global) {
  'use strict';

  var FULL_SPAN_MS = 24 * 3600 * 1000;
  var FINE_SPAN_MS = 30 * 60 * 1000; // 10 秒档覆盖范围
  var MID_SPAN_MS = 6 * 3600 * 1000; // 1 分钟档覆盖范围
  var MIN_ZOOM_MS = 30 * 1000;       // 允许缩到的最小时间范围
  var X_MAX = 1000;                  // 虚拟横轴列数 0..1000
  var GRID = { left: 46, right: 18, top: 40, bottom: 42 };

  function pad2(value) {
    return value < 10 ? '0' + value : '' + value;
  }

  function formatTime(ts, withSeconds, crossDay) {
    var d = new Date(ts);
    if (isNaN(d.getTime())) {
      return '--';
    }
    var text = pad2(d.getHours()) + ':' + pad2(d.getMinutes()) +
      (withSeconds ? ':' + pad2(d.getSeconds()) : '');
    if (crossDay) {
      text = pad2(d.getMonth() + 1) + '-' + pad2(d.getDate()) + ' ' + text;
    }
    return text;
  }

  // 依据当前时间窗口构建分段非线性映射（时间 <-> 虚拟 x 列）
  function buildScale(wStart, wEnd) {
    var W = wEnd - wStart;
    if (W <= FINE_SPAN_MS * 1.15) {
      // 窗口本身较窄（≤~35 分钟）：数据已是 10 秒级，线性映射即可
      return {
        segments: [{ t0: wStart, t1: wEnd, x0: 0, x1: X_MAX }],
        timeToX: function (t) { return X_MAX * (t - wStart) / W; },
        xToTime: function (x) { return wStart + W * x / X_MAX; }
      };
    }
    var fineSpan = Math.min(FINE_SPAN_MS, 0.30 * W);
    var midSpan = Math.min(MID_SPAN_MS - FINE_SPAN_MS, 0.30 * W);
    if (fineSpan + midSpan >= W) {
      midSpan = W - fineSpan - 1;
    }
    var xMid0 = X_MAX * 0.40;
    var xFine0 = X_MAX * 0.70;
    var tFine0 = wEnd - fineSpan;
    var tMid0 = tFine0 - midSpan;
    var segments = [
      { t0: wStart, t1: tMid0, x0: 0, x1: xMid0 },
      { t0: tMid0, t1: tFine0, x0: xMid0, x1: xFine0 },
      { t0: tFine0, t1: wEnd, x0: xFine0, x1: X_MAX }
    ];
    function timeToX(t) {
      for (var i = 0; i < segments.length; i++) {
        var s = segments[i];
        if (t >= s.t0 && t <= s.t1) {
          var span = s.t1 - s.t0;
          return s.x0 + (span <= 0 ? 0 : (t - s.t0) / span * (s.x1 - s.x0));
        }
      }
      return t < wStart ? 0 : X_MAX;
    }
    function xToTime(x) {
      for (var i = 0; i < segments.length; i++) {
        var s = segments[i];
        if (x >= s.x0 && x <= s.x1) {
          var span = s.t1 - s.t0;
          return s.t0 + ((s.x1 - s.x0) <= 0 ? 0 : (x - s.x0) / (s.x1 - s.x0) * span);
        }
      }
      return x <= 0 ? wStart : wEnd;
    }
    return { segments: segments, timeToX: timeToX, xToTime: xToTime };
  }

  // 合并三个时间分档：同一 10 秒时间桶内优先保留更精细档的点
  function buildMergedPoints(payload) {
    var windows = (payload && payload.windows) || [];
    windows = windows.slice().sort(function (a, b) {
      return (b.resolutionSeconds || 0) - (a.resolutionSeconds || 0);
    });
    var buckets = {};
    windows.forEach(function (w) {
      (w.points || []).forEach(function (p) {
        if (!p || typeof p.t !== 'number') {
          return;
        }
        var bucket = Math.floor(p.t / 10000);
        if (!buckets[bucket]) {
          buckets[bucket] = { t: p.t, cpu: p.cpu, pcpu: p.pcpu, mem: p.mem, pmem: p.pmem };
        }
      });
    });
    return Object.keys(buckets).map(function (k) { return buckets[k]; })
      .sort(function (a, b) { return a.t - b.t; });
  }

  function create(el, options) {
    options = options || {};
    var i18n = options.i18n || {};
    var chart = null;
    var overlay = null;
    var emptyEl = null;
    var windowStart = 0;
    var windowEnd = 0;
    var live = true;
    var zoomed = false;
    var dragging = false;
    var dragStartX = 0;
    var lastPayload = null;
    var scale = null;
    var points = [];
    var ticks = {};
    var disposed = false;

    function serverNow() {
      return (lastPayload && typeof lastPayload.now === 'number')
        ? lastPayload.now
        : Date.now();
    }

    function setWindow(start, end, isLive) {
      windowStart = start;
      windowEnd = end;
      live = !!isLive;
      zoomed = !live;
      if (typeof options.onZoomChange === 'function') {
        options.onZoomChange(zoomed);
      }
    }

    function computeTicks() {
      ticks = {};
      var W = windowEnd - windowStart;
      var withSeconds = W < 90 * 60000;
      var crossDay = new Date(windowStart).getDate() !== new Date(windowEnd).getDate();
      ticks[0] = formatTime(windowStart, withSeconds, crossDay);
      ticks[X_MAX] = (!zoomed && live)
        ? (i18n.now || 'now')
        : formatTime(windowEnd, withSeconds, crossDay);
      var interiorCount = scale.segments.length === 1 ? 5 : 2;
      scale.segments.forEach(function (s) {
        if (s.x0 > 1 && s.x0 < X_MAX - 1) {
          ticks[s.x0] = formatTime(s.t0, withSeconds, crossDay);
        }
        for (var i = 1; i <= interiorCount; i++) {
          ticks[Math.round(s.x0 + (s.x1 - s.x0) * i / (interiorCount + 1))] =
            formatTime(s.t0 + (s.t1 - s.t0) * i / (interiorCount + 1), withSeconds, crossDay);
        }
      });
    }

    function makeSeries(field, name, color, dashed, width) {
      var arr = new Array(X_MAX + 1);
      for (var i = 0; i <= X_MAX; i++) {
        arr[i] = null;
      }
      points.forEach(function (p) {
        var v = p[field];
        if (v === null || v === undefined) {
          return;
        }
        var col = Math.round(scale.timeToX(p.t));
        if (col < 0) { col = 0; } else if (col > X_MAX) { col = X_MAX; }
        if (arr[col] === null) {
          arr[col] = v;
        }
      });
      return {
        name: name,
        type: 'line',
        showSymbol: false,
        smooth: false,
        connectNulls: true,
        data: arr,
        lineStyle: { color: color, width: width, type: dashed ? 'dashed' : 'solid' },
        itemStyle: { color: color }
      };
    }

    function render() {
      if (disposed || !chart || !scale) {
        return;
      }
      var W = windowEnd - windowStart;
      var withSeconds = W < 90 * 60000;
      var crossDay = new Date(windowStart).getDate() !== new Date(windowEnd).getDate();
      var merged = buildMergedPoints(lastPayload);
      var pad = 60000;
      points = merged.filter(function (p) {
        return p.t >= windowStart - pad && p.t <= windowEnd + pad;
      }).map(function (p) {
        if (p.t < windowStart) { p.t = windowStart; }
        if (p.t > windowEnd) { p.t = windowEnd; }
        return p;
      });
      computeTicks();

      chart.setOption({
        animation: false,
        grid: { left: GRID.left, right: GRID.right, top: GRID.top, bottom: GRID.bottom },
        tooltip: {
          trigger: 'axis',
          axisPointer: { type: 'line', lineStyle: { color: '#8c52ff', opacity: 0.5 } },
          formatter: function (params) {
            if (!params || !params.length) {
              return '';
            }
            var t = scale.xToTime(Number(params[0].dataIndex) || 0);
            var lines = [formatTime(t, true, crossDay)];
            params.forEach(function (p) {
              if (p.value === null || p.value === undefined) {
                return;
              }
              lines.push(p.marker + ' ' + p.seriesName + ': ' + Number(p.value).toFixed(1) + '%');
            });
            return lines.join('<br>');
          }
        },
        legend: {
          top: 2,
          left: 'center',
          data: [i18n.cpuHost, i18n.cpuWeb, i18n.memHost, i18n.memWeb].filter(Boolean)
        },
        xAxis: {
          type: 'category',
          boundaryGap: false,
          data: (function () {
            var arr = new Array(X_MAX + 1);
            for (var i = 0; i <= X_MAX; i++) {
              arr[i] = i;
            }
            return arr;
          })(),
          axisLine: { lineStyle: { color: '#dcdfe6' } },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: {
            interval: 0,
            color: '#909399',
            formatter: function (value, index) {
              return ticks[index] || '';
            }
          }
        },
        yAxis: {
          type: 'value',
          min: 0,
          max: 100,
          name: '%',
          nameTextStyle: { color: '#909399' },
          axisLabel: { color: '#909399' },
          splitLine: { lineStyle: { color: '#f0f2f5' } }
        },
        series: [
          makeSeries('cpu', i18n.cpuHost, '#8c52ff', false, 1.6),
          makeSeries('pcpu', i18n.cpuWeb, '#00a6a6', true, 1.4),
          makeSeries('mem', i18n.memHost, '#67c23a', false, 1.6),
          makeSeries('pmem', i18n.memWeb, '#f56c6c', true, 1.4)
        ]
      }, true);

      if (emptyEl) {
        emptyEl.style.display = points.length ? 'none' : 'flex';
      }
    }

    function update(payload) {
      if (disposed) {
        return;
      }
      lastPayload = payload;
      var now = serverNow();
      if (windowEnd === 0) {
        setWindow(now - FULL_SPAN_MS, now, true);
      } else if (live) {
        var width = windowEnd - windowStart;
        windowEnd = now;
        windowStart = now - width;
      }
      scale = buildScale(windowStart, windowEnd);
      render();
    }

    function reset() {
      var now = serverNow();
      setWindow(now - FULL_SPAN_MS, now, true);
      scale = buildScale(windowStart, windowEnd);
      render();
    }

    // ===== 框选放大 =====
    function localX(e) {
      var rect = el.getBoundingClientRect();
      return e.clientX - rect.left;
    }

    function columnFromPx(px) {
      var gridWidth = el.clientWidth - GRID.left - GRID.right;
      if (gridWidth <= 0) {
        return 0;
      }
      var col = (px - GRID.left) / gridWidth * X_MAX;
      return Math.max(0, Math.min(X_MAX, col));
    }

    function onMouseDown(e) {
      if (e.button !== 0 || disposed) {
        return;
      }
      dragging = true;
      dragStartX = localX(e);
      el.style.cursor = 'col-resize';
      e.preventDefault();
    }

    function onMouseMove(e) {
      if (!dragging) {
        return;
      }
      var x = localX(e);
      var left = Math.min(dragStartX, x);
      var width = Math.abs(x - dragStartX);
      overlay.style.display = 'block';
      overlay.style.left = left + 'px';
      overlay.style.top = GRID.top + 'px';
      overlay.style.width = width + 'px';
      overlay.style.height = Math.max(0, el.clientHeight - GRID.top - GRID.bottom) + 'px';
    }

    function onMouseUp(e) {
      if (!dragging) {
        return;
      }
      dragging = false;
      el.style.cursor = '';
      overlay.style.display = 'none';
      if (disposed || !scale) {
        return;
      }
      var x = localX(e);
      var c1 = columnFromPx(Math.min(dragStartX, x));
      var c2 = columnFromPx(Math.max(dragStartX, x));
      if (c2 - c1 < 2) {
        return; // 视为单击，忽略
      }
      var t1 = scale.xToTime(c1);
      var t2 = scale.xToTime(c2);
      if (t2 - t1 < MIN_ZOOM_MS) {
        return;
      }
      setWindow(t1, t2, false);
      scale = buildScale(windowStart, windowEnd);
      render();
    }

    var onResize = function () {
      if (chart && !disposed) {
        chart.resize();
      }
    };

    chart = echarts.init(el);
    emptyEl = document.createElement('div');
    emptyEl.className = 'rm-empty';
    emptyEl.textContent = i18n.empty || '';
    emptyEl.style.display = 'none';
    el.appendChild(emptyEl);
    overlay = document.createElement('div');
    overlay.className = 'rm-select-overlay';
    overlay.style.display = 'none';
    el.appendChild(overlay);

    el.addEventListener('mousedown', onMouseDown);
    el.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
    el.addEventListener('dblclick', function () { reset(); });
    window.addEventListener('resize', onResize);

    return {
      update: update,
      reset: reset,
      resize: onResize,
      isZoomed: function () { return zoomed; },
      dispose: function () {
        disposed = true;
        window.removeEventListener('mouseup', onMouseUp);
        window.removeEventListener('resize', onResize);
        el.removeEventListener('mousedown', onMouseDown);
        el.removeEventListener('mousemove', onMouseMove);
        if (chart) {
          chart.dispose();
          chart = null;
        }
        if (overlay && overlay.parentNode) {
          overlay.parentNode.removeChild(overlay);
        }
        if (emptyEl && emptyEl.parentNode) {
          emptyEl.parentNode.removeChild(emptyEl);
        }
      }
    };
  }

  global.ResourceMonitorChart = {
    create: create,
    FullSpanMs: FULL_SPAN_MS
  };
})(window);
