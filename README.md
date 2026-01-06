# ThreeMatchTrea
使用AI写三消游戏

## 🚀 快速开发指南 (Development Workflow)

为了提高开发效率，本项目配置了两种快速测试与运行方案：

### 1. ⚡ 极速自动化测试 (无浏览器)

使用 **bUnit** 框架，无需启动浏览器即可在内存中验证游戏逻辑和 UI 组件。速度极快（毫秒级），推荐在编写逻辑代码后立即运行。

**运行方式：**
在终端执行：
```powershell
dotnet test
```

**包含内容：**
- `Match3.Tests`: 核心游戏逻辑单元测试 (规则、消除、掉落算法)。
- `Match3.Web.Tests`: Web 前端组件测试 (页面渲染、点击交互、状态更新)。

---

### 2. 🔥 极速热重载开发 (Hot Reload)

使用 `dotnet watch` 模式启动 Web 项目。支持**代码热替换**，修改文件保存后浏览器自动更新，无需重启项目。
此外，配套脚本会自动检测并清理端口占用 (5015)，防止启动失败。

**运行方式：**
在终端执行 (或双击文件)：
```cmd
.\run-web.bat
```

**脚本功能：**
- 自动检测端口 `5015` 是否被占用。
- 自动杀掉占用端口的僵尸进程。
- 启动 `dotnet watch` 监听文件变更。

---

## 项目结构
- `src/Match3.Core`: 核心游戏逻辑 (纯 C#, 无 UI 依赖)。
- `src/Match3.Web`: Blazor WebAssembly 前端界面。
- `src/Match3.Tests`: 核心逻辑测试项目。
- `src/Match3.Web.Tests`: 前端组件测试项目 (bUnit)。
