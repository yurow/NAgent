$(document).ready(function() {
    const API_BASE_URL = window.location.origin;

    // ⭐ 页面加载时检查是否已初始化，如果已初始化则跳转
    $.ajax({
        url: `${API_BASE_URL}/api/initialization/status`,
        type: 'GET',
        success: function(response) {
            if (response.success && response.data && response.data.isInitialized) {
                // 已初始化，跳转到登录页
                window.location.replace('/login.html');
            }
        }
    });

    // 表单提交处理
    $('#initForm').on('submit', function(e) {
        e.preventDefault();
        
        // 清除之前的错误
        clearErrors();
        
        // 获取表单数据
        const formData = {
            username: $('#username').val().trim(),
            email: $('#email').val().trim(),
            password: $('#password').val(),
            confirmPassword: $('#confirmPassword').val()
        };
        
        // 客户端验证
        if (!validateForm(formData)) {
            return;
        }
        
        // 禁用按钮，显示加载状态
        setLoading(true);
        
        // 调用初始化 API
        $.ajax({
            url: `${API_BASE_URL}/api/initialization/initialize`,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({
                username: formData.username,
                email: formData.email,
                password: formData.password
            }),
            success: function(response) {
                if (response.success) {
                    showSuccess();
                    // 3秒后跳转到登录页面
                    setTimeout(function() {
                        window.location.href = '/login.html';
                    }, 3000);
                } else {
                    showError(response.message || '初始化失败');
                }
            },
            error: function(xhr) {
                let errorMessage = '初始化失败，请稍后重试';
                
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = '请求参数错误，请检查输入';
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
        if (username.length > 0 && (username.length < 3 || username.length > 50)) {
            showFieldError('username', '用户名长度必须在3-50个字符之间');
        }
    });
    
    $('#email').on('blur', function() {
        const email = $(this).val().trim();
        if (email.length > 0 && !isValidEmail(email)) {
            showFieldError('email', '邮箱格式不正确');
        }
    });
    
    $('#password').on('blur', function() {
        const password = $(this).val();
        if (password.length > 0 && password.length < 6) {
            showFieldError('password', '密码长度至少为6个字符');
        }
    });
    
    $('#confirmPassword').on('blur', function() {
        const password = $('#password').val();
        const confirmPassword = $(this).val();
        if (confirmPassword.length > 0 && password !== confirmPassword) {
            showFieldError('confirmPassword', '两次输入的密码不一致');
        }
    });
    
    // 验证表单
    function validateForm(data) {
        let isValid = true;
        
        // 验证用户名
        if (!data.username) {
            showFieldError('username', '用户名不能为空');
            isValid = false;
        } else if (data.username.length < 3 || data.username.length > 50) {
            showFieldError('username', '用户名长度必须在3-50个字符之间');
            isValid = false;
        }
        
        // 验证邮箱
        if (!data.email) {
            showFieldError('email', '邮箱不能为空');
            isValid = false;
        } else if (!isValidEmail(data.email)) {
            showFieldError('email', '邮箱格式不正确');
            isValid = false;
        }
        
        // 验证密码
        if (!data.password) {
            showFieldError('password', '密码不能为空');
            isValid = false;
        } else if (data.password.length < 6) {
            showFieldError('password', '密码长度至少为6个字符');
            isValid = false;
        }
        
        // 验证确认密码
        if (!data.confirmPassword) {
            showFieldError('confirmPassword', '请确认密码');
            isValid = false;
        } else if (data.password !== data.confirmPassword) {
            showFieldError('confirmPassword', '两次输入的密码不一致');
            isValid = false;
        }
        
        return isValid;
    }
    
    // 验证邮箱格式
    function isValidEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
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
    
    // 显示成功消息
    function showSuccess() {
        $('#successMessage').show();
        $('#initForm')[0].reset();
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
