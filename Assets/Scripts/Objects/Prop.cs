using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Prop : MonoBehaviour {

    [SerializeField]
    private string PropName;



    public string GetPropName() {
        return PropName;
    }
}
