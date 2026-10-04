using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace ARInterior
{
    /// <summary>
    /// FurnitureSpawner: Handles spawning 3D furniture models onto the floor,
    /// and locking their position in physical AR space so you can walk around them freely.
    /// </summary>
    public class FurnitureSpawner : MonoBehaviour
    {
        [Header("Active Furniture")]
        public GameObject currentActiveObject;
        public List<GameObject> lockedFurnitureObjects = new List<GameObject>();

        [Header("Default Prefabs (Fallback / Testing)")]
        [SerializeField] private GameObject sofaPrefab;
        [SerializeField] private GameObject chairPrefab;
        [SerializeField] private GameObject deskPrefab;

        /// <summary>
        /// Spawn a 3D furniture object at the measured room coordinates.
        /// </summary>
        public GameObject SpawnFurniture(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (currentActiveObject != null && !lockedFurnitureObjects.Contains(currentActiveObject))
            {
                Destroy(currentActiveObject);
            }

            if (prefab == null)
            {
                // Fallback cube if model prefab is unassigned
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prefab.transform.localScale = new Vector3(1.2f, 0.7f, 0.7f); // Couch proportions
            }

            currentActiveObject = Instantiate(prefab, spawnPosition, spawnRotation);
            currentActiveObject.name = prefab.name + "_AR";

            Debug.Log($"[FURNITURE SPAWNER] Spawned {currentActiveObject.name} at {spawnPosition}");
            return currentActiveObject;
        }

        /// <summary>
        /// LOCK POSITION: Attaches an ARAnchor and locks the transform so it stays fixed in real space.
        /// </summary>
        public void LockCurrentObject()
        {
            if (currentActiveObject == null)
            {
                Debug.LogWarning("[FURNITURE SPAWNER] No active furniture to lock!");
                return;
            }

            // Attach ARAnchor if on an ARCore device
            ARAnchor anchor = currentActiveObject.GetComponent<ARAnchor>();
            if (anchor == null)
            {
                anchor = currentActiveObject.AddComponent<ARAnchor>();
            }

            if (!lockedFurnitureObjects.Contains(currentActiveObject))
            {
                lockedFurnitureObjects.Add(currentActiveObject);
            }

            Debug.Log($"[FURNITURE SPAWNER] 🔒 Successfully LOCKED {currentActiveObject.name} in place! Total locked items: {lockedFurnitureObjects.Count}");
            currentActiveObject = null; // Ready to place next furniture!
        }

        /// <summary>
        /// Clear all placed furniture from the room.
        /// </summary>
        public void ClearAllFurniture()
        {
            if (currentActiveObject != null)
            {
                Destroy(currentActiveObject);
                currentActiveObject = null;
            }

            foreach (var item in lockedFurnitureObjects)
            {
                if (item != null) Destroy(item);
            }
            lockedFurnitureObjects.Clear();
            Debug.Log("[FURNITURE SPAWNER] Cleared all furniture from scene.");
        }
    }
}
