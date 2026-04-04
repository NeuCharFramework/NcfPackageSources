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
    const scaleDivisor = options.scaleDivisor || 26;
    const sizeAttenuation = typeof options.sizeAttenuation === 'boolean' ? options.sizeAttenuation : true;
    const maxLineLength = options.maxLineLength || 30;
    const maxWorldWidth = options.maxWorldWidth || 15;
    const maxWorldHeight = options.maxWorldHeight || 9;
    const bg = options.background || 'rgba(10,20,30,0.82)';
    const color = options.color || '#EAF2FF';
    const border = options.border || 'rgba(86, 162, 255, 0.55)';

    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    ctx.font = 'bold ' + fontSize + 'px sans-serif';

    const rows = String(text || '').split('\n').map(function (line) {
      const lineText = String(line || '');
      if (lineText.length <= maxLineLength) {
        return lineText;
      }
      return lineText.slice(0, maxLineLength - 1) + '…';
    });
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
    texture.minFilter = THREE.LinearFilter;
    texture.magFilter = THREE.LinearFilter;
    const material = new THREE.SpriteMaterial({
      map: texture,
      transparent: true,
      depthWrite: false,
      sizeAttenuation: sizeAttenuation
    });
    const sprite = new THREE.Sprite(material);
    const worldWidth = Math.min(canvas.width / dpr / scaleDivisor, maxWorldWidth);
    const worldHeight = Math.min(canvas.height / dpr / scaleDivisor, maxWorldHeight);
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

      entry.baseY += (target.y - entry.baseY) * 0.15;
      const movingDistance = Math.hypot(target.x - entry.mesh.position.x, target.z - entry.mesh.position.z);
      const moving = movingDistance > 0.03;

      entry.mesh.position.x += (target.x - entry.mesh.position.x) * 0.09;
      entry.mesh.position.z += (target.z - entry.mesh.position.z) * 0.09;

      entry.motionPhase += moving ? 0.36 : 0.08;
      const hop = moving ? Math.abs(Math.sin(entry.motionPhase)) * 0.75 : 0;
      entry.mesh.position.y = entry.baseY + hop;

      const jelly = Math.sin(entry.motionPhase);
      const stretchY = moving ? (1 + jelly * 0.18) : (1 + jelly * 0.05);
      const squashXZ = moving ? (1 - jelly * 0.1) : (1 - jelly * 0.03);
      entry.mesh.scale.set(squashXZ, stretchY, squashXZ);

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
    const all = [];

    this.groupObjects.forEach(function (g) {
      if (g.mesh) {
        all.push(g.mesh);
      }
      if (g.label) {
        all.push(g.label);
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

    all.forEach(function (obj) {
      if (obj && obj.parent) {
        obj.parent.remove(obj);
      }
      if (obj && typeof obj.traverse === 'function') {
        obj.traverse(function (node) {
          if (node.material) {
            if (Array.isArray(node.material)) {
              node.material.forEach(function (mat) {
                if (mat.map) {
                  mat.map.dispose();
                }
                mat.dispose();
              });
            } else {
              if (node.material.map) {
                node.material.map.dispose();
              }
              node.material.dispose();
            }
          }
          if (node.geometry) {
            node.geometry.dispose();
          }
        });
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
      const finished = statusMap[3] || statusMap['3'] || 0;
      const cancelled = statusMap[4] || statusMap['4'] || 0;
      const failed = statusMap[5] || statusMap['5'] || 0;
      const totalTasks = waiting + chatting + paused + finished + cancelled + failed;
      const text = group.name
        + '\nTasks:' + totalTasks + ' Running:' + group.runningTaskCount
        + '\nW:' + waiting + ' C:' + chatting + ' P:' + paused + ' F:' + finished;
      const label = textSprite(text, {
        fontSize: 24,
        padding: 16,
        scaleDivisor: 18,
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
    const activeLinkKeySet = new Set();
    (this.currentSnapshot.collaborations || []).forEach(function (col) {
      (col.agentIds || []).forEach(function (id) {
        activeAgentIds.add(id);
        activeLinkKeySet.add(col.groupId + '-' + id);
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
      this.decorateCuteAgent(sphere, agent.enable);
      this.scene.add(sphere);

      const promptText = agent.promptCode ? agent.promptCode : '--';
      const scoreText = (typeof agent.score === 'number' && agent.score >= 0) ? agent.score.toFixed(1) : '--';
      const stateText = agent.enable ? 'Enabled' : 'Disabled';
      const label = textSprite(
        agent.name
        + '\nPrompt:' + promptText
        + '\nScore:' + scoreText + '  Running:' + (agent.chattingCount || 0)
        + '\nState:' + stateText,
        {
        fontSize: 20,
        padding: 14,
        scaleDivisor: 18,
        background: 'rgba(6, 14, 24, 0.85)',
        border: 'rgba(94,212,255,0.55)',
        color: '#E8F7FF'
      });
      label.position.set(target.x, target.y + 3.7, target.z);
      label.userData = { type: 'agent-label', agentId: agent.id };
      label.material.opacity = 0.42;
      this.scene.add(label);

      const entry = {
        mesh: sphere,
        label: label,
        agent: agent,
        target: target,
        groupIds: memberGroupIds,
        pulseRing: null,
        isActive: activeAgentIds.has(agent.id) || agent.chattingCount > 0,
        pulsePhase: (hashNumber(agent.id) % 100) / 10,
        motionPhase: (hashNumber(agent.id + '-motion') % 100) / 16,
        baseY: target.y
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
      const activeKey = link.groupId + '-' + link.agentId;
      line.userData = {
        groupId: link.groupId,
        agentId: link.agentId,
        isActive: activeLinkKeySet.has(activeKey),
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
      positions[4] = agentNode.mesh.position.y + 0.4;
      positions[5] = agentNode.mesh.position.z;
      line.geometry.attributes.position.needsUpdate = true;

      if (line.userData.isActive) {
        const elapsed = Date.now() * 0.0018 + line.userData.phase;
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
    this.activeAgentId = null;
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
      const isHovered = this.activeAgentId && entry.agent.id === this.activeAgentId;
      const opacity = !activeGroup || activeMemberSet.has(entry.agent.id) ? 0.98 : 0.08;
      entry.mesh.material.opacity = opacity;
      if (entry.label) {
        const baseOpacity = !activeGroup || activeMemberSet.has(entry.agent.id) ? 0.42 : 0.1;
        entry.label.material.opacity = isHovered ? 1 : baseOpacity;
      }
    }.bind(this));

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
        if (!line.userData.isActive) {
          line.material.opacity = 0.5;
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

  global.AgentGraph3D = AgentGraph3D;
})(window);
