---
name: web_researcher
description: 使用 DuckDuckGo 搜索网络信息，获取实时资讯、技术文档、解决方案等
tools: [web_search]
category: search
version: "1.0"
author: system
---

# 网络研究技能

用于搜索网络信息，获取最新的技术文档、解决方案、资讯等。

## 使用场景

- 查找技术问题的解决方案
- 获取最新技术资讯
- 搜索 API 文档
- 了解第三方库的使用方法
- 查询实时信息（天气、新闻等）

## 参数

- `query`: 搜索关键词（必填）
- `max_results`: 最大结果数，默认 5，范围 1-10（可选）

## 执行步骤

<!-- step: web_search -->

## 注意事项

- 使用 DuckDuckGo 搜索引擎
- 返回结果包含标题、摘要和链接
- 网络搜索有 30 秒超时限制
