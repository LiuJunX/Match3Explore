---
name: test
description: |
  运行测试（支持多种范围）。

  触发词：运行测试、跑测试、测试一下、test
  范围词：
  - 核心/core → Core + Random + Pool 测试
  - web → Web 测试
  - unity → Unity Editor 测试
  - 全量/所有/全部 → 所有测试

  不触发：测试覆盖率、缺什么测试（用 test-check skill）
allowed-tools: Read, Bash(dotnet test:*), Bash(echo:*), Write
---

# 测试运行

## 范围识别

| 关键词 | 范围 | 项目 |
|--------|------|------|
| 核心/core | Core | Match3.Core.Tests, Match3.Random.Tests, Match3.Core.PoolTests |
| web | Web | Match3.Web.Tests |
| editor/编辑器 | Editor | Match3.Editor.Tests |
| unity | Unity | Unity Editor Tests (触发器模式) |
| 全量/所有/all | 全部 | 以上所有 |
| (无范围词) | Core | 默认运行核心测试 |

## 执行流程

### 1. 识别范围
根据用户输入识别要运行的测试范围。

### 2. 运行 dotnet 测试

```bash
# Core 测试
dotnet test src/Match3.Core.Tests src/Match3.Random.Tests src/Match3.Core.PoolTests --nologo --verbosity minimal

# Web 测试
dotnet test src/Match3.Web.Tests --nologo --verbosity minimal

# Editor 测试
dotnet test src/Match3.Editor.Tests --nologo --verbosity minimal

# 全量 dotnet 测试
dotnet test --nologo --verbosity minimal
```

### 3. 运行 Unity 测试（如果需要）

Unity 测试使用触发器模式，编辑器需要已打开：

```bash
# 1. 写入触发文件
echo %date% %time% > unity/.test-trigger

# 2. 等待结果（轮询 test-results.json）
# 检查 .test-running 文件是否存在来判断测试是否还在运行

# 3. 读取结果
# unity/test-results.json
```

### 4. 解析 Unity 测试结果

读取 `unity/test-results.json`：

```json
{
  "total": 27,
  "passed": 25,
  "failed": 2,
  "duration": 1.23,
  "tests": [
    {
      "name": "TestName",
      "className": "TestClass",
      "status": "Failed",
      "message": "Expected X but was Y",
      "stackTrace": "...",
      "sourceFile": "Assets/Tests/Editor/TestFile.cs",
      "sourceLine": 42
    }
  ]
}
```

### 5. 失败分析与修复循环

如果有测试失败：

1. **读取失败测试代码**：根据 sourceFile 和 sourceLine
2. **读取被测源码**：分析测试目标
3. **分析失败原因**：比较预期 vs 实际
4. **修复代码**：编辑源文件
5. **重新运行测试**：验证修复
6. **循环**：直到全部通过或达到重试上限（3次）

### 6. 输出报告

```
## 测试执行报告

### Core 测试
✅ 156 通过 | ❌ 0 失败 | ⏱️ 2.3s

### Unity 测试
✅ 25 通过 | ❌ 2 失败 | ⏱️ 1.2s

#### 失败详情
1. **ObjectPoolTests.MaxSizeZero_MeansUnlimited**
   - 位置: Assets/Tests/Editor/ObjectPoolTests.cs:185
   - 错误: Expected 100 but was 0
   - 原因: [分析]
   - 修复: [已修复/需要手动处理]

### 总计
✅ 181 通过 | ❌ 2 失败
```

## Unity 测试前置条件

- Unity/团结引擎编辑器必须已打开项目
- 编辑器可以在后台运行
- 首次打开编辑器后需要等待编译完成

## 错误处理

### Unity 编辑器未打开
如果 30 秒内没有生成 test-results.json：
```
⚠️ Unity 测试超时
请确保 Unity 编辑器已打开项目 unity/
```

### dotnet test 失败
显示错误信息，不中断其他测试范围。
