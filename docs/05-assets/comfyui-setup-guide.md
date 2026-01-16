# ComfyUI 游戏美术资源生成环境搭建指南

本指南将帮助你搭建一个完整的 AI 美术资源生成工作流。

## 目录

1. [环境概述](#1-环境概述)
2. [安装 ComfyUI](#2-安装-comfyui)
3. [下载模型](#3-下载模型)
4. [安装自定义节点](#4-安装自定义节点)
5. [验证安装](#5-验证安装)
6. [常见问题](#6-常见问题)

---

## 1. 环境概述

### 你的硬件配置

| 组件 | 配置 | 状态 |
|------|------|------|
| GPU | RTX 5060 Ti (8GB VRAM) | ✅ |
| CPU | i9-14900K (24核/32线程) | ✅ |
| RAM | 64GB | ✅ |
| Python | 3.12.10 | ✅ |
| Git | 2.52.0 | ✅ |

### 推荐配置

- **基础模型**: SD 1.5 DreamShaper v8
- **VRAM 占用**: ~4GB (留有充足余量)
- **生成速度**: ~2-3秒/张

---

## 2. 安装 ComfyUI

### 2.1 选择安装目录

建议安装到非系统盘，预留 50GB+ 空间：

```powershell
# 示例：安装到 D:\AI\ComfyUI
cd D:\
mkdir AI
cd AI
```

### 2.2 克隆 ComfyUI

```powershell
git clone https://github.com/comfyanonymous/ComfyUI.git
cd ComfyUI
```

### 2.3 创建虚拟环境（推荐）

```powershell
# 创建虚拟环境
python -m venv venv

# 激活虚拟环境
.\venv\Scripts\activate
```

### 2.4 安装 PyTorch (CUDA 12.x)

```powershell
# RTX 5060 Ti 使用 CUDA 12.x 版本
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu124
```

### 2.5 安装 ComfyUI 依赖

```powershell
pip install -r requirements.txt
```

### 2.6 首次启动测试

```powershell
python main.py
```

启动成功后会显示：
```
Starting server
To see the GUI go to: http://127.0.0.1:8188
```

在浏览器打开 http://127.0.0.1:8188 验证界面正常。

---

## 3. 下载模型

### 3.1 目录结构

ComfyUI 的模型目录结构：

```
ComfyUI/
├── models/
│   ├── checkpoints/     <- 基础模型 (.safetensors)
│   ├── loras/           <- LoRA 模型
│   ├── vae/             <- VAE 模型
│   ├── controlnet/      <- ControlNet 模型
│   └── embeddings/      <- 文本嵌入
```

### 3.2 基础模型下载

**推荐模型**: DreamShaper v8 (SD 1.5 微调版，擅长风格化内容)

下载地址（选择 safetensors 格式）：
- Civitai: https://civitai.com/models/4384/dreamshaper
- HuggingFace: https://huggingface.co/Lykon/DreamShaper

下载后放入：`ComfyUI/models/checkpoints/`

文件名示例：`dreamshaper_8.safetensors` (~2GB)

### 3.3 游戏图标 LoRA 下载

**推荐 LoRA** (增强游戏图标生成效果):

| LoRA 名称 | 用途 | 下载链接 |
|-----------|------|----------|
| Game Icon Institute | 游戏图标风格 | [Civitai](https://civitai.com/models/47800) |
| Flat Color Anime | 扁平卡通风格 | [Civitai](https://civitai.com/models/35960) |
| Pixel Art | 像素风格(可选) | [Civitai](https://civitai.com/models/43820) |

下载后放入：`ComfyUI/models/loras/`

### 3.4 VAE 模型（可选但推荐）

VAE 可以改善图像颜色质量。

推荐：`vae-ft-mse-840000-ema-pruned.safetensors`
- 下载: https://huggingface.co/stabilityai/sd-vae-ft-mse-original

下载后放入：`ComfyUI/models/vae/`

---

## 4. 安装自定义节点

### 4.1 节点管理器（必装）

ComfyUI-Manager 让节点安装更简单：

```powershell
cd ComfyUI/custom_nodes
git clone https://github.com/ltdrdata/ComfyUI-Manager.git
```

重启 ComfyUI 后，界面右侧会出现 "Manager" 按钮。

### 4.2 必要节点列表

通过 ComfyUI-Manager 安装以下节点：

| 节点 | 用途 | 安装方式 |
|------|------|----------|
| **ComfyUI-Impact-Pack** | 图像处理增强 | Manager 搜索安装 |
| **ComfyUI_essentials** | 基础工具集 | Manager 搜索安装 |
| **comfyui-tooling-nodes** | 批处理支持 | Manager 搜索安装 |

### 4.3 背景移除节点

用于自动去除生成图像的背景：

```powershell
cd ComfyUI/custom_nodes
git clone https://github.com/Acly/comfyui-tooling-nodes.git
```

还需要安装 rembg：

```powershell
# 在 ComfyUI 虚拟环境中
pip install rembg[gpu]
```

### 4.4 重启 ComfyUI

安装完节点后，重启 ComfyUI 使其生效：

```powershell
# Ctrl+C 停止当前运行
python main.py
```

---

## 5. 验证安装

### 5.1 检查清单

| 检查项 | 验证方法 |
|--------|----------|
| ComfyUI 启动正常 | 访问 http://127.0.0.1:8188 |
| 模型加载成功 | 左侧面板能看到 checkpoints |
| LoRA 可用 | 左侧面板能看到 loras |
| Manager 工作 | 右侧有 Manager 按钮 |

### 5.2 简单生成测试

1. 打开 ComfyUI 界面
2. 点击 "Load Default" 加载默认工作流
3. 在 "Load Checkpoint" 节点选择 `dreamshaper_8.safetensors`
4. 在正向提示词输入：
   ```
   game icon, red crystal gem, cartoon style, transparent background, centered, simple
   ```
5. 点击 "Queue Prompt" 生成

如果成功生成图像，说明环境搭建完成。

### 5.3 API 测试

确保 API 可用（后续自动化脚本需要）：

```powershell
curl http://127.0.0.1:8188/system_stats
```

应返回 JSON 格式的系统信息。

---

## 6. 常见问题

### Q1: CUDA out of memory

**原因**: VRAM 不足

**解决**:
```powershell
# 启动时添加低显存参数
python main.py --lowvram
```

### Q2: 模型加载失败

**原因**: 文件损坏或格式不对

**解决**:
- 确保下载的是 `.safetensors` 格式
- 重新下载模型文件
- 检查文件完整性

### Q3: 节点报错

**原因**: 依赖缺失

**解决**:
```powershell
# 重新安装节点依赖
cd ComfyUI/custom_nodes/[节点名]
pip install -r requirements.txt
```

### Q4: Windows Defender 拦截

**解决**: 将 ComfyUI 目录添加到 Windows Defender 排除列表

---

## 下一步

环境搭建完成后，继续阅读：
- [ComfyUI 游戏资源工作流设计](./comfyui-workflow-design.md)
- [自动化批量生成脚本](./batch-generation-script.md)

---

## 快速命令参考

```powershell
# 启动 ComfyUI
cd D:\AI\ComfyUI
.\venv\Scripts\activate
python main.py

# 启动 (低显存模式)
python main.py --lowvram

# 启动 (指定端口)
python main.py --port 8189

# 启动 (启用 API)
python main.py --enable-cors-header
```
