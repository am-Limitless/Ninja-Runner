using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField]
    private float rotationSpeed = 100f;
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
            //playerController.ScoreManager();
            Destroy(gameObject);
        }
    }
}
