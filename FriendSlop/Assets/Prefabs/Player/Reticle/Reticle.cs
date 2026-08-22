using UnityEngine;

public class Reticle : MonoBehaviour
{
    private GameObject target;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void LateUpdate()
    {
        if (target == null) return;
        RaycastHit hit;
        if (Physics.Raycast(target.transform.position, Vector3.down, out hit))
        {
            transform.position = hit.point + new Vector3(0f, 0.01f, 0f);
            transform.localScale = target.transform.localScale * 0.25f;
        }
    }

    public void SetTarget(GameObject target)
    {
        this.target = target;

    }
    
}
