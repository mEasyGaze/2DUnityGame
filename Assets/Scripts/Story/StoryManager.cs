using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryManager : MonoBehaviour
{
    public static StoryManager Instance { get; private set; }
    public bool IsStorySceneActive { get; private set; } = false;

    private bool isWaitingForDialogueToEnd = false;
    private Camera mainCamera;
    private Coroutine storyCoroutine;
    private Transform cameraFollowTarget = null;
    private StorySceneRunner currentRunner;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        FindMainCamera();
    }

    #region 場景事件監聽與攝影機管理
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindMainCamera();
    }
    
    private void FindMainCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null && Time.timeSinceLevelLoad > 1f)
        {
            Debug.LogWarning("[StoryManager] 警告：在當前場景中找不到主攝影機。");
        }
    }
    #endregion

    private void LateUpdate()
    {
        if (cameraFollowTarget != null && mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(
                cameraFollowTarget.position.x,
                cameraFollowTarget.position.y,
                mainCamera.transform.position.z
            );
        }
    }

    public void StartStoryScene(StorySceneData sceneData, StorySceneRunner runner)
    {
        if (IsStorySceneActive) return;
        if (sceneData == null || runner == null) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.InStoryScene);
        }
        currentRunner = runner;
        IsStorySceneActive = true;
        storyCoroutine = StartCoroutine(StoryPlaybackController(sceneData));
    }

    private IEnumerator ExecuteStoryScene(StorySceneData sceneData)
    {
        Debug.Log($"--- 劇情場景開始: {sceneData.sceneID} ---");
        foreach (var phase in sceneData.phases)
        {
            List<Coroutine> phaseCoroutines = new List<Coroutine>();
            foreach (var action in phase.actions)
            {
                Coroutine actionCoroutine = StartCoroutine(ExecuteAction(action));
                phaseCoroutines.Add(actionCoroutine);
            }
            foreach (var coroutine in phaseCoroutines)
            {
                yield return coroutine;
            }
        }
        Debug.Log($"--- 劇情場景結束: {sceneData.sceneID} ---");
    }

    private IEnumerator ExecuteAction(StoryAction action)
    {
        bool isCameraAction = action.actionType == StoryActionType.MoveCameraToTarget || 
                              action.actionType == StoryActionType.MoveCameraToPosition || 
                              action.actionType == StoryActionType.FocusOnCharacter || 
                              action.actionType == StoryActionType.ReleaseCameraFocus;
        if (isCameraAction && mainCamera == null) yield break;
        GameObject targetObject = null;
        if (!string.IsNullOrEmpty(action.targetObjectName) && currentRunner != null)
        {
            targetObject = currentRunner.GetActor(action.targetObjectName);
        }
        
        switch (action.actionType)
        {
            case StoryActionType.MoveCharacter:
                if (targetObject != null)
                    yield return MoveObjectCoroutine(targetObject.transform, action.targetPosition, action.duration);
                break;
            
            case StoryActionType.ParabolicMove:
                if (targetObject != null)
                    yield return ParabolicMoveCoroutine(targetObject.transform, action.targetPosition, action.parabolaHeight, action.duration);
                break;

            case StoryActionType.RotateObject:
                if (targetObject != null)
                    yield return RotateObjectCoroutine(targetObject.transform, action.rotationAngles, action.duration);
                break;

            case StoryActionType.ChangeSprite:
                if (targetObject != null && action.newSprite != null)
                {
                    var sr = targetObject.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = action.newSprite;
                    else Debug.LogWarning($"[StoryManager] {targetObject.name} 沒有 SpriteRenderer，無法切換圖片。");
                }
                break;

            case StoryActionType.PlayAnimation:
                if (targetObject != null && !string.IsNullOrEmpty(action.stringID))
                {
                    var animator = targetObject.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetTrigger(action.stringID);
                    }
                    else
                    {
                        Debug.LogWarning($"[StoryManager] {targetObject.name} 沒有 Animator 組件，無法播放動畫。");
                    }
                }
                break;

            case StoryActionType.PlayMusic:
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(action.stringID))
                    AudioManager.Instance.PlayMusic(action.stringID);
                break;

            case StoryActionType.PauseMusic:
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PauseMusic();
                break;

            case StoryActionType.ResumeMusic:
                if (AudioManager.Instance != null)
                    AudioManager.Instance.ResumeMusic();
                break;

            case StoryActionType.StopMusic:
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopMusic();
                break;

            case StoryActionType.PlaySoundEffect:
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(action.stringID))
                    AudioManager.Instance.PlaySFX(action.stringID);
                break;

            case StoryActionType.SetCharacterActive:
                if (targetObject != null) targetObject.SetActive(action.boolValue);
                break;
            
            case StoryActionType.SpawnObject:
                if (action.objectToSpawn != null)
                    Instantiate(action.objectToSpawn, action.targetPosition, Quaternion.identity);
                break;
            
            case StoryActionType.DestroyObject:
                if (targetObject != null) Destroy(targetObject);
                break;

            case StoryActionType.StartDialogue:
                yield return null; 
                isWaitingForDialogueToEnd = true;
                DialogueManager.Instance.StartDialogue(action.dialogueFilePath, action.dialogueID);
                while (isWaitingForDialogueToEnd) yield return null;
                break;

            case StoryActionType.Wait:
                yield return new WaitForSeconds(action.duration);
                break;
            
            case StoryActionType.MoveCameraToTarget:
                if (targetObject != null)
                    yield return MoveObjectCoroutine(mainCamera.transform, targetObject.transform.position, action.duration, true);
                break;

            case StoryActionType.MoveCameraToPosition:
                yield return MoveObjectCoroutine(mainCamera.transform, action.targetPosition, action.duration, true);
                break;
                
            case StoryActionType.FocusOnCharacter:
                if (targetObject != null) cameraFollowTarget = targetObject.transform;
                break;
            
            case StoryActionType.ReleaseCameraFocus:
                cameraFollowTarget = null;
                break;

            case StoryActionType.TriggerGameEvent:
                GameEventManager.Instance.TriggerEvent(action.stringID);
                break;

            case StoryActionType.RunSubStory:
                if (action.subStoryToRun != null) yield return ExecuteStoryScene(action.subStoryToRun);
                break;
        }
    }
    
    private void HandleDialogueEnded()
    {
        if (isWaitingForDialogueToEnd) isWaitingForDialogueToEnd = false;
    }
    
    private IEnumerator MoveObjectCoroutine(Transform objectToMove, Vector2 endPosition, float duration, bool isCamera = false)
    {
        float elapsedTime = 0;
        Vector3 startPosition = objectToMove.position;
        Vector3 finalEndPosition = new Vector3(endPosition.x, endPosition.y, startPosition.z);

        if (duration <= 0)
        {
            objectToMove.position = finalEndPosition;
            yield break;
        }

        while (elapsedTime < duration)
        {
            objectToMove.position = Vector3.Lerp(startPosition, finalEndPosition, (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objectToMove.position = finalEndPosition;
    }

    private IEnumerator ParabolicMoveCoroutine(Transform objectToMove, Vector2 endPosition, float height, float duration)
    {
        float elapsedTime = 0;
        Vector3 startPosition = objectToMove.position;
        Vector3 targetPos = new Vector3(endPosition.x, endPosition.y, startPosition.z);

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            Vector3 linearPos = Vector3.Lerp(startPosition, targetPos, t);
            float heightOffset = 4 * height * t * (1 - t);
            objectToMove.position = linearPos + Vector3.up * heightOffset;
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        objectToMove.position = targetPos;
    }

    private IEnumerator RotateObjectCoroutine(Transform objectToMove, Vector3 anglesPerSecond, float duration)
    {
        float elapsedTime = 0;
        while (elapsedTime < duration)
        {
            objectToMove.Rotate(anglesPerSecond * Time.deltaTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator StoryPlaybackController(StorySceneData sceneData)
    {
        DialogueManager.OnDialogueEnded += HandleDialogueEnded;
        yield return ExecuteStoryScene(sceneData);
        DialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        IsStorySceneActive = false;
        currentRunner = null;
        storyCoroutine = null;
        if (sceneData.restoreSceneBgmOnEnd && AudioBackground.Instance != null)
        {
            AudioBackground.Instance.RestoreBGM();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Exploration);
        }
    }
}