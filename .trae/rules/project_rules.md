---
alwaysApply: true
---
# Project Rules (Trae)

## 1. Project Structure
- Match3.Core：纯业务逻辑，无 UI 依赖，定义接口与核心流程
- Match3.Random：统一随机入口（IRandom、SeedManager、RandomDomain、RandomStreamFactory）
- Match3.Web：应用装配与视图层（IGameView、输入意图）
- Match3.Tests：单元/场景测试（含编码规范）
- Match3.ConsoleDemo：控制台演示 UI（若存在）

## 2. Code Style & Conventions
- 4 空格缩进；Allman 大括号；文件级命名空间；私有字段 _camelCase；公共成员/类型 PascalCase；接口 I 前缀；类型明显用 var

## 3. Design Patterns
- 模型驱动：Core 为唯一真源；坐标实时；用 Update/Tick 推进
- 视图只渲染：禁止插值/物理/独立计时（禁用 CSS 过渡/Task.Delay 位移）
- 分层职责：Controller 管理逻辑与状态；IGameView 仅渲染与输入意图；依赖注入用构造函数

## 4. Best Practices
- 先看上下文：修改/新增前检索既有文件与约定
- 复用现有结构：如 Position、GameBoard
- 安全：空值检查，Try* 模式
- 注释：公共 API 用 XML；复杂说明“为何/如何”；简单不注
- 验证：实现后运行测试并确认行为

## 5. Git
- 提交信息用祈使句且具体；原子提交，配置/逻辑/文档分开，避免混合变更

## 6. Autonomous Workflow（面向助手）
- 规划 → 测试 → 实现 → dotnet test → 通过交付；失败修复

## 7. Documentation Maintenance
- 重大组件/算法/公共接口变更时更新 ARCHITECTURE.md，保持与代码一致
- 新增/修改功能时更新相关文档（注释、README.md）
