---
name: art
description: |
  从 Match3Art 同步美术资源到 Unity 项目。

  触发词：同步美术、更新美术、拉取模型、art同步

  不触发：创建模型、修改模型（在 Match3Art 工程中操作）
allowed-tools: Bash(python:*), Bash(ls:*), Read
---

# 美术资源同步

## 执行流程

### 1. 运行同步脚本

```bash
python ../Match3Art/scripts/export_to_unity.py
```

从 `../Match3Art/Export/` 复制最新 FBX 到 `unity/Assets/Resources/Art/`。

### 2. 验证同步结果

```bash
ls -la unity/Assets/Resources/Art/Gems/Models/
```

### 3. 输出结果

```
## 美术同步完成

| 资源 | 状态 |
|------|------|
| Gem.fbx | 已更新 / 已跳过(无变化) |

Unity Resources 位置：`unity/Assets/Resources/Art/`
```

## 错误处理

### Match3Art 不存在
- 提示：美术工程未找到，请确认 `../Match3Art/` 目录存在

### 无导出文件
- 提示：Export/ 下没有 FBX 文件，请先在 Match3Art 中运行导出
