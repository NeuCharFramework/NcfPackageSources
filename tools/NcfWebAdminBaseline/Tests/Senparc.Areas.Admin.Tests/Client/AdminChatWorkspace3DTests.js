'use strict';

const assert = require('assert');
const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '../../../Senparc.Areas.Admin');
const page = fs.readFileSync(path.join(root, 'Areas/Admin/Pages/AdminChat/Chat.cshtml'), 'utf8');
const chat = fs.readFileSync(path.join(root, 'wwwroot/js/Admin/Pages/AdminChat/Chat.js'), 'utf8');
const viewer = fs.readFileSync(path.join(root, 'wwwroot/js/Admin/Pages/AdminChat/Workspace3DViewer.js'), 'utf8');
const controller = fs.readFileSync(path.join(root, 'OHS/Local/Controllers/AdminChatWorkspaceAssetsController.cs'), 'utf8');
const builder = fs.readFileSync(path.join(root, 'Domain/Services/WorkspaceAssetPreviewBuilder.cs'), 'utf8');
const message = fs.readFileSync(path.join(root, 'Domain/Services/AdminChatMessageService.cs'), 'utf8');
const attachment = fs.readFileSync(path.join(root, 'Domain/Models/DatabaseModel/Dto/AdminChatAttachment.cs'), 'utf8');

for (const extension of ['glb', 'gltf', 'obj', 'stl', 'ply']) {
    assert.ok(viewer.includes(`extension === '${extension}'`), `3D viewer must support .${extension}.`);
}
assert.match(chat, /workspacePreviewIs3D\(\)[\s\S]*model\/gltf-binary/);
assert.match(chat, /CreateWorkspaceAssetCapability|createWorkspaceAssetCapability[\s\S]*\/capabilities/);
assert.match(chat, /\/manifest[\s\S]*runtimeManifest/);
assert.match(page, /sandbox="allow-scripts"/);
assert.match(chat, /connect-src \$\{runtimeManifest \? window\.location\.origin/);
assert.match(viewer, /maxTriangles = 2000000/);
assert.match(viewer, /maxBytes = 32 \* 1024 \* 1024/);
assert.match(viewer, /credentials: 'omit'/);
assert.match(viewer, /referrerPolicy: 'no-referrer'/);
assert.match(viewer, /loadMtlMaterials[\s\S]*map_Kd/);
assert.match(viewer, /baseColorTexture[\s\S]*TextureLoader/);
assert.match(controller, /WorkspaceAssetCapabilityStore/);
assert.match(controller, /Access-Control-Allow-Origin"\] = "\*"/);
assert.match(controller, /metadata\.ETag, capability\.ETag/);
assert.match(controller, /directory\.Depth > 4/);
assert.match(controller, /visited > 100/);
assert.match(builder, /IsPreviewAsset/);
assert.match(message, /CollectWorkspaceArtifactsAsync/);
assert.match(attachment, /IsWorkspaceArtifact/);
assert.match(page, /attachment\.isWorkspaceArtifact \|\| attachment\.previewKind === '3d' \? openWorkspaceArtifact/);
assert.match(page, /attachment\.previewKind === '3d'/);
assert.match(page, /workspacePreviewIsDocument \|\| workspacePreviewIs3D/);

console.log('AdminChat Workspace 3D tests passed.');
