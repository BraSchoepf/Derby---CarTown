using UnityEngine;

public class WheelCustomizer : MonoBehaviour
{
    [System.Serializable]
    public class WheelSlot
    {
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public bool isFrontAxle;
        public bool isLeftSide;

    }

    public WheelSlot[] wheelSlots;
    public CarStatsSO stats;

    public void ApplyWheel(WheelVisualSO wheelVisual)
    {
        if (wheelVisual == null || wheelVisual.wheelMesh == null || stats == null) return;

        foreach (var slot in wheelSlots)
        {
            if (slot.meshFilter == null) continue;

            slot.meshFilter.mesh = wheelVisual.wheelMesh;

            if (slot.meshRenderer != null && wheelVisual.wheelMaterials != null && wheelVisual.wheelMaterials.Length > 0)
                slot.meshRenderer.materials = wheelVisual.wheelMaterials;

            float maxRadiusForThisSlot = slot.isFrontAxle
                ? stats.maxWheelRadiusFront
                : stats.maxWheelRadiusRear;

            float scaleFactor = maxRadiusForThisSlot / wheelVisual.nativeRadius;

            // Mirror en X para el lado izquierdo — como las ruedas ya miran hacia X positivo,
            // negando X las "da vuelta" para que encaren hacia afuera del otro lado del auto
            float xScale = slot.isLeftSide ? -scaleFactor : scaleFactor;
            slot.meshFilter.transform.localScale = new Vector3(xScale, scaleFactor, scaleFactor);
        }
    }
}