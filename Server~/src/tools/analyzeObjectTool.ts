import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { z } from 'zod';
import { McpUnity } from "../unity/mcpUnity.js";
import { Logger } from "../utils/logger.js";
import { McpUnityError, ErrorType } from "../utils/errors.js";

/**
 * 物体分析工具
 * 分析Unity场景物体或Project资源的详细信息
 */

// 分析目标类型
export enum AnalysisTargetType {
  Selected = "Selected",
  SceneObject = "SceneObject", 
  ProjectAsset = "ProjectAsset",
  SpecificPath = "SpecificPath"
}

// 子对象类型
export enum ChildObjectType {
  Empty = "Empty",
  Cube = "Cube",
  Sphere = "Sphere",
  Capsule = "Capsule",
  Cylinder = "Cylinder",
  Plane = "Plane",
  Quad = "Quad",
  Camera = "Camera",
  Light = "Light",
  Canvas = "Canvas",
  Button = "Button",
  Text = "Text",
  Image = "Image",
  ParticleSystem = "ParticleSystem",
  AudioSource = "AudioSource",
  PrefabInstance = "PrefabInstance"
}

// 分析请求接口
export interface AnalyzeObjectRequest {
  targetType?: AnalysisTargetType;
  targetPath?: string;
  includeChildren?: boolean;
  includeComponentDetails?: boolean;
  maxDepth?: number;
  includeDependencies?: boolean;
  addChildOperations?: AddChildObjectOperation[];
}

// 添加子对象操作
export interface AddChildObjectOperation {
  childName: string;
  objectType: ChildObjectType;
  position?: { x: number; y: number; z: number };
  components?: string[];
  componentProperties?: Record<string, any>;
}

// 物体分析响应
export interface AnalyzeObjectResponse {
  success: boolean;
  errorMessage?: string;
  analysisType?: string;
  mainObject?: ObjectAnalysisInfo;
  childObjects?: ObjectAnalysisInfo[];
  dependencies?: AssetDependencyInfo[];
  suggestedActions?: string[];
  analysisSummary?: string;
}

// 物体分析信息
export interface ObjectAnalysisInfo {
  name: string;
  type: string;
  hierarchyPath: string;
  isActive: boolean;
  transform?: TransformInfo;
  components?: ComponentInfo[];
  childCount: number;
  tag: string;
  layer: number;
  assetInfo?: AssetInfo;
}

// Transform信息
export interface TransformInfo {
  position: { x: number; y: number; z: number };
  rotation: { x: number; y: number; z: number };
  scale: { x: number; y: number; z: number };
  localPosition: { x: number; y: number; z: number };
  localRotation: { x: number; y: number; z: number };
  localScale: { x: number; y: number; z: number };
}

// 组件信息
export interface ComponentInfo {
  name: string;
  type: string;
  enabled: boolean;
  description: string;
  properties?: ComponentProperty[];
}

// 组件属性
export interface ComponentProperty {
  name: string;
  value: string;
  type: string;
  isDefault: boolean;
}

// 资源信息
export interface AssetInfo {
  assetPath: string;
  assetType: string;
  fileSize: number;
  isPrefab: boolean;
  importSettings?: string;
}

// 资源依赖信息
export interface AssetDependencyInfo {
  assetPath: string;
  dependencyType: string;
  isMissing: boolean;
}

// Zod schema for validation
const AnalyzeObjectSchema = z.object({
  targetType: z.enum(['Selected', 'SceneObject', 'ProjectAsset', 'SpecificPath']).default('Selected').describe("分析目标类型"),
  targetPath: z.string().optional().describe("目标路径（可选）"),
  includeChildren: z.boolean().default(true).describe("是否包含子物体分析"),
  includeComponentDetails: z.boolean().default(true).describe("是否包含组件详细信息"),
  maxDepth: z.number().min(1).max(10).default(5).describe("分析深度限制"),
  includeDependencies: z.boolean().default(false).describe("是否包含资源依赖分析"),
  addChildOperations: z.array(z.object({
    childName: z.string(),
    objectType: z.string(),
    position: z.object({ x: z.number(), y: z.number(), z: z.number() }).optional(),
    components: z.array(z.string()).optional()
  })).optional().describe("添加子对象操作（可选）")
});

/**
 * 注册物体分析工具到MCP服务器
 * 该工具能够分析Unity场景物体或Project资源的详细信息
 * 
 * @param server 要注册工具的McpServer实例
 * @param mcpUnity Unity通信桥接实例
 * @param logger 日志记录器实例
 */
export function registerAnalyzeObjectTool(
  server: McpServer,
  mcpUnity: McpUnity,
  logger: Logger
) {
  server.tool(
    'analyze_object',
    '分析Unity场景物体或Project资源的详细信息，包括组件、属性、层级结构等',
    {
      targetType: AnalyzeObjectSchema.shape.targetType,
      targetPath: AnalyzeObjectSchema.shape.targetPath,
      includeChildren: AnalyzeObjectSchema.shape.includeChildren,
      includeComponentDetails: AnalyzeObjectSchema.shape.includeComponentDetails,
      maxDepth: AnalyzeObjectSchema.shape.maxDepth,
      includeDependencies: AnalyzeObjectSchema.shape.includeDependencies,
      addChildOperations: AnalyzeObjectSchema.shape.addChildOperations
    },
    async ({ targetType, targetPath, includeChildren, includeComponentDetails, maxDepth, includeDependencies, addChildOperations }) => {
      logger.info(`Analyzing object: ${targetType}${targetPath ? ` - ${targetPath}` : ''}`);
      
      try {
        // 验证输入参数
        const validatedInput = AnalyzeObjectSchema.parse({
          targetType,
          targetPath,
          includeChildren,
          includeComponentDetails,
          maxDepth,
          includeDependencies,
          addChildOperations
        });

        // 构建请求对象
        const request = {
          targetType: validatedInput.targetType,
          targetPath: validatedInput.targetPath || "",
          includeChildren: validatedInput.includeChildren,
          includeComponentDetails: validatedInput.includeComponentDetails,
          maxDepth: validatedInput.maxDepth,
          includeDependencies: validatedInput.includeDependencies,
          addChildOperations: validatedInput.addChildOperations || []
        };

        // 发送请求到Unity Editor
        const response = await mcpUnity.sendRequest({
          method: 'analyze_object',
          params: request
        });

        if (response.success) {
          logger.info(`Successfully analyzed object: ${response.analysisType}`);
          
          // 生成详细的分析报告
          const reportContent = generateAnalysisReport(response);
          
          return {
            content: [
              {
                type: 'text' as const,
                text: reportContent
              }
            ]
          };
        } else {
          throw new McpUnityError(
            ErrorType.TOOL_EXECUTION,
            `Failed to analyze object: ${response.errorMessage || 'Unknown error'}`
          );
        }

      } catch (error) {
        logger.error(`Error analyzing object: ${error instanceof Error ? error.message : String(error)}`);
        
        if (error instanceof McpUnityError) {
          throw error;
        }
        
        throw new McpUnityError(
          ErrorType.TOOL_EXECUTION,
          `Failed to analyze object: ${error instanceof Error ? error.message : String(error)}`
        );
      }
    }
  );
}

/**
 * 解析分析请求参数
 */
function parseAnalyzeRequest(args: any): AnalyzeObjectRequest {
  const request: AnalyzeObjectRequest = {};
  
  // 分析目标类型
  if (args.targetType) {
    const targetType = args.targetType.toString();
    if (Object.values(AnalysisTargetType).includes(targetType as AnalysisTargetType)) {
      request.targetType = targetType as AnalysisTargetType;
    }
  } else {
    request.targetType = AnalysisTargetType.Selected; // 默认分析选中的物体
  }
  
  // 目标路径
  if (args.targetPath) {
    request.targetPath = args.targetPath.toString();
  }
  
  // 分析选项
  request.includeChildren = args.includeChildren !== false; // 默认包含子物体
  request.includeComponentDetails = args.includeComponentDetails !== false; // 默认包含组件详情
  request.maxDepth = Math.min(Math.max(args.maxDepth || 5, 1), 10); // 限制深度在1-10之间
  request.includeDependencies = args.includeDependencies === true; // 默认不包含依赖
  
  // 添加子对象操作
  if (args.addChildOperations && Array.isArray(args.addChildOperations)) {
    request.addChildOperations = parseChildOperations(args.addChildOperations);
  }
  
  return request;
}

/**
 * 解析子对象操作
 */
function parseChildOperations(operations: any[]): AddChildObjectOperation[] {
  return operations.map(op => {
    const operation: AddChildObjectOperation = {
      childName: op.childName || "New Object",
      objectType: op.objectType || ChildObjectType.Empty
    };
    
    if (op.position) {
      operation.position = {
        x: op.position.x || 0,
        y: op.position.y || 0,
        z: op.position.z || 0
      };
    }
    
    if (op.components && Array.isArray(op.components)) {
      operation.components = op.components.map((c: any) => c.toString());
    }
    
    if (op.componentProperties && typeof op.componentProperties === 'object') {
      operation.componentProperties = op.componentProperties;
    }
    
    return operation;
  });
}

/**
 * 格式化分析响应
 */
function formatAnalysisResponse(response: any): AnalyzeObjectResponse {
  const formattedResponse: AnalyzeObjectResponse = {
    success: response.success || false,
    errorMessage: response.errorMessage
  };
  
  if (response.analysisType) {
    formattedResponse.analysisType = response.analysisType;
  }
  
  if (response.mainObject) {
    formattedResponse.mainObject = formatObjectInfo(response.mainObject);
  }
  
  if (response.childObjects && Array.isArray(response.childObjects)) {
    formattedResponse.childObjects = response.childObjects.map(formatObjectInfo);
  }
  
  if (response.dependencies && Array.isArray(response.dependencies)) {
    formattedResponse.dependencies = response.dependencies.map(formatDependencyInfo);
  }
  
  if (response.suggestedActions && Array.isArray(response.suggestedActions)) {
    formattedResponse.suggestedActions = response.suggestedActions;
  }
  
  if (response.analysisSummary) {
    formattedResponse.analysisSummary = response.analysisSummary;
  }
  
  return formattedResponse;
}

/**
 * 格式化物体信息
 */
function formatObjectInfo(obj: any): ObjectAnalysisInfo {
  const info: ObjectAnalysisInfo = {
    name: obj.name || "Unknown",
    type: obj.type || "Unknown",
    hierarchyPath: obj.hierarchyPath || "",
    isActive: obj.isActive || false,
    childCount: obj.childCount || 0,
    tag: obj.tag || "Untagged",
    layer: obj.layer || 0
  };
  
  if (obj.transform) {
    info.transform = {
      position: obj.transform.position || { x: 0, y: 0, z: 0 },
      rotation: obj.transform.rotation || { x: 0, y: 0, z: 0 },
      scale: obj.transform.scale || { x: 1, y: 1, z: 1 },
      localPosition: obj.transform.localPosition || { x: 0, y: 0, z: 0 },
      localRotation: obj.transform.localRotation || { x: 0, y: 0, z: 0 },
      localScale: obj.transform.localScale || { x: 1, y: 1, z: 1 }
    };
  }
  
  if (obj.components && Array.isArray(obj.components)) {
    info.components = obj.components.map(formatComponentInfo);
  }
  
  if (obj.assetInfo) {
    info.assetInfo = {
      assetPath: obj.assetInfo.assetPath || "",
      assetType: obj.assetInfo.assetType || "Unknown",
      fileSize: obj.assetInfo.fileSize || 0,
      isPrefab: obj.assetInfo.isPrefab || false,
      importSettings: obj.assetInfo.importSettings
    };
  }
  
  return info;
}

/**
 * 格式化组件信息
 */
function formatComponentInfo(comp: any): ComponentInfo {
  const info: ComponentInfo = {
    name: comp.name || "Unknown Component",
    type: comp.type || "Unknown",
    enabled: comp.enabled !== false,
    description: comp.description || ""
  };
  
  if (comp.properties && Array.isArray(comp.properties)) {
    info.properties = comp.properties.map((prop: any) => ({
      name: prop.name || "",
      value: prop.value || "",
      type: prop.type || "",
      isDefault: prop.isDefault !== false
    }));
  }
  
  return info;
}

/**
 * 格式化依赖信息
 */
function formatDependencyInfo(dep: any): AssetDependencyInfo {
  return {
    assetPath: dep.assetPath || "",
    dependencyType: dep.dependencyType || "Unknown",
    isMissing: dep.isMissing || false
  };
}

/**
 * 生成物体分析的详细报告
 */
export function generateAnalysisReport(response: AnalyzeObjectResponse): string {
  if (!response.success || !response.mainObject) {
    return `分析失败: ${response.errorMessage || "未知错误"}`;
  }

  let report = "# Unity物体分析报告\n\n";
  
  // 主对象信息
  const mainObj = response.mainObject;
  report += `## 主对象: ${mainObj.name}\n`;
  report += `- **类型**: ${mainObj.type}\n`;
  report += `- **层级路径**: ${mainObj.hierarchyPath}\n`;
  report += `- **激活状态**: ${mainObj.isActive ? "激活" : "未激活"}\n`;
  report += `- **标签**: ${mainObj.tag}\n`;
  report += `- **层级**: ${mainObj.layer}\n`;
  report += `- **子物体数量**: ${mainObj.childCount}\n\n`;
  
  // Transform信息
  if (mainObj.transform) {
    const t = mainObj.transform;
    report += `### Transform信息\n`;
    report += `- **世界位置**: (${t.position.x.toFixed(2)}, ${t.position.y.toFixed(2)}, ${t.position.z.toFixed(2)})\n`;
    report += `- **本地位置**: (${t.localPosition.x.toFixed(2)}, ${t.localPosition.y.toFixed(2)}, ${t.localPosition.z.toFixed(2)})\n`;
    report += `- **旋转**: (${t.rotation.x.toFixed(2)}, ${t.rotation.y.toFixed(2)}, ${t.rotation.z.toFixed(2)})\n`;
    report += `- **缩放**: (${t.scale.x.toFixed(2)}, ${t.scale.y.toFixed(2)}, ${t.scale.z.toFixed(2)})\n\n`;
  }
  
  // 组件信息
  if (mainObj.components && mainObj.components.length > 0) {
    report += `### 组件列表 (${mainObj.components.length}个)\n`;
    
    mainObj.components.forEach((comp, index) => {
      report += `${index + 1}. **${comp.name}** (${comp.enabled ? "启用" : "禁用"})\n`;
      report += `   - 类型: ${comp.type}\n`;
      if (comp.description) {
        report += `   - 描述: ${comp.description}\n`;
      }
      
      if (comp.properties && comp.properties.length > 0) {
        report += `   - 主要属性:\n`;
        comp.properties.slice(0, 5).forEach(prop => { // 只显示前5个属性
          report += `     - ${prop.name}: ${prop.value} (${prop.type})\n`;
        });
        if (comp.properties.length > 5) {
          report += `     - ... 还有 ${comp.properties.length - 5} 个属性\n`;
        }
      }
      report += "\n";
    });
  }
  
  // 资源信息
  if (mainObj.assetInfo) {
    const asset = mainObj.assetInfo;
    report += `### 资源信息\n`;
    report += `- **路径**: ${asset.assetPath}\n`;
    report += `- **类型**: ${asset.assetType}\n`;
    report += `- **文件大小**: ${formatFileSize(asset.fileSize)}\n`;
    report += `- **是否为Prefab**: ${asset.isPrefab ? "是" : "否"}\n\n`;
  }
  
  // 子物体信息
  if (response.childObjects && response.childObjects.length > 0) {
    report += `## 子物体 (${response.childObjects.length}个)\n`;
    
    response.childObjects.forEach((child, index) => {
      report += `${index + 1}. **${child.name}** (${child.type})\n`;
      report += `   - 路径: ${child.hierarchyPath}\n`;
      report += `   - 状态: ${child.isActive ? "激活" : "未激活"}\n`;
      if (child.components && child.components.length > 0) {
        report += `   - 组件: ${child.components.map(c => c.name).join(", ")}\n`;
      }
      report += "\n";
    });
  }
  
  // 依赖关系
  if (response.dependencies && response.dependencies.length > 0) {
    report += `## 依赖关系 (${response.dependencies.length}个)\n`;
    
    response.dependencies.forEach((dep, index) => {
      report += `${index + 1}. **${dep.dependencyType}**: ${dep.assetPath}`;
      if (dep.isMissing) {
        report += " ⚠️ **缺失**";
      }
      report += "\n";
    });
    report += "\n";
  }
  
  // 建议操作
  if (response.suggestedActions && response.suggestedActions.length > 0) {
    report += `## 建议操作\n`;
    response.suggestedActions.forEach((action, index) => {
      report += `${index + 1}. ${action}\n`;
    });
    report += "\n";
  }
  
  // 分析摘要
  if (response.analysisSummary) {
    report += `## 分析摘要\n`;
    report += `${response.analysisSummary}\n`;
  }
  
  return report;
}

/**
 * 格式化文件大小
 */
function formatFileSize(bytes: number): string {
  if (bytes === 0) return "0 Bytes";
  
  const k = 1024;
  const sizes = ["Bytes", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + " " + sizes[i];
}

/**
 * 解析自然语言的分析指令
 */
export function parseAnalysisCommand(command: string): AnalyzeObjectRequest {
  const request: AnalyzeObjectRequest = {
    targetType: AnalysisTargetType.Selected,
    includeChildren: true,
    includeComponentDetails: true,
    maxDepth: 5,
    includeDependencies: false
  };
  
  const lowerCommand = command.toLowerCase();
  
  // 分析目标类型
  if (lowerCommand.includes("选中") || lowerCommand.includes("当前") || lowerCommand.includes("selected")) {
    request.targetType = AnalysisTargetType.Selected;
  } else if (lowerCommand.includes("project") || lowerCommand.includes("资源") || lowerCommand.includes("资产")) {
    request.targetType = AnalysisTargetType.ProjectAsset;
  } else if (lowerCommand.includes("场景") || lowerCommand.includes("scene")) {
    request.targetType = AnalysisTargetType.SceneObject;
  }
  
  // 分析选项
  if (lowerCommand.includes("不包含子物体") || lowerCommand.includes("不要子物体") || lowerCommand.includes("no children")) {
    request.includeChildren = false;
  }
  
  if (lowerCommand.includes("简单分析") || lowerCommand.includes("不要详情") || lowerCommand.includes("no details")) {
    request.includeComponentDetails = false;
  }
  
  if (lowerCommand.includes("包含依赖") || lowerCommand.includes("分析依赖") || lowerCommand.includes("dependencies")) {
    request.includeDependencies = true;
  }
  
  // 深度限制
  const depthMatch = lowerCommand.match(/深度\s*(\d+)|depth\s*(\d+)/);
  if (depthMatch) {
    const depth = parseInt(depthMatch[1] || depthMatch[2]);
    if (depth >= 1 && depth <= 10) {
      request.maxDepth = depth;
    }
  }
  
  return request;
} 