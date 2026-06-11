/* using UnityEngine;

public class GridCellScript : MonoBehaviour
{
    public bool isOccupied = false;

    [Header("Layer")]
    public int layerIndex = 0;
    public SandboxSettings settings;

    private GameObject placedActor;
    public Renderer cellRenderer;

    private void Awake()
    {
        if (!cellRenderer)
            cellRenderer = GetComponent<Renderer>();
    }

    public void PlaceActor(GameObject actorPrefab)
    {
        if (isOccupied || actorPrefab == null) return;

        string uniqueID = System.Guid.NewGuid().ToString();

        // Get cube dimensions
        Vector3 cellSize = transform.localScale;
        float cellHeight = cellSize.y;

        // Place actor on TOP of cube surface
        Vector3 spawnPosition = transform.position;
        spawnPosition.y = transform.position.y + (cellHeight / 2f);

        placedActor = Instantiate(actorPrefab, spawnPosition, Quaternion.identity);
        placedActor.name = uniqueID;
        placedActor.tag = "Actor";

        ActorIdentity identity = placedActor.AddComponent<ActorIdentity>();
        identity.uniqueId = uniqueID;

        // Parent to layer root so visibility toggling works
        if (LayerManager.Instance != null)
        {
            GameObject layerRoot = LayerManager.Instance.GetRootForLayer(layerIndex);
            if (layerRoot != null)
            {
                placedActor.transform.SetParent(layerRoot.transform);
                Debug.Log($"Actor '{actorPrefab.name}' parented to '{layerRoot.name}'");
            }
            else
            {
                Debug.LogWarning($"Layer root for index {layerIndex} is null in LayerManager!");
            }

            // Register with LayerManager for visibility control
            LayerManager.Instance.RegisterActor(placedActor, layerIndex);
        }
        else
        {
            Debug.LogWarning("LayerManager.Instance is null — actor not parented or registered!");
        }

        PlacedActorData actorData = new PlacedActorData
        {
            prefabName = actorPrefab.name.Replace("(Clone)", "").Trim(),
            localPosition = spawnPosition,
            localRotation = Quaternion.identity,
            uniqueID = uniqueID,
            layerIndex = this.layerIndex
        };

        if (EnvironmentDataCache.currentData != null)
        {
            EnvironmentDataCache.currentData.GetLayerActors(layerIndex).Add(actorData);
            Debug.Log($"Added '{actorData.prefabName}' to cache Layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("EnvironmentDataCache is null!");
        }

        isOccupied = true;
    }

    float GetLayerHeight()
    {
        if (settings == null)
        {
            Debug.LogWarning("SandboxSettings null on GridCellScript!");
            return layerIndex * 1.5f;
        }

        if (layerIndex == 0) return settings.layer1Height;
        if (layerIndex == 1) return settings.layer2Height;
        return settings.layer3Height;
    }

    public void MarkAsUnusable()
    {
        isOccupied = true;
        if (cellRenderer)
            cellRenderer.material.color = Color.red;
    }
}*/
/* using UnityEngine;

public class GridCellScript : MonoBehaviour
{
   public bool isOccupied = false;

   [Header("Layer")]
   public int layerIndex = 0;
   public int totalLayerCount = 3;
   public SandboxSettings settings;

   private GameObject placedActor;
   public Renderer cellRenderer;

   private void Awake()
   {
       if (!cellRenderer)
           cellRenderer = GetComponent<Renderer>();
   }

   public void PlaceActor(GameObject actorPrefab)
   {
       if (isOccupied || actorPrefab == null) return;

       string uniqueID = System.Guid.NewGuid().ToString();

       Vector3 cellSize = transform.localScale;
       float cellHeight = cellSize.y;

       Vector3 spawnPosition = transform.position;
       spawnPosition.y = transform.position.y + (cellHeight / 2f);

       placedActor = Instantiate(actorPrefab, spawnPosition, Quaternion.identity);
       placedActor.name = uniqueID;
       placedActor.tag = "Actor";

       ActorIdentity identity = placedActor.AddComponent<ActorIdentity>();
       identity.uniqueId = uniqueID;

       if (LayerManager.Instance != null)
       {
           GameObject layerRoot = LayerManager.Instance.GetRootForLayer(layerIndex);
           if (layerRoot != null)
           {
               placedActor.transform.SetParent(layerRoot.transform);
               Debug.Log($"Actor parented to '{layerRoot.name}'");
           }
           else
           {
               Debug.LogWarning($"Layer root for index {layerIndex} is null!");
           }

           LayerManager.Instance.RegisterActor(placedActor, layerIndex);
       }
       else
       {
           Debug.LogWarning("LayerManager.Instance is null!");
       }

       PlacedActorData actorData = new PlacedActorData
       {
           prefabName = actorPrefab.name.Replace("(Clone)", "").Trim(),
           // Save as local offset from plane origin, not world position
           localPosition = new Vector3(
       transform.localPosition.x,
       0f, // Y will be recalculated from layer height at spawn time
       transform.localPosition.z
   ),
           localRotation = Quaternion.identity,
           uniqueID = uniqueID,
           layerIndex = this.layerIndex
       };

       if (EnvironmentDataCache.currentData != null)
       {
           EnvironmentDataCache.currentData.GetLayerActors(layerIndex).Add(actorData);
           Debug.Log($"Added '{actorData.prefabName}' to Layer {layerIndex}");
       }
       else
       {
           Debug.LogWarning("EnvironmentDataCache is null!");
       }

       isOccupied = true;
   }

   float GetLayerHeight()
   {
       if (settings == null) return layerIndex * 1.5f;
       return settings.GetLayerHeight(layerIndex, totalLayerCount);
   }

   public void MarkAsUnusable()
   {
       isOccupied = true;
       if (cellRenderer)
           cellRenderer.material.color = Color.red;
   }
}*/
using UnityEngine;

public class GridCellScript : MonoBehaviour
{
    public bool isOccupied = false;

    [Header("Layer")]
    public int layerIndex = 0;
    public int totalLayerCount = 3;
    public SandboxSettings settings;

    private GameObject placedActor;
    public Renderer cellRenderer;

    private void Awake()
    {
        if (!cellRenderer)
            cellRenderer = GetComponent<Renderer>();
    }

    public void PlaceActor(GameObject actorPrefab)
    {
        if (isOccupied || actorPrefab == null) return;

        string uniqueID = System.Guid.NewGuid().ToString();

        Vector3 cellSize = transform.localScale;
        float cellHeight = cellSize.y;

        Vector3 spawnPosition = transform.position;
        spawnPosition.y = transform.position.y + (cellHeight / 2f);

        placedActor = Instantiate(actorPrefab, spawnPosition, Quaternion.identity);
        placedActor.name = uniqueID;
        placedActor.tag = "Actor";

        ActorIdentity identity = placedActor.AddComponent<ActorIdentity>();
        identity.uniqueId = uniqueID;

        if (LayerManager.Instance != null)
        {
            GameObject layerRoot = LayerManager.Instance.GetRootForLayer(layerIndex);

            if (layerRoot != null)
            {
                placedActor.transform.SetParent(layerRoot.transform);
                Debug.Log($"Actor parented to '{layerRoot.name}'");
            }

            LayerManager.Instance.RegisterActor(placedActor, layerIndex);
        }

        Vector3 sandboxCenter = Vector3.zero;

        if (LayerManager.Instance != null)
        {
            sandboxCenter = LayerManager.Instance.sandboxCenter;
        }

        PlacedActorData actorData = new PlacedActorData
        {
            prefabName = actorPrefab.name.Replace("(Clone)", "").Trim(),

            // Save relative to sandbox center
            localPosition = new Vector3(
                transform.position.x - sandboxCenter.x,
                0f,
                transform.position.z - sandboxCenter.z
            ),

            localRotation = Quaternion.identity,
            uniqueID = uniqueID,
            layerIndex = this.layerIndex
        };

        if (EnvironmentDataCache.currentData != null)
        {
            EnvironmentDataCache.currentData
                .GetLayerActors(layerIndex)
                .Add(actorData);

            Debug.Log($"Added '{actorData.prefabName}' to Layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("EnvironmentDataCache is null!");
        }

        isOccupied = true;
    }

    float GetLayerHeight()
    {
        if (settings == null)
            return layerIndex * 1.5f;

        return settings.GetLayerHeight(layerIndex, totalLayerCount);
    }

    public void MarkAsUnusable()
    {
        isOccupied = true;

        if (cellRenderer)
            cellRenderer.material.color = Color.red;
    }
}