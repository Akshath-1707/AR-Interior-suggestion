using UnityEngine;

namespace ARInterior
{
    public class FurnitureSpawner : MonoBehaviour
    {
        [Header("Spawner Configuration")]
        [SerializeField] private Transform spawnParent;

        private GameObject currentSpawnedModel;

        public void SpawnFurniturePrefab(GameObject prefabToSpawn, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (currentSpawnedModel != null)
            {
                Destroy(currentSpawnedModel);
            }

            if (prefabToSpawn != null)
            {
                currentSpawnedModel = Instantiate(prefabToSpawn, spawnPosition, spawnRotation, spawnParent);
                Debug.Log($"[SPAWNER] Spawned model {prefabToSpawn.name} at {spawnPosition}");
            }
        }

        public void RemoveCurrentFurniture()
        {
            if (currentSpawnedModel != null)
            {
                Destroy(currentSpawnedModel);
                currentSpawnedModel = null;
            }
        }
    }
}
