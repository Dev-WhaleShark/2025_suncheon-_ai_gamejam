using UnityEngine;

public class TouchDamage : MonoBehaviour
{
    public int damage = 30;
    void OnTriggerEnter2D(Collider2D collider)
    {
        Character characterComponent = collider.GetComponent<Character>();
        if (characterComponent)
        {
            characterComponent.OnTakeDamage(damage);
        }
    }
}
