using UnityEngine;
using System.Collections;

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
    public float breakDelay = 0.5f;
    public bool hasBeenUsed = false;

    [Header("Visual Effects")]
    public bool enableVisualEffects = true;
    public Color originalColor = Color.white;

    private Renderer platformRenderer;
    private Collider platformCollider;
    private bool isBreaking = false;

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
        if (platformType == PlatformType.Fake && platformCollider != null)
            platformCollider.isTrigger = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && collision.contacts.Length > 0 && Vector3.Dot(collision.contacts[0].normal, Vector3.up) > 0.5f)
            HandlePlatformInteraction(player);

        var bot = collision.gameObject.GetComponent<BotController>();
        if (bot != null && collision.contacts.Length > 0 && Vector3.Dot(collision.contacts[0].normal, Vector3.up) > 0.5f)
            HandlePlatformInteraction(bot);
    }

    void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
            HandlePlatformInteraction(player);

        var bot = other.GetComponent<BotController>();
        if (bot != null)
            HandlePlatformInteraction(bot);
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

        yield return new WaitForSeconds(breakDelay);

        // Verifica si el jugador o bot sigue encima (distancia vertical < 0.8)
        if (player != null && Mathf.Abs(player.transform.position.y - transform.position.y) < 0.8f)
            player.DieWithMessage("Has perdido: la plataforma se rompió bajo tus pies.");
        if (bot != null && Mathf.Abs(bot.transform.position.y - transform.position.y) < 0.8f)
            bot.Die();

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