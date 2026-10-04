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

        [Header("Default Prefabs (Optional 3D Models)")]
        [SerializeField] private GameObject sofaPrefab;
        [SerializeField] private GameObject chairPrefab;
        [SerializeField] private GameObject deskPrefab;

        /// <summary>
        /// Spawn a 3D furniture object at the measured room coordinates.
        /// </summary>
        public GameObject SpawnFurniture(GameObject prefab, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            // If there is an unlocked active object, replace it
            if (currentActiveObject != null && !lockedFurnitureObjects.Contains(currentActiveObject))
            {
                Destroy(currentActiveObject);
                currentActiveObject = null;
            }

            if (prefab == null)
            {
                // Clean single fallback primitive
                currentActiveObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                currentActiveObject.name = "AR_Furniture_Placeholder";
                currentActiveObject.transform.position = spawnPosition + Vector3.up * 0.35f; // Sit on floor
                currentActiveObject.transform.rotation = spawnRotation;
                currentActiveObject.transform.localScale = new Vector3(1.4f, 0.7f, 0.8f); // Realistic couch scale

                Renderer ren = currentActiveObject.GetComponent<Renderer>();
                if (ren != null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
                    Material mat = new Material(s);
                    mat.color = new Color(0.92f, 0.90f, 0.86f); // Warm neutral white
                    ren.material = mat;
                }
            }
            else
            {
                currentActiveObject = Instantiate(prefab, spawnPosition, spawnRotation);
                currentActiveObject.name = prefab.name + "_AR";
            }

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
            currentActiveObject = null; // Staged! Ready to place the next item!
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
