---
allowed-tools: Read, Grep, Glob, Bash(dotnet:*)
description: 运行完整的代码质量和测试检查
---

# 完整项目检查

执行以下检查流程：

1. **代码质量检查** - 验证编码规范和架构约束
2. **测试覆盖检查** - 验证测试完整性
3. **构建验证** - 运行 `dotnet build` 确保无警告
4. **测试运行** - 运行 `dotnet test` 确保通过

参考规则文件：
- `.trae/rules/project_rules.md`
- `docs/testing-guidelines.md`

完成后输出综合报告。
