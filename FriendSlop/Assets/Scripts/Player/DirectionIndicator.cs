using UnityEngine;

public class DirectionIndicator : MonoBehaviour
{
    private LineRenderer line;
    [SerializeField] private Transform player;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void ApplyDirection(Vector3 deltaDir)
    {
        line.SetPosition(0, player.position + deltaDir * 0.25f);
        line.SetPosition(1, player.position + deltaDir * 0.75f);
    }
}
