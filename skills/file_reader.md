---
name: file_reader
description: 读取项目工作空间内的文件内容，支持代码文件、配置文件、文档等
tools: [local_file_read]
category: file
version: "1.0"
author: system
---

# 文件读取技能

用于读取项目工作空间内的文件内容，支持各种文本文件。

## 使用场景

- 查看代码文件内容
- 读取配置文件
- 查看日志文件
- 分析文档内容

## 参数

- `file_path`: 相对项目目录的文件路径（必填）
- `encoding`: 文件编码，默认 utf-8（可选）

## 执行步骤

<!-- step: local_file_read -->

## 注意事项

- 只能读取项目工作目录内的文件
- 文件大小限制 1MB
- 支持自动截断显示（超过 10000 字符）
