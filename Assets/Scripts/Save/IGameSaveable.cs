public interface IGameSaveable
{
    void PopulateSaveData(GameSaveData data);
    void LoadFromSaveData(GameSaveData data);
}