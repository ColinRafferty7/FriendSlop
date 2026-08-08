using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class AuthorityManager : NetworkBehaviour
{
    [SerializeField] private bool componentMode;
    [SerializeField] private List<Component> componentList = new List<Component>();
    public override void OnNetworkSpawn()
    {
        if (componentMode)
        {
            DestroyComponents();
        }
        else
        {
            DestroyGameObject();
        }
    }

    private void DestroyComponents()
    {
        if (!IsServer)
        {
            foreach (var component in componentList)
            {
                if (component != null)
                    Destroy(component);
            }
        }
    }

    private void DestroyGameObject()
    {
        if (!IsServer)
        {
            Destroy(this.gameObject);
        }
    }
}
