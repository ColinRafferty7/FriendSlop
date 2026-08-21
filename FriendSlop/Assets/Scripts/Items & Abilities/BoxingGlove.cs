using UnityEngine;
public class BoxingGlove : AbilityBase
{
    public override AbilityType Type => AbilityType.Active;
    [SerializeField] float pushForce = 10f;
    [SerializeField] float range = 5f;
    [SerializeField] float speed = 5f;
    [SerializeField] float spawnOffsetMultiplier = 1.3f;
    [SerializeField] GameObject glovePrefab;
    public override void Activate(GameObject user)
    {
        PlayerPhysics physics = user.GetComponent<PlayerPhysics>();
        PlayerStats stats = user.GetComponent<PlayerStats>();
        if (physics == null || stats == null || glovePrefab == null) return;

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
        GameObject gloveObj = Instantiate(glovePrefab, user.transform.position, Quaternion.LookRotation(direction, Vector3.up));
        GloveProjectile glove = gloveObj.GetComponent<GloveProjectile>();
        glove.Init(user, direction, pushForce, range, speed, stats.GetBallRadius(), spawnOffsetMultiplier);
    }
}