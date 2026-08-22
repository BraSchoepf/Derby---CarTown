using UnityEngine;

public class CheckpointVFX : MonoBehaviour
{
    [Header("Humo por jugador")]
    [SerializeField] private ParticleSystem player1Smoke;
    [SerializeField] private ParticleSystem player2Smoke;

    [Header("Color inicial (antes de que alguien cruce)")]
    [SerializeField] private Color inactiveColor = Color.white;

    [Header("Color al activarse (por jugador)")]
    [SerializeField] private Color player1Color = Color.cyan;
    [SerializeField] private Color player2Color = Color.magenta;

    [Header("Duración del color de jugador antes de volver al inactivo (0 = para siempre)")]
    [SerializeField] private float highlightDuration = 1.5f;

    float p1Timer, p2Timer;
    bool p1Highlighted, p2Highlighted;

    void Awake()
    {
        // Al arrancar, ambos sistemas quedan en el color "inactivo/default"
        SetStartColorFirst(player1Smoke, inactiveColor);
        SetStartColorFirst(player2Smoke, inactiveColor);
    }

    void Update()
    {
        UpdateHighlightTimer(player1Smoke, ref p1Highlighted, ref p1Timer);
        UpdateHighlightTimer(player2Smoke, ref p2Highlighted, ref p2Timer);
    }

    void UpdateHighlightTimer(ParticleSystem ps, ref bool isHighlighted, ref float timer)
    {
        if (!isHighlighted || highlightDuration <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SetStartColorFirst(ps, inactiveColor);
            isHighlighted = false;
        }
    }

    public void OnPassedCorrectly(int humanSlotIndex)
    {
        ParticleSystem smoke = humanSlotIndex == 0 ? player1Smoke : player2Smoke;
        Color activeColor = humanSlotIndex == 0 ? player1Color : player2Color;
        if (smoke == null) return;

        SetStartColorFirst(smoke, activeColor);

        if (humanSlotIndex == 0) { p1Highlighted = true; p1Timer = highlightDuration; }
        else { p2Highlighted = true; p2Timer = highlightDuration; }

        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        smoke.Play();
    }

    void SetStartColorFirst(ParticleSystem ps, Color color)
    {
        if (ps == null) return;

        var main = ps.main;
        ParticleSystem.MinMaxGradient startColor = main.startColor;

        if (startColor.mode == ParticleSystemGradientMode.TwoColors)
        {
            startColor.colorMin = color;
            main.startColor = startColor;
        }
        else if (startColor.mode == ParticleSystemGradientMode.TwoGradients)
        {
            Gradient gradientMin = startColor.gradientMin;
            GradientColorKey[] colorKeys = gradientMin.colorKeys;
            if (colorKeys.Length > 0)
            {
                colorKeys[0].color = color;
                Gradient newGradientMin = new Gradient();
                newGradientMin.SetKeys(colorKeys, gradientMin.alphaKeys);
                startColor.gradientMin = newGradientMin;
                main.startColor = startColor;
            }
        }
        else
        {
            Debug.LogWarning($"[CheckpointVFX] {ps.name}: Start Color no está en modo 'Random Between Two Colors' ni 'Random Between Two Gradients'.", ps);
        }
    }
}