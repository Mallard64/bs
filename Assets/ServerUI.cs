using System;            // ← for Action<T>
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ServerEntryUI : MonoBehaviour
{
    public TextMeshProUGUI serverNameText;
    public Button joinButton;

    public void Initialize(string displayName, Action onJoin)
    {
        serverNameText.text = displayName;
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => onJoin());
    }
}
