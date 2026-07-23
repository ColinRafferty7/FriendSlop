using UnityEngine;

public enum AbilityType
{
    Active,          
    Passive,
    ActiveAndPassive 
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
