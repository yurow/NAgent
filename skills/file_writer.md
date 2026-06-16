---
name: file_writer
description: 在项目工作空间内创建新文件或修改现有文件
tools: [local_file_write]
category: file
version: "1.0"
author: system
---

# 文件写入技能

用于在项目工作空间内创建新文件或修改现有文件内容。

## 使用场景

- 创建新的代码文件
- 修改现有配置文件
- 写入日志或报告
- 生成代码片段

## 参数

- `file_path`: 相对项目目录的文件路径（必填）
- `content`: 文件内容（必填）
- `mode`: 写入模式，`write`（覆盖）或 `append`（追加），默认 `write`（可选）
- `create_dirs`: 是否自动创建目录，默认 `true`（可选）

## 执行步骤

<!-- step: local_file_write -->

## 注意事项

- 只能写入项目工作目录内的文件
- 使用相对路径，禁止 `..` 等非法路径
- 覆盖模式会清空原有内容，请谨慎使用
