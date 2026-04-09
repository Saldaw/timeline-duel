using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour {
  [Header("UI элементы меню")]
  public Slider playersSlider;
  public TextMeshProUGUI playersCountText;
  public Slider roundsSlider;
  public TextMeshProUGUI roundsCountText;
  public Button startButton;
  public GameObject mainMenuPanel;

  [Header("Настройки")]
  public int minPlayers = 1;
  public int maxPlayers = 4;
  public int minRounds = 5;
  public int maxRounds = 50;

  private void Start() {
    playersSlider.minValue = minPlayers;
    playersSlider.maxValue = maxPlayers;
    playersSlider.wholeNumbers = true;
    playersSlider.onValueChanged.AddListener(OnPlayersChanged);

    roundsSlider.minValue = minRounds;
    roundsSlider.maxValue = maxRounds;
    roundsSlider.wholeNumbers = true;
    roundsSlider.onValueChanged.AddListener(OnRoundsChanged);

    startButton.onClick.AddListener(StartGame);

    playersSlider.value = 2;
    roundsSlider.value = 10;
  }

  private void OnPlayersChanged(float value) {
    int count = Mathf.RoundToInt(value);
    playersCountText.text = $"Игроков: {count}";
  }

  private void OnRoundsChanged(float value) {
    int count = Mathf.RoundToInt(value);
    roundsCountText.text = $"Изображений: {count}";
  }

  private void StartGame() {
    GameSettings.NumberOfPlayers = Mathf.RoundToInt(playersSlider.value);
    GameSettings.TotalRounds = Mathf.RoundToInt(roundsSlider.value);

    UnityEngine.SceneManagement.SceneManager.LoadScene("S_GameScene");
  }
}
