using UnityEngine;

public class TestAbility : AbilityBase
{
    public override AbilityType Type => AbilityType.Active;

    public override void Activate(GameObject user)
    {
        Debug.Log("Test ability activated on " + user.name);
    }
}
