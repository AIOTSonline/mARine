using UnityEngine;
using UnityEngine.EventSystems;

public class GridTapPlacer : MonoBehaviour
{
    public Camera arCamera;
    public ActorSelector actorSelector;
    public LayerManager layerManager;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            TryPlaceActor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            TryPlaceActor(Input.GetTouch(0).position);
        }
#endif
    }

    void TryPlaceActor(Vector2 screenPos)
    {
        if (layerManager == null)
        {
            Debug.LogWarning("LayerManager not assigned on GridTapPlacer!");
            return;
        }

        GridManager activeGridManager = layerManager.GetActiveGridManager();
        if (activeGridManager == null)
        {
            Debug.LogWarning("No active GridManager found!");
            return;
        }

        GameObject[,] activeGrid = activeGridManager.GetGrid();
        if (activeGrid == null)
        {
            Debug.LogWarning("Active grid is null!");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        Debug.Log($"Tapping on Layer {layerManager.GetActiveLayer()} — " +
                  $"grid size: {activeGrid.GetLength(0)}x{activeGrid.GetLength(1)}");

        GridCellScript hitCell = null;
        float closestDistance = Mathf.Infinity;

        for (int x = 0; x < activeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < activeGrid.GetLength(1); y++)
            {
                GameObject cellObject = activeGrid[x, y];
                if (cellObject == null) continue;

                Collider cellCollider = cellObject.GetComponent<Collider>();
                if (cellCollider == null) continue;

                if (cellCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        hitCell = cellObject.GetComponent<GridCellScript>();
                        Debug.Log($"Hit cell [{x},{y}] on Layer {layerManager.GetActiveLayer()} " +
                                  $"at distance {hit.distance}");
                    }
                }
            }
        }

        if (hitCell != null && !hitCell.isOccupied)
        {
            GameObject selectedActor = actorSelector.GetSelectedActor();
            if (selectedActor == null)
            {
                Debug.Log("No actor selected!");
                return;
            }

            hitCell.PlaceActor(selectedActor);
            Debug.Log($"Successfully placed actor on Layer {layerManager.GetActiveLayer()}");
            actorSelector.ClearSelection();
        }
        else if (hitCell != null && hitCell.isOccupied)
        {
            Debug.Log($"Cell already occupied on Layer {layerManager.GetActiveLayer()}!");
        }
        else
        {
            Debug.Log($"No cell hit on Layer {layerManager.GetActiveLayer()} — " +
                      $"tap may have missed the grid.");
        }
    }
}