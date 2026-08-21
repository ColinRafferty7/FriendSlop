using UnityEngine;
public class HookAbility : AbilityBase
{
    public override AbilityType Type => AbilityType.Active;
    [SerializeField] float range = 6f;
    [SerializeField] float speed = 6f;
    [SerializeField] float spawnOffsetMultiplier = 1.3f;
    [SerializeField] GameObject hookPrefab;
    public override void Activate(GameObject user)
    {
        PlayerPhysics physics = user.GetComponent<PlayerPhysics>();
        PlayerStats stats = user.GetComponent<PlayerStats>();
        if (physics == null || stats == null || hookPrefab == null) return;

        GameObject target = physics.FindClosestTargetInFront(range);
        Vector3 direction;
        if (target != null)
        {
            direction = target.transform.position - user.transform.position;
            direction.y = 0;
            direction.Normalize();
        }
        else
        {
            direction = physics.Front;
        }
        GameObject hookObj = Instantiate(hookPrefab, user.transform.position, Quaternion.LookRotation(direction, Vector3.up));
        HookProjectile hook = hookObj.GetComponent<HookProjectile>();
        hook.Init(user, direction, range, speed, stats.GetBallRadius(), spawnOffsetMultiplier);
    }
}