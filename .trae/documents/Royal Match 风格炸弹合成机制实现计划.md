# 核心机制实现方案：Royal Match 风格炸弹合成

根据您的需求，我们将实现一套高仿 Royal Match 的炸弹合成机制。

## 1. 核心需求确认 (Requirements)

我们将支持以下 4 种核心合成模式（优先级从高到低）：

| 优先级 | 炸弹名称 | 对应形状 (ASCII) | 消除效果 |
| :--- | :--- | :--- | :--- |
| **1** | **光球 (Light Ball)** | `OOOOO` (直线5连) | 消除场上所有同色块 |
| **2** | **TNT (炸弹)** | `T`型, `L`型, `+`型 (5+连) | 消除 3x3 范围 (两轮) |
| **3** | **火箭 (Rocket)** | `OOOO` (直线4连) | 消除整行或整列 |
| **4** | **螺旋桨 (Propeller)** | `OO` <br> `OO` (2x2 正方形) | 消除相邻+随机打击一个目标 |

> **关键变更**：传统三消只认“直线3连”，我们需要修改核心算法，使其承认“2x2正方形”也是一种合法的消除（即使没有凑够3连直线）。

## 2. 技术实现路径 (Implementation Path)

我们将分 4 步完成代码重构与实现：

### 第一步：重构基础类型 (Refactor Core Types)
- **修改 `BombType` 枚举**：
  - 重命名 `Ufo` -> `Propeller` (螺旋桨)
  - 重命名 `Square3x3` -> `Tnt` (炸弹)
  - 重命名 `Color` -> `LightBall` (光球)
  - 明确 `Horizontal`/`Vertical` 为 `Rocket` (火箭)
- **更新 `MatchGroup` 结构**：
  - 增加 `MatchShape Shape` 字段，用于调试和逻辑判断。

### 第二步：升级匹配算法 (Upgrade MatchFinder)
- **重写 `ClassicMatchFinder`**：
  - **当前逻辑**：只找直线。
  - **新逻辑**：
    1. 使用 **Flood Fill (泛洪算法)** 找出所有同色连通块。
    2. 对每个连通块进行**几何分析**（计算包围盒 Width/Height 和 Tile 数量）。
    3. **特殊规则**：如果发现 `2x2` 的正方形结构，即使没有 3 连直线，也视为有效匹配。

### 第三步：实现模式识别系统 (Pattern Recognition)
- **创建 `PatternAnalyzer` (无GC优化)**：
  - 输入：一个连通块 (`List<Position>`) + 用户交换焦点 (`Focus Position`)。
  - 输出：`BombType` + `SpawnPosition`。
  - **算法逻辑**：
    ```csharp
    if (count >= 5 && isLinear) return LightBall;
    if (count >= 5 && !isLinear) return Tnt; // T or L shape
    if (count == 4 && isLinear) return Rocket;
    if (isSquare2x2) return Propeller;
    ```
- **生成位置策略**：
  - 优先生成在**用户交换的目标格**（如果有）。
  - 否则生成在连通块的几何中心或掉落着陆点。

### 第四步：清理与效果实现 (Clear & Effects)
- **更新 `StandardMatchProcessor`**：
  - 适配新的 `BombType`。
  - 实现 Propeller 的逻辑：先炸周围，再随机寻找一个目标（优先找任务目标/障碍物）。

## 3. 质量保证 (QA)
- **单元测试**：编写 ASCII 字符画测试用例，输入字符矩阵，断言输出的炸弹类型。
- **性能**：使用对象池 (`Pools`) 避免 `List` 和 `HashSet` 的 GC 分配。

请确认此方案，确认后我将开始编写代码。