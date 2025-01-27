using UnityEngine;

public class Coin : MonoBehaviour
{
    public float rotationSpeed = 100f;

    private void Update()
    {
        // Rotate the coin
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player collected the coin
        if (other.CompareTag("Player"))
        {
            // Add your coin collection logic here (e.g., increase score)
            Destroy(gameObject); // Destroy the coin after collection
        }
    }
}
