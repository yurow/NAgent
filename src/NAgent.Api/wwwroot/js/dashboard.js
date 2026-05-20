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
            if (text.includes('模型管理') || text.includes('用户管理')) {
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
        } else if (menuText.includes('AI Agent')) {
            showAgentChat();
        } else if (menuText.includes('模型管理')) {
            showModelManagement();
        } else if (menuText.includes('用户管理')) {
            showUserManagement();
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

// 初始化管理员导航
function setupAdminNavigation() {
    // 管理员可以看到所有菜单项
}

// ⭐ 初始化 AI Agent 聊天
function initAgentChat() {
    let sessionId = generateSessionId();

    // 加载可用模型列表
    loadAvailableModels();

    // 发送消息按钮点击
    $('#sendBtn').on('click', function() {
        sendMessage(sessionId);
    });

    // Enter 键发送（Shift+Enter 换行）
    $('#chatInput').on('keydown', function(e) {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            sendMessage(sessionId);
        }
    });

    // 清空对话按钮
    $('#clearChatBtn').on('click', function() {
        clearChat();
        sessionId = generateSessionId(); // 生成新的会话ID
    });
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
function sendMessage(sessionId) {
    const input = $('#chatInput');
    const message = input.val().trim();

    if (!message) {
        return;
    }

    // 获取选中的模型
    const selectedModel = $('#modelSelect').val();

    // 清空输入框
    input.val('');

    // 添加用户消息到聊天窗口
    addUserMessage(message);

    // 显示加载指示器
    showLoading();

    // 禁用发送按钮
    $('#sendBtn').prop('disabled', true);

    // 调用 API
    const token = localStorage.getItem('jwt_token');
    const requestData = {
        sessionId: sessionId,
        userInput: message
    };
    
    // 如果选择了模型，添加到请求中
    if (selectedModel) {
        requestData.modelId = selectedModel;
    }
    
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
                addBotMessage(response.data);
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

// 添加机器人消息
function addBotMessage(message) {
    const time = getCurrentTime();
    const html = `
        <div class="message bot-message">
            <div class="message-avatar">🤖</div>
            <div class="message-content">
                <p>${escapeHtml(message)}</p>
                <span class="message-time">${time}</span>
            </div>
        </div>
    `;
    $('#chatMessages').append(html);
    scrollToBottom();
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
    html += '<th>用户名</th><th>邮箱</th><th>角色</th><th>状态</th></tr></thead><tbody>';

    users.forEach(user => {
        html += `<tr>
            <td>${escapeHtml(user.username)}</td>
            <td>${escapeHtml(user.email)}</td>
            <td>${user.isAdmin ? '管理员' : '普通用户'}</td>
            <td>${user.isActive ? '活跃' : '禁用'}</td>
        </tr>`;
    });

    html += '</tbody></table>';
    $('#userList').html(html);
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