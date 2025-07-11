using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using McpUnity.Models;
using McpUnity.Utils;
using McpUnity.Unity;

namespace McpUnity.Tools
{
    /// <summary>
    /// 物体分析工具
    /// </summary>
    public class AnalyzeObjectTool : McpToolBase
    {
        public AnalyzeObjectTool()
        {
            Name = "analyze_object";
            Description = "分析Unity场景物体或Project资源的详细信息，包括组件、属性、层级结构等，并支持添加子对象";
        }

        public override JObject Execute(JObject parameters)
        {
            try
            {
                McpLogger.LogInfo("开始执行物体分析...");
                
                var request = ExtractAnalyzeRequest(parameters);
                var response = AnalyzeObject(request);
                
                // 手动构建返回的JObject，避免序列化循环引用
                var result = new JObject();
                result["success"] = response.Success;
                result["errorMessage"] = response.ErrorMessage ?? "";
                result["analysisType"] = request.TargetType.ToString();
                
                if (response.MainObject != null)
                {
                    result["mainObject"] = SerializeObjectInfo(response.MainObject);
                }
                
                if (response.ChildObjects != null && response.ChildObjects.Count > 0)
                {
                    var childArray = new JArray();
                    foreach (var child in response.ChildObjects)
                    {
                        childArray.Add(SerializeObjectInfo(child));
                    }
                    result["childObjects"] = childArray;
                }
                
                if (response.Dependencies != null && response.Dependencies.Count > 0)
                {
                    var depArray = new JArray();
                    foreach (var dep in response.Dependencies)
                    {
                        var depObj = new JObject();
                        depObj["assetPath"] = dep.AssetPath;
                        depObj["dependencyType"] = dep.DependencyType;
                        depObj["isMissing"] = dep.IsMissing;
                        depArray.Add(depObj);
                    }
                    result["dependencies"] = depArray;
                }
                
                if (response.SuggestedActions != null && response.SuggestedActions.Count > 0)
                {
                    result["suggestedActions"] = new JArray(response.SuggestedActions);
                }
                
                result["analysisSummary"] = response.AnalysisSummary ?? "";
                
                McpLogger.LogInfo("物体分析完成");
                return result;
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"物体分析失败: {ex.Message}\n{ex.StackTrace}");
                
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"物体分析失败: {ex.Message}",
                    "analysis_error"
                );
            }
        }

        /// <summary>
        /// 从参数中提取分析请求
        /// </summary>
        private AnalyzeObjectRequest ExtractAnalyzeRequest(JObject parameters)
        {
            var request = new AnalyzeObjectRequest();
            
            // 分析目标类型
            if (parameters.ContainsKey("targetType"))
            {
                var targetTypeStr = parameters["targetType"]?.ToString();
                if (Enum.TryParse<AnalysisTargetType>(targetTypeStr, true, out var targetType))
                {
                    request.TargetType = targetType;
                }
            }
            else
            {
                request.TargetType = AnalysisTargetType.Selected; // 默认分析选中的物体
            }
            
            // 目标路径
            if (parameters.ContainsKey("targetPath"))
            {
                request.TargetPath = parameters["targetPath"]?.ToString();
            }
            
            // 其他选项
            if (parameters.ContainsKey("includeChildren"))
            {
                request.IncludeChildren = parameters["includeChildren"]?.ToObject<bool>() ?? true;
            }
            
            if (parameters.ContainsKey("includeComponentDetails"))
            {
                request.IncludeComponentDetails = parameters["includeComponentDetails"]?.ToObject<bool>() ?? true;
            }
            
            if (parameters.ContainsKey("maxDepth"))
            {
                request.MaxDepth = parameters["maxDepth"]?.ToObject<int>() ?? 5;
            }
            
            if (parameters.ContainsKey("includeDependencies"))
            {
                request.IncludeDependencies = parameters["includeDependencies"]?.ToObject<bool>() ?? false;
            }
            
            // 添加子对象操作
            if (parameters.ContainsKey("addChildOperations"))
            {
                request.AddChildOperations = ExtractChildOperations(parameters["addChildOperations"]);
            }
            
            return request;
        }

        /// <summary>
        /// 提取子对象操作
        /// </summary>
        private List<AddChildObjectOperation> ExtractChildOperations(JToken operationsToken)
        {
            var operations = new List<AddChildObjectOperation>();
            
            if (operationsToken is JArray operationsArray)
            {
                foreach (var opToken in operationsArray)
                {
                    if (opToken is JObject opObj)
                    {
                        var operation = new AddChildObjectOperation();
                        operation.ChildName = opObj["childName"]?.ToString() ?? "New Object";
                        
                        if (opObj.ContainsKey("objectType"))
                        {
                            var typeStr = opObj["objectType"]?.ToString();
                            if (Enum.TryParse<ChildObjectType>(typeStr, true, out var childType))
                            {
                                operation.ObjectType = childType;
                            }
                        }
                        
                        if (opObj.ContainsKey("position"))
                        {
                            operation.Position = ExtractVector3(opObj, "position", Vector3.zero);
                        }
                        
                        if (opObj.ContainsKey("components") && opObj["components"] is JArray compArray)
                        {
                            operation.Components = compArray.Select(t => t.ToString()).ToList();
                        }
                        
                        operations.Add(operation);
                    }
                }
            }
            
            return operations;
        }

        /// <summary>
        /// 提取Vector3
        /// </summary>
        private Vector3 ExtractVector3(JObject obj, string key, Vector3 defaultValue)
        {
            if (!obj.ContainsKey(key)) return defaultValue;
            
            var token = obj[key];
            if (token is JObject vecObj)
            {
                var x = vecObj["x"]?.ToObject<float>() ?? 0f;
                var y = vecObj["y"]?.ToObject<float>() ?? 0f;
                var z = vecObj["z"]?.ToObject<float>() ?? 0f;
                return new Vector3(x, y, z);
            }
            else if (token is JArray vecArray && vecArray.Count >= 3)
            {
                var x = vecArray[0]?.ToObject<float>() ?? 0f;
                var y = vecArray[1]?.ToObject<float>() ?? 0f;
                var z = vecArray[2]?.ToObject<float>() ?? 0f;
                return new Vector3(x, y, z);
            }
            
            return defaultValue;
        }

        /// <summary>
        /// 执行物体分析
        /// </summary>
        private AnalyzeObjectResponse AnalyzeObject(AnalyzeObjectRequest request)
        {
            var response = new AnalyzeObjectResponse();
            
            try
            {
                GameObject targetObject = null;
                UnityEngine.Object targetAsset = null;
                
                // 根据分析类型获取目标对象
                switch (request.TargetType)
                {
                    case AnalysisTargetType.Selected:
                        targetObject = GetSelectedGameObject();
                        if (targetObject == null)
                        {
                            targetAsset = GetSelectedAsset();
                        }
                        break;
                        
                    case AnalysisTargetType.SceneObject:
                        if (!string.IsNullOrEmpty(request.TargetPath))
                        {
                            targetObject = GameObject.Find(request.TargetPath);
                        }
                        break;
                        
                    case AnalysisTargetType.ProjectAsset:
                        if (!string.IsNullOrEmpty(request.TargetPath))
                        {
                            targetAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(request.TargetPath);
                        }
                        break;
                }
                
                if (targetObject == null && targetAsset == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "未找到目标对象或资源";
                    return response;
                }
                
                // 分析主对象
                if (targetObject != null)
                {
                    response.MainObject = AnalyzeGameObject(targetObject, request);
                    
                    // 分析子对象
                    if (request.IncludeChildren)
                    {
                        response.ChildObjects = AnalyzeChildObjects(targetObject, request, 0);
                    }
                    
                    // 执行添加子对象操作
                    if (request.AddChildOperations != null && request.AddChildOperations.Count > 0)
                    {
                        ExecuteAddChildOperations(targetObject, request.AddChildOperations);
                    }
                }
                else if (targetAsset != null)
                {
                    response.MainObject = AnalyzeAsset(targetAsset, request);
                }
                
                // 分析依赖关系
                if (request.IncludeDependencies)
                {
                    response.Dependencies = AnalyzeDependencies(targetObject, targetAsset);
                }
                
                // 生成建议操作
                response.SuggestedActions = GenerateSuggestedActions(response.MainObject, response.ChildObjects);
                
                // 生成分析摘要
                response.AnalysisSummary = GenerateAnalysisSummary(response);
                
                response.Success = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = ex.Message;
                McpLogger.LogError($"分析过程中发生错误: {ex.Message}");
            }
            
            return response;
        }

        /// <summary>
        /// 获取当前选中的GameObject
        /// </summary>
        private GameObject GetSelectedGameObject()
        {
            var selected = Selection.activeGameObject;
            return selected;
        }

        /// <summary>
        /// 获取当前选中的资源
        /// </summary>
        private UnityEngine.Object GetSelectedAsset()
        {
            var selected = Selection.activeObject;
            if (selected != null && AssetDatabase.Contains(selected))
            {
                return selected;
            }
            return null;
        }

        /// <summary>
        /// 分析GameObject
        /// </summary>
        private ObjectAnalysisInfo AnalyzeGameObject(GameObject obj, AnalyzeObjectRequest request)
        {
            var info = new ObjectAnalysisInfo();
            
            info.Name = obj.name;
            info.Type = "GameObject";
            info.HierarchyPath = GetHierarchyPath(obj);
            info.IsActive = obj.activeInHierarchy;
            info.ChildCount = obj.transform.childCount;
            info.Tag = obj.tag;
            info.Layer = obj.layer;
            
            // Transform信息
            info.Transform = new TransformInfo();
            info.Transform.Position = obj.transform.position;
            info.Transform.Rotation = obj.transform.eulerAngles;
            info.Transform.Scale = obj.transform.lossyScale;
            info.Transform.LocalPosition = obj.transform.localPosition;
            info.Transform.LocalRotation = obj.transform.localEulerAngles;
            info.Transform.LocalScale = obj.transform.localScale;
            
            // 组件信息
            if (request.IncludeComponentDetails)
            {
                info.Components = AnalyzeComponents(obj);
            }
            
            // 资源信息（如果是Prefab）
            var prefabType = PrefabUtility.GetPrefabAssetType(obj);
            if (prefabType != PrefabAssetType.NotAPrefab)
            {
                info.AssetInfo = new AssetInfo();
                var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    info.AssetInfo.AssetPath = prefabPath;
                    info.AssetInfo.AssetType = prefabType.ToString();
                    info.AssetInfo.IsPrefab = true;
                }
            }
            
            return info;
        }

        /// <summary>
        /// 分析Asset
        /// </summary>
        private ObjectAnalysisInfo AnalyzeAsset(UnityEngine.Object asset, AnalyzeObjectRequest request)
        {
            var info = new ObjectAnalysisInfo();
            
            info.Name = asset.name;
            info.Type = asset.GetType().Name;
            
            var assetPath = AssetDatabase.GetAssetPath(asset);
            info.HierarchyPath = assetPath;
            
            // 资源信息
            info.AssetInfo = new AssetInfo();
            info.AssetInfo.AssetPath = assetPath;
            info.AssetInfo.AssetType = asset.GetType().Name;
            
            var fileInfo = new System.IO.FileInfo(assetPath);
            if (fileInfo.Exists)
            {
                info.AssetInfo.FileSize = fileInfo.Length;
            }
            
            // 如果是GameObject类型的Asset（Prefab）
            if (asset is GameObject prefabObj)
            {
                info.AssetInfo.IsPrefab = true;
                info.ChildCount = prefabObj.transform.childCount;
                info.Tag = prefabObj.tag;
                info.Layer = prefabObj.layer;
                
                if (request.IncludeComponentDetails)
                {
                    info.Components = AnalyzeComponents(prefabObj);
                }
            }
            
            return info;
        }

        /// <summary>
        /// 分析子对象
        /// </summary>
        private List<ObjectAnalysisInfo> AnalyzeChildObjects(GameObject parent, AnalyzeObjectRequest request, int currentDepth)
        {
            var childObjects = new List<ObjectAnalysisInfo>();
            
            if (currentDepth >= request.MaxDepth)
            {
                return childObjects;
            }
            
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                var child = parent.transform.GetChild(i).gameObject;
                var childInfo = AnalyzeGameObject(child, request);
                childObjects.Add(childInfo);
                
                // 递归分析子对象的子对象
                if (child.transform.childCount > 0 && currentDepth < request.MaxDepth - 1)
                {
                    var grandChildren = AnalyzeChildObjects(child, request, currentDepth + 1);
                    childObjects.AddRange(grandChildren);
                }
            }
            
            return childObjects;
        }

        /// <summary>
        /// 分析组件
        /// </summary>
        private List<ComponentInfo> AnalyzeComponents(GameObject obj)
        {
            var components = new List<ComponentInfo>();
            var allComponents = obj.GetComponents<Component>();
            
            foreach (var component in allComponents)
            {
                if (component == null) continue;
                
                var compInfo = new ComponentInfo();
                compInfo.Name = component.GetType().Name;
                compInfo.Type = component.GetType().FullName;
                compInfo.Description = GetComponentDescription(component);
                
                // 检查组件是否启用
                if (component is MonoBehaviour monoBehaviour)
                {
                    compInfo.Enabled = monoBehaviour.enabled;
                }
                else if (component is Renderer renderer)
                {
                    compInfo.Enabled = renderer.enabled;
                }
                else if (component is Collider collider)
                {
                    compInfo.Enabled = collider.enabled;
                }
                
                // 分析组件属性
                compInfo.Properties = AnalyzeComponentProperties(component);
                
                components.Add(compInfo);
            }
            
            return components;
        }

        /// <summary>
        /// 分析组件属性
        /// </summary>
        private List<ComponentProperty> AnalyzeComponentProperties(Component component)
        {
            var properties = new List<ComponentProperty>();
            var type = component.GetType();
            
            // 获取公共字段和属性
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            
            // 分析字段
            foreach (var field in fields)
            {
                if (ShouldIncludeProperty(field.Name, field.FieldType))
                {
                    var prop = new ComponentProperty();
                    prop.Name = field.Name;
                    prop.Type = field.FieldType.Name;
                    
                    try
                    {
                        var value = field.GetValue(component);
                        prop.Value = GetPropertyValueString(value);
                    }
                    catch
                    {
                        prop.Value = "无法获取";
                    }
                    
                    properties.Add(prop);
                }
            }
            
            // 分析属性
            foreach (var property in props)
            {
                if (property.CanRead && ShouldIncludeProperty(property.Name, property.PropertyType))
                {
                    var prop = new ComponentProperty();
                    prop.Name = property.Name;
                    prop.Type = property.PropertyType.Name;
                    
                    try
                    {
                        var value = property.GetValue(component);
                        prop.Value = GetPropertyValueString(value);
                    }
                    catch
                    {
                        prop.Value = "无法获取";
                    }
                    
                    properties.Add(prop);
                }
            }
            
            return properties;
        }

        /// <summary>
        /// 判断是否应该包含某个属性
        /// </summary>
        private bool ShouldIncludeProperty(string propertyName, Type propertyType)
        {
            // 排除一些不需要的属性
            var excludedNames = new[] { "hideFlags", "name", "tag" };
            if (excludedNames.Contains(propertyName)) return false;
            
            // 只包含基本类型和Unity类型
            if (propertyType.IsPrimitive || propertyType == typeof(string) || 
                propertyType == typeof(Vector3) || propertyType == typeof(Vector2) ||
                propertyType == typeof(Color) || propertyType == typeof(Quaternion) ||
                propertyType.IsEnum)
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// 获取属性值的字符串表示
        /// </summary>
        private string GetPropertyValueString(object value)
        {
            if (value == null) return "null";
            
            if (value is Vector3 v3)
                return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            else if (value is Vector2 v2)
                return $"({v2.x:F2}, {v2.y:F2})";
            else if (value is Color color)
                return $"RGBA({color.r:F2}, {color.g:F2}, {color.b:F2}, {color.a:F2})";
            else if (value is Quaternion q)
                return $"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})";
            else
                return value.ToString();
        }

        /// <summary>
        /// 获取组件描述
        /// </summary>
        private string GetComponentDescription(Component component)
        {
            var type = component.GetType();
            
            // 根据组件类型返回描述
            switch (type.Name)
            {
                case "Transform":
                    return "控制物体的位置、旋转和缩放";
                case "MeshRenderer":
                    return "渲染3D网格";
                case "MeshFilter":
                    return "存储3D网格数据";
                case "BoxCollider":
                    return "盒子形状的碰撞器";
                case "SphereCollider":
                    return "球形碰撞器";
                case "Rigidbody":
                    return "物理刚体组件";
                case "Camera":
                    return "摄像机组件";
                case "Light":
                    return "光源组件";
                case "AudioSource":
                    return "音频播放源";
                default:
                    return $"{type.Name}组件";
            }
        }

        /// <summary>
        /// 获取层级路径
        /// </summary>
        private string GetHierarchyPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        /// <summary>
        /// 分析依赖关系
        /// </summary>
        private List<AssetDependencyInfo> AnalyzeDependencies(GameObject targetObject, UnityEngine.Object targetAsset)
        {
            var dependencies = new List<AssetDependencyInfo>();
            
            // 分析GameObject的依赖
            if (targetObject != null)
            {
                // 分析材质依赖
                var renderers = targetObject.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null)
                        {
                            var assetPath = AssetDatabase.GetAssetPath(material);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                dependencies.Add(new AssetDependencyInfo
                                {
                                    AssetPath = assetPath,
                                    DependencyType = "Material",
                                    IsMissing = false
                                });
                            }
                        }
                    }
                }
                
                // 分析网格依赖
                var meshFilters = targetObject.GetComponentsInChildren<MeshFilter>();
                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter.sharedMesh != null)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                        if (!string.IsNullOrEmpty(assetPath))
                        {
                            dependencies.Add(new AssetDependencyInfo
                            {
                                AssetPath = assetPath,
                                DependencyType = "Mesh",
                                IsMissing = false
                            });
                        }
                    }
                }
            }
            
            // 分析Asset的依赖
            if (targetAsset != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(targetAsset);
                var assetDeps = AssetDatabase.GetDependencies(assetPath);
                
                foreach (var depPath in assetDeps)
                {
                    if (depPath != assetPath) // 排除自身
                    {
                        dependencies.Add(new AssetDependencyInfo
                        {
                            AssetPath = depPath,
                            DependencyType = "Asset Dependency",
                            IsMissing = !System.IO.File.Exists(depPath)
                        });
                    }
                }
            }
            
            return dependencies;
        }

        /// <summary>
        /// 生成建议操作
        /// </summary>
        private List<string> GenerateSuggestedActions(ObjectAnalysisInfo mainObject, List<ObjectAnalysisInfo> childObjects)
        {
            var suggestions = new List<string>();
            
            if (mainObject == null) return suggestions;
            
            // 基于分析结果生成建议
            if (mainObject.Components != null)
            {
                var hasRenderer = mainObject.Components.Any(c => c.Type.Contains("Renderer"));
                var hasCollider = mainObject.Components.Any(c => c.Type.Contains("Collider"));
                var hasRigidbody = mainObject.Components.Any(c => c.Type.Contains("Rigidbody"));
                
                if (hasRenderer && !hasCollider)
                {
                    suggestions.Add("考虑添加碰撞器组件以支持物理交互");
                }
                
                if (hasCollider && !hasRigidbody)
                {
                    suggestions.Add("考虑添加Rigidbody组件以启用物理模拟");
                }
                
                if (mainObject.ChildCount == 0)
                {
                    suggestions.Add("可以添加子对象来丰富物体结构");
                }
                
                if (mainObject.ChildCount > 10)
                {
                    suggestions.Add("子对象较多，考虑优化层级结构");
                }
            }
            
            return suggestions;
        }

        /// <summary>
        /// 生成分析摘要
        /// </summary>
        private string GenerateAnalysisSummary(AnalyzeObjectResponse response)
        {
            if (response.MainObject == null) return "分析失败";
            
            var summary = $"物体 '{response.MainObject.Name}' 分析完成。";
            
            if (response.MainObject.Components != null)
            {
                summary += $" 包含 {response.MainObject.Components.Count} 个组件";
            }
            
            if (response.ChildObjects != null && response.ChildObjects.Count > 0)
            {
                summary += $"，{response.ChildObjects.Count} 个子对象";
            }
            
            if (response.Dependencies != null && response.Dependencies.Count > 0)
            {
                summary += $"，{response.Dependencies.Count} 个依赖资源";
            }
            
            summary += "。";
            
            return summary;
        }

        /// <summary>
        /// 执行添加子对象操作
        /// </summary>
        private void ExecuteAddChildOperations(GameObject parent, List<AddChildObjectOperation> operations)
        {
            foreach (var operation in operations)
            {
                try
                {
                    GameObject childObject = CreateChildObject(operation.ObjectType, operation.ChildName);
                    childObject.transform.SetParent(parent.transform);
                    childObject.transform.localPosition = operation.Position;
                    
                    // 添加指定的组件
                    if (operation.Components != null)
                    {
                        foreach (var componentName in operation.Components)
                        {
                            AddComponentByName(childObject, componentName);
                        }
                    }
                    
                    McpLogger.LogInfo($"已添加子对象: {operation.ChildName} ({operation.ObjectType})");
                }
                catch (Exception ex)
                {
                    McpLogger.LogError($"添加子对象失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 创建子对象
        /// </summary>
        private GameObject CreateChildObject(ChildObjectType objectType, string name)
        {
            GameObject obj;
            
            switch (objectType)
            {
                case ChildObjectType.Cube:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case ChildObjectType.Sphere:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case ChildObjectType.Capsule:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                case ChildObjectType.Cylinder:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case ChildObjectType.Plane:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                case ChildObjectType.Quad:
                    obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    break;
                case ChildObjectType.Camera:
                    obj = new GameObject();
                    obj.AddComponent<Camera>();
                    break;
                case ChildObjectType.Light:
                    obj = new GameObject();
                    obj.AddComponent<Light>();
                    break;
                case ChildObjectType.ParticleSystem:
                    obj = new GameObject();
                    obj.AddComponent<ParticleSystem>();
                    break;
                case ChildObjectType.AudioSource:
                    obj = new GameObject();
                    obj.AddComponent<AudioSource>();
                    break;
                default:
                    obj = new GameObject();
                    break;
            }
            
            obj.name = name;
            return obj;
        }

        /// <summary>
        /// 根据名称添加组件
        /// </summary>
        private void AddComponentByName(GameObject obj, string componentName)
        {
            try
            {
                var type = Type.GetType($"UnityEngine.{componentName}, UnityEngine") ?? 
                          Type.GetType($"UnityEngine.{componentName}, UnityEngine.CoreModule");
                
                if (type != null && typeof(Component).IsAssignableFrom(type))
                {
                    obj.AddComponent(type);
                }
            }
            catch (Exception ex)
            {
                McpLogger.LogError($"添加组件 {componentName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 序列化物体信息，避免循环引用
        /// </summary>
        private JObject SerializeObjectInfo(ObjectAnalysisInfo info)
        {
            var obj = new JObject();
            obj["name"] = info.Name;
            obj["type"] = info.Type;
            obj["hierarchyPath"] = info.HierarchyPath;
            obj["isActive"] = info.IsActive;
            obj["childCount"] = info.ChildCount;
            obj["tag"] = info.Tag;
            obj["layer"] = info.Layer;
            
            if (info.Transform != null)
            {
                var transformObj = new JObject();
                transformObj["position"] = SerializeVector3(info.Transform.Position);
                transformObj["rotation"] = SerializeVector3(info.Transform.Rotation);
                transformObj["scale"] = SerializeVector3(info.Transform.Scale);
                transformObj["localPosition"] = SerializeVector3(info.Transform.LocalPosition);
                transformObj["localRotation"] = SerializeVector3(info.Transform.LocalRotation);
                transformObj["localScale"] = SerializeVector3(info.Transform.LocalScale);
                obj["transform"] = transformObj;
            }
            
            if (info.Components != null && info.Components.Count > 0)
            {
                var componentsArray = new JArray();
                foreach (var comp in info.Components)
                {
                    var compObj = new JObject();
                    compObj["name"] = comp.Name;
                    compObj["type"] = comp.Type;
                    compObj["enabled"] = comp.Enabled;
                    compObj["description"] = comp.Description;
                    
                    if (comp.Properties != null && comp.Properties.Count > 0)
                    {
                        var propsArray = new JArray();
                        foreach (var prop in comp.Properties)
                        {
                            var propObj = new JObject();
                            propObj["name"] = prop.Name;
                            propObj["value"] = prop.Value;
                            propObj["type"] = prop.Type;
                            propObj["isDefault"] = prop.IsDefault;
                            propsArray.Add(propObj);
                        }
                        compObj["properties"] = propsArray;
                    }
                    
                    componentsArray.Add(compObj);
                }
                obj["components"] = componentsArray;
            }
            
            if (info.AssetInfo != null)
            {
                var assetObj = new JObject();
                assetObj["assetPath"] = info.AssetInfo.AssetPath;
                assetObj["assetType"] = info.AssetInfo.AssetType;
                assetObj["fileSize"] = info.AssetInfo.FileSize;
                assetObj["isPrefab"] = info.AssetInfo.IsPrefab;
                assetObj["importSettings"] = info.AssetInfo.ImportSettings;
                obj["assetInfo"] = assetObj;
            }
            
            return obj;
        }

        /// <summary>
        /// 序列化Vector3，避免循环引用
        /// </summary>
        private JObject SerializeVector3(Vector3 vector)
        {
            var obj = new JObject();
            obj["x"] = vector.x;
            obj["y"] = vector.y;
            obj["z"] = vector.z;
            return obj;
        }
    }
} 