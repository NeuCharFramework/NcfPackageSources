(function () {
    'use strict';

    const manifestBytes = Uint8Array.from(atob(document.documentElement.dataset.manifest), character => character.charCodeAt(0));
    const manifest = JSON.parse(new TextDecoder().decode(manifestBytes));
    const status = document.getElementById('status');
    const canvasHost = document.getElementById('viewport');
    const maxTriangles = 2000000;
    const maxBytes = 32 * 1024 * 1024;

    function fail(error) {
        console.error('[Workspace3D]', error);
        status.textContent = manifest.loadError + (error && error.message ? ': ' + error.message : '');
        status.classList.add('error');
    }

    async function readArrayBuffer(url) {
        const response = await fetch(url, { credentials: 'omit', referrerPolicy: 'no-referrer' });
        if (!response.ok) throw new Error('Unable to load model dependency (' + response.status + ').');
        if (Number(response.headers.get('content-length')) > maxBytes) throw new Error('The 3D resource exceeds 32 MB.');
        const buffer = await response.arrayBuffer();
        if (buffer.byteLength > maxBytes) throw new Error('The 3D resource exceeds 32 MB.');
        return buffer;
    }

    async function readText(url) {
        const buffer = await readArrayBuffer(url);
        return new TextDecoder().decode(buffer);
    }

    function route(path) {
        return manifest.dependencies[path] || manifest.url;
    }

    function normalizeRelative(path) {
        const root = manifest.path.includes('/') ? manifest.path.slice(0, manifest.path.lastIndexOf('/') + 1) : '';
        const parts = (root + path).replace(/\\/g, '/').split('/');
        const result = [];
        for (const part of parts) {
            if (!part || part === '.') continue;
            if (part === '..') { if (!result.length) throw new Error('Invalid model dependency path.'); result.pop(); }
            else result.push(part);
        }
        return result.join('/');
    }

    function geometryFrom(values, indices, normals) {
        const geometry = new THREE.BufferGeometry();
        geometry.setAttribute('position', new THREE.Float32BufferAttribute(values, 3));
        if (indices && indices.length) geometry.setIndex(indices);
        if (normals && normals.length === values.length) geometry.setAttribute('normal', new THREE.Float32BufferAttribute(normals, 3));
        else geometry.computeVertexNormals();
        geometry.computeBoundingSphere();
        return geometry;
    }

    function addObject(scene, geometry, color, texture) {
        const material = new THREE.MeshStandardMaterial({
            color: color || 0xffffff,
            map: texture || null,
            metalness: 0.08,
            roughness: 0.72
        });
        const mesh = new THREE.Mesh(geometry, material);
        scene.add(mesh);
        return mesh;
    }

    function componentArray(type, offset, length, buffer) {
        const Types = { 5121: Uint8Array, 5120: Int8Array, 5123: Uint16Array, 5122: Int16Array, 5125: Uint32Array, 5126: Float32Array };
        const ArrayType = Types[type];
        if (!ArrayType) throw new Error('Unsupported glTF component type.');
        return Array.from(new ArrayType(buffer, offset, length));
    }

    const typeSizes = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4, MAT2: 4, MAT3: 9, MAT4: 16 };

    async function loadGltfContainer(object) {
        if (typeof object !== 'object' || !Array.isArray(object.buffers) || !Array.isArray(object.meshes)) {
            throw new Error('Invalid or unsupported glTF document.');
        }
        const buffers = await Promise.all(object.buffers.map(async buffer => {
            if (buffer.uri && buffer.uri.startsWith('data:')) {
                return (await (await fetch(buffer.uri)).arrayBuffer());
            }
            const uri = buffer.uri ? normalizeRelative(buffer.uri) : '';
            if (!uri || !manifest.dependencies[uri]) throw new Error('The glTF buffer dependency is not authorized.');
            return readArrayBuffer(manifest.dependencies[uri]);
        }));
        const group = new THREE.Group();
        let triangles = 0;
        for (const mesh of object.meshes) {
            for (const primitive of mesh.primitives || []) {
                const position = primitive.attributes && primitive.attributes.POSITION;
                const normal = primitive.attributes && primitive.attributes.NORMAL;
                if (position == null) return;
                const accessor = object.accessors[position];
                const normalAccessor = normal == null ? null : object.accessors[normal];
                const positionView = object.bufferViews[accessor.bufferView];
                const positionValues = componentArray(accessor.componentType,
                    (positionView.byteOffset || 0) + (accessor.byteOffset || 0),
                    accessor.count * typeSizes[accessor.type],
                    buffers[positionView.buffer]);
                let normalValues = null;
                if (normalAccessor) {
                    const normalView = object.bufferViews[normalAccessor.bufferView];
                    normalValues = componentArray(normalAccessor.componentType,
                        (normalView.byteOffset || 0) + (normalAccessor.byteOffset || 0),
                        normalAccessor.count * typeSizes[normalAccessor.type],
                        buffers[normalView.buffer]);
                }
                let indices = null;
                if (primitive.indices != null) {
                    const indexAccessor = object.accessors[primitive.indices];
                    const indexView = object.bufferViews[indexAccessor.bufferView];
                    indices = componentArray(indexAccessor.componentType,
                        (indexView.byteOffset || 0) + (indexAccessor.byteOffset || 0),
                        indexAccessor.count, buffers[indexView.buffer]);
                    triangles += Math.floor(indices.length / 3);
                } else {
                    triangles += Math.floor(positionValues.length / 9);
                }
                if (triangles > maxTriangles) throw new Error('The 3D model exceeds the 2,000,000 triangle limit.');
                let color = 0x8fb8e8;
                const material = object.materials && object.materials[primitive.material];
                const base = material && material.pbrMetallicRoughness
                    && material.pbrMetallicRoughness.baseColorFactor;
                if (base && base.length >= 3) color = new THREE.Color(base[0], base[1], base[2]);
                let texture = null;
                const textureReference = material && material.pbrMetallicRoughness
                    && material.pbrMetallicRoughness.baseColorTexture;
                const textureInfo = textureReference && object.textures
                    && object.textures[textureReference.index];
                const image = textureInfo && object.images && object.images[textureInfo.source];
                if (image && image.uri) {
                    const imagePath = normalizeRelative(image.uri);
                    if (manifest.dependencies[imagePath]) {
                        const loader = new THREE.TextureLoader();
                        loader.setCrossOrigin('anonymous');
                        texture = loader.load(manifest.dependencies[imagePath]);
                    }
                }
                addObject(group, geometryFrom(positionValues, indices, normalValues), color, texture);
            }
        }
        return group;
    }

    async function loadGlb(buffer) {
        const view = new DataView(buffer);
        if (view.getUint32(0, true) !== 0x46546c67 || view.getUint32(4, true) !== 2) {
            throw new Error('Invalid GLB 2.0 file.');
        }
        let offset = 12;
        const chunks = [];
        while (offset + 8 <= view.byteLength) {
            const length = view.getUint32(offset, true);
            const type = view.getUint32(offset + 4, true);
            chunks.push({ type, data: buffer.slice(offset + 8, offset + 8 + length) });
            offset += 8 + length;
        }
        const jsonChunk = chunks.find(chunk => chunk.type === 0x4e4f534a);
        if (!jsonChunk) throw new Error('GLB JSON chunk is missing.');
        const json = JSON.parse(new TextDecoder().decode(jsonChunk.data));
        if (chunks.some(chunk => chunk.type === 0x004e4942)) {
            json.buffers = json.buffers || [{}];
            json.buffers[0].uri = undefined;
            const binary = chunks.find(chunk => chunk.type === 0x004e4942).data;
            return loadGltfContainerWithBuffers(json, [binary]);
        }
        return loadGltfContainer(json);
    }

    async function loadGltfContainerWithBuffers(object, forcedBuffers) {
        const originalBuffers = object.buffers;
        const mapped = await Promise.all(object.buffers.map(async (buffer, index) => forcedBuffers[index]));
        const fetchBuffers = async () => mapped;
        return loadGltfGeometry(object, fetchBuffers);
    }

    async function loadGltfGeometry(object, fetchBuffers) {
        const buffers = await fetchBuffers();
        const group = new THREE.Group();
        let triangles = 0;
        for (const mesh of object.meshes || []) {
            for (const primitive of mesh.primitives || []) {
                const accessor = object.accessors && object.accessors[primitive.attributes.POSITION];
                if (!accessor) continue;
                const positionView = object.bufferViews[accessor.bufferView];
                const positions = componentArray(accessor.componentType,
                    (positionView.byteOffset || 0) + (accessor.byteOffset || 0),
                    accessor.count * typeSizes[accessor.type], buffers[positionView.buffer]);
                const normalAccessor = primitive.attributes.NORMAL == null ? null
                    : object.accessors[primitive.attributes.NORMAL];
                let normals = null;
                if (normalAccessor) {
                    const normalView = object.bufferViews[normalAccessor.bufferView];
                    normals = componentArray(normalAccessor.componentType,
                        (normalView.byteOffset || 0) + (normalAccessor.byteOffset || 0),
                        normalAccessor.count * typeSizes[normalAccessor.type], buffers[normalView.buffer]);
                }
                let indices = null;
                if (primitive.indices != null) {
                    const indexAccessor = object.accessors[primitive.indices];
                    const indexView = object.bufferViews[indexAccessor.bufferView];
                    indices = componentArray(indexAccessor.componentType,
                        (indexView.byteOffset || 0) + (indexAccessor.byteOffset || 0),
                        indexAccessor.count, buffers[indexView.buffer]);
                }
                triangles += Math.floor((indices ? indices.length : positions.length / 3) / 3);
                if (triangles > maxTriangles) throw new Error('The 3D model exceeds the 2,000,000 triangle limit.');
                addObject(group, geometryFrom(positions, indices, normals));
            }
        }
        return group;
    }

    function parseObj(text, materials) {
        const positions = [], normals = [], faces = [];
        const sourceNormals = [];
        let materialName = '';
        for (const line of text.split(/\r?\n/)) {
            const parts = line.trim().split(/\s+/);
            if (parts[0] === 'v' && parts.length >= 4) positions.push(parts.slice(1, 4).map(Number));
            if (parts[0] === 'vn' && parts.length >= 4) sourceNormals.push(parts.slice(1, 4).map(Number));
            if (parts[0] === 'usemtl') materialName = parts.slice(1).join(' ');
            if (parts[0] === 'f') {
                const vertices = parts.slice(1).map(item => item.split('/').map(value => value ? Number(value) - 1 : null));
                for (let index = 1; index < vertices.length - 1; index += 1) {
                    faces.push({ vertices: [vertices[0], vertices[index], vertices[index + 1]], materialName });
                }
            }
        }
        if (faces.length > maxTriangles) throw new Error('The OBJ exceeds the 2,000,000 triangle limit.');
        const group = new THREE.Group();
        const materialGroups = new Map();
        faces.forEach(face => {
            const current = materialGroups.get(face.materialName) || { values: [], normalValues: [] };
            face.vertices.forEach(vertex => {
                current.values.push.apply(current.values, positions[vertex[0]] || [0, 0, 0]);
                current.normalValues.push.apply(current.normalValues,
                    (vertex[2] == null ? sourceNormals[0] : sourceNormals[vertex[2]]) || [0, 0, 1]);
            });
            materialGroups.set(face.materialName, current);
        });
        materialGroups.forEach((values, name) => {
            const definition = materials.get(name) || {};
            addObject(group, geometryFrom(values.values, null, values.normalValues),
                definition.color || 0x8fb8e8, definition.texture);
        });
        return group;
    }

    async function loadMtlMaterials(text, mtlPath) {
        const materials = new Map();
        let currentName = '';
        for (const line of text.split(/\r?\n/)) {
            const parts = line.trim().split(/\s+/);
            if (parts[0] === 'newmtl') {
                currentName = parts.slice(1).join(' ');
                materials.set(currentName, { color: 0x8fb8e8, texture: null });
            } else if (parts[0] === 'Kd' && currentName) {
                materials.get(currentName).color = new THREE.Color(Number(parts[1]), Number(parts[2]), Number(parts[3]));
            } else if (/^map_Kd$/i.test(parts[0]) && currentName) {
                const texturePath = normalizeRelativeFrom(mtlPath, parts.slice(1).join(' '));
                if (manifest.dependencies[texturePath]) {
                    const loader = new THREE.TextureLoader();
                    loader.setCrossOrigin('anonymous');
                    materials.get(currentName).texture = loader.load(manifest.dependencies[texturePath]);
                }
            }
        }
        return materials;
    }

    function normalizeRelativeFrom(sourcePath, reference) {
        const base = sourcePath.includes('/') ? sourcePath.slice(0, sourcePath.lastIndexOf('/') + 1) : '';
        const parts = (base + reference.replace(/\\/g, '/')).split('/');
        const normalized = [];
        for (const part of parts) {
            if (!part || part === '.') continue;
            if (part === '..') {
                if (!normalized.length) throw new Error('Invalid model dependency path.');
                normalized.pop();
            }
            else normalized.push(part);
        }
        return normalized.join('/');
    }

    async function loadObj(text) {
        const materials = new Map();
        const libraries = Array.from(text.matchAll(/^\s*mtllib\s+(.+?)\s*$/gmi));
        for (const match of libraries) {
            const path = normalizeRelative(match[1]);
            const url = manifest.dependencies[path];
            if (!url) throw new Error('The OBJ material dependency is not authorized.');
            const library = await loadMtlMaterials(await readText(url), path);
            library.forEach((value, key) => materials.set(key, value));
        }
        return parseObj(text, materials);
    }

    function parseStl(buffer) {
        const text = new TextDecoder().decode(buffer.slice(0, Math.min(128, buffer.byteLength)));
        if (/^solid/i.test(text) && !/facet normal/i.test(text)) text.slice(0);
        if (/facet normal/i.test(text)) {
            const full = new TextDecoder().decode(buffer);
            const positions = [], normals = [];
            const trianglePattern = /facet normal\s+([-\d.e+]+)\s+([-\d.e+]+)\s+([-\d.e+]+)\s+outer loop([\s\S]*?)endloop/gi;
            for (const match of full.matchAll(trianglePattern)) {
                const normal = match.slice(1, 4).map(Number);
                const vertices = match[4].match(/vertex\s+([-\d.e+]+)\s+([-\d.e+]+)\s+([-\d.e+]+)/gi) || [];
                vertices.forEach(vertex => {
                    positions.push.apply(positions, vertex.replace(/vertex/i, '').trim().split(/\s+/).map(Number));
                    normals.push.apply(normals, normal);
                });
            }
            const group = new THREE.Group();
            addObject(group, geometryFrom(positions, null, normals));
            return group;
        }
        if (buffer.byteLength < 84) throw new Error('Invalid STL file.');
        const view = new DataView(buffer);
        const count = view.getUint32(80, true);
        if (count > maxTriangles || 84 + count * 50 > buffer.byteLength) throw new Error('Invalid or oversized binary STL.');
        const positions = [], normals = [];
        let offset = 84;
        for (let index = 0; index < count; index += 1) {
            const normal = [view.getFloat32(offset, true), view.getFloat32(offset + 4, true), view.getFloat32(offset + 8, true)];
            offset += 12;
            for (let vertex = 0; vertex < 3; vertex += 1) {
                positions.push(view.getFloat32(offset, true), view.getFloat32(offset + 4, true), view.getFloat32(offset + 8, true));
                normals.push.apply(normals, normal);
                offset += 12;
            }
            offset += 2;
        }
        const group = new THREE.Group();
        addObject(group, geometryFrom(positions, null, normals));
        return group;
    }

    function parsePly(text) {
        const lines = text.split(/\r?\n/);
        if (lines[0] !== 'ply') throw new Error('Invalid ASCII PLY file.');
        let format = '', vertices = 0, faces = 0, body = 0, vertexProperties = [];
        for (let index = 1; index < lines.length; index += 1) {
            const parts = lines[index].trim().split(/\s+/);
            if (parts[0] === 'format') format = parts[1];
            if (parts[0] === 'element' && parts[1] === 'vertex') vertices = Number(parts[2]);
            if (parts[0] === 'element' && parts[1] === 'face') faces = Number(parts[2]);
            if (parts[0] === 'property' && vertices && !faces) vertexProperties.push(parts[parts.length - 1]);
            if (parts[0] === 'end_header') { body = index + 1; break; }
        }
        if (format !== 'ascii 1.0') throw new Error('Only ASCII PLY is currently supported.');
        if (faces > maxTriangles || vertices > maxTriangles * 3) throw new Error('The PLY exceeds the preview limit.');
        const points = [];
        for (let index = 0; index < vertices; index += 1) {
            const row = lines[body + index].trim().split(/\s+/).map(Number);
            points.push(row.slice(0, 3));
        }
        const values = [];
        for (let index = 0; index < faces; index += 1) {
            const row = lines[body + vertices + index].trim().split(/\s+/).map(Number);
            const count = row[0], list = row.slice(1, 1 + count);
            for (let part = 1; part < list.length - 1; part += 1) {
                values.push.apply(values, points[list[0]]);
                values.push.apply(values, points[list[part]]);
                values.push.apply(values, points[list[part + 1]]);
            }
        }
        const group = new THREE.Group();
        addObject(group, geometryFrom(values));
        return group;
    }

    function fitCamera(object, camera, controls) {
        const box = new THREE.Box3().setFromObject(object);
        const size = box.getSize(new THREE.Vector3());
        const center = box.getCenter(new THREE.Vector3());
        const radius = Math.max(size.x, size.y, size.z, 0.01) * 0.65;
        camera.position.copy(center).add(new THREE.Vector3(radius * 1.8, radius * 1.2, radius * 1.8));
        camera.near = Math.max(radius / 1000, 0.001);
        camera.far = Math.max(radius * 100, 100);
        camera.updateProjectionMatrix();
        controls.target.copy(center);
        controls.update();
    }

    async function start() {
        status.textContent = manifest.loading || '';
        if (!window.THREE) throw new Error('The local Three.js runtime is unavailable.');
        const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
        renderer.setPixelRatio(Math.min(2, window.devicePixelRatio || 1));
        renderer.setClearColor(0xf5f7fa, 1);
        canvasHost.appendChild(renderer.domElement);
        const scene = new THREE.Scene();
        const camera = new THREE.PerspectiveCamera(45, 1, 0.01, 1000);
        const controls = new THREE.OrbitControls(camera, renderer.domElement);
        controls.enableDamping = true;
        scene.add(new THREE.HemisphereLight(0xffffff, 0x43536a, 1.8));
        const light = new THREE.DirectionalLight(0xffffff, 2.2);
        light.position.set(4, 6, 3);
        scene.add(light);
        let object;
        const extension = manifest.path.toLowerCase().split('.').pop();
        if (extension === 'gltf') object = await loadGltfContainer(JSON.parse(await readText(manifest.url)));
        else if (extension === 'glb') object = await loadGlb(await readArrayBuffer(manifest.url));
        else if (extension === 'obj') object = await loadObj(await readText(manifest.url));
        else if (extension === 'stl') object = parseStl(await readArrayBuffer(manifest.url));
        else if (extension === 'ply') object = parsePly(await readText(manifest.url));
        else throw new Error('Unsupported 3D format.');
        scene.add(object);
        fitCamera(object, camera, controls);
        const resize = () => {
            const width = Math.max(canvasHost.clientWidth, 1);
            const height = Math.max(canvasHost.clientHeight, 1);
            renderer.setSize(width, height, false);
            camera.aspect = width / height;
            camera.updateProjectionMatrix();
        };
        new ResizeObserver(resize).observe(canvasHost);
        resize();
        let running = true;
        const render = () => {
            if (!running) return;
            controls.update();
            renderer.render(scene, camera);
            requestAnimationFrame(render);
        };
        render();
        window.addEventListener('pagehide', () => { running = false; renderer.dispose(); }, { once: true });
        status.textContent = '';
    }

    start().catch(fail);
})();
