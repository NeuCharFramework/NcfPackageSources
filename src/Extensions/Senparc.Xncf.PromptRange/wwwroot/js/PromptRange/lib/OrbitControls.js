/**
 * @license
 * OrbitControls - simplified camera controller
 * Optimized specifically for PromptRange 3D maps
 */

(function() {
    'use strict';
    
    const _THREE = window.THREE;
    
    if (!_THREE) {
        console.error('THREE.js must be loaded before OrbitControls.js');
        return;
    }
    
    // Status enum
    const STATE = {
        NONE: -1,
        ROTATE: 0,
        DOLLY: 1,
        PAN: 2
    };
    
    /**
     * OrbitControls constructor
     * @param {THREE.Camera} camera - camera object
     * @param {HTMLElement} domElement - DOM element
     */
    _THREE.OrbitControls = function(camera, domElement) {
        this.camera = camera;
        this.domElement = domElement || document.body;
        
        // public configuration
        this.enabled = true;
        this.target = new _THREE.Vector3();
        
        this.enableDamping = false;
        this.dampingFactor = 0.05;
        
        this.enableZoom = true;
        this.zoomSpeed = 1.0;
        
        this.enableRotate = true;
        this.rotateSpeed = 1.0;
        
        this.enablePan = true;
        this.panSpeed = 1.0;
        
        this.minDistance = 0;
        this.maxDistance = Infinity;
        
        this.minPolarAngle = 0; // radians
        this.maxPolarAngle = Math.PI; // radians
        
        // internal state
        const scope = this;
        let state = STATE.NONE;
        
        // Spherical coordinate system
        const spherical = new _THREE.Spherical();
        const sphericalDelta = new _THREE.Spherical();
        
        let scale = 1;
        const panOffset = new _THREE.Vector3();
        
        // Mouse status
        const rotateStart = new _THREE.Vector2();
        const rotateEnd = new _THREE.Vector2();
        const rotateDelta = new _THREE.Vector2();
        
        const panStart = new _THREE.Vector2();
        const panEnd = new _THREE.Vector2();
        const panDelta = new _THREE.Vector2();
        
        const dollyStart = new _THREE.Vector2();
        const dollyEnd = new _THREE.Vector2();
        const dollyDelta = new _THREE.Vector2();
        
        // update function
        this.update = (function() {
            const offset = new _THREE.Vector3();
            const quat = new _THREE.Quaternion().setFromUnitVectors(
                camera.up, 
                new _THREE.Vector3(0, 1, 0)
            );
            const quatInverse = quat.clone().invert();
            
            const lastPosition = new _THREE.Vector3();
            const lastQuaternion = new _THREE.Quaternion();
            
            return function update() {
                const position = scope.camera.position;
                
                offset.copy(position).sub(scope.target);
                
                // Rotation offset to "y-axis-is-up" space
                offset.applyQuaternion(quat);
                
                // Convert from Cartesian coordinates to spherical coordinates
                spherical.setFromVector3(offset);
                
                if (scope.enableDamping) {
                    spherical.theta += sphericalDelta.theta * scope.dampingFactor;
                    spherical.phi += sphericalDelta.phi * scope.dampingFactor;
                } else {
                    spherical.theta += sphericalDelta.theta;
                    spherical.phi += sphericalDelta.phi;
                }
                
                // Limit phi to a safe range
                let min = scope.minPolarAngle;
                let max = scope.maxPolarAngle;
                spherical.phi = Math.max(min, Math.min(max, spherical.phi));
                
                spherical.makeSafe();
                
                spherical.radius *= scale;
                
                // Limit the radius to between minDistance and maxDistance
                spherical.radius = Math.max(
                    scope.minDistance, 
                    Math.min(scope.maxDistance, spherical.radius)
                );
                
                // Move target to pan
                if (scope.enableDamping === true) {
                    scope.target.addScaledVector(panOffset, scope.dampingFactor);
                } else {
                    scope.target.add(panOffset);
                }
                
                // Convert from spherical coordinates back to Cartesian coordinates
                offset.setFromSpherical(spherical);
                
                // Rotate offset back to "camera-up-vector-is-up" space
                offset.applyQuaternion(quatInverse);
                
                position.copy(scope.target).add(offset);
                
                scope.camera.lookAt(scope.target);
                
                if (scope.enableDamping === true) {
                    sphericalDelta.theta *= (1 - scope.dampingFactor);
                    sphericalDelta.phi *= (1 - scope.dampingFactor);
                    panOffset.multiplyScalar(1 - scope.dampingFactor);
                } else {
                    sphericalDelta.set(0, 0, 0);
                    panOffset.set(0, 0, 0);
                }
                
                scale = 1;
                
                // Check if updates are needed
                if (lastPosition.distanceToSquared(scope.camera.position) > 0.000001 ||
                    8 * (1 - lastQuaternion.dot(scope.camera.quaternion)) > 0.000001) {
                    lastPosition.copy(scope.camera.position);
                    lastQuaternion.copy(scope.camera.quaternion);
                    return true;
                }
                
                return false;
            };
        })();
        
        // Rotate left (horizontal)
        function rotateLeft(angle) {
            sphericalDelta.theta -= angle;
        }
        
        // Rotate up (vertical)
        function rotateUp(angle) {
            sphericalDelta.phi -= angle;
        }
        
        // Zoom
        function dollyIn(dollyScale) {
            scale /= dollyScale;
        }
        
        function dollyOut(dollyScale) {
            scale *= dollyScale;
        }
        
        // Pan
        const panLeft = (function() {
            const v = new _THREE.Vector3();
            
            return function panLeft(distance, objectMatrix) {
                v.setFromMatrixColumn(objectMatrix, 0); // Get column X
                v.multiplyScalar(-distance);
                panOffset.add(v);
            };
        })();
        
        const panUp = (function() {
            const v = new _THREE.Vector3();
            
            return function panUp(distance, objectMatrix) {
                v.setFromMatrixColumn(objectMatrix, 1); // Get column Y
                v.multiplyScalar(distance);
                panOffset.add(v);
            };
        })();
        
        const pan = (function() {
            const offset = new _THREE.Vector3();
            
            return function pan(deltaX, deltaY) {
                const element = scope.domElement;
                
                if (scope.camera.isPerspectiveCamera) {
                    // perspective camera
                    const position = scope.camera.position;
                    offset.copy(position).sub(scope.target);
                    let targetDistance = offset.length();
                    
                    // According to half of the field of view
                    targetDistance *= Math.tan((scope.camera.fov / 2) * Math.PI / 180.0);
                    
                    // We actually use the screen height to do all calculations
                    // So there is no need to change the direction of translation
                    panLeft(2 * deltaX * targetDistance / element.clientHeight, scope.camera.matrix);
                    panUp(2 * deltaY * targetDistance / element.clientHeight, scope.camera.matrix);
                }
            };
        })();
        
        // Mouse event handling
        function handleMouseDownRotate(event) {
            rotateStart.set(event.clientX, event.clientY);
        }
        
        function handleMouseDownDolly(event) {
            dollyStart.set(event.clientX, event.clientY);
        }
        
        function handleMouseDownPan(event) {
            panStart.set(event.clientX, event.clientY);
        }
        
        function handleMouseMoveRotate(event) {
            rotateEnd.set(event.clientX, event.clientY);
            rotateDelta.subVectors(rotateEnd, rotateStart).multiplyScalar(scope.rotateSpeed);
            
            const element = scope.domElement;
            
            rotateLeft(2 * Math.PI * rotateDelta.x / element.clientHeight); // yes, height
            rotateUp(2 * Math.PI * rotateDelta.y / element.clientHeight);
            
            rotateStart.copy(rotateEnd);
            
            scope.update();
        }
        
        function handleMouseMoveDolly(event) {
            dollyEnd.set(event.clientX, event.clientY);
            dollyDelta.subVectors(dollyEnd, dollyStart);
            
            if (dollyDelta.y > 0) {
                dollyIn(getZoomScale());
            } else if (dollyDelta.y < 0) {
                dollyOut(getZoomScale());
            }
            
            dollyStart.copy(dollyEnd);
            
            scope.update();
        }
        
        function handleMouseMovePan(event) {
            panEnd.set(event.clientX, event.clientY);
            panDelta.subVectors(panEnd, panStart).multiplyScalar(scope.panSpeed);
            
            pan(panDelta.x, panDelta.y);
            
            panStart.copy(panEnd);
            
            scope.update();
        }
        
        function handleMouseWheel(event) {
            if (event.deltaY < 0) {
                dollyOut(getZoomScale());
            } else if (event.deltaY > 0) {
                dollyIn(getZoomScale());
            }
            
            scope.update();
        }
        
        function getZoomScale() {
            return Math.pow(0.95, scope.zoomSpeed);
        }
        
        // event listener
        function onMouseDown(event) {
            if (scope.enabled === false) return;
            
            event.preventDefault();
            
            switch (event.button) {
                case 0: // Left click - Rotate
                    if (scope.enableRotate === false) return;
                    handleMouseDownRotate(event);
                    state = STATE.ROTATE;
                    break;
                    
                case 1: // Middle click - zoom
                    if (scope.enableZoom === false) return;
                    handleMouseDownDolly(event);
                    state = STATE.DOLLY;
                    break;
                    
                case 2: // Right click - pan
                    if (scope.enablePan === false) return;
                    handleMouseDownPan(event);
                    state = STATE.PAN;
                    break;
            }
            
            if (state !== STATE.NONE) {
                document.addEventListener('mousemove', onMouseMove, false);
                document.addEventListener('mouseup', onMouseUp, false);
            }
        }
        
        function onMouseMove(event) {
            if (scope.enabled === false) return;
            
            event.preventDefault();
            
            switch (state) {
                case STATE.ROTATE:
                    if (scope.enableRotate === false) return;
                    handleMouseMoveRotate(event);
                    break;
                    
                case STATE.DOLLY:
                    if (scope.enableZoom === false) return;
                    handleMouseMoveDolly(event);
                    break;
                    
                case STATE.PAN:
                    if (scope.enablePan === false) return;
                    handleMouseMovePan(event);
                    break;
            }
        }
        
        function onMouseUp() {
            if (scope.enabled === false) return;
            
            document.removeEventListener('mousemove', onMouseMove, false);
            document.removeEventListener('mouseup', onMouseUp, false);
            
            state = STATE.NONE;
        }
        
        function onMouseWheel(event) {
            if (scope.enabled === false || scope.enableZoom === false || 
                (state !== STATE.NONE && state !== STATE.ROTATE)) return;
            
            event.preventDefault();
            event.stopPropagation();
            
            handleMouseWheel(event);
        }
        
        function onContextMenu(event) {
            if (scope.enabled === false) return;
            event.preventDefault();
        }
        
        // Binding events
        this.domElement.addEventListener('contextmenu', onContextMenu, false);
        this.domElement.addEventListener('mousedown', onMouseDown, false);
        this.domElement.addEventListener('wheel', onMouseWheel, { passive: false });
        
        // Destruction method
        this.dispose = function() {
            scope.domElement.removeEventListener('contextmenu', onContextMenu, false);
            scope.domElement.removeEventListener('mousedown', onMouseDown, false);
            scope.domElement.removeEventListener('wheel', onMouseWheel, false);
            
            document.removeEventListener('mousemove', onMouseMove, false);
            document.removeEventListener('mouseup', onMouseUp, false);
        };
        
        // initialization
        this.update();
    };
    
    // Set up prototype
    _THREE.OrbitControls.prototype = Object.create(_THREE.EventDispatcher.prototype);
    _THREE.OrbitControls.prototype.constructor = _THREE.OrbitControls;
    
})();
