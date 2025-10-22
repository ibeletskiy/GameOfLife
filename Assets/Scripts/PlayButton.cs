using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class PlayButton : MonoBehaviour
{
    [SerializeField] private GameField field;
    [SerializeField] private GameObject playImage;
    [SerializeField] private GameObject pauseImage;

    void Start()
    {
        if (field == null) field = FindObjectOfType<GameField>(true);
        GetComponent<Button>().onClick.AddListener(OnClick);
        Sync();
    }

    void OnClick()
    {
        if (field == null) return;
        field.ToggleRunning();
        Sync();
    }

    void Sync()
    {
        bool running = field != null && field.IsRunning;
        if (playImage)  playImage.SetActive(!running);
        if (pauseImage) pauseImage.SetActive(running);
    }
}