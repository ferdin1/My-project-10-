using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class playerchatsystem : NetworkBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_InputField inputField;

    [SerializeField] private float minMessageTime = 2f;
    [SerializeField] private float maxMessageTime = 8f;
    [SerializeField] private float secondsPerCharacter = 0.08f;

    public bool IsChatting { get; private set; }

    private Coroutine messageCoroutine;

    void Start()
    {
        messageText.text = "";
        messageText.gameObject.SetActive(false);

        inputField.gameObject.SetActive(false);
    }

    void Update()
    {
        // Alleen de eigenaar mag zijn eigen chat gebruiken
        if (!IsOwner)
            return;

        // T opent de chat, maar alleen als je nog niet aan het typen bent
        if (Keyboard.current.tKey.wasPressedThisFrame &&
            !inputField.gameObject.activeSelf)
        {
            OpenChat();
        }

        // Escape sluit de chat
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            inputField.gameObject.activeSelf)
        {
            CloseChat();
        }

        // Enter verstuurt het bericht
        if (Keyboard.current.enterKey.wasPressedThisFrame &&
            inputField.gameObject.activeSelf)
        {
            SendMessageToServer(inputField.text);
        }
    }

    void OpenChat()
    {
            IsChatting = true;
        inputField.gameObject.SetActive(true);
        messageText.gameObject.SetActive(false);

        inputField.text = "";
        inputField.ActivateInputField();
    }

    void CloseChat()
    {
        IsChatting = false;
        inputField.DeactivateInputField();
        inputField.gameObject.SetActive(false);
    }

    void SendMessageToServer(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            CloseChat();
            return;
        }

        // Server krijgt het bericht
        SendMessageServerRpc(message);

        CloseChat();
    }

    [ServerRpc]
    void SendMessageServerRpc(string message)
    {
        // Server controleert de maximale lengte
        if (message.Length > 100)
        {
            message = message.Substring(0, 100);
        }

        ShowMessageClientRpc(message);
    }

    [ClientRpc]
    void ShowMessageClientRpc(string message)
    {
        messageText.text = message;
        messageText.gameObject.SetActive(true);

        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        messageCoroutine = StartCoroutine(HideMessageAfterTime(message));
    }

    IEnumerator HideMessageAfterTime(string message)
    {
        float displayTime = message.Length * secondsPerCharacter;

        displayTime = Mathf.Clamp(
            displayTime,
            minMessageTime,
            maxMessageTime
        );

        yield return new WaitForSeconds(displayTime);

        messageText.gameObject.SetActive(false);

        messageCoroutine = null;
    }
}