using System;
using System.Collections.Generic;
using UnityEngine;

namespace McpUnity.Models
{
    /// <summary>
    /// 分析物体请求
    /// </summary>
    [Serializable]
    public class AnalyzeObjectRequest
    {
        /// <summary>
        /// 分析目标类型
        /// </summary>
        public AnalysisTargetType TargetType;
        
        /// <summary>
        /// 目标物体路径（场景中的路径或Project中的路径）
        /// </summary>
        public string TargetPath;
        
        /// <summary>
        /// 是否包含子物体分析
        /// </summary>
        public bool IncludeChildren = true;
        
        /// <summary>
        /// 是否包含组件详细信息
        /// </summary>
        public bool IncludeComponentDetails = true;
        
        /// <summary>
        /// 分析深度（层级深度限制）
        /// </summary>
        public int MaxDepth = 5;
        
        /// <summary>
        /// 是否包含资源依赖分析
        /// </summary>
        public bool IncludeDependencies = false;
        
        /// <summary>
        /// 添加子对象操作（可选）
        /// </summary>
        public List<AddChildObjectOperation> AddChildOperations;
    }

    /// <summary>
    /// 分析物体响应
    /// </summary>
    [Serializable]
    public class AnalyzeObjectResponse
    {
        /// <summary>
        /// 分析成功标志
        /// </summary>
        public bool Success;
        
        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage;
        
        /// <summary>
        /// 主要分析结果
        /// </summary>
        public ObjectAnalysisInfo MainObject;
        
        /// <summary>
        /// 子物体分析结果
        /// </summary>
        public List<ObjectAnalysisInfo> ChildObjects;
        
        /// <summary>
        /// 资源依赖信息
        /// </summary>
        public List<AssetDependencyInfo> Dependencies;
        
        /// <summary>
        /// 建议的操作
        /// </summary>
        public List<string> SuggestedActions;
        
        /// <summary>
        /// 分析摘要
        /// </summary>
        public string AnalysisSummary;
    }

    /// <summary>
    /// 物体分析信息
    /// </summary>
    [Serializable]
    public class ObjectAnalysisInfo
    {
        /// <summary>
        /// 物体名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 物体类型
        /// </summary>
        public string Type;
        
        /// <summary>
        /// 层级路径
        /// </summary>
        public string HierarchyPath;
        
        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Transform信息
        /// </summary>
        public TransformInfo Transform;
        
        /// <summary>
        /// 组件列表
        /// </summary>
        public List<ComponentInfo> Components;
        
        /// <summary>
        /// 子物体数量
        /// </summary>
        public int ChildCount;
        
        /// <summary>
        /// 标签
        /// </summary>
        public string Tag;
        
        /// <summary>
        /// 层级
        /// </summary>
        public int Layer;
        
        /// <summary>
        /// 资源信息（如果是Prefab）
        /// </summary>
        public AssetInfo AssetInfo;
    }

    /// <summary>
    /// Transform信息
    /// </summary>
    [Serializable]
    public class TransformInfo
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
        public Vector3 LocalPosition;
        public Vector3 LocalRotation;
        public Vector3 LocalScale;
    }

    /// <summary>
    /// 组件信息
    /// </summary>
    [Serializable]
    public class ComponentInfo
    {
        /// <summary>
        /// 组件名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 组件类型
        /// </summary>
        public string Type;
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enabled = true;
        
        /// <summary>
        /// 组件属性
        /// </summary>
        public List<ComponentProperty> Properties;
        
        /// <summary>
        /// 组件描述
        /// </summary>
        public string Description;
    }

    /// <summary>
    /// 组件属性
    /// </summary>
    [Serializable]
    public class ComponentProperty
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 属性值
        /// </summary>
        public string Value;
        
        /// <summary>
        /// 属性类型
        /// </summary>
        public string Type;
        
        /// <summary>
        /// 是否为默认值
        /// </summary>
        public bool IsDefault = true;
    }

    /// <summary>
    /// 资源信息
    /// </summary>
    [Serializable]
    public class AssetInfo
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string AssetPath;
        
        /// <summary>
        /// 资源类型
        /// </summary>
        public string AssetType;
        
        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize;
        
        /// <summary>
        /// 是否为Prefab
        /// </summary>
        public bool IsPrefab;
        
        /// <summary>
        /// 导入设置
        /// </summary>
        public string ImportSettings;
    }

    /// <summary>
    /// 资源依赖信息
    /// </summary>
    [Serializable]
    public class AssetDependencyInfo
    {
        /// <summary>
        /// 依赖资源路径
        /// </summary>
        public string AssetPath;
        
        /// <summary>
        /// 依赖类型
        /// </summary>
        public string DependencyType;
        
        /// <summary>
        /// 是否缺失
        /// </summary>
        public bool IsMissing;
    }

    /// <summary>
    /// 添加子对象操作
    /// </summary>
    [Serializable]
    public class AddChildObjectOperation
    {
        /// <summary>
        /// 子对象名称
        /// </summary>
        public string ChildName;
        
        /// <summary>
        /// 子对象类型
        /// </summary>
        public ChildObjectType ObjectType;
        
        /// <summary>
        /// 相对位置
        /// </summary>
        public Vector3 Position = Vector3.zero;
        
        /// <summary>
        /// 要添加的组件
        /// </summary>
        public List<string> Components;
        
        /// <summary>
        /// 组件属性设置
        /// </summary>
        public Dictionary<string, object> ComponentProperties;
    }

    /// <summary>
    /// 分析目标类型
    /// </summary>
    public enum AnalysisTargetType
    {
        /// <summary>
        /// 当前选中的物体
        /// </summary>
        Selected,
        
        /// <summary>
        /// 场景中的物体
        /// </summary>
        SceneObject,
        
        /// <summary>
        /// Project面板中的资源
        /// </summary>
        ProjectAsset,
        
        /// <summary>
        /// 指定路径的物体
        /// </summary>
        SpecificPath
    }

    /// <summary>
    /// 子对象类型
    /// </summary>
    public enum ChildObjectType
    {
        /// <summary>
        /// 空对象
        /// </summary>
        Empty,
        
        /// <summary>
        /// 立方体
        /// </summary>
        Cube,
        
        /// <summary>
        /// 球体
        /// </summary>
        Sphere,
        
        /// <summary>
        /// 胶囊体
        /// </summary>
        Capsule,
        
        /// <summary>
        /// 圆柱体
        /// </summary>
        Cylinder,
        
        /// <summary>
        /// 平面
        /// </summary>
        Plane,
        
        /// <summary>
        /// 四边形
        /// </summary>
        Quad,
        
        /// <summary>
        /// 摄像机
        /// </summary>
        Camera,
        
        /// <summary>
        /// 灯光
        /// </summary>
        Light,
        
        /// <summary>
        /// UI Canvas
        /// </summary>
        Canvas,
        
        /// <summary>
        /// UI Button
        /// </summary>
        Button,
        
        /// <summary>
        /// UI Text
        /// </summary>
        Text,
        
        /// <summary>
        /// UI Image
        /// </summary>
        Image,
        
        /// <summary>
        /// 粒子系统
        /// </summary>
        ParticleSystem,
        
        /// <summary>
        /// 音频源
        /// </summary>
        AudioSource,
        
        /// <summary>
        /// 预制体实例
        /// </summary>
        PrefabInstance
    }
} 