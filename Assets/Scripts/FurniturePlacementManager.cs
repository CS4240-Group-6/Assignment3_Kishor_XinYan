using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class FurniturePlacementManager : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Button deleteButton;

    private GameObject selectedFurniturePrefab;
    private GameObject selectedFurnitureInstance;
    private List<GameObject> placedFurnitures = new List<GameObject>(); // Keep track of all placed furniture

    private void Start()
    {
        deleteButton.onClick.AddListener(DeleteSelectedFurniture);
    }

    void Update()
    {
        if (Input.touchCount == 0 || EventSystem.current.IsPointerOverGameObject())
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            TryToSelectOrPlaceFurniture(touch.position);
        }
        else if (touch.phase == TouchPhase.Moved && selectedFurnitureInstance != null)
        {
            MoveFurniture(touch.position);
        }
        else if (touch.phase == TouchPhase.Ended)
        {
            selectedFurnitureInstance = null; // Deselect after moving
        }
    }

    public void SelectFurniturePrefab(GameObject prefab)
    {
        selectedFurniturePrefab = prefab;
        DeselectFurnitureInstance();
        debugText.text = "Selected prefab: " + prefab.name;
    }

    private void TryToSelectOrPlaceFurniture(Vector2 screenPosition)
    {
        // Perform the raycast
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Furniture"))
        {
            // If an existing furniture item is hit, select it
            selectedFurnitureInstance = hit.collider.gameObject;
            debugText.text = "Selected instance: " + selectedFurnitureInstance.name;
        }
        else
        {
            // If no existing furniture is hit, try to place a new one
            TryPlaceNewFurniture(screenPosition);
        }
    }

    private void TryPlaceNewFurniture(Vector2 screenPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            if (selectedFurniturePrefab != null && !IsPositionOccupied(hitPose.position))
            {
                GameObject furnitureObject = Instantiate(selectedFurniturePrefab, hitPose.position, Quaternion.identity);
                furnitureObject.transform.localScale = Vector3.zero;
                StartCoroutine(ScaleFurniture(furnitureObject, Vector3.one, 0.5f));
                furnitureObject.tag = "Furniture";
                AddRigidbody(furnitureObject);
                placedFurnitures.Add(furnitureObject);

                debugText.text = "Placed new furniture: " + selectedFurniturePrefab.name;
            }
        }
    }

    private void PlaceFurniture(Pose hitPose)
    {
        GameObject furnitureObject = Instantiate(selectedFurniturePrefab, hitPose.position, Quaternion.identity);
        furnitureObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
        furnitureObject.transform.localScale = Vector3.zero; // Start with scale zero
        StartCoroutine(ScaleFurniture(furnitureObject, new Vector3(0.5f, 0.5f, 0.5f), 0.5f)); // Animate scale to Vector3.one
        furnitureObject.tag = "Furniture"; // Tag the furniture
        AddRigidbody(furnitureObject); // Add Rigidbody for physics interactions
        placedFurnitures.Add(furnitureObject); // Add to list of placed furniture

        selectedFurnitureInstance = furnitureObject; // Keep reference to the placed object
        debugText.text = "Placed new furniture: " + selectedFurniturePrefab.name;
    }

    private bool IsPositionOccupied(Vector3 position, GameObject ignoreObject = null)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Furniture") && collider.gameObject != ignoreObject)
            {
                debugText.text = "Position is occupied by another furniture item.";
                return true;
            }
        }
        return false;
    }

    private void MoveFurniture(Vector2 screenPosition)
    {
        if (selectedFurnitureInstance == null)
            return;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            // Prevent moving the furniture into a position occupied by another furniture
            if (!IsPositionOccupied(hitPose.position, selectedFurnitureInstance))
            {
                selectedFurnitureInstance.transform.position = hitPose.position;
                debugText.text = "Moved furniture to new position.";
            }
        }
    }

    private void DeleteSelectedFurniture()
    {
        if (selectedFurnitureInstance != null)
        {
            placedFurnitures.Remove(selectedFurnitureInstance); // Remove from placed furniture list
            Destroy(selectedFurnitureInstance);
            DeselectFurnitureInstance();
            debugText.text = "Deleted furniture.";
        }
    }

    private IEnumerator ScaleFurniture(GameObject furniture, Vector3 targetScale, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            furniture.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        furniture.transform.localScale = targetScale;
    }

    private void AddRigidbody(GameObject furniture)
    {
        Rigidbody rb = furniture.AddComponent<Rigidbody>();
        rb.useGravity = false; // Set to false to prevent the furniture from falling
        rb.mass = 100.0f; // Adjust mass as needed
    }

    private void DeselectFurnitureInstance()
    {
        selectedFurnitureInstance = null; // Deselect the current furniture
        debugText.text = "Deselected furniture.";
    }
}
