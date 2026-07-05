using System.Text.Json;
using DirectorPrompt.Domain.Models;
using DirectorPrompt.Domain.Repositories;
using DirectorPrompt.Domain.Services;
using Microsoft.Extensions.AI;

namespace DirectorPrompt.Agents.Tools;

public sealed class KnowledgeTools
{
    private readonly IKnowledgeRepository knowledgeRepository;
    private readonly IEmbeddingService    embeddingService;

    public KnowledgeTools(IKnowledgeRepository knowledgeRepository, IEmbeddingService embeddingService)
    {
        this.knowledgeRepository = knowledgeRepository;
        this.embeddingService    = embeddingService;
    }

    public IList<AIFunction> Create(ToolExecutionContext context) =>
    [
        AIFunctionFactory.Create
        (
            (string query, int? topK) => QueryKnowledgeAsync(context, query, topK),
            "query_knowledge",
            "语义检索知识条目。query: 检索内容; topK: 返回条数, 默认 8"
        )
    ];

    private async Task<string> QueryKnowledgeAsync(ToolExecutionContext context, string query, int? topK)
    {
        var entries = await knowledgeRepository.GetActiveEntriesAsync(context.ProjectID);

        if (entries.Count == 0)
            return JsonSerializer.Serialize(Array.Empty<object>());

        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query);
        var limit          = topK ?? 8;

        var scored = entries
                     .Select
                     (e => new
                         {
                             entry     = e,
                             relevance = ComputeRelevance(query, e)
                         }
                     )
                     .OrderByDescending(x => x.relevance)
                     .Take(limit)
                     .Select
                     (x => new
                         {
                             title     = x.entry.Title,
                             content   = x.entry.Content,
                             tags      = x.entry.Tags,
                             relevance = Math.Round(x.relevance, 4)
                         }
                     );

        return JsonSerializer.Serialize(scored);
    }

    private static float ComputeRelevance(string query, KnowledgeEntry entry)
    {
        var queryLower   = query.ToLowerInvariant();
        var titleLower   = entry.Title.ToLowerInvariant();
        var contentLower = entry.Content.ToLowerInvariant();

        var score = 0f;

        if (titleLower.Contains(queryLower))
            score += 0.5f;

        if (contentLower.Contains(queryLower))
            score += 0.3f;

        foreach (var tag in entry.Tags)
        {
            if (queryLower.Contains(tag.ToLowerInvariant()))
            {
                score += 0.2f;
                break;
            }
        }

        return score;
    }
}
