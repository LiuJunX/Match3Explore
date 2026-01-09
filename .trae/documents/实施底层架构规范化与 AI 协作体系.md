我们将分四个阶段实施“底层架构规范化与 AI 协作体系”：

### 阶段 1：建立 AI 可读的架构“法典” (Documentation)
1.  **创建 `docs/ARCHITECTURE_GUIDE.md`**
    *   这将是项目的核心技术白皮书。
    *   内容包含：分层架构图解、底层服务（对象池、日志）使用规范、以及给 AI 的特定指令。
2.  **更新 `.trae/rules/project_rules.md`**
    *   增加 **[AI Context Guidelines]** 章节。
    *   将架构规则转化为 AI 的系统提示词（System Prompt），例如：“Core 层严禁引用 Web 层”、“高频对象必须使用对象池”。

### 阶段 2：代码即文档 - 核心组件标准化 (Standardization)
1.  **增强 `IObjectPool<T>` & `GenericObjectPool<T>`**
    *   添加详细的 XML 注释。
    *   注入 `@ai-usage-note` 标记，明确指示 AI：“在热路径中必须使用 Rent/Return，禁止直接 new”。
2.  **增强 `IGameLogger`**
    *   添加标记，指示 AI：“业务逻辑中禁止使用 Console.WriteLine，必须通过此接口注入”。

### 阶段 3：引入自动化架构守护 (Architecture Enforcement)
1.  **安装 `NetArchTest.Rules`**
    *   在 `Match3.Tests` 项目中引入此库，用于编写架构单元测试。
2.  **编写架构测试用例**
    *   `LayerTests.cs`: 验证 `Match3.Core` 不依赖 `Match3.Web`。
    *   `NamingTests.cs`: 验证接口是否以 `I` 开头，实现类是否符合命名规范。
    *   `DependencyTests.cs`: (可选) 验证 Logic 层是否正确依赖 Interfaces 而非具体实现。

### 阶段 4：验证与交付 (Verification)
1.  运行 `dotnet test` 确保架构测试通过。
2.  确认 AI 能够理解新的规范（通过简单的提问测试）。

---
**执行顺序**：文档 -> 代码注解 -> 架构测试 -> 验证。
