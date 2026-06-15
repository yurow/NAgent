$(document).ready(function() {
    // ⭐ 首先检查项目是否已初始化
    checkInitialization();
});

// 检查项目初始化状态
function checkInitialization() {
    $.ajax({
        url: '/api/initialization/status',
        type: 'GET',
        success: function(response) {
            if (response.success && response.data) {
                const isInitialized = response.data.isInitialized;

                if (!isInitialized) {
                    // 项目未初始化，跳转到初始化页面
                    window.location.href = '/init.html';
                    return;
                }

                // 项目已初始化，继续检查登录状态
                checkLoginStatus();
            } else {
                // 如果获取状态失败，假设未初始化
                window.location.href = '/init.html';
            }
        },
        error: function() {
            // 请求失败，假设未初始化
            window.location.href = '/init.html';
        }
    });
}

// 检查登录状态
function checkLoginStatus() {
    const token = localStorage.getItem('jwt_token');
    if (!token) {
        window.location.href = '/login.html';
        return;
    }

    // 获取用户信息
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');

    // 显示用户名
    $('#username').text(userInfo.username || '未知用户');

    // 如果是管理员，显示管理员徽章并添加管理菜单点击事件
    if (userInfo.isAdmin) {
        $('#adminBadge').show();
        setupAdminNavigation();
    } else {
        // 非管理员隐藏管理相关菜单项
        $('.nav-item').each(function() {
            const text = $(this).text().trim();
            if (text.includes('模型管理') || text.includes('用户管理') || text.includes('Tools 管理') || text.includes('Skills 管理')) {
                $(this).hide();
            }
        });
    }

    // 退出登录
    $('#logoutBtn').on('click', function() {
        // 清除本地存储
        localStorage.removeItem('jwt_token');
        localStorage.removeItem('user_info');

        // 跳转到登录页
        window.location.href = '/login.html';
    });

    // 验证 Token 有效性（可选）
    $.ajax({
        url: '/api/auth/validate',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (!response.success || !response.data.isValid) {
                // Token 无效，跳转到登录页
                localStorage.removeItem('jwt_token');
                localStorage.removeItem('user_info');
                window.location.href = '/login.html';
            }
        },
        error: function() {
            // Token 验证失败，跳转到登录页
            localStorage.removeItem('jwt_token');
            localStorage.removeItem('user_info');
            window.location.href = '/login.html';
        }
    });

    // ⭐ 初始化 AI Agent 聊天功能
    initAgentChat();

    // ⭐ 导航菜单切换
    setupNavigation();
}

// 导航菜单切换
function setupNavigation() {
    $('.nav-item').on('click', function(e) {
        e.preventDefault();

        // 移除所有激活状态
        $('.nav-item').removeClass('active');
        $(this).addClass('active');

        // 获取菜单文本
        const menuText = $(this).text().trim();

        // 根据菜单切换内容
        if (menuText.includes('仪表盘')) {
            showDashboard();
        } else if (menuText.includes('项目管理')) {
            showProjectManagement();
        } else if (menuText.includes('AI Agent')) {
            showAgentChat();
        } else if (menuText.includes('模型管理')) {
            showModelManagement();
        } else if (menuText.includes('用户管理')) {
            showUserManagement();
        } else if (menuText.includes('Tools 管理')) {
            showToolsManagement();
        } else if (menuText.includes('Skills 管理')) {
            showSkillsManagement();
        }
    });
}

// 显示仪表盘
function showDashboard() {
    $('.content').hide();
    $('#dashboardContent').show();
}

// 显示 AI Agent 聊天
function showAgentChat() {
    $('.content').hide();
    $('#agentChatContent').show();

    // 滚动到底部
    scrollToBottom();
}

// 显示模型管理（仅管理员）
function showModelManagement() {
    $('.content').hide();
    $('#modelManagementContent').show();
    loadModelList();
}

// 显示用户管理（仅管理员）
function showUserManagement() {
    $('.content').hide();
    $('#adminContent').show();
    loadUserList();
}

// 显示项目管理
function showProjectManagement() {
    $('.content').hide();
    $('#projectManagementContent').show();
    loadProjectList();
}

// 加载项目列表
function loadProjectList() {
    const token = localStorage.getItem('jwt_token');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const userId = userInfo.userId || userInfo.id;
    
    if (!userId) {
        $('#projectListContainer').html('<p class="error-message">用户信息无效，请重新登录</p>');
        return;
    }
    
    $.ajax({
        url: `/api/projects/user/${userId}`,
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                renderProjectList(response.data);
            } else {
                $('#projectListContainer').html('<p class="error-message">加载项目列表失败</p>');
            }
        },
        error: function() {
            $('#projectListContainer').html('<p class="error-message">加载项目列表失败</p>');
        }
    });
}

// 渲染项目列表
function renderProjectList(projects) {
    const container = $('#projectListContainer');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const isAdmin = userInfo.isAdmin === true;
    
    if (projects.length === 0) {
        container.html(`
            <div class="empty-state">
                <div class="empty-icon">📁</div>
                <h3>还没有项目</h3>
                <p>创建您的第一个项目来开始使用 AI Agent</p>
                <button class="btn-primary" onclick="openProjectModal()">创建项目</button>
            </div>
        `);
        return;
    }

    let html = '<div class="project-grid">';
    projects.forEach(project => {
        const statusClass = project.isActive ? 'active' : 'inactive';
        const statusText = project.isActive ? '活跃' : '未激活';
        const lastAccessed = project.lastAccessedAt ? new Date(project.lastAccessedAt).toLocaleString('zh-CN') : '从未访问';
        
        html += `
            <div class="project-card ${statusClass}" data-project-id="${project.id}">
                <div class="project-card-header">
                    <h3>${escapeHtml(project.name)}</h3>
                    <span class="project-status ${statusClass}">${statusText}</span>
                </div>
                <div class="project-card-body">
                    <p class="project-description">${escapeHtml(project.description || '暂无描述')}</p>
                    <div class="project-meta">
                        <span>📅 创建于: ${new Date(project.createdAt).toLocaleString('zh-CN')}</span>
                        <span>🕒 最后访问: ${lastAccessed}</span>
                    </div>
                </div>
                <div class="project-card-footer">
                    <button class="btn-primary" onclick="chatWithProject('${project.id}', '${escapeHtml(project.name)}')">💬 聊天</button>
                    <button class="btn-secondary" onclick="viewProjectMemory('${project.id}', '${escapeHtml(project.name)}')">🧠 查看记忆</button>
                    ${isAdmin ? `<button class="btn-danger" onclick="deleteProjectWithConfirm('${project.id}', '${escapeHtml(project.name)}')">🗑️ 删除</button>` : ''}
                </div>
            </div>
        `;
    });
    html += '</div>';
    
    container.html(html);
}

// 带二次确认的项目删除（仅管理员）
function deleteProjectWithConfirm(projectId, projectName) {
    if (!confirm(`确定要删除项目「${projectName}」吗？\n\n此操作不可恢复，项目下的所有会话和记忆也将被清除。`)) {
        return;
    }
    // 二次确认
    if (!confirm(`再次确认：请输入「确定」以删除项目「${projectName}」`)) {
        return;
    }
    deleteProject(projectId);
}

// 直接跳转到聊天（从项目卡片）
function chatWithProject(projectId, projectName) {
    // 切换到 AI Agent 聊天页面
    showAgentChat();
    
    // 设置项目选择器
    $('#projectSelect').val(projectId);
    $('#projectSelect').trigger('change');
    
    // 添加提示消息
    addSystemMessage(`已切换到项目：${projectName}`);
    
    // 聚焦输入框
    $('#chatInput').focus();
}

// 查看项目记忆
function viewProjectMemory(projectId, projectName) {
    const token = localStorage.getItem('jwt_token');
    
    // 显示加载状态
    showMemoryModal(projectName, '<p>加载记忆中...</p>');
    
    // 获取项目长期记忆摘要
    $.ajax({
        url: `/api/memory/project/${projectId}/summary?limit=50`,
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data && response.data.length > 0) {
                displayMemoryContent(response.data);
            } else {
                showMemoryModal(projectName, '<p class="empty-memory">暂无记忆内容</p><p class="hint">与 AI Agent 聊天后，记忆会自动保存到这里。</p>');
            }
        },
        error: function() {
            showMemoryModal(projectName, '<p class="error">加载记忆失败，请稍后重试</p>');
        }
    });
}

// 显示记忆模态框
function showMemoryModal(projectName, content) {
    // 如果模态框不存在则创建
    if ($('#memoryModal').length === 0) {
        const modalHtml = `
            <div id="memoryModal" class="modal">
                <div class="modal-content" style="max-width: 800px; max-height: 80vh;">
                    <div class="modal-header">
                        <h2>🧠 项目记忆 - <span id="memoryProjectName"></span></h2>
                        <button class="modal-close" onclick="closeMemoryModal()">&times;</button>
                    </div>
                    <div class="modal-body" style="max-height: 60vh; overflow-y: auto;">
                        <div id="memoryContent"></div>
                    </div>
                    <div class="modal-footer">
                        <button class="btn-secondary" onclick="closeMemoryModal()">关闭</button>
                    </div>
                </div>
            </div>
        `;
        $('body').append(modalHtml);
    }
    
    $('#memoryProjectName').text(projectName);
    $('#memoryContent').html(content);
    $('#memoryModal').show();
}

// 关闭记忆模态框
function closeMemoryModal() {
    $('#memoryModal').hide();
}

// 显示记忆内容
function displayMemoryContent(memories) {
    let html = '<div class="memory-list">';
    
    memories.forEach((memory, index) => {
        const importanceStars = '⭐'.repeat(memory.importance);
        const categoryLabels = {
            'General': '通用',
            'UserPreference': '用户偏好',
            'ProjectKnowledge': '项目知识',
            'CodePattern': '代码模式',
            'ErrorSolution': '错误方案',
            'Decision': '决策记录',
            'TaskContext': '任务上下文'
        };
        const categoryLabel = categoryLabels[memory.category] || memory.category;
        
        html += `
            <div class="memory-item">
                <div class="memory-header">
                    <span class="memory-index">#${index + 1}</span>
                    <span class="memory-category">${escapeHtml(categoryLabel)}</span>
                    <span class="memory-importance" title="重要性">${importanceStars}</span>
                    <span class="memory-time">${new Date(memory.createdAt).toLocaleString('zh-CN')}</span>
                </div>
                <div class="memory-summary">${escapeHtml(memory.summary)}</div>
                <div class="memory-content">${escapeHtml(memory.content)}</div>
            </div>
        `;
    });
    
    html += '</div>';
    $('#memoryContent').html(html);
}

// 打开项目创建模态框
function openProjectModal() {
    $('#projectModal').show();
    $('#projectForm')[0].reset();
    $('#projectModalTitle').text('创建新项目');
}

// 关闭项目模态框
function closeProjectModal() {
    $('#projectModal').hide();
}

// 创建项目
$('#createProjectBtn').on('click', function() {
    openProjectModal();
});

// 项目表单提交
$('#projectForm').on('submit', function(e) {
    e.preventDefault();
    
    const token = localStorage.getItem('jwt_token');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const userId = userInfo.userId || userInfo.id;
    
    if (!userId) {
        alert('用户信息无效，请重新登录');
        return;
    }
    
    const projectData = {
        name: $('#projectName').val().trim(),
        description: $('#projectDescription').val().trim(),
        userId: userId
    };
    
    $.ajax({
        url: '/api/projects',
        type: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify(projectData),
        success: function(response) {
            if (response.success) {
                closeProjectModal();
                loadProjectList();
                alert('项目创建成功！');
            } else {
                alert('项目创建失败：' + response.message);
            }
        },
        error: function(xhr) {
            const error = xhr.responseJSON || {};
            alert('项目创建失败：' + (error.message || '未知错误'));
        }
    });
});

// 激活项目
function activateProject(projectId) {
    const token = localStorage.getItem('jwt_token');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const userId = userInfo.userId || userInfo.id;
    
    if (!userId) {
        alert('用户信息无效，请重新登录');
        return;
    }
    
    $.ajax({
        url: `/api/projects/${projectId}/activate`,
        type: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({ userId: userId }),
        success: function(response) {
            if (response.success) {
                loadProjectList();
                alert('项目已激活！');
            } else {
                alert('项目激活失败：' + response.message);
            }
        },
        error: function(xhr) {
            const error = xhr.responseJSON || {};
            alert('项目激活失败：' + (error.message || '未知错误'));
        }
    });
}

// 停用项目
function deactivateProject(projectId) {
    if (!confirm('确定要停用此项目吗？')) {
        return;
    }
    
    const token = localStorage.getItem('jwt_token');
    
    $.ajax({
        url: `/api/projects/${projectId}/deactivate`,
        type: 'POST',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success) {
                loadProjectList();
                alert('项目已停用！');
            } else {
                alert('项目停用失败：' + response.message);
            }
        },
        error: function(xhr) {
            const error = xhr.responseJSON || {};
            alert('项目停用失败：' + (error.message || '未知错误'));
        }
    });
}

// 删除项目
function deleteProject(projectId) {
    if (!confirm('确定要删除此项目吗？此操作不可恢复！')) {
        return;
    }
    
    const token = localStorage.getItem('jwt_token');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const userId = userInfo.userId || userInfo.id;
    
    if (!userId) {
        alert('用户信息无效，请重新登录');
        return;
    }
    
    $.ajax({
        url: `/api/projects/${projectId}`,
        type: 'DELETE',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({ userId: userId }),
        success: function(response) {
            if (response.success) {
                loadProjectList();
                alert('项目已删除！');
            } else {
                alert('项目删除失败：' + response.message);
            }
        },
        error: function(xhr) {
            const error = xhr.responseJSON || {};
            alert('项目删除失败：' + (error.message || '未知错误'));
        }
    });
}

// 初始化管理员导航
function setupAdminNavigation() {
    // 管理员可以看到所有菜单项
}

// ⭐ 初始化 AI Agent 聊天
function initAgentChat() {
    let sessionId = generateSessionId();
    let currentProjectId = null;

    // 加载可用模型列表
    loadAvailableModels();

    // 加载用户项目列表
    loadUserProjectsForChat();

    // 项目切换事件
    $('#projectSelect').on('change', function() {
        currentProjectId = $(this).val();
        if (currentProjectId) {
            // 切换项目时清空聊天并生成新会话ID
            clearChat();
            sessionId = generateSessionId();
            // 添加项目切换提示
            addSystemMessage(`已切换到项目：${$(this).find('option:selected').text()}`);
        }
    });

    // 发送消息按钮点击
    $('#sendBtn').on('click', function() {
        const useStream = $('#streamToggle').prop('checked');
        sendMessage(sessionId, currentProjectId, useStream);
    });

    // Enter 键发送（Shift+Enter 换行）
    $('#chatInput').on('keydown', function(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            const useStream = $('#streamToggle').prop('checked');
            sendMessage(sessionId, currentProjectId, useStream);
        }
    });

    // 清空对话按钮
    $('#clearChatBtn').on('click', function() {
        clearChat();
        sessionId = generateSessionId(); // 生成新的会话ID
    });
}

// 加载用户项目列表用于聊天
function loadUserProjectsForChat() {
    const token = localStorage.getItem('jwt_token');
    const userInfo = JSON.parse(localStorage.getItem('user_info') || '{}');
    const userId = userInfo.userId || userInfo.id;
    
    if (!userId) {
        console.error('用户信息无效，无法加载项目列表');
        return;
    }
    
    $.ajax({
        url: `/api/projects/user/${userId}`,
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                const select = $('#projectSelect');
                select.empty();
                select.append('<option value="">请选择项目</option>');
                
                response.data.forEach(project => {
                    const option = `<option value="${project.id}" ${project.isActive ? 'selected' : ''}>${escapeHtml(project.name)}</option>`;
                    select.append(option);
                });

                // 如果有活跃项目，自动选择
                const activeProject = response.data.find(p => p.isActive);
                if (activeProject) {
                    select.val(activeProject.id);
                }
            }
        },
        error: function() {
            console.error('加载项目列表失败');
        }
    });
}

// 添加系统消息
function addSystemMessage(message) {
    const messageHtml = `
        <div class="message system-message">
            <div class="message-avatar">ℹ️</div>
            <div class="message-content">
                <p>${escapeHtml(message)}</p>
                <span class="message-time">刚刚</span>
            </div>
        </div>
    `;
    $('#chatMessages').append(messageHtml);
    scrollToBottom();
}

// 加载可用模型列表
function loadAvailableModels() {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: '/api/llm/models',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                const select = $('#modelSelect');
                select.empty();
                select.append('<option value="">默认模型</option>');
                
                response.data.forEach(model => {
                    const option = `<option value="${model.modelId}">${escapeHtml(model.displayName)} (${escapeHtml(model.providerName)})</option>`;
                    select.append(option);
                });
            }
        },
        error: function() {
            console.error('加载模型列表失败');
        }
    });
}

// 生成会话 ID
function generateSessionId() {
    return 'session_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
}

// 发送消息
function sendMessage(sessionId, projectId, useStream = false) {
    const input = $('#chatInput');
    const message = input.val().trim();

    if (!message) {
        return;
    }

    // 检查是否选择了项目
    if (!projectId) {
        addErrorMessage('请先选择一个项目才能发送消息');
        return;
    }

    // 获取选中的模型
    const selectedModel = $('#modelSelect').val();

    // 清空输入框
    input.val('');

    // 添加用户消息到聊天窗口
    addUserMessage(message);

    // 禁用发送按钮
    $('#sendBtn').prop('disabled', true);

    // 调用 API
    const token = localStorage.getItem('jwt_token');
    const requestData = {
        sessionId: sessionId,
        projectId: projectId,
        userInput: message,
        modelId: selectedModel || null
    };

    if (useStream) {
        sendMessageStream(token, requestData);
    } else {
        sendMessageNonStream(token, requestData);
    }
}

// 非流式发送消息
function sendMessageNonStream(token, requestData) {
    // 显示加载指示器
    showLoading();
    
    $.ajax({
        url: '/api/agent/execute',
        type: 'POST',
        contentType: 'application/json',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        data: JSON.stringify(requestData),
        success: function(response) {
            hideLoading();

            if (response.success) {
                addBotMessage(response.data, response.modelName);
            } else {
                addErrorMessage(response.message || '请求失败');
            }

            // 启用发送按钮
            $('#sendBtn').prop('disabled', false);
        },
        error: function(xhr) {
            hideLoading();

            let errorMsg = '网络错误，请稍后重试';
            if (xhr.status === 401) {
                errorMsg = '认证失败，请重新登录';
                setTimeout(() => {
                    localStorage.removeItem('jwt_token');
                    localStorage.removeItem('user_info');
                    window.location.href = '/login.html';
                }, 2000);
            } else if (xhr.responseJSON && xhr.responseJSON.message) {
                errorMsg = xhr.responseJSON.message;
            }

            addErrorMessage(errorMsg);

            // 启用发送按钮
            $('#sendBtn').prop('disabled', false);
        }
    });
}

// 流式发送消息
function sendMessageStream(token, requestData) {
    // 获取选中的模型名称
    const selectedModelName = $('#modelSelect option:selected').text() || '默认模型';
    
    // 创建一个空的机器人消息容器
    const botMessageId = 'bot-msg-' + Date.now();
    addBotMessageContainer(botMessageId, selectedModelName);
    
    // 使用 fetch API 进行流式请求
    fetch('/api/agent/execute-stream', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(requestData)
    })
    .then(response => {
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let fullText = '';
        
        function readStream() {
            return reader.read().then(({ done, value }) => {
                if (done) {
                    // 启用发送按钮
                    $('#sendBtn').prop('disabled', false);
                    return;
                }
                
                const chunk = decoder.decode(value, { stream: true });
                const lines = chunk.split('\n\n');
                
                for (const line of lines) {
                    if (line.startsWith('data: ')) {
                        const data = line.substring(6);
                        
                        if (data === '[DONE]') {
                            // 启用发送按钮
                            $('#sendBtn').prop('disabled', false);
                            return;
                        }
                        
                        if (data.startsWith('ERROR:')) {
                            const errorMsg = data.substring(7);
                            updateBotMessage(botMessageId, fullText);
                            addErrorMessage(errorMsg);
                            $('#sendBtn').prop('disabled', false);
                            return;
                        }
                        
                        fullText += data;
                        updateBotMessage(botMessageId, fullText);
                        scrollToBottom();
                    }
                }
                
                return readStream();
            });
        }
        
        return readStream();
    })
    .catch(error => {
        console.error('Stream error:', error);
        addErrorMessage('流式请求失败: ' + error.message);
        $('#sendBtn').prop('disabled', false);
    });
}

// 添加用户消息
function addUserMessage(message) {
    const time = getCurrentTime();
    const html = `
        <div class="message user-message">
            <div class="message-avatar">👤</div>
            <div class="message-content">
                <p>${escapeHtml(message)}</p>
                <span class="message-time">${time}</span>
            </div>
        </div>
    `;
    $('#chatMessages').append(html);
    scrollToBottom();
}

// 添加机器人消息（支持模型标注）
function addBotMessage(message, modelName) {
    const time = getCurrentTime();
    const modelBadge = modelName ? `<span class="model-badge" title="使用模型">🧠 ${escapeHtml(modelName)}</span>` : '';
    const html = `
        <div class="message bot-message">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                <p>${escapeHtml(message)}</p>
                <div class="message-footer">
                    <span class="message-time">${time}</span>
                    ${modelBadge}
                </div>
            </div>
        </div>
    `;
    $('#chatMessages').append(html);
    scrollToBottom();
}

// 添加机器人消息容器（用于流式输出，支持模型标注）
function addBotMessageContainer(messageId, modelName) {
    const time = getCurrentTime();
    const modelBadge = modelName ? `<span class="model-badge" title="使用模型">🧠 ${escapeHtml(modelName)}</span>` : '';
    const html = `
        <div class="message bot-message" id="${messageId}">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                <p class="streaming-text"></p>
                <div class="message-footer">
                    <span class="message-time">${time}</span>
                    ${modelBadge}
                </div>
            </div>
        </div>
    `;
    $('#chatMessages').append(html);
    scrollToBottom();
}

// 更新机器人消息（用于流式输出）
function updateBotMessage(messageId, message) {
    const $messageContainer = $(`#${messageId}`);
    if ($messageContainer.length > 0) {
        $messageContainer.find('.streaming-text').html(escapeHtml(message));
    }
}

// 添加错误消息
function addErrorMessage(message) {
    const time = getCurrentTime();
    const html = `
        <div class="message system-message">
            <div class="message-avatar">⚠️</div>
            <div class="message-content">
                <p>${escapeHtml(message)}</p>
                <span class="message-time">${time}</span>
            </div>
        </div>
    `;
    $('#chatMessages').append(html);
    scrollToBottom();
}

// 清空聊天
function clearChat() {
    $('#chatMessages').html(`
        <div class="message system-message">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                <p>对话已清空。有什么可以帮助你的吗？</p>
                <span class="message-time">${getCurrentTime()}</span>
            </div>
        </div>
    `);
}

// 显示加载指示器
function showLoading() {
    $('#loadingIndicator').show();
    scrollToBottom();
}

// 隐藏加载指示器
function hideLoading() {
    $('#loadingIndicator').hide();
}

// 滚动到底部
function scrollToBottom() {
    const chatMessages = $('#chatMessages')[0];
    if (chatMessages) {
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}

// 获取当前时间
function getCurrentTime() {
    const now = new Date();
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    return `${hours}:${minutes}`;
}

// HTML 转义（防止 XSS）
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// 加载用户列表（管理员）
function loadUserList() {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: '/api/users',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                displayUserList(response.data);
            } else {
                $('#userList').html('<p>加载失败</p>');
            }
        },
        error: function() {
            $('#userList').html('<p>加载失败</p>');
        }
    });
}

// 显示用户列表
function displayUserList(users) {
    if (!users || users.length === 0) {
        $('#userList').html('<p>暂无用户</p>');
        return;
    }

    let html = '<table class="data-table"><thead><tr>';
    html += '<th>用户名</th><th>邮箱</th><th>角色</th><th>状态</th><th>操作</th></tr></thead><tbody>';

    users.forEach(user => {
        const roleBtn = user.isAdmin 
            ? `<button class="btn-small btn-warning" onclick="updateUserRole('${user.id}', false)">取消管理员</button>`
            : `<button class="btn-small btn-primary" onclick="updateUserRole('${user.id}', true)">设为管理员</button>`;
        
        const statusBtn = user.isActive
            ? `<button class="btn-small btn-danger" onclick="updateUserStatus('${user.id}', false)">禁用</button>`
            : `<button class="btn-small btn-success" onclick="updateUserStatus('${user.id}', true)">启用</button>`;

        html += `<tr>
            <td>${escapeHtml(user.username)}</td>
            <td>${escapeHtml(user.email)}</td>
            <td>${user.isAdmin ? '管理员' : '普通用户'}</td>
            <td>${user.isActive ? '活跃' : '禁用'}</td>
            <td>
                ${roleBtn}
                ${statusBtn}
                <button class="btn-small btn-secondary" onclick="showResetPassword('${user.id}', '${escapeHtml(user.username)}')">重置密码</button>
            </td>
        </tr>`;
    });

    html += '</tbody></table>';
    $('#userList').html(html);
}

// 更新用户角色
function updateUserRole(userId, isAdmin) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/users/${userId}/role`,
        type: 'PUT',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({ isAdmin: isAdmin }),
        success: function(response) {
            if (response.success) {
                loadUserList();
            } else {
                alert('操作失败: ' + response.message);
            }
        },
        error: function() {
            alert('操作失败');
        }
    });
}

// 更新用户状态
function updateUserStatus(userId, isActive) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/users/${userId}/status`,
        type: 'PUT',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({ isActive: isActive }),
        success: function(response) {
            if (response.success) {
                loadUserList();
            } else {
                alert('操作失败: ' + response.message);
            }
        },
        error: function() {
            alert('操作失败');
        }
    });
}

// 显示重置密码对话框
function showResetPassword(userId, username) {
    const newPassword = prompt(`请输入用户 "${username}" 的新密码（至少6个字符）:`);
    if (newPassword && newPassword.length >= 6) {
        resetUserPassword(userId, newPassword);
    } else if (newPassword) {
        alert('密码长度至少为6个字符');
    }
}

// 重置用户密码
function resetUserPassword(userId, newPassword) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/users/${userId}/password`,
        type: 'PUT',
        headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json'
        },
        data: JSON.stringify({ newPassword: newPassword }),
        success: function(response) {
            if (response.success) {
                alert('密码重置成功');
            } else {
                alert('密码重置失败: ' + response.message);
            }
        },
        error: function() {
            alert('密码重置失败');
        }
    });
}

// ===== Tools 管理 =====

// 显示 Tools 管理页面
function showToolsManagement() {
    $('.content').hide();
    $('#toolsManagementContent').show();
    loadToolsList();
}

// 加载 Tools 列表
function loadToolsList() {
    const token = localStorage.getItem('jwt_token');
    $('#toolsListContainer').html('<p>加载中...</p>');

    $.ajax({
        url: '/api/tools',
        type: 'GET',
        headers: { 'Authorization': `Bearer ${token}` },
        success: function(response) {
            if (response.success && response.data && response.data.length > 0) {
                displayToolsList(response.data);
            } else {
                $('#toolsListContainer').html('<p class="section-desc">暂无可用工具。</p>');
            }
        },
        error: function(xhr) {
            let msg = '加载工具列表失败';
            if (xhr.status === 401) msg = '登录已过期，请重新登录';
            $('#toolsListContainer').html(`<p class="section-desc" style="color: #f44336;">${msg}</p>`);
        }
    });
}

// 显示 Tools 列表
function displayToolsList(tools) {
    let html = '<div class="provider-list">';

    tools.forEach(function(tool) {
        const categoryColor = getToolCategoryColor(tool.category);
        const securityColor = getSecurityColor(tool.securityLevel);

        html += `
            <div class="provider-card">
                <h3>${escapeHtml(tool.name)}</h3>
                <p><strong>分类:</strong> <span class="badge" style="background: ${categoryColor};">${escapeHtml(tool.category)}</span></p>
                <p><strong>安全等级:</strong> <span class="badge" style="background: ${securityColor};">${escapeHtml(tool.securityLevel)}</span></p>
                <p><strong>描述:</strong> ${escapeHtml(tool.description)}</p>
                <p><strong>来源:</strong> ${escapeHtml(tool.source)}</p>
                <p><strong>状态:</strong> <span class="badge" style="background: ${tool.isEnabled ? '#4CAF50' : '#f44336'};">${tool.isEnabled ? '启用' : '禁用'}</span></p>
            </div>
        `;
    });

    html += '</div>';
    html += `<p class="section-desc" style="margin-top: 16px; color: #888;">共 ${tools.length} 个系统内置工具。工具在项目隔离的工作空间中执行。</p>`;
    $('#toolsListContainer').html(html);
}

// 获取工具分类颜色
function getToolCategoryColor(category) {
    const colors = {
        'built-in': '#667eea',
        'development': '#2196F3',
        'search': '#FF9800',
        'file': '#4CAF50',
        'system': '#9C27B0'
    };
    return colors[category] || '#666';
}

// 获取安全等级颜色
function getSecurityColor(level) {
    const colors = {
        'Low': '#4CAF50',
        'Medium': '#FF9800',
        'High': '#f44336'
    };
    return colors[level] || '#666';
}

// 重新加载所有 Tools
function reloadAllTools() {
    const token = localStorage.getItem('jwt_token');
    $('#toolsListContainer').html('<p>正在重新加载 Tools...</p>');

    $.ajax({
        url: '/api/tools/reload',
        type: 'POST',
        headers: { 'Authorization': `Bearer ${token}` },
        success: function(response) {
            if (response.success) {
                alert(response.message || '重新加载完成');
                loadToolsList();
            } else {
                alert('重新加载失败: ' + (response.message || '未知错误'));
                loadToolsList();
            }
        },
        error: function(xhr) {
            let msg = '重新加载失败';
            if (xhr.status === 401) msg = '登录已过期，请重新登录';
            else if (xhr.status === 403) msg = '只有管理员可以执行此操作';
            else if (xhr.responseJSON && xhr.responseJSON.message) msg = xhr.responseJSON.message;
            alert(msg);
            loadToolsList();
        }
    });
}

// ===== Skills 管理 =====

// 显示 Skills 管理页面
function showSkillsManagement() {
    $('.content').hide();
    $('#skillsManagementContent').show();
    loadSkillsList();
}

// 加载 Skills 列表
function loadSkillsList() {
    const token = localStorage.getItem('jwt_token');
    $('#skillsListContainer').html('<p>加载中...</p>');
    
    $.ajax({
        url: '/api/skills',
        type: 'GET',
        headers: { 'Authorization': `Bearer ${token}` },
        success: function(response) {
            if (response.success && response.data) {
                displaySkillsList(response.data);
            } else {
                $('#skillsListContainer').html('<p>暂无 Skills 数据</p>');
            }
        },
        error: function() {
            // 显示示例数据
            displaySkillsList([
                { id: '1', name: 'code-analysis', description: '分析代码质量、结构和潜在问题', category: 'development', version: '1.0.0', isEnabled: true, toolNames: ['code_linter', 'security_scanner'], examples: [] }
            ]);
        }
    });
}

// 显示 Skills 列表
function displaySkillsList(skills) {
    if (!skills || skills.length === 0) {
        $('#skillsListContainer').html('<p>暂无 Skills。请将 Markdown 文件放入 <code>skills/</code> 目录并点击「重新加载」。</p>');
        return;
    }

    let html = '<div class="provider-list">';
    skills.forEach(skill => {
        const statusText = skill.isEnabled ? '已启用' : '已禁用';
        const statusColor = skill.isEnabled ? '#4CAF50' : '#999';
        const toolsHtml = skill.toolNames && skill.toolNames.length > 0 
            ? skill.toolNames.map(t => `<span class="badge" style="background: #2196F3; margin-right: 4px;">${escapeHtml(t)}</span>`).join('') 
            : '<span style="color: #888;">无关联工具</span>';
        
        html += `
            <div class="provider-card">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <h3>${escapeHtml(skill.name)}</h3>
                    <span class="badge" style="background: ${statusColor};">${statusText}</span>
                </div>
                <p><strong>分类:</strong> ${escapeHtml(skill.category)}</p>
                <p><strong>版本:</strong> ${escapeHtml(skill.version)}</p>
                <p><strong>描述:</strong> ${escapeHtml(skill.description)}</p>
                <p><strong>关联工具:</strong> ${toolsHtml}</p>
            </div>
        `;
    });
    html += '</div>';
    $('#skillsListContainer').html(html);
}

// 重新加载所有 Skills
function reloadAllSkills() {
    const token = localStorage.getItem('jwt_token');
    
    if (!confirm('确定要重新加载所有 Skills 吗？这将重新读取 skills/ 目录下的所有 Markdown 文件。')) {
        return;
    }
    
    $.ajax({
        url: '/api/skills/reload',
        type: 'POST',
        headers: { 'Authorization': `Bearer ${token}` },
        success: function(response) {
            if (response.success) {
                alert(`成功重新加载 ${response.data} 个 Skills`);
                loadSkillsList();
            } else {
                alert('重新加载失败: ' + response.message);
            }
        },
        error: function() {
            alert('重新加载失败，请检查服务器日志');
        }
    });
}

// 加载模型列表（管理员）
function loadModelList() {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: '/api/llm/providers',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                displayProviderList(response.data);
            } else {
                $('#providerListContainer').html('<p>加载失败</p>');
            }
        },
        error: function() {
            $('#providerListContainer').html('<p>加载失败</p>');
        }
    });
}

// 显示提供商列表
function displayProviderList(providers) {
    if (!providers || providers.length === 0) {
        $('#providerListContainer').html('<p>暂无提供商配置，请点击"添加提供商"按钮开始配置</p>');
        return;
    }

    let html = '<div class="provider-list">';

    providers.forEach(provider => {
        html += `<div class="provider-card" data-provider-id="${provider.id}">
            <h3>${escapeHtml(provider.name)}</h3>
            <p><strong>协议:</strong> ${getProtocolTypeName(provider.protocolType)}</p>
            <p><strong>基础URL:</strong> ${escapeHtml(provider.baseUrl)}</p>
            <p><strong>状态:</strong> ${provider.isEnabled ? '✅ 已启用' : '❌ 已禁用'}</p>
            
            <div class="provider-card-actions">
                <button class="btn-primary btn-small" onclick="showAddModelModal('${provider.id}', '${escapeHtml(provider.name)}')">➕ 添加模型</button>
                <button class="btn-secondary btn-small" onclick="editProvider('${provider.id}')">✏️ 编辑</button>
                <button class="btn-danger btn-small" onclick="deleteProvider('${provider.id}')">🗑️ 删除</button>
            </div>

            <div class="model-list-in-provider">`;

        if (provider.models && provider.models.length > 0) {
            provider.models.forEach(model => {
                html += `<div class="model-item">
                    <div class="model-item-info">
                        <h4>${escapeHtml(model.displayName)} ${model.isDefault ? '<span class="badge-default">默认</span>' : ''}</h4>
                        <p><strong>模型ID:</strong> ${escapeHtml(model.modelId)}</p>
                        <p><strong>上下文窗口:</strong> ${formatNumber(model.contextWindowSize)} tokens</p>
                        <p><strong>最大输出:</strong> ${formatNumber(model.maxOutputTokens)} tokens</p>
                        <p><strong>温度:</strong> ${model.defaultTemperature}</p>
                        <p><strong>状态:</strong> ${model.isEnabled ? '✅ 可用' : '❌ 不可用'}</p>
                        <p><strong>总使用量:</strong> ${formatNumber(model.totalTokenUsage)} tokens</p>
                        <p><strong>创建时间:</strong> ${formatDate(model.createdAt)}</p>
                    </div>
                    <div class="model-item-actions">
                        <button class="btn-secondary btn-small" onclick="editModel('${model.id}', '${provider.id}')">✏️ 编辑</button>
                        <button class="btn-secondary btn-small" onclick="viewModelStats('${model.id}')">📊 统计</button>
                        <button class="btn-danger btn-small" onclick="deleteModel('${model.id}')">🗑️ 删除</button>
                    </div>
                </div>`;
            });
        } else {
            html += '<p style="color: #999; font-style: italic;">暂无模型，请添加模型</p>';
        }

        html += `</div></div>`;
    });

    html += '</div>';
    $('#providerListContainer').html(html);
}

// 获取协议类型名称
function getProtocolTypeName(protocolType) {
    const types = {
        1: 'OpenAI',
        2: 'Anthropic'
    };
    return types[protocolType] || '未知';
}

// 格式化数字
function formatNumber(num) {
    return num.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// 格式化日期
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN') + ' ' + date.toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit' });
}

// 显示添加提供商模态框
$('#addProviderBtn').on('click', function() {
    $('#providerModalTitle').text('添加 LLM 提供商');
    $('#providerForm')[0].reset();
    $('#providerId').val('');
    $('#providerModal').show();
});

// 关闭提供商模态框
function closeProviderModal() {
    $('#providerModal').hide();
}

// 关闭模型模态框
function closeModelModal() {
    $('#modelModal').hide();
}

// 提交提供商表单
$('#providerForm').on('submit', function(e) {
    e.preventDefault();
    
    const providerId = $('#providerId').val();
    const data = {
        name: $('#providerName').val(),
        protocolType: parseInt($('#protocolType').val()),
        baseUrl: $('#baseUrl').val(),
        apiKey: $('#apiKey').val()
    };

    const token = localStorage.getItem('jwt_token');
    const url = providerId ? `/api/llm/providers/${providerId}` : '/api/llm/providers';
    const method = providerId ? 'PUT' : 'POST';

    $.ajax({
        url: url,
        type: method,
        contentType: 'application/json',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        data: JSON.stringify(data),
        success: function(response) {
            if (response.success) {
                alert(providerId ? '提供商更新成功' : '提供商添加成功');
                closeProviderModal();
                loadModelList();
            } else {
                alert('操作失败: ' + response.message);
            }
        },
        error: function(xhr) {
            alert('操作失败: ' + (xhr.responseJSON?.message || '网络错误'));
        }
    });
});

// 编辑提供商
function editProvider(providerId) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: '/api/llm/providers',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                const provider = response.data.find(p => p.id === providerId);
                if (provider) {
                    $('#providerModalTitle').text('编辑 LLM 提供商');
                    $('#providerId').val(provider.id);
                    $('#providerName').val(provider.name);
                    $('#protocolType').val(provider.protocolType);
                    $('#baseUrl').val(provider.baseUrl);
                    $('#apiKey').val(provider.apiKey);
                    $('#providerModal').show();
                }
            }
        },
        error: function() {
            alert('加载提供商信息失败');
        }
    });
}

// 删除提供商
function deleteProvider(providerId) {
    if (!confirm('确定要删除此提供商吗？这将同时删除该提供商下的所有模型。')) {
        return;
    }

    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/llm/providers/${providerId}`,
        type: 'DELETE',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success) {
                alert('提供商删除成功');
                loadModelList();
            } else {
                alert('删除失败: ' + response.message);
            }
        },
        error: function(xhr) {
            alert('删除失败: ' + (xhr.responseJSON?.message || '网络错误'));
        }
    });
}

// 显示添加模型模态框
function showAddModelModal(providerId, providerName) {
    $('#modelProviderId').val(providerId);
    $('#modelDbId').val('');
    $('#modelModalTitle').text('添加模型');
    $('#modelSubmitBtn').text('添加');
    $('#modelForm')[0].reset();
    $('#modelIsEnabled').val('true');
    $('#modelIsDefault').prop('checked', false);
    $('#modelModal').show();
}

// 显示编辑模型模态框
function editModel(modelId, providerId) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: '/api/llm/providers',
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                let model = null;
                let provider = null;
                
                for (const p of response.data) {
                    const foundModel = p.models?.find(m => m.id === modelId);
                    if (foundModel) {
                        model = foundModel;
                        provider = p;
                        break;
                    }
                }
                
                if (model && provider) {
                    $('#modelProviderId').val(provider.id);
                    $('#modelDbId').val(model.id);
                    $('#modelModalTitle').text('编辑模型');
                    $('#modelSubmitBtn').text('保存');
                    
                    $('#modelId').val(model.modelId);
                    $('#modelDisplayName').val(model.displayName);
                    $('#contextWindowSize').val(model.contextWindowSize);
                    $('#maxOutputTokens').val(model.maxOutputTokens);
                    $('#defaultTemperature').val(model.defaultTemperature);
                    $('#modelIsEnabled').val(model.isEnabled ? 'true' : 'false');
                    $('#modelIsDefault').prop('checked', model.isDefault);
                    
                    $('#modelModal').show();
                }
            }
        },
        error: function() {
            alert('加载模型信息失败');
        }
    });
}

// 提交模型表单
$('#modelForm').on('submit', function(e) {
    e.preventDefault();
    
    const providerId = $('#modelProviderId').val();
    const modelDbId = $('#modelDbId').val();
    const isEdit = !!modelDbId;
    
    const data = {
        modelId: $('#modelId').val(),
        displayName: $('#modelDisplayName').val(),
        contextWindowSize: parseInt($('#contextWindowSize').val()),
        maxOutputTokens: parseInt($('#maxOutputTokens').val()),
        defaultTemperature: parseFloat($('#defaultTemperature').val()),
        isEnabled: $('#modelIsEnabled').val() === 'true',
        isDefault: $('#modelIsDefault').prop('checked')
    };

    const token = localStorage.getItem('jwt_token');
    
    if (isEdit) {
        // 编辑模型
        $.ajax({
            url: `/api/llm/models/${modelDbId}`,
            type: 'PUT',
            contentType: 'application/json',
            headers: {
                'Authorization': `Bearer ${token}`
            },
            data: JSON.stringify(data),
            success: function(response) {
                if (response.success) {
                    alert('模型更新成功');
                    closeModelModal();
                    loadModelList();
                } else {
                    alert('更新失败: ' + response.message);
                }
            },
            error: function(xhr) {
                alert('更新失败: ' + (xhr.responseJSON?.message || '网络错误'));
            }
        });
    } else {
        // 添加模型
        $.ajax({
            url: `/api/llm/providers/${providerId}/models`,
            type: 'POST',
            contentType: 'application/json',
            headers: {
                'Authorization': `Bearer ${token}`
            },
            data: JSON.stringify(data),
            success: function(response) {
                if (response.success) {
                    alert('模型添加成功');
                    closeModelModal();
                    loadModelList();
                } else {
                    alert('添加失败: ' + response.message);
                }
            },
            error: function(xhr) {
                alert('添加失败: ' + (xhr.responseJSON?.message || '网络错误'));
            }
        });
    }
});

// 删除模型
function deleteModel(modelId) {
    if (!confirm('确定要删除此模型吗？')) {
        return;
    }

    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/llm/models/${modelId}`,
        type: 'DELETE',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success) {
                alert('模型删除成功');
                loadModelList();
            } else {
                alert('删除失败: ' + response.message);
            }
        },
        error: function(xhr) {
            alert('删除失败: ' + (xhr.responseJSON?.message || '网络错误'));
        }
    });
}

// 查看模型统计
function viewModelStats(modelId) {
    const token = localStorage.getItem('jwt_token');
    $.ajax({
        url: `/api/llm/models/${modelId}/usage`,
        type: 'GET',
        headers: {
            'Authorization': `Bearer ${token}`
        },
        success: function(response) {
            if (response.success && response.data) {
                const stats = response.data;
                let message = `模型使用统计:\n\n`;
                message += `总 Token 使用量: ${formatNumber(stats.totalTokenUsage)}\n\n`;
                
                if (stats.dailyUsage && stats.dailyUsage.length > 0) {
                    message += `近3天使用情况:\n`;
                    stats.dailyUsage.forEach(day => {
                        message += `${day.usageDate}: ${formatNumber(day.totalTokens)} tokens (${day.requestCount} 次请求)\n`;
                    });
                } else {
                    message += `近3天暂无使用记录`;
                }
                
                alert(message);
            }
        },
        error: function() {
            alert('加载使用统计失败');
        }
    });
}