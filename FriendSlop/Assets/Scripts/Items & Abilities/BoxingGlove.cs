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
        GameObject target = PlayerItems.ClosestTarget(user.transform, range);

        Vector3 direction;
        if (target != null)
        {
            direction = target.transform.position - user.transform.position;
            direction.y = 0;
            direction.Normalize();
        }
        else
        {
            return;
            //direction = user.transform.forward;
        }

        GameObject gloveObj = Instantiate(glovePrefab, user.transform.position, Quaternion.LookRotation(direction, Vector3.up));
        GloveProjectile glove = gloveObj.GetComponent<GloveProjectile>();
        glove.Init(user, direction, pushForce, range, speed, 1f, spawnOffsetMultiplier);
    }
}
