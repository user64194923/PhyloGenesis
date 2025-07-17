using UnityEngine;

public class TreeController : MonoBehaviour
{
    public LSystemGenerator generator;
    public TreeDrawer drawer;

    void Start()
    {
        string sequence = generator.Generate();
        drawer.DrawTree(sequence);
    }
}
