// 工作流编辑器
class WorkflowEditor {
    constructor() {
        this.canvas = null;
        this.nodes = new Map(); // nodeId => fabric object
        this.connections = new Map(); // connectionId => fabric line
        this.selectedNode = null;
        this.currentMode = 'select'; // select, connect
        this.connectingFrom = null; // 正在连接的源节点
        this.zoom = 1;
        this.nodeIdCounter = 0;
        this.connectionIdCounter = 0;

        // 节点类型配置（使用Bootstrap Icons的Unicode）
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
        this.initCanvas();
        this.initEvents();
        this.loadWorkflow();
    }

    initCanvas() {
        const canvasEl = document.getElementById('workflowCanvas');
        const container = document.getElementById('centerPanel');

        // 设置画布大小
        canvasEl.width = container.clientWidth;
        canvasEl.height = container.clientHeight;

        // 初始化Fabric画布
        this.canvas = new fabric.Canvas('workflowCanvas', {
            selection: true,
            backgroundColor: 'transparent'
        });

        // 窗口大小调整时重新设置画布大小
        window.addEventListener('resize', () => {
            this.canvas.setWidth(container.clientWidth);
            this.canvas.setHeight(container.clientHeight);
            this.canvas.renderAll();
        });
    }

    initEvents() {
        // 左右面板切换
        document.getElementById('toggleLeftPanelBtn').addEventListener('click', () => {
            document.getElementById('leftPanel').classList.toggle('collapsed');
            setTimeout(() => {
                this.canvas.setWidth(document.getElementById('centerPanel').clientWidth);
                this.canvas.setHeight(document.getElementById('centerPanel').clientHeight);
                this.canvas.renderAll();
            }, 300);
        });

        document.getElementById('toggleRightPanelBtn').addEventListener('click', () => {
            document.getElementById('rightPanel').classList.toggle('collapsed');
            setTimeout(() => {
                this.canvas.setWidth(document.getElementById('centerPanel').clientWidth);
                this.canvas.setHeight(document.getElementById('centerPanel').clientHeight);
                this.canvas.renderAll();
            }, 300);
        });

        // 全屏切换
        document.getElementById('toggleFullscreenBtn').addEventListener('click', () => {
            const container = document.getElementById('workflowEditorContainer');
            container.classList.toggle('fullscreen');
            setTimeout(() => {
                this.canvas.setWidth(document.getElementById('centerPanel').clientWidth);
                this.canvas.setHeight(document.getElementById('centerPanel').clientHeight);
                this.canvas.renderAll();
            }, 100);
        });

        // 画布控制按钮
        document.getElementById('zoomInBtn').addEventListener('click', () => this.zoomIn());
        document.getElementById('zoomOutBtn').addEventListener('click', () => this.zoomOut());
        document.getElementById('fitToScreenBtn').addEventListener('click', () => this.fitToScreen());
        document.getElementById('centerCanvasBtn').addEventListener('click', () => this.centerCanvas());

        // 保存按钮
        document.getElementById('saveWorkflowBtn').addEventListener('click', () => this.saveWorkflow());

        // 操作工具选择
        document.querySelectorAll('.component-item[data-category="action"]').forEach(item => {
            item.addEventListener('click', (e) => {
                const nodeType = item.getAttribute('data-node-type');
                this.setMode(nodeType);

                // 更新选中状态
                document.querySelectorAll('.component-item[data-category="action"]').forEach(i => {
                    i.classList.remove('active');
                });
                item.classList.add('active');
            });
        });

        // 可拖放组件
        document.querySelectorAll('.component-item.draggable').forEach(item => {
            item.addEventListener('dragstart', (e) => {
                const nodeType = item.getAttribute('data-node-type');
                e.dataTransfer.setData('nodeType', nodeType);
                e.dataTransfer.effectAllowed = 'copy';
            });
            item.setAttribute('draggable', 'true');
        });

        // 画布拖放
        const centerPanel = document.getElementById('centerPanel');
        centerPanel.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
        });

        centerPanel.addEventListener('drop', (e) => {
            e.preventDefault();
            const nodeType = e.dataTransfer.getData('nodeType');
            if (nodeType) {
                const rect = this.canvas.upperCanvasEl.getBoundingClientRect();
                const x = (e.clientX - rect.left) / this.zoom;
                const y = (e.clientY - rect.top) / this.zoom;
                this.createNode(nodeType, x, y);
            }
        });

        // 画布事件
        this.canvas.on('selection:created', (e) => this.onNodeSelected(e));
        this.canvas.on('selection:updated', (e) => this.onNodeSelected(e));
        this.canvas.on('selection:cleared', () => this.onNodeDeselected());
        this.canvas.on('mouse:down', (e) => this.onCanvasMouseDown(e));
        this.canvas.on('mouse:move', (e) => this.onCanvasMouseMove(e));
    }

    setMode(mode) {
        this.currentMode = mode;
        this.connectingFrom = null;

        // 更新鼠标样式
        if (mode === 'connect') {
            this.canvas.defaultCursor = 'crosshair';
        } else {
            this.canvas.defaultCursor = 'default';
        }
    }

    createNode(nodeType, x, y) {
        const config = this.nodeTypes[nodeType];
        if (!config) return;

        const nodeId = `node_${++this.nodeIdCounter}`;
        const nodeName = `${config.label}${this.nodeIdCounter}`;

        const nodeWidth = 120;
        const nodeHeight = 100;

        // 创建节点矩形
        const rect = new fabric.Rect({
            width: nodeWidth,
            height: nodeHeight,
            fill: config.color,
            stroke: '#333',
            strokeWidth: 2,
            rx: 8,
            ry: 8,
            originX: 'center',
            originY: 'center'
        });

        // 创建图标
        const icon = new fabric.Text(config.icon, {
            fontSize: 32,
            fill: '#fff',
            fontFamily: 'Arial',
            originX: 'center',
            originY: 'center',
            top: -15
        });

        // 创建节点标签（在图标下方）
        const label = new fabric.Text(nodeName, {
            fontSize: 12,
            fill: '#fff',
            fontFamily: 'Arial',
            originX: 'center',
            originY: 'center',
            top: 25
        });

        // 创建节点组
        const group = new fabric.Group([rect, icon, label], {
            left: x,
            top: y,
            selectable: true,
            hasControls: false,
            hasBorders: true,
            borderColor: '#007bff',
            borderScaleFactor: 2,
            padding: 0,
            nodeId: nodeId,
            nodeType: nodeType,
            nodeName: nodeName,
            nodeData: {
                name: nodeName,
                configuration: this.getDefaultConfiguration(nodeType)
            }
        });

        // 添加连接点
        this.addConnectionPoints(group, nodeType, nodeWidth, nodeHeight);

        // 添加到画布
        this.canvas.add(group);
        this.nodes.set(nodeId, group);
        this.canvas.renderAll();

        return group;
    }

    addConnectionPoints(group, nodeType, nodeWidth, nodeHeight) {
        const config = this.nodeTypes[nodeType];
        const category = config.category;

        const halfWidth = nodeWidth / 2;
        const halfHeight = nodeHeight / 2;

        // 输入点（除了输入组，其他都有） - 在右边边缘
        if (category == 'input') {
            const outputPort = new fabric.Circle({
                radius: 6,
                fill: '#fff',
                stroke: '#333',
                strokeWidth: 2,
                left: group.get("left") + nodeWidth,
                top: group.get("top") + halfHeight,   // 右边缘
                originX: 'center',
                originY: 'center',
                portType: 'input',
                selectable: false
            });
            group.addWithUpdate(outputPort);
        }
        else if (category == 'terminate') {
            const inputPort = new fabric.Circle({
                radius: 6,
                fill: '#fff',
                stroke: '#333',
                strokeWidth: 2,
                left: group.get("left"),
                top: group.get("top") + halfHeight,
                originX: 'center',
                originY: 'center',
                portType: 'output',
                selectable: false
            });
            group.addWithUpdate(inputPort);
        }
        // 输出点 - 在左边边缘
        else {
            const inputPort = new fabric.Circle({
                radius: 6,
                fill: '#fff',
                stroke: '#333',
                strokeWidth: 2,
                left: group.get("left"),
                top: group.get("top") + halfHeight,
                originX: 'center',
                originY: 'center',
                portType: 'output',
                selectable: false
            });
            group.addWithUpdate(inputPort);

            if (nodeType == 'condition') {
                // 条件节点有两个输出：真和假
                const truePort = new fabric.Circle({
                    radius: 6,
                    fill: '#28a745',
                    stroke: '#333',
                    strokeWidth: 2,
                    left: group.get("left") + nodeWidth + 6,  // 右边缘
                    top: group.get("top") + halfHeight / 2,
                    originX: 'center',
                    originY: 'center',
                    portType: 'output',
                    portName: 'true',
                    selectable: false
                });

                const falsePort = new fabric.Circle({
                    radius: 6,
                    fill: '#dc3545',
                    stroke: '#333',
                    strokeWidth: 2,
                    left: group.get("left") + nodeWidth +6,  // 右边缘
                    top: group.get("top") + 3*halfHeight / 2,
                    originX: 'center',
                    originY: 'center',
                    portType: 'output',
                    portName: 'false',
                    selectable: false
                });

                group.addWithUpdate(truePort);
                group.addWithUpdate(falsePort);
            }
            else {
                const outputPort = new fabric.Circle({
                    radius: 6,
                    fill: '#fff',
                    stroke: '#333',
                    strokeWidth: 2,
                    left: group.get("left") + nodeWidth +6,
                    top: group.get("top") + halfHeight,   // 右边缘
                    originX: 'center',
                    originY: 'center',
                    portType: 'input',
                    selectable: false
                });
                group.addWithUpdate(outputPort);
            }
        }        
        
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

    onCanvasMouseDown(e) {
        if (this.currentMode !== 'connect') return;

        const target = e.target;
        if (!target || !target.nodeId) return;

        // 检查是否点击了连接点
        const pointer = this.canvas.getPointer(e.e);
        const port = this.findPortAtPosition(target, pointer);

        if (port && port.portType !== 'output') {
            if (!this.connectingFrom) {
                // 开始连接
                this.connectingFrom = {
                    node: target,
                    port: port.portName || 'default'
                };
            }
        } else if (port && port.portType !== 'input') {
            if (this.connectingFrom) {
                // 完成连接
                this.createConnection(
                    this.connectingFrom.node,
                    target,
                    this.connectingFrom.port
                );
                this.connectingFrom = null;
            }
        }
    }

    onCanvasMouseMove(e) {
        if (this.currentMode !== 'connect') return;

        const target = e.target;
        if (!target || !target.nodeId) {
            // 不在节点上，恢复默认光标
            this.canvas.defaultCursor = 'crosshair';
            this.canvas.hoverCursor = 'crosshair';
            return;
        }

        // 检查鼠标是否在连接区域
        const pointer = this.canvas.getPointer(e.e);
        const port = this.findPortAtPosition(target, pointer);

        if (port) {
            // 在连接区域，显示指针光标表示可以点击
            this.canvas.hoverCursor = 'pointer';
        } else {
            // 不在连接区域，显示默认连接光标
            this.canvas.hoverCursor = 'crosshair';
        }
    }

    findPortAtPosition(node, pointer) {
        // 获取节点的中心点和边界
        const center = node.getCenterPoint();
        const nodeWidth = 120;
        const nodeHeight = 100;
        const halfWidth = nodeWidth / 2;
        const halfHeight = nodeHeight / 2;

        // 计算节点的边界
        const nodeLeft = center.x - halfWidth;
        const nodeRight = center.x + halfWidth;
        const nodeTop = center.y - halfHeight;
        const nodeBottom = center.y + halfHeight;

        // 检查鼠标是否在节点范围内
        if (pointer.x < nodeLeft || pointer.x > nodeRight ||
            pointer.y < nodeTop || pointer.y > nodeBottom) {
            return null;
        }

        // 获取节点的端口对象
        const objects = node.getObjects();
        const ports = objects.filter(obj => obj.portType);

        // 判断鼠标在节点的左半部分还是右半部分
        const isRightHalf = pointer.x > center.x;

        if (isRightHalf) {
            // 右半部分 - 查找输出端口
            const outputPorts = ports.filter(p => p.portType === 'output');

            if (outputPorts.length === 0) {
                return null;
            }

            if (outputPorts.length === 1) {
                // 单个输出端口（普通节点）
                return outputPorts[0];
            } else {
                // 两个输出端口（条件节点）- 平分右半部分的上下区域
                const isTopHalf = pointer.y < center.y;
                // true端口在上半部分，false端口在下半部分
                return outputPorts.find(p =>
                    (isTopHalf && p.portName === 'true') ||
                    (!isTopHalf && p.portName === 'false')
                );
            }
        } else {
            // 左半部分 - 查找输入端口
            const inputPort = ports.find(p => p.portType === 'input');
            return inputPort || null;
        }
    }

    createConnection(sourceNode, targetNode, sourcePort = 'default') {
        const connectionId = `conn_${++this.connectionIdCounter}`;

        // 计算连接线的起点和终点
        const sourcePoint = this.getNodeOutputPoint(sourceNode, sourcePort);
        const targetPoint = this.getNodeInputPoint(targetNode);

        // 创建连接线
        const line = new fabric.Line([
            sourcePoint.x, sourcePoint.y,
            targetPoint.x, targetPoint.y
        ], {
            stroke: '#666',
            strokeWidth: 2,
            selectable: false,
            evented: false,
            connectionId: connectionId,
            sourceNodeId: sourceNode.nodeId,
            targetNodeId: targetNode.nodeId,
            sourcePort: sourcePort
        });

        // 添加箭头
        const arrow = this.createArrow(targetPoint.x, targetPoint.y, Math.PI);

        this.canvas.add(line);
        this.canvas.add(arrow);

        // 将连接线放到最底层
        line.sendToBack();
        arrow.sendToBack();

        this.connections.set(connectionId, { line, arrow, sourceNode, targetNode });
        this.canvas.renderAll();

        // 监听节点移动以更新连接线
        sourceNode.on('moving', () => this.updateConnections());
        targetNode.on('moving', () => this.updateConnections());
    }

    getNodeOutputPoint(node, port) {
        const center = node.getCenterPoint();
        const halfWidth = 60; // nodeWidth / 2
        const halfHeight = 50; // nodeHeight / 2

        if (node.nodeType === 'condition') {
            return {
                x: center.x + halfWidth,
                y: port === 'true' ? center.y - halfHeight / 2 : center.y + halfHeight / 2
            };
        }
        return { x: center.x + halfWidth, y: center.y };
    }

    getNodeInputPoint(node) {
        const center = node.getCenterPoint();
        const halfWidth = 60; // nodeWidth / 2
        return { x: center.x - halfWidth, y: center.y };
    }

    createArrow(x, y, angle) {
        const points = [
            { x: x, y: y },
            { x: x - 10, y: y - 5 },
            { x: x - 10, y: y + 5 }
        ];
        return new fabric.Polygon(points, {
            fill: '#666',
            selectable: false,
            evented: false
        });
    }

    updateConnections() {
        this.connections.forEach((conn, connId) => {
            const sourcePoint = this.getNodeOutputPoint(conn.sourceNode, conn.line.sourcePort);
            const targetPoint = this.getNodeInputPoint(conn.targetNode);

            conn.line.set({
                x1: sourcePoint.x,
                y1: sourcePoint.y,
                x2: targetPoint.x,
                y2: targetPoint.y
            });

            // 更新箭头位置
            conn.arrow.set({
                left: targetPoint.x - 5,
                top: targetPoint.y
            });
        });
        this.canvas.renderAll();
    }

    onNodeSelected(e) {
        const selected = e.selected[0];
        if (selected && selected.nodeId) {
            this.selectedNode = selected;
            this.showProperties(selected);
        }
    }

    onNodeDeselected() {
        this.selectedNode = null;
        this.hideProperties();
    }

    showProperties(node) {
        const propertiesPanel = document.getElementById('propertiesPanel');
        const nodeType = node.nodeType;
        const nodeData = node.nodeData;

        // 生成属性表单
        const formHtml = this.generatePropertyForm(nodeType, nodeData);
        propertiesPanel.innerHTML = formHtml;

        // 绑定表单事件
        this.bindPropertyFormEvents(node);
    }

    hideProperties() {
        const propertiesPanel = document.getElementById('propertiesPanel');
        propertiesPanel.innerHTML = `
            <div class="no-selection-message">
                <i class="bi bi-info-circle"></i>
                <p>请选择一个节点以编辑其属性</p>
            </div>
        `;
    }

    generatePropertyForm(nodeType, nodeData) {
        // 根据节点类型生成不同的表单
        // 这里使用简化版本，实际应用中应该更详细
        let html = `<div class="property-form">`;
        html += `<div class="property-section">`;
        html += `<div class="property-section-title">基本信息</div>`;
        html += `<div class="form-group">`;
        html += `<label>节点名称</label>`;
        html += `<input type="text" class="form-control" id="propName" value="${nodeData.configuration.name || ''}" />`;
        html += `<small class="form-text text-muted">名称不能包含点(.)字符</small>`;
        html += `</div>`;
        html += `</div>`;

        // 根据节点类型添加特定属性
        html += this.generateTypeSpecificProperties(nodeType, nodeData);

        html += `<button class="btn btn-danger btn-sm mt-3" id="deleteNodeBtn">删除节点</button>`;
        html += `</div>`;

        return html;
    }

    generateTypeSpecificProperties(nodeType, nodeData) {
        let html = `<div class="property-section">`;
        html += `<div class="property-section-title">节点配置</div>`;

        switch (nodeType) {
            case 'trigger':
                html += `<div class="form-group">`;
                html += `<label>Cron表达式</label>`;
                html += `<input type="text" class="form-control" id="propCronExpression" value="${nodeData.configuration.cronExpression || ''}" />`;
                html += `<small class="expression-hint">示例: 0 0 * * * (每天午夜执行)</small>`;
                html += `</div>`;
                break;
            case 'event':
                html += `<div class="form-group">`;
                html += `<label>事件主题</label>`;
                html += `<input type="text" class="form-control" id="propEventTopic" value="${nodeData.configuration.eventTopic || ''}" />`;
                html += `</div>`;
                break;
            case 'httpAuth':
            case 'httpAction':
                html += `<div class="form-group">`;
                html += `<label>URL</label>`;
                html += `<input type="text" class="form-control" id="propUrl" value="${nodeData.configuration.url || nodeData.configuration.authUrl || ''}" />`;
                html += `</div>`;
                html += `<div class="form-group">`;
                html += `<label>HTTP方法</label>`;
                html += `<select class="form-select" id="propHttpMethod">`;
                html += `<option value="GET" ${nodeData.configuration.httpMethod === 'GET' ? 'selected' : ''}>GET</option>`;
                html += `<option value="POST" ${nodeData.configuration.httpMethod === 'POST' ? 'selected' : ''}>POST</option>`;
                html += `<option value="PUT" ${nodeData.configuration.httpMethod === 'PUT' ? 'selected' : ''}>PUT</option>`;
                html += `<option value="DELETE" ${nodeData.configuration.httpMethod === 'DELETE' ? 'selected' : ''}>DELETE</option>`;
                html += `</select>`;
                html += `</div>`;
                if (nodeType === 'httpAuth') {
                    html += `<div class="form-group">`;
                    html += `<label>授权类型</label>`;
                    html += `<select class="form-select" id="propAuthType">`;
                    html += `<option value="None" ${nodeData.configuration.authType === 'None' ? 'selected' : ''}>None</option>`;
                    html += `<option value="Basic" ${nodeData.configuration.authType === 'Basic' ? 'selected' : ''}>Basic</option>`;
                    html += `<option value="Bearer" ${nodeData.configuration.authType === 'Bearer' ? 'selected' : ''}>Bearer</option>`;
                    html += `<option value="ApiKey" ${nodeData.configuration.authType === 'ApiKey' ? 'selected' : ''}>ApiKey</option>`;
                    html += `</select>`;
                    html += `</div>`;
                }
                html += `<div class="form-group">`;
                html += `<label>断言表达式</label>`;
                html += `<input type="text" class="form-control" id="propAssertionExpression" value="${nodeData.configuration.assertionExpression || ''}" />`;
                html += `<small class="expression-hint">示例: \${response.status} == 200</small>`;
                html += `</div>`;
                break;
            case 'commandLine':
                html += `<div class="form-group">`;
                html += `<label>命令</label>`;
                html += `<textarea class="form-control" id="propCommand" rows="3">${nodeData.configuration.command || ''}</textarea>`;
                html += `</div>`;
                break;
            case 'condition':
                html += `<div class="form-group">`;
                html += `<label>条件表达式</label>`;
                html += `<input type="text" class="form-control" id="propConditionExpression" value="${nodeData.configuration.conditionExpression || ''}" />`;
                html += `<small class="expression-hint">使用 \${组件名.属性} 引用其他节点的输出</small>`;
                html += `</div>`;
                break;
        }

        html += `</div>`;
        return html;
    }

    bindPropertyFormEvents(node) {
        // 绑定输入事件以更新节点数据
        const inputs = document.querySelectorAll('#propertiesPanel input, #propertiesPanel select, #propertiesPanel textarea');
        inputs.forEach(input => {
            input.addEventListener('change', (e) => {
                this.updateNodeData(node, e.target.id, e.target.value);
            });
        });

        // 删除节点按钮
        const deleteBtn = document.getElementById('deleteNodeBtn');
        if (deleteBtn) {
            deleteBtn.addEventListener('click', () => {
                this.deleteNode(node);
            });
        }
    }

    updateNodeData(node, propertyId, value) {
        const propertyName = propertyId.replace('prop', '');
        const key = propertyName.charAt(0).toLowerCase() + propertyName.slice(1);

        node.nodeData.configuration[key] = value;

        // 如果是名称，更新节点显示
        if (key === 'name') {
            // 验证名称不包含点
            if (value.includes('.')) {
                alert('节点名称不能包含点(.)字符');
                return;
            }
            node.nodeName = value;
            // 找到标签对象（第三个对象，前两个是rect和icon）
            const objects = node.getObjects();
            const labelObj = objects.find((obj, index) => obj.type === 'text' && index > 0);
            if (labelObj) {
                labelObj.set('text', value);
                this.canvas.renderAll();
            }
        }
    }

    deleteNode(node) {
        if (!confirm('确定要删除此节点吗？')) return;

        // 删除相关的连接
        this.connections.forEach((conn, connId) => {
            if (conn.sourceNode.nodeId === node.nodeId || conn.targetNode.nodeId === node.nodeId) {
                this.canvas.remove(conn.line);
                this.canvas.remove(conn.arrow);
                this.connections.delete(connId);
            }
        });

        // 删除节点
        this.canvas.remove(node);
        this.nodes.delete(node.nodeId);
        this.hideProperties();
        this.canvas.renderAll();
    }

    zoomIn() {
        this.zoom = Math.min(this.zoom * 1.2, 3);
        this.applyZoom();
    }

    zoomOut() {
        this.zoom = Math.max(this.zoom / 1.2, 0.3);
        this.applyZoom();
    }

    applyZoom() {
        this.canvas.setZoom(this.zoom);
        document.getElementById('zoomLevel').textContent = `${Math.round(this.zoom * 100)}%`;
        this.canvas.renderAll();
    }

    fitToScreen() {
        const objects = this.canvas.getObjects().filter(obj => obj.nodeId);
        if (objects.length === 0) return;

        const group = new fabric.Group(objects);
        const bounds = group.getBoundingRect();
        group.destroy();

        const canvasWidth = this.canvas.getWidth();
        const canvasHeight = this.canvas.getHeight();

        const scaleX = canvasWidth / (bounds.width + 100);
        const scaleY = canvasHeight / (bounds.height + 100);
        this.zoom = Math.min(scaleX, scaleY, 1);

        this.applyZoom();
        this.centerCanvas();
    }

    centerCanvas() {
        const objects = this.canvas.getObjects().filter(obj => obj.nodeId);
        if (objects.length === 0) return;

        const group = new fabric.Group(objects);
        const bounds = group.getBoundingRect();
        group.destroy();

        const canvasWidth = this.canvas.getWidth();
        const canvasHeight = this.canvas.getHeight();

        const centerX = canvasWidth / 2 / this.zoom;
        const centerY = canvasHeight / 2 / this.zoom;

        const boundsCenterX = bounds.left + bounds.width / 2;
        const boundsCenterY = bounds.top + bounds.height / 2;

        const deltaX = centerX - boundsCenterX;
        const deltaY = centerY - boundsCenterY;

        objects.forEach(obj => {
            obj.set({
                left: obj.left + deltaX,
                top: obj.top + deltaY
            });
            obj.setCoords();
        });

        this.updateConnections();
        this.canvas.renderAll();
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

        // 收集节点数据
        const nodesData = [];
        this.nodes.forEach((node, nodeId) => {
            nodesData.push({
                nodeId: nodeId,
                nodeType: node.nodeType,
                name: node.nodeName,
                positionX: node.left,
                positionY: node.top,
                configuration: JSON.stringify(node.nodeData.configuration)
            });
        });

        // 收集连接数据
        const connectionsData = [];
        this.connections.forEach((conn, connId) => {
            connectionsData.push({
                connectionId: connId,
                sourceNodeId: conn.sourceNode.nodeId,
                targetNodeId: conn.targetNode.nodeId,
                sourcePort: conn.line.sourcePort || 'default'
            });
        });

        const workflowData = {
            customJobId: customJobId || '00000000-0000-0000-0000-000000000000',
            name: workflowName,
            jobType: workflowJobType,
            description: workflowDescription,
            nodes: nodesData,
            connections: connectionsData
        };

        // 发送到服务器
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
            if (!customJobId) {
                window.location.href = `/CustomJobs/WorkflowEditor/${data.customJobId}`;
            }
        })
        .catch(error => {
            console.error('保存错误:', error);
            alert('保存失败: ' + error.message);
        });
    }

    loadWorkflow() {
        const customJobId = document.getElementById('customJobId').value;
        if (!customJobId || customJobId === '00000000-0000-0000-0000-000000000000') {
            return;
        }

        fetch(`/api/customjobs/workflow/${customJobId}`)
            .then(response => {
                if (!response.ok) throw new Error('加载失败');
                return response.json();
            })
            .then(data => {
                // 加载节点
                data.nodes.forEach(nodeData => {
                    const node = this.createNode(
                        nodeData.nodeType,
                        nodeData.positionX,
                        nodeData.positionY
                    );
                    node.nodeId = nodeData.nodeId;
                    node.nodeName = nodeData.name;
                    node.nodeData.configuration = JSON.parse(nodeData.configuration || '{}');

                    // 更新显示名称（找到标签对象，即第二个text对象）
                    const objects = node.getObjects();
                    const labelObj = objects.find((obj, index) => obj.type === 'text' && index > 0);
                    if (labelObj) {
                        labelObj.set('text', nodeData.name);
                    }
                });

                // 加载连接
                data.connections.forEach(connData => {
                    const sourceNode = this.nodes.get(connData.sourceNodeId);
                    const targetNode = this.nodes.get(connData.targetNodeId);
                    if (sourceNode && targetNode) {
                        this.createConnection(sourceNode, targetNode, connData.sourcePort);
                    }
                });

                this.canvas.renderAll();
                this.fitToScreen();
            })
            .catch(error => {
                console.error('加载错误:', error);
            });
    }
}

// 初始化编辑器
document.addEventListener('DOMContentLoaded', () => {
    window.workflowEditor = new WorkflowEditor();
});
