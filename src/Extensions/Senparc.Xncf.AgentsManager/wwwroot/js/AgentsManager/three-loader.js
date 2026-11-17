import * as ThreeModule from '/js/PromptRange/lib/three-esm.min.js';

// Keep the existing AgentGraph3D and OrbitControls scripts compatible while
// loading Three.js through its supported ES Module build.
window.THREE = Object.assign({}, ThreeModule);

const loaderUrl = new URL(import.meta.url, document.baseURI);
const assetVersion = loaderUrl.searchParams.get('v');

function versionedAssetUrl(src) {
  const url = new URL(src, document.baseURI);
  if (assetVersion) {
    url.searchParams.set('v', assetVersion);
  }
  return url.toString();
}

function loadScript(src) {
  return new Promise((resolve, reject) => {
    const script = document.createElement('script');
    script.src = versionedAssetUrl(src);
    script.onload = resolve;
    script.onerror = () => reject(new Error('Failed to load ' + src));
    document.head.appendChild(script);
  });
}

await loadScript('/js/PromptRange/lib/OrbitControls.js');
await loadScript('/js/AgentsManager/axios.js');
await loadScript('/js/AgentsManager/studio-state.js');
await loadScript('/js/AgentsManager/agent-3d.js');
//Legacy comment translated to English.
//Legacy comment translated to English.
await loadScript('/js/AgentsManager/agents-app-utils.js');
await loadScript('/js/AgentsManager/agents-app-data.js');
await loadScript('/js/AgentsManager/agents-app-computed.js');
await loadScript('/js/AgentsManager/agents-app-methods-core.js');
await loadScript('/js/AgentsManager/agents-app-methods-remote-agent.js');
await loadScript('/js/AgentsManager/agents-app-methods-agent-group.js');
await loadScript('/js/AgentsManager/agents-app-methods-task.js');
await loadScript('/js/AgentsManager/agents-app-methods-groupstart-editor.js');
await loadScript('/js/AgentsManager/agents-app-methods-manage.js');
await loadScript('/js/AgentsManager/agents-app-methods-plugin-mcp.js');
await loadScript('/js/AgentsManager/agents-app-components.js');
await loadScript('/js/AgentsManager/studio.js');
await loadScript('/js/AgentsManager/index.js');
