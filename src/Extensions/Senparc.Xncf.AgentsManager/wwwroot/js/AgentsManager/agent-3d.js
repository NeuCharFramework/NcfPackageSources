(function (global) {
  function hashNumber(input) {
    let hash = 0;
    const str = String(input || '0');
    for (let i = 0; i < str.length; i++) {
      hash = ((hash << 5) - hash) + str.charCodeAt(i);
      hash |= 0;
    }
    return Math.abs(hash);
  }

  // Id 仅在本地 Agent 域内唯一；混合群组使用 ParticipantKey 规避本地/远程同号冲突。
  function participantKey(agent) {
    return agent && agent.participantKey ? agent.participantKey : 'local:' + (agent ? agent.id : '0');
  }

  function linkParticipantKey(link) {
    return link && link.participantKey ? link.participantKey : 'local:' + (link ? link.agentId : '0');
  }

  function collaborationParticipantKeys(collaboration) {
    if (collaboration && Array.isArray(collaboration.participantKeys) && collaboration.participantKeys.length) {
      return collaboration.participantKeys;
    }
    return (collaboration && collaboration.agentIds ? collaboration.agentIds : []).map(function (id) { return 'local:' + id; });
  }

  function normalizeSkillKinds(agent) {
    const kinds = agent && Array.isArray(agent.skillKinds) ? agent.skillKinds : [];
    return kinds
      .map(function (kind) { return String(kind || '').trim().toLowerCase(); })
      .filter(Boolean)
      .filter(function (kind, index, list) { return list.indexOf(kind) === index; });
  }

  function skillShortName(kind) {
    return {
      function: 'F',
      workflow: 'W',
      plugin: 'P',
      mcp: 'M',
      a2a: 'A2A',
      human: 'H'
    }[kind] || kind.toUpperCase();
  }

  function skillDisplayText(agent) {
    const skills = normalizeSkillKinds(agent);
    return skills.length ? skills.map(skillShortName).join(' ') : '--';
  }

  function skillColor(kind) {
    return {
      function: 0x5ed4ff,
      workflow: 0x67c23a,
      plugin: 0xa78bfa,
      mcp: 0xf59e0b,
      a2a: 0xffc36e,
      human: 0xf472b6
    }[kind] || 0xb8c7d9;
  }

  function groupStatusInfo(group) {
    const statusMap = group && group.taskStatusCounts ? group.taskStatusCounts : {};
    const waiting = statusMap[0] || statusMap['0'] || 0;
    const chatting = statusMap[1] || statusMap['1'] || 0;
    const paused = statusMap[2] || statusMap['2'] || 0;
    const finished = statusMap[3] || statusMap['3'] || 0;
    const cancelled = statusMap[4] || statusMap['4'] || 0;
    const failed = statusMap[5] || statusMap['5'] || 0;
    const humanPending = Number(group && group.humanInTheLoopPendingCount || 0);
    const total = waiting + chatting + paused + finished + cancelled + failed;

    let kind = 'idle';
    if (group && group.enable === false) {
      kind = 'disabled';
    } else if (humanPending > 0) {
      kind = 'hil-paused';
    } else if (paused > 0) {
      kind = 'paused';
    } else if (chatting > 0) {
      kind = 'chatting';
    } else if (waiting > 0) {
      kind = 'waiting';
    }

    return {
      kind: kind,
      waiting: waiting,
      chatting: chatting,
      paused: paused,
      finished: finished,
      cancelled: cancelled,
      failed: failed,
      humanPending: humanPending,
      total: total
    };
  }

  function groupStatusStyle(status) {
    return {
      disabled: {
        color: 0x6c5561,
        emissive: 0x331821,
        emissiveIntensity: 0.34,
        opacity: 0.48,
        ring: 0xd57585
      },
      'hil-paused': {
        color: 0xd946ef,
        emissive: 0x7e1b77,
        emissiveIntensity: 0.58,
        opacity: 0.92,
        ring: 0xf0abfc
      },
      paused: {
        color: 0xf59e0b,
        emissive: 0x7c3d0b,
        emissiveIntensity: 0.38,
        opacity: 0.9,
        ring: 0xfbbf24
      },
      chatting: {
        color: 0x48c5ff,
        emissive: 0x0b527c,
        emissiveIntensity: 0.3,
        opacity: 0.9,
        ring: 0x7dd3fc
      },
      waiting: {
        color: 0x3376cd,
        emissive: 0x123e74,
        emissiveIntensity: 0.26,
        opacity: 0.86,
        ring: 0x60a5fa
      },
      idle: {
        color: 0x7f91a6,
        emissive: 0x000000,
        emissiveIntensity: 0,
        opacity: 0.86,
        ring: 0x94a3b8
      }
    }[status] || {
      color: 0x7f91a6,
      emissive: 0x000000,
      emissiveIntensity: 0,
      opacity: 0.86,
      ring: 0x94a3b8
    };
  }

  function textSprite(text, options) {
    const fontSize = options.fontSize || 24;
    const padding = options.padding || 14;
    const scaleDivisor = options.scaleDivisor || 26;
    const sizeAttenuation = typeof options.sizeAttenuation === 'boolean' ? options.sizeAttenuation : true;
    const maxLineLength = options.maxLineLength || 30;
    const maxLines = options.maxLines || 8;
    const maxWorldWidth = options.maxWorldWidth || 15;
    const maxWorldHeight = options.maxWorldHeight || 9;
    const bg = options.background || 'rgba(10,20,30,0.82)';
    const color = options.color || '#EAF2FF';
    const border = options.border || 'rgba(86, 162, 255, 0.55)';

    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    ctx.font = 'bold ' + fontSize + 'px sans-serif';

    const rows = [];
    String(text || '').split('\n').forEach(function (line) {
      const lineText = String(line || '');
      if (!lineText) {
        rows.push('');
        return;
      }
      let start = 0;
      while (start < lineText.length) {
        rows.push(lineText.slice(start, start + maxLineLength));
        start += maxLineLength;
        if (rows.length >= maxLines) {
          break;
        }
      }
    });

    if (rows.length === 0) {
      rows.push('');
    }

    if (rows.length > maxLines) {
      rows.length = maxLines;
    }

    if (rows.length >= maxLines) {
      const last = rows[maxLines - 1] || '';
      if (last.length >= maxLineLength) {
        rows[maxLines - 1] = last.slice(0, maxLineLength - 1) + '…';
      }
    }

    const width = Math.max.apply(null, rows.map(function (line) { return ctx.measureText(line).width; })) + padding * 2;
    const rowHeight = Math.ceil(fontSize * 1.45);
    const height = rowHeight * rows.length + padding * 2;

    const dpr = Math.max(1, Math.min(2, window.devicePixelRatio || 1));
    canvas.width = Math.ceil(width * dpr);
    canvas.height = Math.ceil(height * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

    ctx.fillStyle = bg;
    ctx.strokeStyle = border;
    ctx.lineWidth = 3;

    const x = 2;
    const y = 2;
    const w = width - 4;
    const h = height - 4;
    const r = 10;

    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();

    ctx.font = 'bold ' + fontSize + 'px sans-serif';
    ctx.fillStyle = color;
    ctx.textBaseline = 'top';
    rows.forEach(function (line, index) {
      ctx.fillText(line, padding, padding + index * rowHeight);
    });

    const texture = new THREE.CanvasTexture(canvas);
    texture.needsUpdate = true;
    texture.minFilter = THREE.LinearFilter;
    texture.magFilter = THREE.LinearFilter;
    const material = new THREE.SpriteMaterial({
      map: texture,
      transparent: true,
      depthWrite: false,
      sizeAttenuation: sizeAttenuation
    });
    const sprite = new THREE.Sprite(material);
    const rawWorldWidth = width / scaleDivisor;
    const rawWorldHeight = height / scaleDivisor;
    const fitRatio = Math.min(1, maxWorldWidth / rawWorldWidth, maxWorldHeight / rawWorldHeight);
    const worldWidth = rawWorldWidth * fitRatio;
    const worldHeight = rawWorldHeight * fitRatio;
    sprite.scale.set(worldWidth, worldHeight, 1);
    return sprite;
  }

  function AgentGraph3D(container, options) {
    this.container = container;
    this.options = options || {};
    this.scene = null;
    this.camera = null;
    this.renderer = null;
    this.controls = null;
    this.raycaster = null;
    this.mouse = null;
    this.frameId = null;
    this.resizeHandler = null;
    this.pointerMoveHandler = null;

    this.groupObjects = [];
    this.agentObjects = [];
    this.linkObjects = [];

    this.agentById = new Map();
    this.groupById = new Map();

    this.currentSnapshot = null;
    this.targets = new Map();
    this.activeGroupId = null;
    this.activeAgentId = null;
    this.lockedGroupId = null;
    this.pointerClickHandler = null;
    this.taskObjects = [];
    this.room = null;
    this.roomLayout = null;
    this.roomSignature = '';
    this.pointer = null;
    this.dropGroupId = null;
    this.pendingSnapshot = null;
    this.reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    this.listeners = [];
    this.resizeObserver = null;
    this.cameraFitted = false;
    this.roomBounds = null;
    this.keyHandler = null;
    // Keep the mutable WebGL runtime out of Vue 2's deep observation.
    Object.seal(this);
  }

  AgentGraph3D.prototype.init = function () {
    if (!this.container || typeof THREE === 'undefined') {
      return false;
    }

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x102234);

    const width = Math.max(1, this.container.clientWidth || 320);
    const height = Math.max(1, this.container.clientHeight || 320);

    this.camera = new THREE.PerspectiveCamera(48, width / height, 0.1, 1000);
    this.camera.position.set(65, 70, 85);

    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
    this.renderer.setPixelRatio(Math.min(2, window.devicePixelRatio || 1));
    this.renderer.setSize(width, height);
    this.container.innerHTML = '';
    this.container.appendChild(this.renderer.domElement);

    if (typeof THREE.OrbitControls !== 'undefined') {
      this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
      this.controls.enableDamping = true;
      this.controls.dampingFactor = 0.07;
      this.controls.maxDistance = 600;
      this.controls.minDistance = 18;
      this.controls.maxPolarAngle = Math.PI / 2.12;
      this.controls.target.set(0, 0, 0);
    }

    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();

    const ambient = new THREE.AmbientLight(0xffffff, 0.58);
    const key = new THREE.DirectionalLight(0xa9d2ff, 0.9);
    key.position.set(40, 80, 20);
    const rim = new THREE.DirectionalLight(0xffffff, 0.4);
    rim.position.set(-50, 25, -45);

    this.scene.add(ambient);
    this.scene.add(key);
    this.scene.add(rim);

    this.resizeHandler = this.handleResize.bind(this);
    window.addEventListener('resize', this.resizeHandler);
    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(this.resizeHandler);
      this.resizeObserver.observe(this.container);
    }
    this.keyHandler = function (event) {
      if (event.key === 'Escape') this.cancelStudioDrag();
    }.bind(this);
    window.addEventListener('keydown', this.keyHandler);
    const canvas = this.renderer.domElement;
    [
      ['pointerdown', this.handleStudioPointerDown, true],
      ['pointermove', this.handleStudioPointerMove, true],
      ['pointerup', this.handleStudioPointerUp, true],
      ['pointercancel', this.cancelStudioDrag, true],
      ['lostpointercapture', this.cancelStudioDrag, true],
      ['pointerleave', this.clearGroupFocus, false],
      ['webglcontextlost', function (event) {
        event.preventDefault();
        if (this.options.onError) this.options.onError();
      }, false]
    ].forEach(function (entry) {
      const listener = entry[1].bind(this);
      canvas.addEventListener(entry[0], listener, entry[2]);
      this.listeners.push([entry[0], listener, entry[2]]);
    }.bind(this));

    this.animate();
    return true;
  };

  AgentGraph3D.prototype.dispose = function () {
    this.cancelStudioDrag();
    this.pendingSnapshot = null;
    if (this.frameId) {
      cancelAnimationFrame(this.frameId);
      this.frameId = null;
    }

    if (this.renderer && this.pointerMoveHandler) {
      this.renderer.domElement.removeEventListener('mousemove', this.pointerMoveHandler);
    }

    if (this.renderer && this.pointerClickHandler) {
      this.renderer.domElement.removeEventListener('click', this.pointerClickHandler);
    }

    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
    }
    if (this.resizeObserver) this.resizeObserver.disconnect();
    if (this.keyHandler) window.removeEventListener('keydown', this.keyHandler);
    if (this.renderer) {
      this.listeners.forEach(function (entry) {
        this.renderer.domElement.removeEventListener(entry[0], entry[1], entry[2]);
      }.bind(this));
    }
    this.listeners = [];
    this.clearObjects();
    this.disposeObject(this.room);
    this.room = null;

    if (this.controls) {
      this.controls.dispose();
      this.controls = null;
    }

    if (this.renderer) {
      this.renderer.dispose();
      this.renderer.forceContextLoss();
      this.renderer = null;
    }

    if (this.container) {
      this.container.innerHTML = '';
    }

    this.scene = null;
    this.agentById.clear();
    this.groupById.clear();
  };

  AgentGraph3D.prototype.handleResize = function () {
    if (!this.renderer || !this.camera || !this.container) {
      return;
    }
    const width = Math.max(1, this.container.clientWidth || 320);
    const height = Math.max(1, this.container.clientHeight || 320);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height);
    if (this.roomBounds) this.resetCamera();
  };

  AgentGraph3D.prototype.text = function (key) {
    return this.options.text ? this.options.text(key) : key;
  };

  AgentGraph3D.prototype.disposeObject = function (object) {
    if (!object) return;
    const resources = new Set();
    object.traverse(function (node) {
      if (node.geometry) resources.add(node.geometry);
      const materials = Array.isArray(node.material) ? node.material : [node.material];
      materials.filter(Boolean).forEach(function (material) {
        if (material.map) resources.add(material.map);
        resources.add(material);
      });
    });
    resources.forEach(function (resource) { resource.dispose(); });
    if (object.parent) object.parent.remove(object);
  };

  AgentGraph3D.prototype.buildStudioRoom = function (layout, agentCount) {
    const loungeRows = Math.ceil(agentCount / Math.max(1, Math.floor((layout.width - 10) / 8)));
    const depth = layout.depth + Math.max(0, loungeRows - 1) * 8;
    const signature = JSON.stringify([layout.width, depth, layout.stations]);
    if (signature === this.roomSignature) return;
    this.roomSignature = signature;
    this.disposeObject(this.room);
    this.room = new THREE.Group();
    this.scene.add(this.room);
    const room = this.room;
    const width = layout.width;
    const centerZ = (depth - layout.depth) / 2;
    const backZ = centerZ - depth / 2;
    const frontZ = centerZ + depth / 2;
    this.roomBounds = { width: width, depth: depth, centerZ: centerZ };

    function box(w, h, d, color, x, y, z, emissive) {
      const mesh = new THREE.Mesh(new THREE.BoxGeometry(w, h, d),
        new THREE.MeshStandardMaterial({ color: color, roughness: 0.8,
          emissive: emissive || 0x000000, emissiveIntensity: emissive ? 0.35 : 0 }));
      mesh.position.set(x, y, z);
      room.add(mesh);
      return mesh;
    }
    box(width, 0.7, depth, 0x233b4b, 0, -0.55, centerZ);
    box(width, 11, 0.6, 0x314d60, 0, 5.2, backZ);
    box(0.6, 7, depth, 0x294457, -width / 2, 3.2, centerZ);
    box(width, 0.3, 0.4, 0x72c9cc, 0, 10.5, backZ + 0.4, 0x72c9cc);
    const grid = new THREE.GridHelper(Math.max(width, depth), Math.ceil(Math.max(width, depth) / 6),
      0x3f6170, 0x2b4656);
    grid.position.set(0, -0.15, centerZ);
    room.add(grid);
    for (let x = -width / 2 + 9; x < width / 2 - 6; x += 18) {
      box(11, 5.6, 0.22, 0x547990, x, 6.3, backZ + 0.5, 0x305a76);
      box(0.2, 5.6, 0.35, 0x90b5bf, x, 6.3, backZ + 0.7);
      box(11, 0.2, 0.35, 0x90b5bf, x, 6.3, backZ + 0.7);
    }
    const sign = textSprite(this.text('Title'), {
      fontSize: 26, maxWorldWidth: 16, maxWorldHeight: 3,
      background: 'rgba(10,30,43,0.92)', color: '#9cebdc'
    });
    sign.position.set(0, 13, backZ + 0.6);
    room.add(sign);
    layout.stations.forEach(function (station, index) {
      box(layout.stations.length ? station.radius * 1.75 : 20, 0.08, station.radius * 1.65,
        index % 2 ? 0x244c51 : 0x29465c, station.x, -0.08, station.z);
      for (let seat = 0; seat < 6; seat++) {
        const angle = seat * Math.PI / 3;
        box(2.8, 0.5, 2.8, 0x3b5967, station.x + Math.cos(angle) * 8, 0.7,
          station.z + Math.sin(angle) * 8);
      }
    });
    box(width - 10, 0.12, Math.max(8, loungeRows * 8), 0x254b55, 0, 0,
      layout.loungeZ + Math.max(0, loungeRows - 1) * 4);
    const lounge = textSprite(this.text('ReadyArea'), {
      fontSize: 18, maxWorldWidth: 12, maxWorldHeight: 2.5, color: '#9ed8e9'
    });
    lounge.position.set(-width / 2 + 9, 6, layout.loungeZ);
    room.add(lounge);
    [-1, 1].forEach(function (side) {
      const x = side * (width / 2 - 4);
      box(2.4, 2, 2.4, 0x829596, x, 0.8, backZ + 5);
      const leaves = new THREE.Mesh(new THREE.IcosahedronGeometry(2.1, 1),
        new THREE.MeshStandardMaterial({ color: 0x4caa82, roughness: 0.9 }));
      leaves.position.set(x, 3.1, backZ + 5);
      room.add(leaves);
      box(8, 1.6, 3, 0x4e7481, x - side * 4, 0.8, frontZ - 3);
      box(8, 1.4, 0.65, 0x375a70, x - side * 4, 2.2, frontZ - 4.3);
    });
    this.resetCamera();
  };

  AgentGraph3D.prototype.resetCamera = function () {
    if (!this.camera) return;
    const bounds = this.roomBounds || { width: 64, depth: 64, centerZ: 0 };
    const target = new THREE.Vector3(0, 2, bounds.centerZ);
    const direction = new THREE.Vector3(0.68, 0.9, 0.95).normalize();
    const right = new THREE.Vector3().crossVectors(new THREE.Vector3(0, 1, 0), direction).normalize();
    const up = new THREE.Vector3().crossVectors(direction, right);
    const tanVertical = Math.tan(this.camera.fov * Math.PI / 360) * 0.85;
    const tanHorizontal = tanVertical * this.camera.aspect * 0.95;
    let distance = 35;
    [-1, 1].forEach(function (x) {
      [-0.55, 16].forEach(function (y) {
        [-1, 1].forEach(function (z) {
          const corner = new THREE.Vector3(x * bounds.width / 2, y, bounds.centerZ + z * bounds.depth / 2).sub(target);
          const depth = corner.dot(direction);
          distance = Math.max(distance, depth + Math.abs(corner.dot(right)) / tanHorizontal,
            depth + Math.abs(corner.dot(up)) / tanVertical);
        });
      });
    });
    distance *= 1.03;
    this.camera.far = Math.max(1000, distance * 6);
    this.camera.updateProjectionMatrix();
    this.camera.position.copy(target).addScaledVector(direction, distance);
    if (this.controls) {
      this.controls.maxDistance = Math.max(220, distance * 3);
      this.controls.target.copy(target);
      this.controls.update();
    } else {
      this.camera.lookAt(target);
    }
    this.cameraFitted = true;
  };

  AgentGraph3D.prototype.setPointerRay = function (event) {
    if (!this.renderer || !this.camera) return false;
    const rect = this.renderer.domElement.getBoundingClientRect();
    if (!rect.width || !rect.height) return false;
    this.mouse.set(((event.clientX - rect.left) / rect.width) * 2 - 1,
      -((event.clientY - rect.top) / rect.height) * 2 + 1);
    this.raycaster.setFromCamera(this.mouse, this.camera);
    return true;
  };

  AgentGraph3D.prototype.pick = function (event) {
    if (!this.setPointerRay(event)) return null;
    const targets = this.agentObjects.concat(this.groupObjects).reduce(function (all, entry) {
      all.push(entry.mesh, entry.label);
      return all;
    }, []).concat(this.taskObjects.map(function (entry) { return entry.mesh; }));
    const hits = this.raycaster.intersectObjects(targets.filter(Boolean), true);
    for (let i = 0; i < hits.length; i++) {
      let object = hits[i].object;
      while (object && !object.userData.type) object = object.parent;
      if (object && object.userData.type) return object.userData;
    }
    return null;
  };

  AgentGraph3D.prototype.groupAtPointer = function (event) {
    if (!this.renderer) return null;
    const rect = this.renderer.domElement.getBoundingClientRect();
    if (event.clientX < rect.left || event.clientX > rect.right
      || event.clientY < rect.top || event.clientY > rect.bottom) return null;
    if (!this.setPointerRay(event)) return null;
    const hits = this.raycaster.intersectObjects(this.groupObjects.reduce(function (all, entry) {
      return all.concat(entry.mesh, entry.label);
    }, []), true);
    if (hits.length) {
      let object = hits[0].object;
      while (object && !object.userData.groupId) object = object.parent;
      if (object) return object.userData.groupId;
    }
    const point = new THREE.Vector3();
    if (!this.raycaster.ray.intersectPlane(new THREE.Plane(new THREE.Vector3(0, 1, 0), 0), point)) return null;
    const station = (this.roomLayout?.stations || []).find(function (item) {
      return Math.hypot(item.x - point.x, item.z - point.z) < item.radius;
    });
    return station ? station.id : null;
  };

  AgentGraph3D.prototype.highlightDropGroup = function (groupId) {
    this.dropGroupId = groupId;
    this.groupObjects.forEach(function (entry) {
      entry.statusRing.material.color.setHex(entry.group.id === groupId ? 0x67ffbd : entry.style.ring);
      entry.statusRing.scale.setScalar(entry.group.id === groupId ? 1.35 : 1);
    });
  };

  AgentGraph3D.prototype.previewExternalDrop = function (event) {
    this.highlightDropGroup(this.groupAtPointer(event));
  };

  AgentGraph3D.prototype.focusGroup = function (groupId) {
    this.lockedGroupId = groupId;
    if (this.options.onGroupLock) this.options.onGroupLock(groupId, !!groupId);
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.handleStudioPointerDown = function (event) {
    if (event.button !== 0 || this.pointer) return;
    const hit = this.pick(event);
    const agentKey = hit && (hit.type === 'agent' || hit.type === 'agent-label') ? hit.agentId : null;
    const entry = agentKey ? this.agentById.get(agentKey) : null;
    this.pointer = { id: event.pointerId, x: event.clientX, y: event.clientY,
      agentKey: entry?.agent.enable && (!this.options.canDrag || this.options.canDrag())
        ? agentKey : null, dragging: false, moved: false, hit: hit };
    if (this.pointer.agentKey) {
      if (this.controls) this.controls.enabled = false;
      this.renderer.domElement.setPointerCapture(event.pointerId);
      event.stopImmediatePropagation();
    }
  };

  AgentGraph3D.prototype.handleStudioPointerMove = function (event) {
    const pointer = this.pointer;
    if (!pointer || pointer.id !== event.pointerId) {
      if (!pointer) this.handlePointerMove(event);
      return;
    }
    if (Math.hypot(event.clientX - pointer.x, event.clientY - pointer.y) > 6) pointer.moved = true;
    if (!pointer.agentKey || !pointer.moved) return;
    event.stopImmediatePropagation();
    const entry = this.agentById.get(pointer.agentKey);
    if (!pointer.dragging) {
      pointer.dragging = true;
      this.renderer.domElement.style.cursor = 'grabbing';
      if (this.options.onDragStart) this.options.onDragStart(entry.agent);
    }
    this.setPointerRay(event);
    const point = new THREE.Vector3();
    if (this.raycaster.ray.intersectPlane(new THREE.Plane(new THREE.Vector3(0, 1, 0), -2.1), point)) {
      entry.mesh.position.copy(point);
      entry.label.position.set(point.x, point.y + 3.5, point.z);
      if (entry.pulseRing) entry.pulseRing.position.set(point.x, 0.25, point.z);
      if (entry.statusBadge) entry.statusBadge.position.set(point.x, point.y + 2.15, point.z);
    }
    this.highlightDropGroup(this.groupAtPointer(event));
  };

  AgentGraph3D.prototype.handleStudioPointerUp = function (event) {
    const pointer = this.pointer;
    if (!pointer || pointer.id !== event.pointerId) return;
    const dropTarget = pointer.dragging ? document.elementFromPoint(event.clientX, event.clientY) : null;
    const teamDrop = dropTarget?.closest('.studio-team-dock');
    const groupDrop = dropTarget?.closest('[data-studio-group-id]');
    const groupId = pointer.dragging
      ? (groupDrop ? Number(groupDrop.dataset.studioGroupId) : this.groupAtPointer(event)) : null;
    const hit = pointer.hit;
    this.cancelStudioDrag();
    if (pointer.dragging) {
      event.stopImmediatePropagation();
      if (teamDrop && this.options.onAgentTeamDrop) this.options.onAgentTeamDrop(pointer.agentKey);
      else if (groupId && this.options.onAgentDrop) this.options.onAgentDrop(pointer.agentKey, groupId);
    } else if (!pointer.moved && hit) {
      if (hit.type === 'group') {
        this.focusGroup(hit.groupId);
        if (this.options.onSelect) this.options.onSelect('group', hit.groupId);
      } else if (hit.type === 'task') {
        if (this.options.onSelect) this.options.onSelect('task', hit.taskId);
      } else if (this.options.onSelect) this.options.onSelect('agent', hit.agentId);
    }
  };

  AgentGraph3D.prototype.cancelStudioDrag = function () {
    const pointer = this.pointer;
    this.pointer = null;
    if (this.controls) this.controls.enabled = true;
    if (this.renderer) {
      this.renderer.domElement.style.cursor = '';
      if (pointer && this.renderer.domElement.hasPointerCapture(pointer.id)) {
        this.renderer.domElement.releasePointerCapture(pointer.id);
      }
    }
    this.highlightDropGroup(null);
    if (pointer?.dragging && this.options.onDragEnd) this.options.onDragEnd();
    if (this.pendingSnapshot) {
      const snapshot = this.pendingSnapshot;
      this.pendingSnapshot = null;
      this.updateGraph(snapshot);
    }
  };

  AgentGraph3D.prototype.animate = function () {
    if (!this.renderer || !this.scene || !this.camera) {
      return;
    }

    this.frameId = requestAnimationFrame(this.animate.bind(this));

    this.agentObjects.forEach(function (entry) {
      if (this.pointer && this.pointer.dragging && this.pointer.agentKey === participantKey(entry.agent)) return;
      const target = entry.target;
      if (!target) {
        return;
      }

      entry.baseY += (target.y - entry.baseY) * (this.reducedMotion ? 1 : 0.15);
      const movingDistance = Math.hypot(target.x - entry.mesh.position.x, target.z - entry.mesh.position.z);
      const moving = movingDistance > 0.03;

      entry.mesh.position.x += (target.x - entry.mesh.position.x) * (this.reducedMotion ? 1 : 0.09);
      entry.mesh.position.z += (target.z - entry.mesh.position.z) * (this.reducedMotion ? 1 : 0.09);

      entry.motionPhase += this.reducedMotion ? 0 : moving ? 0.36 : 0.025;
      const hop = moving && !this.reducedMotion ? Math.abs(Math.sin(entry.motionPhase)) * 0.45 : 0;
      entry.mesh.position.y = entry.baseY + hop;

      const jelly = this.reducedMotion ? 0 : Math.sin(entry.motionPhase);
      const stretchY = moving ? (1 + jelly * 0.18) : (1 + jelly * 0.05);
      const squashXZ = moving ? (1 - jelly * 0.1) : (1 - jelly * 0.03);
      entry.mesh.scale.set(squashXZ, stretchY, squashXZ);

      if (entry.pulseRing) {
        entry.pulseRing.position.set(entry.mesh.position.x, 0.25, entry.mesh.position.z);
        if (entry.isActive) {
          const elapsed = this.reducedMotion ? 0 : Date.now() * 0.0025 + entry.pulsePhase;
          const scale = 1 + ((Math.sin(elapsed) + 1) * 0.22);
          entry.pulseRing.scale.set(scale, scale, scale);
          entry.pulseRing.material.opacity = 0.2 + ((Math.sin(elapsed) + 1) * 0.2);
        }
      }
      if (entry.label) {
        const labelOffset = entry.labelOffset || { x: 0, y: 3.7, z: 0 };
        entry.label.position.set(
          entry.mesh.position.x + labelOffset.x,
          entry.mesh.position.y + labelOffset.y,
          entry.mesh.position.z + labelOffset.z);
      }
      if (entry.statusBadge) {
        entry.statusBadge.position.set(
          entry.mesh.position.x,
          entry.mesh.position.y + 2.15,
          entry.mesh.position.z);
        if (!this.reducedMotion) entry.statusBadge.rotation.y += moving ? 0.025 : 0.008;
      }
    }.bind(this));

    this.groupObjects.forEach(function (entry) {
      if (!entry.statusRing) {
        return;
      }
      const elapsed = this.reducedMotion ? 0 : Date.now() * 0.002 + entry.pulsePhase;
      const isHIL = entry.statusKind === 'hil-paused';
      const scale = entry.group.id === this.dropGroupId ? 1.35
        : isHIL ? 1 + ((Math.sin(elapsed) + 1) * 0.14) : 1;
      entry.statusRing.scale.set(scale, scale, scale);
      entry.statusRing.material.opacity = isHIL
        ? 0.38 + ((Math.sin(elapsed * 1.4) + 1) * 0.22)
        : 0.48;
    }.bind(this));

    this.refreshLinkGeometry();

    if (this.controls) {
      this.controls.update();
    }
    this.renderer.render(this.scene, this.camera);
  };

  AgentGraph3D.prototype.clearObjects = function () {
    const all = [];

    this.groupObjects.forEach(function (g) {
      if (g.mesh) {
        all.push(g.mesh);
      }
      if (g.label) {
        all.push(g.label);
      }
      if (g.statusRing) {
        all.push(g.statusRing);
      }
    });

    this.agentObjects.forEach(function (a) {
      if (a.mesh) {
        all.push(a.mesh);
      }
      if (a.label) {
        all.push(a.label);
      }
      if (a.pulseRing) {
        all.push(a.pulseRing);
      }
      if (a.statusBadge) {
        all.push(a.statusBadge);
      }
    });

    this.linkObjects.forEach(function (line) {
      if (line) {
        all.push(line);
      }
    });

    this.linkObjects.forEach(function (line) {
      if (line && line.userData && line.userData.flowDot) {
        all.push(line.userData.flowDot);
      }
    });

    this.taskObjects.forEach(function (task) { all.push(task.mesh); });
    const disposed = new Set();
    all.forEach(function (obj) {
      if (obj && obj.parent) {
        obj.parent.remove(obj);
      }
      if (obj && typeof obj.traverse === 'function') {
        obj.traverse(function (node) {
          if (node.material) {
            if (Array.isArray(node.material)) {
              node.material.forEach(function (mat) {
                if (mat.map && !disposed.has(mat.map)) {
                  disposed.add(mat.map);
                  mat.map.dispose();
                }
                if (!disposed.has(mat)) { disposed.add(mat); mat.dispose(); }
              });
            } else {
              if (node.material.map && !disposed.has(node.material.map)) {
                disposed.add(node.material.map);
                node.material.map.dispose();
              }
              if (!disposed.has(node.material)) { disposed.add(node.material); node.material.dispose(); }
            }
          }
          if (node.geometry && !disposed.has(node.geometry)) {
            disposed.add(node.geometry);
            node.geometry.dispose();
          }
        });
      }
    });

    this.groupObjects = [];
    this.agentObjects = [];
    this.linkObjects = [];
    this.taskObjects = [];
    this.agentById.clear();
    this.groupById.clear();
  };

  AgentGraph3D.prototype.updateGraph = function (snapshot) {
    if (this.pointer) {
      this.pendingSnapshot = snapshot;
      return;
    }
    const previousPositions = new Map(this.agentObjects.map(function (entry) {
      return [participantKey(entry.agent), entry.mesh.position.clone()];
    }));
    this.currentSnapshot = snapshot || { agents: [], groups: [], links: [], collaborations: [] };
    this.clearObjects();

    const groups = (this.currentSnapshot.groups || []).map(function (group) { return Object.assign({}, group); });
    const agents = this.currentSnapshot.agents || [];
    const links = this.currentSnapshot.links || [];
    const groupedKeys = new Set(links.map(linkParticipantKey));
    const loungeAgents = agents.filter(function (agent) { return !groupedKeys.has(participantKey(agent)); });
    const loungeIndex = new Map(loungeAgents.map(function (agent, index) { return [participantKey(agent), index]; }));
    this.roomLayout = AgentStudioState.layout(this.currentSnapshot);
    const layout = this.roomLayout;
    this.buildStudioRoom(layout, loungeAgents.length);
    const groupGeom = new THREE.CylinderGeometry(4.6, 4.6, 0.65, 32);
    groups.forEach(function (group) {
      const station = layout.stations.find(function (item) { return item.id === group.id; });
      group._pos = new THREE.Vector3(station.x, 0, station.z);
      const isEnabled = group.enable !== false;
      const status = groupStatusInfo(group);
      const style = groupStatusStyle(status.kind);
      const totalTasks = status.total;
      const mat = new THREE.MeshStandardMaterial({
        color: style.color,
        emissive: style.emissive,
        emissiveIntensity: style.emissiveIntensity,
        transparent: true,
        opacity: style.opacity,
        metalness: 0.15,
        roughness: 0.55
      });
      const pillar = new THREE.Mesh(groupGeom, mat);
      pillar.position.copy(group._pos);
      pillar.position.y = 2.9;
      pillar.userData = { type: 'group', groupId: group.id };
      const base = new THREE.Mesh(new THREE.CylinderGeometry(2.1, 2.7, 2.6, 16),
        new THREE.MeshStandardMaterial({ color: 0x284455, roughness: 0.65 }));
      base.position.y = -1.6;
      pillar.add(base);
      const screen = new THREE.Mesh(new THREE.BoxGeometry(2.8, 1.8, 0.18),
        new THREE.MeshStandardMaterial({ color: 0x172939, emissive: style.ring, emissiveIntensity: 0.2 }));
      screen.position.set(0, 1.2, -1.3);
      pillar.add(screen);
      this.scene.add(pillar);
      const text = group.name + '\n' + this.text(isEnabled ? 'Enabled' : 'Disabled')
        + ' · ' + totalTasks + ' ' + this.text('Tasks')
        + (status.humanPending > 0 ? '\n' + this.text('HumanPending') + ': ' + status.humanPending : '');
      const label = textSprite(text, {
        fontSize: 18,
        padding: 16,
        scaleDivisor: 20,
        maxLineLength: 24,
        maxLines: 3,
        maxWorldWidth: 12,
        maxWorldHeight: 6,
        background: status.kind === 'hil-paused'
          ? 'rgba(54,12,62,0.94)'
          : status.kind === 'paused'
            ? 'rgba(54,31,5,0.94)'
            : !isEnabled ? 'rgba(40,17,24,0.92)' : 'rgba(5,14,26,0.90)',
        border: status.kind === 'hil-paused'
          ? 'rgba(240,171,252,0.9)'
          : status.kind === 'paused'
            ? 'rgba(251,191,36,0.86)'
            : !isEnabled ? 'rgba(239,119,139,0.82)' : 'rgba(72,197,255,0.65)',
        color: status.kind === 'hil-paused'
          ? '#fce7ff'
          : status.kind === 'paused' ? '#fff1c2' : !isEnabled ? '#FFE2E8' : '#DDEFFF'
      });
      label.position.set(group._pos.x, 8.5, group._pos.z - 2);
      label.userData = { type: 'group', groupId: group.id };
      this.scene.add(label);

      let statusRing = null;
      {
        const ringGeometry = new THREE.TorusGeometry(5.1, 0.12, 10, 48);
        const ringMaterial = new THREE.MeshBasicMaterial({
          color: style.ring,
          transparent: true,
          opacity: 0.48,
          side: THREE.DoubleSide,
          depthWrite: false
        });
        statusRing = new THREE.Mesh(ringGeometry, ringMaterial);
        statusRing.rotation.x = -Math.PI / 2;
        statusRing.position.set(group._pos.x, 0.18, group._pos.z);
        this.scene.add(statusRing);
      }

      const groupEntry = {
        mesh: pillar,
        label: label,
        group: group,
        statusRing: statusRing,
        statusKind: status.kind,
        style: style,
        pulsePhase: (hashNumber(group.id + '-status') % 100) / 10
      };
      this.groupObjects.push(groupEntry);
      this.groupById.set(group.id, groupEntry);
    }.bind(this));

    const memberships = new Map();
    links.forEach(function (link) {
      const key = linkParticipantKey(link);
      if (!memberships.has(key)) {
        memberships.set(key, []);
      }
      memberships.get(key).push(link.groupId);
    });

    const memberOrderByGroup = new Map();
    groups.forEach(function (group) {
      const configuredKeys = Array.isArray(group.memberParticipantKeys) && group.memberParticipantKeys.length
        ? group.memberParticipantKeys
        : (group.memberAgentIds || []).map(function (id) { return 'local:' + id; });
      const linkedKeys = links
        .filter(function (link) { return link.groupId === group.id; })
        .map(linkParticipantKey);
      const keys = configuredKeys.length ? configuredKeys : linkedKeys;
      memberOrderByGroup.set(group.id, keys);
    });

    const activeGroupIds = new Set((this.currentSnapshot.collaborations || []).map(function (c) { return c.groupId; }));
    const activeAgentIds = new Set();
    const activeLinkKeySet = new Set();
    (this.currentSnapshot.collaborations || []).forEach(function (col) {
      collaborationParticipantKeys(col).forEach(function (key) {
        activeAgentIds.add(key);
        activeLinkKeySet.add(col.groupId + '-' + key);
      });
    });
    const agentGeom = new THREE.SphereGeometry(1.6, 22, 22);

    agents.forEach(function (agent, index) {
      const agentKey = participantKey(agent);
      const memberGroupIds = memberships.get(agentKey) || [];
      let target = null;
      let labelOffset = { x: 0, y: 4.2, z: 0 };

      const activeGroupId = memberGroupIds.find(function (groupId) { return activeGroupIds.has(groupId); });
      const primaryGroupId = activeGroupId || memberGroupIds[0];
      if (primaryGroupId) {
        const groupNode = this.groupById.get(primaryGroupId);
        if (groupNode) {
          const memberKeys = memberOrderByGroup.get(primaryGroupId) || [];
          const memberIndex = Math.max(0, memberKeys.indexOf(agentKey));
          const memberCount = Math.max(memberKeys.length, memberGroupIds.length, 1);
          const ringCapacity = Math.min(10, Math.max(6, Math.ceil(Math.sqrt(memberCount) * 3)));
          const ringIndex = Math.floor(memberIndex / ringCapacity);
          const slot = memberIndex % ringCapacity;
          const theta = (Math.PI * 2 * slot / ringCapacity)
            - Math.PI / 2;
          const spread = 8 + ringIndex * 4;
          target = new THREE.Vector3(
            groupNode.mesh.position.x + Math.cos(theta) * spread,
            2.1,
            groupNode.mesh.position.z + Math.sin(theta) * spread
          );
          labelOffset = {
            x: 0,
            y: 3.5,
            z: 0
          };
        }
      }

      if (!target) {
        const slot = loungeIndex.get(agentKey) || 0;
        const columns = Math.max(1, Math.floor((layout.width - 10) / 8));
        target = new THREE.Vector3(
          (slot % columns - (Math.min(columns, loungeAgents.length) - 1) / 2) * 8,
          2.1,
          layout.loungeZ + Math.floor(slot / columns) * 8);
        labelOffset = { x: 0, y: 3.5, z: 0 };
      }

      const isHILPaused = Number(agent.humanInTheLoopPausedCount || 0) > 0;
      const isPaused = Number(agent.pausedCount || 0) > 0;
      const isActive = activeAgentIds.has(agentKey) || agent.chattingCount > 0;
      const agentColor = !agent.enable
        ? 0x6e7d90
        : isHILPaused
          ? 0xf472b6
          : isPaused
            ? 0xf59e0b
            : agent.agentKind === 'RemoteA2A'
              ? (isActive ? 0xffb34d : 0xf59e0b)
              : (isActive ? 0x8be8bd : 0x5ed4ff);
      const agentEmissive = isHILPaused
        ? 0x7e1b58
        : isPaused
          ? 0x7c3d0b
          : isActive
            ? (agent.agentKind === 'RemoteA2A' ? 0x7c3d0b : 0x175f54)
            : 0x000000;
      const mat = new THREE.MeshStandardMaterial({
        color: agentColor,
        emissive: agentEmissive,
        emissiveIntensity: isHILPaused ? 0.55 : isPaused || isActive ? 0.38 : 0,
        transparent: true,
        opacity: 0.98,
        metalness: 0.08,
        roughness: 0.45
      });
      const sphere = new THREE.Mesh(agentGeom, mat);
      sphere.position.copy(previousPositions.get(agentKey) || target);
      sphere.userData = { type: 'agent', agentId: agentKey };
      this.decorateCuteAgent(sphere, agent.enable);
      this.decorateAgentSkills(sphere, agent.skillKinds);
      this.scene.add(sphere);

      const stateText = !agent.enable
        ? this.text('Disabled')
        : isHILPaused
          ? this.text('HumanPending')
          : isPaused
            ? this.text('Paused')
            : isActive ? this.text('Working') : this.text('Ready');
      const label = textSprite(
        agent.name
        + '\n' + (agent.agentKind === 'RemoteA2A' ? 'A2A' : agent.isHuman ? this.text('Human') : this.text('Local'))
        + ' · ' + stateText
        + '\n' + skillDisplayText(agent),
        {
        fontSize: 16,
        padding: 12,
        scaleDivisor: 22,
        maxLineLength: 26,
        maxLines: 3,
        maxWorldWidth: 8.5,
        maxWorldHeight: 3.8,
        background: isHILPaused ? 'rgba(73,18,59,0.9)' : isPaused ? 'rgba(67,38,6,0.9)' : 'rgba(6,14,24,0.86)',
        border: isHILPaused ? 'rgba(244,114,182,0.82)' : isPaused ? 'rgba(251,191,36,0.82)' : 'rgba(94,212,255,0.55)',
        color: '#E8F7FF'
      });
      label.position.set(target.x + labelOffset.x, target.y + labelOffset.y, target.z + labelOffset.z);
      label.userData = { type: 'agent-label', agentId: agentKey };
      label.material.opacity = 0.9;
      this.scene.add(label);

      const entry = {
        mesh: sphere,
        label: label,
        agent: agent,
        target: target,
        groupIds: memberGroupIds,
        pulseRing: null,
        statusBadge: null,
        labelOffset: labelOffset,
        isActive: isActive,
        pulsePhase: (hashNumber(agentKey) % 100) / 10,
        motionPhase: (hashNumber(agentKey + '-motion') % 100) / 16,
        baseY: sphere.position.y
      };

      if (entry.isActive) {
        const ringGeometry = new THREE.RingGeometry(1.9, 2.25, 36);
        const ringMaterial = new THREE.MeshBasicMaterial({
          color: 0x66d4ff,
          transparent: true,
          opacity: 0.35,
          side: THREE.DoubleSide,
          depthWrite: false
        });
        const ring = new THREE.Mesh(ringGeometry, ringMaterial);
        ring.rotation.x = -Math.PI / 2;
        ring.position.set(target.x, 0.25, target.z);
        this.scene.add(ring);
        entry.pulseRing = ring;
      }

      if (isHILPaused || isPaused) {
        const badgeGeometry = new THREE.TorusGeometry(1.9, 0.13, 10, 32);
        const badgeMaterial = new THREE.MeshBasicMaterial({
          color: isHILPaused ? 0xf472b6 : 0xf59e0b,
          transparent: true,
          opacity: 0.72,
          side: THREE.DoubleSide,
          depthWrite: false
        });
        const badge = new THREE.Mesh(badgeGeometry, badgeMaterial);
        badge.rotation.x = -Math.PI / 2;
        badge.position.set(target.x, target.y + 2.15, target.z);
        this.scene.add(badge);
        entry.statusBadge = badge;
      }

      this.agentObjects.push(entry);
      this.agentById.set(agentKey, entry);
    }.bind(this));

    links.forEach(function (link) {
      const groupNode = this.groupById.get(link.groupId);
      const linkKey = linkParticipantKey(link);
      const agentNode = this.agentById.get(linkKey);
      if (!groupNode || !agentNode) {
        return;
      }

      const geometry = new THREE.BufferGeometry();
      geometry.setAttribute('position', new THREE.Float32BufferAttribute([0, 0, 0, 0, 0, 0], 3));
      const material = new THREE.LineBasicMaterial({
        color: 0x7ed8ff,
        transparent: true,
        opacity: 0.5
      });
      const line = new THREE.Line(geometry, material);
      const activeKey = link.groupId + '-' + linkKey;
      const isGroupEnabled = groupNode.group.enable !== false;
      line.userData = {
        groupId: link.groupId,
        agentId: linkKey,
        isActive: isGroupEnabled && activeLinkKeySet.has(activeKey),
        isGroupEnabled: isGroupEnabled,
        phase: (hashNumber(activeKey) % 100) / 10,
        flowDot: null
      };

      if (line.userData.isActive) {
        const dotGeometry = new THREE.SphereGeometry(0.22, 12, 12);
        const dotMaterial = new THREE.MeshStandardMaterial({
          color: 0x7be3ff,
          emissive: 0x2eaad1,
          emissiveIntensity: 0.65,
          metalness: 0.1,
          roughness: 0.25,
          transparent: true,
          opacity: 0.92
        });
        const flowDot = new THREE.Mesh(dotGeometry, dotMaterial);
        this.scene.add(flowDot);
        line.userData.flowDot = flowDot;
      }

      this.scene.add(line);
      if (!isGroupEnabled) {
        line.material.color.setHex(0x735764);
        line.material.opacity = 0.24;
      }
      this.linkObjects.push(line);
    }.bind(this));

    const tasks = AgentStudioState.tasks(this.currentSnapshot);
    groups.forEach(function (group) {
      const groupTasks = tasks.filter(function (task) { return task.groupId === group.id; }).slice(0, 4);
      groupTasks.forEach(function (task, index) {
        const colors = ['#80b6fa', '#67ddc6', '#ffc475', '#a8dba3', '#a8bdcf', '#ffa79f'];
        const card = textSprite('#' + task.id + ' ' + task.name, {
          fontSize: 14, maxLines: 1, maxLineLength: 24, maxWorldWidth: 9, maxWorldHeight: 1.8,
          color: colors[task.status] || '#eaf2ff', background: 'rgba(15,35,52,0.95)'
        });
        card.position.set(group._pos.x, 6.3 + index * 1.5, group._pos.z + 7);
        card.userData = { type: 'task', taskId: task.id };
        this.scene.add(card);
        this.taskObjects.push({ mesh: card, task: task });
      }.bind(this));
    }.bind(this));
    if (this.lockedGroupId && !this.groupById.has(this.lockedGroupId)) {
      this.lockedGroupId = null;
      if (this.options.onGroupLock) this.options.onGroupLock(null, false);
    }
    this.refreshLinkGeometry();
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.refreshLinkGeometry = function () {
    this.linkObjects.forEach(function (line) {
      const groupNode = this.groupById.get(line.userData.groupId);
      const agentNode = this.agentById.get(line.userData.agentId);
      if (!groupNode || !agentNode) {
        return;
      }

      const positions = line.geometry.attributes.position.array;
      positions[0] = groupNode.mesh.position.x;
      positions[1] = groupNode.mesh.position.y + 0.5;
      positions[2] = groupNode.mesh.position.z;
      positions[3] = agentNode.mesh.position.x;
      positions[4] = agentNode.mesh.position.y + 0.4;
      positions[5] = agentNode.mesh.position.z;
      line.geometry.attributes.position.needsUpdate = true;

      if (line.userData.isActive) {
        const elapsed = this.reducedMotion ? 0.5 : Date.now() * 0.0018 + line.userData.phase;
        line.material.opacity = 0.45 + ((Math.sin(elapsed * 2.2) + 1) * 0.2);

        const t = (elapsed % 1 + 1) % 1;
        const x = positions[0] + (positions[3] - positions[0]) * t;
        const y = positions[1] + (positions[4] - positions[1]) * t;
        const z = positions[2] + (positions[5] - positions[2]) * t;

        if (line.userData.flowDot) {
          line.userData.flowDot.position.set(x, y, z);
          line.userData.flowDot.material.opacity = 0.6 + ((Math.sin(elapsed * 3.1) + 1) * 0.2);
        }
      }
    }.bind(this));
  };

  AgentGraph3D.prototype.handlePointerMove = function (event) {
    if (!this.camera || !this.renderer || !this.raycaster) {
      return;
    }

    const rect = this.renderer.domElement.getBoundingClientRect();
    this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    this.raycaster.setFromCamera(this.mouse, this.camera);

    const hoverTargets = this.agentObjects.reduce(function (acc, entry) {
      acc.push(entry.mesh);
      if (entry.label) {
        acc.push(entry.label);
      }
      return acc;
    }, []);
    const agentIntersects = this.raycaster.intersectObjects(hoverTargets, false);
    if (agentIntersects.length > 0) {
      this.activeAgentId = agentIntersects[0].object.userData.agentId || null;
    } else {
      this.activeAgentId = null;
    }
    if (typeof this.options.onAgentHover === 'function') {
      const agentEntry = this.activeAgentId ? this.agentById.get(this.activeAgentId) : null;
      this.options.onAgentHover(agentEntry ? agentEntry.agent : null);
    }

    if (this.lockedGroupId) {
      this.applyGroupHighlight();
      return;
    }

    const intersects = this.raycaster.intersectObjects(this.groupObjects.map(function (g) { return g.mesh; }), false);
    if (intersects.length > 0) {
      this.activeGroupId = intersects[0].object.userData.groupId;
      if (typeof this.options.onGroupHover === 'function') {
        this.options.onGroupHover(this.activeGroupId);
      }
    } else {
      this.activeGroupId = null;
      if (typeof this.options.onGroupHover === 'function') {
        this.options.onGroupHover(null);
      }
    }
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.handlePointerClick = function (event) {
    if (!this.camera || !this.renderer || !this.raycaster) {
      return;
    }

    const rect = this.renderer.domElement.getBoundingClientRect();
    this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;
    this.raycaster.setFromCamera(this.mouse, this.camera);

    const intersects = this.raycaster.intersectObjects(this.groupObjects.map(function (g) { return g.mesh; }), false);
    if (intersects.length === 0) {
      this.lockedGroupId = null;
      if (typeof this.options.onGroupLock === 'function') {
        this.options.onGroupLock(null, false);
      }
      this.applyGroupHighlight();
      return;
    }

    const groupId = intersects[0].object.userData.groupId;
    if (this.lockedGroupId === groupId) {
      this.lockedGroupId = null;
    } else {
      this.lockedGroupId = groupId;
    }
    if (typeof this.options.onGroupLock === 'function') {
      this.options.onGroupLock(this.lockedGroupId, Boolean(this.lockedGroupId));
    }
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.clearGroupFocus = function () {
    if (this.lockedGroupId) {
      this.applyGroupHighlight();
      return;
    }
    this.activeGroupId = null;
    this.activeAgentId = null;
    if (typeof this.options.onAgentHover === 'function') {
      this.options.onAgentHover(null);
    }
    if (typeof this.options.onGroupHover === 'function') {
      this.options.onGroupHover(null);
    }
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.applyGroupHighlight = function () {
    if (!this.currentSnapshot) {
      return;
    }

    const activeGroup = this.lockedGroupId || this.activeGroupId;
    const activeMemberSet = new Set();

    if (activeGroup) {
      const focused = (this.currentSnapshot.groups || []).find(function (g) { return g.id === activeGroup; });
      if (focused) {
        const memberKeys = (Array.isArray(focused.memberParticipantKeys) && focused.memberParticipantKeys.length)
          ? focused.memberParticipantKeys
          : (focused.memberAgentIds || []).map(function (id) { return 'local:' + id; });
        memberKeys.forEach(function (key) { activeMemberSet.add(key); });
      }
    }

    this.agentObjects.forEach(function (entry) {
      const agentKey = participantKey(entry.agent);
      const isHovered = this.activeAgentId && agentKey === this.activeAgentId;
      const isGroupFocused = activeGroup && activeMemberSet.has(agentKey);
      const opacity = !activeGroup || activeMemberSet.has(agentKey) ? 0.98 : 0.4;
      entry.mesh.material.opacity = opacity;
      if (entry.label) {
        const baseOpacity = !activeGroup || activeMemberSet.has(agentKey) ? 0.9 : 0.45;
        entry.label.material.opacity = (isHovered || isGroupFocused) ? 1 : baseOpacity;
      }
    }.bind(this));

    this.groupObjects.forEach(function (entry) {
      const isActive = activeGroup && entry.group.id === activeGroup;
      entry.mesh.material.opacity = !activeGroup ? entry.style.opacity : (isActive ? 1 : 0.55);
      entry.mesh.material.emissive.setHex(isActive ? 0x2b8fd1 : entry.style.emissive);
      entry.mesh.material.emissiveIntensity = isActive ? 0.35 : entry.style.emissiveIntensity;
      if (entry.label) {
        entry.label.material.opacity = !activeGroup ? 1 : (isActive ? 1 : 0.3);
      }
    });

    this.linkObjects.forEach(function (line) {
      if (!activeGroup) {
        if (!line.userData.isActive) {
          line.material.opacity = line.userData.isGroupEnabled ? 0.5 : 0.24;
        }
      } else {
        line.material.opacity = line.userData.groupId === activeGroup ? 0.85 : 0.08;
      }

      if (line.userData.flowDot) {
        line.userData.flowDot.visible = !activeGroup || line.userData.groupId === activeGroup;
      }
    });
  };

  AgentGraph3D.prototype.decorateCuteAgent = function (bodyMesh, enable) {
    const accentColor = enable ? 0x8fe7ff : 0xa8b3bf;

    const cap = new THREE.Mesh(
      new THREE.SphereGeometry(1.0, 16, 16),
      new THREE.MeshStandardMaterial({
        color: accentColor,
        transparent: true,
        opacity: 0.55,
        metalness: 0.05,
        roughness: 0.25
      })
    );
    cap.position.set(0, 0.85, 0);
    bodyMesh.add(cap);

    const eyeGeometry = new THREE.SphereGeometry(0.12, 10, 10);
    const eyeMaterial = new THREE.MeshBasicMaterial({ color: 0x10253c });

    const leftEye = new THREE.Mesh(eyeGeometry, eyeMaterial);
    leftEye.position.set(-0.35, 0.28, 1.3);
    const rightEye = new THREE.Mesh(eyeGeometry, eyeMaterial);
    rightEye.position.set(0.35, 0.28, 1.3);
    bodyMesh.add(leftEye);
    bodyMesh.add(rightEye);

    const smile = new THREE.Mesh(
      new THREE.TorusGeometry(0.22, 0.04, 8, 24, Math.PI),
      new THREE.MeshBasicMaterial({ color: 0x153a57 })
    );
    smile.position.set(0, -0.08, 1.28);
    smile.rotation.set(Math.PI * 0.05, 0, Math.PI);
    bodyMesh.add(smile);

    const antenna = new THREE.Mesh(
      new THREE.SphereGeometry(0.14, 10, 10),
      new THREE.MeshStandardMaterial({ color: 0xe7f9ff, emissive: 0x84dfff, emissiveIntensity: 0.35 })
    );
    antenna.position.set(0, 1.55, 0.2);
    bodyMesh.add(antenna);

    const footGeometry = new THREE.SphereGeometry(0.22, 12, 12);
    const footMaterial = new THREE.MeshStandardMaterial({ color: 0xb8f1ff, roughness: 0.6, metalness: 0.02 });
    const leftFoot = new THREE.Mesh(footGeometry, footMaterial);
    leftFoot.scale.set(1.35, 0.7, 1.1);
    leftFoot.position.set(-0.45, -1.35, 0.5);
    const rightFoot = new THREE.Mesh(footGeometry, footMaterial);
    rightFoot.scale.set(1.35, 0.7, 1.1);
    rightFoot.position.set(0.45, -1.35, 0.5);
    bodyMesh.add(leftFoot);
    bodyMesh.add(rightFoot);
  };

  AgentGraph3D.prototype.decorateAgentSkills = function (bodyMesh, skillKinds) {
    const skills = (Array.isArray(skillKinds) ? skillKinds : [])
      .map(function (kind) { return String(kind || '').trim().toLowerCase(); })
      .filter(function (kind, index, list) { return kind && list.indexOf(kind) === index; });
    if (skills.length === 0) {
      return;
    }

    const markerCount = Math.min(skills.length, 6);
    for (let index = 0; index < markerCount; index++) {
      const kind = skills[index];
      let geometry;
      if (kind === 'workflow') {
        geometry = new THREE.TorusGeometry(0.34, 0.08, 8, 18);
      } else if (kind === 'plugin') {
        geometry = new THREE.OctahedronGeometry(0.38, 0);
      } else if (kind === 'mcp') {
        geometry = new THREE.CylinderGeometry(0.24, 0.24, 0.58, 10);
      } else if (kind === 'a2a') {
        geometry = new THREE.IcosahedronGeometry(0.38, 0);
      } else {
        geometry = new THREE.BoxGeometry(0.48, 0.48, 0.48);
      }

      const material = new THREE.MeshStandardMaterial({
        color: skillColor(kind),
        emissive: skillColor(kind),
        emissiveIntensity: 0.22,
        metalness: 0.12,
        roughness: 0.4,
        transparent: true,
        opacity: 0.94
      });
      const marker = new THREE.Mesh(geometry, material);
      const angle = (Math.PI * 2 * index / markerCount) - Math.PI / 2;
      marker.position.set(Math.cos(angle) * 1.45, 1.85, Math.sin(angle) * 1.45);
      if (kind === 'workflow') {
        marker.rotation.x = Math.PI / 2;
      }
      bodyMesh.add(marker);
    }
  };

  global.AgentGraph3D = AgentGraph3D;
})(window);
