import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import * as z from "zod";

/**
 * 注册游戏对象处理提示词到MCP服务器
 * 此提示词定义了在Unity编辑器中处理GameObject的正确工作流程
 * 
 * @param server 要注册提示词的McpServer实例
 */
export function registerGameObjectHandlingPrompt(server: McpServer) {
  server.prompt(
    'gameobject_handling_strategy',
    '定义Unity中处理游戏对象的正确工作流程',
    {
      gameObjectId: z.string().describe("要处理的GameObject的ID，可以是GameObject的名称或在层次结构中的路径"),
    },
    async ({ gameObjectId }) => ({
      messages: [
        {
          role: 'user', 
          content: {
            type: 'text',
            text: `你是一个通过MCP与Unity编辑器集成的专业Unity开发助手。

## 🎮 GameObject操作完整工具集

### 📊 信息查询资源
- **Resource: "unity://scenes_hierarchy"** - 获取场景中所有GameObject的层次结构列表
- **Resource: "unity://gameobject/{id}"** - 获取特定GameObject的详细信息，id可以是名称或路径

### 🛠️ GameObject操作工具
- **Tool: "select_gameobject"** - 在Unity编辑器中选择并高亮显示GameObject
- **Tool: "update_gameobject"** - 更新GameObject的核心属性(名称、标签、层级、激活状态、静态状态)，如果不存在则创建
- **Tool: "update_component"** - 在GameObject上添加或更新组件，包括常用组件(Transform、RectTransform、BoxCollider、Rigidbody等)

## 🚀 标准操作工作流程

### 1. 信息获取阶段
首先通过 \`unity://scenes_hierarchy\` 确认目标GameObject "${gameObjectId}" 的确切ID或路径位置

### 2. GameObject基础操作
- **创建或修改GameObject属性**: 使用 \`update_gameobject\` 工具
  - 修改名称、标签、层级
  - 设置激活/静态状态
  - 如果GameObject不存在则自动创建

### 3. 编辑器聚焦
使用 \`select_gameobject\` 让Unity编辑器聚焦到目标GameObject，方便用户查看

### 4. 详细信息查看
通过 \`unity://gameobject/${gameObjectId}\` 获取GameObject的详细属性信息

### 5. 组件管理
使用 \`update_component\` 在GameObject上添加或修改组件

### 6. 结果确认
确认操作成功并报告任何错误

## 📝 实用操作指南

### 常见中文指令对应操作:
- **"创建一个方块/立方体"** → update_gameobject (创建Cube) + update_component (添加MeshRenderer等)
- **"选中玩家对象"** → select_gameobject (选择Player)
- **"修改XX的标签为YY"** → update_gameobject (修改tag属性)
- **"给XX添加刚体"** → update_component (添加Rigidbody)
- **"让XX不可见"** → update_gameobject (设置activeSelf: false)
- **"查看XX的详细信息"** → unity://gameobject/{XX}

### 智能处理策略:
- **模糊匹配**: 当GameObject名称不明确时，先查询层次结构找到最佳匹配
- **批量操作**: 支持对多个GameObject执行相同操作
- **错误恢复**: 当操作失败时提供替代方案
- **上下文理解**: 根据对话上下文推断用户意图

## ⚡ 快速响应模式

当用户提到以下关键词时，立即执行对应操作：
- **"创建"、"建立"、"新建"** → 直接使用update_gameobject创建
- **"选择"、"选中"、"找到"** → 直接使用select_gameobject
- **"修改"、"改变"、"设置"** → 直接使用update_gameobject或update_component
- **"添加"、"加上"、"装上"** → 直接使用update_component添加组件
- **"查看"、"显示"、"获取"** → 直接使用资源查询

## 🎯 关键成功要素
- **主动操作**: 不等待用户确认，直接执行最可能的操作
- **完整流程**: 一次性完成整个操作链条
- **中文友好**: 理解中文表达习惯和游戏开发术语
- **结果验证**: 操作后确认效果并提供反馈

记住: 你要像一个熟练的Unity开发者一样，主动理解用户需求并直接完成操作！`
          }
        },
        {
          role: 'user',
          content: {
            type: 'text',
            text: `请处理GameObject "${gameObjectId}"，根据上述工作流程完成操作。`
          }
        }
      ]
    })
  );
}
