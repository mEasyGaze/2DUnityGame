using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleEndUI : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private TextMeshProUGUI defeatText;
    [SerializeField] private Button exitButton;

    private void Start()
    {
        exitButton.onClick.AddListener(OnExitButtonClicked);
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(false);
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(false);
        }
    }

    public void ShowVictory(int goldReward)
    {
        victoryPanel.SetActive(true);
        defeatPanel.SetActive(false);
        rewardText.text = $"獲得金幣: {goldReward}";
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
        }
    }

    public void ShowDefeat()
    {
        victoryPanel.SetActive(false);
        defeatPanel.SetActive(true);
        
        var encounter = GameManager.Instance.CurrentEncounter;
        if (encounter != null && encounter.defeatType != DefeatActionType.ReturnToTitle)
        {
            if (defeatText != null) defeatText.text = "體力不支，準備撤退...";
        }
        else
        {
            if (defeatText != null) defeatText.text = "戰鬥失敗";
        }

        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
        }
    }

    private void OnExitButtonClicked()
    {
        exitButton.interactable = false;
        GameManager.Instance.EndBattle();
    }
}