using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour {
  [Header("Настройки игры")]
  public int numberOfPlayers = 2;
  public int pointsForExactGuess = 100;
  public int maxPointsForClose = 50;
  public int yearRange = 50;

  [Header("UI элементы")]
  public TextMeshProUGUI inventionNameText;
  public Image inventionImage;
  public Slider yearSlider;
  public TextMeshProUGUI selectedYearText;
  public Button mainButton;
  public TextMeshProUGUI buttonText;
  public TextMeshProUGUI statusText;

  [Header("Счёт игроков")]
  public Transform scoresContainer;
  public GameObject playerScorePrefab;
  public Color[] playerColors;

  private List<InventionData> inventions;
  private InventionData currentInvention;
  private int currentPlayerIndex = 0;
  private int[] playerScores;
  private TextMeshProUGUI[] playerScoreTexts;
  private bool isRoundFinished = false;

  void Start() {
    inventions = new List<InventionData>(Resources.LoadAll<InventionData>("Inventions"));
    if (inventions.Count == 0) {
      Debug.LogError("Нет изобретений в Resources/Inventions!");
      return;
    }

    playerScores = new int[numberOfPlayers];
    CreatePlayerScoreTexts();
    UpdateAllScoresUI();

    yearSlider.wholeNumbers = true;
    yearSlider.onValueChanged.AddListener(OnSliderChanged);
    mainButton.onClick.AddListener(OnMainButtonClicked);

    StartNewRound();
  }

  void CreatePlayerScoreTexts() {
    playerScoreTexts = new TextMeshProUGUI[numberOfPlayers];

    for (int i = 0; i < numberOfPlayers; i++) {
      GameObject textObj;
      if (playerScorePrefab != null)
        textObj = Instantiate(playerScorePrefab, scoresContainer);
      else {
        textObj = new GameObject($"Player{i + 1}Score", typeof(TextMeshProUGUI));
        textObj.transform.SetParent(scoresContainer, false);
      }

      var tmpText = textObj.GetComponent<TextMeshProUGUI>();
      if (tmpText == null)
        tmpText = textObj.AddComponent<TextMeshProUGUI>();

      if (playerColors != null && i < playerColors.Length)
        tmpText.color = playerColors[i];
      else
        tmpText.color = Color.white;
      playerScoreTexts[i] = tmpText;
    }
  }

  void UpdateAllScoresUI() {
    for (int i = 0; i < playerScores.Length; i++) {
      playerScoreTexts[i].text = $"Игрок {i + 1}: {playerScores[i]}";
    }
  }

  void UpdateSingleScoreUI(int playerIndex) {
    playerScoreTexts[playerIndex].text = $"Игрок {playerIndex + 1}: {playerScores[playerIndex]}";
  }

  void StartNewRound() {
    currentInvention = inventions[Random.Range(0, inventions.Count)];
    inventionNameText.text = currentInvention.inventionName;
    inventionImage.sprite = currentInvention.inventionImage;

    yearSlider.minValue = currentInvention.minYear;
    yearSlider.maxValue = currentInvention.maxYear;
    float startValue = (currentInvention.minYear + currentInvention.maxYear) / 2f;
    yearSlider.value = startValue;
    OnSliderChanged(startValue);

    string playerColorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(currentPlayerIndex));
    statusText.text = $"Ход <color=#{playerColorHex}>Игрока {currentPlayerIndex + 1}</color>";

    isRoundFinished = false;
    mainButton.interactable = true;
    buttonText.text = "Угадать";
  }

  Color GetPlayerColor(int index) {
    if (playerColors != null && index < playerColors.Length)
      return playerColors[index];
    return Color.white;
  }

  void OnSliderChanged(float value) {
    selectedYearText.text = value.ToString("0");
  }

  void OnMainButtonClicked() {
    if (!isRoundFinished)
      ProcessGuess();
    else
      ContinueToNextPlayer();
  }

  void ProcessGuess() {
    int guessedYear = Mathf.RoundToInt(yearSlider.value);
    int correctYear = currentInvention.correctYear;

    int scoreEarned = CalculateScore(guessedYear, correctYear);
    playerScores[currentPlayerIndex] += scoreEarned;
    UpdateSingleScoreUI(currentPlayerIndex);

    string resultMessage =
        $"Игрок {currentPlayerIndex + 1} выбрал {guessedYear}. Правильный год: {correctYear}.\n";
    if (guessedYear == correctYear)
      resultMessage += "Точно! +" + pointsForExactGuess;
    else if (scoreEarned > 0)
      resultMessage +=
          $"Близко! +{scoreEarned} (разница {Mathf.Abs(guessedYear - correctYear)} лет)";
    else
      resultMessage += "Мимо! 0 очков.";

    statusText.text = resultMessage;

    isRoundFinished = true;
    buttonText.text = "Продолжить";
  }

  void ContinueToNextPlayer() {
    currentPlayerIndex = (currentPlayerIndex + 1) % numberOfPlayers;
    StartNewRound();
  }

  int CalculateScore(int guess, int correct) {
    int diff = Mathf.Abs(guess - correct);
    if (diff == 0)
      return pointsForExactGuess;
    else if (diff <= yearRange)
      return Mathf.RoundToInt(maxPointsForClose * (1 - (float)diff / yearRange));
    else
      return 0;
  }
}
