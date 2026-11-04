using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private playerchatsystem chatSystem;

    public float speed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (chatSystem == null)
        {
            chatSystem = GetComponent<playerchatsystem>();
            if (chatSystem == null)
            {
                chatSystem = Object.FindFirstObjectByType<playerchatsystem>();
            }
        }
    }

    private void Update()
    {
        // Alleen de owner mag dit object besturen.
        // Anders zou elke client input lezen en versturen voor ALLE spelers.
        if (!IsOwner) return;

        if (chatSystem != null && chatSystem.IsChatting)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float x = 0f;
        float y = 0f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  x -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    y += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  y -= 1f;

        input = new Vector2(x, y);
        if (input.sqrMagnitude > 1f)
            input.Normalize();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (rb == null) return;

        rb.linearVelocity = input * speed;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Camera playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
        }
    }
}