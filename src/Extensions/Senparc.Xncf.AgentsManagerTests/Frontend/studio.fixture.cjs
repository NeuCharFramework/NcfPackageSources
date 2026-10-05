const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '../../../..');
const moduleRoot = path.join(root, 'src/Extensions/Senparc.Xncf.AgentsManager');
const siteRoot = path.join(root, 'tools/NcfSimulatedSite/Senparc.Web/wwwroot');
const promptRoot = path.join(root, 'src/Extensions/Senparc.Xncf.PromptRange/wwwroot');
const assets = {
  '/vue.js': path.join(siteRoot, 'lib/vue/vue.js'),
  '/element.js': path.join(siteRoot, 'lib/element-ui_2.13.2/element.js'),
  '/element.css': path.join(siteRoot, 'lib/element-ui_2.13.2/element.css'),
  '/fonts/element-icons.woff': path.join(siteRoot, 'lib/element-ui_2.13.2/fonts/element-icons.woff'),
  '/fonts/element-icons.ttf': path.join(siteRoot, 'lib/element-ui_2.13.2/fonts/element-icons.ttf')
};
const agent = (id, name, remote = false) => ({
  id, name, participantKey: (remote ? 'remote:' : 'local:') + id,
  agentKind: remote ? 'RemoteA2A' : 'Local', enable: true, description: 'Studio test agent',
  skillKinds: remote ? ['a2a'] : ['function'], chattingCount: 0, pausedCount: 0
});
const group = (id, name, keys) => ({
  id, name, enable: true, adminAgentTemplateId: 1, enterAgentTemplateId: 1,
  memberParticipantKeys: keys, memberAgentIds: keys.filter(key => key.startsWith('local:')).map(key => Number(key.split(':')[1])),
  runningTaskCount: 0, pausedTaskCount: 0, humanInTheLoopPendingCount: 0, taskStatusCounts: {}, state: 0
});
const snapshot = {
  agents: [agent(1, 'Coordinator'), agent(2, 'Researcher'), agent(1, 'Remote Research', true)],
  groups: [group(1, 'Product Studio', ['local:1']), group(2, 'Empty Studio', [])],
  links: [{ groupId: 1, participantKey: 'local:1', agentId: 1 }],
  collaborations: [],
  tasks: [{ id: 1, groupId: 1, name: 'Completed analysis', status: 3 }]
};
const writes = [];
const translations = Object.fromEntries([...fs.readFileSync(
  path.join(moduleRoot, 'Resources/AgentsManagerResource.zh-CN.resx'), 'utf8')
  .matchAll(/<data name="(Studio\.[^"]+)"[^>]*><value>([^<]*)<\/value><\/data>/g)]
  .map(match => ['AgentsManager.' + match[1], match[2].replace(/&amp;/g, '&')]));
const template = fs.readFileSync(path.join(moduleRoot, 'Areas/Admin/Pages/AgentsManager/_Studio.cshtml'), 'utf8')
  .replace(/@@/g, '@');

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url, 'http://localhost');
  const send = (body, status = 200, type = 'application/json') => {
    response.writeHead(status, { 'Content-Type': type });
    response.end(type === 'application/json' ? JSON.stringify(body) : body);
  };
  if (url.pathname === '/') {
    return send(`<!doctype html><html lang="zh-CN"><head><meta charset="utf-8">
      <meta name="viewport" content="width=device-width,initial-scale=1">
      <link rel="stylesheet" href="/element.css"><link rel="stylesheet" href="/css/AgentsManager/index.css">
      <link rel="stylesheet" href="/css/AgentsManager/studio.css">
      <style>body{margin:0;padding:16px;background:#e8eff5;font-family:system-ui}</style>
      </head><body><div id="app"><div class="tabPane-container studio-layout">
      <div class="sidebar-container"></div><div class="main-container agent-container">
      <div class="agent-view-mode-row">Agents → 3D</div>${template}</div></div></div>
      <script src="/vue.js"></script><script src="/element.js"></script>
      <script src="/js/AgentsManager/lib/axios.min.js"></script><script src="/js/AgentsManager/lib/marked.min.js"></script>
      <script>window.ncfT=key=>(${JSON.stringify(translations)})[key]||key;
      window.ncfTranslateText=value=>value;
      const OriginalVue=Vue;
      window.Vue=function(options){
        options.created=[];
        options.mounted=[function(){
          this.agentListViewMode='three';
          this.$nextTick(()=>{this.ensureAgentGraph3d();this.refreshAgentGraphSnapshot(true);this.startAgentGraphPolling()});
        }];
        return new OriginalVue(options);
      };
      Object.assign(Vue,OriginalVue); Vue.prototype=OriginalVue.prototype;</script>
      <script type="module" src="/js/AgentsManager/three-loader.js"></script></body></html>`, 200, 'text/html');
  }
  if (url.pathname === '/rendered') {
    const renderedPath = process.env.NCF_STUDIO_RENDERED_PAGE;
    if (!renderedPath || !fs.existsSync(renderedPath))
      return send('Run AgentStudioRenderingTests with NCF_STUDIO_RENDERED_PAGE before this check.', 500, 'text/plain');
    const rendered = JSON.parse(fs.readFileSync(renderedPath, 'utf8'));
    return send(`<!doctype html><html lang="zh-CN"><head><meta charset="utf-8">
      <meta name="viewport" content="width=device-width,initial-scale=1">
      <link rel="stylesheet" href="/element.css">${rendered.styles}
      <style>body{margin:0;padding:16px;background:#e8eff5;font-family:system-ui}</style>
      </head><body><div id="app">${rendered.body}</div>
      <script src="/vue.js"></script><script src="/element.js"></script>
      <script>
        window.ncfRegisterSourceTranslations=value=>{window.ncfSourceTranslations=value};
        window.ncfTranslateText=value=>window.ncfSourceTranslations?.[value]||value;
        window.ncfT=key=>window.ncfI18n?.[key]||key;
        window.getInterfaceQueryStr=query=>new URLSearchParams(query).toString();
      </script>${rendered.scripts}</body></html>`, 200, 'text/html');
  }
  if (url.pathname === '/fixture-state') return send({ snapshot, writes });
  if (url.pathname === '/favicon.ico') {
    response.writeHead(204);
    return response.end();
  }
  if (url.pathname === '/fixture-rename') {
    snapshot.groups[0].name = 'Renamed Product Studio';
    snapshot.agents[0].name = 'Renamed Coordinator';
    return send({ success: true });
  }
  if (url.pathname === '/fixture-add') {
    snapshot.agents.push(agent(4, 'New Designer'));
    snapshot.groups.push(group(4, 'New Studio', []));
    snapshot.tasks.push({ id: 4, groupId: 4, name: 'New failed task', status: 5 });
    return send({ success: true });
  }
  if (url.pathname.startsWith('/api/')) {
    let rawBody = '';
    for await (const chunk of request) rawBody += chunk;
    const body = rawBody ? JSON.parse(rawBody) : null;
    const method = url.pathname.split('.').at(-1);
    const query = Object.fromEntries(url.searchParams);
    if (request.method === 'POST') writes.push({ method, body, query });
    let data = '';
    if (method === 'GetAgentGraphSnapshot') data = snapshot;
    else if (method === 'GetPluginTypes' || method === 'GetKnowledgeBaseOptions') data = [];
    else if (method === 'GetChatGroupList') data = { chatGroupDtoList: snapshot.groups };
    else if (method === 'GetListAsync') data = [{ id: 1, alias: 'Test chat model', configModelType: 2 }];
    else if (method === 'SetStudioParticipant') {
      const id = Number(query.groupId);
      const target = snapshot.groups.find(group => group.id === id);
      if (!target) return send({ success: false, errorMessage: 'Group missing' });
      if (query.remove === 'true') {
        snapshot.links = snapshot.links.filter(link => !(link.groupId === id && link.participantKey === query.participantKey));
        target.memberParticipantKeys = target.memberParticipantKeys.filter(key => key !== query.participantKey);
      } else if (!target.memberParticipantKeys.includes(query.participantKey)) {
        target.memberParticipantKeys.push(query.participantKey);
        snapshot.links.push({ groupId: id, participantKey: query.participantKey });
      }
      data = 'Membership saved';
    } else if (method === 'CreateStudioTeam') {
      const id = Math.max(...snapshot.groups.map(group => group.id)) + 1;
      const created = group(id, body.name, body.participantKeys);
      snapshot.groups.push(created);
      body.participantKeys.forEach(key => snapshot.links.push({ groupId: id, participantKey: key }));
      data = created;
    } else if (method === 'StartStudioTask') {
      const task = { id: snapshot.tasks.length + 10, groupId: body.chatGroupId, name: body.name, status: 1 };
      snapshot.tasks.push(task);
      const target = snapshot.groups.find(group => group.id === body.chatGroupId);
      target.runningTaskCount++;
      target.taskStatusCounts[1] = (target.taskStatusCounts[1] || 0) + 1;
      data = { chatTaskId: task.id, chatGroupId: task.groupId, chatGroupName: target.name, status: 1 };
    } else if (method === 'GetItem') {
      const task = snapshot.tasks.find(task => task.id === Number(query.id));
      data = { chatTaskDto: { ...task, promptCommand: 'Test instructions', totalRounds: 2, totalTokens: 60 } };
    } else if (method === 'GetList') {
      if (url.pathname.includes('AgentTemplateAppService'))
        data = { list: snapshot.agents.filter(agent => agent.agentKind === 'Local') };
      else if (url.pathname.includes('ChatTaskAppService'))
        data = { chatTaskList: snapshot.tasks.map(task => ({ ...task, chatGroupId: task.groupId })) };
      else
        data = { chatGroupHistories: [{ id: 1, fromParticipantName: 'Coordinator', message: '**Test result**\n<script>alert(1)</script>' }] };
    } else if (method === 'ForceStop') {
      snapshot.tasks.find(task => task.id === Number(query.id)).status = 4;
      data = 'Task stopped';
    } else if (method === 'SetArchiveStatus') {
      snapshot.tasks = snapshot.tasks.filter(task => task.id !== Number(query.id));
      data = 'Task archived';
    } else if (method === 'Enable') {
      const list = url.pathname.includes('ChatGroupAppService') ? snapshot.groups : snapshot.agents;
      list.find(item => item.id === Number(query.id)
        && (!url.pathname.includes('RemoteAgentAppService') || item.agentKind === 'RemoteA2A')).enable = query.enable === 'true';
    }
    return send({ success: true, data });
  }
  let file = assets[url.pathname];
  if (!file && /^\/(js|css|images)\/AgentsManager\//.test(url.pathname))
    file = path.join(moduleRoot, 'wwwroot', url.pathname);
  if (!file && url.pathname.startsWith('/js/PromptRange/')) file = path.join(promptRoot, url.pathname);
  if (!file || !fs.existsSync(file)) return send('Not found', 404, 'text/plain');
  const extension = path.extname(file);
  const types = { '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png' };
  response.writeHead(200, { 'Content-Type': types[extension] || 'application/octet-stream' });
  fs.createReadStream(file).pipe(response);
});
server.listen(Number(process.env.PORT || 51961), '127.0.0.1', () =>
  console.log('Studio fixture: http://127.0.0.1:' + server.address().port));
