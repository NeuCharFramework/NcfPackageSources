const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const root = path.resolve(__dirname, '../../Senparc.Xncf.AgentsManager');
const state = require(path.join(root, 'wwwroot/js/AgentsManager/studio-state.js'));

const snapshot = {
  agents: [
    { id: 1, participantKey: 'local:1', name: 'Owner', enable: true, agentKind: 'Local' },
    { id: 1, participantKey: 'remote:1', name: 'Remote', enable: true, agentKind: 'RemoteA2A' },
    { id: 2, participantKey: 'local:2', name: 'Human', enable: true, isHuman: true },
    { id: 3, participantKey: 'local:3', name: 'Disabled', enable: false }
  ],
  groups: [{ id: 1 }, { id: 2 }],
  links: [{ groupId: 1, participantKey: 'remote:1' }],
  tasks: [
    { id: 1, groupId: 1, name: 'Finished', status: 3 },
    { id: 2, groupId: 1, name: 'Failed', status: 5 },
    { id: 3, groupId: 1, name: 'Running', status: 1 },
    { id: 4, groupId: 2, name: 'Approval', status: 2, humanPendingCount: 1 }
  ]
};

test('participant identity does not collide between local and remote ids', () => {
  assert.deepEqual(state.members(snapshot, 1).map(state.key), ['remote:1']);
  assert.notEqual(state.key(snapshot.agents[0]), state.key(snapshot.agents[1]));
});

test('only enabled non-human local members can own or enter a team', () => {
  assert.deepEqual(snapshot.agents.filter(state.roleEligible).map(state.key), ['local:1']);
});

test('task board prioritizes human requests and active tasks, and supports failed status filtering', () => {
  assert.deepEqual(state.tasks(snapshot).map(task => task.id), [4, 3, 2, 1]);
  assert.deepEqual(state.tasks(snapshot, 1, [5]).map(task => task.id), [2]);
  assert.deepEqual(state.tasks(snapshot, null, [], 'RUN').map(task => task.id), [3]);
});

test('room layout is deterministic, non-mutating, and accommodates new and empty groups', () => {
  const source = JSON.stringify(snapshot);
  const layout = state.layout(snapshot);
  const reversed = { ...snapshot, groups: snapshot.groups.slice().reverse() };
  assert.deepEqual(state.layout(reversed), layout);
  assert.equal(JSON.stringify(snapshot), source);
  assert.equal(layout.stations.length, 2);
  assert.equal(state.layout({ agents: [], groups: [], links: [] }).stations.length, 0);
  const expanded = state.layout({ ...snapshot, groups: Array.from({ length: 40 }, (_, i) => ({ id: i + 1 })) });
  assert.equal(expanded.stations.length, 40);
  assert.ok(expanded.width > layout.width);
});

function createStudio() {
  const context = {
    window: { AgentStudioState: state }, console,
    ncfT: key => key,
    serviceAM: {}, getInterfaceQueryStr: query => new URLSearchParams(query).toString()
  };
  vm.runInNewContext(fs.readFileSync(path.join(root, 'wwwroot/js/AgentsManager/studio.js'), 'utf8'), context);
  const mixin = context.window.AgentsStudioMixin;
  const studio = Object.assign(mixin.data(), {
    agentGraphSnapshot: structuredClone(snapshot),
    refreshAgentGraphSnapshot: async () => {},
    $message: { warning() {}, success() {}, error() {}, info() {} }
  });
  for (const [name, method] of Object.entries(mixin.methods)) studio[name] = method.bind(studio);
  for (const [name, getter] of Object.entries(mixin.computed))
    Object.defineProperty(studio, name, { get: getter.bind(studio) });
  return { studio, mixin, context };
}

test('failed task submission retains the saved team and retries without creating duplicate teams', async () => {
  const { studio } = createStudio();
  Object.assign(studio, {
    studioSelectedKeys: ['local:1', 'remote:1'], studioTeamName: 'Team',
    studioAdminId: 1, studioEntryId: 1, studioTaskName: 'Task', studioCommand: 'Work',
    studioComposerOpen: true
  });
  const calls = [];
  let failRun = true;
  studio.studioRequest = async (service, method, query, body) => {
    calls.push({ method, query, body });
    if (method === 'CreateStudioTeam') return { id: 8 };
    if (failRun) throw new Error('Model unavailable');
    return { chatTaskId: 10 };
  };
  studio.studioReportError = error => { studio.studioError = error.message; };
  await studio.studioSubmitTeam(true);
  assert.equal(studio.studioCreatedTeamId, 8);
  assert.equal(studio.studioComposerOpen, true);
  failRun = false;
  await studio.studioSubmitTeam(true);
  assert.equal(calls.filter(call => call.method === 'CreateStudioTeam').length, 1);
  assert.deepEqual(calls.filter(call => call.method === 'StartStudioTask').map(call => call.query.chatGroupId), [8, 8]);
  assert.equal(studio.studioComposerOpen, false);
});

test('duplicate membership drop does not send a write and disabled agents cannot be selected', async () => {
  const { studio } = createStudio();
  let calls = 0;
  studio.studioRequest = async () => { calls++; };
  await studio.studioChangeMember(1, 'remote:1');
  studio.studioToggleAgent(snapshot.agents[3]);
  assert.equal(calls, 0);
  assert.equal(studio.studioSelectedKeys.length, 0);
});

test('snapshot reconciliation removes deleted members but never merges local/remote identities', () => {
  const { studio, mixin } = createStudio();
  studio.studioSelectedKeys = ['local:1', 'remote:1'];
  studio.studioAdminId = 1;
  studio.studioEntryId = 1;
  studio.agentGraphSnapshot.agents = studio.agentGraphSnapshot.agents.filter(agent => agent.agentKind === 'RemoteA2A');
  mixin.watch.agentGraphSnapshot.call(studio);
  assert.deepEqual(Array.from(studio.studioSelectedKeys), ['remote:1']);
  assert.equal(studio.studioAdminId, null);
});

test('a selected agent that becomes disabled can still be removed from a draft team', () => {
  const { studio } = createStudio();
  studio.studioSelectedKeys = ['local:3'];
  studio.studioToggleAgent(snapshot.agents[3]);
  assert.equal(studio.studioSelectedKeys.length, 0);
});

test('a success response without a persisted task id is not treated as task submission success', async () => {
  const { studio } = createStudio();
  Object.assign(studio, {
    studioRunGroupId: 1, studioTaskName: 'Task', studioCommand: 'Work', studioComposerOpen: true
  });
  studio.studioRequest = async () => ({});
  studio.studioReportError = error => { studio.studioError = error.message; };
  await studio.studioSubmitTeam(true);
  assert.equal(studio.studioComposerOpen, true);
  assert.ok(studio.studioError);
});

test('all studio resource keys exist in neutral and Chinese resources', () => {
  const neutral = fs.readFileSync(path.join(root, 'Resources/AgentsManagerResource.resx'), 'utf8');
  const chinese = fs.readFileSync(path.join(root, 'Resources/AgentsManagerResource.zh-CN.resx'), 'utf8');
  const keys = [...neutral.matchAll(/name="Studio\.([^"]+)"/g)].map(match => match[1]);
  for (const key of keys) assert.ok(chinese.includes('name="Studio.' + key + '"'), key);
  const sources = ['wwwroot/js/AgentsManager/studio.js', 'Areas/Admin/Pages/AgentsManager/_Studio.cshtml']
    .map(file => fs.readFileSync(path.join(root, file), 'utf8')).join('\n');
  for (const match of sources.matchAll(/\bst\('([^']+)'\)/g))
    assert.ok(keys.includes(match[1]), 'Missing resource: ' + match[1]);
});
