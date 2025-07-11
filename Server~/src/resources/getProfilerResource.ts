import { Logger } from "../utils/logger.js";
import { McpUnity } from "../unity/mcpUnity.js";
import { ResourceTemplate, McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";
import { ReadResourceResult } from '@modelcontextprotocol/sdk/types.js';
import { Variables } from '@modelcontextprotocol/sdk/shared/uriTemplate.js';

// Constants for the resource
const resourceName = "get_profiler_data";
const resourceUriTemplate = "unity://profiler/{dataType}";
const resourceMimeType = "application/json";

/**
 * Registers the Unity Profiler data resource with the MCP server
 * This resource provides access to Unity's performance profiling data
 *
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerGetProfilerResource(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  // Create a resource template with the MCP SDK
  const resourceTemplate = new ResourceTemplate(
    resourceUriTemplate,
    {
      list: undefined // No listing functionality needed for profiler data
    }
  );
  logger.info(`Registering resource: ${resourceName}`);

  // Register this resource with the MCP server
  server.resource(
    resourceName,
    resourceTemplate,
    {
      description: "📊 获取Unity实时性能数据，包含CPU使用率、内存统计、GPU帧时间、调用层次分析和性能建议。dataType参数用于过滤特定指标：'cpu'-CPU性能，'memory'-内存使用，'gpu'-GPU指标，'rendering'-渲染统计(Draw Calls/Batches)，'hierarchy'-调用堆栈分析，'summary'-性能概览，'all'-完整数据。当需要了解性能状况时优先使用此资源。",
      mimeType: resourceMimeType
    },
    async (uri: URL, variables: Variables): Promise<ReadResourceResult> => {
      try {
        logger.info(`Fetching profiler resource`, { uri: uri.toString() });
        
        // Extract dataType from URI template variables
        const dataType = (variables["dataType"] as string) || "all";
        
        // Validate dataType parameter
        const validTypes = ["all", "cpu", "memory", "gpu", "rendering", "hierarchy", "summary"];
        const validatedDataType = validTypes.includes(dataType.toLowerCase()) ? dataType.toLowerCase() : "all";

        // Send request to Unity using the resource method name
        const response = await mcpUnity.sendRequest({
          method: resourceName,
          params: {
            dataType: validatedDataType
          }
        });

        if (!response.success) {
          throw new McpUnityError(
            ErrorType.RESOURCE_FETCH,
            response.message || "Failed to fetch profiler data from Unity"
          );
        }

        // Format the response data for better readability
        const formattedData = formatProfilerData(response.data || response, validatedDataType);
        
        return {
          contents: [
            {
              uri: `unity://profiler/${validatedDataType}`,
              mimeType: resourceMimeType,
              text: JSON.stringify(formattedData, null, 2)
            }
          ]
        };
        
      } catch (error) {
        logger.error(`Failed to fetch profiler resource`, error);
        throw error;
      }
    }
  );
}

/**
 * Format profiler data for better presentation to AI assistants
 * @param data Raw profiler data from Unity
 * @param dataType Type of data requested
 * @returns Formatted data with explanations and context
 */
function formatProfilerData(data: any, dataType: string): any {
  const formatted: any = {
    dataType: dataType,
    timestamp: data.timestamp || new Date().toISOString(),
    frameCount: data.frameCount || 0,
    data: data
  };

  // Add explanatory context based on data type
  switch (dataType) {
    case "cpu":
      formatted.explanation = {
        description: "CPU performance metrics showing time spent in different engine systems",
        metrics: {
          mainThreadFrameTime: "Time spent on main thread per frame (target: <16.67ms for 60fps)",
          behaviourUpdate: "Time spent in Update() methods across all scripts",
          physicsFixedUpdate: "Time spent in physics simulation",
          fps: "Current frames per second"
        },
        recommendations: generateCPURecommendations(data)
      };
      break;
      
    case "memory":
      formatted.explanation = {
        description: "Memory usage statistics for system and garbage collection",
        metrics: {
          systemUsedMemory: "Total memory used by the application (MB)",
          gcReservedMemory: "Memory reserved by garbage collector (MB)",
          gcUsedMemory: "Memory currently used by managed objects (MB)",
          totalAllocatedMemory: "Total allocated memory including native (MB)"
        },
        recommendations: generateMemoryRecommendations(data)
      };
      break;
      
    case "gpu":
      formatted.explanation = {
        description: "GPU performance metrics for rendering",
        metrics: {
          gpuFrameTime: "Time spent on GPU per frame (target: <16.67ms for 60fps)"
        },
        recommendations: generateGPURecommendations(data)
      };
      break;
      
    case "rendering":
      formatted.explanation = {
        description: "Rendering statistics including draw calls and geometry",
        metrics: {
          drawCalls: "Number of draw calls per frame (lower is better)",
          batches: "Number of batches after batching optimizations",
          setPassCalls: "Number of material/shader switches",
          triangles: "Total triangles rendered",
          vertices: "Total vertices processed"
        },
        recommendations: generateRenderingRecommendations(data)
      };
      break;
      
    case "hierarchy":
      formatted.explanation = {
        description: "CPU call hierarchy showing most expensive methods",
        metrics: {
          topCostlyMethods: "Methods taking the most CPU time, sorted by total time",
          frameIndex: "Frame number when data was captured",
          frameTimeMs: "Total frame time in milliseconds"
        }
      };
      break;
      
    case "summary":
      formatted.explanation = {
        description: "Performance summary with key metrics and automatic issue detection",
        metrics: {
          currentFPS: "Current frames per second",
          performanceGrade: "Overall performance grade (A-F)",
          issues: "Automatically detected performance issues",
          recommendations: "Suggested optimizations based on current metrics"
        }
      };
      break;
      
    case "all":
      formatted.explanation = {
        description: "Comprehensive profiler data including all metrics and analysis",
        sections: {
          cpu: "CPU performance metrics and timing",
          memory: "Memory usage and garbage collection stats",
          gpu: "GPU rendering performance",
          rendering: "Draw calls, batching, and geometry statistics",
          summary: "Overall performance assessment with recommendations"
        }
      };
      break;
  }

  return formatted;
}

/**
 * Generate CPU-specific recommendations based on the data
 */
function generateCPURecommendations(data: any): string[] {
  const recommendations: string[] = [];
  
  if (data.mainThreadFrameTime > 16.67) {
    recommendations.push("CPU frame time exceeds 60fps target. Consider optimizing Update() methods.");
  }
  
  if (data.behaviourUpdate > 8) {
    recommendations.push("High Update() overhead detected. Cache expensive operations and reduce update frequency.");
  }
  
  if (data.physicsFixedUpdate > 5) {
    recommendations.push("Physics taking significant CPU time. Consider reducing Rigidbody count or simplifying colliders.");
  }
  
  if (data.fps < 30) {
    recommendations.push("Very low FPS detected. Consider reducing scene complexity or implementing LOD system.");
  }
  
  return recommendations;
}

/**
 * Generate memory-specific recommendations based on the data
 */
function generateMemoryRecommendations(data: any): string[] {
  const recommendations: string[] = [];
  
  if (data.gcReservedMemory > 200) {
    recommendations.push("High GC memory usage. Implement object pooling to reduce allocations.");
  }
  
  if (data.systemUsedMemory > 1000) {
    recommendations.push("High system memory usage. Consider texture compression and audio optimization.");
  }
  
  const gcEfficiency = data.gcUsedMemory / Math.max(data.gcReservedMemory, 1);
  if (gcEfficiency < 0.7) {
    recommendations.push("Low GC efficiency. Avoid frequent allocations and use StringBuilder for string operations.");
  }
  
  return recommendations;
}

/**
 * Generate GPU-specific recommendations based on the data
 */
function generateGPURecommendations(data: any): string[] {
  const recommendations: string[] = [];
  
  if (data.gpuFrameTime > 16.67) {
    recommendations.push("GPU frame time exceeds 60fps target. Optimize shaders and reduce rendering complexity.");
  }
  
  return recommendations;
}

/**
 * Generate rendering-specific recommendations based on the data
 */
function generateRenderingRecommendations(data: any): string[] {
  const recommendations: string[] = [];
  
  if (data.drawCalls > 1000) {
    recommendations.push("High draw call count. Use static batching, GPU instancing, or texture atlases.");
  }
  
  if (data.setPassCalls > 500) {
    recommendations.push("High SetPass calls. Reduce number of unique materials.");
  }
  
  if (data.triangles > 1000000) {
    recommendations.push("High triangle count. Implement LOD system or reduce model complexity.");
  }
  
  if (data.drawCalls > 0 && data.batches > 0) {
    const batchingEfficiency = data.batches / data.drawCalls;
    if (batchingEfficiency < 0.5) {
      recommendations.push("Poor batching efficiency. Enable static batching in Player Settings.");
    }
  }
  
  return recommendations;
} 