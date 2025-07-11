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
using McpUnity.Utils;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for detailed module-specific analysis of Unity performance data
    /// </summary>
    public class AnalyzeModuleTool : McpToolBase
    {
        private readonly Dictionary<string, ProfilerRecorder> _profilerRecorders = new Dictionary<string, ProfilerRecorder>();
        
        public AnalyzeModuleTool()
        {
            Name = "analyze_module";
            Description = "对Unity性能数据进行细化模块分析，包括内存、渲染、CPU、GPU、网络、代码等各个模块的深度性能分析";
            IsAsync = false;
            
            InitializeProfilerRecorders();
        }

        /// <summary>
        /// Execute the module analysis tool
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            try
            {
                // Extract parameters
                string moduleType = parameters?["moduleType"]?.ToObject<string>()?.ToLower() ?? "memory";
                int? frameIndex = parameters?["frameIndex"]?.ToObject<int?>();
                string analysisDepth = parameters?["analysisDepth"]?.ToObject<string>()?.ToLower() ?? "detailed";
                bool includeOptimizations = parameters?["includeOptimizations"]?.ToObject<bool>() ?? true;

                McpLogger.LogInfo($"开始{moduleType}模块的{analysisDepth}分析...");

                JObject result = new JObject
                {
                    ["success"] = true,
                    ["moduleType"] = moduleType,
                    ["analysisDepth"] = analysisDepth,
                    ["frameIndex"] = frameIndex,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };

                // Perform module-specific analysis
                switch (moduleType)
                {
                    case "memory":
                        result["analysis"] = AnalyzeMemoryModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "rendering":
                        result["analysis"] = AnalyzeRenderingModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "cpu":
                        result["analysis"] = AnalyzeCPUModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "gpu":
                        result["analysis"] = AnalyzeGPUModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "network":
                        result["analysis"] = AnalyzeNetworkModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "code":
                        result["analysis"] = AnalyzeCodeModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "physics":
                        result["analysis"] = AnalyzePhysicsModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    case "audio":
                        result["analysis"] = AnalyzeAudioModule(frameIndex, analysisDepth, includeOptimizations);
                        break;
                    default:
                        return McpUnitySocketHandler.CreateErrorResponse(
                            $"不支持的模块类型: {moduleType}。支持的类型: memory, rendering, cpu, gpu, network, code, physics, audio", 
                            "invalid_module_type"
                        );
                }

                McpLogger.LogInfo($"{moduleType}模块分析完成");
                return result;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"模块分析时发生错误: {ex.Message}");
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"模块分析失败: {ex.Message}", 
                    "analysis_error"
                );
            }
        }

        /// <summary>
        /// Initialize ProfilerRecorders for module analysis
        /// </summary>
        private void InitializeProfilerRecorders()
        {
            try
            {
                // Memory recorders
                _profilerRecorders["SystemUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
                _profilerRecorders["GCReservedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
                _profilerRecorders["GCUsedMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
                _profilerRecorders["TextureMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Texture Memory");
                _profilerRecorders["MeshMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Mesh Memory");
                _profilerRecorders["AudioMemory"] = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Audio Memory");
                
                // CPU recorders
                _profilerRecorders["MainThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 30);
                _profilerRecorders["CPURenderThreadFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Render Thread Frame Time", 30);
                _profilerRecorders["BehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "BehaviourUpdate", 30);
                _profilerRecorders["LateBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "LateBehaviourUpdate", 30);
                _profilerRecorders["FixedBehaviourUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedBehaviourUpdate", 30);
                
                // GPU recorders
                _profilerRecorders["GPUFrameTime"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "GPU Frame Time", 30);
                
                // Rendering recorders (Editor only)
#if UNITY_EDITOR
                _profilerRecorders["DrawCallsCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                _profilerRecorders["BatchesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
                _profilerRecorders["SetPassCallsCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                _profilerRecorders["TrianglesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                _profilerRecorders["VerticesCount"] = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
#endif
                
                // Physics recorders
                _profilerRecorders["PhysicsFixedUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "FixedUpdate.PhysicsFixedUpdate", 30);
                _profilerRecorders["PhysicsProcessReports"] = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Physics.Processing", 30);
                
                // Audio recorders
                _profilerRecorders["AudioUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Audio, "Audio.Update", 30);
                _profilerRecorders["AudioMixerUpdate"] = ProfilerRecorder.StartNew(ProfilerCategory.Audio, "AudioMixer.Update", 30);
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"初始化性能记录器时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// Deep analysis of memory performance and usage patterns
        /// </summary>
        private JObject AnalyzeMemoryModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject memoryAnalysis = new JObject
            {
                ["analysisType"] = "memory_deep_dive",
                ["memoryBreakdown"] = GetMemoryBreakdown(),
                ["gcAnalysis"] = AnalyzeGarbageCollection(frameIndex),
                ["allocationSources"] = GetAllocationSources(frameIndex),
                ["memoryTrends"] = GetMemoryTrends(),
                ["textureAnalysis"] = AnalyzeTextureMemory(),
                ["meshAnalysis"] = AnalyzeMeshMemory(),
                ["audioMemoryAnalysis"] = AnalyzeAudioMemory()
            };

            if (includeOptimizations)
            {
                memoryAnalysis["optimizationRecommendations"] = GenerateMemoryOptimizations(memoryAnalysis);
                memoryAnalysis["memoryGrade"] = CalculateMemoryGrade(memoryAnalysis);
            }

            return memoryAnalysis;
        }

        /// <summary>
        /// Detailed analysis of rendering performance and bottlenecks
        /// </summary>
        private JObject AnalyzeRenderingModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject renderingAnalysis = new JObject
            {
                ["analysisType"] = "rendering_deep_dive",
                ["drawCallAnalysis"] = AnalyzeDrawCalls(frameIndex),
                ["batchingAnalysis"] = AnalyzeBatching(),
                ["shaderAnalysis"] = AnalyzeShaderPerformance(frameIndex),
                ["lightingAnalysis"] = AnalyzeLightingPerformance(frameIndex),
                ["shadowAnalysis"] = AnalyzeShadowPerformance(frameIndex),
                ["cullingAnalysis"] = AnalyzeCullingEfficiency(),
                ["overdrawAnalysis"] = AnalyzeOverdraw(frameIndex)
            };

            if (includeOptimizations)
            {
                renderingAnalysis["optimizationRecommendations"] = GenerateRenderingOptimizations(renderingAnalysis);
                renderingAnalysis["renderingGrade"] = CalculateRenderingGrade(renderingAnalysis);
            }

            return renderingAnalysis;
        }

        /// <summary>
        /// Deep CPU performance analysis with profiling data
        /// </summary>
        private JObject AnalyzeCPUModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject cpuAnalysis = new JObject
            {
                ["analysisType"] = "cpu_deep_dive",
                ["threadAnalysis"] = AnalyzeThreadPerformance(frameIndex),
                ["updateMethodAnalysis"] = AnalyzeUpdateMethods(frameIndex),
                ["scriptPerformance"] = AnalyzeScriptPerformance(frameIndex),
                ["callStackAnalysis"] = GetDetailedCallStack(frameIndex),
                ["hotSpotAnalysis"] = IdentifyCPUHotSpots(frameIndex),
                ["concurrencyAnalysis"] = AnalyzeConcurrency()
            };

            if (includeOptimizations)
            {
                cpuAnalysis["optimizationRecommendations"] = GenerateCPUOptimizations(cpuAnalysis);
                cpuAnalysis["cpuGrade"] = CalculateCPUGrade(cpuAnalysis);
            }

            return cpuAnalysis;
        }

        /// <summary>
        /// GPU performance analysis including shader and fill rate analysis
        /// </summary>
        private JObject AnalyzeGPUModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject gpuAnalysis = new JObject
            {
                ["analysisType"] = "gpu_deep_dive",
                ["gpuUtilization"] = AnalyzeGPUUtilization(frameIndex),
                ["shaderComplexity"] = AnalyzeShaderComplexity(),
                ["fillRateAnalysis"] = AnalyzeFillRate(frameIndex),
                ["vertexProcessing"] = AnalyzeVertexProcessing(frameIndex),
                ["textureStreamingAnalysis"] = AnalyzeTextureStreaming(),
                ["gpuMemoryAnalysis"] = AnalyzeGPUMemory()
            };

            if (includeOptimizations)
            {
                gpuAnalysis["optimizationRecommendations"] = GenerateGPUOptimizations(gpuAnalysis);
                gpuAnalysis["gpuGrade"] = CalculateGPUGrade(gpuAnalysis);
            }

            return gpuAnalysis;
        }

        /// <summary>
        /// Network performance analysis
        /// </summary>
        private JObject AnalyzeNetworkModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject networkAnalysis = new JObject
            {
                ["analysisType"] = "network_analysis",
                ["networkCalls"] = GetNetworkCallsAnalysis(),
                ["latencyAnalysis"] = AnalyzeNetworkLatency(),
                ["bandwidthUsage"] = AnalyzeBandwidthUsage(),
                ["note"] = "网络分析需要运行时数据，在编辑器中可能信息有限"
            };

            if (includeOptimizations)
            {
                networkAnalysis["optimizationRecommendations"] = GenerateNetworkOptimizations(networkAnalysis);
            }

            return networkAnalysis;
        }

        /// <summary>
        /// Code-specific performance analysis
        /// </summary>
        private JObject AnalyzeCodeModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject codeAnalysis = new JObject
            {
                ["analysisType"] = "code_analysis",
                ["scriptAnalysis"] = AnalyzeActiveScripts(),
                ["componentAnalysis"] = AnalyzeComponentPerformance(),
                ["coroutineAnalysis"] = AnalyzeCoroutines(),
                ["eventSystemAnalysis"] = AnalyzeEventSystem()
            };

            if (includeOptimizations)
            {
                codeAnalysis["optimizationRecommendations"] = GenerateCodeOptimizations(codeAnalysis);
                codeAnalysis["codeGrade"] = CalculateCodeGrade(codeAnalysis);
            }

            return codeAnalysis;
        }

        /// <summary>
        /// Physics performance analysis
        /// </summary>
        private JObject AnalyzePhysicsModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject physicsAnalysis = new JObject
            {
                ["analysisType"] = "physics_analysis",
                ["physicsTime"] = GetRecorderAverage("PhysicsFixedUpdate"),
                ["rigidbodyCount"] = GetActiveRigidbodyCount(),
                ["colliderAnalysis"] = AnalyzeColliders(),
                ["raycastAnalysis"] = AnalyzeRaycasts(),
                ["jointAnalysis"] = AnalyzeJoints()
            };

            if (includeOptimizations)
            {
                physicsAnalysis["optimizationRecommendations"] = GeneratePhysicsOptimizations(physicsAnalysis);
                physicsAnalysis["physicsGrade"] = CalculatePhysicsGrade(physicsAnalysis);
            }

            return physicsAnalysis;
        }

        /// <summary>
        /// Audio performance analysis
        /// </summary>
        private JObject AnalyzeAudioModule(int? frameIndex, string analysisDepth, bool includeOptimizations)
        {
            JObject audioAnalysis = new JObject
            {
                ["analysisType"] = "audio_analysis",
                ["audioMemory"] = GetRecorderValue("AudioMemory") / (1024 * 1024), // MB
                ["audioUpdateTime"] = GetRecorderAverage("AudioUpdate"),
                ["audioSourcesAnalysis"] = AnalyzeAudioSources(),
                ["compressionAnalysis"] = AnalyzeAudioCompression()
            };

            if (includeOptimizations)
            {
                audioAnalysis["optimizationRecommendations"] = GenerateAudioOptimizations(audioAnalysis);
                audioAnalysis["audioGrade"] = CalculateAudioGrade(audioAnalysis);
            }

            return audioAnalysis;
        }

        #region Helper Methods for Memory Analysis
        
        private JObject GetMemoryBreakdown()
        {
            return new JObject
            {
                ["systemUsedMemory"] = GetRecorderValue("SystemUsedMemory") / (1024 * 1024), // MB
                ["gcReservedMemory"] = GetRecorderValue("GCReservedMemory") / (1024 * 1024), // MB
                ["gcUsedMemory"] = GetRecorderValue("GCUsedMemory") / (1024 * 1024), // MB
                ["textureMemory"] = GetRecorderValue("TextureMemory") / (1024 * 1024), // MB
                ["meshMemory"] = GetRecorderValue("MeshMemory") / (1024 * 1024), // MB
                ["audioMemory"] = GetRecorderValue("AudioMemory") / (1024 * 1024), // MB
                ["totalAllocatedMemory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024) // MB
            };
        }

        private JObject AnalyzeGarbageCollection(int? frameIndex)
        {
            JObject gcAnalysis = new JObject
            {
                ["gcReservedMB"] = GetRecorderValue("GCReservedMemory") / (1024 * 1024),
                ["gcUsedMB"] = GetRecorderValue("GCUsedMemory") / (1024 * 1024),
                ["gcEfficiency"] = CalculateGCEfficiency(),
                ["gcPressure"] = CalculateGCPressure()
            };

            if (frameIndex.HasValue)
            {
                gcAnalysis["frameGCAllocation"] = GetFrameGCAllocation(frameIndex.Value);
            }

            return gcAnalysis;
        }

        private JObject GetAllocationSources(int? frameIndex)
        {
            // This would require detailed profiler data analysis
            return new JObject
            {
                ["note"] = "内存分配源分析需要在Profiler窗口中查看详细数据",
                ["commonSources"] = new JArray
                {
                    "字符串连接和格式化",
                    "临时数组和集合分配",
                    "装箱操作",
                    "LINQ操作",
                    "Unity API调用产生的临时对象"
                }
            };
        }

        private float CalculateGCEfficiency()
        {
            long gcReserved = GetRecorderValue("GCReservedMemory");
            long gcUsed = GetRecorderValue("GCUsedMemory");
            
            if (gcReserved == 0) return 1.0f;
            return (float)gcUsed / gcReserved;
        }

        private string CalculateGCPressure()
        {
            long gcReservedMB = GetRecorderValue("GCReservedMemory") / (1024 * 1024);
            
            if (gcReservedMB < 50) return "低";
            else if (gcReservedMB < 100) return "中等";
            else if (gcReservedMB < 200) return "高";
            else return "极高";
        }

        #endregion

        #region Helper Methods for Rendering Analysis

        private JObject AnalyzeDrawCalls(int? frameIndex)
        {
            return new JObject
            {
                ["drawCallsCount"] = GetRecorderValue("DrawCallsCount"),
                ["batchesCount"] = GetRecorderValue("BatchesCount"),
                ["setPassCallsCount"] = GetRecorderValue("SetPassCallsCount"),
                ["batchingEfficiency"] = CalculateBatchingEfficiency()
            };
        }

        private JObject AnalyzeBatching()
        {
            long drawCalls = GetRecorderValue("DrawCallsCount");
            long batches = GetRecorderValue("BatchesCount");
            
            return new JObject
            {
                ["staticBatching"] = "需要检查静态批处理设置",
                ["dynamicBatching"] = "需要检查动态批处理设置",
                ["gpuInstancing"] = "检查GPU实例化使用情况",
                ["srp"] = "检查SRP批处理器使用情况",
                ["batchingRatio"] = drawCalls > 0 ? (float)batches / drawCalls : 1.0f
            };
        }

        private float CalculateBatchingEfficiency()
        {
            long drawCalls = GetRecorderValue("DrawCallsCount");
            long batches = GetRecorderValue("BatchesCount");
            
            if (drawCalls == 0) return 1.0f;
            return 1.0f - ((float)batches / drawCalls);
        }

        #endregion

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

            return (float)(sum / samplesCount);
        }

        private long GetRecorderValue(string recorderName)
        {
            if (!_profilerRecorders.TryGetValue(recorderName, out var recorder) || !recorder.Valid)
                return 0;

            return recorder.LastValue;
        }

        #endregion

        #region Stub Methods (To be implemented based on specific needs)

        private JObject GetMemoryTrends() => new JObject { ["note"] = "内存趋势分析需要长期数据收集" };
        private JObject AnalyzeTextureMemory() => new JObject { ["textureMemoryMB"] = GetRecorderValue("TextureMemory") / (1024 * 1024) };
        private JObject AnalyzeMeshMemory() => new JObject { ["meshMemoryMB"] = GetRecorderValue("MeshMemory") / (1024 * 1024) };
        private JObject AnalyzeAudioMemory() => new JObject { ["audioMemoryMB"] = GetRecorderValue("AudioMemory") / (1024 * 1024) };
        private JObject AnalyzeShaderPerformance(int? frameIndex) => new JObject { ["note"] = "着色器性能分析需要GPU Profiler数据" };
        private JObject AnalyzeLightingPerformance(int? frameIndex) => new JObject { ["note"] = "光照性能需要在Profiler中查看Lighting模块" };
        private JObject AnalyzeShadowPerformance(int? frameIndex) => new JObject { ["note"] = "阴影性能需要在Profiler中查看Shadow模块" };
        private JObject AnalyzeCullingEfficiency() => new JObject { ["note"] = "剔除效率分析需要Profiler数据" };
        private JObject AnalyzeOverdraw(int? frameIndex) => new JObject { ["note"] = "过度绘制分析需要Frame Debugger" };
        private JObject AnalyzeThreadPerformance(int? frameIndex) => new JObject { ["mainThreadTime"] = GetRecorderAverage("MainThreadFrameTime") };
        private JObject AnalyzeUpdateMethods(int? frameIndex) => new JObject { ["updateTime"] = GetRecorderAverage("BehaviourUpdate") };
        private JObject AnalyzeScriptPerformance(int? frameIndex) => new JObject { ["note"] = "脚本性能需要Deep Profiling" };
        private JObject GetDetailedCallStack(int? frameIndex) => new JObject { ["note"] = "详细调用栈需要Profiler窗口数据" };
        private JObject IdentifyCPUHotSpots(int? frameIndex) => new JObject { ["note"] = "CPU热点识别需要Deep Profiling" };
        private JObject AnalyzeConcurrency() => new JObject { ["note"] = "并发分析需要多线程Profiler数据" };
        private JObject AnalyzeGPUUtilization(int? frameIndex) => new JObject { ["gpuTime"] = GetRecorderAverage("GPUFrameTime") };
        private JObject AnalyzeShaderComplexity() => new JObject { ["note"] = "着色器复杂度需要Shader Profiler" };
        private JObject AnalyzeFillRate(int? frameIndex) => new JObject { ["note"] = "填充率分析需要GPU Profiler" };
        private JObject AnalyzeVertexProcessing(int? frameIndex) => new JObject { ["verticesCount"] = GetRecorderValue("VerticesCount") };
        private JObject AnalyzeTextureStreaming() => new JObject { ["note"] = "纹理流送分析需要运行时数据" };
        private JObject AnalyzeGPUMemory() => new JObject { ["note"] = "GPU内存分析需要平台特定工具" };
        private JObject GetNetworkCallsAnalysis() => new JObject { ["note"] = "网络调用分析需要自定义网络Profiler" };
        private JObject AnalyzeNetworkLatency() => new JObject { ["note"] = "网络延迟分析需要运行时测量" };
        private JObject AnalyzeBandwidthUsage() => new JObject { ["note"] = "带宽使用分析需要网络监控工具" };
        private JObject AnalyzeActiveScripts() => new JObject { ["note"] = "活动脚本分析需要场景对象检查" };
        private JObject AnalyzeComponentPerformance() => new JObject { ["note"] = "组件性能需要Deep Profiling" };
        private JObject AnalyzeCoroutines() => new JObject { ["note"] = "协程分析需要特定的性能监控" };
        private JObject AnalyzeEventSystem() => new JObject { ["note"] = "事件系统分析需要UI系统检查" };
        private int GetActiveRigidbodyCount() => UnityEngine.Object.FindObjectsOfType<Rigidbody>().Length;
        private JObject AnalyzeColliders() => new JObject { ["colliderCount"] = UnityEngine.Object.FindObjectsOfType<Collider>().Length };
        private JObject AnalyzeRaycasts() => new JObject { ["note"] = "射线检测分析需要Physics Profiler" };
        private JObject AnalyzeJoints() => new JObject { ["jointCount"] = UnityEngine.Object.FindObjectsOfType<Joint>().Length };
        private JObject AnalyzeAudioSources() => new JObject { ["audioSourceCount"] = UnityEngine.Object.FindObjectsOfType<AudioSource>().Length };
        private JObject AnalyzeAudioCompression() => new JObject { ["note"] = "音频压缩分析需要Asset分析" };
        private long GetFrameGCAllocation(int frameIndex) => 0; // Placeholder

        #endregion

        #region Optimization Recommendations

        private JArray GenerateMemoryOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            long gcReservedMB = analysis["gcAnalysis"]?["gcReservedMB"]?.ToObject<long>() ?? 0;
            if (gcReservedMB > 100)
            {
                recommendations.Add("减少GC分配：使用对象池，避免频繁创建临时对象");
            }
            
            float gcEfficiency = analysis["gcAnalysis"]?["gcEfficiency"]?.ToObject<float>() ?? 1.0f;
            if (gcEfficiency < 0.7f)
            {
                recommendations.Add("优化内存使用：清理未使用的引用，减少内存碎片");
            }
            
            return recommendations;
        }

        private JArray GenerateRenderingOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            long drawCalls = analysis["drawCallAnalysis"]?["drawCallsCount"]?.ToObject<long>() ?? 0;
            if (drawCalls > 1000)
            {
                recommendations.Add("减少Draw Calls：使用批处理、合并网格、优化材质使用");
            }
            
            float batchingEfficiency = analysis["drawCallAnalysis"]?["batchingEfficiency"]?.ToObject<float>() ?? 1.0f;
            if (batchingEfficiency < 0.5f)
            {
                recommendations.Add("改善批处理：启用静态批处理，使用GPU实例化，优化材质设置");
            }
            
            return recommendations;
        }

        private JArray GenerateCPUOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            float mainThreadTime = analysis["threadAnalysis"]?["mainThreadTime"]?.ToObject<float>() ?? 0f;
            if (mainThreadTime > 16.67f)
            {
                recommendations.Add("优化主线程：减少Update频率，使用协程，优化算法复杂度");
            }
            
            return recommendations;
        }

        private JArray GenerateGPUOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            float gpuTime = analysis["gpuUtilization"]?["gpuTime"]?.ToObject<float>() ?? 0f;
            if (gpuTime > 16.67f)
            {
                recommendations.Add("优化GPU性能：简化着色器，减少纹理分辨率，使用LOD系统");
            }
            
            return recommendations;
        }

        private JArray GenerateNetworkOptimizations(JObject analysis)
        {
            return new JArray { "优化网络：减少网络调用频率，压缩数据，使用连接池" };
        }

        private JArray GenerateCodeOptimizations(JObject analysis)
        {
            return new JArray { "优化代码：减少Update中的计算，使用事件驱动架构，避免频繁的查找操作" };
        }

        private JArray GeneratePhysicsOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            int rigidbodyCount = analysis["rigidbodyCount"]?.ToObject<int>() ?? 0;
            if (rigidbodyCount > 100)
            {
                recommendations.Add("优化物理：减少Rigidbody数量，简化碰撞体，调整物理更新频率");
            }
            
            return recommendations;
        }

        private JArray GenerateAudioOptimizations(JObject analysis)
        {
            JArray recommendations = new JArray();
            
            float audioMemoryMB = analysis["audioMemory"]?.ToObject<float>() ?? 0f;
            if (audioMemoryMB > 50)
            {
                recommendations.Add("优化音频：压缩音频文件，使用流式加载，减少同时播放的音源数量");
            }
            
            return recommendations;
        }

        #endregion

        #region Grade Calculations

        private string CalculateMemoryGrade(JObject analysis)
        {
            long gcReservedMB = analysis["gcAnalysis"]?["gcReservedMB"]?.ToObject<long>() ?? 0;
            float gcEfficiency = analysis["gcAnalysis"]?["gcEfficiency"]?.ToObject<float>() ?? 1.0f;
            
            if (gcReservedMB < 50 && gcEfficiency > 0.8f) return "A";
            else if (gcReservedMB < 100 && gcEfficiency > 0.6f) return "B";
            else if (gcReservedMB < 200) return "C";
            else return "D";
        }

        private string CalculateRenderingGrade(JObject analysis)
        {
            long drawCalls = analysis["drawCallAnalysis"]?["drawCallsCount"]?.ToObject<long>() ?? 0;
            float batchingEfficiency = analysis["drawCallAnalysis"]?["batchingEfficiency"]?.ToObject<float>() ?? 1.0f;
            
            if (drawCalls < 500 && batchingEfficiency > 0.7f) return "A";
            else if (drawCalls < 1000 && batchingEfficiency > 0.5f) return "B";
            else if (drawCalls < 2000) return "C";
            else return "D";
        }

        private string CalculateCPUGrade(JObject analysis)
        {
            float mainThreadTime = analysis["threadAnalysis"]?["mainThreadTime"]?.ToObject<float>() ?? 0f;
            
            if (mainThreadTime <= 8.33f) return "A"; // 120+ FPS
            else if (mainThreadTime <= 16.67f) return "B"; // 60+ FPS
            else if (mainThreadTime <= 33.33f) return "C"; // 30+ FPS
            else return "D";
        }

        private string CalculateGPUGrade(JObject analysis)
        {
            float gpuTime = analysis["gpuUtilization"]?["gpuTime"]?.ToObject<float>() ?? 0f;
            
            if (gpuTime <= 8.33f) return "A"; // 120+ FPS
            else if (gpuTime <= 16.67f) return "B"; // 60+ FPS
            else if (gpuTime <= 33.33f) return "C"; // 30+ FPS
            else return "D";
        }

        private string CalculateCodeGrade(JObject analysis)
        {
            // Placeholder grading based on general metrics
            return "B"; // Default grade for code analysis
        }

        private string CalculatePhysicsGrade(JObject analysis)
        {
            float physicsTime = analysis["physicsTime"]?.ToObject<float>() ?? 0f;
            int rigidbodyCount = analysis["rigidbodyCount"]?.ToObject<int>() ?? 0;
            
            if (physicsTime < 2.0f && rigidbodyCount < 50) return "A";
            else if (physicsTime < 5.0f && rigidbodyCount < 100) return "B";
            else if (physicsTime < 10.0f) return "C";
            else return "D";
        }

        private string CalculateAudioGrade(JObject analysis)
        {
            float audioMemoryMB = analysis["audioMemory"]?.ToObject<float>() ?? 0f;
            float audioUpdateTime = analysis["audioUpdateTime"]?.ToObject<float>() ?? 0f;
            
            if (audioMemoryMB < 20 && audioUpdateTime < 1.0f) return "A";
            else if (audioMemoryMB < 50 && audioUpdateTime < 2.0f) return "B";
            else if (audioMemoryMB < 100) return "C";
            else return "D";
        }

        #endregion

        /// <summary>
        /// Dispose of ProfilerRecorders when the tool is destroyed
        /// </summary>
        ~AnalyzeModuleTool()
        {
            foreach (var recorder in _profilerRecorders.Values)
            {
                if (recorder.Valid)
                    recorder.Dispose();
            }
        }
    }
} 