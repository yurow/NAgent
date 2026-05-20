$(document).ready(function() {
    const API_BASE_URL = window.location.origin;

    // 表单提交处理
    $('#loginForm').on('submit', function(e) {
        e.preventDefault();
        
        // 清除之前的错误
        clearErrors();
        
        // 获取表单数据
        const formData = {
            username: $('#username').val().trim(),
            password: $('#password').val()
        };
        
        // 客户端验证
        if (!validateForm(formData)) {
            return;
        }
        
        // 禁用按钮，显示加载状态
        setLoading(true);
        
        // 调用登录 API
        $.ajax({
            url: `${API_BASE_URL}/api/auth/login`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(formData),
            success: function(response) {
                if (response.success && response.data && response.data.token) {
                    // 保存 Token 到 localStorage
                    localStorage.setItem('jwt_token', response.data.token);
                    localStorage.setItem('user_info', JSON.stringify(response.data.user));
                    
                    // 跳转到首页或仪表盘
                    window.location.href = '/dashboard.html';
                } else {
                    showError(response.message || '登录失败');
                }
            },
            error: function(xhr) {
                let errorMessage = '登录失败，请检查用户名和密码';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 401) {
                    errorMessage = '用户名或密码错误';
                } else if (xhr.status === 400) {
                    errorMessage = '请求参数错误';
                } else if (xhr.status === 500) {
                    errorMessage = '服务器内部错误';
                }
                
                showError(errorMessage);
            },
            complete: function() {
                setLoading(false);
            }
        });
    });
    
    // 实时验证
    $('#username').on('blur', function() {
        const username = $(this).val().trim();
        if (username.length === 0) {
            showFieldError('username', '用户名不能为空');
        }
    });
    
    $('#password').on('blur', function() {
        const password = $(this).val();
        if (password.length === 0) {
            showFieldError('password', '密码不能为空');
        }
    });
    
    // 验证表单
    function validateForm(data) {
        let isValid = true;
        
        // 验证用户名
        if (!data.username) {
            showFieldError('username', '用户名不能为空');
            isValid = false;
        }
        
        // 验证密码
        if (!data.password) {
            showFieldError('password', '密码不能为空');
            isValid = false;
        }
        
        return isValid;
    }
    
    // 显示字段错误
    function showFieldError(fieldName, message) {
        $(`#${fieldName}`).addClass('error');
        $(`#${fieldName}Error`).text(message);
    }
    
    // 清除所有错误
    function clearErrors() {
        $('.form-group input').removeClass('error');
        $('.error-message').text('');
        $('#errorMessage').hide();
    }
    
    // 显示错误消息
    function showError(message) {
        $('#errorMessage').text(message).show();
    }
    
    // 设置加载状态
    function setLoading(isLoading) {
        const submitBtn = $('#submitBtn');
        const btnText = submitBtn.find('.btn-text');
        const btnLoading = submitBtn.find('.btn-loading');
        
        if (isLoading) {
            submitBtn.prop('disabled', true);
            btnText.hide();
            btnLoading.show();
        } else {
            submitBtn.prop('disabled', false);
            btnText.show();
            btnLoading.hide();
        }
    }
});
