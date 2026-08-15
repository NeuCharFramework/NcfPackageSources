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
await loadScript('/js/AgentsManager/agent-3d.js');
await loadScript('/js/AgentsManager/index.js');
