---
name: code-analysis
description: 分析代码质量、结构和潜在问题
category: development
version: 1.0.0
author: NAgent
---

# 代码分析 Skill

## 概述

本 Skill 提供代码分析能力，可以检查代码质量、识别潜在问题、建议改进方案。

## 能力

- 代码风格检查
- 潜在 Bug 检测
- 性能优化建议
- 安全漏洞扫描
- 代码复杂度分析

## Tools

- code_linter
- security_scanner
- complexity_analyzer

## 示例

#### 示例 1: 分析 Python 代码

Input:
```python
def calculate_sum(numbers):
    result = 0
    for i in range(len(numbers)):
        result = result + numbers[i]
    return result
```

Output:
```
问题:
1. 使用了 range(len()) 反模式，应该使用 for num in numbers
2. 可以使用内置 sum() 函数简化

建议:
- def calculate_sum(numbers):
-     return sum(numbers)
```

#### 示例 2: 检查 SQL 注入风险

Input:
```python
query = "SELECT * FROM users WHERE name = '" + user_input + "'"
```

Output:
```
安全风险: SQL 注入
建议: 使用参数化查询
query = "SELECT * FROM users WHERE name = ?"
cursor.execute(query, (user_input,))
```
