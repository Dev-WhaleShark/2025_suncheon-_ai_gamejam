using UnityEngine;

public class Sludge : MonoBehaviour
{
    public int damage = 0;
    public float lifetime = 10.0f;

    public float slowAmount = 0.5f;
    public float slowDuration = 2.0f;
    private void Start()
    {
        Destroy(gameObject, lifetime); // Destroy after 10 seconds to prevent clutter
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Character characterComponent = other.GetComponent<Character>();
        if (characterComponent)
        {
            //other.GetComponent<Character>().OnTakeDamage(damage);
            characterComponent.OnSlow(slowAmount, slowDuration);
        }
    }
}
