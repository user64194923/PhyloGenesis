using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(TreeController))]
public class Pot : MonoBehaviour, IInteractable {

    [SerializeField]
    private string FertilizerObjectName;

    [SerializeField]
    private string SoilObjectName;

    [SerializeField]
    private GameObject FertilizerObject;

    [SerializeField]
    private GameObject SoilObject;

    [SerializeField]
    private bool IsFertilized;

    [SerializeField]
    private bool IsSoiled;

    [SerializeField]
    private bool IsGrowed;

    private TreeController TreeController;

    [SerializeField]
    private string TreeSequence;

    private void Start() {



        TreeSequence = "";

        IsFertilized = false;
        IsSoiled = false;
        IsGrowed = false;

        TreeController = GetComponent<TreeController>();
    }



    public void Interact() {

        if (IsGrowed) return;

        PlayerInteraction PI = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInteraction>();

        if (PI == null) {

            Debug.LogWarning("Cant find PlayerInteraction reference");
            return;
        }

        string equippedObjectName = PI.GetEquippedObjectName();

        if (equippedObjectName == "") return;

        if (equippedObjectName == FertilizerObjectName) {
            FertilizerObject.SetActive(true);
            IsFertilized = true;
        }

        if (equippedObjectName == SoilObjectName) {
            SoilObject.SetActive(true);
            IsSoiled = true;
        }

        if (IsFertilized && IsSoiled) {
            TreeSequence = TreeController.GrowTree();
            IsGrowed = true;
        }

    }
    
}
