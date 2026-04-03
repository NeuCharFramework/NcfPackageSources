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

  function textSprite(text, options) {
    const fontSize = options.fontSize || 24;
    const padding = options.padding || 14;
    const bg = options.background || 'rgba(10,20,30,0.82)';
    const color = options.color || '#EAF2FF';
    const border = options.border || 'rgba(86, 162, 255, 0.55)';

    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    ctx.font = 'bold ' + fontSize + 'px sans-serif';

    const rows = String(text || '').split('\n');
    const width = Math.max.apply(null, rows.map(function (line) { return ctx.measureText(line).width; })) + padding * 2;
    const rowHeight = Math.ceil(fontSize * 1.45);
    const height = rowHeight * rows.length + padding * 2;

    canvas.width = Math.ceil(width);
    canvas.height = Math.ceil(height);

    ctx.fillStyle = bg;
    ctx.strokeStyle = border;
    ctx.lineWidth = 3;

    const x = 2;
    const y = 2;
    const w = canvas.width - 4;
    const h = canvas.height - 4;
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
    const material = new THREE.SpriteMaterial({ map: texture, transparent: true, depthWrite: false });
    const sprite = new THREE.Sprite(material);
    sprite.scale.set(canvas.width / 30, canvas.height / 30, 1);
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
    this.lockedGroupId = null;
    this.avatarTextureLoader = null;
    this.avatarTextureCache = new Map();
    this.pointerClickHandler = null;
  }

  AgentGraph3D.prototype.init = function () {
    if (!this.container || typeof THREE === 'undefined') {
      return false;
    }

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x06131f);

    const width = Math.max(320, this.container.clientWidth || 320);
    const height = Math.max(320, this.container.clientHeight || 320);

    this.camera = new THREE.PerspectiveCamera(48, width / height, 0.1, 1000);
    this.camera.position.set(0, 58, 96);

    this.renderer = new THREE.WebGLRenderer({ antialias: true, alpha: false });
    this.renderer.setPixelRatio(window.devicePixelRatio || 1);
    this.renderer.setSize(width, height);
    this.container.innerHTML = '';
    this.container.appendChild(this.renderer.domElement);

    if (typeof THREE.OrbitControls !== 'undefined') {
      this.controls = new THREE.OrbitControls(this.camera, this.renderer.domElement);
      this.controls.enableDamping = true;
      this.controls.dampingFactor = 0.07;
      this.controls.maxDistance = 220;
      this.controls.minDistance = 35;
      this.controls.target.set(0, 15, 0);
    }

    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();
    this.avatarTextureLoader = new THREE.TextureLoader();

    const ambient = new THREE.AmbientLight(0xffffff, 0.58);
    const key = new THREE.DirectionalLight(0xa9d2ff, 0.9);
    key.position.set(40, 80, 20);
    const rim = new THREE.DirectionalLight(0xffffff, 0.4);
    rim.position.set(-50, 25, -45);

    this.scene.add(ambient);
    this.scene.add(key);
    this.scene.add(rim);

    const grid = new THREE.GridHelper(130, 26, 0x2c4f70, 0x15324c);
    grid.position.y = 0;
    this.scene.add(grid);

    this.resizeHandler = this.handleResize.bind(this);
    window.addEventListener('resize', this.resizeHandler);

    this.pointerMoveHandler = this.handlePointerMove.bind(this);
    this.renderer.domElement.addEventListener('mousemove', this.pointerMoveHandler);
    this.renderer.domElement.addEventListener('mouseleave', this.clearGroupFocus.bind(this));
    this.pointerClickHandler = this.handlePointerClick.bind(this);
    this.renderer.domElement.addEventListener('click', this.pointerClickHandler);

    this.animate();
    return true;
  };

  AgentGraph3D.prototype.dispose = function () {
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

    if (this.controls) {
      this.controls.dispose();
      this.controls = null;
    }

    if (this.renderer) {
      this.renderer.dispose();
      this.renderer.forceContextLoss();
      this.renderer.domElement = null;
      this.renderer = null;
    }

    if (this.container) {
      this.container.innerHTML = '';
    }

    this.scene = null;
    this.agentById.clear();
    this.groupById.clear();
    this.avatarTextureCache.clear();
  };

  AgentGraph3D.prototype.handleResize = function () {
    if (!this.renderer || !this.camera || !this.container) {
      return;
    }
    const width = Math.max(320, this.container.clientWidth || 320);
    const height = Math.max(320, this.container.clientHeight || 320);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height);
  };

  AgentGraph3D.prototype.animate = function () {
    if (!this.renderer || !this.scene || !this.camera) {
      return;
    }

    this.frameId = requestAnimationFrame(this.animate.bind(this));

    this.agentObjects.forEach(function (entry) {
      const target = entry.target;
      if (!target) {
        return;
      }
      entry.mesh.position.lerp(target, 0.09);
      if (entry.avatarSprite) {
        entry.avatarSprite.position.set(entry.mesh.position.x, entry.mesh.position.y + 0.02, entry.mesh.position.z);
      }
      if (entry.pulseRing) {
        entry.pulseRing.position.set(entry.mesh.position.x, 0.25, entry.mesh.position.z);
        if (entry.isActive) {
          const elapsed = Date.now() * 0.0025 + entry.pulsePhase;
          const scale = 1 + ((Math.sin(elapsed) + 1) * 0.22);
          entry.pulseRing.scale.set(scale, scale, scale);
          entry.pulseRing.material.opacity = 0.2 + ((Math.sin(elapsed) + 1) * 0.2);
        }
      }
      if (entry.label) {
        entry.label.position.set(entry.mesh.position.x, entry.mesh.position.y + 3.7, entry.mesh.position.z);
      }
    });

    this.refreshLinkGeometry();

    if (this.controls) {
      this.controls.update();
    }
    this.renderer.render(this.scene, this.camera);
  };

  AgentGraph3D.prototype.clearObjects = function () {
    const all = this.groupObjects.concat(this.agentObjects.map(function (z) { return z.mesh; }), this.linkObjects);
    this.groupObjects.forEach(function (g) {
      if (g.label) {
        all.push(g.label);
      }
    });
    this.agentObjects.forEach(function (a) {
      if (a.label) {
        all.push(a.label);
      }
    });

    all.forEach(function (obj) {
      if (obj && obj.parent) {
        obj.parent.remove(obj);
      }
      if (obj && obj.material && obj.material.map) {
        obj.material.map.dispose();
      }
      if (obj && obj.material) {
        obj.material.dispose();
      }
      if (obj && obj.geometry) {
        obj.geometry.dispose();
      }
    });

    this.groupObjects = [];
    this.agentObjects = [];
    this.linkObjects = [];
    this.agentById.clear();
    this.groupById.clear();
  };

  AgentGraph3D.prototype.updateGraph = function (snapshot) {
    this.currentSnapshot = snapshot || { agents: [], groups: [], links: [], collaborations: [] };
    this.clearObjects();

    const groups = this.currentSnapshot.groups || [];
    const agents = this.currentSnapshot.agents || [];
    const links = this.currentSnapshot.links || [];

    const radius = Math.max(18, groups.length * 3 + 14);

    groups.forEach(function (group, index) {
      const angle = (Math.PI * 2 * index) / Math.max(1, groups.length);
      group._pos = new THREE.Vector3(Math.cos(angle) * radius, 0, Math.sin(angle) * radius);
    });

    const groupGeom = new THREE.CylinderGeometry(0.9, 0.9, 16, 16);
    groups.forEach(function (group) {
      const mat = new THREE.MeshStandardMaterial({
        color: group.runningTaskCount > 0 ? 0x48c5ff : 0x7f91a6,
        transparent: true,
        opacity: 0.86,
        metalness: 0.15,
        roughness: 0.55
      });
      const pillar = new THREE.Mesh(groupGeom, mat);
      pillar.position.copy(group._pos);
      pillar.position.y = 8;
      pillar.userData = { type: 'group', groupId: group.id };
      this.scene.add(pillar);

      const statusMap = group.taskStatusCounts || {};
      const waiting = statusMap[0] || statusMap['0'] || 0;
      const chatting = statusMap[1] || statusMap['1'] || 0;
      const paused = statusMap[2] || statusMap['2'] || 0;
      const text = group.name + '\nR:' + group.runningTaskCount + ' W:' + waiting + ' C:' + chatting + ' P:' + paused;
      const label = textSprite(text, {
        fontSize: 20,
        background: 'rgba(5,14,26,0.90)',
        border: 'rgba(72,197,255,0.65)',
        color: '#DDEFFF'
      });
      label.position.set(group._pos.x, 19, group._pos.z);
      this.scene.add(label);

      this.groupObjects.push({ mesh: pillar, label: label, group: group });
      this.groupById.set(group.id, { mesh: pillar, label: label, group: group });
    }.bind(this));

    const memberships = new Map();
    links.forEach(function (link) {
      if (!memberships.has(link.agentId)) {
        memberships.set(link.agentId, []);
      }
      memberships.get(link.agentId).push(link.groupId);
    });

    const activeGroupIds = new Set((this.currentSnapshot.collaborations || []).map(function (c) { return c.groupId; }));
    const activeAgentIds = new Set();
    (this.currentSnapshot.collaborations || []).forEach(function (col) {
      (col.agentIds || []).forEach(function (id) {
        activeAgentIds.add(id);
      });
    });
    const agentGeom = new THREE.SphereGeometry(1.6, 22, 22);

    agents.forEach(function (agent, index) {
      const memberGroupIds = memberships.get(agent.id) || [];
      let target = null;

      const activeGroupId = memberGroupIds.find(function (groupId) { return activeGroupIds.has(groupId); });
      if (activeGroupId) {
        const groupNode = this.groupById.get(activeGroupId);
        if (groupNode) {
          const h = hashNumber(agent.id);
          const theta = (h % 360) * Math.PI / 180;
          const spread = 4 + (h % 3);
          target = new THREE.Vector3(
            groupNode.mesh.position.x + Math.cos(theta) * spread,
            2 + ((h % 5) * 0.35),
            groupNode.mesh.position.z + Math.sin(theta) * spread
          );
        }
      }

      if (!target && memberGroupIds.length > 0) {
        const groupNode = this.groupById.get(memberGroupIds[0]);
        if (groupNode) {
          const h = hashNumber(agent.id + '-base');
          const theta = (h % 360) * Math.PI / 180;
          const spread = 10 + (h % 6);
          target = new THREE.Vector3(
            groupNode.mesh.position.x + Math.cos(theta) * spread,
            2,
            groupNode.mesh.position.z + Math.sin(theta) * spread
          );
        }
      }

      if (!target) {
        const angle = (Math.PI * 2 * index) / Math.max(1, agents.length);
        target = new THREE.Vector3(Math.cos(angle) * (radius + 20), 2, Math.sin(angle) * (radius + 20));
      }

      const mat = new THREE.MeshStandardMaterial({
        color: agent.enable ? 0x5ed4ff : 0x6e7d90,
        transparent: true,
        opacity: 0.98,
        metalness: 0.08,
        roughness: 0.45
      });
      const sphere = new THREE.Mesh(agentGeom, mat);
      sphere.position.copy(target);
      sphere.userData = { type: 'agent', agentId: agent.id };
      this.scene.add(sphere);

      const promptText = agent.promptCode ? agent.promptCode : '--';
      const scoreText = (typeof agent.score === 'number' && agent.score >= 0) ? agent.score.toFixed(1) : '--';
      const label = textSprite(agent.name + '\n' + promptText + '\nScore:' + scoreText, {
        fontSize: 16,
        background: 'rgba(6, 14, 24, 0.85)',
        border: 'rgba(94,212,255,0.55)',
        color: '#E8F7FF'
      });
      label.position.set(target.x, target.y + 3.7, target.z);
      this.scene.add(label);

      const entry = {
        mesh: sphere,
        label: label,
        agent: agent,
        target: target,
        groupIds: memberGroupIds,
        avatarSprite: null,
        pulseRing: null,
        isActive: activeAgentIds.has(agent.id) || agent.chattingCount > 0,
        pulsePhase: (hashNumber(agent.id) % 100) / 10
      };

      const avatarUrl = agent.avastar || '/images/AgentsManager/avatar/avatar1.png';
      const avatarSprite = this.createAgentAvatarSprite(avatarUrl);
      if (avatarSprite) {
        avatarSprite.position.set(target.x, target.y + 0.02, target.z);
        this.scene.add(avatarSprite);
        entry.avatarSprite = avatarSprite;
      }

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

      this.agentObjects.push(entry);
      this.agentById.set(agent.id, entry);
    }.bind(this));

    (this.currentSnapshot.collaborations || []).forEach(function (col) {
      const members = (col.agentIds || []).map(function (id) { return this.agentById.get(id); }.bind(this)).filter(Boolean);
      if (members.length < 2) {
        return;
      }
      const center = new THREE.Vector3();
      members.forEach(function (m) { center.add(m.target); });
      center.divideScalar(members.length);
      members.forEach(function (m, idx) {
        const angle = (Math.PI * 2 * idx) / members.length;
        m.target = new THREE.Vector3(
          center.x + Math.cos(angle) * 2.2,
          2.5,
          center.z + Math.sin(angle) * 2.2
        );
      });
    }.bind(this));

    links.forEach(function (link) {
      const groupNode = this.groupById.get(link.groupId);
      const agentNode = this.agentById.get(link.agentId);
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
      line.userData = { groupId: link.groupId, agentId: link.agentId };
      this.scene.add(line);
      this.linkObjects.push(line);
    }.bind(this));

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
      positions[1] = groupNode.mesh.position.y + 7.8;
      positions[2] = groupNode.mesh.position.z;
      positions[3] = agentNode.mesh.position.x;
      positions[4] = agentNode.mesh.position.y;
      positions[5] = agentNode.mesh.position.z;
      line.geometry.attributes.position.needsUpdate = true;
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
      this.applyGroupHighlight();
      return;
    }

    const groupId = intersects[0].object.userData.groupId;
    if (this.lockedGroupId === groupId) {
      this.lockedGroupId = null;
    } else {
      this.lockedGroupId = groupId;
    }
    this.applyGroupHighlight();
  };

  AgentGraph3D.prototype.clearGroupFocus = function () {
    if (this.lockedGroupId) {
      this.applyGroupHighlight();
      return;
    }
    this.activeGroupId = null;
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
      if (focused && Array.isArray(focused.memberAgentIds)) {
        focused.memberAgentIds.forEach(function (id) { activeMemberSet.add(id); });
      }
    }

    this.agentObjects.forEach(function (entry) {
      const opacity = !activeGroup || activeMemberSet.has(entry.agent.id) ? 0.98 : 0.08;
      entry.mesh.material.opacity = opacity;
      if (entry.label) {
        entry.label.material.opacity = !activeGroup || activeMemberSet.has(entry.agent.id) ? 1 : 0.12;
      }
    });

    this.groupObjects.forEach(function (entry) {
      const isActive = activeGroup && entry.group.id === activeGroup;
      entry.mesh.material.opacity = !activeGroup ? 0.86 : (isActive ? 1 : 0.2);
      entry.mesh.material.emissive = new THREE.Color(isActive ? 0x2b8fd1 : 0x000000);
      entry.mesh.material.emissiveIntensity = isActive ? 0.35 : 0;
      if (entry.label) {
        entry.label.material.opacity = !activeGroup ? 1 : (isActive ? 1 : 0.3);
      }
    });

    this.linkObjects.forEach(function (line) {
      if (!activeGroup) {
        line.material.opacity = 0.5;
      } else {
        line.material.opacity = line.userData.groupId === activeGroup ? 0.85 : 0.08;
      }
    });
  };

  AgentGraph3D.prototype.createAgentAvatarSprite = function (avatarUrl) {
    if (!this.avatarTextureLoader || !avatarUrl) {
      return null;
    }

    const material = new THREE.SpriteMaterial({
      transparent: true,
      opacity: 0.92,
      depthWrite: false
    });
    const sprite = new THREE.Sprite(material);
    sprite.scale.set(2.35, 2.35, 1);

    const cached = this.avatarTextureCache.get(avatarUrl);
    if (cached) {
      material.map = cached;
      material.needsUpdate = true;
      return sprite;
    }

    this.avatarTextureLoader.load(
      avatarUrl,
      function (texture) {
        this.avatarTextureCache.set(avatarUrl, texture);
        material.map = texture;
        material.needsUpdate = true;
      }.bind(this),
      undefined,
      function () {
        material.opacity = 0;
      }
    );

    return sprite;
  };

  global.AgentGraph3D = AgentGraph3D;
})(window);
