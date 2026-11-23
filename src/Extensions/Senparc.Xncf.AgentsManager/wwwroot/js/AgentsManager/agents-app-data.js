/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

var AgentsAppData = {
      devHost: 'http://pr-felixj.frp.senparc.com',
      elSize: 'medium', //Legacy comment translated to English.
      tabsActiveName: 'first', // first(agent) second(group) third(task)
      //Legacy comment translated to English.
      visible: {
        drawerAgent: false, // agent add|edit
        drawerRemoteAgent: false, //Legacy comment translated to English.
        dialogRemoteAgentEditor: false, //Legacy comment translated to English.
        dialogPublishedA2A: false, //Legacy comment translated to English.
        dialogGroupAgent: false, // agent adddialog
        drawerGroup: false, // group add|edit
        drawerGroupStart: false, // group start 
        dialogAgentParameter: false, //Legacy comment translated to English.
        dialogTaskDescription: false, // taskdescription
        dialogTaskEvaluation: false, // task evaluationpage
        dialogMcpTools: false, //Legacy comment translated to English.
        drawerFunctionBindings: false, //Legacy comment translated to English.
        drawerMcpSelect: false, //Legacy comment translated to English.
      },
      taskStateText: {
        0: ncfST('Frontend.AgentsManager.Waiting'),  // wait Waiting stand #3376cd
        1: ncfST('Frontend.AgentsManager.Chatting'),
        2: ncfST('Frontend.AgentsManager.Paused'),
        3: ncfST('Frontend.AgentsManager.Done'), // complete Finished success #67C23A
        4: ncfST('Frontend.AgentsManager.Cancel'),
        5: ncfST('Frontend.AgentsManager.Failed'),
      },
      taskStateColor: {
        0: 'waitColor',
        1: 'chartColor',
        2: 'chartColor',
        3: 'successColor',
        4: 'cancelledColor',
        5: 'errorColor',
      },
      taskStateIcon: {
        0: 'fas fa-clock',
        1: 'fas fa-spinner fa-pulse',//Legacy comment translated to English.
        2: 'fas fa-play-circle',
        3: 'fas fa-check-circle', // el-icon-success
        4: 'fal fa-minus-circle fa-rotate-45', //Legacy comment translated to English.
        5: 'fas fa-times-circle',// el-icon-error
      },
      agentStateText: {
        1: ncfST('Frontend.AgentsManager.Standby'),
        2: ncfST('Frontend.AgentsManager.Running'),
        3: ncfST('Frontend.AgentsManager.Disabled'),
      },
      agentStateColor: {
        1: 'standColor',
        2: 'proceColor',
        3: 'stopColor',
      },
      agentAvatarList: [
        '/images/AgentsManager/avatar/avatar1.png',
        '/images/AgentsManager/avatar/avatar2.png',
        '/images/AgentsManager/avatar/avatar3.png',
        '/images/AgentsManager/avatar/avatar4.png',
        '/images/AgentsManager/avatar/avatar5.png',
      ],
      // agent ---start
      agentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      agentFCPVisible: false, //Legacy comment translated to English.
      agentFilterCriteria: [
        {
          label: ncfST('Frontend.AgentsManager.All'),
          value: 'all',
          checked: true
        },
        {
          label: ncfST('Frontend.AgentsManager.Running'),
          value: 'proce',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Disabled'),
          value: 'stop',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Standby'),
          value: 'stand',
          checked: false
        }
      ],
      agentList: [],
      remoteAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '',
      },
      remoteAgentList: [],
      remoteAgentBatchTesting: false,
      remoteAgentTestingIds: {},
      remoteAgentForm: {
        id: 0,
        name: '',
        description: '',
        enable: true,
        protocol: 0,
        agentCardUrl: '',
        authenticationMode: 0,
        authHeaderName: '',
        authSecretKey: '',
        timeoutSeconds: 60,
      },
      remoteAgentFormRules: {
        name: [{ required: true, message: ncfST('Frontend.AgentsManager.EnterAgentName'), trigger: 'blur' }],
        agentCardUrl: [{ required: true, message: ncfST('Frontend.AgentsManager.AgentCardURL'), trigger: 'blur' }]
      },
      publishedA2AForm: {
        id: 0,
        agentTemplateId: 0,
        publicAgentKey: '',
        enable: false,
        cardName: '',
        cardDescription: '',
        skillId: 'chat',
        skillName: '',
        skillDescription: '',
        allowFunctionCalls: false,
        maxInputCharacters: 12000,
        authenticationMode: 0,
        authHeaderName: '',
        authSecretKey: '',
        agentCardUrl: ''
      },
      publishedA2AFormRules: {
        publicAgentKey: [{ required: true, message: ncfST('Frontend.AgentsManager.PublicIdentifier'), trigger: 'blur' }]
      },
      knowledgeBaseOptions: [],
      knowledgeBaseOptionsLoaded: false,
      fillCardNum: 0, //Legacy comment translated to English.
      agentListElResizeObserver: null,
      scrollbarAgentIndex: '', //Legacy comment translated to English.
      agentDetails: '', //Legacy comment translated to English.
      //Legacy comment translated to English.
      agentDetailsTabsActiveName: 'first', // first(group) second(task)
      //Legacy comment translated to English.
      agentDetailsGroupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      agentDetailsGroupList: [],
      agentDetailsGroupShowType: '1', //Legacy comment translated to English.
      agentDetailsGroupIndex: 0, //Legacy comment translated to English.
      agentDetailsGroupDetails: '',
      agentDetailsGroupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      agentGroupTaskSelection: [], //Legacy comment translated to English.
      agentDetailsGroupTaskList: [], // group task list
      agentDetailsGroupTaskHistoryList: [],
      agentDetailsGroupDetailsTaskDetails: '',
      agentDetailsGroupTaskMemberList: [],
      agentGroupTaskMemberfilter: '',
      agentGroupTaskMemberfilterList: [],
      //Legacy comment translated to English.
      agentDetailsTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      agentDetailsTaskIndex: 0, //Legacy comment translated to English.
      agentDetailsTaskList: [],
      agentDetailsTaskDetails: '',
      agentDetailsTaskHistoryList: [],
      agentDetailsTaskMemberList: [],
      agentTaskMemberfilter: '',
      agentTaskMemberfilterList: [],
      // agent ---end
      // group ---start
      groupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      groupFCPVisible: false, //Legacy comment translated to English.
      groupFilterCriteria: [
        {
          label: ncfST('Frontend.AgentsManager.All'),
          value: 'all',
          checked: true
        },
        {
          label: ncfST('Frontend.AgentsManager.Running'),
          value: 'proce',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Disabled'),
          value: 'stop',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Standby'),
          value: 'stand',
          checked: false
        }
      ],
      groupTreeDefaultProps: {
        children: 'children',
        label: 'name'
      },
      groupTreeData: [],
      groupSelection: [],
      groupList: [],
      groupShowType: '1', //Legacy comment translated to English.
      scrollbarGroupIndex: '', //Legacy comment translated to English.
      groupDetails: '',
      groupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      groupTaskSelection: [],
      groupTaskList: [],
      groupTaskListLastNew: [],
      groupTaskDetails: '',
      groupTaskHistoryList: [],
      groupTaskMemberList: [],
      groupTaskMemberfilter: '',
      groupTaskMemberfilterList: [],
      // group add|edit agent
      groupAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      isGetGroupAgent: false,
      groupAgentList: [], //Legacy comment translated to English.
      groupAgentTotal: 0,
      isGetGroupRemoteAgent: false,
      groupRemoteAgentList: [],
      groupRemoteAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '',
      },
      // group ---end
      // task task ---start
      taskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // filter text
        timeSort: false, // descending by default
        proce: false, // in progress
        stop: false, // disabled
        stand: false, // standby
      },
      taskArchiveScope: 'active', // active | archived | all
      taskArchiveScopeOptions: [
        { label: ncfST('Frontend.AgentsManager.All'), value: 'active' },
        { label: ncfST('Frontend.AgentsManager.Archive'), value: 'archived' },
        { label: ncfST('Frontend.AgentsManager.All'), value: 'all' }
      ],
      taskArchiveSavingId: 0,
      taskFCPVisible: false, //Legacy comment translated to English.
      taskFilterCriteria: [
        {
          label: ncfST('Frontend.AgentsManager.All'),
          value: 'all',
          checked: true
        },
        {
          label: ncfST('Frontend.AgentsManager.Running'),
          value: 'proce',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Disabled'),
          value: 'stop',
          checked: false
        },
        {
          label: ncfST('Frontend.AgentsManager.Standby'),
          value: 'stand',
          checked: false
        }
      ],
      scrollbarTaskIndex: '', //Legacy comment translated to English.
      taskSelection: [],
      taskList: [],
      taskDetails: '', //Legacy comment translated to English.
      taskHistoryList: [],
      taskMemberList: [],
      taskMemberfilter: '',
      taskMemberfilterList: [],
      // task task ---end
      // agent add|edit
      agentForm: {
        id: 0, //Legacy comment translated to English.
        name: '', // name
        systemMessageType: '1',
        systemMessage: '', // 
        enable: true, // whether enabled
        description: '', // description
        hookRobotType: 0, //Legacy comment translated to English.
        hookRobotParameter: '', //Legacy comment translated to English.
        avastar: '/images/AgentsManager/avatar/avatar1.png', //Legacy comment translated to English.
        functionCallNames: '', //Legacy comment translated to English.
        functionBindings: [], //Legacy comment translated to English.
        mcpEndpoints: '', // MCP Endpoints
        knowledgeBaseId: null, //Legacy comment translated to English.
        modelBinding: 0, //Legacy comment translated to English.
        aiModelId: null,
        connectorInstanceIds: [],
      },
      //Legacy comment translated to English.
      agentSystemMessageTypeDetectionPending: false,
      agentPromptConversionLoading: false,
      agentFormRules: {
        name: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
        systemMessage: [
          { required: true, message: ncfST('Frontend.AgentsManager.SelectAlternate'), trigger: 'change' },
        ],
        // description: [
        //     { required: true, message: 'please enter a value', trigger: 'blur' },
        // ],
        hookRobotType: [
          { required: true, message: ncfST('Frontend.AgentsManager.SelectAlternate'), trigger: 'change' },
        ],
        // hookRobotParameter: [
        //     { required: true, message: 'please enter a value', trigger: 'blur' },
        // ],
        avastar: [
          { required: true, message: ncfST('Frontend.AgentsManager.SelectAlternate'), trigger: 'change' },
        ],
        functionCallNames: [
          { required: false, message: ncfST('Frontend.AgentsManager.EnterTheFunctionCallName'), trigger: 'change' }
        ]
      },
      // group add|edit
      groupForm: {
        enable: true, //Legacy comment translated to English.
        name: '', // name
        members: [], //Legacy comment translated to English.
        remoteMembers: [], //Legacy comment translated to English.
        description: '', // description
        contextSharingMode: null, //Legacy comment translated to English.
        adminAgentTemplateId: '', //Legacy comment translated to English.
        enterAgentTemplateId: '', //Legacy comment translated to English.
        includeHumanParticipant: false //Legacy comment translated to English.
      },
      groupFormRules: {
        name: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
        members: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'change' },
        ],
        adminAgentTemplateId: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'change' },
        ],
        enterAgentTemplateId: [
          { required: true, message: ncfST('Frontend.AgentsManager.SelectAlternate'), trigger: 'change' },
        ],
        // description: [
        //     { required: true, message: 'please enter a value', trigger: 'blur' },
        // ],
      },
      // group start
      groupStartForm: {
        groupName: '', // groupname
        chatGroupId: '', // groupid
        name: '', //Legacy comment translated to English.
        aiModelId: '', // model id
        promptCommand: '', // taskdescription
        personality: true, //Legacy comment translated to English.
        requireHumanApproval: false, //Legacy comment translated to English.
        humanInTheLoopLevel: 0, //Legacy comment translated to English.
        pluginToolPermission: 0, //Legacy comment translated to English.
        mcpToolPermission: 0,
        includeHumanParticipant: false,
        chatMaxRound: 20,
        description: '',
        limitConnectors: false,
        connectorInstanceIds: []
      },
      groupStartParticipants: [],
      groupStartParticipantLoading: false,
      groupStartHumanParticipantTouched: false,
      groupStartPromptCaretStart: 0,
      groupStartPromptCaretEnd: 0,
      groupStartFormRules: {
        chatGroupId: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
        name: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
        aiModelId: [
          { required: true, message: ncfST('Frontend.AgentsManager.SelectAlternate'), trigger: 'change' },
        ],
        promptCommand: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
      },
      // task evaluation
      evaluationForm: {
        score: '',
        evaluation: ''
      },
      evaluationFormRules: {
        // change
        score: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ],
        evaluation: [
          { required: true, message: ncfST('Frontend.AgentsManager.PleaseFillIn'), trigger: 'blur' },
        ]
      },
      //Legacy comment translated to English.
      historyTimer: {},
      //Legacy comment translated to English.
      taskListRetryTimer: {},
      //Legacy comment translated to English.
      historyStream: {},
      historyStreamSilentTimer: {},
      historyStreamingDrafts: {},
      humanApprovalRequests: {},
      toolApprovalDialogVisible: false,
      toolApprovalRequest: null,
      toolApprovalArgumentText: '',
      toolApprovalQueue: [],
      toolApprovalSubmitting: false,
      humanReplyDialogVisible: false,
      humanReplyRequest: null,
      humanReplyText: '',
      humanReplySubmitting: false,
      usageAnalyticsVisible: false,
      usageAnalyticsLoading: false,
      usageAnalyticsTaskId: null,
      usageAnalyticsTaskName: '',
      usageAnalyticsDateRange: [],
      usageAnalyticsAgentId: '',
      usageAnalyticsAgentOptions: [],
      usageAnalyticsData: {
        overview: {
          messageCount: 0,
          promptTokens: 0,
          completionTokens: 0,
          totalTokens: 0,
          averageResponseMilliseconds: 0,
          minResponseMilliseconds: 0,
          maxResponseMilliseconds: 0,
          p95ResponseMilliseconds: 0,
        },
        roundStats: [],
        agentStats: [],
        timelineStats: [],
      },
      //Legacy comment translated to English.
      agentParameterTabsValue: '', //Legacy comment translated to English.
      agentParameterList: [],
      // descriptioncontent
      describeContent: '',
      taskDescriptionDetails: null,
      functionCallInputVisible: false,
      functionCallInputValue: '',
      functionCallTags: [], //Legacy comment translated to English.
      pluginTypes: [], //Legacy comment translated to English.
      functionBindingCatalog: {
        functions: [],
        plugins: [],
        workflows: [],
        currentBindings: []
      },
      functionBindingTab: 'function',
      functionBindingSearch: '',
      functionBindingLoading: false,
      functionBindingSaving: false,
      agentAutoAttachXncf: false, //Legacy comment translated to English.
      editorFormInitialSnapshots: {}, //Legacy comment translated to English.
      // MCP Endpointsrelated
      mcpEndpointInputVisible: false,
      mcpEndpointNameValue: '',
      mcpEndpointUrlValue: '',
      mcpEndpointEditMode: false,
      mcpEndpointOriginalName: '',
      mcpSelectLoading: false, //Legacy comment translated to English.
      mcpSelectSearch: '', //Legacy comment translated to English.
      mcpSelectMessage: '', //Legacy comment translated to English.
      mcpModuleAvailable: false, //Legacy comment translated to English.
      mcpEndpointOptions: [], //Legacy comment translated to English.
      connectorOptions: [],
      connectorOptionsLoading: false,
      currentMcpTools: [], //Legacy comment translated to English.
      agentListViewMode: 'panel',
      agentStatisticMetric: 'totalTokens',
      agentStatisticMetricOptions: [
        { value: 'totalTokens', label: ncfST('Frontend.AgentsManager.TotalToken'), unit: 'Token' },
        { value: 'completedConversationRounds', label: ncfST('Frontend.AgentsManager.CompletedRounds'), unit: ncfST('Frontend.AgentsManager.Round') },
        { value: 'chattingCount', label: ncfST('Frontend.AgentsManager.Chat'), unit: ncfST('Frontend.AgentsManager.Agents') },
        { value: 'score', label: ncfST('Frontend.AgentsManager.Score'), unit: ncfST('Frontend.AgentsManager.Score') }
      ],
      agentGraphSnapshot: {
        agents: [],
        groups: [],
        links: [],
        collaborations: [],
        tasks: []
      },
      agentGraphPollingTimer: null,
      hoveredAgentGroupId: null,
      agentGraph3d: null,
      agentGraphFilterGroupId: null,
      agentGraphFilterTaskStatuses: [],
      agentGraphShowOnlyActiveGroup: false,
      agentGraphRequesting: false,
      agentGraphFocus: {
        groupId: null,
        locked: false
      },
      agentGraphFocusedAgent: null,
      agentGraphLastSignature: '',
      agentGraphLastRefreshAt: null,
      agentGraphLastRenderAt: null,
      agentGraphRenderCount: 0,
      quickJumpGroupId: null,
      quickJumpTaskId: null,
      quickJumpTaskOptions: [],
      hashChangeHandler: null,
      isApplyingHashRoute: false,
};

