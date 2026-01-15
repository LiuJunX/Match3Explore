---
name: project-check
description: |
  C# 代码质量检查，验证项目编码规范和架构约束。
  用于代码审查、提交前检查、PR 审查。
allowed-tools: Read, Grep, Glob, Bash(dotnet build:*), Bash(dotnet test:*)
---

# 项目代码质量检查

结合通用检查工具和项目特定规则进行全面代码审查。

## 检查清单

### 1. 代码风格（来自 .trae/rules/project_rules.md）

- **缩进**：4 个空格（禁用 Tab）
- **大括号**：Allman 风格（左括号独占一行）
- **命名空间**：文件级命名空间 `namespace Match3.Core;`
- **命名约定**：
  - Private 字段：`_camelCase`
  - Public 成员：`PascalCase`
  - 接口：`I` 前缀

### 2. 架构约束

- `Match3.Core` 绝不能引用 `Match3.Web`
- Logic 类必须保持无状态
- 热路径集合必须使用 `Pools.ObtainList<T>()` / `Pools.Release()`
- 随机数必须使用 `Match3.Random` 接口，禁止 `System.Random`

### 3. 热路径优化

检查 Update/Tick 方法中：
- 禁止 `new List<T>()` / `new HashSet<T>()`
- 禁止字符串插值 `$"..."`
- 使用 `IGameLogger.LogInfo<T>()` 模板日志

### 4. 文档要求

- 公共 API 必须有 XML 注释
- 复杂逻辑说明"为何"而非"什么"

## 执行流程

1. 扫描修改的 .cs 文件
2. 检查代码风格违规
3. 验证架构约束
4. 运行 `dotnet build` 检查编译警告
5. 输出结构化报告

## 输出格式

```
## 检查结果

### 违规项
- [ ] 文件:行号 - 问题描述
      修复建议: 代码示例

### 通过的检查
- [x] Allman 大括号
- [x] 命名约定
- [x] 架构约束
- [x] 集合使用

### 建议
- 可选的改进建议
```
