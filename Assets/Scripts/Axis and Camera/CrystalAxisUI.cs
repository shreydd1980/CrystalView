using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CrystalAxisUI : MonoBehaviour
{
    public CrystalAxis crystalAxis;
    
    [Header("General Sliders")]
    public Slider sliderMode; // 0 = Normal axes, 1 = Crystallographic axes
    public Slider sliderAngle1; // Will be used for Alpha
    public Slider sliderAngle2; // Will be used for Beta
    public Slider sliderAngle3; // Will be used for Gamma
    
    void Start()
    {
        // Check for null references and warn if missing
        if (crystalAxis == null)
        {
            Debug.LogError("CrystalAxis reference is missing!");
            return;
        }
        if (sliderMode == null)
        {
            Debug.LogError("Mode slider is not assigned in the Inspector!");
            return;
        }

        // Set initial slider values from crystalAxis
        sliderMode.value = crystalAxis.mode;
        if (sliderAngle1 != null) sliderAngle1.value = crystalAxis.alpha;
        if (sliderAngle2 != null) sliderAngle2.value = crystalAxis.beta;
        if (sliderAngle3 != null) sliderAngle3.value = crystalAxis.gamma;

        // Add mode slider listener
        sliderMode.onValueChanged.AddListener(val =>
        {
            crystalAxis.SetMode(Mathf.RoundToInt(val));
        });

        // Add angle slider listeners
        if (sliderAngle1 != null)
        {
            sliderAngle1.onValueChanged.AddListener(val =>
            {
                crystalAxis.alpha = Mathf.RoundToInt(val);
                if (crystalAxis.mode == 1)
                    crystalAxis.UpdateAxes();
            });
        }
        
        if (sliderAngle2 != null)
        {
            sliderAngle2.onValueChanged.AddListener(val =>
            {
                crystalAxis.beta = Mathf.RoundToInt(val);
                if (crystalAxis.mode == 1)
                    crystalAxis.UpdateAxes();
            });
        }
        
        if (sliderAngle3 != null)
        {
            sliderAngle3.onValueChanged.AddListener(val =>
            {
                crystalAxis.gamma = Mathf.RoundToInt(val);
                if (crystalAxis.mode == 1)
                    crystalAxis.UpdateAxes();
            });
        }
    }

    // Public methods for external control
    public void UpdateUI()
    {
        if (sliderMode != null) sliderMode.value = crystalAxis.mode;
        if (sliderAngle1 != null) sliderAngle1.value = crystalAxis.alpha;
        if (sliderAngle2 != null) sliderAngle2.value = crystalAxis.beta;
        if (sliderAngle3 != null) sliderAngle3.value = crystalAxis.gamma;
    }
}