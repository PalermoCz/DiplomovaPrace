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
    var _facilityNodeDragEnabled = false;
    var _selectedNodeKeys = new Set();      // Blazor-driven selected node keys (group drag detection)
    var _suppressNextClick = false;         // Prevents onClick from clearing selection after rectSelect
    var _pendingRectSelectionKeys = null;
    var _pendingRectSelectionUntil = 0; // timestamp guard for first drag after rect-select
    var _pendingRectSelectionElements = null; // exact selected node elements from last rect-select
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

    function getActiveNodeElements() {
    var content = getContent();
    if (!content) return [];

    var els = Array.from(
        content.querySelectorAll('[data-node-key][data-node-x][data-node-y]')
    );

    console.debug('[active-node-elements]', {
        count: els.length,
        keys: els.map(function (el) { return el.dataset.nodeKey; })
    });

    return els;
    }


    function applyLocalRectSelection(x1, y1, x2, y2, additive) {
    var nextSelectedKeys = additive ? new Set(_selectedNodeKeys) : new Set();
    var nextSelectedElements = [];

    getActiveNodeElements().forEach(function (el) {
        var nodeKey = el.dataset.nodeKey;
        var nodeX = parseFloat(el.dataset.nodeX);
        var nodeY = parseFloat(el.dataset.nodeY);

        if (!nodeKey || Number.isNaN(nodeX) || Number.isNaN(nodeY)) return;

        if (nodeX >= x1 && nodeX <= x2 && nodeY >= y1 && nodeY <= y2) {
            nextSelectedKeys.add(nodeKey);
            nextSelectedElements.push({
                el: el,
                nodeKey: nodeKey,
                origX: nodeX,
                origY: nodeY
            });
        }
    });

    _selectedNodeKeys = nextSelectedKeys;
    _pendingRectSelectionKeys = new Set(nextSelectedKeys);
    _pendingRectSelectionElements = nextSelectedElements;
    _pendingRectSelectionUntil = Date.now() + 800;

    console.debug('[rect-select-local]', {
        selected: Array.from(_selectedNodeKeys || []),
        pending: _pendingRectSelectionKeys ? Array.from(_pendingRectSelectionKeys) : [],
        elementCount: _pendingRectSelectionElements ? _pendingRectSelectionElements.length : 0
    });

    return nextSelectedKeys;
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

            if (_facilityNodeDragEnabled) {
                var nodeEl = findWithData(e.target, 'nodeKey');
                if (nodeEl) {
                    e.stopPropagation();
                    var clickedKey = nodeEl.dataset.nodeKey;
                    var origNodeX = parseFloat(nodeEl.dataset.nodeX);
                    var origNodeY = parseFloat(nodeEl.dataset.nodeY);

                    if (Number.isNaN(origNodeX) || Number.isNaN(origNodeY)) {
                        origNodeX = pos.x;
                        origNodeY = pos.y;
                    }

                    // Group drag: clicked node is in selected set AND 2+ nodes selected
                    
                    
                    var effectiveSelectedKeys =
                        (_pendingRectSelectionKeys &&
                        _pendingRectSelectionKeys.size > 1 &&
                        Date.now() < _pendingRectSelectionUntil)
                            ? _pendingRectSelectionKeys
                            : _selectedNodeKeys;


                    
                    console.debug('[group-drag-check]', {
                        clickedKey: clickedKey,
                        selectedSize: _selectedNodeKeys ? _selectedNodeKeys.size : 0,
                        selectedHasClicked: _selectedNodeKeys ? _selectedNodeKeys.has(clickedKey) : false,
                        pendingSize: _pendingRectSelectionKeys ? _pendingRectSelectionKeys.size : 0,
                        pendingHasClicked: _pendingRectSelectionKeys ? _pendingRectSelectionKeys.has(clickedKey) : false,
                        effectiveSize: effectiveSelectedKeys ? effectiveSelectedKeys.size : 0,
                        effectiveHasClicked: effectiveSelectedKeys ? effectiveSelectedKeys.has(clickedKey) : false
                    });

                    if (effectiveSelectedKeys && effectiveSelectedKeys.size > 1 && effectiveSelectedKeys.has(clickedKey)) {
    var groupEls = [];

    // First drag immediately after rect-select: use the exact locally captured elements.
    if (
        _pendingRectSelectionElements &&
        _pendingRectSelectionElements.length > 1 &&
        _pendingRectSelectionKeys &&
        _pendingRectSelectionKeys.has(clickedKey) &&
        Date.now() < _pendingRectSelectionUntil
    ) {
        groupEls = _pendingRectSelectionElements.map(function (item) {
            return {
                el: item.el,
                nodeKey: item.nodeKey,
                origX: item.origX,
                origY: item.origY
            };
        });
    } else {
        getActiveNodeElements().forEach(function (el) {
            if (effectiveSelectedKeys.has(el.dataset.nodeKey)) {
                var nx = parseFloat(el.dataset.nodeX);
                var ny = parseFloat(el.dataset.nodeY);
                if (!Number.isNaN(nx) && !Number.isNaN(ny)) {
                    groupEls.push({
                        el: el,
                        nodeKey: el.dataset.nodeKey,
                        origX: nx,
                        origY: ny
                    });
                }
            }
        });
    }

    console.debug('[group-drag-branch]', {
        groupCount: groupEls.length,
        groupKeys: groupEls.map(function (x) { return x.nodeKey; }),
        usedPendingElements:
            !!(_pendingRectSelectionElements &&
               _pendingRectSelectionElements.length > 1 &&
               _pendingRectSelectionKeys &&
               _pendingRectSelectionKeys.has(clickedKey) &&
               Date.now() < _pendingRectSelectionUntil)
    });

    _state = {
        type: 'groupDrag',
        anchorKey: clickedKey,
        startCX: pos.x,
        startCY: pos.y,
        groupEls: groupEls,
        moved: false
    };
        } else {
            console.debug('[single-drag-branch]', {
                clickedKey: clickedKey
            });

            _state = {
                type: 'dragFacilityNode',
                nodeKey: clickedKey,
                startCX: pos.x,
                startCY: pos.y,
                origX: origNodeX,
                origY: origNodeY,
                el: nodeEl,
                moved: false
            };
        }


                    return;
                }
                // No node hit → start rectangle selection
                {
                    e.preventDefault();
                    var selG = getContent();
                    var selR = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
                    selR.setAttribute('id', _svgId + '-sel-rect');
                    selR.setAttribute('x', pos.x); selR.setAttribute('y', pos.y);
                    selR.setAttribute('width', '0'); selR.setAttribute('height', '0');
                    selR.setAttribute('fill', 'rgba(37,99,235,0.08)');
                    selR.setAttribute('stroke', '#2563eb');
                    selR.setAttribute('stroke-width', String(1.5 / _transform.scale));
                    selR.setAttribute('stroke-dasharray', String(4 / _transform.scale) + ',' + String(2 / _transform.scale));
                    selR.setAttribute('pointer-events', 'none');
                    if (selG) selG.appendChild(selR);
                    _state = { type: 'rectSelect', startX: pos.x, startY: pos.y, currX: pos.x, currY: pos.y, ctrlKey: (e.ctrlKey || e.metaKey) };
                    return;
                }
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
            return;
        }

        if (_state.type === 'dragFacilityNode') {
            var ndx = pos.x - _state.startCX;
            var ndy = pos.y - _state.startCY;
            if (Math.abs(ndx) > 2 || Math.abs(ndy) > 2) _state.moved = true;
            if (_state.moved) {
                _state.el.setAttribute('transform', 'translate(' + ndx + ' ' + ndy + ')');
            }
            return;
        }

        if (_state.type === 'groupDrag') {
            var gdx = pos.x - _state.startCX;
            var gdy = pos.y - _state.startCY;
            if (Math.abs(gdx) > 2 || Math.abs(gdy) > 2) _state.moved = true;
            if (_state.moved) {
                _state.groupEls.forEach(function (item) {
                    item.el.setAttribute('transform', 'translate(' + gdx + ' ' + gdy + ')');
                });
            }
            return;
        }

        if (_state.type === 'rectSelect') {
            _state.currX = pos.x; _state.currY = pos.y;
            var selRectEl = document.getElementById(_svgId + '-sel-rect');
            if (selRectEl) {
                var rx = Math.min(_state.startX, pos.x);
                var ry = Math.min(_state.startY, pos.y);
                var rw = Math.abs(pos.x - _state.startX);
                var rh = Math.abs(pos.y - _state.startY);
                selRectEl.setAttribute('x', rx); selRectEl.setAttribute('y', ry);
                selRectEl.setAttribute('width', rw); selRectEl.setAttribute('height', rh);
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
                _suppressNextClick = true;
            }
            return;
        }

        if (s.type === 'dragFacilityNode') {
            s.el.removeAttribute('transform');
            if (s.moved) {
                var nodePos = clientToContent(e.clientX, e.clientY);
                var rawX = s.origX + (nodePos.x - s.startCX);
                var rawY = s.origY + (nodePos.y - s.startCY);
                var finalNodeX = snapToGrid(rawX);
                var finalNodeY = snapToGrid(rawY);
                if (_dotNet) _dotNet.invokeMethodAsync('JsOnFacilityNodeDragEnd', s.nodeKey, finalNodeX, finalNodeY);
                _suppressNextClick = true;
            }
            return;
        }

        
        if (s.type === 'groupDrag') {
    s.groupEls.forEach(function (item) {
        item.el.removeAttribute('transform');
    });

    if (s.moved) {
        var gPos = clientToContent(e.clientX, e.clientY);
        var rawDx = gPos.x - s.startCX;
        var rawDy = gPos.y - s.startCY;

        var anchorEl =
            s.groupEls.find(function (g) { return g.nodeKey === s.anchorKey; }) ||
            s.groupEls[0];

        if (!anchorEl) {
            _suppressNextClick = true;
            return;
        }

        var snappedAnchorX = snapToGrid(anchorEl.origX + rawDx);
        var snappedAnchorY = snapToGrid(anchorEl.origY + rawDy);
        var snappedDx = snappedAnchorX - anchorEl.origX;
        var snappedDy = snappedAnchorY - anchorEl.origY;

        if (_dotNet) {
            _dotNet.invokeMethodAsync('JsOnFacilityGroupDragEnd', snappedDx, snappedDy);
        }

        // Keep the moved group selected for the next immediate drag.
        // Blazor currently sends an empty sync right after drop, so we must
        // preserve the local multi-selection for a short time.
        var movedKeys = new Set(
            s.groupEls.map(function (item) { return item.nodeKey; })
        );

        _selectedNodeKeys = movedKeys;
        _pendingRectSelectionKeys = new Set(movedKeys);
        _pendingRectSelectionElements = null; // next drag can rebuild from current DOM
        _pendingRectSelectionUntil = Date.now() + 1500;
    }

    _suppressNextClick = true;
    return;
    }


        
        if (s.type === 'rectSelect') {
            var selRectEl2 = document.getElementById(_svgId + '-sel-rect');
            if (selRectEl2) selRectEl2.remove();

            var x1 = Math.min(s.startX, s.currX);
            var y1 = Math.min(s.startY, s.currY);
            var x2 = Math.max(s.startX, s.currX);
            var y2 = Math.max(s.startY, s.currY);

            if ((x2 - x1) > 4 && (y2 - y1) > 4) {
                _suppressNextClick = true;

                // Local JS update first, before the Blazor roundtrip finishes.
                applyLocalRectSelection(x1, y1, x2, y2, s.ctrlKey);
                
                console.debug('[rect-select-local]', {
                    selected: Array.from(_selectedNodeKeys || []),
                    pending: _pendingRectSelectionKeys ? Array.from(_pendingRectSelectionKeys) : []
                });


                if (_dotNet) {
                    _dotNet.invokeMethodAsync('JsOnRectSelectEnd', x1, y1, x2, y2, s.ctrlKey);
                }
            }
        return;
    }

    }

    function onClick(e) {
        // Zpracuje kliknutí, která nejsou součástí kreslení/tahu
        // (draw/drag se uzavírají v onMouseUp)
        if (_suppressNextClick) { _suppressNextClick = false; return; }
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
                if ((e.ctrlKey || e.metaKey) && _facilityNodeDragEnabled) {
                    // Ctrl+click: additive selection toggle in edit mode
                    if (_dotNet) _dotNet.invokeMethodAsync('JsOnFacilityNodeCtrlClicked', nodeEl.dataset.nodeKey);
                } else {
                    if (_dotNet) _dotNet.invokeMethodAsync('JsOnElementClicked', nodeEl.dataset.nodeKey, 'FacilityNode');
                }
            } else {                
                _selectedNodeKeys = new Set();
                _pendingRectSelectionKeys = null;
                _pendingRectSelectionElements = null;
                _pendingRectSelectionUntil = 0;
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
        // Esc: clear selection when in edit mode (drag enabled)
        if (e.code === 'Escape' && !e.repeat && !e.target.matches('input, textarea') && _facilityNodeDragEnabled) {
            if (_dotNet) _dotNet.invokeMethodAsync('JsOnKeyboardShortcut', 'escape');
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
            
        console.debug('[editor-init]', {
            svgId: svgId,
            sameInstance: _svgId === svgId,
            selectedSize: _selectedNodeKeys ? _selectedNodeKeys.size : 0,
            pendingSize: _pendingRectSelectionKeys ? _pendingRectSelectionKeys.size : 0
        });

        // If the same schematic instance is being re-rendered, do NOT reset
        // selected-node state / pending rect-selection state / transform.
        if (_svgId === svgId) {
            _dotNet = dotNet;

            var svg = getSvg();
            if (svg) {
                svg.style.cursor = cursorForTool(_tool);
            }

            applyTransform();
            return;
        }

        // True instance change: dispose old handlers/state first
        if (_svgId) {
            this.dispose();
        }

        _svgId = svgId;
        _dotNet = dotNet;
        _transform = { tx: 0, ty: 0, scale: 1.0 };
        _state = null;
        _spaceDown = false;
        _selectedNodeKeys = new Set();
        _pendingRectSelectionKeys = null;

        var svg = getSvg();
        if (!svg) return;

        var content = getContent();
        if (!content) return;

        _handlers = {
            mousedown: onMouseDown,
            mousemove: onMouseMove,
            mouseup: onMouseUp,
            click: onClick,
            wheel: onWheel,
            keydown: onKeyDown,
            keyup: onKeyUp
        };

        svg.addEventListener('mousedown', _handlers.mousedown);
        window.addEventListener('mousemove', _handlers.mousemove);
        window.addEventListener('mouseup', _handlers.mouseup);
        svg.addEventListener('click', _handlers.click);
        svg.addEventListener('wheel', _handlers.wheel, { passive: false });
        window.addEventListener('keydown', _handlers.keydown);
        window.addEventListener('keyup', _handlers.keyup);

        svg.style.cursor = cursorForTool(_tool);
        applyTransform();
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

        /**
         * Fit-to-view: compute initial transform so the content group's bounding box
         * is centered and scaled to fill the SVG viewport with some padding.
         *
         * For SVGs without a viewBox (screen-pixel coordinate space):
         *   - uses getBoundingClientRect() for actual container dimensions
         *   - for wide tree layouts (aspect ratio > 2:1): fills width, top-aligns
         *   - for square/portrait layouts: fits entirely with center alignment
         *
         * For SVGs with a viewBox (legacy floor editor):
         *   - uses viewBox dimensions as target space (backward compatible)
         */
        fitToView: function (mode) {
            mode = mode || 'default';
            var svg = getSvg();
            var g = getContent();
            if (!svg || !g) return;
            var bbox = g.getBBox();
            if (bbox.width < 1 || bbox.height < 1) return;
            var vb = svg.viewBox.baseVal;
            var hasViewBox = vb && vb.width > 1;
            var vw, vh;
            if (hasViewBox) {
                vw = vb.width;
                vh = vb.height;
            } else {
                var rect = svg.getBoundingClientRect();
                vw = rect.width > 10 ? rect.width : 800;
                vh = rect.height > 10 ? rect.height : 600;
            }
            var pad = 28;
            var scaleX = (vw - pad * 2) / bbox.width;
            var scaleY = (vh - pad * 2) / bbox.height;
            var scale;
            var contentAspect = bbox.width / bbox.height;

            if (mode === 'focus') {
                // Focus mode: fit entire tree centered with generous padding
                var focusPad = 36;
                var focusScaleX = (vw - focusPad * 2) / bbox.width;
                var focusScaleY = (vh - focusPad * 2) / bbox.height;
                scale = Math.min(focusScaleX, focusScaleY);
                pad = focusPad;
            } else if (mode === 'edit') {
                // Edit mode: slightly more padding, fit-center
                var editPad = 40;
                var editScaleX = (vw - editPad * 2) / bbox.width;
                var editScaleY = (vh - editPad * 2) / bbox.height;
                scale = Math.min(editScaleX, editScaleY);
                pad = editPad;
            } else {
                // Default dashboard: wide trees fill width (top-align), others fit-center
                if (!hasViewBox && contentAspect > 2.0) {
                    scale = scaleX;
                } else {
                    scale = Math.min(scaleX, scaleY);
                }
            }

            if (scale > 2.5) scale = 2.5;
            var tx = pad + (vw - pad * 2 - bbox.width * scale) / 2 - bbox.x * scale;
            var ty;
            if (mode === 'default' && !hasViewBox && contentAspect > 2.0) {
                // Top-align for wide tree: show root nodes at top
                ty = pad - bbox.y * scale;
            } else {
                ty = pad + (vh - pad * 2 - bbox.height * scale) / 2 - bbox.y * scale;
            }
            _transform = { tx: tx, ty: ty, scale: scale };
            applyTransform();
        },

        /**
         * goHome — root-centric view: fit tree to width with top-align bias,
         * then apply a zoom-in factor so root nodes are clearly visible.
         * Called on initial render and by the Home button.
         */
        goHome: function () {
            var svg = getSvg();
            var g = getContent();
            if (!svg || !g) return;
            var bbox = g.getBBox();
            if (bbox.width < 1 || bbox.height < 1) return;
            var vb = svg.viewBox.baseVal;
            var hasViewBox = vb && vb.width > 1;
            var vw, vh;
            if (hasViewBox) {
                vw = vb.width; vh = vb.height;
            } else {
                var rect = svg.getBoundingClientRect();
                vw = rect.width > 10 ? rect.width : 800;
                vh = rect.height > 10 ? rect.height : 600;
            }
            var pad = 20;
            // Fit to width first
            var scaleX = (vw - pad * 2) / bbox.width;
            // Apply a zoom factor so root area is clearly visible; cap at 1.8 for legibility
            var scale = Math.min(scaleX * 1.65, 1.8);
            if (scale < 0.35) scale = 0.35;
            // Center horizontally
            var tx = pad + (vw - pad * 2 - bbox.width * scale) / 2 - bbox.x * scale;
            // Top-align so root node shows at top
            var ty = pad - bbox.y * scale;
            _transform = { tx: tx, ty: ty, scale: scale };
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

        setFacilityNodeDragEnabled: function (enabled) {
            _facilityNodeDragEnabled = !!enabled;
            if (!enabled) _selectedNodeKeys = new Set();
        },

        setSelectedNodes: function (keys) {
    var next = new Set(Array.isArray(keys) ? keys : []);

    // Ignore transient empty sync immediately after local rect-select.
    if (
        next.size === 0 &&
        _pendingRectSelectionKeys &&
        _pendingRectSelectionKeys.size > 1 &&
        Date.now() < _pendingRectSelectionUntil
    ) {
        console.debug('[setSelectedNodes] ignoring transient empty sync', {
            incomingSize: next.size,
            pendingSize: _pendingRectSelectionKeys.size,
            msLeft: _pendingRectSelectionUntil - Date.now()
        });
        return;
    }

    _selectedNodeKeys = next;

    if (next.size > 1) {
        _pendingRectSelectionKeys = new Set(next);
    } else {
        _pendingRectSelectionKeys = null;
        _pendingRectSelectionElements = null;
        _pendingRectSelectionUntil = 0;
    }

    console.debug('[setSelectedNodes]', {
        size: next.size,
        keys: Array.from(next)
    });
},


		/** Pans the viewport so that the given canvas point (x, y) is centered in the current view. */
        panToNode: function (x, y, scale) {
            var svg = getSvg();
            if (!svg) return;
            var rect = svg.getBoundingClientRect();
            var vw = rect.width > 10 ? rect.width : 800;
            var vh = rect.height > 10 ? rect.height : 600;
            var s = (scale > 0) ? scale : 1.4;
            _transform.tx = vw / 2 - x * s;
            _transform.ty = vh / 2 - y * s;
            _transform.scale = s;
            applyTransform();
        },

        
        dispose: function () {
            var svg = getSvg();

            if (svg && _handlers.mousedown) svg.removeEventListener('mousedown', _handlers.mousedown);
            if (svg && _handlers.click) svg.removeEventListener('click', _handlers.click);
            if (svg && _handlers.wheel) svg.removeEventListener('wheel', _handlers.wheel);

            if (_handlers.mousemove) window.removeEventListener('mousemove', _handlers.mousemove);
            if (_handlers.mouseup) window.removeEventListener('mouseup', _handlers.mouseup);
            if (_handlers.keydown) window.removeEventListener('keydown', _handlers.keydown);
            if (_handlers.keyup) window.removeEventListener('keyup', _handlers.keyup);

            _handlers = {};
            _state = null;
            _svgId = null;
            _dotNet = null;
            _selectedNodeKeys = new Set();
            _pendingRectSelectionKeys = null;
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

