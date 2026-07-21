using UnityEngine;

public class AoePush : AbilityBase
{
    public override AbilityType Type => AbilityType.Active;
    [SerializeField] float pushForce = 10f;
    [SerializeField] float pushRadius = 5f;

    public override void Activate(GameObject user)
    {
        Collider[] hits = Physics.OverlapSphere(user.transform.position, pushRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.gameObject != user)
            {
                Vector3 dir = (hit.transform.position - user.transform.position);
                dir.y = 0;
                dir = (dir.normalized + Vector3.up * 0.5f).normalized;
                hit.attachedRigidbody.AddForce(dir * pushForce, ForceMode.Impulse);
            }
        }
    }
}