using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaCMonoUIController : MonoBehaviour
{
    public BaCMonoGenerator generator;
    
    // Lattice parameter sliders
    public Slider sliderA, sliderB, sliderC;
    public Slider sliderBeta; // Angle slider for monoclinic
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
            Debug.LogError("BaCMonoGenerator reference is missing!");
            return;
        }
        if (sliderA == null || sliderB == null || sliderC == null || sliderBeta == null)
        {
            Debug.LogError("Required UI elements are not assigned in the Inspector!");
            return;
        }

        // Set initial values from generator
        sliderA.value = generator.a;
        sliderB.value = generator.b;
        sliderC.value = generator.c;
        sliderBeta.value = generator.gamma;
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
        sliderBeta.onValueChanged.AddListener(val => { generator.gamma = Mathf.RoundToInt(val); UpdateBetaUI(); Regenerate(); });
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

    void UpdateBetaUI()
    {
        if (sliderBeta != null) sliderBeta.value = generator.gamma;
    }

    void Regenerate()
    {
        // Optionally update color from UI images if you have color pickers
        if (atomColorImage != null) generator.atomColor = atomColorImage.color;
        if (bondColorImage != null) generator.bondColor = bondColorImage.color;

        generator.GenerateBaseCenteredMonoclinic();
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
        generator.gamma = 75;
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
        if (sliderBeta != null) sliderBeta.value = generator.gamma;
        if (sliderAtomScale != null) sliderAtomScale.value = generator.atomScale;
        if (sliderBondRadius != null) sliderBondRadius.value = generator.bondRadius;
        if (atomColorImage != null) atomColorImage.color = generator.atomColor;
        if (bondColorImage != null) bondColorImage.color = generator.bondColor;
        if (toggleShowPlanes != null) toggleShowPlanes.isOn = generator.showPlanes;
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();
    }

    // Methods for base-centered monoclinic crystal examples
    public void SetBaseCenteredGypsumExample()
    {
        // Base-centered gypsum variant - monoclinic
        generator.a = 1f;
        generator.b = 1.47f; // b/a ≈ 1.47
        generator.c = 1.13f; // c/a ≈ 1.13
        generator.gamma = 81; // γ ≈ 81°
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetBaseCenteredOrthoclaseExample()
    {
        // Base-centered orthoclase variant - monoclinic
        generator.a = 1f;
        generator.b = 1.28f; // b/a ≈ 1.28
        generator.c = 0.84f; // c/a ≈ 0.84
        generator.gamma = 64; // γ ≈ 64°
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetBaseCenteredClinopyroxeneExample()
    {
        // Base-centered clinopyroxene - monoclinic
        generator.a = 1f;
        generator.b = 0.90f; // b/a ≈ 0.90
        generator.c = 0.57f; // c/a ≈ 0.57
        generator.gamma = 72; // γ ≈ 72°
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetBaseCenteredBiotiteExample()
    {
        // Base-centered biotite - monoclinic
        generator.a = 1f;
        generator.b = 0.97f; // b/a ≈ 0.97
        generator.c = 1.02f; // c/a ≈ 1.02
        generator.gamma = 85; // γ ≈ 85°
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
    public void SetAxisRatios(float bOverA, float cOverA, int betaAngle)
    {
        generator.b = generator.a * bOverA;
        generator.c = generator.a * cOverA;
        generator.gamma = Mathf.Clamp(betaAngle, 1, 89);
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetElongatedBaCMono()
    {
        // c > b > a with moderate angle (emphasizes base-centering)
        SetAxisRatios(1.3f, 1.8f, 70);
    }

    public void SetFlattenedBaCMono()
    {
        // a > b > c with small angle (flattened base planes)
        SetAxisRatios(0.7f, 0.5f, 85);
    }

    public void SetHighAngleBaCMono()
    {
        // Nearly orthogonal (close to base-centered orthorhombic)
        SetAxisRatios(1.2f, 0.9f, 88);
    }

    public void SetLowAngleBaCMono()
    {
        // Very oblique angle (highly distorted base planes)
        SetAxisRatios(1.1f, 1.4f, 45);
    }

    // Method to randomize parameters for exploration
    public void RandomizeParameters()
    {
        float bOverA = Random.Range(0.6f, 1.8f);
        float cOverA = Random.Range(0.5f, 1.7f);
        int betaAngle = Random.Range(30, 89);
        SetAxisRatios(bOverA, cOverA, betaAngle);
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

    // Beta angle controls
    public void IncreaseBeta()
    {
        generator.gamma = Mathf.Min(89, generator.gamma + 1);
        UpdateBetaUI();
        Regenerate();
    }

    public void DecreaseBeta()
    {
        generator.gamma = Mathf.Max(1, generator.gamma - 1);
        UpdateBetaUI();
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

    // Educational methods to demonstrate base-centered monoclinic properties
    public void ShowAngleEffect()
    {
        Debug.Log($"Current gamma angle: {generator.gamma}°. Base-centered monoclinic systems have γ ≠ 90° with additional atoms at base centers");
    }

    public void ShowBaCMonoclinicVsSimpleMonoclinic()
    {
        Debug.Log("Base-centered monoclinic: Corner atoms + atoms at base face centers. Simple monoclinic: Only corner atoms");
    }

    public void ShowBaseCenteringEffect()
    {
        Debug.Log("Base-centering adds atoms at the centers of the (001) faces, creating a denser structure");
    }

    // BaC-specific controls
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

    // Method to demonstrate the difference between simple and base-centered monoclinic
    public void ShowSimpleMonoclinicComparison()
    {
        Debug.Log("To compare with Simple Monoclinic, use SimpleMonoGenerator");
    }

    // Special presets for base-centered structures
    public void SetNearCubicBaC()
    {
        // Nearly cubic but still monoclinic and base-centered
        SetAxisRatios(0.96f, 0.94f, 87);
    }

    public void SetHighlyDistortedBaC()
    {
        // Highly distorted base-centered monoclinic
        SetAxisRatios(1.8f, 0.6f, 35);
    }

    public void SetTypicalBaC()
    {
        // Typical base-centered monoclinic ratios
        SetAxisRatios(1.2f, 0.9f, 72);
    }
}