---
name: web_researcher
description: 搜索网络信息并深入抓取详情页，获取实时资讯、技术文档、解决方案等。支持多轮搜索+URL抓取。
tools: [web_search, web_fetch]
category: search
version: "2.0"
author: system
needs_further_processing: true
---

# 网络研究技能

用于搜索网络信息，并可以点击进入搜索结果页面获取详细内容。支持多轮深入研究。

## 使用场景

- 查找技术问题的解决方案
- 获取最新技术资讯
- 搜索 API 文档
- 了解第三方库的使用方法
- 查询实时信息（天气、新闻等）
- 需要深入了解某个搜索结果时（通过 web_fetch 抓取详情）

## 参数

- `query`: 搜索关键词（用于 web_search，必填）
- `url`: 要抓取的网页 URL（用于 web_fetch，可选）
- `max_results`: 最大结果数，默认 10，范围 1-10（可选）

## 执行步骤

<!-- step: web_search | condition: query -->
<!-- step: web_fetch | condition: url -->

## 研究策略

1. **初始搜索**: 先用 web_search 搜索关键词，获取 10 条结果
2. **评估结果**: 检查搜索结果摘要是否充分回答了用户问题
3. **深入抓取**: 如果搜索结果只有摘要不够详细，用 web_fetch 抓取最相关的 URL 获取详情
4. **补充搜索**: 如果第一轮搜索结果不充分，可以换关键词再次搜索
5. **如实反馈**: 如果确实找不到更多信息，如实说明已搜索到的内容，不要编造

## 注意事项

- 使用百度 + Bing 双引擎搜索（百度优先，Bing 备用）
- 返回结果包含标题、摘要和链接
- 可以用 web_fetch 抓取搜索结果中的 URL 获取页面正文
- 网络搜索有 30 秒超时限制，web_fetch 有 15 秒超时限制
- 如果搜索结果不足以回答问题，应主动尝试不同关键词或用 web_fetch 深入查看
