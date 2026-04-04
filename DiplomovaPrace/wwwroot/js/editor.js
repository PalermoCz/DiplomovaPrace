/**
 * editor.js — SVG canvas interop pro konfigurační editor budov.
 * Zodpovědnosti:
 *   - Pan (střední tlačítko nebo Space+drag)
 *   - Zoom (scroll wheel, střed ve směru kurzoru)
 *   - Kreslení místností rubber-band drahem (AddRoom nástroj)
 *   - Přetahování zařízení drag & drop (Select nástroj)
 *   - Detekce kliků na prvky / prázdný canvas
 *   - Volání JSInvokable metod na Blazor komponentě
 *
 * Blazor re-renderuje statický obsah SVG, JS řídí interakci.
 * Blazor neemituje 'transform' na #<svgId>-content, takže JS transform přežije re-render.
 */
window.editorCanvas = (function () {
    'use strict';

    var _svgId = null;
    var _dotNet = null;
    var _tool = 'Select';
    var _transform = { tx: 0, ty: 0, scale: 1.0 };
    var _state = null;
    var _spaceDown = false;
    var _handlers = {};
    var _floorBounds = { w: 8000, h: 3000 }; // velké výchozí — neomezuje dokud nenastaven
    var _gridEnabled = false;
    var _gridSize    = 20;

    // ── Helpers ──────────────────────────────────────────────────────────────

    function getSvg() { return document.getElementById(_svgId); }
    function getContent() { return document.getElementById(_svgId + '-content'); }

    function applyTransform() {
        var g = getContent();
        if (g) g.setAttribute('transform',
            'translate(' + _transform.tx + ' ' + _transform.ty + ') scale(' + _transform.scale + ')');
    }

    /**
     * Převod klientských souřadnic → souřadnice v prostoru content group.
     * Respektuje SVG viewBox a aktuální pan/zoom transform.
     */
    function clientToContent(clientX, clientY) {
        var svg = getSvg();
        if (!svg || !svg.getScreenCTM) return { x: clientX, y: clientY };
        var pt = svg.createSVGPoint();
        pt.x = clientX; pt.y = clientY;
        var svgPt = pt.matrixTransform(svg.getScreenCTM().inverse());
        return {
            x: (svgPt.x - _transform.tx) / _transform.scale,
            y: (svgPt.y - _transform.ty) / _transform.scale
        };
    }

    /**
     * Prochází DOM nahoru od el, hledá element s data-<attrName>.
     * Zastaví se na <svg> elementu.
     */
    function findWithData(el, attrName) {
        var svg = getSvg();
        while (el && el !== svg && el !== document.body) {
            if (el.dataset && el.dataset[attrName]) return el;
            el = el.parentElement;
        }
        return null;
    }

    function removePreview() {
        var el = document.getElementById(_svgId + '-preview');
        if (el) el.remove();
    }

    /** Přichytí hodnotu k mřížce (pokud je mřížka aktivní). */
    function snapToGrid(val) {
        return _gridEnabled ? Math.round(val / _gridSize) * _gridSize : val;
    }

    // ── Event Handlers ───────────────────────────────────────────────────────

    function onMouseDown(e) {
        var svg = getSvg();
        if (!svg) return;

        // Pan: střední tlačítko nebo Space + levé tlačítko
        if (e.button === 1 || (e.button === 0 && _spaceDown)) {
            e.preventDefault();
            svg.style.cursor = 'grabbing';
            _state = {
                type: 'pan',
                startCX: e.clientX, startCY: e.clientY,
                origTx: _transform.tx, origTy: _transform.ty
            };
            return;
        }

        if (e.button !== 0) return;
        var pos = clientToContent(e.clientX, e.clientY);

        if (_tool === 'AddRoom') {
            e.preventDefault();
            var startX = snapToGrid(pos.x);
            var startY = snapToGrid(pos.y);
            // Vytvoř preview rect jako SVG element přidaný přímo do content group
            var g = getContent();
            var rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            rect.setAttribute('id', _svgId + '-preview');
            rect.setAttribute('x', startX); rect.setAttribute('y', startY);
            rect.setAttribute('width', '0'); rect.setAttribute('height', '0');
            rect.setAttribute('fill', 'rgba(41,128,185,0.12)');
            rect.setAttribute('stroke', '#2980b9');
            rect.setAttribute('stroke-width', String(1.5 / _transform.scale));
            rect.setAttribute('stroke-dasharray', String(6 / _transform.scale) + ',' + String(3 / _transform.scale));
            rect.setAttribute('pointer-events', 'none');
            if (g) g.appendChild(rect);
            _state = { type: 'draw', startX: startX, startY: startY, currX: startX, currY: startY };
            return;
        }

        if (_tool === 'Select') {
            var deviceEl = findWithData(e.target, 'deviceId');
            if (deviceEl) {
                e.stopPropagation();
                var circle = deviceEl.querySelector('circle');
                var origCx = circle ? parseFloat(circle.getAttribute('cx')) : pos.x;
                var origCy = circle ? parseFloat(circle.getAttribute('cy')) : pos.y;
                _state = {
                    type: 'drag',
                    deviceId: deviceEl.dataset.deviceId,
                    startCX: pos.x, startCY: pos.y,
                    origCx: origCx, origCy: origCy,
                    el: deviceEl, moved: false
                };
                return;
            }
        }
    }

    function onMouseMove(e) {
        if (!_state) return;
        var pos = clientToContent(e.clientX, e.clientY);

        if (_state.type === 'pan') {
            var dx = e.clientX - _state.startCX;
            var dy = e.clientY - _state.startCY;
            _transform.tx = _state.origTx + dx;
            _transform.ty = _state.origTy + dy;
            applyTransform();
            return;
        }

        if (_state.type === 'draw') {
            var sx = snapToGrid(pos.x);
            var sy = snapToGrid(pos.y);
            _state.currX = sx; _state.currY = sy;
            var preview = document.getElementById(_svgId + '-preview');
            if (preview) {
                var rx = Math.min(_state.startX, sx);
                var ry = Math.min(_state.startY, sy);
                var rw = Math.abs(sx - _state.startX);
                var rh = Math.abs(sy - _state.startY);
                preview.setAttribute('x', rx); preview.setAttribute('y', ry);
                preview.setAttribute('width', rw); preview.setAttribute('height', rh);
            }
            return;
        }

        if (_state.type === 'drag') {
            var ddx = pos.x - _state.startCX;
            var ddy = pos.y - _state.startCY;
            if (Math.abs(ddx) > 3 || Math.abs(ddy) > 3) _state.moved = true;
            if (_state.moved) {
                _state.el.setAttribute('transform', 'translate(' + ddx + ' ' + ddy + ')');
            }
        }
    }

    function onMouseUp(e) {
        if (!_state) return;
        var s = _state;
        _state = null;

        var svg = getSvg();
        if (svg && !_spaceDown) svg.style.cursor = cursorForTool(_tool);

        if (s.type === 'pan') return;

        if (s.type === 'draw') {
            removePreview();
            var rx = Math.max(0, Math.min(s.startX, s.currX));
            var ry = Math.max(0, Math.min(s.startY, s.currY));
            var rw = Math.min(Math.abs(s.currX - s.startX), _floorBounds.w - rx);
            var rh = Math.min(Math.abs(s.currY - s.startY), _floorBounds.h - ry);
            if (rw > 20 && rh > 20 && _dotNet)
                _dotNet.invokeMethodAsync('JsOnRoomDrawn', rx, ry, rw, rh);
            return;
        }

        if (s.type === 'drag') {
            s.el.removeAttribute('transform');
            if (!s.moved) {
                // Klik bez tahu → select
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', s.deviceId, 'Device');
            } else {
                var pos = clientToContent(e.clientX, e.clientY);
                var finalX = Math.max(0, Math.min(_floorBounds.w, s.origCx + (pos.x - s.startCX)));
                var finalY = Math.max(0, Math.min(_floorBounds.h, s.origCy + (pos.y - s.startCY)));
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnDeviceDragEnd', s.deviceId, finalX, finalY);
            }
        }
    }

    function onClick(e) {
        // Zpracuje kliknutí, která nejsou součástí kreslení/tahu
        // (draw/drag se uzavírají v onMouseUp)
        if (_state) return; // stále v interakci (pan)
        var pos = clientToContent(e.clientX, e.clientY);

        if (_tool === 'Select') {
            var deviceEl = findWithData(e.target, 'deviceId');
            var roomEl = findWithData(e.target, 'roomId');
            var nodeEl = findWithData(e.target, 'nodeKey');
            if (deviceEl) {
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', deviceEl.dataset.deviceId, 'Device');
            } else if (roomEl) {
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', roomEl.dataset.roomId, 'Room');
            } else if (nodeEl) {
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', nodeEl.dataset.nodeKey, 'FacilityNode');
            } else {
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', '', 'None');
            }
        } else if (_tool === 'AddDevice') {
            if (_dotNet) _dotNet.invokeMethodAsync('JsOnCanvasClicked', pos.x, pos.y);
        } else if (_tool === 'Delete') {
            var deviceEl2 = findWithData(e.target, 'deviceId');
            var roomEl2 = findWithData(e.target, 'roomId');
            if (deviceEl2 && _dotNet)
                _dotNet.invokeMethodAsync('JsOnDeleteClicked', deviceEl2.dataset.deviceId, 'Device');
            else if (roomEl2 && _dotNet)
                _dotNet.invokeMethodAsync('JsOnDeleteClicked', roomEl2.dataset.roomId, 'Room');
        }
    }

    function onWheel(e) {
        e.preventDefault();
        var svg = getSvg();
        if (!svg) return;
        var factor = e.deltaY < 0 ? 1.12 : 0.89;
        var newScale = Math.max(0.15, Math.min(6.0, _transform.scale * factor));
        if (Math.abs(newScale - _transform.scale) < 0.001) return;
        // Zoom směrem k pozici kurzoru
        var pt = svg.createSVGPoint();
        pt.x = e.clientX; pt.y = e.clientY;
        var anchor = pt.matrixTransform(svg.getScreenCTM().inverse());
        var ratio = newScale / _transform.scale;
        _transform.tx = anchor.x + (_transform.tx - anchor.x) * ratio;
        _transform.ty = anchor.y + (_transform.ty - anchor.y) * ratio;
        _transform.scale = newScale;
        applyTransform();
    }

    function onKeyDown(e) {
        // Ctrl+Z = Undo, Ctrl+Y / Ctrl+Shift+Z = Redo
        if ((e.ctrlKey || e.metaKey) && !e.repeat && !e.target.matches('input, textarea')) {
            if (e.code === 'KeyZ' && !e.shiftKey) {
                e.preventDefault();
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnKeyboardShortcut', 'undo');
                return;
            }
            if (e.code === 'KeyY' || (e.code === 'KeyZ' && e.shiftKey)) {
                e.preventDefault();
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnKeyboardShortcut', 'redo');
                return;
            }
        }
        if (e.code === 'Space' && !e.repeat && !e.target.matches('input, textarea')) {
            _spaceDown = true;
            var svg = getSvg();
            if (svg) svg.style.cursor = 'grab';
            e.preventDefault();
        }
    }

    function onKeyUp(e) {
        if (e.code === 'Space') {
            _spaceDown = false;
            var svg = getSvg();
            if (svg) svg.style.cursor = cursorForTool(_tool);
        }
    }

    function cursorForTool(tool) {
        var map = { Select: 'default', AddRoom: 'crosshair', AddDevice: 'cell', Delete: 'not-allowed' };
        return map[tool] || 'default';
    }

    // ── Public API ───────────────────────────────────────────────────────────

    return {
        init: function (svgId, dotNet) {
            this.dispose(svgId);
            _svgId = svgId;
            _dotNet = dotNet;
            _transform = { tx: 0, ty: 0, scale: 1.0 };
            _state = null;

            var svg = getSvg();
            if (!svg) { console.warn('editorCanvas.init: SVG #' + svgId + ' not found'); return; }

            _handlers = {
                mousedown: onMouseDown,
                mousemove: onMouseMove,
                mouseup: onMouseUp,
                click: onClick,
                wheel: onWheel,
                mouseleave: function () {
                    if (_state && _state.type === 'draw') removePreview();
                    if (_state && _state.type === 'drag' && _state.el)
                        _state.el.removeAttribute('transform');
                    if (_state && _state.type === 'pan') svg.style.cursor = cursorForTool(_tool);
                    _state = null;
                },
                contextmenu: function (e) { e.preventDefault(); },
                keydown: onKeyDown,
                keyup: onKeyUp
            };

            svg.addEventListener('mousedown', _handlers.mousedown);
            svg.addEventListener('mousemove', _handlers.mousemove);
            svg.addEventListener('mouseup', _handlers.mouseup);
            svg.addEventListener('click', _handlers.click);
            svg.addEventListener('wheel', _handlers.wheel, { passive: false });
            svg.addEventListener('mouseleave', _handlers.mouseleave);
            svg.addEventListener('contextmenu', _handlers.contextmenu);
            document.addEventListener('keydown', _handlers.keydown);
            document.addEventListener('keyup', _handlers.keyup);

            svg.style.cursor = cursorForTool(_tool);
        },

        setTool: function (tool) {
            _tool = tool;
            removePreview();
            if (_state && _state.type === 'drag' && _state.el)
                _state.el.removeAttribute('transform');
            _state = null;
            var svg = getSvg();
            if (svg) svg.style.cursor = cursorForTool(tool);
        },

        resetView: function () {
            _transform = { tx: 0, ty: 0, scale: 1.0 };
            applyTransform();
        },

        /** Volá se z OnAfterRenderAsync — Blazor nemění transform, ale jen pro jistotu znovu aplikujeme. */
        reapplyTransform: function () { applyTransform(); },

        /** Nastaví hranice patra pro clamping kreslení místností a tahu zařízení. */
        setFloorBounds: function (w, h) {
            _floorBounds.w = w;
            _floorBounds.h = h;
        },

        /** Zapne/vypne přichytávání k mřížce a nastaví velikost buňky. */
        setGrid: function (enabled, size) {
            _gridEnabled = !!enabled;
            _gridSize = size || 20;
        },

        dispose: function (svgId) {
            var id = svgId || _svgId;
            var svg = document.getElementById(id);
            if (svg && _handlers.mousedown) {
                svg.removeEventListener('mousedown', _handlers.mousedown);
                svg.removeEventListener('mousemove', _handlers.mousemove);
                svg.removeEventListener('mouseup', _handlers.mouseup);
                svg.removeEventListener('click', _handlers.click);
                svg.removeEventListener('wheel', _handlers.wheel);
                svg.removeEventListener('mouseleave', _handlers.mouseleave);
                svg.removeEventListener('contextmenu', _handlers.contextmenu);
            }
            if (_handlers.keydown) {
                document.removeEventListener('keydown', _handlers.keydown);
                document.removeEventListener('keyup', _handlers.keyup);
            }
            if (_dotNet) { _dotNet = null; }
            _handlers = {};
            _state = null;
        }
    };
}());

// ── Globální utility ──────────────────────────────────────────────────────────

/**
 * Stáhne textový soubor do prohlížeče.
 * Volá se z Blazoru: JSRuntime.InvokeVoidAsync("downloadTextFile", filename, content)
 */
window.downloadTextFile = function (filename, content) {
    var blob = new Blob([content], { type: 'application/json;charset=utf-8' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

/**
 * Spustí klik na element dle ID (pro skrytý <input type="file">).
 * Volá se z Blazoru: JSRuntime.InvokeVoidAsync("triggerClick", elementId)
 */
window.triggerClick = function (elementId) {
    var el = document.getElementById(elementId);
    if (el) el.click();
};

