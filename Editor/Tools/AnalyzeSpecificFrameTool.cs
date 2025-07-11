using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using Newtonsoft.Json.Linq;
using McpUnity.Unity;
using McpUnity.Utils;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for analyzing specific frame data from Unity Profiler window
    /// </summary>
    public class AnalyzeSpecificFrameTool : McpToolBase
    {
        public AnalyzeSpecificFrameTool()
        {
            Name = "analyze_specific_frame";
            Description = "分析Unity Profiler窗口中特定帧的详细性能数据，包括CPU耗时、内存分配、渲染统计等";
            IsAsync = false;
        }

        /// <summary>
        /// Execute the specific frame analysis tool
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            try
            {
                // Extract parameters
                int? frameIndex = parameters?["frameIndex"]?.ToObject<int?>();
                string analysisType = parameters?["analysisType"]?.ToObject<string>()?.ToLower() ?? "full";
                bool includeHierarchy = parameters?["includeHierarchy"]?.ToObject<bool>() ?? true;
                bool includeMemoryAllocs = parameters?["includeMemoryAllocs"]?.ToObject<bool>() ?? true;

                // Validate frame index
                if (!frameIndex.HasValue)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "frameIndex参数是必需的。请提供要分析的帧号，例如1183", 
                        "missing_parameter"
                    );
                }

                // Check if Profiler window is open and has data
                if (!IsProfilerWindowAvailable())
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "Profiler窗口未打开或没有可用数据。请先打开Profiler窗口(Window > Analysis > Profiler)并录制一些性能数据", 
                        "profiler_not_available"
                    );
                }

                // Get the frame range available in profiler
                int firstFrame = ProfilerDriver.firstFrameIndex;
                int lastFrame = ProfilerDriver.lastFrameIndex;
                
                if (frameIndex.Value < firstFrame || frameIndex.Value > lastFrame)
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"帧号{frameIndex.Value}超出可用范围。当前Profiler数据范围: {firstFrame} - {lastFrame}", 
                        "frame_out_of_range"
                    );
                }

                McpLogger.LogInfo($"开始分析第{frameIndex.Value}帧的性能数据...");

                JObject result = new JObject
                {
                    ["success"] = true,
                    ["frameIndex"] = frameIndex.Value,
                    ["analysisType"] = analysisType,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["frameRange"] = new JObject
                    {
                        ["firstFrame"] = firstFrame,
                        ["lastFrame"] = lastFrame,
                        ["totalFrames"] = lastFrame - firstFrame + 1
                    }
                };

                // Perform analysis based on type
                switch (analysisType)
                {
                    case "cpu":
                        result["analysis"] = AnalyzeCPUFrame(frameIndex.Value, includeHierarchy);
                        break;
                    case "memory":
                        result["analysis"] = AnalyzeMemoryFrame(frameIndex.Value, includeMemoryAllocs);
                        break;
                    case "gpu":
                        result["analysis"] = AnalyzeGPUFrame(frameIndex.Value);
                        break;
                    case "rendering":
                        result["analysis"] = AnalyzeRenderingFrame(frameIndex.Value);
                        break;
                    case "hierarchy":
                        result["analysis"] = AnalyzeFrameHierarchy(frameIndex.Value);
                        break;
                    case "full":
                    default:
                        result["analysis"] = PerformFullFrameAnalysis(frameIndex.Value, includeHierarchy, includeMemoryAllocs);
                        break;
                }

                McpLogger.LogInfo($"第{frameIndex.Value}帧分析完成");
                return result;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"分析特定帧时发生错误: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"分析特定帧失败: {ex.Message}", 
                    "analysis_error"
                );
            }
        }

        /// <summary>
        /// Check if Profiler window is available and has data
        /// </summary>
        private bool IsProfilerWindowAvailable()
        {
            try
            {
                return ProfilerDriver.firstFrameIndex >= 0 && ProfilerDriver.lastFrameIndex >= ProfilerDriver.firstFrameIndex;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Perform comprehensive analysis of a specific frame
        /// </summary>
        private JObject PerformFullFrameAnalysis(int frameIndex, bool includeHierarchy, bool includeMemoryAllocs)
        {
            JObject analysis = new JObject
            {
                ["frameBasics"] = GetFrameBasicInfo(frameIndex),
                ["cpuAnalysis"] = AnalyzeCPUFrame(frameIndex, includeHierarchy),
                ["memoryAnalysis"] = AnalyzeMemoryFrame(frameIndex, includeMemoryAllocs),
                ["gpuAnalysis"] = AnalyzeGPUFrame(frameIndex),
                ["renderingAnalysis"] = AnalyzeRenderingFrame(frameIndex)
            };

            if (includeHierarchy)
            {
                analysis["hierarchyAnalysis"] = AnalyzeFrameHierarchy(frameIndex);
            }

            // Generate frame-specific recommendations
            analysis["recommendations"] = GenerateFrameRecommendations(analysis);
            analysis["performanceGrade"] = CalculateFramePerformanceGrade(analysis);

            return analysis;
        }

        /// <summary>
        /// Get basic frame information using FrameDataView
        /// </summary>
        private JObject GetFrameBasicInfo(int frameIndex)
        {
            JObject frameInfo = new JObject
            {
                ["frameIndex"] = frameIndex,
                ["frameTime"] = 0f,
                ["frameTimeGPU"] = 0f,
                ["fps"] = 0f
            };

            try
            {
                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, 
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (frameData.valid)
                    {
                        frameInfo["frameTime"] = frameData.frameTimeMs;
                        frameInfo["frameTimeGPU"] = frameData.frameGpuTimeMs;
                        frameInfo["fps"] = frameData.frameFps;
                        frameInfo["frameStartTime"] = frameData.frameStartTimeMs;
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"获取帧基础信息时出错: {ex.Message}");
            }

            return frameInfo;
        }

        /// <summary>
        /// Analyze CPU performance for specific frame using HierarchyFrameDataView
        /// </summary>
        private JObject AnalyzeCPUFrame(int frameIndex, bool includeHierarchy)
        {
            JObject cpuAnalysis = new JObject
            {
                ["mainThreadTime"] = 0f,
                ["renderThreadTime"] = 0f,
                ["cpuUtilizationPercent"] = 0f
            };

            try
            {
                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, 
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (frameData.valid)
                    {
                        cpuAnalysis["mainThreadTime"] = frameData.frameTimeMs;
                        
                        // Calculate CPU utilization
                        float frameTimeMs = frameData.frameTimeMs;
                        cpuAnalysis["cpuUtilizationPercent"] = (frameTimeMs / 16.67f) * 100f; // Assuming 60fps target

                        // Get detailed CPU breakdown by analyzing hierarchy
                        if (includeHierarchy)
                        {
                            cpuAnalysis["topCPUConsumers"] = GetTopCPUConsumersFromHierarchy(frameData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"CPU分析时出错: {ex.Message}");
            }

            return cpuAnalysis;
        }

        /// <summary>
        /// Analyze memory for specific frame
        /// </summary>
        private JObject AnalyzeMemoryFrame(int frameIndex, bool includeAllocs)
        {
            JObject memoryAnalysis = new JObject
            {
                ["gcAlloc"] = 0f,
                ["systemMemoryUsed"] = 0f
            };

            try
            {
                // Get GC allocation data using RawFrameDataView
                if (includeAllocs)
                {
                    memoryAnalysis["memoryAllocations"] = AnalyzeMemoryAllocationsFromRaw(frameIndex);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"内存分析时出错: {ex.Message}");
            }

            return memoryAnalysis;
        }

        /// <summary>
        /// Analyze GPU performance for specific frame
        /// </summary>
        private JObject AnalyzeGPUFrame(int frameIndex)
        {
            JObject gpuAnalysis = new JObject
            {
                ["gpuFrameTime"] = 0f,
                ["gpuUtilizationPercent"] = 0f
            };

            try
            {
                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, 
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (frameData.valid)
                    {
                        float gpuTime = frameData.frameGpuTimeMs;
                        gpuAnalysis["gpuFrameTime"] = gpuTime;
                        gpuAnalysis["gpuUtilizationPercent"] = (gpuTime / 16.67f) * 100f; // Assuming 60fps target
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"GPU分析时出错: {ex.Message}");
            }

            return gpuAnalysis;
        }

        /// <summary>
        /// Analyze rendering performance for specific frame
        /// </summary>
        private JObject AnalyzeRenderingFrame(int frameIndex)
        {
            JObject renderingAnalysis = new JObject
            {
                ["drawCalls"] = 0,
                ["triangles"] = 0,
                ["vertices"] = 0,
                ["batchingEfficiency"] = 0f
            };

            try
            {
                // Try to get rendering statistics from frame data
                // Note: Some rendering stats might not be available in all Unity versions
                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, 
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (frameData.valid)
                    {
                        // Look for rendering-related markers in the frame data
                        var renderingStats = ExtractRenderingStatsFromFrame(frameData);
                        foreach (var kvp in renderingStats)
                        {
                            renderingAnalysis[kvp.Key] = JToken.FromObject(kvp.Value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"渲染分析时出错: {ex.Message}");
            }

            return renderingAnalysis;
        }

        /// <summary>
        /// Analyze call hierarchy for specific frame
        /// </summary>
        private JObject AnalyzeFrameHierarchy(int frameIndex)
        {
            JObject hierarchyAnalysis = new JObject
            {
                ["frameIndex"] = frameIndex,
                ["topMethods"] = new JArray(),
                ["totalMethodsAnalyzed"] = 0
            };

            try
            {
                using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(frameIndex, 0, 
                    HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                {
                    if (frameData.valid)
                    {
                        hierarchyAnalysis["topMethods"] = GetTopMethodsByTime(frameData, 10);
                        hierarchyAnalysis["renderingHierarchy"] = GetRenderingHierarchy(frameData);
                        hierarchyAnalysis["updateHierarchy"] = GetUpdateHierarchy(frameData);
                    }
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"层次分析时出错: {ex.Message}");
            }

            return hierarchyAnalysis;
        }

        /// <summary>
        /// Get top CPU consumers from hierarchy data
        /// </summary>
        private JArray GetTopCPUConsumersFromHierarchy(HierarchyFrameDataView frameData)
        {
            JArray topConsumers = new JArray();
            
            try
            {
                int rootId = frameData.GetRootItemID();
                var parentsList = new List<int>();
                var childrenList = new List<int>();
                
                frameData.GetItemDescendantsThatHaveChildren(rootId, parentsList);
                
                var allMethods = new List<JObject>();
                
                foreach (int parentId in parentsList)
                {
                    childrenList.Clear();
                    frameData.GetItemChildren(parentId, childrenList);
                    
                    if (childrenList.Count > 0)
                    {
                        float totalTime = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnTotalTime);
                        if (totalTime > 0.1f) // Only include methods taking > 0.1ms
                        {
                            JObject methodInfo = new JObject
                            {
                                ["methodName"] = frameData.GetItemName(parentId),
                                ["timeMs"] = totalTime,
                                ["percentage"] = (totalTime / frameData.frameTimeMs) * 100f
                            };
                            
                            allMethods.Add(methodInfo);
                        }
                    }
                }
                
                // Sort by time descending and take top 10
                var sortedMethods = allMethods
                    .OrderByDescending(m => m["timeMs"]?.ToObject<float>() ?? 0f)
                    .Take(10);
                
                foreach (var method in sortedMethods)
                {
                    topConsumers.Add(method);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"获取CPU消耗详情时出错: {ex.Message}");
            }
            
            return topConsumers;
        }

        /// <summary>
        /// Analyze memory allocations using RawFrameDataView
        /// </summary>
        private JObject AnalyzeMemoryAllocationsFromRaw(int frameIndex)
        {
            JObject allocations = new JObject
            {
                ["totalGCAlloc"] = 0L,
                ["allocCount"] = 0
            };
            
            try
            {
                long totalGCAlloc = 0;
                int allocCount = 0;
                
                // Iterate through all threads to find GC allocations
                for (int threadIndex = 0; ; threadIndex++)
                {
                    using (var rawFrameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                    {
                        if (!rawFrameData.valid)
                            break;
                        
                        // Look for GC.Alloc marker
                        int gcAllocMarkerId = rawFrameData.GetMarkerId("GC.Alloc");
                        if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                            continue;
                        
                        for (int i = 0; i < rawFrameData.sampleCount; i++)
                        {
                            if (rawFrameData.GetSampleMarkerId(i) == gcAllocMarkerId)
                            {
                                // Try to get allocation size from metadata
                                if (rawFrameData.GetSampleMetadataCount(i) > 0)
                                {
                                    long allocSize = rawFrameData.GetSampleMetadataAsLong(i, 0);
                                    totalGCAlloc += allocSize;
                                    allocCount++;
                                }
                            }
                        }
                    }
                }
                
                allocations["totalGCAlloc"] = totalGCAlloc;
                allocations["allocCount"] = allocCount;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"分析内存分配时出错: {ex.Message}");
            }
            
            return allocations;
        }

        /// <summary>
        /// Extract rendering statistics from frame data
        /// </summary>
        private Dictionary<string, object> ExtractRenderingStatsFromFrame(HierarchyFrameDataView frameData)
        {
            var stats = new Dictionary<string, object>();
            
            try
            {
                // This is a simplified implementation
                // In a real implementation, you would traverse the hierarchy to find rendering-related markers
                stats["note"] = "渲染统计需要更深层的Profiler集成";
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"提取渲染统计时出错: {ex.Message}");
            }
            
            return stats;
        }

        /// <summary>
        /// Get top methods by execution time from hierarchy data
        /// </summary>
        private JArray GetTopMethodsByTime(HierarchyFrameDataView frameData, int count)
        {
            return GetTopCPUConsumersFromHierarchy(frameData);
        }

        /// <summary>
        /// Get rendering call hierarchy from frame data
        /// </summary>
        private JObject GetRenderingHierarchy(HierarchyFrameDataView frameData)
        {
            return new JObject
            {
                ["note"] = "渲染层次分析需要遍历特定的渲染标记"
            };
        }

        /// <summary>
        /// Get update call hierarchy from frame data
        /// </summary>
        private JObject GetUpdateHierarchy(HierarchyFrameDataView frameData)
        {
            return new JObject
            {
                ["note"] = "Update层次分析需要遍历特定的Update标记"
            };
        }

        /// <summary>
        /// Generate frame-specific recommendations
        /// </summary>
        private JArray GenerateFrameRecommendations(JObject analysis)
        {
            JArray recommendations = new JArray();

            try
            {
                var frameBasics = analysis["frameBasics"] as JObject;
                var cpuAnalysis = analysis["cpuAnalysis"] as JObject;

                // CPU recommendations
                float mainThreadTime = frameBasics?["frameTime"]?.ToObject<float>() ?? 0f;
                if (mainThreadTime > 16.67f)
                {
                    recommendations.Add("主线程耗时超过16.67ms，建议优化Update方法和减少每帧计算量");
                }

                float cpuUtilization = cpuAnalysis?["cpuUtilizationPercent"]?.ToObject<float>() ?? 0f;
                if (cpuUtilization > 100f)
                {
                    recommendations.Add("CPU利用率超过100%，存在严重性能瓶颈");
                }

                if (recommendations.Count == 0)
                {
                    recommendations.Add("该帧性能表现良好，无明显瓶颈");
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"生成建议时出错: {ex.Message}");
                recommendations.Add("无法生成详细建议，但已完成性能数据分析");
            }

            return recommendations;
        }

        /// <summary>
        /// Calculate overall performance grade for the frame
        /// </summary>
        private string CalculateFramePerformanceGrade(JObject analysis)
        {
            try
            {
                var frameBasics = analysis["frameBasics"] as JObject;
                float mainThreadTime = frameBasics?["frameTime"]?.ToObject<float>() ?? 0f;
                float gpuTime = frameBasics?["frameTimeGPU"]?.ToObject<float>() ?? 0f;

                int score = 100;

                // Deduct points for poor performance
                if (mainThreadTime > 16.67f) score -= 20;
                if (mainThreadTime > 33.33f) score -= 20;
                if (gpuTime > 16.67f) score -= 15;

                if (score >= 90) return "A";
                if (score >= 80) return "B";
                if (score >= 70) return "C";
                if (score >= 60) return "D";
                return "F";
            }
            catch
            {
                return "未知";
            }
        }
    }
} 