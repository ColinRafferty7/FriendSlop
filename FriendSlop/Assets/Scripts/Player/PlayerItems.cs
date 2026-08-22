using System.Collections.Generic;
using UnityEngine;
using static BallController;
public class PlayerItems : MonoBehaviour
{
    [SerializeField] int maxOwnedAbilities = 3;
    List<AbilityBase> ownedAbilities = new List<AbilityBase>();
    [SerializeField] bool replaceOldestWhenFull = true;
    int currentAbilityIndex = -1;
    AbilityBase currentAbility;
    float cooldownTimer = 0f;

    [SerializeField] private GameObject reticlePrefab;
    private Reticle reticle;

    void Awake()
    {
        GameObject retObj = Instantiate(reticlePrefab, Vector3.zero, Quaternion.identity);
        reticle = retObj.GetComponent<Reticle>();
        reticle.gameObject.SetActive(false);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    public void AttemptAim()
    {
        if (currentAbility != null && cooldownTimer <= 0f &&
            (currentAbility.Type == AbilityType.Active || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            currentAbility.Aim(gameObject, reticle);
        }
    }

    public void AttemptActivation()
    {
        if (currentAbility != null && cooldownTimer <= 0f &&
            (currentAbility.Type == AbilityType.Active || currentAbility.Type == AbilityType.ActiveAndPassive))
        {
            reticle.gameObject.SetActive(false);
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
    }
    public void SwapAbility(int direction)
    {
        if (ownedAbilities.Count == 0) return;
        int newIndex = (currentAbilityIndex + direction + ownedAbilities.Count) % ownedAbilities.Count;
        EquipByIndex(newIndex);
    }

    public static GameObject FindClosestTargetInFront(Transform transform, float searchRadius)
    {
        Collider[] candidates = Physics.OverlapSphere(transform.position, searchRadius);

        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in candidates)
        {
            if (col.attachedRigidbody == null) continue;

            Vector3 toTarget = col.transform.position - transform.position;
            toTarget.y = 0;

            if (toTarget.magnitude < 0.01f) continue;

            Vector3 dirToTarget = toTarget.normalized;
            float dot = Vector3.Dot(transform.forward, dirToTarget);

            if (dot > 0f)
            {
                float dist = toTarget.magnitude;
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = col.gameObject;
                }
            }
        }
        return closest;
    }

    public static GameObject ClosestTarget(Transform transform, float radius)
    {
        Collider[] candidates = Physics.OverlapSphere(transform.position, radius);

        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var col in candidates)
        {
            if (col.attachedRigidbody == null) continue;

            Vector3 toTarget = col.transform.position - transform.position;
            toTarget.y = 0;

            if (toTarget.magnitude < 0.01f) continue;

            if (toTarget.magnitude < closestDist)
            {
                closestDist = toTarget.magnitude;
                closest = col.gameObject;
            }
        }
        return closest;
    }
}