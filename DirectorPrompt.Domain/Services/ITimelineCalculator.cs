using DirectorPrompt.Domain.Models;

namespace DirectorPrompt.Domain.Services;

public interface ITimelineCalculator
{
    /// <summary>
    ///     根据相邻场景计算新场景的 timelinePosition
    /// </summary>
    /// <param name="afterSceneID">新场景在时间轴上位于此场景之后</param>
    /// <param name="beforeSceneID">新场景在时间轴上位于此场景之前</param>
    /// <param name="existingScenes">同项目下已有场景列表</param>
    /// <returns>新场景的 timelinePosition</returns>
    /// <exception cref="ArgumentException">参数校验不通过</exception>
    long CalculatePosition(long? afterSceneID, long? beforeSceneID, IReadOnlyList<Scene> existingScenes);
}
