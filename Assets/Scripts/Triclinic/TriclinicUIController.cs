using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TriclinicUIController : MonoBehaviour
{
    public TriclinicGenerator generator;
    
    // Lattice parameter sliders
    public Slider sliderA, sliderB, sliderC;
    public Slider sliderAlpha, sliderBeta, sliderGamma;
    public Slider sliderAtomScale, sliderBondRadius;
    public Image atomColorImage, bondColorImage;
    
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
            Debug.LogError("TriclinicGenerator reference is missing!");
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
        
        if (sliderAlpha != null) sliderAlpha.value = generator.alpha;
        if (sliderBeta != null) sliderBeta.value = generator.beta;
        if (sliderGamma != null) sliderGamma.value = generator.gamma;
        
        if (sliderAtomScale != null) sliderAtomScale.value = generator.atomScale;
        if (sliderBondRadius != null) sliderBondRadius.value = generator.bondRadius;
        if (atomColorImage != null) atomColorImage.color = generator.atomColor;
        if (bondColorImage != null) bondColorImage.color = generator.bondColor;

        // Set initial input field values
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();

        // Add slider listeners for lattice parameters
        sliderA.onValueChanged.AddListener(val => { generator.a = val; Regenerate(); });
        sliderB.onValueChanged.AddListener(val => { generator.b = val; Regenerate(); });
        sliderC.onValueChanged.AddListener(val => { generator.c = val; Regenerate(); });
        
        if (sliderAlpha != null)
            sliderAlpha.onValueChanged.AddListener(val => { generator.alpha = Mathf.RoundToInt(val); Regenerate(); });
        if (sliderBeta != null)
            sliderBeta.onValueChanged.AddListener(val => { generator.beta = Mathf.RoundToInt(val); Regenerate(); });
        if (sliderGamma != null)
            sliderGamma.onValueChanged.AddListener(val => { generator.gamma = Mathf.RoundToInt(val); Regenerate(); });
        
        if (sliderAtomScale != null)
            sliderAtomScale.onValueChanged.AddListener(val => { generator.atomScale = val; Regenerate(); });
        if (sliderBondRadius != null)
            sliderBondRadius.onValueChanged.AddListener(val => { generator.bondRadius = val; Regenerate(); });

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

        generator.GenerateTriclinic();
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
        generator.alpha = 90;
        generator.beta = 90;
        generator.gamma = 90;
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
        if (sliderAlpha != null) sliderAlpha.value = generator.alpha;
        if (sliderBeta != null) sliderBeta.value = generator.beta;
        if (sliderGamma != null) sliderGamma.value = generator.gamma;
        if (sliderAtomScale != null) sliderAtomScale.value = generator.atomScale;
        if (sliderBondRadius != null) sliderBondRadius.value = generator.bondRadius;
        if (atomColorImage != null) atomColorImage.color = generator.atomColor;
        if (bondColorImage != null) bondColorImage.color = generator.bondColor;
        if (inputNX != null) inputNX.text = generator.nx.ToString();
        if (inputNY != null) inputNY.text = generator.ny.ToString();
        if (inputNZ != null) inputNZ.text = generator.nz.ToString();
    }

    // Method to randomize angles for interesting triclinic shapes
    public void RandomizeAngles()
    {
        generator.alpha = Random.Range(60, 89);
        generator.beta = Random.Range(60, 89);
        generator.gamma = Random.Range(60, 89);
        
        // Ensure angles are different for true triclinic symmetry
        while (Mathf.Abs(generator.alpha - generator.beta) < 5)
            generator.beta = Random.Range(60, 89);
        while (Mathf.Abs(generator.alpha - generator.gamma) < 5 || Mathf.Abs(generator.beta - generator.gamma) < 5)
            generator.gamma = Random.Range(60, 89);
            
        UpdateUIFromGenerator();
        Regenerate();
    }

    // Method to set common triclinic examples
    public void SetMicroclineExample()
    {
        // Microcline (K-feldspar) approximate values
        generator.a = 1f;
        generator.b = 1.2f;
        generator.c = 0.9f;
        generator.alpha = 87;
        generator.beta = 84;
        generator.gamma = 69;
        UpdateUIFromGenerator();
        Regenerate();
    }

    public void SetAxiniteExample()
    {
        // Axinite approximate values
        generator.a = 1f;
        generator.b = 1.5f;
        generator.c = 0.8f;
        generator.alpha = 82;
        generator.beta = 81;
        generator.gamma = 77;
        UpdateUIFromGenerator();
        Regenerate();
    }
}