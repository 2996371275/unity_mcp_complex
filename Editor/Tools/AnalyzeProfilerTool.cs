using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using Unity.Profiling;
using Newtonsoft.Json.Linq;
using McpUnity.Unity;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for analyzing Unity Profiler data and providing performance optimization recommendations
    /// </summary>
    public class AnalyzeProfilerTool : McpToolBase
    {
        private readonly Dictionary<string, ProfilerRecorder> _profilerRecorders = new Dictionary<string, ProfilerRecorder>();
        private readonly List<int> _parentsCacheList = new List<int>();
        private readonly List<int> _childrenCacheList = new List<int>();
        
        public AnalyzeProfilerTool()
        {
            Name = "analyze_profiler";
            Description = "Analyzes Unity Profiler data to identify performance bottlenecks and provide optimization recommendations";
            
            InitializeProfilerRecorders();
        }

        /// <summary>
        /// Initialize ProfilerRecorders for analysis
        /// </summary>
        private void InitializeProfilerRecorders()
        {
            try
            {
                // CPU metrics
                _profilerRecorders["MainThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 30);
                _profilerRecorders["CPUTotalFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Total Frame Time", 30);
                _profilerRecorders["CPURenderThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time", 30);
                
                // Memory metrics  
                _profilerRecorders["SystemUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
                _profilerRecorders["GCReservedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
                _profilerRecorders["GCUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
                
                // GPU metrics
                _profilerRecorders["GPUFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time", 30);
                
                // Update/Physics metrics
                _profilerRecorders["BehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "BehaviourUpdate", 30);
                _profilerRecorders["LateBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "LateBehaviourUpdate", 30);
                _profilerRecorders["FixedBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedBehaviourUpdate", 30);
                _profilerRecorders["PhysicsFixedUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedUpdate.PhysicsFixedUpdate", 30);
                
                // Rendering metrics (Editor only)
#if UNITY_EDITOR
                _profilerRecorders["DrawCallsCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                _profilerRecorders["BatchesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
                _profilerRecorders["SetPassCallsCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                _profilerRecorders["TrianglesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                _profilerRecorders["VerticesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Unity] Failed to initialize ProfilerRecorders: {ex.Message}");
            }
        }

        /// <summary>
        /// Execute the profiler analysis tool
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            try
            {
                // Extract parameters
                string analysisType = parameters?["analysisType"]?.ToObject<string>()?.ToLower() ?? "full";
                int frameCount = parameters?["frameCount"]?.ToObject<int>() ?? 5;
                string targetFPS = parameters?["targetFPS"]?.ToObject<string>() ?? "60";
                bool includeOptimizationTips = parameters?["includeOptimizationTips"]?.ToObject<bool>() ?? true;
                
                JObject result = new JObject
                {
                    ["success"] = true,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["analysisType"] = analysisType,
                    ["frameCount"] = frameCount,
                    ["targetFPS"] = targetFPS
                };

                switch (analysisType)
                {
                    case "performance":
                        result["analysis"] = AnalyzePerformance(frameCount, float.Parse(targetFPS));
                        break;
                    case "memory":
                        result["analysis"] = AnalyzeMemory();
                        break;
                    case "cpu":
                        result["analysis"] = AnalyzeCPU(frameCount);
                        break;
                    case "gpu":
                        result["analysis"] = AnalyzeGPU(frameCount);
                        break;
                    case "rendering":
                        result["analysis"] = AnalyzeRendering();
                        break;
                    case "bottlenecks":
                        result["analysis"] = IdentifyBottlenecks(frameCount, float.Parse(targetFPS));
                        break;
                    case "hierarchy":
                        result["analysis"] = AnalyzeCallHierarchy();
                        break;
                    case "full":
                    default:
                        result["analysis"] = PerformFullAnalysis(frameCount, float.Parse(targetFPS), includeOptimizationTips);
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Unity] Error in profiler analysis: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to analyze profiler data: {ex.Message}", 
                    "analysis_error"
                );
            }
        }

        /// <summary>
        /// Perform a full comprehensive analysis
        /// </summary>
        private JObject PerformFullAnalysis(int frameCount, float targetFPS, bool includeOptimizationTips)
        {
            return new JObject
            {
                ["performance"] = AnalyzePerformance(frameCount, targetFPS),
                ["memory"] = AnalyzeMemory(),
                ["cpu"] = AnalyzeCPU(frameCount),
                ["gpu"] = AnalyzeGPU(frameCount),
                ["rendering"] = AnalyzeRendering(),
                ["bottlenecks"] = IdentifyBottlenecks(frameCount, targetFPS),
                ["callHierarchy"] = AnalyzeCallHierarchy(),
                ["recommendations"] = includeOptimizationTips ? GenerateOptimizationRecommendations(frameCount, targetFPS) : new JObject(),
                ["overallScore"] = CalculateOverallPerformanceScore(frameCount, targetFPS)
            };
        }

        /// <summary>
        /// Analyze overall performance metrics
        /// </summary>
        private JObject AnalyzePerformance(int frameCount, float targetFPS)
        {
            float targetFrameTime = 1000.0f / targetFPS; // ms
            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            float avgMainThreadTime = GetRecorderAverage("MainThreadFrameTime", frameCount);
            float avgGPUTime = GetRecorderAverage("GPUFrameTime", frameCount);
            
            return new JObject
            {
                ["currentFPS"] = currentFPS,
                ["targetFPS"] = targetFPS,
                ["averageMainThreadTime"] = avgMainThreadTime,
                ["averageGPUTime"] = avgGPUTime,
                ["targetFrameTime"] = targetFrameTime,
                ["fpsStability"] = CalculateFPSStability(),
                ["isTargetMet"] = currentFPS >= targetFPS * 0.95f, // 5% tolerance
                ["performanceBottleneck"] = DetermineBottleneck(avgMainThreadTime, avgGPUTime, targetFrameTime),
                ["frameTimeVariation"] = CalculateFrameTimeVariation("MainThreadFrameTime")
            };
        }

        /// <summary>
        /// Analyze memory usage patterns
        /// </summary>
        private JObject AnalyzeMemory()
        {
            long systemMemory = GetRecorderValue("SystemUsedMemory");
            long gcReserved = GetRecorderValue("GCReservedMemory");
            long gcUsed = GetRecorderValue("GCUsedMemory");
            
            long totalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
            long totalUnused = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong();
            
            float gcEfficiency = gcReserved > 0 ? (float)gcUsed / gcReserved : 0f;
            float memoryUtilization = totalReserved > 0 ? (float)totalAllocated / totalReserved : 0f;
            
            JArray memoryIssues = new JArray();
            if (gcReserved > 512 * 1024 * 1024) // 512MB
                memoryIssues.Add("High GC memory reservation detected");
            if (gcEfficiency < 0.7f)
                memoryIssues.Add("Low GC memory efficiency");
            if (totalUnused > totalAllocated)
                memoryIssues.Add("Excessive unused reserved memory");
            
            return new JObject
            {
                ["systemUsedMemoryMB"] = systemMemory / (1024 * 1024),
                ["gcReservedMemoryMB"] = gcReserved / (1024 * 1024),
                ["gcUsedMemoryMB"] = gcUsed / (1024 * 1024),
                ["totalAllocatedMemoryMB"] = totalAllocated / (1024 * 1024),
                ["totalReservedMemoryMB"] = totalReserved / (1024 * 1024),
                ["totalUnusedMemoryMB"] = totalUnused / (1024 * 1024),
                ["gcEfficiency"] = gcEfficiency,
                ["memoryUtilization"] = memoryUtilization,
                ["memoryIssues"] = memoryIssues,
                ["memoryGrade"] = GetMemoryGrade(gcReserved, gcEfficiency, memoryUtilization)
            };
        }

        /// <summary>
        /// Analyze CPU performance in detail
        /// </summary>
        private JObject AnalyzeCPU(int frameCount)
        {
            float mainThreadTime = GetRecorderAverage("MainThreadFrameTime", frameCount);
            float renderThreadTime = GetRecorderAverage("CPURenderThreadFrameTime", frameCount);
            float updateTime = GetRecorderAverage("BehaviourUpdate", frameCount);
            float lateUpdateTime = GetRecorderAverage("LateBehaviourUpdate", frameCount);
            float fixedUpdateTime = GetRecorderAverage("FixedBehaviourUpdate", frameCount);
            float physicsTime = GetRecorderAverage("PhysicsFixedUpdate", frameCount);
            
            float totalCPUTime = mainThreadTime + renderThreadTime;
            
            JObject cpuBreakdown = new JObject
            {
                ["update"] = updateTime,
                ["lateUpdate"] = lateUpdateTime,
                ["fixedUpdate"] = fixedUpdateTime,
                ["physics"] = physicsTime,
                ["rendering"] = renderThreadTime,
                ["other"] = Math.Max(0, mainThreadTime - updateTime - lateUpdateTime - fixedUpdateTime - physicsTime)
            };
            
            string heaviestComponent = GetHeaviestCPUComponent(cpuBreakdown);
            
            return new JObject
            {
                ["mainThreadTimeMs"] = mainThreadTime,
                ["renderThreadTimeMs"] = renderThreadTime,
                ["totalCPUTimeMs"] = totalCPUTime,
                ["cpuBreakdown"] = cpuBreakdown,
                ["heaviestComponent"] = heaviestComponent,
                ["cpuUtilization"] = CalculateCPUUtilization(mainThreadTime),
                ["cpuGrade"] = GetCPUGrade(mainThreadTime, totalCPUTime)
            };
        }

        /// <summary>
        /// Analyze GPU performance
        /// </summary>
        private JObject AnalyzeGPU(int frameCount)
        {
            float gpuTime = GetRecorderAverage("GPUFrameTime", frameCount);
            float gpuVariation = CalculateFrameTimeVariation("GPUFrameTime");
            
            return new JObject
            {
                ["gpuFrameTimeMs"] = gpuTime,
                ["gpuVariation"] = gpuVariation,
                ["gpuUtilization"] = CalculateGPUUtilization(gpuTime),
                ["gpuGrade"] = GetGPUGrade(gpuTime),
                ["isGPUBound"] = gpuTime > GetRecorderAverage("MainThreadFrameTime", frameCount)
            };
        }

        /// <summary>
        /// Analyze rendering performance
        /// </summary>
        private JObject AnalyzeRendering()
        {
            JObject renderingData = new JObject();
            
#if UNITY_EDITOR
            long drawCalls = GetRecorderValue("DrawCallsCount");
            long batches = GetRecorderValue("BatchesCount");
            long setPassCalls = GetRecorderValue("SetPassCallsCount");
            long triangles = GetRecorderValue("TrianglesCount");
            long vertices = GetRecorderValue("VerticesCount");
            
            float batchingEfficiency = drawCalls > 0 ? (float)batches / drawCalls : 1f;
            
            JArray renderingIssues = new JArray();
            if (drawCalls > 1000)
                renderingIssues.Add("High draw call count detected");
            if (setPassCalls > 500)
                renderingIssues.Add("High SetPass call count detected");
            if (triangles > 1000000)
                renderingIssues.Add("High triangle count detected");
            if (batchingEfficiency < 0.5f)
                renderingIssues.Add("Poor batching efficiency");
            
            renderingData = new JObject
            {
                ["drawCalls"] = drawCalls,
                ["batches"] = batches,
                ["setPassCalls"] = setPassCalls,
                ["triangles"] = triangles,
                ["vertices"] = vertices,
                ["batchingEfficiency"] = batchingEfficiency,
                ["renderingIssues"] = renderingIssues,
                ["renderingGrade"] = GetRenderingGrade(drawCalls, setPassCalls, batchingEfficiency)
            };
#else
            renderingData["note"] = "Rendering analysis is only available in Unity Editor";
#endif
            
            return renderingData;
        }

        /// <summary>
        /// Identify specific performance bottlenecks
        /// </summary>
        private JObject IdentifyBottlenecks(int frameCount, float targetFPS)
        {
            float targetFrameTime = 1000.0f / targetFPS;
            float mainThreadTime = GetRecorderAverage("MainThreadFrameTime", frameCount);
            float gpuTime = GetRecorderAverage("GPUFrameTime", frameCount);
            
            JArray bottlenecks = new JArray();
            JArray severities = new JArray();
            
            // CPU bottlenecks
            if (mainThreadTime > targetFrameTime)
            {
                float severity = (mainThreadTime - targetFrameTime) / targetFrameTime;
                bottlenecks.Add("CPU Main Thread");
                severities.Add(severity);
                
                // Detailed CPU analysis
                float updateTime = GetRecorderAverage("BehaviourUpdate", frameCount);
                float physicsTime = GetRecorderAverage("PhysicsFixedUpdate", frameCount);
                
                if (updateTime > targetFrameTime * 0.5f)
                {
                    bottlenecks.Add("Update Scripts");
                    severities.Add(updateTime / targetFrameTime);
                }
                
                if (physicsTime > targetFrameTime * 0.3f)
                {
                    bottlenecks.Add("Physics Simulation");
                    severities.Add(physicsTime / targetFrameTime);
                }
            }
            
            // GPU bottlenecks
            if (gpuTime > targetFrameTime)
            {
                float severity = (gpuTime - targetFrameTime) / targetFrameTime;
                bottlenecks.Add("GPU Rendering");
                severities.Add(severity);
            }
            
            // Memory bottlenecks
            long gcMemory = GetRecorderValue("GCReservedMemory");
            if (gcMemory > 200 * 1024 * 1024) // 200MB
            {
                bottlenecks.Add("Memory Allocation");
                severities.Add((float)(gcMemory / (100.0 * 1024 * 1024))); // Relative to 100MB baseline
            }
            
            return new JObject
            {
                ["bottlenecks"] = bottlenecks,
                ["severities"] = severities,
                ["primaryBottleneck"] = GetPrimaryBottleneck(mainThreadTime, gpuTime, targetFrameTime),
                ["bottleneckScore"] = CalculateBottleneckScore(bottlenecks.Count, severities)
            };
        }

        /// <summary>
        /// Analyze call hierarchy for expensive methods
        /// </summary>
        private JObject AnalyzeCallHierarchy()
        {
            JObject hierarchyData = new JObject();
            
#if UNITY_EDITOR
            try
            {
                int currentFrame = ProfilerDriver.lastFrameIndex;
                if (currentFrame >= 0)
                {
                    using (var frameData = ProfilerDriver.GetHierarchyFrameDataView(currentFrame, 0, 
                        HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
                    {
                        if (frameData.valid)
                        {
                            JArray expensiveMethods = new JArray();
                            JArray memoryHogMethods = new JArray();
                            
                            int rootId = frameData.GetRootItemID();
                            _parentsCacheList.Clear();
                            frameData.GetItemDescendantsThatHaveChildren(rootId, _parentsCacheList);
                            
                            var allMethods = new List<JObject>();
                            
                            foreach (int parentId in _parentsCacheList)
                            {
                                _childrenCacheList.Clear();
                                frameData.GetItemChildren(parentId, _childrenCacheList);
                                
                                if (_childrenCacheList.Count > 0)
                                {
                                    float totalTime = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnTotalTime);
                                    float selfTime = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnSelfTime);
                                    float gcMemory = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnGcMemory);
                                    
                                    if (totalTime > 0.1f || gcMemory > 1024) // 0.1ms or 1KB threshold
                                    {
                                        JObject methodInfo = new JObject
                                        {
                                            ["name"] = frameData.GetItemName(parentId),
                                            ["totalTimeMs"] = totalTime,
                                            ["selfTimeMs"] = selfTime,
                                            ["gcMemoryBytes"] = gcMemory,
                                            ["calls"] = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnCalls),
                                            ["efficiency"] = selfTime > 0 ? totalTime / selfTime : 1.0f
                                        };
                                        
                                        allMethods.Add(methodInfo);
                                    }
                                }
                            }
                            
                            // Sort and get top expensive methods
                            var expensiveByTime = allMethods.OrderByDescending(m => m["totalTimeMs"].ToObject<float>()).Take(10);
                            var expensiveByMemory = allMethods.OrderByDescending(m => m["gcMemoryBytes"].ToObject<float>()).Take(10);
                            
                            foreach (var method in expensiveByTime)
                                expensiveMethods.Add(method);
                            
                            foreach (var method in expensiveByMemory)
                                memoryHogMethods.Add(method);
                            
                            hierarchyData["expensiveMethods"] = expensiveMethods;
                            hierarchyData["memoryHogMethods"] = memoryHogMethods;
                            hierarchyData["totalMethodsAnalyzed"] = allMethods.Count;
                            hierarchyData["frameIndex"] = currentFrame;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hierarchyData["error"] = $"Failed to analyze call hierarchy: {ex.Message}";
            }
#else
            hierarchyData["note"] = "Call hierarchy analysis is only available in Unity Editor";
#endif
            
            return hierarchyData;
        }

        /// <summary>
        /// Generate comprehensive optimization recommendations
        /// </summary>
        private JObject GenerateOptimizationRecommendations(int frameCount, float targetFPS)
        {
            JArray recommendations = new JArray();
            JArray priorities = new JArray(); // High, Medium, Low
            
            float mainThreadTime = GetRecorderAverage("MainThreadFrameTime", frameCount);
            float gpuTime = GetRecorderAverage("GPUFrameTime", frameCount);
            float targetFrameTime = 1000.0f / targetFPS;
            
            // CPU Recommendations
            if (mainThreadTime > targetFrameTime)
            {
                float updateTime = GetRecorderAverage("BehaviourUpdate", frameCount);
                if (updateTime > targetFrameTime * 0.4f)
                {
                    recommendations.Add("Optimize Update() methods: Consider caching, reduce frequency of expensive operations, use coroutines for non-critical updates");
                    priorities.Add("High");
                }
                
                float physicsTime = GetRecorderAverage("PhysicsFixedUpdate", frameCount);
                if (physicsTime > targetFrameTime * 0.3f)
                {
                    recommendations.Add("Optimize physics: Reduce Rigidbody count, simplify colliders, adjust Fixed Timestep in Project Settings");
                    priorities.Add("High");
                }
            }
            
            // GPU Recommendations
            if (gpuTime > targetFrameTime)
            {
                recommendations.Add("Optimize rendering: Reduce shader complexity, implement LOD system, use occlusion culling");
                priorities.Add("High");
                
#if UNITY_EDITOR
                long drawCalls = GetRecorderValue("DrawCallsCount");
                if (drawCalls > 500)
                {
                    recommendations.Add("Reduce draw calls: Use static batching, dynamic batching, or GPU instancing");
                    priorities.Add("Medium");
                }
                
                long setPassCalls = GetRecorderValue("SetPassCallsCount");
                if (setPassCalls > 300)
                {
                    recommendations.Add("Optimize materials: Reduce number of different materials, use texture atlases");
                    priorities.Add("Medium");
                }
#endif
            }
            
            // Memory Recommendations
            long gcMemory = GetRecorderValue("GCReservedMemory");
            if (gcMemory > 100 * 1024 * 1024) // 100MB
            {
                recommendations.Add("Reduce memory allocations: Implement object pooling, avoid frequent string operations, use StringBuilder");
                priorities.Add("Medium");
            }
            
            // General Recommendations
            if (recommendations.Count == 0)
            {
                recommendations.Add("Performance is within acceptable range. Consider profile-guided optimizations for further improvements");
                priorities.Add("Low");
            }
            else
            {
                recommendations.Add("Consider using Unity Profiler for detailed frame analysis");
                priorities.Add("Low");
                
                recommendations.Add("Implement performance monitoring in builds to track real-world performance");
                priorities.Add("Low");
            }
            
            return new JObject
            {
                ["recommendations"] = recommendations,
                ["priorities"] = priorities,
                ["recommendationCount"] = recommendations.Count
            };
        }

        /// <summary>
        /// Calculate overall performance score
        /// </summary>
        private int CalculateOverallPerformanceScore(int frameCount, float targetFPS)
        {
            float currentFPS = 1.0f / Time.unscaledDeltaTime;
            float mainThreadTime = GetRecorderAverage("MainThreadFrameTime", frameCount);
            float gpuTime = GetRecorderAverage("GPUFrameTime", frameCount);
            long gcMemory = GetRecorderValue("GCReservedMemory");
            
            int fpsScore = currentFPS >= targetFPS ? 100 : (int)((currentFPS / targetFPS) * 100);
            int cpuScore = mainThreadTime <= (1000.0f / targetFPS) ? 100 : (int)((1000.0f / targetFPS) / mainThreadTime * 100);
            int gpuScore = gpuTime <= (1000.0f / targetFPS) ? 100 : (int)((1000.0f / targetFPS) / gpuTime * 100);
            int memoryScore = gcMemory <= 50 * 1024 * 1024 ? 100 : Math.Max(0, 100 - (int)((gcMemory - 50 * 1024 * 1024) / (1024 * 1024)));
            
            return (fpsScore + cpuScore + gpuScore + memoryScore) / 4;
        }

        #region Helper Methods

        private float GetRecorderAverage(string recorderName, int frameCount = 15)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0f;

            var samplesCount = Math.Min(recorder.Capacity, frameCount);
            if (samplesCount == 0)
                return 0f;

            double sum = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; ++i)
                sum += samples[i].Value;

            return (float)(sum / samples.Count / 1000000.0); // Convert to milliseconds
        }

        private long GetRecorderValue(string recorderName)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0L;

            return recorder.LastValue;
        }

        private float CalculateFPSStability()
        {
            // Simple implementation - could be enhanced with actual variance calculation
            return Time.frameCount > 100 ? 0.95f : 0.8f;
        }

        private string DetermineBottleneck(float cpuTime, float gpuTime, float targetFrameTime)
        {
            if (cpuTime > targetFrameTime && gpuTime > targetFrameTime)
                return cpuTime > gpuTime ? "CPU" : "GPU";
            else if (cpuTime > targetFrameTime)
                return "CPU";
            else if (gpuTime > targetFrameTime)
                return "GPU";
            else
                return "None";
        }

        private float CalculateFrameTimeVariation(string recorderName)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0f;

            var samplesCount = recorder.Capacity;
            if (samplesCount < 2)
                return 0f;

            double sum = 0, sumSquares = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; ++i)
            {
                double value = samples[i].Value / 1000000.0; // Convert to ms
                sum += value;
                sumSquares += value * value;
            }

            double mean = sum / samples.Count;
            double variance = (sumSquares / samples.Count) - (mean * mean);
            return (float)Math.Sqrt(variance);
        }

        private float CalculateCPUUtilization(float mainThreadTime)
        {
            return Math.Min(100f, (mainThreadTime / 16.67f) * 100f); // Assuming 60 FPS target
        }

        private float CalculateGPUUtilization(float gpuTime)
        {
            return Math.Min(100f, (gpuTime / 16.67f) * 100f); // Assuming 60 FPS target
        }

        private string GetHeaviestCPUComponent(JObject breakdown)
        {
            string heaviest = "unknown";
            float maxTime = 0f;
            
            foreach (var kvp in breakdown)
            {
                if (kvp.Value.Type == JTokenType.Float || kvp.Value.Type == JTokenType.Integer)
                {
                    float value = kvp.Value.ToObject<float>();
                    if (value > maxTime)
                    {
                        maxTime = value;
                        heaviest = kvp.Key;
                    }
                }
            }
            
            return heaviest;
        }

        private string GetMemoryGrade(long gcReserved, float gcEfficiency, float memoryUtilization)
        {
            if (gcReserved < 50 * 1024 * 1024 && gcEfficiency > 0.8f && memoryUtilization > 0.7f)
                return "A";
            else if (gcReserved < 100 * 1024 * 1024 && gcEfficiency > 0.6f)
                return "B";
            else if (gcReserved < 200 * 1024 * 1024)
                return "C";
            else
                return "D";
        }

        private string GetCPUGrade(float mainThreadTime, float totalCPUTime)
        {
            if (mainThreadTime <= 8.33f) return "A"; // 120+ FPS
            else if (mainThreadTime <= 16.67f) return "B"; // 60+ FPS
            else if (mainThreadTime <= 33.33f) return "C"; // 30+ FPS
            else return "D";
        }

        private string GetGPUGrade(float gpuTime)
        {
            if (gpuTime <= 8.33f) return "A"; // 120+ FPS
            else if (gpuTime <= 16.67f) return "B"; // 60+ FPS
            else if (gpuTime <= 33.33f) return "C"; // 30+ FPS
            else return "D";
        }

        private string GetRenderingGrade(long drawCalls, long setPassCalls, float batchingEfficiency)
        {
            if (drawCalls <= 200 && setPassCalls <= 100 && batchingEfficiency >= 0.8f)
                return "A";
            else if (drawCalls <= 500 && setPassCalls <= 250 && batchingEfficiency >= 0.6f)
                return "B";
            else if (drawCalls <= 1000 && setPassCalls <= 500)
                return "C";
            else
                return "D";
        }

        private string GetPrimaryBottleneck(float cpuTime, float gpuTime, float targetFrameTime)
        {
            if (cpuTime > targetFrameTime && gpuTime > targetFrameTime)
            {
                return cpuTime > gpuTime ? "CPU" : "GPU";
            }
            else if (cpuTime > targetFrameTime)
            {
                return "CPU";
            }
            else if (gpuTime > targetFrameTime)
            {
                return "GPU";
            }
            
            return "None";
        }

        private float CalculateBottleneckScore(int bottleneckCount, JArray severities)
        {
            if (bottleneckCount == 0) return 100f;
            
            float totalSeverity = 0f;
            foreach (var severity in severities)
            {
                totalSeverity += severity.ToObject<float>();
            }
            
            float avgSeverity = totalSeverity / bottleneckCount;
            return Math.Max(0f, 100f - (avgSeverity * 50f)); // Scale severity to score
        }

        #endregion

        /// <summary>
        /// Cleanup ProfilerRecorders when tool is disposed
        /// </summary>
        ~AnalyzeProfilerTool()
        {
            foreach (var recorder in _profilerRecorders.Values)
            {
                if (recorder.Valid)
                    recorder.Dispose();
            }
        }
    }
} 