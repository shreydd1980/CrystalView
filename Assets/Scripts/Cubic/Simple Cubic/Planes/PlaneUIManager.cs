using UnityEngine;
using TMPro;

public class PlaneUIManager : MonoBehaviour
{
    [Header("Script Reference")]
    public PlaneSC planeSC;

    [Header("Miller Index Input")]
    public TMP_InputField inputH;
    public TMP_InputField inputK;
    public TMP_InputField inputL;

    void Start()
    {
        InitializeUI();
        SetupEventListeners();
    }

    void InitializeUI()
    {
        // Check for null references
        if (planeSC == null)
        {
            Debug.LogError("PlaneSC reference is missing!");
            return;
        }

    }

    void SetupEventListeners()
    {
        // Input field listeners - automatically update plane when values change
        if (inputH != null)
            inputH.onEndEdit.AddListener(OnInputChanged);

        if (inputK != null)
            inputK.onEndEdit.AddListener(OnInputChanged);

        if (inputL != null)
            inputL.onEndEdit.AddListener(OnInputChanged);
    }

    void OnInputChanged(string input)
    {
        // Validate input and update plane automatically
        ValidateInput(input);
        UpdateMillerPlane();
    }

    void UpdateMillerPlane()
    {
        if (planeSC == null) return;

        // Get values from input fields
        int h = GetInputValue(inputH);
        int k = GetInputValue(inputK);
        int l = GetInputValue(inputL);

        // Clear existing planes and add new one
        planeSC.ClearMillerPlanes();
        
        // Only add plane if not (0,0,0)
        if (h != 0 || k != 0 || l != 0)
        {
            planeSC.AddMillerPlane(h, k, l, Color.green);
        }
    }

    int GetInputValue(TMP_InputField inputField)
    {
        if (inputField == null || string.IsNullOrEmpty(inputField.text))
            return 0;

        if (int.TryParse(inputField.text, out int value))
            return value;

        return 0;
    }

    void ValidateInput(string input)
    {
        // Ensure input is a valid integer
        TMP_InputField field = null;
        
        if (inputH != null && inputH.text == input) field = inputH;
        else if (inputK != null && inputK.text == input) field = inputK;
        else if (inputL != null && inputL.text == input) field = inputL;

        if (field != null)
        {
            if (int.TryParse(input, out int value))
            {
                // Clamp value to reasonable range
                value = Mathf.Clamp(value, -10, 10);
                field.text = value.ToString();
            }
            else
            {
                field.text = "0";
            }
        }
    }
}