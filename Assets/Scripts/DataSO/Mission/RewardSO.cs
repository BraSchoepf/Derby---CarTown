using UnityEngine;

[CreateAssetMenu(fileName = "NewReward", menuName = "Missions/Reward")]
public class RewardSO : ScriptableObject
{
    public enum RewardType { Car, WheelVisual, Color }

    public string rewardId; // único, para el UnlockRegistry
    public RewardType type;
    public Sprite icon;
    public string displayName;

    public CarStatsSO carReward;
    public WheelVisualSO wheelReward;
    public Color colorReward;
}