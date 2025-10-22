using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] private GameField field;
    [SerializeField] private Button startPauseButton;
    [SerializeField] private Button randomFillButton;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private PlayButton playIcon;
    [SerializeField] private Button clearButton;

    void Start()
    {
        if (randomFillButton) randomFillButton.onClick.AddListener(OnRandomFill);
        if (clearButton) clearButton.onClick.AddListener(OnClear);
        if (speedSlider)
        {
            speedSlider.minValue = 0.02f;
            speedSlider.maxValue = 1.5f;
            if (field) speedSlider.value = field.UpdateInterval;
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }
    }

    void OnRandomFill()
    {
        if (!field) return;
        field.RandomFillVisible();
    }

    void OnSpeedChanged(float v)
    {
        if (!field) return;
        field.UpdateInterval = v;
    }

    void OnClear()
    {
        if (!field) return;
        field.Clear();
    }
}