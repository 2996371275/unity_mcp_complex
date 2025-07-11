import * as z from 'zod';
import { McpUnity } from '../unity/mcpUnity.js';
import { Logger } from '../utils/logger.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'analyze_specific_frame';
const toolDescription = '🔍 深度分析Unity Profiler窗口中特定帧的详细性能数据。当用户说"分析第XXX帧"、"查看帧1183的性能"等时立即使用此工具。可以分析CPU耗时、内存分配、渲染统计、调用层次等详细信息。';

// Schema for tool parameters
const paramsSchema = z.object({
  frameIndex: z.number().int().describe('要分析的帧号，例如1183。这个帧号必须在Unity Profiler窗口的可用数据范围内'),
  analysisType: z
    .enum(['full', 'cpu', 'memory', 'gpu', 'rendering', 'hierarchy'])
    .optional()
    .default('full')
    .describe('分析类型: full(完整分析), cpu(CPU性能), memory(内存分析), gpu(GPU性能), rendering(渲染分析), hierarchy(调用层次)'),
  includeHierarchy: z
    .boolean()
    .optional()
    .default(true)
    .describe('是否包含调用层次分析，显示最耗时的方法调用'),
  includeMemoryAllocs: z
    .boolean()
    .optional()
    .default(true)
    .describe('是否包含详细的内存分配分析')
});

/**
 * Creates and registers the Analyze Specific Frame tool with the MCP server
 * This tool analyzes detailed performance data for a specific frame from Unity Profiler
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerAnalyzeSpecificFrameTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
    logger.info(`Registering tool: ${toolName}`);
        
    // Register this tool with the MCP server
    server.tool(
      toolName,
      toolDescription,
      paramsSchema.shape,
      async (params: any) => {
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
 * Handles specific frame analysis requests
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @param logger The logger instance for diagnostic information
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any, logger: Logger): Promise<CallToolResult> {
    const { frameIndex, analysisType, includeHierarchy, includeMemoryAllocs } = params;

    // Validate frame index
    if (frameIndex === undefined || frameIndex === null || frameIndex < 0) {
        throw new McpUnityError(
            ErrorType.VALIDATION,
            "frameIndex参数是必需的，必须是一个有效的帧号（大于等于0）"
        );
    }

    logger.info(`Analyzing frame ${frameIndex} with type: ${analysisType}`);

    const response = await mcpUnity.sendRequest({
        method: 'analyze_specific_frame',
        params: {
            frameIndex: frameIndex,
            analysisType: analysisType,
            includeHierarchy: includeHierarchy,
            includeMemoryAllocs: includeMemoryAllocs
        }
    });

    if (!response.success) {
        throw new McpUnityError(
            ErrorType.TOOL_EXECUTION,
            response.message || `Failed to analyze frame ${frameIndex}`
        );
    }

    // Format the analysis results for better presentation
    const formattedAnalysis = formatFrameAnalysisResults(response, frameIndex, analysisType, logger);

    return {
        content: [{
            type: "text",
            text: formattedAnalysis
        }]
    };
}

/**
 * Format frame analysis results for better presentation
 * @param response Response from Unity containing analysis data
 * @param frameIndex The frame number that was analyzed
 * @param analysisType Type of analysis performed
 * @param logger Logger instance for diagnostic information
 * @returns Formatted analysis text
 */
function formatFrameAnalysisResults(response: any, frameIndex: number, analysisType: string, logger: Logger): string {
    if (!response.analysis) {
        return `❌ 无法获取第${frameIndex}帧的分析数据。请确保Unity Profiler窗口已打开并包含该帧的数据。`;
    }

    let formattedText = "";

    // Add header
    const headers = {
        full: "📊 完整帧性能分析",
        cpu: "🖥️ CPU性能分析", 
        memory: "💾 内存分析",
        gpu: "🎮 GPU性能分析",
        rendering: "🎨 渲染分析",
        hierarchy: "📋 调用层次分析"
    };

    formattedText += `${headers[analysisType as keyof typeof headers] || "📊 帧性能分析"}\n`;
    formattedText += "=" + "=".repeat(50) + "\n\n";
    
    // Frame basic info
    if (response.frameRange) {
        formattedText += `🎯 **分析帧号**: ${frameIndex}\n`;
        formattedText += `📈 **可用数据范围**: 第${response.frameRange.firstFrame} - ${response.frameRange.lastFrame}帧 (共${response.frameRange.totalFrames}帧)\n`;
        formattedText += `⏰ **分析时间**: ${response.timestamp}\n\n`;
    }

    try {
        const analysis = response.analysis;

        switch (analysisType) {
            case 'full':
                formattedText += formatFullFrameAnalysis(analysis, frameIndex);
                break;
            case 'cpu':
                formattedText += formatCPUFrameAnalysis(analysis, frameIndex);
                break;
            case 'memory':
                formattedText += formatMemoryFrameAnalysis(analysis, frameIndex);
                break;
            case 'gpu':
                formattedText += formatGPUFrameAnalysis(analysis, frameIndex);
                break;
            case 'rendering':
                formattedText += formatRenderingFrameAnalysis(analysis, frameIndex);
                break;
            case 'hierarchy':
                formattedText += formatHierarchyFrameAnalysis(analysis, frameIndex);
                break;
            default:
                formattedText += formatGenericFrameAnalysis(analysis, frameIndex);
                break;
        }
    } catch (error) {
        logger.error("Error formatting frame analysis results", error);
        formattedText += "⚠️ 格式化分析结果时出错，但已完成数据收集。\n";
        formattedText += `原始数据: ${JSON.stringify(response.analysis, null, 2)}`;
    }

    return formattedText;
}

/**
 * Format full frame analysis results
 */
function formatFullFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `## 🎯 第${frameIndex}帧 - 完整性能分析\n\n`;

    // Frame basics
    if (analysis.frameBasics) {
        const basics = analysis.frameBasics;
        text += `### 📊 基础信息\n`;
        text += `- **帧时间**: ${formatNumber(basics.frameTime)}ms\n`;
        text += `- **GPU帧时间**: ${formatNumber(basics.frameTimeGPU)}ms\n`;
        text += `- **实际帧率**: ${formatNumber(basics.fps)} FPS\n`;
        if (basics.gameTime !== undefined) {
            text += `- **游戏时间**: ${formatNumber(basics.gameTime)}s\n`;
        }
        text += `\n`;
    }

    // CPU Analysis
    if (analysis.cpuAnalysis) {
        text += formatCPUFrameAnalysis(analysis.cpuAnalysis, frameIndex);
    }

    // Memory Analysis
    if (analysis.memoryAnalysis) {
        text += formatMemoryFrameAnalysis(analysis.memoryAnalysis, frameIndex);
    }

    // GPU Analysis
    if (analysis.gpuAnalysis) {
        text += formatGPUFrameAnalysis(analysis.gpuAnalysis, frameIndex);
    }

    // Rendering Analysis
    if (analysis.renderingAnalysis) {
        text += formatRenderingFrameAnalysis(analysis.renderingAnalysis, frameIndex);
    }

    // Hierarchy Analysis
    if (analysis.hierarchyAnalysis) {
        text += formatHierarchyFrameAnalysis(analysis.hierarchyAnalysis, frameIndex);
    }

    // Performance Grade
    if (analysis.performanceGrade) {
        text += `### 🏆 性能评级\n`;
        text += `**整体评分**: ${analysis.performanceGrade}\n\n`;
    }

    // Recommendations
    if (analysis.recommendations && analysis.recommendations.length > 0) {
        text += `### 💡 优化建议\n`;
        analysis.recommendations.forEach((rec: string, index: number) => {
            text += `${index + 1}. ${rec}\n`;
        });
        text += `\n`;
    }

    return text;
}

/**
 * Format CPU frame analysis results
 */
function formatCPUFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 🖥️ CPU性能分析\n`;
    
    text += `- **主线程时间**: ${formatNumber(analysis.mainThreadTime)}ms`;
    if (analysis.cpuUtilizationPercent !== undefined) {
        text += ` (${formatNumber(analysis.cpuUtilizationPercent)}% 利用率)`;
    }
    text += `\n`;
    
    if (analysis.renderThreadTime !== undefined) {
        text += `- **渲染线程时间**: ${formatNumber(analysis.renderThreadTime)}ms\n`;
    }
    
    text += `- **Update调用**: ${formatNumber(analysis.behaviourUpdate)}ms\n`;
    text += `- **LateUpdate调用**: ${formatNumber(analysis.lateBehaviourUpdate)}ms\n`;
    text += `- **FixedUpdate调用**: ${formatNumber(analysis.fixedBehaviourUpdate)}ms\n`;
    text += `- **物理计算**: ${formatNumber(analysis.physicsUpdate)}ms\n`;
    text += `- **渲染时间**: ${formatNumber(analysis.renderingTime)}ms\n`;
    
    if (analysis.garbageCollection !== undefined && analysis.garbageCollection > 0) {
        text += `- **垃圾回收**: ${formatNumber(analysis.garbageCollection)}ms\n`;
    }

    // Top CPU consumers
    if (analysis.topCPUConsumers && analysis.topCPUConsumers.length > 0) {
        text += `\n**🔝 主要CPU消耗**:\n`;
        analysis.topCPUConsumers.forEach((consumer: any, index: number) => {
            text += `${index + 1}. ${consumer.methodName}: ${formatNumber(consumer.timeMs)}ms (${formatNumber(consumer.percentage)}%)\n`;
        });
    }

    text += `\n`;
    return text;
}

/**
 * Format memory frame analysis results
 */
function formatMemoryFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 💾 内存分析\n`;
    
    text += `- **GC分配**: ${formatBytes(analysis.gcAlloc * 1024)}B\n`; // Convert KB to bytes
    text += `- **系统内存使用**: ${formatBytes(analysis.systemMemoryUsed * 1024)}B\n`;
    
    if (analysis.renderTextureMemory !== undefined) {
        text += `- **渲染纹理内存**: ${formatBytes(analysis.renderTextureMemory * 1024)}B\n`;
    }
    if (analysis.textureMemory !== undefined) {
        text += `- **纹理内存**: ${formatBytes(analysis.textureMemory * 1024)}B\n`;
    }
    if (analysis.meshMemory !== undefined) {
        text += `- **网格内存**: ${formatBytes(analysis.meshMemory * 1024)}B\n`;
    }
    if (analysis.audioMemory !== undefined) {
        text += `- **音频内存**: ${formatBytes(analysis.audioMemory * 1024)}B\n`;
    }

    // Memory allocations details
    if (analysis.memoryAllocations) {
        const allocations = analysis.memoryAllocations;
        if (allocations.mainAllocators && allocations.mainAllocators.length > 0) {
            text += `\n**📈 主要内存分配源**:\n`;
            allocations.mainAllocators.forEach((alloc: any, index: number) => {
                text += `${index + 1}. ${alloc.type}: ${formatBytes(alloc.allocationKB * 1024)}B\n`;
            });
        }
    }

    text += `\n`;
    return text;
}

/**
 * Format GPU frame analysis results
 */
function formatGPUFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 🎮 GPU性能分析\n`;
    
    text += `- **GPU帧时间**: ${formatNumber(analysis.gpuFrameTime)}ms`;
    if (analysis.gpuUtilizationPercent !== undefined) {
        text += ` (${formatNumber(analysis.gpuUtilizationPercent)}% 利用率)`;
    }
    text += `\n`;
    
    if (analysis.gpuIdleTime !== undefined) {
        text += `- **GPU空闲时间**: ${formatNumber(analysis.gpuIdleTime)}ms\n`;
    }
    if (analysis.gpuPresentWait !== undefined) {
        text += `- **GPU Present等待**: ${formatNumber(analysis.gpuPresentWait)}ms\n`;
    }

    text += `\n`;
    return text;
}

/**
 * Format rendering frame analysis results
 */
function formatRenderingFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 🎨 渲染分析\n`;
    
    text += `- **Draw Calls**: ${formatNumber(analysis.drawCalls)}\n`;
    text += `- **三角面**: ${formatNumber(analysis.triangles)}\n`;
    text += `- **顶点数**: ${formatNumber(analysis.vertices)}\n`;
    text += `- **SetPass Calls**: ${formatNumber(analysis.setPassCalls)}\n`;
    
    if (analysis.batchingEfficiency !== undefined) {
        text += `- **批处理效率**: ${formatNumber(analysis.batchingEfficiency)}%\n`;
    }
    
    if (analysis.shadowCasters !== undefined) {
        text += `- **阴影投射者**: ${formatNumber(analysis.shadowCasters)}\n`;
    }
    if (analysis.visibleSkinnedMeshes !== undefined) {
        text += `- **可见蒙皮网格**: ${formatNumber(analysis.visibleSkinnedMeshes)}\n`;
    }

    text += `\n`;
    return text;
}

/**
 * Format hierarchy frame analysis results
 */
function formatHierarchyFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 📋 调用层次分析\n`;

    if (analysis.topMethods && analysis.topMethods.length > 0) {
        text += `**🔝 最耗时方法**:\n`;
        analysis.topMethods.forEach((method: any, index: number) => {
            text += `${index + 1}. ${method.methodName}: ${formatNumber(method.timeMs)}ms (${formatNumber(method.percentage)}%)\n`;
        });
        text += `\n`;
    }

    if (analysis.renderingHierarchy) {
        text += `**🎨 渲染层次**:\n`;
        const rendering = analysis.renderingHierarchy;
        if (rendering.cameraRender !== undefined) text += `- Camera.Render: ${formatNumber(rendering.cameraRender)}ms\n`;
        if (rendering.shadowMapRender !== undefined) text += `- 阴影渲染: ${formatNumber(rendering.shadowMapRender)}ms\n`;
        if (rendering.opaqueGeometry !== undefined) text += `- 不透明几何体: ${formatNumber(rendering.opaqueGeometry)}ms\n`;
        if (rendering.transparentGeometry !== undefined) text += `- 透明几何体: ${formatNumber(rendering.transparentGeometry)}ms\n`;
        text += `\n`;
    }

    if (analysis.updateHierarchy) {
        text += `**🔄 Update层次**:\n`;
        const update = analysis.updateHierarchy;
        if (update.behaviourUpdate !== undefined) text += `- BehaviourUpdate: ${formatNumber(update.behaviourUpdate)}ms\n`;
        if (update.lateBehaviourUpdate !== undefined) text += `- LateBehaviourUpdate: ${formatNumber(update.lateBehaviourUpdate)}ms\n`;
        if (update.fixedBehaviourUpdate !== undefined) text += `- FixedBehaviourUpdate: ${formatNumber(update.fixedBehaviourUpdate)}ms\n`;
        if (update.physicsUpdate !== undefined) text += `- 物理更新: ${formatNumber(update.physicsUpdate)}ms\n`;
        text += `\n`;
    }

    return text;
}

/**
 * Format generic frame analysis results
 */
function formatGenericFrameAnalysis(analysis: any, frameIndex: number): string {
    let text = `### 📊 第${frameIndex}帧分析结果\n\n`;
    text += `分析数据:\n${JSON.stringify(analysis, null, 2)}\n\n`;
    return text;
}

/**
 * Format a number for display
 */
function formatNumber(num: any): string {
    if (num === undefined || num === null) return "0";
    const value = parseFloat(num);
    if (isNaN(value)) return "0";
    return value.toFixed(2);
}

/**
 * Format bytes for display
 */
function formatBytes(bytes: any): string {
    if (bytes === undefined || bytes === null) return "0";
    const value = parseFloat(bytes);
    if (isNaN(value)) return "0";
    
    if (value < 1024) return `${value.toFixed(0)}`;
    if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)}K`;
    if (value < 1024 * 1024 * 1024) return `${(value / (1024 * 1024)).toFixed(1)}M`;
    return `${(value / (1024 * 1024 * 1024)).toFixed(1)}G`;
} 