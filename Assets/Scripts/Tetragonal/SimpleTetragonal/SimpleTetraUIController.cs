using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimpleTetraUIController : MonoBehaviour
{
    public SimpleTetraGenerator generator;
    
    // Lattice parameter sliders
    public Slider sliderA, sliderC;
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
            Debug.LogError("SimpleTetraGenerator reference is missing!");
            return;
        }
        if (sliderA == null || sliderC == null)
        {
            Debug.LogError("Required UI elements are not assigned in the Inspector!");
            return;
        }

        // Set initial values from generator
        sliderA.value = generator.a;
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

        generator.GenerateTetragonal();
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
        generator.c = 1.5f;
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

    // Methods for common tetragonal crystal examples
    public void SetRutileExample()
    {
        // Rutile (TiO2) tetragonal structure
        generator.a = 1f;
        generator.c = 0.64f; // c/a ≈ 0.64 for rutile
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetCassiteriteExample()
    {
        // Cassiterite (SnO2) tetragonal structure
        generator.a = 1f;
        generator.c = 0.67f; // c/a ≈ 0.67 for cassiterite
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetZirconExample()
    {
        // Zircon (ZrSiO4) tetragonal structure
        generator.a = 1f;
        generator.c = 1.24f; // c/a ≈ 1.24 for zircon
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetAnataseExample()
    {
        // Anatase (TiO2) tetragonal structure
        generator.a = 1f;
        generator.c = 2.51f; // c/a ≈ 2.51 for anatase
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

    // Methods for c/a ratio presets
    public void SetCOverARatio(float ratio)
    {
        generator.c = generator.a * ratio;
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetCompressedTetragonal()
    {
        // c < a (compressed along c-axis)
        SetCOverARatio(0.7f);
    }

    public void SetElongatedTetragonal()
    {
        // c > a (elongated along c-axis)
        SetCOverARatio(1.5f);
    }

    public void SetNearCubic()
    {
        // c ≈ a (nearly cubic, but still tetragonal)
        SetCOverARatio(0.95f);
    }

    // Method to randomize c/a ratio for exploration
    public void RandomizeCOverARatio()
    {
        float ratio = Random.Range(0.5f, 2.5f);
        SetCOverARatio(ratio);
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
}