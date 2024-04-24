using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SessionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI sessionNameTMP;
    [SerializeField] private TextMeshProUGUI sessionCountTMP;
    [SerializeField] private RectTransform sessionUserList;
    [SerializeField] private SessionUserUI sessionUserItemPrefab;
    [SerializeField] private TextMeshProUGUI readyOrStartText;
    public UnityEvent onSessionIn;
    public UnityEvent onSessionOut;
    private NetworkManager networkManager;

    private Dictionary<PlayerRef, RoomPlayer> sessionUserDic;
    private void Awake()
    {
        sessionUserDic = new Dictionary<PlayerRef, RoomPlayer>();
    }
    private void OnEnable()
    {
        if (networkManager == null)
            networkManager = GameManagers.Instance.NetworkManager;


    }

    private void Update()
    {
        if (networkManager == null)
            return;

        if (networkManager.Runner == null)
            return;

        foreach (var player in networkManager.Runner.ActivePlayers)
        {
            if (networkManager.Runner.TryGetPlayerObject(player, out var playerObject))
            {
                if (!sessionUserDic.ContainsKey(player))
                {
                    if (playerObject.TryGetComponent(out RoomPlayer roomPlayer))
                    {
                        SessionUserUI userUI = Instantiate(sessionUserItemPrefab, sessionUserList);
                        roomPlayer.Setup(userUI, player);
                        sessionUserDic.Add(player, roomPlayer);
                        roomPlayer.onDespawn += () => { sessionUserDic.Remove(player); };

                        if (networkManager.Runner.TryGetPlayerObject(networkManager.Runner.LocalPlayer, out NetworkObject localPlayer))
                        {
                            if (localPlayer.GetComponent<RoomPlayer>().isHost)
                            {
                                readyOrStartText.text = "����";
                            }
                            else
                            {
                                readyOrStartText.text = "�غ�";

                            }
                        }
                    }
                }
            }
        }

    }
    public void CreateSession(string sessionName, int maxCount)
    {
        Debug.Log(sessionName);
        gameObject.SetActive(true);
        onSessionIn?.Invoke();
        sessionNameTMP.text = sessionName;
        sessionCountTMP.text = $"1/{maxCount}";
    }
    public void JoinSession(SessionInfo sessionInfo)
    {
        gameObject.SetActive(true);
        onSessionIn?.Invoke();
        sessionNameTMP.text = sessionInfo.Name;
        sessionCountTMP.text = $"({sessionInfo.PlayerCount}/{sessionInfo.MaxPlayers})";
    }

    public async void ExitSession()
    {
        GameManagers.Instance.UIManager.ActiveLoading(true, "���� ������ ���Դϴ�.");
        StartGameResult result = await networkManager.JoinLobby();
        if (result.Ok)
        {
            gameObject.SetActive(false);
            onSessionOut?.Invoke();
            foreach (Transform player in sessionUserList.transform)
            {
                Destroy(player.gameObject);
            }
            sessionUserDic.Clear();
        }
        GameManagers.Instance.UIManager.ActiveLoading(false);
    }
    public void PressReadyOrStartButton()
    {
        PlayerRef playerRef = networkManager.Runner.LocalPlayer;
        if (sessionUserDic.ContainsKey(playerRef))
        {
            if (!sessionUserDic[playerRef].isHost)
            {
                PlayerPreviewController playerPreview = FindObjectOfType<PlayerPreviewController>();
                sessionUserDic[playerRef].RPC_Ready();
                sessionUserDic[playerRef].RPC_AddClientPreset(PlayerPreviewController.AppearanceType.Preset,playerPreview.GetCurrenIndex(PlayerPreviewController.AppearanceType.Preset));
                sessionUserDic[playerRef].RPC_AddClientPreset(PlayerPreviewController.AppearanceType.Color,playerPreview.GetCurrenIndex(PlayerPreviewController.AppearanceType.Color));
                sessionUserDic[playerRef].RPC_AddClientPreset(PlayerPreviewController.AppearanceType.Hair,playerPreview.GetCurrenIndex(PlayerPreviewController.AppearanceType.Hair));
                sessionUserDic[playerRef].RPC_AddClientPreset(PlayerPreviewController.AppearanceType.Breard,playerPreview.GetCurrenIndex(PlayerPreviewController.AppearanceType.Breard));
            }
            else
            {
                int readyCount = 1;
                foreach (var player in sessionUserDic.Values)
                {
                    readyCount += player.IsReady ? 1 : 0;
                }

                if (readyCount == sessionUserDic.Count)
                    sessionUserDic[playerRef].StartGame();
            }
        }
    }
}