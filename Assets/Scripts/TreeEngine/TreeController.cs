using UnityEngine;

[RequireComponent(typeof(LSystemGenerator))]
[RequireComponent(typeof(TreeDrawer))]
public class TreeController : MonoBehaviour {

    private LSystemGenerator generator;
    private TreeDrawer drawer;

    private void Start() {

        generator = GetComponent<LSystemGenerator>();
        drawer = GetComponent<TreeDrawer>();

    }

    public string GrowTree() {

        string sequence = generator.Generate();
        drawer.DrawTree(sequence);

        return sequence;
    }
}
