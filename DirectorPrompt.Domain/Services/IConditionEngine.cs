namespace DirectorPrompt.Domain.Services;

public interface IConditionEngine
{
    bool Evaluate(string condition, ConditionContext context);
}
