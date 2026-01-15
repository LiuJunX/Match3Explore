---
name: test-check
description: |
  测试覆盖率和质量检查，验证测试完整性。
  用于 PR 审查、新功能验证、回归测试。
allowed-tools: Read, Grep, Glob, Bash(dotnet test:*)
---

# 测试覆盖率检查

根据 `docs/testing-guidelines.md` 验证测试质量。

## 测试要求清单

### 1. 输入变体测试
- 测试所有可能的输入变体（方向、位置、边界情况）
- 正向和负向场景
- 边界条件和极端值

### 2. 多系统集成测试
- 使用真实系统进行集成测试，不仅仅 Stub 隔离
- 验证跨系统交互行为
- 检查系统间的数据流

### 3. 异步/多帧行为测试
- 验证中间状态，不只是最终结果
- 检查状态转换的正确性
- 使用 `AnimationTestHelper` 进行多帧测试

### 4. 跨系统稳定性
- 当多个系统交互时，检查所有相关系统的稳定性条件
- 验证 `IsStable` 状态
- 检查物理系统和动画系统的协调

## 执行流程

1. 识别修改的源文件
2. 查找对应的测试文件
3. 验证测试覆盖新增/修改的方法
4. 运行 `dotnet test` 验证通过
5. 检查测试是否符合上述清单

## 测试文件映射

| 源文件 | 测试文件 |
|--------|---------|
| `Match3.Core/Systems/**/*.cs` | `Match3.Core.Tests/Systems/**/*Tests.cs` |
| `Match3.Editor/**/*.cs` | `Match3.Editor.Tests/**/*Tests.cs` |
| `Match3.Web/**/*.cs` | `Match3.Web.Tests/**/*Tests.cs` |

## 输出格式

```
## 测试检查结果

### 测试覆盖
- 源文件: X 个
- 测试文件: Y 个
- 覆盖率: Z%

### 缺失测试
- [ ] ClassName.MethodName - 无对应测试

### 测试质量
- [x] 输入变体覆盖
- [x] 集成测试存在
- [ ] 多帧行为验证

### 测试运行
- 通过: N 个
- 失败: M 个
- 跳过: K 个
```
