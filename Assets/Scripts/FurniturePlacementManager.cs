using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class FurniturePlacementManager : MonoBehaviour
{
    public GameObject SpawnableFurniture;
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    private GameObject selectedFurniture = null;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    private void Update() {
        if(Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);

            if(touch.phase == TouchPhase.Began) {
                // if(EventSystem.current.IsPointerOverGameObject(touch.fingerId)) {
                //     return;
                // }

                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                RaycastHit hit;

                bool collision = raycastManager.Raycast(touch.position, raycastHits, TrackableType.PlaneWithinPolygon);

                // Selecting or placing furniture based on touch input.
                if (Physics.Raycast(ray, out hit)) {
                    if (hit.collider.gameObject.CompareTag("Furniture")) {
                        selectedFurniture = hit.collider.gameObject;
                        // Maybe we can highlight selected furniture.
                        // Can just set the selected gameobject to a different color
                        return;
                    }
                }

                // Handling the placement of new furniture if no existing furniture was selected.
                if (collision) {
                    Pose hitPose = raycastHits[0].pose;
                    if (selectedFurniture == null) {
                        PlaceFurniture(hitPose.position, hitPose.rotation);
                    }
                }
            } else if(touch.phase == TouchPhase.Moved && selectedFurniture != null) {
                MoveFurniture(touch.position);
            }
        }
    }

    void PlaceFurniture(Vector3 position, Quaternion rotation) {
        GameObject item = Instantiate(SpawnableFurniture, position, rotation);

        foreach (var plane in planeManager.trackables) 
        {
            plane.gameObject.SetActive(false);
        }

        planeManager.enabled = false;
    }

    void MoveFurniture(Vector2 touchPosition) {
        bool collision = raycastManager.Raycast(touchPosition, raycastHits, TrackableType.PlaneWithinPolygon);

        if (collision) {
            selectedFurniture.transform.position = raycastHits[0].pose.position;
            selectedFurniture.transform.rotation = raycastHits[0].pose.rotation;
        }
    }

    public void DeleteSelectedFurniture() {
        if (selectedFurniture != null) {
            Destroy(selectedFurniture);
            selectedFurniture = null;
        }
    }

    public bool isButtonPressed() {
        return EventSystem.current.currentSelectedGameObject != null;
    }

    public void SwitchFurniture(GameObject furniture) {
        SpawnableFurniture = furniture;
    }
}
