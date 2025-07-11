// Import MCP SDK components
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { McpUnity } from './unity/mcpUnity.js';
import { Logger, LogLevel } from './utils/logger.js';
import { registerMenuItemTool } from './tools/menuItemTool.js';
import { registerSelectGameObjectTool } from './tools/selectGameObjectTool.js';
import { registerAddPackageTool } from './tools/addPackageTool.js';
import { registerRunTestsTool } from './tools/runTestsTool.js';
import { registerSendConsoleLogTool } from './tools/sendConsoleLogTool.js';
import { registerGetConsoleLogsTool } from './tools/getConsoleLogsTool.js';
import { registerUpdateComponentTool } from './tools/updateComponentTool.js';
import { registerAddAssetToSceneTool } from './tools/addAssetToSceneTool.js';
import { registerUpdateGameObjectTool } from './tools/updateGameObjectTool.js';
import { registerAnalyzeProfilerTool } from './tools/analyzeProfilerTool.js';
import { registerAnalyzeSpecificFrameTool } from './tools/analyzeSpecificFrameTool.js';
import { registerAnalyzeModuleTool } from './tools/analyzeModuleTool.js';

// Import resource modules
import { registerGetGameObjectResource } from './resources/getGameObjectResource.js';
import { registerGetHierarchyResource } from './resources/getScenesHierarchyResource.js';
import { registerGetAssetsResource } from './resources/getAssetsResource.js';
import { registerGetPackagesResource } from './resources/getPackagesResource.js';
import { registerGetConsoleLogsResource } from './resources/getConsoleLogsResource.js';
import { registerGetTestsResource } from './resources/getTestsResource.js';
import { registerGetMenuItemsResource } from './resources/getMenuItemResource.js';
import { registerGetProfilerResource } from './resources/getProfilerResource.js';

// Import prompt modules
import { registerGameObjectHandlingPrompt } from './prompts/gameobjectHandlingPrompt.js';
import { registerUnityPerformanceAnalysisPrompt } from './prompts/unityPerformanceAnalysisPrompt.js';
import { registerUnityMcpGuidePrompt } from './prompts/unityMcpGuidePrompt.js';
import { registerUnityOperationsPrompt } from './prompts/unityOperationsPrompt.js';
import { registerUnityUIPrompt } from './prompts/unityUIPrompt.js';
import { registerUnityScriptingPrompt } from './prompts/unityScriptingPrompt.js';
import { registerUnityFrameAnalysisPrompt } from './prompts/unityFrameAnalysisPrompt.js';
import { registerUnityModuleAnalysisPrompt } from './prompts/unityModuleAnalysisPrompt.js';

// Initialize loggers
const serverLogger = new Logger('Server', LogLevel.INFO);
const unityLogger = new Logger('Unity', LogLevel.INFO);
const toolLogger = new Logger('Tools', LogLevel.INFO);
const resourceLogger = new Logger('Resources', LogLevel.INFO);

// Initialize the MCP server
const server = new McpServer (
  {
    name: "MCP Unity Server",
    version: "1.0.0"
  },
  {
    capabilities: {
      tools: {},
      resources: {},
      prompts: {},
    },
  }
);

// Initialize MCP HTTP bridge with Unity editor
const mcpUnity = new McpUnity(unityLogger);

// Register all tools into the MCP server
registerMenuItemTool(server, mcpUnity, toolLogger);
registerSelectGameObjectTool(server, mcpUnity, toolLogger);
registerAddPackageTool(server, mcpUnity, toolLogger);
registerRunTestsTool(server, mcpUnity, toolLogger);
registerSendConsoleLogTool(server, mcpUnity, toolLogger);
registerGetConsoleLogsTool(server, mcpUnity, toolLogger);
registerUpdateComponentTool(server, mcpUnity, toolLogger);
registerAddAssetToSceneTool(server, mcpUnity, toolLogger);
registerUpdateGameObjectTool(server, mcpUnity, toolLogger);
registerAnalyzeProfilerTool(server, mcpUnity, toolLogger);
registerAnalyzeSpecificFrameTool(server, mcpUnity, toolLogger);
registerAnalyzeModuleTool(server, mcpUnity, toolLogger);

// Register all resources into the MCP server
registerGetGameObjectResource(server, mcpUnity, resourceLogger);
registerGetHierarchyResource(server, mcpUnity, resourceLogger);
registerGetAssetsResource(server, mcpUnity, resourceLogger);
registerGetPackagesResource(server, mcpUnity, resourceLogger);
registerGetConsoleLogsResource(server, mcpUnity, resourceLogger);
registerGetTestsResource(server, mcpUnity, resourceLogger);
registerGetMenuItemsResource(server, mcpUnity, resourceLogger);
registerGetProfilerResource(server, mcpUnity, resourceLogger);

// Register all prompts into the MCP server
registerGameObjectHandlingPrompt(server);
registerUnityPerformanceAnalysisPrompt(server);
registerUnityMcpGuidePrompt(server);
registerUnityOperationsPrompt(server);
registerUnityUIPrompt(server);
registerUnityScriptingPrompt(server);
registerUnityFrameAnalysisPrompt(server);
registerUnityModuleAnalysisPrompt(server);

// Server startup function
async function startServer() {
  try {
    // Initialize STDIO transport for MCP client communication
    const stdioTransport = new StdioServerTransport();
    
    // Connect the server to the transport
    await server.connect(stdioTransport);

    serverLogger.info('MCP Server started');
    
    // Get the client name from the MCP server
    const clientName = server.server.getClientVersion()?.name || 'Unknown MCP Client';
    serverLogger.info(`Connected MCP client: ${clientName}`);
    
    // Start Unity Bridge connection with client name in headers
    await mcpUnity.start(clientName);
    
  } catch (error) {
    serverLogger.error('Failed to start server', error);
    process.exit(1);
  }
}

// Start the server
startServer();

// Handle shutdown
process.on('SIGINT', async () => {
  serverLogger.info('Shutting down...');
  await mcpUnity.stop();
  process.exit(0);
});

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
  serverLogger.error('Uncaught exception', error);
});

// Handle unhandled promise rejections
process.on('unhandledRejection', (reason) => {
  serverLogger.error('Unhandled rejection', reason);
});
