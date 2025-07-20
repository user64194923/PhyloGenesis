using UnityEngine;
using System.Collections;

public class PlayerInteraction : MonoBehaviour {

    #region Fields

    [Header("Input")]
    [SerializeField]
    private KeyCode InteractKey;
    [SerializeField]
    private KeyCode EquipKey;
    [SerializeField]
    private KeyCode FireKey;

    [Header("Settings")]
    [SerializeField]
    private float interactionRange = 4.0f;
    [SerializeField]
    private LayerMask interactableLayer;
    [SerializeField]
    private float ObjectThrowForce;
    [SerializeField]
    private float ObjectHoldForce;

    [Tooltip("Sway ampunt for equipped object")]
    [SerializeField]
    private float SwayMultiplier;
    [Tooltip("Sway smooth factor for equipped object")]
    [SerializeField]
    private float SwaySmooth;

    [Header("References")]
    [SerializeField]
    private GameObject Player;
    // [SerializeField]
    // private PlayerUI PlayerUI;
    [SerializeField]
    private Transform playerCamera;
    [SerializeField]
    private Transform ObjectHoldingPoint;
    [SerializeField]
    private Transform ObjectEquipPoint;

    // heldObject tmp variables
    private GameObject heldObject;
    private Rigidbody heldObjectRb;
    private GameObject equippedObject;
    private Rigidbody equippedObjectRb;

    public bool IsHoldingObject;
    public bool IsEquippedObject;

    #endregion

    #region MonoBehaviour

    private void Start() {
        IsHoldingObject = false;
        IsEquippedObject = false;
    }

    private void Update() {

        if (heldObject != null) {

            MoveHeldObject();

            if (Input.GetKeyUp(InteractKey)) DropObject();
            if (Input.GetKeyDown(FireKey)) ThrowObject();

        }

        if (equippedObject != null) {

            EquippedObjectSway();

            if (Input.GetKeyDown(EquipKey)) UnEquipObject();
        }

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionRange, interactableLayer)) {

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            // if (heldObject == null) PlayerUI.ActivateCrosshairRing(true);

            if (Input.GetKeyDown(InteractKey)) {
                if (interactable != null) {
                    interactable.Interact();
                } else {
                    if (!Input.GetKey(KeyCode.Mouse1)) PickUpObject(hit);
                }
            }

            if (Input.GetKeyDown(EquipKey)) {
                if (!IsEquippedObject) EquipObject(hit);
                
            }

            
        } else {
            // PlayerUI.ActivateCrosshairRing(false);
        }

    }

    #endregion

    #region Methods

    private void EquipObject(RaycastHit hit) {
        equippedObjectRb = hit.transform.gameObject.GetComponent<Rigidbody>();

        if (equippedObjectRb != null && !equippedObjectRb.isKinematic) {

            IsEquippedObject = true;

            equippedObject = hit.transform.gameObject;
            equippedObject.transform.position = ObjectEquipPoint.position;
            equippedObject.transform.rotation = ObjectEquipPoint.rotation;
            equippedObjectRb.useGravity = false;
            equippedObjectRb.constraints = RigidbodyConstraints.FreezeRotation;
            equippedObjectRb.transform.parent = ObjectEquipPoint;
            equippedObjectRb.isKinematic = true;

            Physics.IgnoreCollision(equippedObject.GetComponent<Collider>(), Player.GetComponent<Collider>(), true);

        } else {
            Debug.Log("Cant equip this object");
        }
    }

    private void UnEquipObject() {

        Debug.Log("UnEquip");

        if (equippedObject == null) return;

        equippedObject.transform.position = ObjectEquipPoint.position;
        equippedObject.transform.rotation = ObjectEquipPoint.rotation;
        equippedObjectRb.useGravity = true;
        equippedObjectRb.constraints = RigidbodyConstraints.None;
        equippedObjectRb.transform.parent = null;
        equippedObjectRb.isKinematic = false;

        equippedObjectRb.AddForce(ObjectEquipPoint.forward * (ObjectThrowForce * 0.1f), ForceMode.Impulse);

        StartCoroutine(ClearEquippedObject());

    }

    private void EquippedObjectSway() {

        if (!IsEquippedObject) return;
        if (equippedObject == null) return;

        // get mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * SwayMultiplier;
        float mouseY = Input.GetAxisRaw("Mouse Y") * SwayMultiplier;

        // calculate target rotation
        Quaternion rotationX = Quaternion.AngleAxis(-mouseY, Vector3.right);
        Quaternion rotationY = Quaternion.AngleAxis(mouseX, Vector3.up);

        Quaternion targetRotation = rotationX * rotationY;

        // rotate 
        equippedObject.transform.localRotation = Quaternion.Slerp(equippedObject.transform.localRotation, targetRotation, SwaySmooth * Time.deltaTime);


    }

    private void PickUpObject(RaycastHit hit) {


        heldObjectRb = hit.transform.gameObject.GetComponent<Rigidbody>();

        if (heldObjectRb != null && !heldObjectRb.isKinematic) {

            IsHoldingObject = true;

            heldObject = hit.transform.gameObject;
            heldObjectRb.useGravity = false;
            heldObjectRb.drag = 4;
            heldObjectRb.constraints = RigidbodyConstraints.FreezeRotation;
            heldObjectRb.transform.parent = ObjectHoldingPoint;

            Physics.IgnoreCollision(heldObject.GetComponent<Collider>(), Player.GetComponent<Collider>(), true);

            MoveHeldObject();

        } else {
            Debug.Log("Cant interact or hold this object");
        }

    }

    private void MoveHeldObject() {

        if (Vector3.Distance(heldObject.transform.position, ObjectHoldingPoint.position) > 0.1f) {
            Vector3 moveDirection = (ObjectHoldingPoint.position - heldObject.transform.position);
            heldObjectRb.AddForce(moveDirection * ObjectHoldForce);
        }
        
    }

    private void DropObject() {

        if (heldObjectRb != null) {
            Physics.IgnoreCollision( heldObject.GetComponent<Collider>(), Player.GetComponent<Collider>(), false);

            heldObjectRb.drag = 1;
            heldObjectRb.constraints = RigidbodyConstraints.None;

            heldObjectRb.useGravity = true;
            heldObject.transform.parent = null;
            heldObject = null;
            heldObjectRb = null;
            IsHoldingObject = false;
        }
    }

    private void ThrowObject() {

        if (heldObjectRb != null) {
            Physics.IgnoreCollision( heldObject.GetComponent<Collider>(), Player.GetComponent<Collider>(), false);
            heldObject.transform.parent = null;

            heldObjectRb.drag = 0;
            heldObjectRb.constraints = RigidbodyConstraints.None;

            heldObjectRb.useGravity = true;
            
            
            heldObjectRb.AddForce(ObjectHoldingPoint.forward * ObjectThrowForce, ForceMode.Impulse);

            heldObject = null;
            heldObjectRb = null;
            StartCoroutine(ClearHoldObject());
        }
    }

    private IEnumerator ClearEquippedObject() {

        yield return new WaitForSeconds(0.1f);
        equippedObject = null;
        equippedObjectRb = null;
        IsEquippedObject = false;
    }

    private IEnumerator ClearHoldObject() {

        yield return new WaitForSeconds(0.1f);
        heldObject = null;
        heldObjectRb = null;
        IsHoldingObject = false;
    }


    private void StopClipping() {

        var clipRange = Vector3.Distance(heldObject.transform.position, ObjectHoldingPoint.position);

        RaycastHit[] hits;
        hits = Physics.RaycastAll(ObjectHoldingPoint.position, ObjectHoldingPoint.TransformDirection(Vector3.forward), clipRange);

        if (hits.Length > 1) {

            //offset slightly downward to stop object dropping above player 
            heldObject.transform.position = ObjectHoldingPoint.position + new Vector3(0f, -0.5f, 0f); 

        }
    }

    #endregion



    #region Getters


    public bool GetIsHoldingObject() {
        if (heldObject == null) return false;
        else return true;
    }
    #endregion
}
