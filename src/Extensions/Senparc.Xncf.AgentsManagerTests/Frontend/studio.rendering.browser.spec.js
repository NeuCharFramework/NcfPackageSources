async page => {
  const errors = [];
  const failedAssets = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('response', response => {
    if (response.status() >= 400 && /\/(js|css)\//.test(response.url()))
      failedAssets.push(response.url());
  });
  const check = (condition, message) => { if (!condition) throw new Error(message); };
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto('about:blank');
  await page.goto('http://127.0.0.1:51961/rendered#tab=first&view=three');
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 3, null, { timeout: 10000 });
  check(await page.locator('.agent-studio').isVisible(), 'Compiled studio partial is not visible');
  check(await page.locator('.agent-3d-canvas canvas').isVisible(), 'Compiled page has no visible WebGL canvas');
  check(await page.locator('partial').count() === 0, 'Razor left a literal partial tag in the page');
  check(await page.evaluate(() => !!THREE.WebGLRenderer && !!THREE.OrbitControls),
    'The existing Three.js and OrbitControls components were not loaded');
  const controls = page.locator('.agent-view-mode-row .el-radio-group');
  await controls.getByText('面板视图', { exact: true }).click();
  await page.waitForFunction(() => app.agentListViewMode === 'panel' && !app.agentGraph3d, null, { timeout: 10000 });
  check(await page.locator('.agent-cardBox').count() === 2, 'Existing agent panel was not preserved');
  await controls.getByText('3D视图', { exact: true }).click();
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 3);
  await controls.getByText('统计图', { exact: true }).click();
  await page.waitForFunction(() => app.agentListViewMode === 'stats' && !app.agentGraph3d);
  check(await page.locator('.agent-statistics-wrap').isVisible(), 'Existing statistics view was not preserved');
  await controls.getByText('3D视图', { exact: true }).click();
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 3);
  await page.reload();
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 3);
  check(await page.locator('.studio-agent-card').count() === 3, 'Studio roster disappeared after reload');
  check(errors.length === 0, 'Page errors: ' + errors.join('; '));
  check(failedAssets.length === 0, 'Failed assets: ' + failedAssets.join('; '));
  return 'PASS: compiled Razor partial, localized/versioned assets, original Vue lifecycle/hash routing, existing Three.js/OrbitControls, panel/3D/statistics switching and reload';
}
