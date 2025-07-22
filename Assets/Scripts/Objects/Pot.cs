using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pot : MonoBehaviour, IInteractable {

    [SerializeField]
    private string FertilizerObjectName;

    [SerializeField]
    private string SoilObjectName;

    [SerializeField]
    private GameObject FertilizerObject;



    [SerializeField]
    private GameObject SoilObject;


    public void Interact() {

        Debug.Log("interact");

        PlayerInteraction PI = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerInteraction>();

        if (PI == null) {

            Debug.LogWarning("Cant find PlayerInteraction reference");
            return;
        }

        string equippedObjectName = PI.GetEquippedObjectName();

        if (equippedObjectName == "") return;

        if (equippedObjectName == FertilizerObjectName) {
            FertilizerObject.SetActive(true);
        }

        if (equippedObjectName == SoilObjectName) {
            SoilObject.SetActive(true);
        }

    }
    
}
