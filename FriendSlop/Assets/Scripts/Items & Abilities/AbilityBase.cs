using UnityEngine;

public enum AbilityType
{
    Active,          // only does something when triggered
    Passive,         // only does something continuously while equipped
    ActiveAndPassive // does both
}
public abstract class AbilityBase : MonoBehaviour
{
    public abstract AbilityType Type { get; }
    [SerializeField] protected float cooldown = 1f;
    public float Cooldown => cooldown;
    public virtual void Activate(GameObject user) { }
    public virtual void OnEquip(GameObject user) { }
    public virtual void OnUnequip(GameObject user) { }
    public virtual void PassiveTick(GameObject user) { }
}
