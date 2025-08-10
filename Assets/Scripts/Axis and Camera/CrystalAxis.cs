using UnityEngine;

public class CrystalAxis : MonoBehaviour
{
    public GameObject axisPrefab;
    public Material matX, matY, matZ;
    public float axisLength = 10000f; // Very long to appear infinite
    
    [Header("Axis Mode Control")]
    [Range(0, 1)]
    public int mode = 0; // 0 = Normal axes, 1 = Crystallographic axes
    
    [Header("Crystallographic Angles (degrees)")]
    [Range(1, 179)]
    public int alpha = 90; // Angle between b and c
    [Range(1, 179)]
    public int beta = 90;  // Angle between a and c
    [Range(1, 179)]
    public int gamma = 90; // Angle between a and b
    
    private GameObject xAxis, yAxis, zAxis;
    private TriclinicGenerator triclinicGenerator;

    void Start()
    {
        // Try to find triclinic generator to sync angles
        triclinicGenerator = FindObjectOfType<TriclinicGenerator>();
        if (triclinicGenerator != null)
        {
            alpha = triclinicGenerator.alpha;
            beta = triclinicGenerator.beta;
            gamma = triclinicGenerator.gamma;
        }
        
        CreateAxes();
    }

    void CreateAxes()
    {
        // Destroy existing axes if they exist
        if (xAxis != null) DestroyImmediate(xAxis);
        if (yAxis != null) DestroyImmediate(yAxis);
        if (zAxis != null) DestroyImmediate(zAxis);

        if (mode == 0)
        {
            // Normal Cartesian axes (X, Y, Z at 90° angles)
            CreateNormalAxes();
        }
        else
        {
            // Crystallographic axes (a, b, c with custom angles)
            CreateCrystallographicAxes();
        }
    }

    void CreateNormalAxes()
    {
        xAxis = CreateAxis(Vector3.right, matX, "X-Axis");      // X (red)
        yAxis = CreateAxis(Vector3.up, matY, "Y-Axis");         // Y (green)
        zAxis = CreateAxis(-Vector3.forward, matZ, "Z-Axis");   // Z (blue, flipped)
    }

    void CreateCrystallographicAxes()
    {
        // Convert angles to radians
        float alphaRad = Mathf.Deg2Rad * alpha;
        float betaRad = Mathf.Deg2Rad * beta;
        float gammaRad = Mathf.Deg2Rad * gamma;

        // Calculate crystallographic lattice vectors (same as TriclinicGenerator)
        Vector3 a1 = Vector3.right; // a-axis along X
        
        Vector3 a2 = new Vector3(
            Mathf.Cos(gammaRad),
            Mathf.Sin(gammaRad),
            0
        ); // b-axis in XY plane
        
        float cx = Mathf.Cos(betaRad);
        float cy = (Mathf.Cos(alphaRad) - Mathf.Cos(betaRad) * Mathf.Cos(gammaRad)) / Mathf.Sin(gammaRad);
        float cz = Mathf.Sqrt(Mathf.Max(0f, 1 - cx * cx - cy * cy));
        Vector3 a3 = new Vector3(cx, cy, -cz); // c-axis with inverted Z component

        // Create axes with crystallographic directions
        xAxis = CreateAxis(a1, matX, "a-Axis");
        yAxis = CreateAxis(a2, matY, "b-Axis");
        zAxis = CreateAxis(a3, matZ, "c-Axis"); // c-Axis with inverted Z movement
    }

    GameObject CreateAxis(Vector3 dir, Material mat, string name)
    {
        GameObject axis = Instantiate(axisPrefab, transform);
        axis.name = name;
        
        // The cylinder's local Y axis is its length, so scale Y to axisLength
        axis.transform.localScale = new Vector3(0.05f, axisLength / 2, 0.05f);
        
        // Center the axis at the origin so it extends equally in both directions
        axis.transform.position = Vector3.zero;
        axis.transform.up = dir.normalized;
        
        if (axis.GetComponent<Renderer>() != null)
            axis.GetComponent<Renderer>().material = mat;
            
        return axis;
    }

    // Public method to update axes (called from UI)
    public void UpdateAxes()
    {
        CreateAxes();
    }

    // Method to sync with triclinic generator
    public void SyncWithTriclinicGenerator()
    {
        if (triclinicGenerator != null)
        {
            alpha = triclinicGenerator.alpha;
            beta = triclinicGenerator.beta;
            gamma = triclinicGenerator.gamma;
            
            if (mode == 1) // Only update if in crystallographic mode
                UpdateAxes();
        }
    }

    // Method to set mode from external scripts (like UI)
    public void SetMode(int newMode)
    {
        mode = Mathf.Clamp(newMode, 0, 1);
        UpdateAxes();
    }

    // Method to set angles from external scripts
    public void SetAngles(int newAlpha, int newBeta, int newGamma)
    {
        alpha = Mathf.Clamp(newAlpha, 1, 179);
        beta = Mathf.Clamp(newBeta, 1, 179);
        gamma = Mathf.Clamp(newGamma, 1, 179);
        
        if (mode == 1) // Only update if in crystallographic mode
            UpdateAxes();
    }

    // Preset methods for common crystal systems
    public void SetToCubic()
    {
        SetAngles(90, 90, 90);
    }

    public void SetToTriclinic()
    {
        SetAngles(85, 95, 75); // Example triclinic angles
    }

    public void SetToHexagonal()
    {
        SetAngles(90, 90, 120);
    }

    public void SetToMonoclinic()
    {
        SetAngles(90, 100, 90); // Example with β ≠ 90°
    }

    // Update method for real-time changes in editor
    void OnValidate()
    {
        if (Application.isPlaying)
        {
            UpdateAxes();
        }
    }
}