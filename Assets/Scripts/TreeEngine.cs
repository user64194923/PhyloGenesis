using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeEngine : MonoBehaviour
{
    [SerializeField] private GameObject treeTrunkPrefab;
    [SerializeField] private GameObject branchPrefab;
    [SerializeField] private GameObject leafPrefab;
    [SerializeField] private Transform startPoint;
    private TreeData treeData;

    public int iters = 100;

    public class TreeData
    {
        public string seed = "";
        public int GrowthIterations = 20;
        public int TreeHeight = 30;
        public int NumberOfBranches = 10;
        public float BranchSpread = 2.1f;
        public float BranchAngle = 2.1f;
        public float BranchScale = 2.1f;
        public float LeafHeight = 2.1f;
    }

    private void Start()
    {
        treeData = new TreeData();
        GrowTree(startPoint.position, Vector3.down, treeData.TreeHeight);
    }

    private void GrowTree(Vector3 position, Vector3 direction, int height)
    {

        if (height <= 0)
        {
            return;
        }

        // Создаем ствол дерева
        GameObject trunk = Instantiate(treeTrunkPrefab, position, Quaternion.identity);
        trunk.transform.LookAt(position + direction);

        // Создаем ветви
        Vector3 branchDirection = Quaternion.AngleAxis(treeData.BranchAngle, Vector3.up) * direction;
        for (int i = 0; i < treeData.NumberOfBranches; i++)
        {
            float angle = i * Mathf.PI * 2 / treeData.NumberOfBranches;
            Vector3 branchOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * treeData.BranchSpread;
            Vector3 branchPosition = position + branchOffset * treeData.BranchScale;

            GameObject branch = Instantiate(branchPrefab, branchPosition, Quaternion.identity);
            branch.transform.LookAt(branchPosition + branchDirection);

            // Рекурсивно создаем следующий уровень ветвей, но с уменьшенной высотой
            GrowTree(branchPosition, branchDirection, height - 1);
        }

        // Создаем листья на верхушках ветвей
        if (height == 1)
        {
            Vector3 leafPosition = position + direction * treeData.LeafHeight;
            GameObject leaf = Instantiate(leafPrefab, leafPosition, Quaternion.identity);
        }
    }
}
