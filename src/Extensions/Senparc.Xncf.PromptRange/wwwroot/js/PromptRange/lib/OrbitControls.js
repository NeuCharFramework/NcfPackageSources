/**
 * @license
 * OrbitControls - 简化版相机控制器
 * 专为 PromptRange 3D 地图优化
 */

(function() {
    'use strict';
    
    const _THREE = window.THREE;
    
    if (!_THREE) {
        console.error('THREE.js must be loaded before OrbitControls.js');
        return;
    }
    
    // 状态枚举
    const STATE = {
        NONE: -1,
        ROTATE: 0,
        DOLLY: 1,
        PAN: 2
    };
    
    /**
     * OrbitControls 构造函数
     * @param {THREE.Camera} camera - 相机对象
     * @param {HTMLElement} domElement - DOM 元素
     */
    _THREE.OrbitControls = function(camera, domElement) {
        this.camera = camera;
        this.domElement = domElement || document.body;
        
        // 公共配置
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
        
        // 内部状态
        const scope = this;
        let state = STATE.NONE;
        
        // 球坐标系
        const spherical = new _THREE.Spherical();
        const sphericalDelta = new _THREE.Spherical();
        
        let scale = 1;
        const panOffset = new _THREE.Vector3();
        
        // 鼠标状态
        const rotateStart = new _THREE.Vector2();
        const rotateEnd = new _THREE.Vector2();
        const rotateDelta = new _THREE.Vector2();
        
        const panStart = new _THREE.Vector2();
        const panEnd = new _THREE.Vector2();
        const panDelta = new _THREE.Vector2();
        
        const dollyStart = new _THREE.Vector2();
        const dollyEnd = new _THREE.Vector2();
        const dollyDelta = new _THREE.Vector2();
        
        // 更新函数
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
                
                // 旋转偏移到 "y-axis-is-up" 空间
                offset.applyQuaternion(quat);
                
                // 从笛卡尔坐标转换到球坐标
                spherical.setFromVector3(offset);
                
                if (scope.enableDamping) {
                    spherical.theta += sphericalDelta.theta * scope.dampingFactor;
                    spherical.phi += sphericalDelta.phi * scope.dampingFactor;
                } else {
                    spherical.theta += sphericalDelta.theta;
                    spherical.phi += sphericalDelta.phi;
                }
                
                // 限制 phi 在安全范围内
                let min = scope.minPolarAngle;
                let max = scope.maxPolarAngle;
                spherical.phi = Math.max(min, Math.min(max, spherical.phi));
                
                spherical.makeSafe();
                
                spherical.radius *= scale;
                
                // 限制半径在 minDistance 和 maxDistance 之间
                spherical.radius = Math.max(
                    scope.minDistance, 
                    Math.min(scope.maxDistance, spherical.radius)
                );
                
                // 移动目标以平移
                if (scope.enableDamping === true) {
                    scope.target.addScaledVector(panOffset, scope.dampingFactor);
                } else {
                    scope.target.add(panOffset);
                }
                
                // 从球坐标转换回笛卡尔坐标
                offset.setFromSpherical(spherical);
                
                // 旋转偏移回 "camera-up-vector-is-up" 空间
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
                
                // 检查是否需要更新
                if (lastPosition.distanceToSquared(scope.camera.position) > 0.000001 ||
                    8 * (1 - lastQuaternion.dot(scope.camera.quaternion)) > 0.000001) {
                    lastPosition.copy(scope.camera.position);
                    lastQuaternion.copy(scope.camera.quaternion);
                    return true;
                }
                
                return false;
            };
        })();
        
        // 旋转左（水平）
        function rotateLeft(angle) {
            sphericalDelta.theta -= angle;
        }
        
        // 旋转上（垂直）
        function rotateUp(angle) {
            sphericalDelta.phi -= angle;
        }
        
        // 缩放
        function dollyIn(dollyScale) {
            scale /= dollyScale;
        }
        
        function dollyOut(dollyScale) {
            scale *= dollyScale;
        }
        
        // 平移
        const panLeft = (function() {
            const v = new _THREE.Vector3();
            
            return function panLeft(distance, objectMatrix) {
                v.setFromMatrixColumn(objectMatrix, 0); // 获取 X 列
                v.multiplyScalar(-distance);
                panOffset.add(v);
            };
        })();
        
        const panUp = (function() {
            const v = new _THREE.Vector3();
            
            return function panUp(distance, objectMatrix) {
                v.setFromMatrixColumn(objectMatrix, 1); // 获取 Y 列
                v.multiplyScalar(distance);
                panOffset.add(v);
            };
        })();
        
        const pan = (function() {
            const offset = new _THREE.Vector3();
            
            return function pan(deltaX, deltaY) {
                const element = scope.domElement;
                
                if (scope.camera.isPerspectiveCamera) {
                    // 透视相机
                    const position = scope.camera.position;
                    offset.copy(position).sub(scope.target);
                    let targetDistance = offset.length();
                    
                    // 根据视场角的一半
                    targetDistance *= Math.tan((scope.camera.fov / 2) * Math.PI / 180.0);
                    
                    // 我们实际使用屏幕高度来做所有计算
                    // 所以不需要改变平移的方向
                    panLeft(2 * deltaX * targetDistance / element.clientHeight, scope.camera.matrix);
                    panUp(2 * deltaY * targetDistance / element.clientHeight, scope.camera.matrix);
                }
            };
        })();
        
        // 鼠标事件处理
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
        
        // 事件监听器
        function onMouseDown(event) {
            if (scope.enabled === false) return;
            
            event.preventDefault();
            
            switch (event.button) {
                case 0: // 左键 - 旋转
                    if (scope.enableRotate === false) return;
                    handleMouseDownRotate(event);
                    state = STATE.ROTATE;
                    break;
                    
                case 1: // 中键 - 缩放
                    if (scope.enableZoom === false) return;
                    handleMouseDownDolly(event);
                    state = STATE.DOLLY;
                    break;
                    
                case 2: // 右键 - 平移
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
        
        // 绑定事件
        this.domElement.addEventListener('contextmenu', onContextMenu, false);
        this.domElement.addEventListener('mousedown', onMouseDown, false);
        this.domElement.addEventListener('wheel', onMouseWheel, { passive: false });
        
        // 销毁方法
        this.dispose = function() {
            scope.domElement.removeEventListener('contextmenu', onContextMenu, false);
            scope.domElement.removeEventListener('mousedown', onMouseDown, false);
            scope.domElement.removeEventListener('wheel', onMouseWheel, false);
            
            document.removeEventListener('mousemove', onMouseMove, false);
            document.removeEventListener('mouseup', onMouseUp, false);
        };
        
        // 初始化
        this.update();
    };
    
    // 设置原型
    _THREE.OrbitControls.prototype = Object.create(_THREE.EventDispatcher.prototype);
    _THREE.OrbitControls.prototype.constructor = _THREE.OrbitControls;
    
})();
