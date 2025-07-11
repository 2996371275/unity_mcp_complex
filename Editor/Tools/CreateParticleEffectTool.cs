using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using McpUnity.Models;
using McpUnity.Utils;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// 工具用于根据自然语言描述创建粒子效果
    /// 支持智能参数配置和层级管理
    /// </summary>
    public class CreateParticleEffectTool : McpToolBase
    {
        public CreateParticleEffectTool()
        {
            Name = "create_particle_effect";
            Description = "根据自然语言描述创建粒子效果，自动配置参数和管理层级";
        }

        /// <summary>
        /// 粒子效果预设配置
        /// </summary>
        private static readonly Dictionary<string, ParticleEffectConfig> EffectPresets = new Dictionary<string, ParticleEffectConfig>
        {
            // 天气效果
            ["下雪"] = new ParticleEffectConfig
            {
                EffectName = "Snow Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 8f,
                    StartSpeed = 2f,
                    StartSize = 0.1f,
                    StartColor = Color.white,
                    MaxParticles = 500,
                    RateOverTime = 50f
                },
                Shape = ParticleSystemShapeType.Box,
                ShapeRadius = new Vector3(20f, 0.1f, 20f),
                VelocityOverLifetime = new Vector3(0f, -1f, 0f),
                SizeOverLifetime = AnimationCurve.Constant(0f, 1f, 1f),
                ColorOverLifetime = AnimationCurve.Constant(0f, 1f, 1f),
                TextureSheetAnimation = false,
                Collision = true
            },
            
            ["下雨"] = new ParticleEffectConfig
            {
                EffectName = "Rain Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 2f,
                    StartSpeed = 15f,
                    StartSize = 0.05f,
                    StartColor = new Color(0.6f, 0.8f, 1f, 0.8f),
                    MaxParticles = 1000,
                    RateOverTime = 300f
                },
                Shape = ParticleSystemShapeType.Box,
                ShapeRadius = new Vector3(30f, 0.1f, 30f),
                VelocityOverLifetime = new Vector3(0f, -20f, 0f),
                SizeOverLifetime = AnimationCurve.Constant(0f, 1f, 1f),
                ColorOverLifetime = AnimationCurve.Constant(0f, 1f, 1f),
                TextureSheetAnimation = false,
                Collision = true
            },

            ["火焰"] = new ParticleEffectConfig
            {
                EffectName = "Fire Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 2f,
                    StartSpeed = 3f,
                    StartSize = 0.5f,
                    StartColor = Color.red,
                    MaxParticles = 200,
                    RateOverTime = 100f
                },
                Shape = ParticleSystemShapeType.Circle,
                ShapeRadius = new Vector3(0.5f, 0f, 0f),
                VelocityOverLifetime = new Vector3(0f, 5f, 0f),
                SizeOverLifetime = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f)),
                ColorOverLifetime = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f)),
                TextureSheetAnimation = true,
                Collision = false
            },

            ["烟雾"] = new ParticleEffectConfig
            {
                EffectName = "Smoke Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 5f,
                    StartSpeed = 2f,
                    StartSize = 1f,
                    StartColor = new Color(0.5f, 0.5f, 0.5f, 0.3f),
                    MaxParticles = 100,
                    RateOverTime = 20f
                },
                Shape = ParticleSystemShapeType.Circle,
                ShapeRadius = new Vector3(0.3f, 0f, 0f),
                VelocityOverLifetime = new Vector3(0f, 3f, 0f),
                SizeOverLifetime = new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 2f)),
                ColorOverLifetime = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)),
                TextureSheetAnimation = false,
                Collision = false
            },

            ["爆炸"] = new ParticleEffectConfig
            {
                EffectName = "Explosion Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 1f,
                    StartSpeed = 8f,
                    StartSize = 0.3f,
                    StartColor = new Color(1f, 0.5f, 0f),
                    MaxParticles = 300,
                    RateOverTime = 0f, // 使用Burst
                    BurstCount = 100,
                    BurstTime = 0f
                },
                Shape = ParticleSystemShapeType.Sphere,
                ShapeRadius = new Vector3(0.1f, 0.1f, 0.1f),
                VelocityOverLifetime = Vector3.zero,
                SizeOverLifetime = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)),
                ColorOverLifetime = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)),
                TextureSheetAnimation = true,
                Collision = false
            },

            ["魔法光芒"] = new ParticleEffectConfig
            {
                EffectName = "Magic Sparkle Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 3f,
                    StartSpeed = 1f,
                    StartSize = 0.2f,
                    StartColor = new Color(0.5f, 0.8f, 1f),
                    MaxParticles = 150,
                    RateOverTime = 50f
                },
                Shape = ParticleSystemShapeType.Sphere,
                ShapeRadius = new Vector3(2f, 2f, 2f),
                VelocityOverLifetime = Vector3.zero,
                SizeOverLifetime = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0f)),
                ColorOverLifetime = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)),
                TextureSheetAnimation = true,
                Collision = false
            },

            ["闪电"] = new ParticleEffectConfig
            {
                EffectName = "Lightning Effect",
                MainSettings = new ParticleMainSettings
                {
                    StartLifetime = 0.8f,
                    StartSpeed = 25f,
                    StartSize = 0.3f,
                    StartColor = new Color(0.8f, 0.9f, 1f),
                    MaxParticles = 100,
                    RateOverTime = 0f, // 使用Burst
                    BurstCount = 50,
                    BurstTime = 0f
                },
                Shape = ParticleSystemShapeType.Box,
                ShapeRadius = new Vector3(0.1f, 5f, 0.1f), // 细长的Box模拟线性发射
                VelocityOverLifetime = new Vector3(0f, -10f, 0f),
                SizeOverLifetime = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.3f, 0.8f), new Keyframe(1f, 0.2f)),
                ColorOverLifetime = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 1f), new Keyframe(1f, 0f)),
                TextureSheetAnimation = false,
                Collision = false,
                UseTrails = true // 新属性，启用拖尾
            }
        };

        public override JObject Execute(JObject parameters)
        {
            try
            {
                // Extract and validate parameters
                string description = parameters["Description"]?.ToObject<string>();
                if (string.IsNullOrEmpty(description))
                {
                    return McpUnitySocketHandler.CreateErrorResponse(
                        "Required parameter 'Description' not provided", 
                        "validation_error"
                    );
                }

                // Build request object from parameters
                var request = new CreateParticleEffectRequest
                {
                    Description = description,
                    Position = ExtractVector3(parameters, "Position", Vector3.zero),
                    Scale = parameters["Scale"]?.ToObject<float>() ?? 1f,
                    AutoPlay = parameters["AutoPlay"]?.ToObject<bool>() ?? true,
                    ParentName = parameters["ParentName"]?.ToObject<string>() ?? "",
                    IntensityMultiplier = parameters["IntensityMultiplier"]?.ToObject<float>() ?? 1f
                };

                McpLogger.LogInfo($"Creating particle effect: {request.Description}");

                var result = CreateParticleEffect(request);
                
                // Convert result to JObject manually to avoid Vector3 serialization issues
                return new JObject
                {
                    ["Success"] = result.Success,
                    ["ObjectName"] = result.ObjectName,
                    ["InstanceId"] = result.InstanceId,
                    ["EffectType"] = result.EffectType,
                    ["ParticleCount"] = result.ParticleCount,
                    ["Message"] = result.Message,
                    ["SubEffectCount"] = result.SubEffectCount,
                    ["Position"] = new JObject
                    {
                        ["x"] = result.Position.x,
                        ["y"] = result.Position.y,
                        ["z"] = result.Position.z
                    }
                };
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"Error creating particle effect: {ex.Message}");
                McpLogger.LogError($"Stack trace: {ex.StackTrace}");
                return McpUnitySocketHandler.CreateErrorResponse(ex.Message, "execution_error");
            }
        }

        /// <summary>
        /// Helper method to extract Vector3 from JObject parameters
        /// </summary>
        private Vector3 ExtractVector3(JObject parameters, string key, Vector3 defaultValue)
        {
            var vectorObj = parameters[key];
            if (vectorObj == null) return defaultValue;

            try
            {
                // Handle different possible input formats
                if (vectorObj.Type == JTokenType.Object)
                {
                    float x = vectorObj["x"]?.ToObject<float>() ?? defaultValue.x;
                    float y = vectorObj["y"]?.ToObject<float>() ?? defaultValue.y;
                    float z = vectorObj["z"]?.ToObject<float>() ?? defaultValue.z;
                    return new Vector3(x, y, z);
                }
                else if (vectorObj.Type == JTokenType.Array)
                {
                    var array = vectorObj.ToObject<float[]>();
                    if (array != null && array.Length >= 3)
                    {
                        return new Vector3(array[0], array[1], array[2]);
                    }
                }
                
                McpLogger.LogWarning($"Invalid Vector3 format for parameter '{key}', using default value");
                return defaultValue;
            }
            catch (Exception ex)
            {
                McpLogger.LogWarning($"Error parsing Vector3 parameter '{key}': {ex.Message}, using default value");
                return defaultValue;
            }
        }

        /// <summary>
        /// 创建粒子效果
        /// </summary>
        private CreateParticleEffectResponse CreateParticleEffect(CreateParticleEffectRequest request)
        {
            // 解析效果描述，找到最匹配的预设
            var effectConfig = ParseEffectDescription(request.Description);
            
            // 应用强度和缩放调整
            ApplyRequestModifiers(effectConfig, request);
            
            // 创建父对象
            var parentObj = CreateParentObject(effectConfig.EffectName, request.Position, request.ParentName);
            
            // 应用缩放
            parentObj.transform.localScale = Vector3.one * request.Scale;
            
            // 创建主粒子系统
            var mainParticle = CreateMainParticleSystem(parentObj, effectConfig);
            
            // 创建子效果（如果需要）
            var subEffects = CreateSubEffects(parentObj, effectConfig, request.Description);
            
            // 如果不自动播放，停止所有粒子系统
            if (!request.AutoPlay)
            {
                mainParticle.Stop();
                foreach (var subEffect in subEffects)
                {
                    subEffect.Stop();
                }
            }
            
            // 选择创建的对象
            Selection.activeGameObject = parentObj;
            
            var response = new CreateParticleEffectResponse
            {
                Success = true,
                ObjectName = parentObj.name,
                InstanceId = parentObj.GetInstanceID(),
                EffectType = effectConfig.EffectName,
                ParticleCount = effectConfig.MainSettings.MaxParticles,
                Message = $"成功创建粒子效果: {effectConfig.EffectName}",
                SubEffectCount = subEffects.Count,
                Position = parentObj.transform.position
            };

            McpLogger.LogInfo($"Particle effect created: {effectConfig.EffectName} at {request.Position}");
            return response;
        }

        /// <summary>
        /// 应用请求中的修饰符到效果配置
        /// </summary>
        private void ApplyRequestModifiers(ParticleEffectConfig config, CreateParticleEffectRequest request)
        {
            // 应用强度倍数
            if (request.IntensityMultiplier != 1f)
            {
                config.MainSettings.RateOverTime *= request.IntensityMultiplier;
                config.MainSettings.MaxParticles = Mathf.RoundToInt(config.MainSettings.MaxParticles * request.IntensityMultiplier);
                if (config.MainSettings.BurstCount > 0)
                {
                    config.MainSettings.BurstCount = Mathf.RoundToInt(config.MainSettings.BurstCount * request.IntensityMultiplier);
                }
            }
            
            // 根据描述中的强度关键词进一步调整
            string desc = request.Description.ToLower();
            if (desc.Contains("大") || desc.Contains("暴") || desc.Contains("强烈"))
            {
                config.MainSettings.RateOverTime *= 1.5f;
                config.MainSettings.MaxParticles = Mathf.RoundToInt(config.MainSettings.MaxParticles * 1.5f);
            }
            else if (desc.Contains("小") || desc.Contains("轻微") || desc.Contains("微弱"))
            {
                config.MainSettings.RateOverTime *= 0.6f;
                config.MainSettings.MaxParticles = Mathf.RoundToInt(config.MainSettings.MaxParticles * 0.6f);
            }
            
            // 根据描述中的颜色关键词调整颜色
            if (desc.Contains("红色"))
                config.MainSettings.StartColor = Color.red;
            else if (desc.Contains("蓝色"))
                config.MainSettings.StartColor = Color.blue;
            else if (desc.Contains("绿色"))
                config.MainSettings.StartColor = Color.green;
            else if (desc.Contains("金色") || desc.Contains("黄色"))
                config.MainSettings.StartColor = Color.yellow;
            else if (desc.Contains("紫色"))
                config.MainSettings.StartColor = Color.magenta;
        }

        /// <summary>
        /// 解析效果描述，匹配预设配置
        /// </summary>
        private ParticleEffectConfig ParseEffectDescription(string description)
        {
            description = description.ToLower();
            
            // 完全匹配
            foreach (var preset in EffectPresets)
            {
                if (description.Contains(preset.Key.ToLower()))
                {
                    return preset.Value;
                }
            }
            
            // 关键词匹配
            if (description.Contains("雪") || description.Contains("snow"))
                return EffectPresets["下雪"];
            if (description.Contains("雨") || description.Contains("rain"))
                return EffectPresets["下雨"];
            if (description.Contains("火") || description.Contains("fire") || description.Contains("flame"))
                return EffectPresets["火焰"];
            if (description.Contains("烟") || description.Contains("smoke"))
                return EffectPresets["烟雾"];
            if (description.Contains("爆") || description.Contains("explosion"))
                return EffectPresets["爆炸"];
            if (description.Contains("魔法") || description.Contains("光") || description.Contains("magic") || description.Contains("sparkle"))
                return EffectPresets["魔法光芒"];
            if (description.Contains("闪电") || description.Contains("雷电") || description.Contains("电") || description.Contains("lightning") || description.Contains("thunder"))
                return EffectPresets["闪电"];
            
            // 默认返回烟雾效果
            McpLogger.LogWarning($"未找到匹配的粒子效果预设，使用默认烟雾效果: {description}");
            return EffectPresets["烟雾"];
        }

        /// <summary>
        /// 创建父对象
        /// </summary>
        private GameObject CreateParentObject(string effectName, Vector3 position, string parentName)
        {
            var parentObj = new GameObject(effectName);
            parentObj.transform.position = position;
            
            // 设置层级和标签
            parentObj.layer = LayerMask.NameToLayer("Default");
            parentObj.tag = "Untagged";
            
            // 如果指定了父对象名称，尝试找到并设置父对象
            if (!string.IsNullOrEmpty(parentName))
            {
                var targetParent = GameObject.Find(parentName);
                if (targetParent != null)
                {
                    parentObj.transform.SetParent(targetParent.transform);
                    McpLogger.LogInfo($"粒子效果已设置父对象: {parentName}");
                }
                else
                {
                    McpLogger.LogWarning($"未找到指定的父对象: {parentName}，粒子效果将创建在根级别");
                }
            }
            
            return parentObj;
        }

        /// <summary>
        /// 创建主粒子系统
        /// </summary>
        private ParticleSystem CreateMainParticleSystem(GameObject parent, ParticleEffectConfig config)
        {
            var particleObj = new GameObject("Main Particle");
            particleObj.transform.parent = parent.transform;
            particleObj.transform.localPosition = Vector3.zero;
            
            var particleSystem = particleObj.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(particleSystem, config);
            
            return particleSystem;
        }

        /// <summary>
        /// 配置粒子系统参数
        /// </summary>
        private void ConfigureParticleSystem(ParticleSystem ps, ParticleEffectConfig config)
        {
            // Main module
            var main = ps.main;
            main.startLifetime = config.MainSettings.StartLifetime;
            main.startSpeed = config.MainSettings.StartSpeed;
            main.startSize = config.MainSettings.StartSize;
            main.startColor = config.MainSettings.StartColor;
            main.maxParticles = config.MainSettings.MaxParticles;

            // Emission module
            var emission = ps.emission;
            emission.rateOverTime = config.MainSettings.RateOverTime;
            
            if (config.MainSettings.BurstCount > 0)
            {
                var burst = new ParticleSystem.Burst(config.MainSettings.BurstTime, config.MainSettings.BurstCount);
                emission.SetBursts(new ParticleSystem.Burst[] { burst });
            }

            // Shape module
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = config.Shape;
            if (config.Shape == ParticleSystemShapeType.Box)
            {
                shape.scale = config.ShapeRadius;
            }
            else if (config.Shape == ParticleSystemShapeType.Circle || config.Shape == ParticleSystemShapeType.Sphere)
            {
                shape.radius = config.ShapeRadius.x;
            }

            // Velocity over Lifetime
            if (config.VelocityOverLifetime != Vector3.zero)
            {
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = config.VelocityOverLifetime.x;
                velocity.y = config.VelocityOverLifetime.y;
                velocity.z = config.VelocityOverLifetime.z;
            }

            // Size over Lifetime
            if (config.SizeOverLifetime != null)
            {
                var sizeOverLifetime = ps.sizeOverLifetime;
                sizeOverLifetime.enabled = true;
                var minMaxCurve = new ParticleSystem.MinMaxCurve();
                minMaxCurve.mode = ParticleSystemCurveMode.Curve;
                minMaxCurve.curve = config.SizeOverLifetime;
                minMaxCurve.curveMultiplier = 1f;
                sizeOverLifetime.size = minMaxCurve;
            }

            // Color over Lifetime
            if (config.ColorOverLifetime != null)
            {
                var colorOverLifetime = ps.colorOverLifetime;
                colorOverLifetime.enabled = true;
                // Create a gradient from the animation curve
                var gradient = CreateGradientFromCurve(config.ColorOverLifetime, config.MainSettings.StartColor);
                colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            // Texture Sheet Animation
            if (config.TextureSheetAnimation)
            {
                var textureSheet = ps.textureSheetAnimation;
                textureSheet.enabled = true;
                textureSheet.numTilesX = 2;
                textureSheet.numTilesY = 2;
                textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
                var frameOverTimeCurve = new ParticleSystem.MinMaxCurve();
                frameOverTimeCurve.mode = ParticleSystemCurveMode.Curve;
                frameOverTimeCurve.curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                textureSheet.frameOverTime = frameOverTimeCurve;
            }

            // Collision
            if (config.Collision)
            {
                var collision = ps.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.dampen = 0.5f;
                collision.bounce = 0.3f;
            }

            // Trails (拖尾效果)
            if (config.UseTrails)
            {
                var trails = ps.trails;
                trails.enabled = true;
                trails.mode = ParticleSystemTrailMode.PerParticle;
                trails.ratio = 1f; // 每个粒子都有拖尾
                trails.lifetime = 0.5f; // 拖尾持续时间
                trails.dieWithParticles = true;
                trails.sizeAffectsWidth = true;
                trails.sizeAffectsLifetime = false;
                trails.inheritParticleColor = true;
                
                // 设置拖尾宽度随时间变化
                var widthCurve = new ParticleSystem.MinMaxCurve();
                widthCurve.mode = ParticleSystemCurveMode.Curve;
                widthCurve.curve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(1f, 0f)
                );
                trails.widthOverTrail = widthCurve;
                
                // 设置拖尾颜色渐变
                var colorGradient = new Gradient();
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(config.MainSettings.StartColor, 0f),
                    new GradientColorKey(config.MainSettings.StartColor, 1f)
                };
                var alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                };
                colorGradient.SetKeys(colorKeys, alphaKeys);
                trails.colorOverTrail = new ParticleSystem.MinMaxGradient(colorGradient);
            }
        }

        /// <summary>
        /// 从AnimationCurve创建Gradient用于颜色过渡
        /// </summary>
        private Gradient CreateGradientFromCurve(AnimationCurve curve, Color baseColor)
        {
            var gradient = new Gradient();
            
            // 为火焰效果创建特殊的颜色渐变
            if (baseColor == Color.red) // 火焰效果
            {
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(Color.red, 0f),
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f), // 橙色
                    new GradientColorKey(Color.yellow, 0.8f),
                    new GradientColorKey(Color.white, 1f)
                };
                
                var alphaKeys = new GradientAlphaKey[curve.keys.Length];
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    var key = curve.keys[i];
                    alphaKeys[i] = new GradientAlphaKey(key.value, key.time);
                }
                
                gradient.SetKeys(colorKeys, alphaKeys);
            }
            else if (baseColor.r == 0.8f && baseColor.g == 0.9f && baseColor.b == 1f) // 闪电效果 (检测闪电的默认蓝白色)
            {
                var colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.3f, 0.6f, 1f), 0f), // 深蓝
                    new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0.3f), // 亮蓝
                    new GradientColorKey(Color.white, 0.7f), // 白色闪光
                    new GradientColorKey(new Color(0.9f, 0.95f, 1f), 1f) // 淡蓝白
                };
                
                var alphaKeys = new GradientAlphaKey[curve.keys.Length];
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    var key = curve.keys[i];
                    alphaKeys[i] = new GradientAlphaKey(key.value, key.time);
                }
                
                gradient.SetKeys(colorKeys, alphaKeys);
            }
            else
            {
                // 其他效果使用基础颜色的alpha变化
                var colorKeys = new GradientColorKey[curve.keys.Length];
                var alphaKeys = new GradientAlphaKey[curve.keys.Length];
                
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    var key = curve.keys[i];
                    float time = key.time;
                    float alpha = key.value;
                    
                    colorKeys[i] = new GradientColorKey(baseColor, time);
                    alphaKeys[i] = new GradientAlphaKey(alpha * baseColor.a, time);
                }
                
                gradient.SetKeys(colorKeys, alphaKeys);
            }
            
            return gradient;
        }

        /// <summary>
        /// 创建子效果
        /// </summary>
        private List<ParticleSystem> CreateSubEffects(GameObject parent, ParticleEffectConfig config, string description)
        {
            var subEffects = new List<ParticleSystem>();
            
            // 根据描述添加额外效果
            if (description.Contains("爆炸"))
            {
                // 添加火花效果
                var sparkEffect = CreateSparkEffect(parent);
                subEffects.Add(sparkEffect);
                
                // 添加烟雾效果
                var smokeEffect = CreateSmokeEffect(parent);
                subEffects.Add(smokeEffect);
            }
            else if (description.Contains("魔法"))
            {
                // 添加光环效果
                var auraEffect = CreateAuraEffect(parent);
                subEffects.Add(auraEffect);
            }
            else if (description.Contains("雷暴") || description.Contains("闪电风暴"))
            {
                // 添加闪光效果
                var flashEffect = CreateFlashEffect(parent);
                subEffects.Add(flashEffect);
                
                // 添加电弧效果
                var arcEffect = CreateElectricArcEffect(parent);
                subEffects.Add(arcEffect);
            }
            else if (description.Contains("闪电") && (description.Contains("爆炸") || description.Contains("冲击")))
            {
                // 闪电爆炸组合：闪电 + 冲击波
                var shockwaveEffect = CreateShockwaveEffect(parent);
                subEffects.Add(shockwaveEffect);
            }
            
            return subEffects;
        }

        /// <summary>
        /// 创建火花子效果
        /// </summary>
        private ParticleSystem CreateSparkEffect(GameObject parent)
        {
            var sparkObj = new GameObject("Spark Sub Effect");
            sparkObj.transform.parent = parent.transform;
            sparkObj.transform.localPosition = Vector3.zero;
            
            var ps = sparkObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 15f;
            main.startSize = 0.1f;
            main.startColor = Color.yellow;
            main.maxParticles = 50;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst(0f, 50);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            
            return ps;
        }

        /// <summary>
        /// 创建烟雾子效果
        /// </summary>
        private ParticleSystem CreateSmokeEffect(GameObject parent)
        {
            var smokeObj = new GameObject("Smoke Sub Effect");
            smokeObj.transform.parent = parent.transform;
            smokeObj.transform.localPosition = Vector3.zero;
            
            var ps = smokeObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            main.maxParticles = 30;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst(0.1f, 20);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            
            return ps;
        }

        /// <summary>
        /// 创建光环子效果
        /// </summary>
        private ParticleSystem CreateAuraEffect(GameObject parent)
        {
            var auraObj = new GameObject("Aura Sub Effect");
            auraObj.transform.parent = parent.transform;
            auraObj.transform.localPosition = Vector3.zero;
            
            var ps = auraObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.6f);
            main.maxParticles = 20;
            
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.5f;
            
            return ps;
        }

        /// <summary>
        /// 创建闪光子效果
        /// </summary>
        private ParticleSystem CreateFlashEffect(GameObject parent)
        {
            var flashObj = new GameObject("Flash Sub Effect");
            flashObj.transform.parent = parent.transform;
            flashObj.transform.localPosition = Vector3.zero;
            
            var ps = flashObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 2f;
            main.startColor = Color.white;
            main.maxParticles = 10;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst(0f, 5);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            
            // 闪光效果快速变大后消失
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeMinMaxCurve = new ParticleSystem.MinMaxCurve();
            sizeMinMaxCurve.mode = ParticleSystemCurveMode.Curve;
            sizeMinMaxCurve.curve = new AnimationCurve(
                new Keyframe(0f, 0.1f),
                new Keyframe(0.1f, 1f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = sizeMinMaxCurve;
            
            return ps;
        }

        /// <summary>
        /// 创建电弧子效果
        /// </summary>
        private ParticleSystem CreateElectricArcEffect(GameObject parent)
        {
            var arcObj = new GameObject("Electric Arc Sub Effect");
            arcObj.transform.parent = parent.transform;
            arcObj.transform.localPosition = Vector3.zero;
            
            var ps = arcObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 0.6f;
            main.startSpeed = 8f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.5f, 0.8f, 1f);
            main.maxParticles = 30;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst(0f, 20);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            
            // 启用拖尾效果
            var trails = ps.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = 0.3f;
            trails.inheritParticleColor = true;
            
            return ps;
        }

        /// <summary>
        /// 创建冲击波子效果
        /// </summary>
        private ParticleSystem CreateShockwaveEffect(GameObject parent)
        {
            var shockObj = new GameObject("Shockwave Sub Effect");
            shockObj.transform.parent = parent.transform;
            shockObj.transform.localPosition = Vector3.zero;
            
            var ps = shockObj.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startLifetime = 1f;
            main.startSpeed = 15f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.8f, 0.9f, 1f, 0.5f);
            main.maxParticles = 50;
            
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var burst = new ParticleSystem.Burst(0.1f, 30);
            emission.SetBursts(new ParticleSystem.Burst[] { burst });
            
            // 环形发射
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.1f;
            
            return ps;
        }
    }

    /// <summary>
    /// 粒子效果配置数据结构
    /// </summary>
    [Serializable]
    public class ParticleEffectConfig
    {
        public string EffectName;
        public ParticleMainSettings MainSettings;
        public ParticleSystemShapeType Shape;
        public Vector3 ShapeRadius;
        public Vector3 VelocityOverLifetime;
        public AnimationCurve SizeOverLifetime;
        public AnimationCurve ColorOverLifetime;
        public bool TextureSheetAnimation;
        public bool Collision;
        public bool UseTrails = false; // 是否启用拖尾效果
    }

    /// <summary>
    /// 粒子主要设置
    /// </summary>
    [Serializable]
    public class ParticleMainSettings
    {
        public float StartLifetime = 5f;
        public float StartSpeed = 5f;
        public float StartSize = 1f;
        public Color StartColor = Color.white;
        public int MaxParticles = 1000;
        public float RateOverTime = 10f;
        public int BurstCount = 0;
        public float BurstTime = 0f;
    }
} 