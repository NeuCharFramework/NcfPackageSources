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
        entry.statusBadge.rotation.y += moving ? 0.025 : 0.008;
      }
    });

    this.groupObjects.forEach(function (entry) {
      if (!entry.statusRing) {
        return;
      }
      const elapsed = Date.now() * 0.002 + entry.pulsePhase;
      const isHIL = entry.statusKind === 'hil-paused';
      const scale = isHIL ? 1 + ((Math.sin(elapsed) + 1) * 0.14) : 1;
      entry.statusRing.scale.set(scale, scale, scale);
      entry.statusRing.material.opacity = isHIL
        ? 0.38 + ((Math.sin(elapsed * 1.4) + 1) * 0.22)
        : 0.48;
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
      const isEnabled = group.enable !== false;
      const status = groupStatusInfo(group);
      const style = groupStatusStyle(status.kind);
      const waiting = status.waiting;
      const chatting = status.chatting;
      const paused = status.paused;
      const finished = status.finished;
      const cancelled = status.cancelled;
      const failed = status.failed;
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
      const heightScale = 0.72 + Math.min(1.45, totalTasks * 0.08 + group.runningTaskCount * 0.16);
      pillar.scale.y = heightScale;
      pillar.position.copy(group._pos);
      pillar.position.y = 8 * heightScale;
      pillar.userData = { type: 'group', groupId: group.id };
      this.scene.add(pillar);

      const enableText = isEnabled ? '已启用' : '已停用';
      const text = group.name
        + '\n状态:' + enableText
        + '\n任务:' + totalTasks + ' 运行:' + group.runningTaskCount
        + '\n等待:' + waiting + ' 聊天:' + chatting + ' 暂停:' + paused
        + '\n完成:' + finished + ' 取消:' + cancelled + ' 失败:' + failed
        + (status.humanPending > 0 ? '\nHIL等待:' + status.humanPending : '');
      const label = textSprite(text, {
        fontSize: 18,
        padding: 16,
        scaleDivisor: 20,
        maxLineLength: 24,
        maxLines: 6,
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
      label.position.set(group._pos.x, 22 + heightScale, group._pos.z);
      this.scene.add(label);

      let statusRing = null;
      if (status.kind === 'hil-paused' || status.kind === 'paused') {
        const ringGeometry = new THREE.TorusGeometry(1.55, 0.16, 10, 32);
        const ringMaterial = new THREE.MeshBasicMaterial({
          color: style.ring,
          transparent: true,
          opacity: 0.48,
          side: THREE.DoubleSide,
          depthWrite: false
        });
        statusRing = new THREE.Mesh(ringGeometry, ringMaterial);
        statusRing.rotation.x = -Math.PI / 2;
        statusRing.position.set(group._pos.x, 16 * heightScale + 0.35, group._pos.z);
        this.scene.add(statusRing);
      }

      const groupEntry = {
        mesh: pillar,
        label: label,
        group: group,
        statusRing: statusRing,
        statusKind: status.kind,
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
          const h = hashNumber(agentKey + '-' + primaryGroupId);
          const theta = (Math.PI * 2 * slot / ringCapacity)
            - Math.PI / 2
            + (((h % 17) - 8) * Math.PI / 180);
          const spread = 11 + ringIndex * 5.5 + (activeGroupId ? 1.5 : 0);
          target = new THREE.Vector3(
            groupNode.mesh.position.x + Math.cos(theta) * spread,
            2.5 + ringIndex * 1.15,
            groupNode.mesh.position.z + Math.sin(theta) * spread
          );
          labelOffset = {
            x: Math.cos(theta) * 4.3,
            y: 2.2 + ringIndex * 0.35,
            z: Math.sin(theta) * 4.3
          };
        }
      }

      if (!target) {
        const ringCapacity = 10;
        const ringIndex = Math.floor(index / ringCapacity);
        const slot = index % ringCapacity;
        const angle = (Math.PI * 2 * slot / ringCapacity) - Math.PI / 2;
        const spread = radius + 22 + ringIndex * 5.5;
        target = new THREE.Vector3(
          Math.cos(angle) * spread,
          2.5 + ringIndex * 1.15,
          Math.sin(angle) * spread);
        labelOffset = {
          x: Math.cos(angle) * 4.3,
          y: 2.2 + ringIndex * 0.35,
          z: Math.sin(angle) * 4.3
        };
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
      sphere.position.copy(target);
      sphere.userData = { type: 'agent', agentId: agentKey };
      this.decorateCuteAgent(sphere, agent.enable);
      this.decorateAgentSkills(sphere, agent.skillKinds);
      this.scene.add(sphere);

      const stateText = !agent.enable
        ? '已停用'
        : isHILPaused
          ? 'HIL等待'
          : isPaused
            ? '暂停'
            : isActive ? '运行中' : '已启用';
      const label = textSprite(
        agent.name
        + '\n' + (agent.agentKind === 'RemoteA2A' ? '远程 A2A' : '本地 Agent')
        + ' · ' + stateText
        + '\n技能:' + skillDisplayText(agent),
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
      label.material.opacity = isActive || isPaused ? 0.8 : 0.42;
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
      const opacity = !activeGroup || activeMemberSet.has(agentKey) ? 0.98 : 0.08;
      entry.mesh.material.opacity = opacity;
      if (entry.label) {
        const baseOpacity = !activeGroup || activeMemberSet.has(agentKey) ? 0.42 : 0.1;
        entry.label.material.opacity = (isHovered || isGroupFocused) ? 1 : baseOpacity;
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
