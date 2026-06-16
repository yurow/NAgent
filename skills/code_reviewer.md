---
name: code_reviewer
description: 读取代码文件并进行审查分析，可结合网络搜索获取最佳实践
tools: [local_file_read, web_search]
category: code
version: "1.0"
author: system
needs_further_processing: false
---

# 代码审查技能

用于读取项目中的代码文件并进行分析审查，可结合网络搜索获取最佳实践和常见问题。

## 使用场景

- 代码质量检查
- 潜在 Bug 识别
- 性能优化建议
- 安全漏洞检查
- 代码风格审查

## 参数

- `file_path`: 要审查的代码文件路径（必填）
- `focus`: 审查重点，如 `security`、`performance`、`style`（可选）

## 执行步骤

<!-- step: local_file_read | condition: file_path -->
<!-- step: web_search -->

## 审查维度

1. **语法正确性** - 检查明显的语法错误
2. **逻辑问题** - 识别潜在的逻辑 Bug
3. **性能问题** - 发现性能瓶颈
4. **安全隐患** - 检查 SQL 注入、XSS 等安全问题
5. **代码风格** - 检查命名规范、注释等
6. **最佳实践** - 对比行业最佳实践

## 输出格式

返回结构化的审查报告，包含：
- 问题分类（严重/警告/建议）
- 具体问题描述
- 改进建议
- 参考链接（如有）
