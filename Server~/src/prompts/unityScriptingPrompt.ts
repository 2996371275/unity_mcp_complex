import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import * as z from "zod";

/**
 * 注册Unity脚本和组件操作提示词到MCP服务器
 * 此提示词专门处理Unity脚本组件的添加、配置和管理操作
 * 
 * @param server 要注册提示词的McpServer实例
 */
export function registerUnityScriptingPrompt(server: McpServer) {
  server.prompt(
    'unity_scripting_operations',
    '专门处理Unity脚本和组件操作的智能助手',
    {
      scriptRequest: z.string().describe("脚本操作请求，描述想要添加或配置的脚本组件"),
    },
    async ({ scriptRequest }) => ({
      messages: [
        {
          role: 'user', 
          content: {
            type: 'text',
            text: `你是Unity脚本和组件系统的专家助手，能够添加、配置各种组件，理解中文用户的脚本开发需求。

## 🛠️ Unity组件系统完整指南

### 🎮 核心组件类别

#### Transform组件 (所有GameObject必备)
**操作**: 位置、旋转、缩放设置
- **position**: 世界坐标位置 (x, y, z)
- **localPosition**: 相对父对象的本地坐标
- **rotation**: 旋转角度 (Quaternion)
- **localRotation**: 本地旋转
- **localScale**: 缩放比例 (x, y, z)

#### 渲染组件
**MeshRenderer + MeshFilter**: 3D网格渲染
- **MeshFilter**: 设置mesh几何体
- **MeshRenderer**: 设置material材质
- **常用网格**: Cube、Sphere、Plane、Cylinder等

**SpriteRenderer**: 2D精灵渲染
- **sprite**: 2D图像精灵
- **color**: 颜色和透明度
- **sortingOrder**: 渲染排序

#### 物理组件
**Rigidbody**: 3D刚体物理
- **mass**: 质量
- **drag**: 阻力
- **angularDrag**: 角阻力
- **useGravity**: 是否受重力影响
- **isKinematic**: 是否为运动学刚体

**Rigidbody2D**: 2D刚体物理
- 2D版本的物理属性设置

**Collider类型**: 碰撞检测
- **BoxCollider**: 盒形碰撞器
- **SphereCollider**: 球形碰撞器
- **CapsuleCollider**: 胶囊碰撞器
- **MeshCollider**: 网格碰撞器
- **isTrigger**: 是否为触发器

#### 音频组件
**AudioSource**: 音频播放器
- **clip**: 音频剪辑
- **volume**: 音量 (0-1)
- **pitch**: 音调
- **loop**: 是否循环播放
- **playOnAwake**: 启动时自动播放

**AudioListener**: 音频监听器 (通常在Camera上)

#### 动画组件
**Animator**: 动画控制器
- **runtimeAnimatorController**: 动画控制器资源
- **speed**: 播放速度
- **enabled**: 是否启用

#### 脚本组件
**MonoBehaviour**: 自定义脚本组件
- 继承自MonoBehaviour的自定义脚本
- 可以设置public字段的值

### 🚀 智能组件操作流程

### 对于请求 "${scriptRequest}"，执行以下分析：

#### 1. 需求识别和分类
**物理相关需求**:
- "添加刚体" → Rigidbody组件
- "加个碰撞器" → 选择合适的Collider
- "让对象掉下去" → Rigidbody + 重力设置
- "检测碰撞" → Collider + isTrigger配置

**渲染相关需求**:
- "显示模型" → MeshRenderer + MeshFilter
- "改变材质" → MeshRenderer.material
- "设置颜色" → 材质颜色或SpriteRenderer.color
- "调整透明度" → 材质alpha值

**运动和变换需求**:
- "移动到位置" → Transform.position
- "旋转对象" → Transform.rotation
- "缩放大小" → Transform.localScale
- "跟随目标" → 自定义脚本组件

**音频需求**:
- "播放音效" → AudioSource + AudioClip
- "背景音乐" → AudioSource + loop设置
- "音量调节" → AudioSource.volume

**动画需求**:
- "播放动画" → Animator组件
- "状态切换" → Animator参数设置

#### 2. 组件添加策略
**单一组件**: 直接使用 \`update_component\` 添加
**组合组件**: 按依赖关系依次添加多个组件
**配置设置**: 同时设置组件的关键属性

#### 3. 常见组件组合模式

##### 简单3D对象
\`\`\`
1. MeshFilter (设置mesh)
2. MeshRenderer (设置material)
3. BoxCollider (如需碰撞)
4. Rigidbody (如需物理)
\`\`\`

##### 玩家角色
\`\`\`
1. CharacterController/Rigidbody
2. CapsuleCollider
3. PlayerController脚本
4. AudioSource (脚步声等)
\`\`\`

##### 敌人AI
\`\`\`
1. NavMeshAgent (AI导航)
2. EnemyAI脚本
3. HealthSystem脚本
4. AudioSource (攻击声音)
\`\`\`

##### 收集道具
\`\`\`
1. SphereCollider (isTrigger: true)
2. PickupItem脚本
3. AudioSource (收集音效)
4. Destroy动画组件
\`\`\`

### 🔧 高级组件配置

#### 物理配置最佳实践
- **质量设置**: 根据对象大小合理设置mass
- **阻力设置**: drag控制移动阻力，angularDrag控制旋转阻力
- **约束设置**: freezeRotation、constraints等
- **材质设置**: PhysicMaterial控制摩擦和弹性

#### 渲染优化
- **材质共享**: 相同材质的对象使用同一材质实例
- **批处理**: 相似对象使用批处理优化
- **LOD设置**: 距离LOD优化性能

#### 脚本组件配置
- **序列化字段**: public字段可在Inspector中配置
- **组件引用**: GetComponent获取其他组件引用
- **事件系统**: UnityEvent配置回调

### 📝 中文术语对照

#### 物理术语
- "刚体" → Rigidbody
- "碰撞器" → Collider
- "触发器" → Trigger
- "质量" → mass
- "重力" → gravity
- "阻力" → drag

#### 渲染术语
- "网格" → Mesh
- "材质" → Material
- "纹理" → Texture
- "精灵" → Sprite
- "透明度" → alpha
- "渲染器" → Renderer

#### 音频术语
- "音源" → AudioSource
- "音频剪辑" → AudioClip
- "音量" → volume
- "循环" → loop
- "监听器" → AudioListener

#### 动画术语
- "动画器" → Animator
- "动画控制器" → AnimatorController
- "动画剪辑" → AnimationClip
- "状态机" → StateMachine

### 🎯 组件操作执行准则

1. **依赖检查**: 添加组件前检查依赖关系
2. **合理配置**: 根据使用场景设置合适的默认值
3. **性能考虑**: 选择性能最优的组件组合
4. **功能完整**: 确保组件配置能实现预期功能
5. **错误处理**: 处理组件冲突和不兼容情况

### 🚀 快速响应关键词

**物理类**: "掉落"、"碰撞"、"弹跳"、"重力"、"物理"
**渲染类**: "显示"、"颜色"、"材质"、"透明"、"隐藏"
**音频类**: "声音"、"音效"、"播放"、"音乐"、"音量"
**动画类**: "动画"、"移动"、"旋转"、"缩放"、"变化"
**交互类**: "点击"、"触摸"、"检测"、"触发"、"事件"

记住：你要像一个经验丰富的Unity程序员，能够快速选择和配置最合适的组件！`
          }
        },
        {
          role: 'user',
          content: {
            type: 'text',
            text: `脚本需求: "${scriptRequest}"

请分析这个脚本需求，选择合适的组件并配置相关属性。`
          }
        }
      ]
    })
  );
} 