using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour {
  [Header("Настройки игры")]
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
  public GameObject sliderArea;
  public GameObject factArea;
  public TextMeshProUGUI factText;

  [Header("Счёт игроков")]
  public Transform scoresContainer;
  public GameObject playerScorePrefab;
  public Color[] playerColors;

  [Header("Завершение игры")]
  public GameObject gameOverPanel;
  public TextMeshProUGUI winnerText;
  public Button restartButton;
  public Button menuButton;

  private List<InventionData> allInventions;
  private List<InventionData> gameInventions;
  private InventionData currentInvention;
  private int currentPlayerIndex = 0;
  private int[] playerScores;
  private TextMeshProUGUI[] playerScoreTexts;
  private bool isRoundFinished = false;
  private int currentRound = 0;
  private int numberOfPlayers;
  private int totalRounds;

  void Start() {
    numberOfPlayers = GameSettings.NumberOfPlayers;
    totalRounds = GameSettings.TotalRounds;

    allInventions = new List<InventionData>(Resources.LoadAll<InventionData>("Inventions"));
    if (allInventions.Count == 0) {
      Debug.LogError("Нет изобретений в Resources/Inventions!");
      return;
    }

    PrepareGameInventions();

    playerScores = new int[numberOfPlayers];
    CreatePlayerScoreTexts();
    UpdateAllScoresUI();

    yearSlider.wholeNumbers = true;
    yearSlider.onValueChanged.AddListener(OnSliderChanged);
    mainButton.onClick.AddListener(OnMainButtonClicked);

    if (restartButton != null)
      restartButton.onClick.AddListener(RestartGame);
    if (menuButton != null)
      menuButton.onClick.AddListener(ReturnToMenu);

    gameOverPanel.SetActive(false);
    factArea.SetActive(false);
    sliderArea.SetActive(true);

    StartNewRound();
  }

  void PrepareGameInventions() {
    gameInventions = new List<InventionData>();

    while (gameInventions.Count < totalRounds) {
      foreach (var invention in allInventions) {
        gameInventions.Add(invention);
        if (gameInventions.Count >= totalRounds)
          break;
      }
    }

    ShuffleList(gameInventions);
  }

  void ShuffleList(List<InventionData> list) {
    for (int i = list.Count - 1; i > 0; i--) {
      int randomIndex = Random.Range(0, i + 1);
      var temp = list[i];
      list[i] = list[randomIndex];
      list[randomIndex] = temp;
    }
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
    if (currentRound >= totalRounds) {
      EndGame();
      return;
    }

    currentInvention = gameInventions[currentRound];

    inventionNameText.text = currentInvention.inventionName;
    inventionImage.sprite = currentInvention.inventionImage;

    yearSlider.minValue = currentInvention.minYear;
    yearSlider.maxValue = currentInvention.maxYear;
    float startValue = (currentInvention.minYear + currentInvention.maxYear) / 2f;
    yearSlider.value = startValue;
    OnSliderChanged(startValue);

    string playerColorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(currentPlayerIndex));
    statusText.text =
        $"Раунд {currentRound + 1}/{totalRounds} | Ход <color=#{playerColorHex}>Игрока {currentPlayerIndex + 1}</color>";

    sliderArea.SetActive(true);
    factArea.SetActive(false);

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
      resultMessage += $"Точно! +{pointsForExactGuess}";
    else if (scoreEarned > 0)
      resultMessage +=
          $"Близко! +{scoreEarned} (разница {Mathf.Abs(guessedYear - correctYear)} лет)";
    else
      resultMessage += "Мимо! 0 очков.";

    statusText.text = resultMessage;

    sliderArea.SetActive(false);
    factArea.SetActive(true);
    factText.text = $"<b>Интересный факт:</b>\n{currentInvention.description}";

    isRoundFinished = true;
    buttonText.text = "Продолжить";
  }

  void ContinueToNextPlayer() {
    currentPlayerIndex++;

    if (currentPlayerIndex >= numberOfPlayers) {
      currentPlayerIndex = 0;
      currentRound++;
    }

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

  public int CalculateScoreForTest(int guess, int correct) {
    return CalculateScore(guess, correct);
  }

  void EndGame() {
    gameOverPanel.SetActive(true);
    factArea.SetActive(false);
    mainButton.gameObject.SetActive(false);

    var playerResults = new(int playerIndex, int score)[numberOfPlayers];
    for (int i = 0; i < numberOfPlayers; i++) {
      playerResults[i] = (i, playerScores[i]);
    }

    System.Array.Sort(playerResults, (a, b) => b.score.CompareTo(a.score));

    string resultsText = "Результаты:\n\n";

    for (int i = 0; i < playerResults.Length; i++) {
      int playerIndex = playerResults[i].playerIndex;
      int score = playerResults[i].score;
      string colorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(playerIndex));

      string medal = "    ";
      if (i == 0)
        medal = "<color=#FFD700>1. </color>";
      else if (i == 1)
        medal = "<color=#C0C0C0>2. </color>";
      else if (i == 2)
        medal = "<color=#CD7F32>3. </color>";
      else
        medal = $"{i + 1}. ";

      resultsText += $"{medal}<color=#{colorHex}>Игрок {playerIndex + 1}</color>: {score} очков\n";
    }

    int maxScore = playerResults[0].score;
    int winnerIndex = playerResults[0].playerIndex;
    bool tie = false;

    for (int i = 1; i < playerResults.Length; i++) {
      if (playerResults[i].score == maxScore)
        tie = true;
    }

    string winnerMessage;
    if (tie)
      winnerMessage = $"Ничья! Победителей несколько с результатом {maxScore} очков.";
    else {
      string winnerColorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(winnerIndex));
      winnerMessage =
          $"Победитель: <color=#{winnerColorHex}>Игрок {winnerIndex + 1}</color> с результатом {maxScore} очков!";
    }

    winnerText.text = winnerMessage + "\n\n" + resultsText;
  }

  void RestartGame() {
    UnityEngine.SceneManagement.SceneManager.LoadScene("S_GameScene");
  }

  void ReturnToMenu() {
    UnityEngine.SceneManagement.SceneManager.LoadScene("S_MainMenu");
  }
}
