using UnityEngine;

public class HCPAxis : MonoBehaviour
{
    public GameObject axisPrefab;
    public Material matA1, matA2, matA3, matC;
    public float axisLength = 10000f; // Very long to appear infinite

    void Start()
    {
        CreateAxis(GetA1Direction(), matA1);        // a1 axis (0° from +X)
        CreateAxis(GetA2Direction(), matA2);        // a2 axis (120° from a1)
        CreateAxis(GetA3Direction(), matA3);        // a3 axis (240° from a1)
        CreateAxis(Vector3.up, matC);               // c axis (vertical, +Y)
    }

    Vector3 GetA1Direction()
    {
        // a1 axis should pass through atoms at 0° from +X axis
        // Based on HCPPlane atom positions: atoms are at angles 0°, 60°, 120°, 180°, 240°, 300°
        return new Vector3(1f, 0, 0); // 0° - passes through atom at (a, 0, 0)
    }

    Vector3 GetA2Direction()
    {
        // a2 axis should pass through atoms at 120° from a1
        float angle = 120f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)); // 120° - passes through atom
    }

    Vector3 GetA3Direction()
    {
        // a3 axis should pass through atoms at 240° from a1  
        float angle = 240f * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)); // 240° - passes through atom
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