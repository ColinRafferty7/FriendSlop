using UnityEngine;

public class AoePull : AbilityBase
{
    public override AbilityType Type => AbilityType.Active;
    [SerializeField] float pullForce = 10f;
    [SerializeField] float upwardForce = 10f;
    [SerializeField] float pullRadius = 5f;

    public override void Activate(GameObject user)
    {
        Collider[] hits = Physics.OverlapSphere(user.transform.position, pullRadius);
        foreach (var hit in hits)
        {
            if (hit.attachedRigidbody != null && hit.gameObject != user)
            {
                Vector3 dir = (user.transform.position - hit.transform.position);
                Vector3 upForce = new (0f, upwardForce, 0f);
                dir.y = 0;
                dir = (dir.normalized * 0.5f).normalized;
                upForce = (upForce.normalized * 0.5f).normalized;
                dir.y = upForce.y;
                hit.attachedRigidbody.AddForce(dir * pullForce, ForceMode.Impulse);
            }
        }
    }
}
