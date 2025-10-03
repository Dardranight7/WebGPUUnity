using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum PlatformType
{
    Static,
    Breakable,
    Fake
}

public class Platform : MonoBehaviour
{
    [Header("Platform Configuration")]
    public PlatformType platformType = PlatformType.Static;

    [Header("Breakable Platform Settings")]
    public float breakDelay = 1f;
    public bool hasBeenUsed = false;

    [Header("Visual Effects")]
    public bool enableVisualEffects = true;
    public Color originalColor = Color.white;

    private Renderer platformRenderer;
    private Collider platformCollider;
    private bool isBreaking = false;
    
    // Lista actual de colliders que están en contacto con la plataforma
    private HashSet<Collider> currentOccupants = new HashSet<Collider>();

    void Start()
    {
        platformRenderer = GetComponent<Renderer>();
        platformCollider = GetComponent<Collider>();
        if (platformRenderer != null)
            originalColor = platformRenderer.material.color;
        ConfigurePlatformType();
    }

    void ConfigurePlatformType()
    {
        if (platformType == PlatformType.Fake && platformCollider != null)
            platformCollider.isTrigger = true;
        
        if (platformType == PlatformType.Fake && platformRenderer != null && enableVisualEffects)
        {
            Color fakeColor = originalColor;
            fakeColor.a = 0.6f;
            platformRenderer.material.color = fakeColor;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Guardamos el collider en occupants si la normal indica que aterrizaron encima
        if (collision.contacts.Length > 0 && Vector3.Dot(collision.contacts[0].normal, Vector3.up) > 0.5f)
        {
            AddOccupant(collision.collider);
            // Manejo inmediato (para fake/breakable)
            var player = collision.gameObject.GetComponent<PlayerController>();
            var bot = collision.gameObject.GetComponent<BotController>();
            if (player != null)
                HandlePlatformInteraction(player);
            else if (bot != null)
                HandlePlatformInteraction(bot);
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        RemoveOccupant(collision.collider);
    }

    void OnTriggerEnter(Collider other)
    {
        AddOccupant(other);
        var player = other.GetComponent<PlayerController>();
        var bot = other.GetComponent<BotController>();
        if (player != null)
            HandlePlatformInteraction(player);
        else if (bot != null)
            HandlePlatformInteraction(bot);
    }
    
    void OnTriggerExit(Collider other)
    {
        RemoveOccupant(other);
    }

    void AddOccupant(Collider c)
    {
        if (c == null) return;
        currentOccupants.Add(c);
    }

    void RemoveOccupant(Collider c)
    {
        if (c == null) return;
        currentOccupants.Remove(c);
    }

    void HandlePlatformInteraction(PlayerController player)
    {
        switch (platformType)
        {
            case PlatformType.Breakable:
                if (!hasBeenUsed && !isBreaking)
                    StartCoroutine(BreakPlatform(player, null));
                break;
            case PlatformType.Fake:
                player.DieWithMessage("Has perdido: plataforma invisible.");
                break;
        }
    }

    void HandlePlatformInteraction(BotController bot)
    {
        switch (platformType)
        {
            case PlatformType.Breakable:
                if (!hasBeenUsed && !isBreaking)
                    StartCoroutine(BreakPlatform(null, bot));
                break;
            case PlatformType.Fake:
                bot.Die();
                break;
        }
    }

    IEnumerator BreakPlatform(PlayerController player, BotController bot)
    {
        isBreaking = true;
        hasBeenUsed = true;
        
        if (platformRenderer != null && enableVisualEffects)
            platformRenderer.material.color = Color.red;
        
        // LOG para depuración: quien activó la ruptura
        if (player != null) Debug.Log($"[Platform] ruptura iniciada por el judaor en  '{gameObject.name}'");
        if (bot != null) Debug.Log($"[Platform] ruptura iniciada por el bot en  '{gameObject.name}'");

        yield return new WaitForSeconds(breakDelay);

        bool playerOnTop = false;
        bool botOnTop = false;
        
        // 1) Revisa la lista de occupants por componentes PlayerController/BotController
        foreach (var c in currentOccupants)
        {
            if (c == null) continue;
            if (!playerOnTop && c.GetComponent<PlayerController>() != null) playerOnTop = true;
            if (!botOnTop && c.GetComponent<BotController>() != null) botOnTop = true;
        }

        // 2) Si no se detecta por occupants (por ejemplo si colliders ya no están), usar OverlapBox en zona superior
        if (!playerOnTop && !botOnTop && platformCollider != null)
        {
            // calcular un pequeño box en la parte superior de la plataforma
            Bounds b = platformCollider.bounds;
            Vector3 boxCenter = b.center + Vector3.up * (b.extents.y + 0.15f);
            Vector3 boxHalfExtents = new Vector3(b.extents.x * 0.9f, 0.25f, b.extents.z * 0.9f);

            Collider[] hits = Physics.OverlapBox(boxCenter, boxHalfExtents, Quaternion.identity);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (!playerOnTop && h.GetComponent<PlayerController>() != null) playerOnTop = true;
                if (!botOnTop && h.GetComponent<BotController>() != null) botOnTop = true;
            }
        }

        // LOG estado después del delay
        Debug.Log($"[Platform] after delay on '{gameObject.name}' -> playerOnTop: {playerOnTop}, botOnTop: {botOnTop}, occupantsCount: {currentOccupants.Count}");

        // Ejecutar muerte si corresponde
        if (playerOnTop)
        {
            // Si tenemos la referencia al player original, úsala; si no, buscamos uno en la escena en la posición
            if (player != null && player.IsAlive)
            {
                Debug.Log($"[Platform] Killing player on '{gameObject.name}'");
                player.DieWithMessage("Has perdido: plataforma rota.");
            }
            else
            {
                // buscar el PlayerController en la zona
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 1f);
                foreach (var h in hits)
                {
                    var p = h.GetComponent<PlayerController>();
                    if (p != null && p.IsAlive)
                    {
                        Debug.Log($"[Platform] Killing found player {p.name} on '{gameObject.name}'");
                        p.DieWithMessage("Has perdido: plataforma rota.");
                        break;
                    }
                }
            }
        }
        
        if (botOnTop)
        {
            if (bot != null && bot.isAlive)
            {
                Debug.Log($"[Platform] Killing bot {bot.botName} on '{gameObject.name}'");
                bot.Die();
            }
            else
            {
                // buscar bots en la zona
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.5f, 1f);
                foreach (var h in hits)
                {
                    var b = h.GetComponent<BotController>();
                    if (b != null && b.isAlive)
                    {
                        Debug.Log($"[Platform] Killing found bot {b.botName} on '{gameObject.name}'");
                        b.Die();
                    }
                }
            }
        }

        // Deshabilitar collider para que ya no se pise
        if (platformCollider != null)
            platformCollider.enabled = false;

        if (enableVisualEffects)
            StartCoroutine(DisappearEffect());
        else
            gameObject.SetActive(false);

    }

    IEnumerator DisappearEffect()
    {
        float fallDuration = 1f;
        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / fallDuration;
            if (platformRenderer != null)
            {
                Color fadeColor = originalColor;
                fadeColor.a = 1f - progress;
                platformRenderer.material.color = fadeColor;
            }
            yield return null;
        }
        gameObject.SetActive(false);
    }
}