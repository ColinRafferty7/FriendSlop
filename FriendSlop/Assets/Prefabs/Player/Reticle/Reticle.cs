using UnityEngine;

public class Reticle : MonoBehaviour
{
    private GameObject target;

    public void SetTarget(GameObject target)
    {
        this.target = target;
    }

    void LateUpdate()
    {
        transform.position = target.transform.position - (Vector3.down * target.transform.localScale.x);
    }
}
