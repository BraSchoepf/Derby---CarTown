using UnityEngine;

public class CheckpointVFX : MonoBehaviour
{
    [Header("Humo por jugador")]
    [SerializeField] private ParticleSystem player1Smoke;
    [SerializeField] private ParticleSystem player2Smoke;

    [Header("Color (se aplica al PRIMER color de Start Color)")]
    [SerializeField] private Color player1Color = Color.cyan;
    [SerializeField] private Color player2Color = Color.magenta;

    void Awake()
    {
        ApplyStartColorFirst(player1Smoke, player1Color);
        ApplyStartColorFirst(player2Smoke, player2Color);
    }

    public void OnPassedCorrectly(int humanSlotIndex)
    {
        ParticleSystem smoke = humanSlotIndex == 0 ? player1Smoke : player2Smoke;
        if (smoke == null) return;

        smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        smoke.Play();
    }

    void ApplyStartColorFirst(ParticleSystem ps, Color color)
    {
        if (ps == null) return;

        var main = ps.main;
        ParticleSystem.MinMaxGradient startColor = main.startColor;

        if (startColor.mode == ParticleSystemGradientMode.TwoColors)
        {
            // Reemplaza SOLO el primer color, mantiene el segundo (colorMax) intacto
            startColor.colorMin = color;
            main.startColor = startColor;
        }
        else if (startColor.mode == ParticleSystemGradientMode.TwoGradients)
        {
            // Si en vez de dos colores planos usás dos gradientes, tocamos
            // el primer color key del gradiente "min"
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
            Debug.LogWarning($"[CheckpointVFX] {ps.name}: Start Color no está en modo 'Random Between Two Colors' ni 'Random Between Two Gradients' — no se puede aplicar un color 'primero' distinto del segundo.", ps);
        }
    }
}