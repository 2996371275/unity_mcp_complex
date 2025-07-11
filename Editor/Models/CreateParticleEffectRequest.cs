using System;
using UnityEngine;

namespace McpUnity.Models
{
    /// <summary>
    /// 创建粒子效果请求的数据模型
    /// </summary>
    [Serializable]
    public class CreateParticleEffectRequest
    {
        /// <summary>
        /// 粒子效果的自然语言描述
        /// </summary>
        public string Description;
        
        /// <summary>
        /// 粒子效果的生成位置
        /// </summary>
        public Vector3 Position = Vector3.zero;
        
        /// <summary>
        /// 粒子效果的缩放大小
        /// </summary>
        public float Scale = 1f;
        
        /// <summary>
        /// 是否自动播放粒子效果
        /// </summary>
        public bool AutoPlay = true;
        
        /// <summary>
        /// 粒子效果的父对象名称（可选）
        /// </summary>
        public string ParentName;
        
        /// <summary>
        /// 自定义粒子数量倍数（可选，默认为1）
        /// </summary>
        public float IntensityMultiplier = 1f;
    }
    
    /// <summary>
    /// 创建粒子效果响应的数据模型
    /// </summary>
    [Serializable]
    public class CreateParticleEffectResponse
    {
        /// <summary>
        /// 操作是否成功
        /// </summary>
        public bool Success;
        
        /// <summary>
        /// 创建的粒子效果对象名称
        /// </summary>
        public string ObjectName;
        
        /// <summary>
        /// 创建的粒子效果对象实例ID
        /// </summary>
        public int InstanceId;
        
        /// <summary>
        /// 粒子效果类型
        /// </summary>
        public string EffectType;
        
        /// <summary>
        /// 粒子数量
        /// </summary>
        public int ParticleCount;
        
        /// <summary>
        /// 返回消息
        /// </summary>
        public string Message;
        
        /// <summary>
        /// 创建的子效果数量
        /// </summary>
        public int SubEffectCount;
        
        /// <summary>
        /// 粒子效果位置
        /// </summary>
        public Vector3 Position;
    }
} 