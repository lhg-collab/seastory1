public interface ITargetProvider
{
    bool IsAiming { get; }
    Gatherable CurrentTarget { get; }
    float CurrentTargetDistance { get; }
}
