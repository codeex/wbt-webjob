// 基于 LogicFlow v2 的工作流编辑器 - 使用全局变量方式
// 需要在HTML中先引入LogicFlow的CDN脚本

// 兼容 shim：一些 logicflow 构建会导出为 Core 或 default，确保全局有 LogicFlow 变量
const { LogicFlow, RectNode, RectNodeModel, PolygonNode, PolygonNodeModel, h } = window.Core;

// 尝试从扩展中获取 MiniMap
const MiniMap = window.MiniMapPlugin || window.LogicFlowExtension?.MiniMap || null;

// 自定义节点类
class BaseModel extends RectNodeModel { }
class WorkflowNode extends RectNode {
    getShape() {
        const { model } = this.props;
        const { x, y, width, height } = model;
        const properties = model.properties || {};
        return h('g', {}, [
            h('rect', {
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
            h('text', {
                fill: '#fff',
                fontSize: 32,
                textAnchor: 'middle',
                x,
                y: y - 5
            }, properties.icon || ''),
            h('text', {
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
class StartModel extends BaseModel {
   
}

// 触发器节点
class TriggerNode extends WorkflowNode {
    static extendKey = 'trigger';
}
class TriggerModel extends BaseModel {  }
// 事件节点
class EventNode extends WorkflowNode {
    static extendKey = 'event';
}
class EventModel extends BaseModel {  }

// HTTP授权节点
class HttpAuthNode extends WorkflowNode {
    static extendKey = 'httpAuth';
}
class HttpAuthModel extends BaseModel { }

class HttpActionModel extends BaseModel {  }
// HTTP处理节点

class HttpActionNode extends WorkflowNode {
    static extendKey = 'httpAction';
}

class CommandLineModel extends BaseModel {  }
// 命令行节点
class CommandLineNode extends WorkflowNode {
    static extendKey = 'commandLine';
}

class EndModel extends BaseModel {  }
// 条件判断节点（菱形）
class ConditionNode extends PolygonNode {
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

        return h('g', {}, [
            h('polygon', {
                points: points.map(p => p.join(',')).join(' '),
                fill: '#fd7e14',
                stroke: '#333',
                strokeWidth: 2
            }),
            h('text', {
                fill: '#fff',
                fontSize: 28,
                textAnchor: 'middle',
                x,
                y: y + 8
            }, '?')
        ]);
    }
}

class ConditionModel extends (PolygonNodeModel || RectNodeModel) {  }

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
        this.miniMapVisible = false;
        this.currentEdgeType = 'polyline';

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
    clientToGraphPoint(clientX, clientY) {
        // 优先使用 SVG 的 createSVGPoint + getScreenCTM 反变换（准确）
        try {
            const container = this.lf && (this.lf.container || (this.lf.getGraphDom && this.lf.getGraphDom()));
            if (container) {
                const svg = container.querySelector && container.querySelector('svg');
                if (svg && typeof svg.createSVGPoint === 'function') {
                    const pt = svg.createSVGPoint();
                    pt.x = clientX;
                    pt.y = clientY;
                    const ctm = svg.getScreenCTM && svg.getScreenCTM();
                    if (ctm) {
                        const inv = ctm.inverse();
                        const p = pt.matrixTransform(inv);
                        return { x: p.x, y: p.y };
                    }
                }

                // 如果没有 SVG，退回到容器坐标并尝试考虑 LogicFlow 的 transform/zoom
                const rect = container.getBoundingClientRect();
                let x = clientX - rect.left;
                let y = clientY - rect.top;

                // 尝试读取 lf 的 transform / zoom 信息
                let scale = 1, tx = 0, ty = 0;
                try {
                    const t = (this.lf && (this.lf.getTransform ? this.lf.getTransform() : (this.lf.graphModel && this.lf.graphModel.transform)));
                    if (t) {
                        scale = t.SCALE_X || t.scaleX || t.scale || 1;
                        tx = t.TRANSLATE_X || t.translateX || t.tx || 0;
                        ty = t.TRANSLATE_Y || t.translateY || t.ty || 0;
                    } else if (this.lf && typeof this.lf.getZoom === 'function') {
                        scale = this.lf.getZoom() || 1;
                    }
                } catch (e) {
                    // ignore
                }

                // 将容器坐标转换为图坐标（考虑平移和缩放）
                return { x: (x - tx) / scale, y: (y - ty) / scale };
            }
        } catch (e) {
            console.warn('clientToGraphPoint 失败，退回使用 client 坐标：', e);
        }
        return { x: clientX, y: clientY };
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
        try {
            this.lf.register({ type: 'start', model: StartModel, view: StartNode });
            this.lf.register({ type: 'trigger', model: TriggerModel, view: TriggerNode });
            this.lf.register({ type: 'event', model: EventModel, view: EventNode });
            this.lf.register({ type: 'httpAuth', model: HttpAuthModel, view: HttpAuthNode });
            this.lf.register({ type: 'httpAction', model: HttpActionModel, view: HttpActionNode });
            this.lf.register({ type: 'commandLine', model: CommandLineModel, view: CommandLineNode });
            this.lf.register({ type: 'condition', model: ConditionModel, view: ConditionNode });
            this.lf.register({ type: 'end', model: EndModel, view: EndNode });
        } catch (err) {
            console.error('注册自定义节点失败：', err);
        }

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

        // 保存按钮 - 打开模态框
        document.getElementById('saveWorkflowBtn')?.addEventListener('click', () => this.showSaveModal());

        // 模态框确认保存按钮
        document.getElementById('confirmSaveBtn')?.addEventListener('click', () => this.saveWorkflow());

        // 撤销/重做
        document.getElementById('undoBtn')?.addEventListener('click', () => this.undo());
        document.getElementById('redoBtn')?.addEventListener('click', () => this.redo());

        // 删除选中
        document.getElementById('deleteSelectedBtn')?.addEventListener('click', () => this.deleteSelected());

        // 自动布局
        document.getElementById('autoLayoutBtn')?.addEventListener('click', () => this.autoLayout());

        // 创建分组
        document.getElementById('createGroupBtn')?.addEventListener('click', () => this.createGroup());

        // 导出功能
        document.getElementById('exportPngBtn')?.addEventListener('click', (e) => {
            e.preventDefault();
            this.exportToPng();
        });
        document.getElementById('exportSvgBtn')?.addEventListener('click', (e) => {
            e.preventDefault();
            this.exportToSvg();
        });

        // 小地图切换
        document.getElementById('toggleMiniMapBtn')?.addEventListener('click', () => this.toggleMiniMap());

        // 连接线样式切换
        document.querySelectorAll('.edge-type-option').forEach(option => {
            option.addEventListener('click', (e) => {
                e.preventDefault();
                const edgeType = option.getAttribute('data-edge-type');
                this.changeEdgeType(edgeType);
            });
        });

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
                if (!nodeType || !this.lf) return;

                // 计算图坐标
                let pt = null;
                try {
                    pt = this.clientToGraphPoint(e.clientX, e.clientY);
                } catch (err) {
                    console.warn('计算图坐标失败，使用容器相对坐标：', err);
                }

                if (!pt) {
                    const rect = (this.lf && this.lf.container) ? this.lf.container.getBoundingClientRect() : document.getElementById('workflowCanvas').getBoundingClientRect();
                    pt = { x: e.clientX - rect.left, y: e.clientY - rect.top };
                }

                try {
                    this.createNode(nodeType, pt.x, pt.y);
                } catch (err) {
                    console.error('drop 创建节点失败：', err);
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

    // 撤销
    undo() {
        if (this.lf && typeof this.lf.undo === 'function') {
            this.lf.undo();
        }
    }

    // 重做
    redo() {
        if (this.lf && typeof this.lf.redo === 'function') {
            this.lf.redo();
        }
    }

    // 显示保存模态框
    showSaveModal() {
        const modal = new bootstrap.Modal(document.getElementById('saveWorkflowModal'));
        modal.show();
    }

    // 切换小地图
    toggleMiniMap() {
        this.miniMapVisible = !this.miniMapVisible;
        const minimapContainer = document.getElementById('minimapContainer');

        if (this.miniMapVisible) {
            minimapContainer.style.display = 'block';
            this.initMiniMap();
        } else {
            minimapContainer.style.display = 'none';
        }
    }

    // 初始化小地图
    initMiniMap() {
        const minimapContainer = document.getElementById('minimapContainer');
        if (!minimapContainer || !this.lf) return;

        // 清空容器
        minimapContainer.innerHTML = '';

        // 创建简单的小地图（使用缩小的SVG副本）
        try {
            const graphData = this.lf.getGraphData();
            const canvas = document.getElementById('workflowCanvas');
            const svg = canvas.querySelector('svg');

            if (svg) {
                // 克隆SVG
                const clone = svg.cloneNode(true);
                clone.style.width = '100%';
                clone.style.height = '100%';
                clone.style.pointerEvents = 'none';
                minimapContainer.appendChild(clone);
            }
        } catch (error) {
            console.error('初始化小地图失败:', error);
            minimapContainer.innerHTML = '<div style="padding: 10px; text-align: center; color: #999;">小地图不可用</div>';
        }
    }

    // 自动布局
    autoLayout() {
        const graphData = this.lf.getGraphData();
        if (!graphData || graphData.nodes.length === 0) {
            alert('画布上没有节点');
            return;
        }

        // 简单的自动布局算法 - 垂直排列
        const startX = 300;
        const startY = 100;
        const horizontalGap = 250;
        const verticalGap = 150;

        // 按类型分组节点
        const nodesByType = {
            input: [],
            process: [],
            terminate: []
        };

        graphData.nodes.forEach(node => {
            const nodeType = node.properties?.nodeType || node.type;
            const config = this.nodeTypes[nodeType];
            if (config) {
                const category = config.category;
                if (nodesByType[category]) {
                    nodesByType[category].push(node);
                }
            }
        });

        // 布局各个层级
        let currentY = startY;

        // 输入节点层
        if (nodesByType.input.length > 0) {
            this.layoutNodesInRow(nodesByType.input, startX, currentY, horizontalGap);
            currentY += verticalGap;
        }

        // 处理节点层
        if (nodesByType.process.length > 0) {
            this.layoutNodesInRow(nodesByType.process, startX, currentY, horizontalGap);
            currentY += verticalGap;
        }

        // 终止节点层
        if (nodesByType.terminate.length > 0) {
            this.layoutNodesInRow(nodesByType.terminate, startX, currentY, horizontalGap);
        }

        // 适应屏幕
        setTimeout(() => this.fitToScreen(), 100);
    }

    layoutNodesInRow(nodes, startX, y, gap) {
        const totalWidth = (nodes.length - 1) * gap;
        let x = startX - totalWidth / 2;

        nodes.forEach(node => {
            this.lf.setProperties(node.id, {
                ...node.properties,
                x: x,
                y: y
            });
            // 更新节点位置
            const nodeModel = this.lf.getNodeModelById(node.id);
            if (nodeModel) {
                nodeModel.x = x;
                nodeModel.y = y;
                nodeModel.moveTo(x, y);
            }
            x += gap;
        });
    }

    // 创建分组
    createGroup() {
        const graphData = this.lf.getGraphData();
        const selectedNodes = graphData.nodes.filter(node => {
            const element = this.lf.getNodeModelById(node.id);
            return element && element.isSelected;
        });

        if (selectedNodes.length < 2) {
            alert('请至少选择两个节点来创建分组');
            return;
        }

        // 计算边界
        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
        selectedNodes.forEach(node => {
            const width = 120;
            const height = 60;
            minX = Math.min(minX, node.x - width / 2);
            minY = Math.min(minY, node.y - height / 2);
            maxX = Math.max(maxX, node.x + width / 2);
            maxY = Math.max(maxY, node.y + height / 2);
        });

        const padding = 30;
        const groupWidth = maxX - minX + padding * 2;
        const groupHeight = maxY - minY + padding * 2;
        const groupX = (minX + maxX) / 2;
        const groupY = (minY + maxY) / 2;

        // 创建分组（使用rect节点模拟）
        try {
            this.lf.addNode({
                type: 'rect',
                x: groupX,
                y: groupY,
                text: '分组',
                properties: {
                    width: groupWidth,
                    height: groupHeight,
                    style: {
                        fill: 'rgba(135, 206, 250, 0.1)',
                        stroke: '#87CEEB',
                        strokeWidth: 2,
                        strokeDasharray: '5,5'
                    }
                }
            });
            alert('分组创建成功');
        } catch (error) {
            console.error('创建分组失败:', error);
            alert('创建分组失败，LogicFlow可能不支持此功能');
        }
    }

    // 切换连接线样式
    changeEdgeType(edgeType) {
        this.currentEdgeType = edgeType;
        this.lf.setDefaultEdgeType(edgeType);

        // 更新标签文本
        const labels = {
            'polyline': '折线',
            'bezier': '贝塞尔曲线',
            'line': '直线'
        };
        const label = document.getElementById('edgeTypeLabel');
        if (label) {
            label.textContent = labels[edgeType] || edgeType;
        }

        alert(`连接线样式已切换为: ${labels[edgeType]}`);
    }

    // 导出为PNG
    exportToPng() {
        const canvas = document.getElementById('workflowCanvas');
        const svg = canvas.querySelector('svg');

        if (!svg) {
            alert('无法找到画布SVG元素');
            return;
        }

        try {
            // 获取SVG的尺寸
            const bbox = svg.getBBox();
            const width = bbox.width + 100;
            const height = bbox.height + 100;

            // 创建临时canvas
            const tempCanvas = document.createElement('canvas');
            tempCanvas.width = width;
            tempCanvas.height = height;
            const ctx = tempCanvas.getContext('2d');

            // 设置白色背景
            ctx.fillStyle = '#ffffff';
            ctx.fillRect(0, 0, width, height);

            // 将SVG转换为图片
            const svgData = new XMLSerializer().serializeToString(svg);
            const img = new Image();
            const svgBlob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
            const url = URL.createObjectURL(svgBlob);

            img.onload = () => {
                ctx.drawImage(img, 50, 50);
                URL.revokeObjectURL(url);

                // 下载PNG
                tempCanvas.toBlob((blob) => {
                    const link = document.createElement('a');
                    link.download = 'workflow-' + new Date().getTime() + '.png';
                    link.href = URL.createObjectURL(blob);
                    link.click();
                    URL.revokeObjectURL(link.href);
                });
            };

            img.src = url;
        } catch (error) {
            console.error('导出PNG失败:', error);
            alert('导出PNG失败: ' + error.message);
        }
    }

    // 导出为SVG
    exportToSvg() {
        const canvas = document.getElementById('workflowCanvas');
        const svg = canvas.querySelector('svg');

        if (!svg) {
            alert('无法找到画布SVG元素');
            return;
        }

        try {
            // 克隆SVG以避免修改原始SVG
            const clonedSvg = svg.cloneNode(true);

            // 设置SVG属性
            clonedSvg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
            clonedSvg.setAttribute('xmlns:xlink', 'http://www.w3.org/1999/xlink');

            // 序列化SVG
            const svgData = new XMLSerializer().serializeToString(clonedSvg);
            const svgBlob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });

            // 下载SVG
            const link = document.createElement('a');
            link.download = 'workflow-' + new Date().getTime() + '.svg';
            link.href = URL.createObjectURL(svgBlob);
            link.click();
            URL.revokeObjectURL(link.href);
        } catch (error) {
            console.error('导出SVG失败:', error);
            alert('导出SVG失败: ' + error.message);
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
                // 关闭模态框
                const modal = bootstrap.Modal.getInstance(document.getElementById('saveWorkflowModal'));
                if (modal) {
                    modal.hide();
                }

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

