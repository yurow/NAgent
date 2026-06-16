---
name: project_initializer
description: 初始化新项目工作空间，创建 init.md 和 spec.md，分析项目用途
tools: [list_workspace_files, local_file_write]
category: project
version: "1.0"
author: system
needs_further_processing: false
---

# 项目初始化技能

用于初始化新项目的工作空间，创建必要的文档文件，并分析项目用途。

## 使用场景

- 用户首次创建项目
- 需要重新初始化项目结构
- 生成项目规范文档

## 执行步骤

<!-- step: list_workspace_files -->
<!-- step: local_file_write -->

## 初始化流程

1. 检查工作目录是否存在
2. 创建 `init.md`（项目初始化标记）
3. 创建 `spec.md`（项目规范文档）
4. 分析项目文件结构推测用途

## 输出

返回初始化完成的确认信息，包括工作目录路径和创建的文件列表。
