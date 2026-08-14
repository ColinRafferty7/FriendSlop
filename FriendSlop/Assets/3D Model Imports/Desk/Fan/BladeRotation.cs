using Unity.VisualScripting;
using UnityEngine;

public class BladeRotation : MonoBehaviour
{
    [SerializeField] private float speed = 1000f;

    private void Update()
    {
        transform.Rotate(-Vector3.right, speed * Time.deltaTime);
    }
}
