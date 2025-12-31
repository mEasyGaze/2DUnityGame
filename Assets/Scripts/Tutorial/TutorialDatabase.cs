using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class TutorialDatabase
{
    private static Dictionary<string, TutorialSO> tutorials;
    private static bool isLoaded = false;

    private static void LoadDatabase()
    {
        if (isLoaded) return;

        tutorials = new Dictionary<string, TutorialSO>();
        TutorialSO[] loadedTutorials = Resources.LoadAll<TutorialSO>("GameData/Tutorials");

        foreach (var tutorial in loadedTutorials)
        {
            if (!tutorials.ContainsKey(tutorial.tutorialID))
            {
                tutorials.Add(tutorial.tutorialID, tutorial);
            }
            else
            {
                Debug.LogWarning($"[TutorialDatabase] 發現重複的教學 ID: {tutorial.tutorialID}");
            }
        }
        Debug.Log($"[TutorialDatabase] 成功載入 {tutorials.Count} 個教學。");
        isLoaded = true;
    }

    public static TutorialSO GetTutorialByID(string id)
    {
        if (!isLoaded)
        {
            LoadDatabase();
        }

        if (tutorials.TryGetValue(id, out TutorialSO tutorial))
        {
            return tutorial;
        }
        Debug.LogWarning($"[TutorialDatabase] 找不到 ID 為 '{id}' 的教學。");
        return null;
    }
}