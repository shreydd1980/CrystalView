using UnityEngine;

public class AxisGenerator : MonoBehaviour
{
    public GameObject axisPrefab;
    public Material matX, matY, matZ;
    public float axisLength = 10000f; // Very long to appear infinite

    void Start()
    {
        CreateAxis(Vector3.right, matX);      // X (horizontal)
        CreateAxis(Vector3.forward, matZ);    // Z (vertical)
        CreateAxis(Vector3.up, matY);         // Y (horizontal)
    }

    void CreateAxis(Vector3 dir, Material mat)
    {
        GameObject axis = Instantiate(axisPrefab, transform);
        // The cylinder's local Y axis is its length, so scale Y to axisLength
        axis.transform.localScale = new Vector3(0.05f, axisLength / 2, 0.05f);
        // Center the axis at the origin so it extends equally in both directions
        axis.transform.position = Vector3.zero;
        axis.transform.up = dir;
        axis.GetComponent<Renderer>().material = mat;
    }
}