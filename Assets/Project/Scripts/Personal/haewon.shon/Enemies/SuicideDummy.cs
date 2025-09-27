using UnityEngine;

public class SuicideDummy : Enemy
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OnDied();   
    }
}
