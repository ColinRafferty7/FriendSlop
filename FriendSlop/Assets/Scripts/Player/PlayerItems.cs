using System.Collections.Generic;
using UnityEngine;

public class PlayerItems : MonoBehaviour
{
    [SerializeField] int maxOwnedAbilities = 3;
    List<AbilityBase> ownedAbilities = new List<AbilityBase>();
    [SerializeField] bool replaceOldestWhenFull = true;
    int currentAbilityIndex = -1;
    AbilityBase currentAbility;
    float cooldownTimer = 0f;
    bool activatePressed = false;
    int swapPressed = 0;


    public void AttemptActivation()
    {
        if (activatePressed && currentAbility != null && cooldownTimer <= 0 && (currentAbility.Type == AbilityType.Active || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            currentAbility.Activate(gameObject);
            cooldownTimer = currentAbility.Cooldown;
        }
    }


    public void CollectAbility(AbilityBase prefab)
    {
        if (ownedAbilities.Count >= maxOwnedAbilities)
        {
            int indexToRemove = replaceOldestWhenFull ? 0 : currentAbilityIndex;

            if (indexToRemove < 0 || indexToRemove >= ownedAbilities.Count)
            {
                indexToRemove = 0;
            }

            AbilityBase removed = ownedAbilities[indexToRemove];

            if (removed == currentAbility)
            {
                currentAbility.OnUnequip(gameObject);
                currentAbility = null;
                currentAbilityIndex = -1;
            }

            Destroy(removed.gameObject);
            ownedAbilities.RemoveAt(indexToRemove);
        }

        AbilityBase instance = Instantiate(prefab, transform);
        instance.enabled = true;
        ownedAbilities.Add(instance);

        EquipByIndex(ownedAbilities.Count - 1);
    }

    void EquipByIndex(int index)
    {
        if (currentAbility != null)
            currentAbility.OnUnequip(gameObject);

        currentAbilityIndex = index;
        currentAbility = ownedAbilities[index];
        cooldownTimer = 0f;

        currentAbility.OnEquip(gameObject);
        Debug.Log("Equipped: " + currentAbility.GetType().Name);
    }

    public void SwapAbility(int direction)
    {
        if (ownedAbilities.Count == 0) return;

        int newIndex = (currentAbilityIndex + direction + ownedAbilities.Count) % ownedAbilities.Count;
        EquipByIndex(newIndex);
    }
}
