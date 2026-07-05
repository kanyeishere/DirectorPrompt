using DirectorPrompt.Domain.Models;
using DirectorPrompt.Domain.Services;

namespace DirectorPrompt.Agents;

public sealed class TimelineCalculator : ITimelineCalculator
{
    public const long GAP = 100000;

    public long CalculatePosition(long? afterSceneID, long? beforeSceneID, IReadOnlyList<Scene> existingScenes)
    {
        if (existingScenes.Count == 0)
            return 0;

        if (!afterSceneID.HasValue && !beforeSceneID.HasValue)
            return 0;

        if (afterSceneID.HasValue && !beforeSceneID.HasValue)
            return CalculateAfterTail(afterSceneID.Value, existingScenes);

        if (!afterSceneID.HasValue && beforeSceneID.HasValue)
            return CalculateBeforeHead(beforeSceneID.Value, existingScenes);

        return CalculateMidpoint(afterSceneID!.Value, beforeSceneID!.Value, existingScenes);
    }

    private static long CalculateAfterTail(long afterSceneID, IReadOnlyList<Scene> scenes)
    {
        var after         = FindScene(afterSceneID, scenes);
        var hasSceneAfter = scenes.Any(s => s.TimelinePosition > after.TimelinePosition);

        if (hasSceneAfter)
        {
            throw new ArgumentException
            (
                $"场景 {afterSceneID} 后面还有场景, 应同时指定 beforeSceneID 进行插入"
            );
        }

        return after.TimelinePosition + GAP;
    }

    private static long CalculateBeforeHead(long beforeSceneID, IReadOnlyList<Scene> scenes)
    {
        var before         = FindScene(beforeSceneID, scenes);
        var hasSceneBefore = scenes.Any(s => s.TimelinePosition < before.TimelinePosition);

        if (hasSceneBefore)
        {
            throw new ArgumentException
            (
                $"场景 {beforeSceneID} 前面还有场景, 应同时指定 afterSceneID 进行插入"
            );
        }

        return before.TimelinePosition - GAP;
    }

    private static long CalculateMidpoint(long afterSceneID, long beforeSceneID, IReadOnlyList<Scene> scenes)
    {
        var after  = FindScene(afterSceneID,  scenes);
        var before = FindScene(beforeSceneID, scenes);

        if (after.TimelinePosition >= before.TimelinePosition)
        {
            throw new ArgumentException
            (
                "afterSceneID 的 timelinePosition 必须小于 beforeSceneID 的 timelinePosition"
            );
        }

        var mid = (after.TimelinePosition + before.TimelinePosition) / 2;

        if (mid == after.TimelinePosition || mid == before.TimelinePosition)
        {
            throw new ArgumentException
            (
                $"场景 {afterSceneID} 和 {beforeSceneID} 之间的空间已耗尽, 无法插入新场景"
            );
        }

        return mid;
    }

    private static Scene FindScene(long sceneID, IReadOnlyList<Scene> scenes)
    {
        var scene = scenes.FirstOrDefault(s => s.ID == sceneID);

        if (scene is null)
            throw new ArgumentException($"场景 {sceneID} 不存在");

        return scene;
    }
}
