(function (root, factory) {
  const state = factory();
  if (typeof module === 'object' && module.exports) module.exports = state;
  else root.AgentStudioState = state;
})(typeof window === 'undefined' ? globalThis : window, function () {
  function key(agent) {
    return agent.participantKey || 'local:' + agent.id;
  }

  function roleEligible(agent) {
    return agent.enable && agent.agentKind !== 'RemoteA2A' && !agent.isHuman
      && !(agent.skillKinds || []).includes('human');
  }

  function members(snapshot, groupId) {
    const keys = new Set((snapshot.links || []).filter(link => link.groupId === groupId)
      .map(link => link.participantKey || 'local:' + link.agentId));
    return (snapshot.agents || []).filter(agent => keys.has(key(agent)));
  }

  function taskPriority(task) {
    return task.humanPendingCount > 0 ? -1 : ({ 2: 0, 1: 1, 0: 2, 5: 3, 3: 4, 4: 5 }[task.status] ?? 6);
  }

  function tasks(snapshot, groupId, statuses, search) {
    const query = String(search || '').trim().toLowerCase();
    return (snapshot.tasks || []).filter(task =>
      (!groupId || task.groupId === groupId)
      && (!statuses || !statuses.length || statuses.includes(Number(task.status)))
      && (!query || String(task.name).toLowerCase().includes(query)))
      .slice().sort((a, b) => taskPriority(a) - taskPriority(b) || b.id - a.id);
  }

  function layout(snapshot) {
    const groups = (snapshot.groups || []).slice().sort((a, b) => a.id - b.id);
    const columns = Math.max(1, Math.ceil(Math.sqrt(groups.length)));
    const maxMembers = Math.max(1, ...groups.map(group => members(snapshot, group.id).length));
    const spacing = Math.max(30, 20 + Math.ceil(maxMembers / 10) * 8);
    const rows = Math.max(1, Math.ceil(groups.length / columns));
    return {
      stations: groups.map((group, index) => ({
        id: group.id,
        x: (index % columns - (columns - 1) / 2) * spacing,
        z: (Math.floor(index / columns) - (rows - 1) / 2) * spacing,
        radius: spacing / 2 - 2
      })),
      width: Math.max(64, columns * spacing + 12),
      depth: Math.max(56, rows * spacing + 30),
      loungeZ: rows * spacing / 2 + 12
    };
  }

  return { key, roleEligible, members, tasks, layout };
});
