import * as z from "zod";
import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { CallToolResult } from "@modelcontextprotocol/sdk/types.js";

// Constants for the tool
const toolName = "analyze_profiler";
const toolDescription = "🔍 深度分析Unity性能数据，识别性能瓶颈并提供优化建议。适用场景：游戏卡顿、帧率低、内存占用高、渲染性能差等问题。当用户提到'卡顿'、'掉帧'、'性能差'、'内存泄漏'等关键词时应立即使用此工具。";

// Schema for tool parameters
const paramsSchema = z.object({
  analysisType: z
    .enum(["full", "performance", "memory", "cpu", "gpu", "rendering", "bottlenecks", "hierarchy"])
    .optional()
    .default("full")
    .describe("Type of analysis to perform: 'full' for comprehensive analysis, 'performance' for overall performance, 'memory' for memory usage, 'cpu' for CPU performance, 'gpu' for GPU metrics, 'rendering' for draw calls and batching, 'bottlenecks' for identifying specific issues, 'hierarchy' for call stack analysis"),
  frameCount: z
    .number()
    .int()
    .min(1)
    .max(30)
    .optional()
    .default(5)
    .describe("Number of frames to analyze for averaging performance metrics (1-30)"),
  targetFPS: z
    .string()
    .optional()
    .default("60")
    .describe("Target FPS for performance evaluation (30, 60, 120)"),
  includeOptimizationTips: z
    .boolean()
    .optional()
    .default(true)
    .describe("Whether to include detailed optimization recommendations based on the analysis")
});

/**
 * Creates and registers the Analyze Profiler tool with the MCP server
 * This tool provides deep analysis of Unity performance data
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerAnalyzeProfilerTool(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  logger.info(`Registering tool: ${toolName}`);

  // Register this tool with the MCP server
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: z.infer<typeof paramsSchema>) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params, logger);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

/**
 * Handles profiler analysis requests
 *
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @param logger The logger instance for diagnostic information
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(
  mcpUnity: McpUnity,
  params: z.infer<typeof paramsSchema>,
  logger: Logger
): Promise<CallToolResult> {
  const { analysisType, frameCount, targetFPS, includeOptimizationTips } = params;

  // Send request to Unity using the tool method name
  const response = await mcpUnity.sendRequest({
    method: "analyze_profiler",
    params: {
      analysisType: analysisType,
      frameCount: frameCount,
      targetFPS: targetFPS,
      includeOptimizationTips: includeOptimizationTips
    }
  });

  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || "Failed to analyze profiler data in Unity"
    );
  }

  // Format the analysis results for better presentation
  const formattedAnalysis = formatAnalysisResults(response.analysis, analysisType, logger);

  return {
    content: [
      {
        type: "text",
        text: formattedAnalysis
      }
    ]
  };
}

/**
 * Format analysis results for better presentation to AI assistants
 * @param analysis Raw analysis data from Unity
 * @param analysisType Type of analysis performed
 * @param logger Logger instance for diagnostic information
 * @returns Formatted analysis text
 */
function formatAnalysisResults(analysis: any, analysisType: string, logger: Logger): string {
  if (!analysis) {
    return "❌ No analysis data received from Unity. Ensure the profiler is running and Unity is connected.";
  }

  let formattedText = "";

  // Add header based on analysis type
  const headers = {
    full: "📊 Complete Unity Performance Analysis",
    performance: "⚡ Performance Analysis",
    memory: "💾 Memory Analysis",
    cpu: "🖥️ CPU Performance Analysis",
    gpu: "🎮 GPU Performance Analysis",
    rendering: "🎨 Rendering Analysis",
    bottlenecks: "🔍 Performance Bottleneck Analysis",
    hierarchy: "📋 Call Hierarchy Analysis"
  };

  formattedText += `${headers[analysisType as keyof typeof headers] || "📊 Unity Performance Analysis"}\n`;
  formattedText += "=" + "=".repeat(50) + "\n\n";

  try {
    switch (analysisType) {
      case "full":
        formattedText += formatFullAnalysis(analysis);
        break;
      case "performance":
        formattedText += formatPerformanceAnalysis(analysis);
        break;
      case "memory":
        formattedText += formatMemoryAnalysis(analysis);
        break;
      case "cpu":
        formattedText += formatCPUAnalysis(analysis);
        break;
      case "gpu":
        formattedText += formatGPUAnalysis(analysis);
        break;
      case "rendering":
        formattedText += formatRenderingAnalysis(analysis);
        break;
      case "bottlenecks":
        formattedText += formatBottleneckAnalysis(analysis);
        break;
      case "hierarchy":
        formattedText += formatHierarchyAnalysis(analysis);
        break;
      default:
        formattedText += formatGenericAnalysis(analysis);
        break;
    }
  } catch (error) {
    logger.error("Error formatting analysis results", error);
    formattedText += "⚠️ Error formatting analysis results. Raw data:\n";
    formattedText += JSON.stringify(analysis, null, 2);
  }

  return formattedText;
}

/**
 * Format full comprehensive analysis
 */
function formatFullAnalysis(analysis: any): string {
  let text = "";
  
  // Overall Score
  if (analysis.overallScore !== undefined) {
    const scoreEmoji = getScoreEmoji(analysis.overallScore);
    text += `🎯 **Overall Performance Score: ${analysis.overallScore}/100** ${scoreEmoji}\n\n`;
  }

  // Performance Summary
  if (analysis.performance) {
    text += "## 📈 Performance Overview\n";
    const perf = analysis.performance;
    text += `- Current FPS: ${perf.currentFPS?.toFixed(1) || 'N/A'}\n`;
    text += `- Target FPS: ${perf.targetFPS || 'N/A'}\n`;
    text += `- Target Met: ${perf.isTargetMet ? '✅ Yes' : '❌ No'}\n`;
    text += `- Main Thread Time: ${perf.averageMainThreadTime?.toFixed(2) || 'N/A'}ms\n`;
    text += `- GPU Time: ${perf.averageGPUTime?.toFixed(2) || 'N/A'}ms\n`;
    text += `- Bottleneck: ${perf.performanceBottleneck || 'Unknown'}\n\n`;
  }

  // Bottlenecks
  if (analysis.bottlenecks) {
    text += formatBottleneckAnalysis(analysis.bottlenecks);
  }

  // Memory
  if (analysis.memory) {
    text += "## 💾 Memory Status\n";
    const mem = analysis.memory;
    text += `- GC Memory: ${mem.gcReservedMemoryMB?.toFixed(1) || 'N/A'}MB (Grade: ${mem.memoryGrade || 'N/A'})\n`;
    text += `- System Memory: ${mem.systemUsedMemoryMB?.toFixed(1) || 'N/A'}MB\n`;
    text += `- Memory Efficiency: ${((mem.gcEfficiency || 0) * 100).toFixed(1)}%\n`;
    if (mem.memoryIssues && mem.memoryIssues.length > 0) {
      text += "- Issues: " + mem.memoryIssues.join(", ") + "\n";
    }
    text += "\n";
  }

  // Rendering
  if (analysis.rendering) {
    text += "## 🎨 Rendering Statistics\n";
    const render = analysis.rendering;
    text += `- Draw Calls: ${render.drawCalls || 'N/A'} (Grade: ${render.renderingGrade || 'N/A'})\n`;
    text += `- Batches: ${render.batches || 'N/A'}\n`;
    text += `- SetPass Calls: ${render.setPassCalls || 'N/A'}\n`;
    text += `- Triangles: ${formatNumber(render.triangles)}\n`;
    text += `- Batching Efficiency: ${((render.batchingEfficiency || 0) * 100).toFixed(1)}%\n`;
    if (render.renderingIssues && render.renderingIssues.length > 0) {
      text += "- Issues: " + render.renderingIssues.join(", ") + "\n";
    }
    text += "\n";
  }

  // Recommendations
  if (analysis.recommendations) {
    text += formatRecommendations(analysis.recommendations);
  }

  return text;
}

/**
 * Format performance analysis
 */
function formatPerformanceAnalysis(analysis: any): string {
  let text = "";
  
  text += `🎯 **Current FPS:** ${analysis.currentFPS?.toFixed(1) || 'N/A'}\n`;
  text += `🎯 **Target FPS:** ${analysis.targetFPS || 'N/A'}\n`;
  text += `✅ **Target Met:** ${analysis.isTargetMet ? 'Yes' : 'No'}\n`;
  text += `⏱️ **Main Thread Time:** ${analysis.averageMainThreadTime?.toFixed(2) || 'N/A'}ms\n`;
  text += `🎮 **GPU Time:** ${analysis.averageGPUTime?.toFixed(2) || 'N/A'}ms\n`;
  text += `📊 **Frame Time Variation:** ${analysis.frameTimeVariation?.toFixed(2) || 'N/A'}ms\n`;
  text += `🔍 **Primary Bottleneck:** ${analysis.performanceBottleneck || 'Unknown'}\n`;
  text += `📈 **FPS Stability:** ${((analysis.fpsStability || 0) * 100).toFixed(1)}%\n\n`;

  return text;
}

/**
 * Format memory analysis
 */
function formatMemoryAnalysis(analysis: any): string {
  let text = "";
  
  text += `💾 **System Memory:** ${analysis.systemUsedMemoryMB?.toFixed(1) || 'N/A'}MB\n`;
  text += `🗑️ **GC Reserved:** ${analysis.gcReservedMemoryMB?.toFixed(1) || 'N/A'}MB\n`;
  text += `📦 **GC Used:** ${analysis.gcUsedMemoryMB?.toFixed(1) || 'N/A'}MB\n`;
  text += `📊 **Total Allocated:** ${analysis.totalAllocatedMemoryMB?.toFixed(1) || 'N/A'}MB\n`;
  text += `📈 **GC Efficiency:** ${((analysis.gcEfficiency || 0) * 100).toFixed(1)}%\n`;
  text += `📋 **Memory Grade:** ${analysis.memoryGrade || 'N/A'}\n\n`;

  if (analysis.memoryIssues && analysis.memoryIssues.length > 0) {
    text += "⚠️ **Memory Issues:**\n";
    analysis.memoryIssues.forEach((issue: string) => {
      text += `- ${issue}\n`;
    });
    text += "\n";
  }

  return text;
}

/**
 * Format CPU analysis
 */
function formatCPUAnalysis(analysis: any): string {
  let text = "";
  
  text += `🖥️ **Main Thread Time:** ${analysis.mainThreadTimeMs?.toFixed(2) || 'N/A'}ms\n`;
  text += `🔄 **Render Thread Time:** ${analysis.renderThreadTimeMs?.toFixed(2) || 'N/A'}ms\n`;
  text += `📊 **Total CPU Time:** ${analysis.totalCPUTimeMs?.toFixed(2) || 'N/A'}ms\n`;
  text += `📈 **CPU Utilization:** ${analysis.cpuUtilization?.toFixed(1) || 'N/A'}%\n`;
  text += `📋 **CPU Grade:** ${analysis.cpuGrade || 'N/A'}\n`;
  text += `🏆 **Heaviest Component:** ${analysis.heaviestComponent || 'Unknown'}\n\n`;

  if (analysis.cpuBreakdown) {
    text += "**CPU Time Breakdown:**\n";
    const breakdown = analysis.cpuBreakdown;
    Object.keys(breakdown).forEach(key => {
      const value = breakdown[key];
      if (typeof value === 'number' && value > 0) {
        text += `- ${key}: ${value.toFixed(2)}ms\n`;
      }
    });
    text += "\n";
  }

  return text;
}

/**
 * Format GPU analysis
 */
function formatGPUAnalysis(analysis: any): string {
  let text = "";
  
  text += `🎮 **GPU Frame Time:** ${analysis.gpuFrameTimeMs?.toFixed(2) || 'N/A'}ms\n`;
  text += `📊 **GPU Variation:** ${analysis.gpuVariation?.toFixed(2) || 'N/A'}ms\n`;
  text += `📈 **GPU Utilization:** ${analysis.gpuUtilization?.toFixed(1) || 'N/A'}%\n`;
  text += `📋 **GPU Grade:** ${analysis.gpuGrade || 'N/A'}\n`;
  text += `🔍 **GPU Bound:** ${analysis.isGPUBound ? 'Yes' : 'No'}\n\n`;

  return text;
}

/**
 * Format rendering analysis
 */
function formatRenderingAnalysis(analysis: any): string {
  let text = "";
  
  text += `🎨 **Draw Calls:** ${analysis.drawCalls || 'N/A'}\n`;
  text += `📦 **Batches:** ${analysis.batches || 'N/A'}\n`;
  text += `🔄 **SetPass Calls:** ${analysis.setPassCalls || 'N/A'}\n`;
  text += `📐 **Triangles:** ${formatNumber(analysis.triangles)}\n`;
  text += `📊 **Vertices:** ${formatNumber(analysis.vertices)}\n`;
  text += `📈 **Batching Efficiency:** ${((analysis.batchingEfficiency || 0) * 100).toFixed(1)}%\n`;
  text += `📋 **Rendering Grade:** ${analysis.renderingGrade || 'N/A'}\n\n`;

  if (analysis.renderingIssues && analysis.renderingIssues.length > 0) {
    text += "⚠️ **Rendering Issues:**\n";
    analysis.renderingIssues.forEach((issue: string) => {
      text += `- ${issue}\n`;
    });
    text += "\n";
  }

  return text;
}

/**
 * Format bottleneck analysis
 */
function formatBottleneckAnalysis(analysis: any): string {
  let text = "## 🔍 Performance Bottlenecks\n";
  
  if (analysis.primaryBottleneck) {
    text += `🎯 **Primary Bottleneck:** ${analysis.primaryBottleneck}\n`;
  }
  
  if (analysis.bottleneckScore !== undefined) {
    const scoreEmoji = getScoreEmoji(analysis.bottleneckScore);
    text += `📊 **Bottleneck Score:** ${analysis.bottleneckScore.toFixed(1)}/100 ${scoreEmoji}\n`;
  }

  if (analysis.bottlenecks && analysis.bottlenecks.length > 0) {
    text += "\n**Identified Bottlenecks:**\n";
    analysis.bottlenecks.forEach((bottleneck: string, index: number) => {
      const severity = analysis.severities && analysis.severities[index] ? 
        analysis.severities[index].toFixed(2) : 'N/A';
      text += `- ${bottleneck} (Severity: ${severity})\n`;
    });
  } else {
    text += "\n✅ No significant bottlenecks detected!\n";
  }
  
  text += "\n";
  return text;
}

/**
 * Format hierarchy analysis
 */
function formatHierarchyAnalysis(analysis: any): string {
  let text = "";
  
  if (analysis.frameIndex !== undefined) {
    text += `📊 **Frame Index:** ${analysis.frameIndex}\n`;
  }
  if (analysis.totalMethodsAnalyzed !== undefined) {
    text += `🔍 **Methods Analyzed:** ${analysis.totalMethodsAnalyzed}\n`;
  }
  text += "\n";

  if (analysis.expensiveMethods && analysis.expensiveMethods.length > 0) {
    text += "**🔥 Most CPU-Intensive Methods:**\n";
    analysis.expensiveMethods.slice(0, 10).forEach((method: any, index: number) => {
      text += `${index + 1}. ${method.name}\n`;
      text += `   - Total Time: ${method.totalTimeMs?.toFixed(2) || 'N/A'}ms\n`;
      text += `   - Self Time: ${method.selfTimeMs?.toFixed(2) || 'N/A'}ms\n`;
      text += `   - Calls: ${method.calls || 'N/A'}\n`;
      if (method.gcMemoryBytes && method.gcMemoryBytes > 0) {
        text += `   - GC Memory: ${formatBytes(method.gcMemoryBytes)}\n`;
      }
      text += "\n";
    });
  }

  if (analysis.memoryHogMethods && analysis.memoryHogMethods.length > 0) {
    text += "**💾 Most Memory-Intensive Methods:**\n";
    analysis.memoryHogMethods.slice(0, 5).forEach((method: any, index: number) => {
      if (method.gcMemoryBytes && method.gcMemoryBytes > 1024) { // Only show methods with >1KB
        text += `${index + 1}. ${method.name}\n`;
        text += `   - GC Memory: ${formatBytes(method.gcMemoryBytes)}\n`;
        text += `   - Total Time: ${method.totalTimeMs?.toFixed(2) || 'N/A'}ms\n`;
        text += "\n";
      }
    });
  }

  return text;
}

/**
 * Format generic analysis fallback
 */
function formatGenericAnalysis(analysis: any): string {
  return JSON.stringify(analysis, null, 2);
}

/**
 * Format recommendations section
 */
function formatRecommendations(recommendations: any): string {
  let text = "## 💡 Optimization Recommendations\n";
  
  if (recommendations.recommendations && recommendations.recommendations.length > 0) {
    recommendations.recommendations.forEach((rec: string, index: number) => {
      const priority = recommendations.priorities && recommendations.priorities[index] ? 
        recommendations.priorities[index] : 'Medium';
      const priorityEmoji = ({
        'High': '🔴',
        'Medium': '🟡',
        'Low': '🟢'
      } as Record<string, string>)[priority] || '⚪';
      
      text += `${priorityEmoji} **${priority} Priority:** ${rec}\n\n`;
    });
  } else {
    text += "✅ No specific recommendations at this time. Performance appears to be within acceptable ranges.\n\n";
  }
  
  return text;
}

/**
 * Helper functions
 */
function getScoreEmoji(score: number): string {
  if (score >= 90) return "🟢";
  if (score >= 70) return "🟡";
  if (score >= 50) return "🟠";
  return "🔴";
}

function formatNumber(num: any): string {
  if (typeof num !== 'number') return 'N/A';
  if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
  if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
  return num.toString();
}

function formatBytes(bytes: any): string {
  if (typeof bytes !== 'number') return 'N/A';
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)}MB`;
  if (bytes >= 1024) return `${(bytes / 1024).toFixed(1)}KB`;
  return `${bytes}B`;
} 