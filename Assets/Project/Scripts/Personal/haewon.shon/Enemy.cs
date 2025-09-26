using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float HP;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnTakeDamage(int damage)
    {
        HP -= damage;

        if (HP <= 0)
        {
            Debug.Log(gameObject.name + " is dead!");
            Destroy(gameObject);
        }
    }
}
