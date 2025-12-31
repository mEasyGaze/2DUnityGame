public class BuffInstance
{
    public BuffDefinition Definition { get; }
    public int RemainingDuration { get; private set; }
    public IBattleUnit_ReadOnly Source { get; }

    public BuffInstance(BuffDefinition definition, IBattleUnit_ReadOnly source)
    {
        Definition = definition;
        RemainingDuration = definition.Duration;
        Source = source;
    }

    public void Tick()
    {
        if (RemainingDuration > 0)
        {
            RemainingDuration--;
        }
    }

    public bool IsExpired()
    {
        return RemainingDuration <= 0;
    }
}