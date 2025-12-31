public interface ISceneSaveable
{
    object CaptureState();
    void RestoreState(object stateData);
}