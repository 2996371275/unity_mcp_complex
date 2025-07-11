import * as z from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

// Constants for the tool
const toolName = "analyze_module";
const toolDescription = "🔬 对Unity性能进行细化模块分析，深入分析内存、渲染、CPU、GPU、网络、代码、物理、音频等各个模块的详细性能数据。当需要深入了解特定模块的性能瓶颈时使用此工具。适用场景：内存泄漏追踪、渲染优化、CPU热点分析、GPU瓶颈识别等专项性能调优。";

// Schema for tool parameters
const paramsSchema = z.object({
  moduleType: z
    .enum(["memory", "rendering", "cpu", "gpu", "network", "code", "physics", "audio"])
    .describe("要分析的模块类型：'memory'-内存深度分析(GC、分配源、纹理内存等), 'rendering'-渲染分析(Draw Calls、批处理、着色器等), 'cpu'-CPU分析(线程、热点、调用栈等), 'gpu'-GPU分析(利用率、着色器复杂度、填充率等), 'network'-网络分析(延迟、带宽等), 'code'-代码分析(脚本性能、组件分析等), 'physics'-物理分析(碰撞、刚体等), 'audio'-音频分析(内存、压缩等)"),
  frameIndex: z
    .number()
    .int()
    .optional()
    .describe("可选：要分析的特定帧号，用于基于特定帧的模块分析"),
  analysisDepth: z
    .enum(["basic", "detailed", "comprehensive"])
    .optional()
    .default("detailed")
    .describe("分析深度：'basic'-基础分析, 'detailed'-详细分析(默认), 'comprehensive'-全面分析(包含所有细节)"),
  includeOptimizations: z
    .boolean()
    .optional()
    .default(true)
    .describe("是否包含针对该模块的优化建议和性能评级")
});

type AnalyzeModuleParams = z.infer<typeof paramsSchema>;

/**
 * Register the analyze module tool with the MCP server
 * @param server MCP server instance
 * @param mcpUnity Unity bridge instance
 * @param logger Logger instance
 */
export function registerAnalyzeModuleTool(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (args): Promise<CallToolResult> => {
      try {
        logger.info(`Starting module analysis`, { moduleType: args.moduleType, frameIndex: args.frameIndex });

        // Validate parameters using Zod schema
        const validatedParams = paramsSchema.parse(args);

        // Send request to Unity
        const response = await mcpUnity.sendRequest({
          method: toolName,
          params: validatedParams
        });

        if (!response.success) {
          throw new McpUnityError(
            ErrorType.TOOL_EXECUTION,
            response.message || `模块分析失败: ${validatedParams.moduleType}`
          );
        }

        logger.info(`Module analysis completed successfully`, { 
          moduleType: validatedParams.moduleType,
          analysisDepth: validatedParams.analysisDepth 
        });

        // Format the response for better AI understanding
        const formattedResult = formatModuleAnalysisResult(response, validatedParams);

        return {
          content: [
            {
              type: "text",
              text: formattedResult
            }
          ]
        };

      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : "未知错误";
        logger.error(`Module analysis failed`, { error: errorMessage, args });

        if (error instanceof McpUnityError) {
          throw error;
        }

        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          `模块分析工具执行失败: ${errorMessage}`
        );
      }
    }
  );
}

/**
 * Format module analysis result for AI consumption
 * @param response Response from Unity
 * @param params Original parameters
 * @returns Formatted analysis result
 */
function formatModuleAnalysisResult(response: any, params: AnalyzeModuleParams): string {
  const analysis = response.analysis || response;
  const moduleType = params.moduleType;
  const moduleTypeNames = {
    memory: "内存",
    rendering: "渲染",
    cpu: "CPU",
    gpu: "GPU", 
    network: "网络",
    code: "代码",
    physics: "物理",
    audio: "音频"
  };

  let result = `# ${moduleTypeNames[moduleType]}模块深度分析报告\n\n`;
  result += `**分析时间**: ${response.timestamp || new Date().toISOString()}\n`;
  result += `**分析深度**: ${params.analysisDepth}\n`;
  
  if (params.frameIndex) {
    result += `**分析帧号**: ${params.frameIndex}\n`;
  }
  
  result += `**模块类型**: ${moduleTypeNames[moduleType]}模块\n\n`;

  // Module-specific formatting
  switch (moduleType) {
    case "memory":
      result += formatMemoryAnalysis(analysis);
      break;
    case "rendering":
      result += formatRenderingAnalysis(analysis);
      break;
    case "cpu":
      result += formatCPUAnalysis(analysis);
      break;
    case "gpu":
      result += formatGPUAnalysis(analysis);
      break;
    case "network":
      result += formatNetworkAnalysis(analysis);
      break;
    case "code":
      result += formatCodeAnalysis(analysis);
      break;
    case "physics":
      result += formatPhysicsAnalysis(analysis);
      break;
    case "audio":
      result += formatAudioAnalysis(analysis);
      break;
  }

  // Add optimization recommendations if available
  if (params.includeOptimizations && analysis.optimizationRecommendations) {
    result += formatOptimizationRecommendations(analysis.optimizationRecommendations, moduleType);
  }

  // Add performance grade if available
  const gradeKey = `${moduleType}Grade`;
  if (analysis[gradeKey]) {
    result += `\n## 🏆 性能评级\n\n`;
    result += `**${moduleTypeNames[moduleType]}模块评级**: ${analysis[gradeKey]}\n`;
    result += getGradeExplanation(analysis[gradeKey]);
  }

  return result;
}

/**
 * Format memory analysis section
 */
function formatMemoryAnalysis(analysis: any): string {
  let result = "## 📊 内存使用详情\n\n";
  
  if (analysis.memoryBreakdown) {
    const breakdown = analysis.memoryBreakdown;
    result += "### 内存分布\n";
    result += `- **系统使用内存**: ${breakdown.systemUsedMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **GC保留内存**: ${breakdown.gcReservedMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **GC使用内存**: ${breakdown.gcUsedMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **纹理内存**: ${breakdown.textureMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **网格内存**: ${breakdown.meshMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **音频内存**: ${breakdown.audioMemory?.toFixed(1) || 'N/A'} MB\n`;
    result += `- **总分配内存**: ${breakdown.totalAllocatedMemory?.toFixed(1) || 'N/A'} MB\n\n`;
  }

  if (analysis.gcAnalysis) {
    const gc = analysis.gcAnalysis;
    result += "### 垃圾回收分析\n";
    result += `- **GC效率**: ${(gc.gcEfficiency * 100)?.toFixed(1) || 'N/A'}%\n`;
    result += `- **GC压力**: ${gc.gcPressure || 'N/A'}\n`;
    if (gc.frameGCAllocation !== undefined) {
      result += `- **当前帧GC分配**: ${gc.frameGCAllocation} bytes\n`;
    }
    result += "\n";
  }

  if (analysis.allocationSources) {
    const sources = analysis.allocationSources;
    result += "### 内存分配源\n";
    if (sources.commonSources && Array.isArray(sources.commonSources)) {
      sources.commonSources.forEach((source: string, index: number) => {
        result += `${index + 1}. ${source}\n`;
      });
    }
    if (sources.note) {
      result += `\n*注意*: ${sources.note}\n`;
    }
    result += "\n";
  }

  return result;
}

/**
 * Format rendering analysis section
 */
function formatRenderingAnalysis(analysis: any): string {
  let result = "## 🎨 渲染性能详情\n\n";
  
  if (analysis.drawCallAnalysis) {
    const drawCalls = analysis.drawCallAnalysis;
    result += "### Draw Call分析\n";
    result += `- **Draw Calls数量**: ${drawCalls.drawCallsCount || 'N/A'}\n`;
    result += `- **批处理数量**: ${drawCalls.batchesCount || 'N/A'}\n`;
    result += `- **SetPass Calls**: ${drawCalls.setPassCallsCount || 'N/A'}\n`;
    result += `- **批处理效率**: ${(drawCalls.batchingEfficiency * 100)?.toFixed(1) || 'N/A'}%\n\n`;
  }

  if (analysis.batchingAnalysis) {
    const batching = analysis.batchingAnalysis;
    result += "### 批处理分析\n";
    result += `- **批处理比率**: ${(batching.batchingRatio * 100)?.toFixed(1) || 'N/A'}%\n`;
    if (batching.staticBatching) result += `- **静态批处理**: ${batching.staticBatching}\n`;
    if (batching.dynamicBatching) result += `- **动态批处理**: ${batching.dynamicBatching}\n`;
    if (batching.gpuInstancing) result += `- **GPU实例化**: ${batching.gpuInstancing}\n`;
    if (batching.srp) result += `- **SRP批处理**: ${batching.srp}\n`;
    result += "\n";
  }

  // Add other rendering subsections as they become available
  addAnalysisSection(result, analysis, "shaderAnalysis", "### 着色器分析");
  addAnalysisSection(result, analysis, "lightingAnalysis", "### 光照分析");
  addAnalysisSection(result, analysis, "shadowAnalysis", "### 阴影分析");
  addAnalysisSection(result, analysis, "cullingAnalysis", "### 剔除分析");
  addAnalysisSection(result, analysis, "overdrawAnalysis", "### 过度绘制分析");

  return result;
}

/**
 * Format CPU analysis section
 */
function formatCPUAnalysis(analysis: any): string {
  let result = "## ⚡ CPU性能详情\n\n";
  
  if (analysis.threadAnalysis) {
    const threads = analysis.threadAnalysis;
    result += "### 线程分析\n";
    if (threads.mainThreadTime !== undefined) {
      result += `- **主线程时间**: ${threads.mainThreadTime?.toFixed(2) || 'N/A'} ms\n`;
    }
    result += "\n";
  }

  addAnalysisSection(result, analysis, "updateMethodAnalysis", "### Update方法分析");
  addAnalysisSection(result, analysis, "scriptPerformance", "### 脚本性能");
  addAnalysisSection(result, analysis, "callStackAnalysis", "### 调用栈分析");
  addAnalysisSection(result, analysis, "hotSpotAnalysis", "### CPU热点分析");
  addAnalysisSection(result, analysis, "concurrencyAnalysis", "### 并发分析");

  return result;
}

/**
 * Format GPU analysis section
 */
function formatGPUAnalysis(analysis: any): string {
  let result = "## 🎮 GPU性能详情\n\n";
  
  if (analysis.gpuUtilization) {
    const gpu = analysis.gpuUtilization;
    result += "### GPU利用率\n";
    if (gpu.gpuTime !== undefined) {
      result += `- **GPU帧时间**: ${gpu.gpuTime?.toFixed(2) || 'N/A'} ms\n`;
    }
    result += "\n";
  }

  addAnalysisSection(result, analysis, "shaderComplexity", "### 着色器复杂度");
  addAnalysisSection(result, analysis, "fillRateAnalysis", "### 填充率分析");
  addAnalysisSection(result, analysis, "vertexProcessing", "### 顶点处理");
  addAnalysisSection(result, analysis, "textureStreamingAnalysis", "### 纹理流送分析");
  addAnalysisSection(result, analysis, "gpuMemoryAnalysis", "### GPU内存分析");

  return result;
}

/**
 * Format network analysis section
 */
function formatNetworkAnalysis(analysis: any): string {
  let result = "## 🌐 网络性能详情\n\n";
  
  addAnalysisSection(result, analysis, "networkCalls", "### 网络调用分析");
  addAnalysisSection(result, analysis, "latencyAnalysis", "### 延迟分析");
  addAnalysisSection(result, analysis, "bandwidthUsage", "### 带宽使用分析");

  if (analysis.note) {
    result += `\n*注意*: ${analysis.note}\n\n`;
  }

  return result;
}

/**
 * Format code analysis section
 */
function formatCodeAnalysis(analysis: any): string {
  let result = "## 💻 代码性能详情\n\n";
  
  addAnalysisSection(result, analysis, "scriptAnalysis", "### 脚本分析");
  addAnalysisSection(result, analysis, "componentAnalysis", "### 组件分析");
  addAnalysisSection(result, analysis, "coroutineAnalysis", "### 协程分析");
  addAnalysisSection(result, analysis, "eventSystemAnalysis", "### 事件系统分析");

  return result;
}

/**
 * Format physics analysis section
 */
function formatPhysicsAnalysis(analysis: any): string {
  let result = "## 🏃 物理性能详情\n\n";
  
  result += "### 物理系统概览\n";
  if (analysis.physicsTime !== undefined) {
    result += `- **物理计算时间**: ${analysis.physicsTime?.toFixed(2) || 'N/A'} ms\n`;
  }
  if (analysis.rigidbodyCount !== undefined) {
    result += `- **活动刚体数量**: ${analysis.rigidbodyCount}\n`;
  }
  result += "\n";

  addAnalysisSection(result, analysis, "colliderAnalysis", "### 碰撞体分析");
  addAnalysisSection(result, analysis, "raycastAnalysis", "### 射线检测分析");
  addAnalysisSection(result, analysis, "jointAnalysis", "### 关节分析");

  return result;
}

/**
 * Format audio analysis section
 */
function formatAudioAnalysis(analysis: any): string {
  let result = "## 🔊 音频性能详情\n\n";
  
  result += "### 音频系统概览\n";
  if (analysis.audioMemory !== undefined) {
    result += `- **音频内存使用**: ${analysis.audioMemory?.toFixed(1) || 'N/A'} MB\n`;
  }
  if (analysis.audioUpdateTime !== undefined) {
    result += `- **音频更新时间**: ${analysis.audioUpdateTime?.toFixed(2) || 'N/A'} ms\n`;
  }
  result += "\n";

  addAnalysisSection(result, analysis, "audioSourcesAnalysis", "### 音源分析");
  addAnalysisSection(result, analysis, "compressionAnalysis", "### 压缩分析");

  return result;
}

/**
 * Helper function to add analysis sections
 */
function addAnalysisSection(result: string, analysis: any, sectionKey: string, title: string): string {
  if (analysis[sectionKey]) {
    result += `${title}\n`;
    const section = analysis[sectionKey];
    
    if (typeof section === 'object') {
      Object.entries(section).forEach(([key, value]) => {
        if (key !== 'note' && value !== undefined) {
          result += `- **${key}**: ${value}\n`;
        }
      });
      if (section.note) {
        result += `\n*注意*: ${section.note}\n`;
      }
    } else {
      result += `${section}\n`;
    }
    result += "\n";
  }
  return result;
}

/**
 * Format optimization recommendations
 */
function formatOptimizationRecommendations(recommendations: any, moduleType: string): string {
  if (!recommendations || !Array.isArray(recommendations) || recommendations.length === 0) {
    return "";
  }

  const moduleTypeNames = {
    memory: "内存",
    rendering: "渲染",
    cpu: "CPU",
    gpu: "GPU",
    network: "网络",
    code: "代码", 
    physics: "物理",
    audio: "音频"
  };

  let result = `\n## 💡 ${moduleTypeNames[moduleType as keyof typeof moduleTypeNames]}优化建议\n\n`;
  
  recommendations.forEach((recommendation: string, index: number) => {
    result += `${index + 1}. ${recommendation}\n`;
  });
  
  result += "\n";
  return result;
}

/**
 * Get grade explanation
 */
function getGradeExplanation(grade: string): string {
  const gradeExplanations = {
    'A': '🟢 优秀 - 性能表现卓越，无需优化',
    'B': '🟡 良好 - 性能表现良好，可考虑微调',
    'C': '🟠 一般 - 存在性能问题，建议优化',
    'D': '🔴 较差 - 性能问题严重，急需优化',
    'F': '❌ 很差 - 性能极差，必须立即优化'
  };
  
  return gradeExplanations[grade as keyof typeof gradeExplanations] || '❓ 未知等级';
} 