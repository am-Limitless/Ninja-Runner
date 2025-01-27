using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NinjaRunner
{
    public class TileSpawner : MonoBehaviour
    {
        [SerializeField]
        private int tileStartCount = 10;
        [SerializeField]
        private int minimumStraightTiles = 3;
        [SerializeField]
        private int maximumStraightTiles = 15;
        [SerializeField]
        private GameObject startingTilePrefab;
        [SerializeField]
        private List<GameObject> turnTilePrefabs;
        [SerializeField]
        private List<GameObject> obstaclesPrefabs;
        [SerializeField]
        private GameObject coinPrefab;

        private Vector3 currentTileLocation = Vector3.zero;
        private Vector3 currentTileDirection = Vector3.forward;
        private GameObject prevTile;

        private List<GameObject> currentTiles;
        private List<GameObject> currentObstacles;

        private void Start()
        {
            currentTiles = new List<GameObject>();
            currentObstacles = new List<GameObject>();

            Random.InitState(System.DateTime.Now.Millisecond);

            for (int i = 0; i < tileStartCount; i++)
            {
                SpawnTile(startingTilePrefab.GetComponent<Tile>());
            }

            SpawnTile(SelectRandomGameObjectFromList(turnTilePrefabs).GetComponent<Tile>());

            // Start the coroutine to spawn obstacles after a delay
            StartCoroutine(SpawnObstaclesAfterDelay(10f));
        }

        private void SpawnTile(Tile tile, bool spawnObstacle = false)
        {
            Quaternion newTileRotation = tile.gameObject.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);

            prevTile = GameObject.Instantiate(tile.gameObject, currentTileLocation, newTileRotation);
            currentTiles.Add(prevTile);

            if (spawnObstacle)
            {
                SpawnObstacle();
            }
            // Spawn a coin with a certain probability
            if (Random.value < 0.5f) // 50% chance to spawn a coin
            {
                SpawnCoin();
            }

            if (tile.type == TileType.STRAIGHT)
            {
                currentTileLocation += Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size, currentTileDirection);
            }
        }

        private IEnumerator SpawnObstaclesAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            while (true) // Keep spawning obstacles indefinitely
            {
                SpawnObstacle();
                yield return new WaitForSeconds(2f); // Adjust the delay between obstacle spawns as needed
            }
        }

        private void SpawnCoin()
        {
            // Generate random offsets for X and Y
            float randomXOffset = Random.Range(-1.5f, 1.5f);
            float randomYOffset = Random.Range(1f, 3f);

            Vector3 coinPosition = prevTile.transform.position + new Vector3(randomXOffset, randomYOffset, 0);
            Instantiate(coinPrefab, coinPosition, Quaternion.identity);
        }

        public void AddNewDirection(Vector3 direction)
        {
            currentTileDirection = direction;
            DeletePreviousTiles();

            Vector3 tilePlacementScale;
            if (prevTile.GetComponent<Tile>().type == TileType.SIDEWAYS)
            {
                tilePlacementScale = Vector3.Scale(prevTile.GetComponent<Renderer>().bounds.size + (Vector3.one *
                    startingTilePrefab.GetComponent<BoxCollider>().size.z / 2), currentTileDirection);
            }
            else
            {
                tilePlacementScale = Vector3.Scale((prevTile.GetComponent<Renderer>().bounds.size - (Vector3.one * 2)) + (Vector3.one *
                    startingTilePrefab.GetComponent<BoxCollider>().size.z / 2), currentTileDirection);
            }

            currentTileLocation += tilePlacementScale;

            int currentPathLength = Random.Range(minimumStraightTiles, maximumStraightTiles);
            for (int i = 0; i < currentPathLength; i++)
            {
                SpawnTile(startingTilePrefab.GetComponent<Tile>(), (i == 0) ? false : true);
            }

            SpawnTile(SelectRandomGameObjectFromList(turnTilePrefabs).GetComponent<Tile>(), false);
        }

        private void SpawnObstacle()
        {
            // Define a minimum distance to avoid overlapping obstacles
            float minimumDistance = 1.0f;

            // Check if the previous tile is a turn tile
            Tile prevTileComponent = prevTile.GetComponent<Tile>();
            if (prevTileComponent != null && prevTileComponent.type == TileType.LEFT || prevTileComponent.type == TileType.RIGHT)
            {
                // Skip spawning obstacles on turn tiles
                return;
            }

            foreach (GameObject tile in currentTiles)
            {
                if (Random.value > 0.5f) return;

                Vector3 obstaclePosition = prevTile.transform.position;
                GameObject obstaclePrefab = SelectRandomGameObjectFromList(obstaclesPrefabs);
                if (obstaclePrefab == null) continue;

                // Calculate the position for the new obstacle
                Quaternion obstacleRotation = obstaclePrefab.transform.rotation * Quaternion.LookRotation(currentTileDirection, Vector3.up);
                Vector3 newObstaclePosition = obstaclePosition; // You can adjust this if needed

                // Check if the new obstacle position overlaps with existing obstacles
                bool canSpawn = true;
                foreach (GameObject existingObstacle in currentObstacles)
                {
                    if (Vector3.Distance(newObstaclePosition, existingObstacle.transform.position) < minimumDistance)
                    {
                        canSpawn = false;
                        break;
                    }
                }

                // Only spawn the obstacle if it doesn't overlap with existing ones
                if (canSpawn)
                {
                    GameObject obstacle = Instantiate(obstaclePrefab, newObstaclePosition, obstacleRotation);
                    currentObstacles.Add(obstacle);
                }
            }
        }
        private IEnumerator StartObstacleSpawningAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SpawnObstacle();
        }

        private void DeletePreviousTiles()
        {
            while (currentTiles.Count != 1)
            {
                GameObject tile = currentTiles[0];
                currentTiles.RemoveAt(0);
                Destroy(tile);
            }

            while (currentObstacles.Count != 0)
            {
                GameObject obstacle = currentObstacles[0];
                currentObstacles.RemoveAt(0);
                Destroy(obstacle);
            }
        }

        private GameObject SelectRandomGameObjectFromList(List<GameObject> list)
        {
            if (list.Count == 0) return null;

            return list[Random.Range(0, list.Count)];
        }
    }
}

