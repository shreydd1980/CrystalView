using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaCOrthoUIController : MonoBehaviour
{
    public BaCOrthoGenerator generator;
    
    // Lattice parameter sliders
    public Slider sliderA, sliderB, sliderC;
    public Slider sliderAtomScale, sliderBondRadius;
    public Image atomColorImage, bondColorImage;
    public Toggle toggleShowPlanes;
    
    // NX controls
    public Button buttonNXUp, buttonNXDown;
    public TMP_InputField inputNX;
    
    // NY controls
    public Button buttonNYUp, buttonNYDown;
    public TMP_InputField inputNY;
    
    // NZ controls
    public Button buttonNZUp, buttonNZDown;
    public TMP_InputField inputNZ;

    void Start()
    {
        // Check for null references and warn if missing
        if (generator == null)
        {
            Debug.LogError("BaCOrthoGenerator reference is missing!");
            return;
        }
        if (sliderA == null || sliderB == null || sliderC == null)
        {
            Debug.LogError("Required UI elements are not assigned in the Inspector!");
            return;
        }

        // Set initial values from generator
        sliderA.value = generator.a;
        sliderB.value = generator.b;
        sliderC.value = generator.c;
        if (sliderAtomScale != null) sliderAtomScale.value = generator.atomScale;
        if (sliderBondRadius != null) sliderBondRadius.value = generator.bondRadius;
        if (atomColorImage != null) atomColorImage.color = generator.atomColor;
        if (bondColorImage != null) bondColorImage.color = generator.bondColor;
        if (toggleShowPlanes != null) toggleShowPlanes.isOn = generator.showPlanes;

        // Set initial input field values
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();

        // Add slider listeners for lattice parameters
        sliderA.onValueChanged.AddListener(val => { generator.a = val; Regenerate(); });
        sliderB.onValueChanged.AddListener(val => { generator.b = val; Regenerate(); });
        sliderC.onValueChanged.AddListener(val => { generator.c = val; Regenerate(); });
        if (sliderAtomScale != null)
            sliderAtomScale.onValueChanged.AddListener(val => { generator.atomScale = val; Regenerate(); });
        if (sliderBondRadius != null)
            sliderBondRadius.onValueChanged.AddListener(val => { generator.bondRadius = val; Regenerate(); });
        if (toggleShowPlanes != null)
            toggleShowPlanes.onValueChanged.AddListener(val => { generator.showPlanes = val; generator.TogglePlanes(); });

        // Add button listeners for NX
        if (buttonNXUp != null)
            buttonNXUp.onClick.AddListener(() => ChangeNX(1));
        if (buttonNXDown != null)
            buttonNXDown.onClick.AddListener(() => ChangeNX(-1));
        if (inputNX != null)
            inputNX.onEndEdit.AddListener(val => UpdateNXFromInput(val));

        // Add button listeners for NY
        if (buttonNYUp != null)
            buttonNYUp.onClick.AddListener(() => ChangeNY(1));
        if (buttonNYDown != null)
            buttonNYDown.onClick.AddListener(() => ChangeNY(-1));
        if (inputNY != null)
            inputNY.onEndEdit.AddListener(val => UpdateNYFromInput(val));

        // Add button listeners for NZ
        if (buttonNZUp != null)
            buttonNZUp.onClick.AddListener(() => ChangeNZ(1));
        if (buttonNZDown != null)
            buttonNZDown.onClick.AddListener(() => ChangeNZ(-1));
        if (inputNZ != null)
            inputNZ.onEndEdit.AddListener(val => UpdateNZFromInput(val));
    }

    void ChangeNX(int delta)
    {
        generator.nx = Mathf.Max(1, generator.nx + delta); // Prevent going below 1
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        Regenerate();
    }

    void ChangeNY(int delta)
    {
        generator.ny = Mathf.Max(1, generator.ny + delta); // Prevent going below 1
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        Regenerate();
    }

    void ChangeNZ(int delta)
    {
        generator.nz = Mathf.Max(1, generator.nz + delta); // Prevent going below 1
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();
        Regenerate();
    }

    void UpdateNXFromInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            generator.nx = Mathf.Max(1, result); // Prevent going below 1
            inputNX.text = generator.nx.ToString(); // Update field to show clamped value
            Regenerate();
        }
        else
        {
            inputNX.text = generator.nx.ToString(); // Reset to current value if invalid
        }
    }

    void UpdateNYFromInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            generator.ny = Mathf.Max(1, result); // Prevent going below 1
            inputNY.text = generator.ny.ToString(); // Update field to show clamped value
            Regenerate();
        }
        else
        {
            inputNY.text = generator.ny.ToString(); // Reset to current value if invalid
        }
    }

    void UpdateNZFromInput(string value)
    {
        if (int.TryParse(value, out int result))
        {
            generator.nz = Mathf.Max(1, result); // Prevent going below 1
            inputNZ.text = generator.nz.ToString(); // Update field to show clamped value
            Regenerate();
        }
        else
        {
            inputNZ.text = generator.nz.ToString(); // Reset to current value if invalid
        }
    }

    void Regenerate()
    {
        // Optionally update color from UI images if you have color pickers
        if (atomColorImage != null) generator.atomColor = atomColorImage.color;
        if (bondColorImage != null) generator.bondColor = bondColorImage.color;

        generator.GenerateBaseCenteredOrthorhombic();
    }

    // Public methods for UI buttons (can be called from button OnClick events)
    public void RegenerateStructure()
    {
        Regenerate();
    }

    public void ResetToDefaults()
    {
        // Reset to default values
        generator.a = 1f;
        generator.b = 1.5f;
        generator.c = 2f;
        generator.nx = 1;
        generator.ny = 1;
        generator.nz = 1;
        generator.atomScale = 0.2f;
        generator.bondRadius = 0.02f;
        generator.atomColor = Color.white;
        generator.bondColor = Color.yellow;
        generator.showPlanes = true;

        // Update UI elements
        UpdateUIFromGenerator();
        Regenerate();
    }

    void UpdateUIFromGenerator()
    {
        if (sliderA != null) sliderA.value = generator.a;
        if (sliderB != null) sliderB.value = generator.b;
        if (sliderC != null) sliderC.value = generator.c;
        if (sliderAtomScale != null) sliderAtomScale.value = generator.atomScale;
        if (sliderBondRadius != null) sliderBondRadius.value = generator.bondRadius;
        if (atomColorImage != null) atomColorImage.color = generator.atomColor;
        if (bondColorImage != null) bondColorImage.color = generator.bondColor;
        if (toggleShowPlanes != null) toggleShowPlanes.isOn = generator.showPlanes;
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();
    }

    // Methods for common base-centered orthorhombic crystal examples
    public void SetStibniteExample()
    {
        // Stibnite (Sb2S3) - base-centered orthorhombic
        generator.a = 1f;
        generator.b = 1.31f; // b/a ≈ 1.31
        generator.c = 0.99f; // c/a ≈ 0.99
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetBismuthiniteExample()
    {
        // Bismuthinite (Bi2S3) - base-centered orthorhombic
        generator.a = 1f;
        generator.b = 1.28f; // b/a ≈ 1.28
        generator.c = 0.95f; // c/a ≈ 0.95
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetOrpimentExample()
    {
        // Orpiment (As2S3) - base-centered orthorhombic
        generator.a = 1f;
        generator.b = 0.82f; // b/a ≈ 0.82
        generator.c = 0.71f; // c/a ≈ 0.71
        UpdateUIFromGenerator();
        Regenerate();
    }

    // Toggle planes visibility
    public void TogglePlanesVisibility()
    {
        generator.showPlanes = !generator.showPlanes;
        if (toggleShowPlanes != null) toggleShowPlanes.isOn = generator.showPlanes;
        generator.TogglePlanes();
    }

    // Methods for axis ratio presets
    public void SetAxisRatios(float bOverA, float cOverA)
    {
        generator.b = generator.a * bOverA;
        generator.c = generator.a * cOverA;
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetElongatedBaCOrtho()
    {
        // c > b > a (elongated along c)
        SetAxisRatios(1.4f, 1.9f);
    }

    public void SetFlattenedBaCOrtho()
    {
        // a > b > c (flattened along c)
        SetAxisRatios(0.7f, 0.5f);
    }

    public void SetNearCubicBaCOrtho()
    {
        // a ≈ b ≈ c (nearly cubic, but still orthorhombic)
        SetAxisRatios(0.94f, 0.91f);
    }

    // Method to randomize axis ratios for exploration
    public void RandomizeAxisRatios()
    {
        float bOverA = Random.Range(0.6f, 1.8f);
        float cOverA = Random.Range(0.5f, 1.6f);
        SetAxisRatios(bOverA, cOverA);
    }

    // Methods to control specific Miller plane visibility
    public void ShowOnly100Planes()
    {
        generator.ShowMillerPlanes(true, false, false);
    }

    public void ShowOnly010Planes()
    {
        generator.ShowMillerPlanes(false, true, false);
    }

    public void ShowOnly001Planes()
    {
        generator.ShowMillerPlanes(false, false, true);
    }

    public void ShowAllPlanes()
    {
        generator.ShowMillerPlanes(true, true, true);
    }

    // Method to increase/decrease lattice parameters with buttons
    public void IncreaseA()
    {
        generator.a = Mathf.Min(5f, generator.a + 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void DecreaseA()
    {
        generator.a = Mathf.Max(0.1f, generator.a - 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void IncreaseB()
    {
        generator.b = Mathf.Min(5f, generator.b + 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void DecreaseB()
    {
        generator.b = Mathf.Max(0.1f, generator.b - 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void IncreaseC()
    {
        generator.c = Mathf.Min(5f, generator.c + 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void DecreaseC()
    {
        generator.c = Mathf.Max(0.1f, generator.c - 0.1f);
        UpdateUIFromGenerator();
        Regenerate();
    }

    // Method to swap axes for exploration
    public void SwapAB()
    {
        float temp = generator.a;
        generator.a = generator.b;
        generator.b = temp;
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SwapAC()
    {
        float temp = generator.a;
        generator.a = generator.c;
        generator.c = temp;
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SwapBC()
    {
        float temp = generator.b;
        generator.b = generator.c;
        generator.c = temp;
        UpdateUIFromGenerator();
        Regenerate();
    }

    // Additional BaCO-specific controls
    public void ShowBaseCenterAtoms()
    {
        // Method to highlight or change color of base-center atoms
        // This would require modification to the generator to track which atoms are base-centered
        Debug.Log("Base-center atoms visualization - requires generator modification");
    }

    public void ShowOnlyCornerAtoms()
    {
        // Method to show only corner atoms
        Debug.Log("Corner atoms only - requires generator modification");
    }

    public void ShowBothAtomTypes()
    {
        // Method to show both corner and base-center atoms with different colors
        Debug.Log("Both atom types - requires generator modification");
    }

    // Method to demonstrate the difference between simple and base-centered orthorhombic
    public void ShowSimpleOrthoComparison()
    {
        Debug.Log("To compare with Simple Orthorhombic, use SimpleOrthoGenerator");
    }
}