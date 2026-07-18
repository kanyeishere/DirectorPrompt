using DirectorPrompt.Agents.Config;
using DirectorPrompt.Agents.Retrieval;
using DirectorPrompt.Domain.Configurations;
using DirectorPrompt.Domain.Models;
using DirectorPrompt.Domain.Repositories;
using DirectorPrompt.Domain.Services;
using ModelContextProtocol.Server;
using Serilog;

namespace DirectorPrompt.Agents.MCP;

public sealed class MCPProjectTools
(
    IProjectContentService        projectContentService,
    IProjectPortService           projectPortService,
    IKnowledgeRepository          knowledgeRepository,
    EmbeddingIndexService         embeddingIndexService,
    AgentConfigResolver           agentConfigResolver,
    UserSettings                  userSettings
)
{
    private const int WRITE_QUEUE_CAPACITY = 64;

    private readonly SemaphoreSlim writeLock = new(1, 1);

    private int pendingWrites;

    [McpServerTool(Name = "list_projects")]
    public Task<MCPToolResponse> ListProjectsAsync(CancellationToken cancellationToken) =>
        ExecuteAsync
        (
            "list_projects",
            null,
            async () => await projectContentService.ListProjectsAsync(cancellationToken)
        );

    [McpServerTool(Name = "get_project")]
    public Task<MCPToolResponse> GetProjectAsync(long projectID, CancellationToken cancellationToken) =>
        ExecuteAsync
        (
            "get_project",
            new { projectID },
            async () =>
            {
                var snapshot = await projectContentService.GetProjectAsync(projectID, cancellationToken);

                if (snapshot is null)
                    throw new InvalidOperationException($"项目不存在: ID={projectID}");

                return snapshot;
            }
        );

    [McpServerTool(Name = "create_project")]
    public Task<MCPToolResponse> CreateProjectAsync
    (
        string            name,
        string            description,
        string            openingMessage,
        ProjectBlueprint? blueprint         = null,
        bool              dryRun            = false,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "create_project",
            new { name, description, openingMessage, blueprint, dryRun },
            async () =>
            {
                var result = await projectContentService.CreateProjectAsync
                             (
                                 name,
                                 description,
                                 openingMessage,
                                 blueprint,
                                 dryRun,
                                 cancellationToken
                             );

                if (dryRun)
                    return result;

                var indexStatus = await TryIndexKnowledgeAsync(result.Project.ID, cancellationToken);
                return result with { IndexStatus = indexStatus };
            },
            true
        );

    [McpServerTool(Name = "update_project")]
    public Task<MCPToolResponse> UpdateProjectAsync
    (
        long              projectID,
        string?           name              = null,
        string?           description       = null,
        string?           openingMessage    = null,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "update_project",
            new { projectID, name, description, openingMessage },
            async () =>
            {
                return await projectContentService.PatchProjectAsync
                       (
                           projectID,
                           new ProjectPatch
                           {
                               Name           = name,
                               Description    = description,
                               OpeningMessage = openingMessage
                           },
                           cancellationToken
                       );
            },
            true
        );

    [McpServerTool(Name = "delete_project")]
    public Task<MCPToolResponse> DeleteProjectAsync(long projectID, CancellationToken cancellationToken = default) =>
        ExecuteAsync
        (
            "delete_project",
            new { projectID },
            async () =>
            {
                return await projectContentService.DeleteProjectAsync(projectID, cancellationToken);
            },
            true
        );

    [McpServerTool(Name = "import_project")]
    public Task<MCPToolResponse> ImportProjectAsync
    (
        string              sourcePath,
        ProjectImportFormat format,
        CancellationToken   cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "import_project",
            new { sourcePath, format },
            async () =>
            {
                var path = ValidateLocalPath(sourcePath, nameof(sourcePath));

                if (!File.Exists(path))
                    throw new FileNotFoundException("导入文件不存在", path);

                var result = format switch
                {
                    ProjectImportFormat.DirectorPromptPackage    => await projectPortService.ImportAsync(path, cancellationToken),
                    ProjectImportFormat.SillyTavernCharacterCard => await projectPortService.ImportSillyTavernAsync(path, cancellationToken),
                    _                                            => throw new ArgumentOutOfRangeException(nameof(format))
                };

                projectContentService.NotifyProjectChanged(result.ProjectID);
                return result;
            },
            true
        );

    [McpServerTool(Name = "export_project")]
    public Task<MCPToolResponse> ExportProjectAsync
    (
        long              projectID,
        string            destinationPath,
        bool              overwrite,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "export_project",
            new { projectID, destinationPath, overwrite },
            async () =>
            {
                var path = ValidateLocalPath(destinationPath, nameof(destinationPath));

                if (File.Exists(path) && !overwrite)
                    throw new IOException($"导出目标已存在: {path}");

                var directory = Path.GetDirectoryName(path);

                if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                    throw new DirectoryNotFoundException($"导出目录不存在: {directory}");

                if (File.Exists(path))
                    File.Delete(path);

                await projectPortService.ExportAsync(projectID, path, cancellationToken);
                return new { projectID, destinationPath = path };
            },
            true
        );

    [McpServerTool(Name = "manage_knowledge_group")]
    public Task<MCPToolResponse> ManageKnowledgeGroupAsync
    (
        long                 projectID,
        ProjectContentAction action,
        KnowledgeGroup?      group             = null,
        KnowledgeGroupPatch? patch             = null,
        long?                groupID           = null,
        CancellationToken    cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "manage_knowledge_group",
            new { projectID, action, group, patch, groupID },
            async () =>
            {
                return action switch
                {
                    ProjectContentAction.Create => patch is not null ?
                                                  throw new ArgumentException("知识分组创建不支持 patch", nameof(patch)) :
                                                  await projectContentService.ManageKnowledgeGroupAsync
                                                  (
                                                      projectID,
                                                      action,
                                                      group ?? throw new ArgumentException("缺少知识分组创建数据", nameof(group)),
                                                      null,
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Update => group is not null ?
                                                  throw new ArgumentException("知识分组更新仅支持 patch", nameof(group)) :
                                                  await projectContentService.PatchKnowledgeGroupAsync
                                                  (
                                                      projectID,
                                                      groupID ?? throw new ArgumentException("缺少知识分组 ID", nameof(groupID)),
                                                      patch ?? throw new ArgumentException("缺少知识分组更新数据", nameof(patch)),
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Delete => await projectContentService.ManageKnowledgeGroupAsync
                                                   (
                                                       projectID,
                                                       action,
                                                       null,
                                                       groupID ?? throw new ArgumentException("缺少知识分组 ID", nameof(groupID)),
                                                       cancellationToken
                                                   ),
                    _                           => throw new ArgumentOutOfRangeException(nameof(action))
                };
            },
            true
        );

    [McpServerTool(Name = "manage_knowledge_entry")]
    public Task<MCPToolResponse> ManageKnowledgeEntryAsync
    (
        long                 projectID,
        ProjectContentAction action,
        KnowledgeEntry?      entry             = null,
        KnowledgeEntryPatch? patch             = null,
        long?                entryID           = null,
        CancellationToken    cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "manage_knowledge_entry",
            new { projectID, action, entry, patch, entryID },
            async () =>
            {
                var result = action switch
                {
                    ProjectContentAction.Create => patch is not null ?
                                                  throw new ArgumentException("知识条目创建不支持 patch", nameof(patch)) :
                                                  await projectContentService.ManageKnowledgeEntryAsync
                                                  (
                                                      projectID,
                                                      action,
                                                      entry ?? throw new ArgumentException("缺少知识条目创建数据", nameof(entry)),
                                                      null,
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Update => entry is not null ?
                                                  throw new ArgumentException("知识条目更新仅支持 patch", nameof(entry)) :
                                                  await projectContentService.PatchKnowledgeEntryAsync
                                                  (
                                                      projectID,
                                                      entryID ?? throw new ArgumentException("缺少知识条目 ID", nameof(entryID)),
                                                      patch ?? throw new ArgumentException("缺少知识条目更新数据", nameof(patch)),
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Delete => await projectContentService.ManageKnowledgeEntryAsync
                                                   (
                                                       projectID,
                                                       action,
                                                       null,
                                                       entryID ?? throw new ArgumentException("缺少知识条目 ID", nameof(entryID)),
                                                       cancellationToken
                                                   ),
                    _                           => throw new ArgumentOutOfRangeException(nameof(action))
                };
                var indexStatus = action is ProjectContentAction.Create or ProjectContentAction.Update ?
                                      await TryIndexKnowledgeAsync(projectID, cancellationToken) :
                                      "not_required";

                return new { entry = result, indexStatus };
            },
            true
        );

    [McpServerTool(Name = "manage_character_category")]
    public Task<MCPToolResponse> ManageCharacterCategoryAsync
    (
        long                 projectID,
        ProjectContentAction action,
        CharacterCategory?   category          = null,
        CharacterCategoryPatch? patch          = null,
        long?                categoryID        = null,
        CancellationToken    cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "manage_character_category",
            new { projectID, action, category, patch, categoryID },
            async () =>
            {
                return action switch
                {
                    ProjectContentAction.Create => patch is not null ?
                                                  throw new ArgumentException("人物分类创建不支持 patch", nameof(patch)) :
                                                  await projectContentService.ManageCharacterCategoryAsync
                                                  (
                                                      projectID,
                                                      action,
                                                      category ?? throw new ArgumentException("缺少人物分类创建数据", nameof(category)),
                                                      null,
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Update => category is not null ?
                                                  throw new ArgumentException("人物分类更新仅支持 patch", nameof(category)) :
                                                  await projectContentService.PatchCharacterCategoryAsync
                                                  (
                                                      projectID,
                                                      categoryID ?? throw new ArgumentException("缺少人物分类 ID", nameof(categoryID)),
                                                      patch ?? throw new ArgumentException("缺少人物分类更新数据", nameof(patch)),
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Delete => await projectContentService.ManageCharacterCategoryAsync
                                                   (
                                                       projectID,
                                                       action,
                                                       null,
                                                       categoryID ?? throw new ArgumentException("缺少人物分类 ID", nameof(categoryID)),
                                                       cancellationToken
                                                   ),
                    _                           => throw new ArgumentOutOfRangeException(nameof(action))
                };
            },
            true
        );

    [McpServerTool(Name = "manage_state_attribute")]
    public Task<MCPToolResponse> ManageStateAttributeAsync
    (
        long                      projectID,
        ProjectContentAction      action,
        StateAttributeDefinition? definition        = null,
        StateAttributePatch?      patch             = null,
        long?                     attributeID       = null,
        CancellationToken         cancellationToken = default
    ) =>
        ExecuteAsync
        (
            "manage_state_attribute",
            new { projectID, action, definition, patch, attributeID },
            async () =>
            {
                var result = action switch
                {
                    ProjectContentAction.Create => patch is not null ?
                                                  throw new ArgumentException("状态属性创建不支持 patch", nameof(patch)) :
                                                  await projectContentService.ManageStateAttributeAsync
                                                  (
                                                      projectID,
                                                      action,
                                                      definition ?? throw new ArgumentException("缺少状态属性创建数据", nameof(definition)),
                                                      null,
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Update => definition is not null ?
                                                  throw new ArgumentException("状态属性更新仅支持 patch", nameof(definition)) :
                                                  await projectContentService.PatchStateAttributeAsync
                                                  (
                                                      projectID,
                                                      attributeID ?? throw new ArgumentException("缺少状态属性 ID", nameof(attributeID)),
                                                      patch ?? throw new ArgumentException("缺少状态属性更新数据", nameof(patch)),
                                                      cancellationToken
                                                  ),
                    ProjectContentAction.Delete => await projectContentService.ManageStateAttributeAsync
                                                   (
                                                       projectID,
                                                       action,
                                                       null,
                                                       attributeID ?? throw new ArgumentException("缺少状态属性 ID", nameof(attributeID)),
                                                       cancellationToken
                                                   ),
                    _                           => throw new ArgumentOutOfRangeException(nameof(action))
                };

                if (action == ProjectContentAction.Delete)
                    return new { attributeID = result.ID, deleted = true };

                var snapshot = await projectContentService.GetProjectAsync(projectID, cancellationToken) ??
                               throw new InvalidOperationException($"项目不存在: ID={projectID}");

                return snapshot.StateAttributes.First(attribute => attribute.ID == result.ID);
            },
            true
        );

    private async Task<string> TryIndexKnowledgeAsync(long projectID, CancellationToken cancellationToken)
    {
        var embeddingConfig = agentConfigResolver.ResolveEmbedding(userSettings.EmbeddingConfig);

        if (embeddingConfig is null)
            return "not_configured";

        try
        {
            var entries = await knowledgeRepository.GetPendingIndexEntriesAsync
                          (
                              projectID,
                              embeddingConfig.Fingerprint,
                              100,
                              cancellationToken
                          );
            await embeddingIndexService.IndexKnowledgeAsync(entries, embeddingConfig, cancellationToken);
            return "completed";
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "MCP 知识索引失败: {ProjectID}", projectID);
            return $"failed: {exception.Message}";
        }
    }

    private static string ValidateLocalPath(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("路径不能为空", parameterName);

        var path = Path.GetFullPath(value);

        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("仅支持本机绝对路径", parameterName);

        return path;
    }

    private async Task<MCPToolResponse> ExecuteAsync
    (
        string              toolName,
        object?             parameters,
        Func<Task<object?>> operation,
        bool                isWriteOperation = false
    )
    {
        if (!isWriteOperation)
            return await ExecuteCoreAsync(toolName, parameters, operation);

        if (Interlocked.Increment(ref pendingWrites) > WRITE_QUEUE_CAPACITY)
        {
            Interlocked.Decrement(ref pendingWrites);
            Log.Warning("内部 MCP 写入队列已满: {ToolName}, 参数={@Parameters}", toolName, parameters);
            return new MCPToolResponse(1, false, null, "MCP 写入请求过多，请稍后重试", "MCP_WRITE_QUEUE_FULL");
        }

        try
        {
            await writeLock.WaitAsync();

            try
            {
                return await ExecuteCoreAsync(toolName, parameters, operation);
            }
            finally
            {
                writeLock.Release();
            }
        }
        finally
        {
            Interlocked.Decrement(ref pendingWrites);
        }
    }

    private static async Task<MCPToolResponse> ExecuteCoreAsync
    (
        string              toolName,
        object?             parameters,
        Func<Task<object?>> operation
    )
    {
        Log.Information("内部 MCP 调用: {ToolName}, 参数={@Parameters}", toolName, parameters);

        try
        {
            var data     = await operation();
            var response = new MCPToolResponse(1, true, data);
            Log.Information("内部 MCP 返回: {ToolName}, 响应={@Response}", toolName, response);
            return response;
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "内部 MCP 调用失败: {ToolName}, 参数={@Parameters}", toolName, parameters);
            var response = new MCPToolResponse(1, false, null, exception.Message);
            Log.Information("内部 MCP 返回: {ToolName}, 响应={@Response}", toolName, response);
            return response;
        }
    }
}
