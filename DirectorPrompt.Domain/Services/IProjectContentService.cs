using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Services;

public interface IProjectContentService
{
    event Action<ProjectContentChange>? Changed;

    IDisposable BeginChangeBatch();

    void NotifyProjectChanged(long projectID, bool isDeleted = false);

    Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken = default);

    Task<ProjectContentSnapshot?> GetProjectAsync(long projectID, CancellationToken cancellationToken = default);

    Task<ProjectBlueprintResult> CreateProjectAsync
    (
        string            name,
        string            description,
        string            openingMessage,
        ProjectBlueprint? blueprint,
        bool              dryRun,
        CancellationToken cancellationToken = default
    );

    Task<Project> UpdateProjectAsync(Project project, CancellationToken cancellationToken = default);

    Task<Project> PatchProjectAsync
    (
        long              projectID,
        ProjectPatch      patch,
        CancellationToken cancellationToken = default
    );

    Task<ProjectDeleteSummary> DeleteProjectAsync(long projectID, CancellationToken cancellationToken = default);

    Task<KnowledgeGroup> ManageKnowledgeGroupAsync
    (
        long                 projectID,
        ProjectContentAction action,
        KnowledgeGroup?      group,
        long?                groupID,
        CancellationToken    cancellationToken = default
    );

    Task<KnowledgeGroup> PatchKnowledgeGroupAsync
    (
        long                projectID,
        long                groupID,
        KnowledgeGroupPatch patch,
        CancellationToken   cancellationToken = default
    );

    Task<KnowledgeEntry> ManageKnowledgeEntryAsync
    (
        long                 projectID,
        ProjectContentAction action,
        KnowledgeEntry?      entry,
        long?                entryID,
        CancellationToken    cancellationToken = default
    );

    Task<KnowledgeEntry> PatchKnowledgeEntryAsync
    (
        long                projectID,
        long                entryID,
        KnowledgeEntryPatch patch,
        CancellationToken   cancellationToken = default
    );

    Task<CharacterCategory> ManageCharacterCategoryAsync
    (
        long                 projectID,
        ProjectContentAction action,
        CharacterCategory?   category,
        long?                categoryID,
        CancellationToken    cancellationToken = default
    );

    Task<CharacterCategory> PatchCharacterCategoryAsync
    (
        long                   projectID,
        long                   categoryID,
        CharacterCategoryPatch patch,
        CancellationToken      cancellationToken = default
    );

    Task<StateAttribute> ManageStateAttributeAsync
    (
        long                      projectID,
        ProjectContentAction      action,
        StateAttributeDefinition? definition,
        long?                     attributeID,
        CancellationToken         cancellationToken = default
    );

    Task<StateAttribute> PatchStateAttributeAsync
    (
        long                projectID,
        long                attributeID,
        StateAttributePatch patch,
        CancellationToken   cancellationToken = default
    );
}
