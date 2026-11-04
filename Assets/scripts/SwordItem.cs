using UnityEngine;

/// <summary>
/// VOORBEELD: EEN WAPEN / ZWAARD SCRIPT
/// 
/// Dit script hangt op de prefab van het zwaard (samen met bijv. een SpriteRenderer en een Collider/Hitbox).
/// Omdat het erft van ItemBase, krijgt het automatisch toegang tot:
/// - Instance (huidige durability)
/// - Data (naam, maxDurability)
/// - OnUse() (linkermuisklik om aan te vallen)
/// </summary>
public class SwordItem : ItemBase
{
    [Header("Zwaard Instellingen")]
    [Tooltip("De schade die dit zwaard toebrengt aan vijanden.")]
    public int attackDamage = 15;

    [Tooltip("Animatie/zwaaisnelheid.")]
    public float swingSpeed = 10f;

    private bool isSwinging = false;
    private float swingTimer = 0f;

    /// <summary>
    /// Wordt aangeroepen als de speler links-klikt terwijl het zwaard vastgehouden wordt.
    /// </summary>
    public override void OnUse()
    {
        if (isSwinging) return; // Voorkom spammen tijdens zwaai-animatie

        Debug.Log($"⚔️ [{Data.itemName}] Zwaait met het zwaard! Schade: {attackDamage}");

        // Start simpele zwaaibeweging
        isSwinging = true;
        swingTimer = 0.2f;

        // Breng durability-schade toe aan dit specifieke zwaard
        DamageDurability(1);

        // HIER KUN JE HITBOX OF OVERLAPCHECK TOEVOEGEN:
        // Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, 1.2f);
        // foreach (var enemy in hitEnemies) { ... }
    }

    private void Update()
    {
        // Simpel visueel zwaai-effectje (rotatie)
        if (isSwinging)
        {
            transform.localRotation = Quaternion.Euler(0, 0, Mathf.PingPong(Time.time * 300f, 60f) - 30f);
            swingTimer -= Time.deltaTime;
            if (swingTimer <= 0f)
            {
                isSwinging = false;
                transform.localRotation = Quaternion.identity;
            }
        }
    }
}
