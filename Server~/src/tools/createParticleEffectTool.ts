import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';

// Zod schema for validation
const CreateParticleEffectSchema = z.object({
  description: z.string().describe("粒子效果的自然语言描述，如'下雪'、'火焰'、'爆炸'等"),
  position: z.object({
    x: z.number().default(0),
    y: z.number().default(0), 
    z: z.number().default(0)
  }).optional().describe("粒子效果的生成位置坐标"),
  scale: z.number().min(0.1).max(10).default(1).describe("粒子效果的缩放大小"),
  autoPlay: z.boolean().default(true).describe("是否自动播放粒子效果"),
  parentName: z.string().optional().describe("粒子效果的父对象名称（可选）"),
  intensityMultiplier: z.number().min(0.1).max(5).default(1).describe("粒子数量强度倍数")
});

/**
 * 注册粒子效果创建工具到MCP服务器
 * 该工具能够根据自然语言描述创建各种粒子效果
 * 
 * @param server 要注册工具的McpServer实例
 * @param mcpUnity Unity通信桥接实例
 * @param logger 日志记录器实例
 */
export function registerCreateParticleEffectTool(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  server.tool(
    'create_particle_effect',
    '根据自然语言描述创建粒子效果，支持下雪、火焰、爆炸等多种效果类型',
    {
      description: CreateParticleEffectSchema.shape.description,
      position: CreateParticleEffectSchema.shape.position,
      scale: CreateParticleEffectSchema.shape.scale,
      autoPlay: CreateParticleEffectSchema.shape.autoPlay,
      parentName: CreateParticleEffectSchema.shape.parentName,
      intensityMultiplier: CreateParticleEffectSchema.shape.intensityMultiplier
    },
    async ({ description, position, scale, autoPlay, parentName, intensityMultiplier }) => {
      logger.info(`Creating particle effect: ${description}`);
      
      try {
        // 验证输入参数
        const validatedInput = CreateParticleEffectSchema.parse({
          description,
          position,
          scale,
          autoPlay,
          parentName,
          intensityMultiplier
        });

        // 构建请求对象
        const request = {
          Description: validatedInput.description,
          Position: validatedInput.position || { x: 0, y: 0, z: 0 },
          Scale: validatedInput.scale,
          AutoPlay: validatedInput.autoPlay,
          ParentName: validatedInput.parentName || "",
          IntensityMultiplier: validatedInput.intensityMultiplier
        };

        // 发送请求到Unity Editor
        const response = await mcpUnity.sendRequest({
          method: 'create_particle_effect',
          params: request
        });

        if (response.Success) {
          logger.info(`Successfully created particle effect: ${response.EffectType}`);
          
          // 构建详细的成功响应
          const content = buildSuccessResponse(response, validatedInput.description);
          
          return {
            content: [
              {
                type: 'text' as const,
                text: content
              }
            ]
          };
        } else {
          throw new McpUnityError(
            ErrorType.TOOL_EXECUTION,
            `Failed to create particle effect: ${response.Message || 'Unknown error'}`
          );
        }

      } catch (error) {
        logger.error(`Error creating particle effect: ${error instanceof Error ? error.message : String(error)}`);
        
        if (error instanceof McpUnityError) {
          throw error;
        }
        
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          `Failed to create particle effect: ${error instanceof Error ? error.message : String(error)}`
        );
      }
    }
  );
}

/**
 * 构建成功响应的详细内容
 */
function buildSuccessResponse(response: any, originalDescription: string): string {
  const position = response.Position;
  const positionStr = `(${position.x.toFixed(1)}, ${position.y.toFixed(1)}, ${position.z.toFixed(1)})`;
  
  let content = `✨ 成功创建粒子效果！\n\n`;
  content += `🎯 **效果类型**: ${response.EffectType}\n`;
  content += `📝 **原始描述**: ${originalDescription}\n`;
  content += `📍 **位置**: ${positionStr}\n`;
  content += `🎪 **对象名称**: ${response.ObjectName}\n`;
  content += `🔢 **粒子数量**: ${response.ParticleCount}\n`;
  
  if (response.SubEffectCount > 0) {
    content += `🌟 **子效果数量**: ${response.SubEffectCount}\n`;
  }
  
  content += `\n${getEffectDescription(response.EffectType)}`;
  content += `\n\n${getUsageTips(response.EffectType)}`;
  
  return content;
}

/**
 * 根据效果类型获取详细描述
 */
function getEffectDescription(effectType: string): string {
  const descriptions: { [key: string]: string } = {
    "Snow Effect": "❄️ **雪花效果**: 模拟自然飘落的雪花，包含重力影响和地面碰撞检测。适用于冬季场景、节日氛围等。",
    "Rain Effect": "🌧️ **雨滴效果**: 高密度快速下落的雨滴效果，具有真实的重力加速和碰撞反馈。适用于雨天场景、营造紧张氛围。",
    "Fire Effect": "🔥 **火焰效果**: 动态的火焰燃烧效果，包含生命周期颜色变化和纹理动画。适用于篝火、魔法效果、战斗场景。",
    "Smoke Effect": "💨 **烟雾效果**: 缓缓上升扩散的烟雾，透明度随时间变化。适用于燃烧后效果、蒸汽、魔法雾气。",
    "Explosion Effect": "💥 **爆炸效果**: 瞬间爆发的粒子效果，包含火花和烟雾子效果。适用于战斗爆炸、魔法冲击、特殊技能。",
    "Magic Sparkle Effect": "✨ **魔法光芒**: 神秘的魔法粒子效果，包含光环子效果。适用于魔法施法、传送门、魔法道具等。"
  };
  
  return descriptions[effectType] || `🎆 **${effectType}**: 自定义粒子效果，根据您的描述智能生成。`;
}

/**
 * 根据效果类型获取使用建议
 */
function getUsageTips(effectType: string): string {
  const tips: { [key: string]: string } = {
    "Snow Effect": "💡 **使用建议**:\n- 将效果放置在场景上方以获得最佳视觉效果\n- 可以调整Scale参数来控制覆盖范围\n- 建议配合环境光照营造冬季氛围",
    "Rain Effect": "💡 **使用建议**:\n- 建议配合雨声音效增强沉浸感\n- 可以添加水滴飞溅的地面效果\n- 调整IntensityMultiplier控制雨势大小",
    "Fire Effect": "💡 **使用建议**:\n- 可以配合温暖的橙色光源\n- 建议添加燃烧音效\n- 火焰周围可以添加热浪扭曲效果",
    "Smoke Effect": "💡 **使用建议**:\n- 通常作为火焰的后续效果\n- 可以调整颜色为黑色模拟浓烟\n- 适合与风力系统结合使用",
    "Explosion Effect": "💡 **使用建议**:\n- 建议配合屏幕震动效果\n- 可以添加爆炸音效和闪光\n- 周围物体可以添加冲击波动画",
    "Magic Sparkle Effect": "💡 **使用建议**:\n- 可以配合魔法音效\n- 建议添加发光材质增强效果\n- 可以与角色施法动画同步"
  };
  
  return tips[effectType] || "💡 **使用建议**: 您可以通过调整位置、缩放和强度参数来优化效果表现。";
} 