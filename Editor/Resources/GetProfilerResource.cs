using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using Unity.Profiling;
using Newtonsoft.Json.Linq;

namespace McpUnity.Resources
{
    /// <summary>
    /// Resource for retrieving Unity Profiler performance data
    /// </summary>
    public class GetProfilerResource : McpResourceBase
    {
        private readonly Dictionary<string, ProfilerRecorder> _profilerRecorders = new Dictionary<string, ProfilerRecorder>();
        private readonly List<int> _parentsCacheList = new List<int>();
        private readonly List<int> _childrenCacheList = new List<int>();
        
        public GetProfilerResource()
        {
            Name = "get_profiler_data";
            Description = "Retrieves Unity Profiler performance data including CPU usage, memory statistics, GPU frame time, and call hierarchy analysis";
            Uri = "unity://profiler/{dataType}";
            IsAsync = false;
            
            InitializeProfilerRecorders();
        }

        /// <summary>
        /// Initialize ProfilerRecorders for various performance metrics
        /// </summary>
        private void InitializeProfilerRecorders()
        {
            try
            {
                // CPU metrics
                _profilerRecorders["MainThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
                _profilerRecorders["CPUTotalFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Total Frame Time", 15);
                _profilerRecorders["CPURenderThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time", 15);
                
                // Memory metrics  
                _profilerRecorders["SystemUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
                _profilerRecorders["GCReservedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
                _profilerRecorders["GCUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
                
                // GPU metrics
                _profilerRecorders["GPUFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time", 15);
                
                // Update/Physics metrics
                _profilerRecorders["BehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "BehaviourUpdate", 15);
                _profilerRecorders["LateBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "LateBehaviourUpdate", 15);
                _profilerRecorders["FixedBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedBehaviourUpdate", 15);
                _profilerRecorders["PhysicsFixedUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedUpdate.PhysicsFixedUpdate", 15);
                
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
        /// Fetch profiler data based on the requested data type
        /// </summary>
        /// <param name="parameters">Resource parameters containing dataType filter</param>
        /// <returns>A JObject containing profiler performance data</returns>
        public override JObject Fetch(JObject parameters)
        {
            try
            {
                string dataType = parameters?["dataType"]?.ToObject<string>()?.ToLower() ?? "all";
                
                JObject result = new JObject
                {
                    ["success"] = true,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["frameCount"] = Time.frameCount
                };

                switch (dataType)
                {
                    case "cpu":
                        result["data"] = GetCPUData();
                        break;
                    case "memory":
                        result["data"] = GetMemoryData();
                        break;
                    case "gpu":
                        result["data"] = GetGPUData();
                        break;
                    case "rendering":
                        result["data"] = GetRenderingData();
                        break;
                    case "hierarchy":
                        result["data"] = GetHierarchyData();
                        break;
                    case "summary":
                        result["data"] = GetPerformanceSummary();
                        break;
                    case "all":
                    default:
                        result["data"] = GetAllProfilerData();
                        break;
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCP Unity] Error fetching profiler data: {ex.Message}");
                return new JObject
                {
                    ["success"] = false,
                    ["message"] = $"Failed to fetch profiler data: {ex.Message}",
                    ["error"] = "profiler_error"
                };
            }
        }

        /// <summary>
        /// Get CPU performance data
        /// </summary>
        private JObject GetCPUData()
        {
            return new JObject
            {
                ["mainThreadFrameTime"] = GetRecorderAverage("MainThreadFrameTime"),
                ["cpuTotalFrameTime"] = GetRecorderAverage("CPUTotalFrameTime"),
                ["cpuRenderThreadFrameTime"] = GetRecorderAverage("CPURenderThreadFrameTime"),
                ["behaviourUpdate"] = GetRecorderAverage("BehaviourUpdate"),
                ["lateBehaviourUpdate"] = GetRecorderAverage("LateBehaviourUpdate"),
                ["fixedBehaviourUpdate"] = GetRecorderAverage("FixedBehaviourUpdate"),
                ["physicsFixedUpdate"] = GetRecorderAverage("PhysicsFixedUpdate"),
                ["fps"] = Time.frameCount > 0 ? 1.0f / Time.unscaledDeltaTime : 0f
            };
        }

        /// <summary>
        /// Get memory performance data
        /// </summary>
        private JObject GetMemoryData()
        {
            return new JObject
            {
                ["systemUsedMemory"] = GetRecorderValue("SystemUsedMemory") / (1024 * 1024), // MB
                ["gcReservedMemory"] = GetRecorderValue("GCReservedMemory") / (1024 * 1024), // MB
                ["gcUsedMemory"] = GetRecorderValue("GCUsedMemory") / (1024 * 1024), // MB
                ["totalAllocatedMemory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024), // MB
                ["totalReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024), // MB
                ["totalUnusedReservedMemory"] = UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong() / (1024 * 1024) // MB
            };
        }

        /// <summary>
        /// Get GPU performance data
        /// </summary>
        private JObject GetGPUData()
        {
            return new JObject
            {
                ["gpuFrameTime"] = GetRecorderAverage("GPUFrameTime")
            };
        }

        /// <summary>
        /// Get rendering performance data
        /// </summary>
        private JObject GetRenderingData()
        {
            JObject renderData = new JObject();
            
#if UNITY_EDITOR
            renderData["drawCalls"] = GetRecorderValue("DrawCallsCount");
            renderData["batches"] = GetRecorderValue("BatchesCount");
            renderData["setPassCalls"] = GetRecorderValue("SetPassCallsCount");
            renderData["triangles"] = GetRecorderValue("TrianglesCount");
            renderData["vertices"] = GetRecorderValue("VerticesCount");
#else
            renderData["note"] = "Rendering metrics are only available in Unity Editor";
#endif
            
            return renderData;
        }

        /// <summary>
        /// Get CPU call hierarchy data for the current frame
        /// </summary>
        private JObject GetHierarchyData()
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
                            hierarchyData["frameIndex"] = currentFrame;
                            hierarchyData["threadName"] = frameData.threadName;
                            hierarchyData["frameTimeMs"] = frameData.frameTimeMs;
                            
                            JArray topCalls = new JArray();
                            int rootId = frameData.GetRootItemID();
                            
                            _parentsCacheList.Clear();
                            frameData.GetItemDescendantsThatHaveChildren(rootId, _parentsCacheList);
                            
                            foreach (int parentId in _parentsCacheList)
                            {
                                _childrenCacheList.Clear();
                                frameData.GetItemChildren(parentId, _childrenCacheList);
                                
                                if (_childrenCacheList.Count > 0)
                                {
                                    JObject callInfo = new JObject
                                    {
                                        ["name"] = frameData.GetItemName(parentId),
                                        ["totalTimeMs"] = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnTotalTime),
                                        ["selfTimeMs"] = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnSelfTime),
                                        ["calls"] = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnCalls),
                                        ["gcMemory"] = frameData.GetItemColumnDataAsFloat(parentId, HierarchyFrameDataView.columnGcMemory),
                                        ["childrenCount"] = _childrenCacheList.Count
                                    };
                                    
                                    topCalls.Add(callInfo);
                                }
                            }
                            
                            // Sort by total time descending and take top 10
                            var sortedCalls = new List<JObject>();
                            foreach (JObject call in topCalls)
                            {
                                sortedCalls.Add(call);
                            }
                            sortedCalls.Sort((a, b) => 
                                b["totalTimeMs"].ToObject<float>().CompareTo(a["totalTimeMs"].ToObject<float>()));
                            
                            JArray top10Calls = new JArray();
                            for (int i = 0; i < Math.Min(10, sortedCalls.Count); i++)
                            {
                                top10Calls.Add(sortedCalls[i]);
                            }
                            
                            hierarchyData["topCostlyMethods"] = top10Calls;
                        }
                        else
                        {
                            hierarchyData["error"] = "Frame data not valid";
                        }
                    }
                }
                else
                {
                    hierarchyData["error"] = "No valid frame data available";
                }
            }
            catch (Exception ex)
            {
                hierarchyData["error"] = $"Failed to get hierarchy data: {ex.Message}";
            }
#else
            hierarchyData["note"] = "CPU call hierarchy is only available in Unity Editor";
#endif
            
            return hierarchyData;
        }

        /// <summary>
        /// Get a performance summary with key metrics and recommendations
        /// </summary>
        private JObject GetPerformanceSummary()
        {
            JObject summary = new JObject();
            
            float mainThreadTime = GetRecorderAverage("MainThreadFrameTime");
            float gpuTime = GetRecorderAverage("GPUFrameTime");
            float currentFPS = Time.frameCount > 0 ? 1.0f / Time.unscaledDeltaTime : 0f;
            long gcMemoryMB = GetRecorderValue("GCReservedMemory") / (1024 * 1024);
            
            summary["currentFPS"] = currentFPS;
            summary["mainThreadTimeMs"] = mainThreadTime;
            summary["gpuTimeMs"] = gpuTime;
            summary["gcMemoryMB"] = gcMemoryMB;
            
            // Performance assessment
            JArray issues = new JArray();
            JArray recommendations = new JArray();
            
            if (currentFPS < 30)
            {
                issues.Add("Low FPS detected");
                recommendations.Add("Consider optimizing CPU/GPU performance");
            }
            
            if (mainThreadTime > 16.67f) // 60 FPS target
            {
                issues.Add("High CPU frame time");
                recommendations.Add("Profile CPU usage and optimize expensive operations");
            }
            
            if (gpuTime > 16.67f)
            {
                issues.Add("High GPU frame time");
                recommendations.Add("Optimize rendering, reduce draw calls or shader complexity");
            }
            
            if (gcMemoryMB > 100)
            {
                issues.Add("High GC memory usage");
                recommendations.Add("Reduce managed allocations and implement object pooling");
            }
            
            summary["issues"] = issues;
            summary["recommendations"] = recommendations;
            summary["performanceGrade"] = GetPerformanceGrade(currentFPS, mainThreadTime, gpuTime);
            
            return summary;
        }

        /// <summary>
        /// Get all profiler data in a comprehensive format
        /// </summary>
        private JObject GetAllProfilerData()
        {
            return new JObject
            {
                ["cpu"] = GetCPUData(),
                ["memory"] = GetMemoryData(),
                ["gpu"] = GetGPUData(),
                ["rendering"] = GetRenderingData(),
                ["summary"] = GetPerformanceSummary()
            };
        }

        /// <summary>
        /// Get average value from a ProfilerRecorder
        /// </summary>
        private float GetRecorderAverage(string recorderName)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0f;

            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0f;

            double sum = 0;
            var samples = new List<ProfilerRecorderSample>(samplesCount);
            recorder.CopyTo(samples);
            for (var i = 0; i < samples.Count; ++i)
                sum += samples[i].Value;

            return (float)(sum / samples.Count / 1000000.0); // Convert to milliseconds
        }

        /// <summary>
        /// Get current value from a ProfilerRecorder
        /// </summary>
        private long GetRecorderValue(string recorderName)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0L;

            return recorder.LastValue;
        }

        /// <summary>
        /// Calculate performance grade based on key metrics
        /// </summary>
        private string GetPerformanceGrade(float fps, float cpuTime, float gpuTime)
        {
            if (fps >= 60 && cpuTime <= 16.67f && gpuTime <= 16.67f)
                return "A"; // Excellent
            else if (fps >= 45 && cpuTime <= 22.22f && gpuTime <= 22.22f)
                return "B"; // Good
            else if (fps >= 30 && cpuTime <= 33.33f && gpuTime <= 33.33f)
                return "C"; // Fair
            else if (fps >= 20)
                return "D"; // Poor
            else
                return "F"; // Very Poor
        }

        /// <summary>
        /// Cleanup ProfilerRecorders when object is disposed
        /// </summary>
        ~GetProfilerResource()
        {
            foreach (var recorder in _profilerRecorders.Values)
            {
                if (recorder.Valid)
                    recorder.Dispose();
            }
        }
    }
} 