# Unity物体分析工具 🔍

## 功能概述

为MCP Unity项目成功添加了强大的**物体分析工具**，能够全面分析Unity场景物体和Project资源，提供结构化的诊断报告，并支持智能扩展建议和子对象自动添加。

## 🎯 核心功能

### 1. **多目标分析支持**
- 🎯 **当前选中物体**: 分析Unity编辑器中当前选中的物体
- 🏗️ **场景物体**: 分析指定场景中的任意物体
- 📦 **Project资源**: 分析Project面板中的资源文件
- 📍 **指定路径**: 分析特定路径的物体或资源

### 2. **全方位分析维度**
- 📋 **基本信息**: 物体名称、类型、激活状态、层级关系
- 🔧 **Transform详情**: 世界/本地坐标、位置、旋转、缩放
- 🧩 **组件分析**: 组件类型、属性、启用状态、配置检查
- 🏗️ **层级结构**: 子物体关系、嵌套深度、结构合理性
- 🔗 **资源依赖**: 材质、贴图、网格、脚本等依赖关系

### 3. **智能诊断系统**
- ⚠️ **问题识别**: 自动发现配置问题、缺失组件、性能隐患
- 💡 **优化建议**: 基于分析结果提供具体改进建议
- 🚀 **性能评估**: 渲染、物理、内存等性能影响分析
- 🛠️ **扩展建议**: 推荐适合添加的组件和子对象

### 4. **子对象智能创建**
- 🎯 **16种对象类型**: 从基础几何体到复杂UI组件
- 🎨 **自动配置**: 智能设置新增对象的基本属性
- 📍 **精确定位**: 支持相对位置和父子关系设置
- 🔧 **组件自动添加**: 根据需求自动挂载相关组件

## 🎮 支持的子对象类型

### 基础几何体
```
立方体 (Cube) - 基础盒子形状
球体 (Sphere) - 完美球形
胶囊体 (Capsule) - 人物碰撞器常用
圆柱体 (Cylinder) - 柱状物体
平面 (Plane) - 大面积地面
四边形 (Quad) - 单面显示板
```

### 功能组件
```
摄像机 (Camera) - 视角控制
灯光 (Light) - 场景照明
粒子系统 (ParticleSystem) - 特效系统
音频源 (AudioSource) - 声音播放
```

### UI组件
```
Canvas - UI画布
Button - 交互按钮
Text - 文本显示
Image - 图像显示
```

### 高级对象
```
空对象 (Empty) - 组织容器
预制体实例 (PrefabInstance) - 复用资源
```

## 🔍 分析模式

### 基础分析模式
```javascript
{
  targetType: "Selected",
  includeChildren: true,
  includeComponentDetails: false,
  maxDepth: 3
}
```
- 快速了解物体基本结构
- 适合初步检查和概览
- 性能开销小，速度快

### 详细分析模式
```javascript
{
  targetType: "Selected", 
  includeChildren: true,
  includeComponentDetails: true,
  maxDepth: 5,
  includeDependencies: true
}
```
- 全面深入的结构分析
- 包含所有组件属性详情
- 完整的依赖关系映射
- 适合问题诊断和优化

### 性能诊断模式
```javascript
{
  targetType: "Selected",
  includeChildren: true,
  includeComponentDetails: true,
  analysisType: "performance"
}
```
- 专注于性能相关问题
- 渲染、物理、内存分析
- 组件启用状态检查
- 优化建议生成

### 扩展规划模式
```javascript
{
  targetType: "Selected",
  addChildOperations: [
    {
      childName: "Audio Source",
      objectType: "AudioSource",
      position: { x: 0, y: 0, z: 0 },
      components: ["AudioSource"]
    }
  ]
}
```
- 分析完成后自动添加子对象
- 支持批量创建多个子对象
- 智能配置新对象属性

## 📝 自然语言支持

### 基础分析指令
```
"分析当前选中的物体"
"检查这个物体的结构"
"查看Player对象的组件"
"诊断UI界面的问题"
```

### 详细分析指令
```
"深度分析这个预制体，包含所有子物体"
"全面检查场景物体，包含依赖关系"
"详细分析组件配置，深度5层"
"完整诊断这个复杂物体结构"
```

### 性能诊断指令
```
"检查这个物体的性能问题"
"分析渲染性能影响"
"诊断物理组件配置"
"评估内存占用情况"
```

### 扩展创建指令
```
"为角色添加音频组件"
"给平台加个碰撞器"
"为UI创建按钮子对象"
"添加粒子特效到武器"
```

### 项目资源分析
```
"分析这个预制体资源"
"检查材质的依赖关系" 
"评估模型的导入设置"
"诊断贴图的压缩配置"
```

## 🛠️ 技术实现

### Unity Editor端核心
```csharp
// 主要分析工具类
public class AnalyzeObjectTool : McpToolBase
{
    // 支持4种分析目标类型
    // 深度组件属性分析
    // 智能问题诊断
    // 自动子对象创建
}
```

### 组件深度分析
```csharp
// 反射获取组件属性
var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

// 智能属性值格式化
Vector3 → "(1.00, 2.00, 3.00)"
Color → "RGBA(1.00, 0.50, 0.25, 1.00)" 
Quaternion → "(0.00, 0.71, 0.00, 0.71)"
```

### 依赖关系分析
```csharp
// 材质依赖检查
var renderers = targetObject.GetComponentsInChildren<Renderer>();
foreach (var material in renderer.sharedMaterials)

// 网格依赖检查  
var meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();
foreach (var mesh in meshFilter.sharedMesh)

// 资源依赖映射
var assetDeps = AssetDatabase.GetDependencies(assetPath);
```

### 智能建议生成
```csharp
// 基于分析结果的智能建议
if (hasRenderer && !hasCollider)
    suggestions.Add("考虑添加碰撞器组件以支持物理交互");

if (hasCollider && !hasRigidbody)  
    suggestions.Add("考虑添加Rigidbody组件以启用物理模拟");

if (mainObject.ChildCount > 10)
    suggestions.Add("子对象较多，考虑优化层级结构");
```

## 📊 分析报告格式

### 结构化报告示例
```markdown
# Unity物体分析报告

## 主对象: Player Character
- **类型**: GameObject
- **层级路径**: Characters/Player Character  
- **激活状态**: 激活
- **标签**: Player
- **层级**: 8 (Player)
- **子物体数量**: 12

### Transform信息
- **世界位置**: (10.50, 0.00, -5.25)
- **本地位置**: (0.00, 0.00, 0.00)
- **旋转**: (0.00, 45.00, 0.00)
- **缩放**: (1.00, 1.00, 1.00)

### 组件列表 (8个)
1. **Transform** (启用)
   - 类型: UnityEngine.Transform
   - 描述: 控制物体的位置、旋转和缩放

2. **CharacterController** (启用)  
   - 类型: UnityEngine.CharacterController
   - 主要属性:
     - center: (0.00, 1.00, 0.00) (Vector3)
     - radius: 0.5 (Single)
     - height: 2 (Single)

3. **Animator** (启用)
   - 类型: UnityEngine.Animator
   - 描述: 动画控制器组件

### 子物体 (12个)
1. **PlayerModel** (GameObject)
   - 路径: Characters/Player Character/PlayerModel
   - 状态: 激活
   - 组件: Transform, SkinnedMeshRenderer, ...

### 依赖关系 (5个)
1. **Material**: Assets/Materials/PlayerMaterial.mat
2. **Mesh**: Assets/Models/PlayerModel.fbx
3. **Texture**: Assets/Textures/PlayerTexture.png

### 建议操作
1. 考虑添加AudioSource组件以支持角色音效
2. 可以添加粒子系统子对象来增强视觉效果
3. 建议为武器添加挂点子对象

### 分析摘要
物体 'Player Character' 分析完成。包含 8 个组件，12 个子对象，5 个依赖资源。
```

## 🎯 使用场景

### 开发调试
```
"这个角色为什么不能移动？" → 分析组件配置发现缺少Rigidbody
"UI按钮点击没反应？" → 分析发现缺少EventSystem
"粒子效果不显示？" → 分析发现材质引用丢失
```

### 性能优化
```
"场景帧率下降原因？" → 分析发现过多激活的Light组件
"内存占用过高？" → 分析发现大量未压缩贴图
"渲染批次过多？" → 分析发现材质分散问题
```

### 项目重构
```
"预制体结构合理吗？" → 分析层级深度和组件配置
"这个模型如何优化？" → 分析网格、材质、贴图依赖
"UI界面如何改进？" → 分析Canvas设置和响应性布局
```

### 团队协作
```
"这个物体有什么功能？" → 生成详细的组件和属性报告
"如何复用这个系统？" → 分析依赖关系和配置需求
"新人如何理解这个结构？" → 提供结构化的分析文档
```

## ⚡ 性能特点

### 高效分析
- **增量分析**: 只分析请求的深度和范围
- **智能缓存**: 避免重复分析相同组件
- **异步处理**: 不阻塞Unity编辑器主线程
- **内存优化**: 及时释放分析过程中的临时对象

### 可配置性
- **深度控制**: 1-10层可调节分析深度
- **范围选择**: 可选择包含/排除子物体
- **详情级别**: 基础信息或完整属性
- **依赖分析**: 可选的资源依赖检查

### 安全性
- **异常处理**: 完善的错误捕获和处理
- **权限检查**: 只分析用户有权限的对象
- **数据隔离**: 避免循环引用和序列化问题
- **状态保护**: 分析过程不修改原始对象

## 🔮 扩展计划

### 高级分析功能
- **性能Profiler集成**: 实时性能数据分析
- **内存分析器**: 详细的内存占用分布
- **依赖图可视化**: 图形化显示依赖关系
- **批量分析**: 同时分析多个物体或整个场景

### 智能优化建议
- **自动化修复**: 一键修复常见问题
- **性能评分**: 给物体配置打分评级
- **最佳实践检查**: 对照Unity最佳实践标准
- **团队规范验证**: 检查是否符合项目规范

### AI增强功能
- **模式识别**: 识别常见的物体配置模式
- **智能重构**: AI驱动的结构优化建议
- **问题预测**: 基于经验预测潜在问题
- **学习适应**: 根据用户习惯优化建议

## 🎉 总结

Unity物体分析工具为MCP Unity项目带来了强大的**物体诊断和优化**能力：

- 🔍 **全面分析**: 从基础信息到深度依赖的完整分析
- 🧠 **智能诊断**: 自动发现问题并提供针对性建议  
- 🛠️ **即时扩展**: 分析完成后可直接添加推荐的子对象
- 📝 **自然交互**: 支持中文自然语言的分析指令
- 🚀 **高性能**: 优化的分析算法，不影响编辑器性能
- 📊 **结构化输出**: 易读的Markdown格式分析报告

无论是调试问题、优化性能还是理解复杂结构，这个工具都能为您提供专业级的分析支持！🔍✨ 