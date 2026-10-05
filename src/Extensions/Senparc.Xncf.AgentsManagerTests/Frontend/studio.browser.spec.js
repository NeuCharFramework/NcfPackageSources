async page => {
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  const check = (condition, message) => { if (!condition) throw new Error(message); };
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto('http://127.0.0.1:51961/');
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 3);
  check(await page.locator('.agent-3d-canvas canvas').count() === 1, 'WebGL canvas missing');
  check(await page.locator('.studio-agent-card').count() === 3, 'Roster missing');
  check(await page.evaluate(() => {
    const scene = app.agentGraph3d;
    const bounds = scene.roomBounds;
    return [-1, 1].every(x => [-0.55, 16].every(y => [-1, 1].every(z => {
      const point = new THREE.Vector3(x * bounds.width / 2, y, bounds.centerZ + z * bounds.depth / 2).project(scene.camera);
      return Math.abs(point.x) < 0.95 && Math.abs(point.y) < 0.95;
    })));
  }), 'Overview camera clips the studio bounds');

  const position = async (type, id) => page.evaluate(({ type, id }) => {
    const scene = app.agentGraph3d;
    const entry = type === 'agent' ? scene.agentById.get(id) : scene.groupById.get(id);
    const point = entry.mesh.position.clone().project(scene.camera);
    const rect = scene.renderer.domElement.getBoundingClientRect();
    return { x: rect.left + (point.x + 1) * rect.width / 2,
      y: rect.top + (1 - point.y) * rect.height / 2, left: rect.left, top: rect.top };
  }, { type, id });

  const camera = await page.evaluate(() => app.agentGraph3d.camera.position.toArray());
  const source = await position('agent', 'local:2');
  const target = await position('group', 1);
  await page.mouse.move(source.x, source.y);
  await page.mouse.down();
  await page.mouse.move(target.x, target.y, { steps: 12 });
  await page.waitForTimeout(80);
  check(await page.evaluate(() => {
    const scene = app.agentGraph3d;
    const point = new THREE.Vector3();
    scene.raycaster.ray.intersectPlane(new THREE.Plane(new THREE.Vector3(0, 1, 0), -2.1), point);
    return scene.pointer?.dragging && scene.agentById.get('local:2').mesh.position.distanceTo(point) < 0.01;
  }), 'Dragged agent drifted away from the pointer during animation');
  await page.mouse.up();
  await page.waitForFunction(() => app.agentGraphSnapshot.links.some(link =>
    link.groupId === 1 && link.participantKey === 'local:2'));
  const cameraAfter = await page.evaluate(() => app.agentGraph3d.camera.position.toArray());
  check(JSON.stringify(camera) === JSON.stringify(cameraAfter), 'Agent drag incorrectly orbited camera');

  const remote = page.locator('.studio-agent-card').filter({ hasText: 'Remote Research' });
  const canvas = page.locator('.agent-3d-canvas');
  const drop = await position('group', 1);
  await remote.dragTo(canvas, { targetPosition: { x: drop.x - drop.left, y: drop.y - drop.top } });
  await page.waitForFunction(() => app.agentGraphSnapshot.links.some(link =>
    link.groupId === 1 && link.participantKey === 'remote:1'));
  check(await page.evaluate(() => app.agentGraphSnapshot.links.filter(link => link.groupId === 1).length) === 3,
    'Remote/local participant identities collided');

  const fixture = async () => page.evaluate(() => fetch('/fixture-state').then(response => response.json()));
  const beforeDuplicate = (await fixture()).writes.filter(write => write.method === 'SetStudioParticipant').length;
  await remote.dragTo(canvas, { targetPosition: { x: drop.x - drop.left, y: drop.y - drop.top } });
  check((await fixture()).writes.filter(write => write.method === 'SetStudioParticipant').length === beforeDuplicate,
    'Duplicate drop wrote membership again');

  const traySource = await position('agent', 'local:2');
  const tray = await page.locator('.studio-team-dock').boundingBox();
  await page.mouse.move(traySource.x, traySource.y);
  await page.mouse.down();
  await page.mouse.move(tray.x + tray.width / 2, tray.y + tray.height / 2, { steps: 10 });
  await page.mouse.up();
  await page.waitForFunction(() => app.studioSelectedKeys.includes('local:2'));
  await page.locator('.studio-agent-card').filter({ hasText: 'Researcher' }).locator('.studio-select-agent').click();

  await page.locator('.studio-agent-card').filter({ hasText: 'Coordinator' }).locator('.studio-select-agent').click();
  await remote.locator('.studio-select-agent').click();
  await page.locator('.studio-team-dock .el-button').click();
  const composer = page.locator('.studio-composer');
  await composer.locator('.el-form-item').filter({ hasText: '团队名称' }).locator('input').fill('Browser Test Team');
  await composer.locator('.el-form-item').filter({ hasText: '任务标题' }).locator('input').fill('Browser Test Task');
  await composer.locator('textarea').fill('Prepare a concise test result');
  await composer.getByText('工具调用需要人工审批', { exact: true }).click();
  await page.route('**/*ChatGroupAppService.StartStudioTask*', route => route.fulfill({
    status: 200, contentType: 'application/json', body: JSON.stringify({ success: false, errorMessage: 'Test scheduling failure' })
  }));
  const run = page.locator('.el-dialog__footer').getByRole('button', { name: '组队并运行', exact: true });
  await run.click();
  await page.getByText('团队已保存，请修正任务请求后重试，将复用同一个团队。', { exact: true }).waitFor();
  check((await fixture()).writes.filter(write => write.method === 'CreateStudioTeam').length === 1, 'Team was not persisted');
  await page.unroute('**/*ChatGroupAppService.StartStudioTask*');
  await run.click();
  await page.waitForFunction(() => app.agentGraphSnapshot.tasks.some(task => task.name === 'Browser Test Task'));
  const data = await fixture();
  check(data.writes.filter(write => write.method === 'CreateStudioTeam').length === 1, 'Retry created a duplicate team');
  const runWrite = data.writes.find(write => write.method === 'StartStudioTask');
  check(runWrite.body.requireHumanApproval && runWrite.body.humanInTheLoopLevel === 2, 'Human approval policy lost');
  check(Number(runWrite.query.chatGroupId) === runWrite.body.chatGroupId, 'StartStudioTask body/query mismatch');

  await page.locator('.studio-shelf .studio-task-card').filter({ hasText: 'Browser Test Task' }).click();
  await page.locator('.studio-history').waitFor();
  check(await page.locator('.studio-history script').count() === 0, 'Raw HTML was injected into conversation');
  check(await page.locator('.studio-history').innerText().then(text => text.includes('<script>')), 'Escaped history not rendered');
  await page.locator('.studio-inspector').getByRole('button', { name: '停止', exact: true }).click();
  await page.locator('.el-message-box__btns .el-button--primary').click();
  await page.waitForFunction(() => app.studioSelectedTask?.status === 4);
  await page.locator('.studio-inspector').getByRole('button', { name: '归档', exact: true }).click();
  await page.waitForFunction(() => !app.agentGraphSnapshot.tasks.some(task => task.name === 'Browser Test Task'));

  await page.evaluate(() => fetch('/fixture-add'));
  await page.waitForFunction(() => app.agentGraphSnapshot.agents.some(agent => agent.id === 4));
  await page.getByRole('tab', { name: '智能体', exact: true }).click();
  await page.locator('.studio-agent-card').filter({ hasText: 'New Designer' }).waitFor();
  await page.getByRole('tab', { name: '团队', exact: true }).click();
  await page.locator('.studio-group-card').filter({ hasText: 'New Studio' }).waitFor();
  await page.getByRole('tab', { name: '任务', exact: true }).click();
  await page.locator('.studio-task-card').filter({ hasText: 'New failed task' }).waitFor();
  const renderCount = await page.evaluate(() => app.agentGraphRenderCount);
  await page.evaluate(() => fetch('/fixture-rename'));
  await page.waitForFunction(count => app.agentGraphRenderCount > count
    && app.agentGraphSnapshot.groups[0].name === 'Renamed Product Studio', renderCount);

  await page.locator('.studio-scene-controls').getByPlaceholder('任务状态').click();
  await page.locator('.el-select-dropdown__item').filter({ hasText: /^失败$/ }).click();
  await page.locator('.studio-brand').click();
  check(await page.locator('.studio-shelf .studio-task-card').count() === 1, 'Failed status filter missing');
  await page.getByPlaceholder('搜索智能体 / 团队 / 任务').fill('no match');
  check(await page.locator('.studio-shelf .studio-task-card').count() === 0, 'Task search did not filter');
  await page.getByPlaceholder('搜索智能体 / 团队 / 任务').fill('');

  await page.setViewportSize({ width: 480, height: 900 });
  await page.waitForTimeout(250);
  check(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth),
    'Mobile layout overflows horizontally');
  check(await page.evaluate(() => {
    const scene = app.agentGraph3d;
    return Math.abs(scene.renderer.domElement.getBoundingClientRect().width - scene.container.clientWidth) < 1;
  }), 'Mobile WebGL canvas is wider than its actual viewport');
  await page.locator('.studio-team-dock').scrollIntoViewIfNeeded();
  check(await page.evaluate(() => {
    const dock = document.querySelector('.studio-team-dock').getBoundingClientRect();
    const container = document.querySelector('.main-container').getBoundingClientRect();
    return dock.bottom <= container.bottom && dock.bottom <= window.innerHeight;
  }), 'Production parent layout clips the mobile team tray');
  await page.setViewportSize({ width: 1440, height: 1000 });

  const memory = await page.evaluate(() => {
    const renderer = app.agentGraph3d.renderer;
    app.destroyAgentGraph3d();
    return { geometries: renderer.info.memory.geometries, textures: renderer.info.memory.textures };
  });
  check(memory.geometries === 0 && memory.textures === 0, 'Disposed studio retained GPU resources: ' + JSON.stringify(memory));
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('http://127.0.0.1:51961/');
  await page.waitForFunction(() => window.app?.agentGraph3d?.agentById.size === 4);
  check(await page.evaluate(() => app.agentGraph3d.reducedMotion && app.agentGraph3d.agentObjects.every(entry =>
    entry.mesh.scale.y === 1 && entry.mesh.position.distanceTo(entry.target) < 0.01)), 'Reduced-motion preference ignored');
  await page.evaluate(() => {
    app.destroyAgentGraph3d();
    const original = THREE.WebGLRenderer;
    THREE.WebGLRenderer = function () { throw new Error('WebGL unavailable for fallback test'); };
    app.ensureAgentGraph3d();
    THREE.WebGLRenderer = original;
  });
  await page.locator('.studio-scene-fallback').waitFor();
  await page.locator('.studio-agent-card').filter({ hasText: 'New Designer' }).locator('.studio-select-agent').click();
  check(await page.locator('.studio-team-dock .el-button').isEnabled(), 'Studio management disabled without WebGL');
  check(errors.length === 0, 'Browser runtime errors: ' + errors.join('; '));
  return 'PASS: canvas/tray/roster drag, identity, duplicate drop, team/run retry, HIL policy, safe conversation, stop/archive, live additions/rename, failed filter/search, mobile, reduced motion, GPU cleanup and WebGL fallback';
}
