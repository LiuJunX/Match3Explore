# 核心逻辑分析报告：Refill 与 Gravity 的协同机制

经过对 `RealtimeRefillSystem.cs` 和 `RealtimeGravitySystem.cs` 的深度代码审查与测试用例分析，我的结论是：**这两个系统不存在逻辑冲突，而是设计严密的“生产者-消费者”协作关系。**

## 1. 核心结论
用户担心的“一格一格变化”与“直接拉到底”的矛盾，实际上是 **逻辑触发（Discrete Trigger）** 与 **物理运动（Continuous Motion）** 的分工配合，而非冲突。

## 2. 详细机制分析

### A. 职责分工
*   **RefillSystem (生产者)**: 类似于水龙头。它只关注 **Row 0 (入口)**。只要入口为空，它就生成新的 Tile。
*   **GravitySystem (消费者)**: 类似于重力场。它负责将 Tile 从当前位置搬运到 **物理上允许的最低点**。

### B. 针对“冲突”疑虑的解释
用户提到的“Gravity 直接拉到最下面”是指 `FindLowestVerticalTarget` 方法会计算出 Tile 的最终落点（例如从 Row 0 直接看准 Row 10）。这可能会让人误以为 Tile 会瞬间“瞬移”或忽略中间过程。但实际上：

1.  **目标 vs 过程**: Gravity 计算的是 **目标 (Target)**，但执行的是 **物理模拟 (SimulatePhysics)**。Tile 是根据速度 `Velocity * dt` 逐渐下落的。
2.  **逐格更新**: 随着 Tile 下落，`UpdateGridPosition` 会在 Tile 穿过网格边界时，**逐格**更新 `GameState`。
    *   当 Tile 从 Row 0 移动到 Row 1 时，Row 0 变为空。
    *   **正是这个瞬间**，`RefillSystem` 再次检测到 Row 0 为空，从而生成下一个 Tile。
    *   **结论**: Gravity 的下落运动恰恰是驱动 Refill 继续工作的动力。

### C. 连续性保障 (流体感)
代码中有一处关键的协同逻辑，证明了二者是设计好要配合的：

*   **RefillSystem.cs (L33-43)**:
    ```csharp
    // Optimization: ... spawn relative to it to create a continuous stream
    if (tileBelow.IsFalling) { startY = tileBelow.Position.Y - 1.0f; }
    ```
    这段代码明确处理了“下方 Tile 正在下落”的情况。它确保新生成的 Tile 不会呆板地出现在默认位置，而是**紧贴着**下方正在被 Gravity 加速拉走的 Tile 生成。这消除了“一格一格生成”可能带来的视觉间隙，形成了连贯的下落流。

## 3. 潜在的边缘情况 (非逻辑错误)
虽然逻辑无冲突，但在极端配置下可能会有视觉瑕疵：
*   **速度过快**: 如果 `GravitySpeed` 极大，导致一帧内位移超过 1 格（Tunneling），`UpdateGridPosition` 可能会跳过 Row 1 直接更新到 Row 2。虽然逻辑上是安全的（不会穿墙，因为 `FindLowestVerticalTarget` 保证了路径畅通），但这可能导致 Refill 的 `tileBelow` 检测（只看 Row 1）失效，从而产生瞬间的视觉间隙。
*   **建议**: 保持 `MaxFallSpeed` 在合理范围内（例如小于 `CellSize / dt`），即可保证完美衔接。

## 4. 总结
这两个脚本不仅不冲突，反而通过 **“Gravity 腾出空间 -> Refill 填补空间”** 的循环，以及 **“Refill 追踪下方 Tile 位置”** 的优化，共同实现了一个流畅的实时掉落系统。

**下一步建议**:
无需修改代码。目前的实现符合高性能 Match-3 核心（ECS-lite 风格）的标准范式。