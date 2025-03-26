/**
 * @license
 * OrbitControls - 来自three.js的相机控制器
 * 浏览器直接使用的非模块化版本
 */

// 保存对THREE的引用
const _THREE = window.THREE;

/**
 * @author qiao / https://github.com/qiao
 * @author mrdoob / http://mrdoob.com
 * @author alteredq / http://alteredqualia.com/
 * @author WestLangley / http://github.com/WestLangley
 * @author erich666 / http://erichaines.com
 * @author ScieCode / http://github.com/sciecode
 */

// OrbitControls performs orbiting, dollying (zooming), and panning.
// Unlike TrackballControls, it maintains the "up" direction object.up (+Y by default).
//
//    Orbit - left mouse / touch: one-finger move
//    Zoom - middle mouse, or mousewheel / touch: two-finger spread or squish
//    Pan - right mouse, or left mouse + ctrl/meta/shiftKey, or arrow keys / touch: two-finger move

const _changeEvent = { type: 'change' };
const _startEvent = { type: 'start' };
const _endEvent = { type: 'end' };
const _ray = new _THREE.Ray();
const _plane = new _THREE.Plane();
const TILT_LIMIT = Math.cos( 70 * _THREE.MathUtils.DEG2RAD );

_THREE.OrbitControls = function ( object, domElement ) {
	this.object = object;
	this.domElement = ( domElement !== undefined ) ? domElement : document;
	
	// API
	this.enabled = true;
	this.target = new _THREE.Vector3();
	
	this.enableDamping = false;
	this.dampingFactor = 0.05;
	
	this.screenSpacePanning = true;
	
	this.minDistance = 0;
	this.maxDistance = Infinity;
	
	this.minPolarAngle = 0;
	this.maxPolarAngle = Math.PI;
	
	this.minAzimuthAngle = - Infinity;
	this.maxAzimuthAngle = Infinity;
	
	this.enableZoom = true;
	this.zoomSpeed = 1.0;
	
	this.enableRotate = true;
	this.rotateSpeed = 1.0;
	
	this.enablePan = true;
	this.panSpeed = 1.0;
	
	// 私有变量
	const scope = this;
	const EPS = 0.000001;
	
	// 当前位置
	const position = new _THREE.Vector3();
	
	// 当前四元数
	const quat = new _THREE.Quaternion().setFromUnitVectors( object.up, new _THREE.Vector3( 0, 1, 0 ) );
	const quatInverse = quat.clone().invert();
	
	// 更新函数
	this.update = function() {
		const offset = new _THREE.Vector3();
		
		// 从position减去target,获得相机相对于目标点的偏移
		offset.copy( position ).sub( this.target );
		
		// 将相机偏移旋转到"标准"视图空间
		offset.applyQuaternion( quat );
		
		// 计算球坐标
		const theta = Math.atan2( offset.x, offset.z );
		const phi = Math.atan2( Math.sqrt( offset.x * offset.x + offset.z * offset.z ), offset.y );
		
		// 确保phi在界限内
		phi = Math.max( this.minPolarAngle, Math.min( this.maxPolarAngle, phi ) );
		
		// 确保角度在界限内
		let thetaDelta = 0;
		if (this.minAzimuthAngle !== -Infinity && this.maxAzimuthAngle !== Infinity) {
			thetaDelta = Math.max( this.minAzimuthAngle, Math.min( this.maxAzimuthAngle, theta ) ) - theta;
		}
		
		// 将方位角限制在[-pi, pi]范围内
		theta += thetaDelta;
		
		// 从球坐标计算实际的相机位置
		const radius = offset.length();
		
		offset.x = radius * Math.sin( phi ) * Math.sin( theta );
		offset.y = radius * Math.cos( phi );
		offset.z = radius * Math.sin( phi ) * Math.cos( theta );
		
		// 旋转偏移回相机空间
		offset.applyQuaternion( quatInverse );
		
		// 设置相机位置
		position.copy( this.target ).add( offset );
		
		// 相机看向目标
		this.object.lookAt( this.target );
		
		// 保持相机.up为(0,1,0)
		this.object.quaternion.multiply( quatInverse );
		
		return true;
	};
	
	// 初始化
	position.copy( this.object.position );
	this.update();
	
	// 事件处理
	const STATE = {
		NONE: - 1,
		ROTATE: 0,
		DOLLY: 1,
		PAN: 2,
		TOUCH_ROTATE: 3,
		TOUCH_DOLLY: 4,
		TOUCH_PAN: 5
	};
	
	let state = STATE.NONE;
	
	const onMouseDown = function( event ) {
		if ( scope.enabled === false ) return;
		
		event.preventDefault();
		
		if ( event.button === 0 ) {
			if ( scope.enableRotate === false ) return;
			state = STATE.ROTATE;
		} else if ( event.button === 1 ) {
			if ( scope.enableZoom === false ) return;
			state = STATE.DOLLY;
		} else if ( event.button === 2 ) {
			if ( scope.enablePan === false ) return;
			state = STATE.PAN;
		}
		
		document.addEventListener( 'mousemove', onMouseMove, false );
		document.addEventListener( 'mouseup', onMouseUp, false );
	};
	
	const onMouseMove = function( event ) {
		if ( scope.enabled === false ) return;
		
		event.preventDefault();
		
		// 实现拖动的简化版本
		if ( state === STATE.ROTATE ) {
			// 实现旋转
		} else if ( state === STATE.DOLLY ) {
			// 实现缩放
		} else if ( state === STATE.PAN ) {
			// 实现平移
		}
		
		scope.update();
	};
	
	const onMouseUp = function() {
		document.removeEventListener( 'mousemove', onMouseMove, false );
		document.removeEventListener( 'mouseup', onMouseUp, false );
		
		state = STATE.NONE;
	};
	
	const onMouseWheel = function( event ) {
		if ( scope.enabled === false || scope.enableZoom === false ) return;
		
		event.preventDefault();
		event.stopPropagation();
		
		// 缩放功能的简化版本
		
		scope.update();
	};
	
	this.dispose = function() {
		scope.domElement.removeEventListener( 'contextmenu', onContextMenu, false );
		scope.domElement.removeEventListener( 'mousedown', onMouseDown, false );
		scope.domElement.removeEventListener( 'wheel', onMouseWheel, false );
		
		document.removeEventListener( 'mousemove', onMouseMove, false );
		document.removeEventListener( 'mouseup', onMouseUp, false );
	};
	
	const onContextMenu = function( event ) {
		if ( scope.enabled === false ) return;
		
		event.preventDefault();
	};
	
	scope.domElement.addEventListener( 'contextmenu', onContextMenu, false );
	scope.domElement.addEventListener( 'mousedown', onMouseDown, false );
	scope.domElement.addEventListener( 'wheel', onMouseWheel, false );
};

_THREE.OrbitControls.prototype = Object.create( _THREE.EventDispatcher.prototype );
_THREE.OrbitControls.prototype.constructor = _THREE.OrbitControls;
