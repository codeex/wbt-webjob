// 基于 LogicFlow v2 的工作流编辑器 - 使用全局变量方式
// 需要在HTML中先引入LogicFlow的CDN脚本

// 自定义节点类
class WorkflowNode extends LogicFlow.RectNode {
    getShape() {
        const { model } = this.props;
        const { x, y, width, height } = model;
        const properties = model.properties;

        return LogicFlow.h('g', {}, [
            LogicFlow.h('rect', {
                x: x - width / 2,
                y: y - height / 2,
                rx: 8,
                ry: 8,
                width,
                height,
                fill: properties.color || '#007bff',
                stroke: '#333',
                strokeWidth: 2
            }),
            LogicFlow.h('text', {
                fill: '#fff',
                fontSize: 32,
                textAnchor: 'middle',
                x,
                y: y - 5
            }, properties.icon || ''),
            LogicFlow.h('text', {
                fill: '#fff',
                fontSize: 12,
                textAnchor: 'middle',
                x,
                y: y + 25
            }, properties.label || '')
        ]);
    }
}

// 开始节点
class StartNode extends WorkflowNode {
    static extendKey = 'start';
}

// 触发器节点
class TriggerNode extends WorkflowNode {
    static extendKey = 'trigger';
}

// 事件节点
class EventNode extends WorkflowNode {
    static extendKey = 'event';
}

// HTTP授权节点
class HttpAuthNode extends WorkflowNode {
    static extendKey = 'httpAuth';
}

// HTTP处理节点
class HttpActionNode extends WorkflowNode {
    static extendKey = 'httpAction';
}

// 命令行节点
class CommandLineNode extends WorkflowNode {
    static extendKey = 'commandLine';
}

// 条件判断节点（菱形）
class ConditionNode extends LogicFlow.PolygonNode {
    static extendKey = 'condition';

    getShape() {
        const { model } = this.props;
        const { x, y, width, height } = model;
        const properties = model.properties;

        const points = [
            [x, y - height / 2],
            [x + width / 2, y],
            [x, y + height / 2],
            [x - width / 2, y]
        ];

        return LogicFlow.h('g', {}, [
            LogicFlow.h('polygon', {
                points: points.map(p => p.join(',')).join(' '),
                fill: '#fd7e14',
                stroke: '#333',
                strokeWidth: 2
            }),
            LogicFlow.h('text', {
                fill: '#fff',
                fontSize: 28,
                textAnchor: 'middle',
                x,
                y: y + 8
            }, '?')
        ]);
    }
}

// 结束节点
class EndNode extends WorkflowNode {
    static extendKey = 'end';
}

// 工作流编辑器主类
class WorkflowEditor {
    constructor() {
        this.lf = null;
        this.selectedNode = null;
        this.selectedEdge = null;
        this.nodeIdCounter = 0;
        this.connectionIdCounter = 0;

        // 节点类型配置
        this.nodeTypes = {
            start: { label: '开始', color: '#28a745', icon: '▶', category: 'input' },
            trigger: { label: '触发器', color: '#17a2b8', icon: '⏰', category: 'input' },
            event: { label: '事件', color: '#ffc107', icon: '🔔', category: 'input' },
            httpAuth: { label: 'HTTP授权', color: '#6f42c1', icon: '🔑', category: 'process' },
            httpAction: { label: 'HTTP处理', color: '#007bff', icon: '⇄', category: 'process' },
            commandLine: { label: '命令行', color: '#343a40', icon: '▣', category: 'process' },
            condition: { label: '条件判断', color: '#fd7e14', icon: '◆', category: 'process' },
            end: { label: '结束', color: '#dc3545', icon: '⬛', category: 'terminate' }
        };

        this.init();
    }

    init() {
        this.initLogicFlow();
        this.initEvents();
        this.loadWorkflow();
    }

    initLogicFlow() {
        const container = document.getElementById('workflowCanvas');
        if (!container) {
            console.error('找不到画布容器');
            return;
        }

        // 初始化 LogicFlow
        this.lf = new LogicFlow({
            container: container,
            width: container.clientWidth,
            height: container.clientHeight,
            grid: {
                size: 20,
                visible: true,
                type: 'dot',
                config: {
                    color: '#ababab',
                    thickness: 1,
                }
            },
            background: {
                color: '#f8f9fa'
            },
            keyboard: {
                enabled: true
            },
            snapline: true,
            history: true,
            partial: true,
            edgeType: 'bezier',
            isSilentMode: false,
            stopScrollGraph: false,
            stopZoomGraph: false,
            style: {
                rect: {
                    rx: 8,
                    ry: 8,
                    strokeWidth: 2
                },
                circle: {
                    fill: '#f5f5f5',
                    stroke: '#666'
                },
                nodeText: {
                    color: '#000',
                    overflowMode: 'autoWrap',
                    lineHeight: 1.5
                },
                edgeText: {
                    textWidth: 100,
                    overflowMode: 'autoWrap',
                    fontSize: 12,
                    background: {
                        fill: '#ffffff'
                    }
                }
            }
        });

        // 注册自定义节点
        this.lf.register(StartNode);
        this.lf.register(TriggerNode);
        this.lf.register(EventNode);
        this.lf.register(HttpAuthNode);
        this.lf.register(HttpActionNode);
        this.lf.register(CommandLineNode);
        this.lf.register(ConditionNode);
        this.lf.register(EndNode);

        // 设置默认边类型为贝塞尔曲线
        this.lf.setDefaultEdgeType('polyline');

        // 渲染画布
        this.lf.render();

        // 窗口大小调整
        window.addEventListener('resize', () => {
            if (this.lf && container) {
                this.lf.resize(container.clientWidth, container.clientHeight);
            }
        });
    }

    initEvents() {
        // 左右面板切换
        const toggleLeftBtn = document.getElementById('toggleLeftPanelBtn');
        if (toggleLeftBtn) {
            toggleLeftBtn.addEventListener('click', () => {
                document.getElementById('leftPanel').classList.toggle('collapsed');
                setTimeout(() => {
                    const container = document.getElementById('workflowCanvas');
                    if (this.lf && container) {
                        this.lf.resize(container.clientWidth, container.clientHeight);
                    }
                }, 300);
            });
        }

        const toggleRightBtn = document.getElementById('toggleRightPanelBtn');
        if (toggleRightBtn) {
            toggleRightBtn.addEventListener('click', () => {
                document.getElementById('rightPanel').classList.toggle('collapsed');
                setTimeout(() => {
                    const container = document.getElementById('workflowCanvas');
                    if (this.lf && container) {
                        this.lf.resize(container.clientWidth, container.clientHeight);
                    }
                }, 300);
            });
        }

        // 全屏切换
        const toggleFullscreenBtn = document.getElementById('toggleFullscreenBtn');
        if (toggleFullscreenBtn) {
            toggleFullscreenBtn.addEventListener('click', () => {
                const editorContainer = document.getElementById('workflowEditorContainer');
                editorContainer.classList.toggle('fullscreen');
                setTimeout(() => {
                    const container = document.getElementById('workflowCanvas');
                    if (this.lf && container) {
                        this.lf.resize(container.clientWidth, container.clientHeight);
                    }
                }, 100);
            });
        }

        // 工具栏按钮
        document.getElementById('zoomInBtn')?.addEventListener('click', () => this.zoomIn());
        document.getElementById('zoomOutBtn')?.addEventListener('click', () => this.zoomOut());
        document.getElementById('fitToScreenBtn')?.addEventListener('click', () => this.fitToScreen());
        document.getElementById('centerCanvasBtn')?.addEventListener('click', () => this.centerCanvas());
        document.getElementById('saveWorkflowBtn')?.addEventListener('click', () => this.saveWorkflow());

        // 拖拽组件到画布
        document.querySelectorAll('.component-item.draggable').forEach(item => {
            item.addEventListener('dragstart', (e) => {
                const nodeType = item.getAttribute('data-node-type');
                e.dataTransfer.setData('nodeType', nodeType);
            });
            item.setAttribute('draggable', 'true');
        });

        const centerPanel = document.getElementById('centerPanel');
        if (centerPanel) {
            centerPanel.addEventListener('dragover', (e) => {
                e.preventDefault();
            });

            centerPanel.addEventListener('drop', (e) => {
                e.preventDefault();
                const nodeType = e.dataTransfer.getData('nodeType');
                if (nodeType && this.lf) {
                    const point = this.lf.getPointByClient(e.clientX, e.clientY);
                    this.createNode(nodeType, point.x, point.y);
                }
            });
        }

        // LogicFlow 事件监听
        if (this.lf) {
            // 节点点击
            this.lf.on('node:click', ({ data }) => {
                this.selectedNode = data;
                this.selectedEdge = null;
                this.showProperties(data);
            });

            // 边点击
            this.lf.on('edge:click', ({ data }) => {
                this.selectedEdge = data;
                this.selectedNode = null;
                this.showEdgeProperties(data);
            });

            // 画布点击
            this.lf.on('blank:click', () => {
                this.selectedNode = null;
                this.selectedEdge = null;
                this.hideProperties();
            });

            // 节点删除
            this.lf.on('node:delete', ({ data }) => {
                if (this.selectedNode && this.selectedNode.id === data.id) {
                    this.hideProperties();
                }
            });

            // 边删除
            this.lf.on('edge:delete', ({ data }) => {
                if (this.selectedEdge && this.selectedEdge.id === data.id) {
                    this.hideProperties();
                }
            });
        }
    }

    createNode(nodeType, x, y) {
        const config = this.nodeTypes[nodeType];
        if (!config) return;

        const nodeId = `node_${++this.nodeIdCounter}`;
        const nodeName = `${config.label}${this.nodeIdCounter}`;

        const nodeConfig = {
            id: nodeId,
            type: nodeType,
            x: x,
            y: y,
            text: nodeName,
            properties: {
                nodeId: nodeId,
                nodeType: nodeType,
                nodeName: nodeName,
                label: nodeName,
                icon: config.icon,
                color: config.color,
                configuration: this.getDefaultConfiguration(nodeType)
            }
        };

        this.lf.addNode(nodeConfig);

        return nodeId;
    }

    getDefaultConfiguration(nodeType) {
        const defaults = {
            start: { name: '', variables: {} },
            trigger: { name: '', cronExpression: '0 0 * * *' },
            event: { name: '', eventTopic: '' },
            httpAuth: { name: '', authUrl: '', authType: 'None' },
            httpAction: { name: '', url: '', httpMethod: 'POST' },
            commandLine: { name: '', command: '' },
            condition: { name: '', conditionExpression: '' },
            end: { name: '' }
        };
        return defaults[nodeType] || {};
    }

    showProperties(nodeData) {
        const propertiesPanel = document.getElementById('propertiesPanel');
        if (!propertiesPanel) return;

        const nodeType = nodeData.properties?.nodeType || nodeData.type;
        const properties = nodeData.properties || {};

        const formHtml = this.generatePropertyForm(nodeType, properties);
        propertiesPanel.innerHTML = formHtml;

        this.bindPropertyFormEvents(nodeData);
    }

    showEdgeProperties(edgeData) {
        const propertiesPanel = document.getElementById('propertiesPanel');
        if (!propertiesPanel) return;

        // 获取源节点和目标节点信息
        const sourceNode = this.lf.getNodeModelById(edgeData.sourceNodeId);
        const targetNode = this.lf.getNodeModelById(edgeData.targetNodeId);

        let html = `<div class="property-form">`;
        html += `<div class="property-section">`;
        html += `<div class="property-section-title">连接线属性</div>`;

        html += `<div class="form-group">`;
        html += `<label>连接线名称</label>`;
        html += `<input type="text" class="form-control" id="edgeName" value="${edgeData.text?.value || ''}" placeholder="输入连接线名称" />`;
        html += `</div>`;

        html += `<div class="form-group">`;
        html += `<label>起始节点</label>`;
        html += `<input type="text" class="form-control" value="${sourceNode?.text?.value || ''}" readonly />`;
        html += `<small class="form-text text-muted">ID: ${edgeData.sourceNodeId}</small>`;
        html += `</div>`;

        html += `<div class="form-group">`;
        html += `<label>目标节点</label>`;
        html += `<input type="text" class="form-control" value="${targetNode?.text?.value || ''}" readonly />`;
        html += `<small class="form-text text-muted">ID: ${edgeData.targetNodeId}</small>`;
        html += `</div>`;

        html += `<div class="form-group">`;
        html += `<label>连接点</label>`;
        html += `<div class="row">`;
        html += `<div class="col-6">`;
        html += `<small>起始: ${edgeData.sourceAnchorId || 'default'}</small>`;
        html += `</div>`;
        html += `<div class="col-6">`;
        html += `<small>目标: ${edgeData.targetAnchorId || 'default'}</small>`;
        html += `</div>`;
        html += `</div>`;
        html += `</div>`;

        html += `</div>`;
        html += `<button class="btn btn-danger btn-sm mt-3" id="deleteEdgeBtn">删除连接线</button>`;
        html += `</div>`;

        propertiesPanel.innerHTML = html;

        // 绑定事件
        const edgeNameInput = document.getElementById('edgeName');
        if (edgeNameInput) {
            edgeNameInput.addEventListener('change', (e) => {
                this.lf.updateText(edgeData.id, e.target.value);
            });
        }

        const deleteEdgeBtn = document.getElementById('deleteEdgeBtn');
        if (deleteEdgeBtn) {
            deleteEdgeBtn.addEventListener('click', () => {
                if (confirm('确定要删除此连接线吗?')) {
                    this.lf.deleteEdge(edgeData.id);
                    this.hideProperties();
                }
            });
        }
    }

    hideProperties() {
        const propertiesPanel = document.getElementById('propertiesPanel');
        if (!propertiesPanel) return;

        propertiesPanel.innerHTML = `
            <div class="no-selection-message">
                <i class="bi bi-info-circle"></i>
                <p>请选择一个节点或连接线以编辑其属性</p>
            </div>
        `;
    }

    generatePropertyForm(nodeType, properties) {
        const configuration = properties.configuration || {};

        let html = `<div class="property-form">`;
        html += `<div class="property-section">`;
        html += `<div class="property-section-title">基本信息</div>`;
        html += `<div class="form-group">`;
        html += `<label>节点名称</label>`;
        html += `<input type="text" class="form-control" id="propName" value="${configuration.name || properties.nodeName || ''}" />`;
        html += `<small class="form-text text-muted">名称不能包含点(.)字符</small>`;
        html += `</div>`;
        html += `</div>`;

        html += this.generateTypeSpecificProperties(nodeType, configuration);

        html += `<button class="btn btn-danger btn-sm mt-3" id="deleteNodeBtn">删除节点</button>`;
        html += `</div>`;

        return html;
    }

    generateTypeSpecificProperties(nodeType, configuration) {
        let html = `<div class="property-section">`;
        html += `<div class="property-section-title">节点配置</div>`;

        switch (nodeType) {
            case 'trigger':
                html += `<div class="form-group">`;
                html += `<label>Cron表达式</label>`;
                html += `<input type="text" class="form-control" id="propCronExpression" value="${configuration.cronExpression || ''}" />`;
                html += `<small class="expression-hint">示例: 0 0 * * * (每天午夜执行)</small>`;
                html += `</div>`;
                break;
            case 'event':
                html += `<div class="form-group">`;
                html += `<label>事件主题</label>`;
                html += `<input type="text" class="form-control" id="propEventTopic" value="${configuration.eventTopic || ''}" />`;
                html += `</div>`;
                break;
            case 'httpAuth':
            case 'httpAction':
                html += `<div class="form-group">`;
                html += `<label>URL</label>`;
                html += `<input type="text" class="form-control" id="propUrl" value="${configuration.url || configuration.authUrl || ''}" />`;
                html += `</div>`;
                html += `<div class="form-group">`;
                html += `<label>HTTP方法</label>`;
                html += `<select class="form-select" id="propHttpMethod">`;
                html += `<option value="GET" ${configuration.httpMethod === 'GET' ? 'selected' : ''}>GET</option>`;
                html += `<option value="POST" ${configuration.httpMethod === 'POST' ? 'selected' : ''}>POST</option>`;
                html += `<option value="PUT" ${configuration.httpMethod === 'PUT' ? 'selected' : ''}>PUT</option>`;
                html += `<option value="DELETE" ${configuration.httpMethod === 'DELETE' ? 'selected' : ''}>DELETE</option>`;
                html += `</select>`;
                html += `</div>`;
                if (nodeType === 'httpAuth') {
                    html += `<div class="form-group">`;
                    html += `<label>授权类型</label>`;
                    html += `<select class="form-select" id="propAuthType">`;
                    html += `<option value="None" ${configuration.authType === 'None' ? 'selected' : ''}>None</option>`;
                    html += `<option value="Basic" ${configuration.authType === 'Basic' ? 'selected' : ''}>Basic</option>`;
                    html += `<option value="Bearer" ${configuration.authType === 'Bearer' ? 'selected' : ''}>Bearer</option>`;
                    html += `<option value="ApiKey" ${configuration.authType === 'ApiKey' ? 'selected' : ''}>ApiKey</option>`;
                    html += `</select>`;
                    html += `</div>`;
                }
                html += `<div class="form-group">`;
                html += `<label>断言表达式</label>`;
                html += `<input type="text" class="form-control" id="propAssertionExpression" value="${configuration.assertionExpression || ''}" />`;
                html += `<small class="expression-hint">示例: \${response.status} == 200</small>`;
                html += `</div>`;
                break;
            case 'commandLine':
                html += `<div class="form-group">`;
                html += `<label>命令</label>`;
                html += `<textarea class="form-control" id="propCommand" rows="3">${configuration.command || ''}</textarea>`;
                html += `</div>`;
                break;
            case 'condition':
                html += `<div class="form-group">`;
                html += `<label>条件表达式</label>`;
                html += `<input type="text" class="form-control" id="propConditionExpression" value="${configuration.conditionExpression || ''}" />`;
                html += `<small class="expression-hint">使用 \${组件名.属性} 引用其他节点的输出</small>`;
                html += `</div>`;
                break;
        }

        html += `</div>`;
        return html;
    }

    bindPropertyFormEvents(nodeData) {
        const inputs = document.querySelectorAll('#propertiesPanel input, #propertiesPanel select, #propertiesPanel textarea');
        inputs.forEach(input => {
            input.addEventListener('change', (e) => {
                this.updateNodeData(nodeData, e.target.id, e.target.value);
            });
        });

        const deleteBtn = document.getElementById('deleteNodeBtn');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', () => {
                if (confirm('确定要删除此节点吗?')) {
                    this.lf.deleteNode(nodeData.id);
                    this.hideProperties();
                }
            });
        }
    }

    updateNodeData(nodeData, propertyId, value) {
        const propertyName = propertyId.replace('prop', '');
        const key = propertyName.charAt(0).toLowerCase() + propertyName.slice(1);

        if (!nodeData.properties) {
            nodeData.properties = {};
        }

        if (!nodeData.properties.configuration) {
            nodeData.properties.configuration = {};
        }

        nodeData.properties.configuration[key] = value;

        if (key === 'name') {
            if (value.includes('.')) {
                alert('节点名称不能包含点(.)字符');
                return;
            }
            nodeData.properties.nodeName = value;
            nodeData.properties.label = value;
            this.lf.updateText(nodeData.id, value);
        }

        // 更新节点模型
        this.lf.setProperties(nodeData.id, nodeData.properties);
    }

    deleteSelected() {
        if (this.selectedNode) {
            if (confirm('确定要删除选中的节点吗?')) {
                this.lf.deleteNode(this.selectedNode.id);
                this.hideProperties();
            }
        } else if (this.selectedEdge) {
            if (confirm('确定要删除选中的连接线吗?')) {
                this.lf.deleteEdge(this.selectedEdge.id);
                this.hideProperties();
            }
        }
    }

    zoomIn() {
        this.lf.zoom(true);
        this.updateZoomLevel();
    }

    zoomOut() {
        this.lf.zoom(false);
        this.updateZoomLevel();
    }

    updateZoomLevel() {
        const transform = this.lf.getTransform();
        const zoom = transform ? (transform.SCALE_X || transform.scaleX || 1) : 1;
        const zoomLevelEl = document.getElementById('zoomLevel');
        if (zoomLevelEl) {
            zoomLevelEl.textContent = `${Math.round(zoom * 100)}%`;
        }
    }

    fitToScreen() {
        this.lf.fitView();
        this.updateZoomLevel();
    }

    centerCanvas() {
        const graphData = this.lf.getGraphData();
        if (graphData.nodes.length > 0) {
            this.lf.fitView();
        }
    }

    saveWorkflow() {
        const workflowName = document.getElementById('workflowName').value.trim();
        const workflowJobType = document.getElementById('workflowJobType').value.trim();
        const workflowDescription = document.getElementById('workflowDescription').value.trim();
        const customJobId = document.getElementById('customJobId').value;

        if (!workflowName) {
            alert('请输入工作流名称');
            return;
        }

        if (!workflowJobType) {
            alert('请输入任务类型');
            return;
        }

        const graphData = this.lf.getGraphData();

        // 转换节点数据
        const nodesData = graphData.nodes.map(node => ({
            nodeId: node.id,
            nodeType: node.properties?.nodeType || node.type,
            name: node.properties?.nodeName || node.text?.value || '',
            positionX: node.x,
            positionY: node.y,
            configuration: JSON.stringify(node.properties?.configuration || {})
        }));

        // 转换连接数据
        const connectionsData = graphData.edges.map(edge => ({
            connectionId: edge.id,
            sourceNodeId: edge.sourceNodeId,
            targetNodeId: edge.targetNodeId,
            sourcePort: edge.sourceAnchorId || 'default'
        }));

        const workflowData = {
            customJobId: customJobId || '00000000-0000-0000-0000-000000000000',
            name: workflowName,
            jobType: workflowJobType,
            description: workflowDescription,
            nodes: nodesData,
            connections: connectionsData
        };

        fetch('/api/customjobs/workflow', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(workflowData)
        })
        .then(response => {
            if (!response.ok) throw new Error('保存失败');
            return response.json();
        })
        .then(data => {
            alert('工作流保存成功！');
            if (!customJobId || customJobId === '00000000-0000-0000-0000-000000000000') {
                window.location.href = `/CustomJobs/WorkflowEditor/${data.customJobId}`;
            }
        })
        .catch(error => {
            console.error('保存错误:', error);
            alert('保存失败: ' + error.message);
        });
    }

    loadWorkflow() {
        const customJobId = document.getElementById('customJobId')?.value;
        if (!customJobId || customJobId === '00000000-0000-0000-0000-000000000000') {
            return;
        }

        fetch(`/api/customjobs/workflow/${customJobId}`)
            .then(response => {
                if (!response.ok) throw new Error('加载失败');
                return response.json();
            })
            .then(data => {
                const nodes = [];
                const edges = [];

                // 加载节点
                data.nodes.forEach(nodeData => {
                    const config = JSON.parse(nodeData.configuration || '{}');
                    const nodeTypeConfig = this.nodeTypes[nodeData.nodeType];

                    nodes.push({
                        id: nodeData.nodeId,
                        type: nodeData.nodeType,
                        x: nodeData.positionX,
                        y: nodeData.positionY,
                        text: nodeData.name,
                        properties: {
                            nodeId: nodeData.nodeId,
                            nodeType: nodeData.nodeType,
                            nodeName: nodeData.name,
                            label: nodeData.name,
                            icon: nodeTypeConfig?.icon,
                            color: nodeTypeConfig?.color,
                            configuration: config
                        }
                    });

                    // 更新计数器
                    const idNum = parseInt(nodeData.nodeId.replace('node_', ''));
                    if (!isNaN(idNum) && idNum > this.nodeIdCounter) {
                        this.nodeIdCounter = idNum;
                    }
                });

                // 加载连接
                data.connections.forEach(connData => {
                    edges.push({
                        id: connData.connectionId,
                        type: 'polyline',
                        sourceNodeId: connData.sourceNodeId,
                        targetNodeId: connData.targetNodeId,
                        sourceAnchorId: connData.sourcePort
                    });

                    // 更新计数器
                    const idNum = parseInt(connData.connectionId.replace('conn_', ''));
                    if (!isNaN(idNum) && idNum > this.connectionIdCounter) {
                        this.connectionIdCounter = idNum;
                    }
                });

                this.lf.render({ nodes, edges });
                setTimeout(() => {
                    this.fitToScreen();
                }, 100);
            })
            .catch(error => {
                console.error('加载错误:', error);
            });
    }
}

// 初始化编辑器
document.addEventListener('DOMContentLoaded', () => {
    // 确保 LogicFlow 已加载
    if (typeof LogicFlow !== 'undefined') {
        window.workflowEditor = new WorkflowEditor();
    } else {
        console.error('LogicFlow 未加载，请检查脚本引用');
    }
});
