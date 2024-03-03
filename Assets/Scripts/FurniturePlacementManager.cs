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
    }

    public void SelectFurniturePrefab(GameObject prefab)
    {
        DeselectFurnitureInstance();
        selectedFurniturePrefab = prefab;
        debugText.text = "Selected prefab: " + prefab.name;
    }

    private void TryToSelectOrPlaceFurniture(Vector2 screenPosition)
    {
        // Try to select an existing furniture instance
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Furniture"))
        {
            selectedFurnitureInstance = hit.collider.gameObject;
            debugText.text = "Selected instance: " + hit.collider.gameObject.name;
        }
        else
        {
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                if (selectedFurniturePrefab != null)
                {
                    GameObject furnitureObject = Instantiate(selectedFurniturePrefab, hitPose.position, Quaternion.identity);
                    furnitureObject.transform.localScale = Vector3.zero; // Start with scale zero
                    StartCoroutine(ScaleFurniture(furnitureObject, Vector3.one, 0.5f)); // Animate scale to Vector3.one
                    furnitureObject.tag = "Furniture"; // Tag the furniture
                    AddRigidbody(furnitureObject); // Add Rigidbody for physics interactions

                    selectedFurnitureInstance = furnitureObject; // Keep reference to the placed object
                    debugText.text = "Placed new furniture: " + selectedFurniturePrefab.name;
                }
            }
        }
    }

    private bool IsPositionOccupied(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.5f); // Check for colliders in a small radius around the position
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Furniture"))
            {
                debugText.text = "Position is occupied by another furniture item.";
                return true; // Position is occupied by another furniture item
            }
        }
        return false;
    }

    private void MoveFurniture(Vector2 screenPosition)
    {
        List<ARRaycastHit> hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            selectedFurnitureInstance.transform.position = hitPose.position;
            debugText.text = "Moved furniture to new position.";
        }
    }

    private void DeleteSelectedFurniture()
    {
        if (selectedFurnitureInstance != null)
        {
            Destroy(selectedFurnitureInstance);
            DeselectFurnitureInstance();
            debugText.text = "Deleted furniture.";
        }
    }

    private void DeselectFurnitureInstance()
    {
        selectedFurnitureInstance = null;
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
}
