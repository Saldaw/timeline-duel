using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
  [Header("Настройки игры")]
  public int pointsForExactGuess = 100;
  public int maxPointsForClose = 50;
  public int yearRange = 50;

  [Header("UI элементы")]
  public TextMeshProUGUI inventionNameText;
  public Image inventionImage;
  public Image sliderFillImage;
  public Slider yearSlider;
  public TextMeshProUGUI selectedYearText;
  public Button mainButton;
  public TextMeshProUGUI buttonText;
  public TextMeshProUGUI statusText;
  public GameObject sliderArea;
  public GameObject factArea;
  public TextMeshProUGUI factText;

  [Header("Кнопки точной настройки")]
  public Button plusButton;
  public Button minusButton;
  public int yearStep = 1;

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

  [Header("Анимация карточки")]
  public RectTransform inventionCard;
  public CanvasGroup cardCanvasGroup;
  public float flyOutDuration = 0.4f;
  public float flyInDuration = 0.5f;
  public AnimationCurve flyOutMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve flyOutRotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve flyOutAlphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve flyInMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve flyInRotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public AnimationCurve flyInAlphaCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public float flyOutRotationAmount = 20f;
  public float flyInRotationAmount = -20f;

  private Vector2 cardStartPosition;
  private Vector2 offScreenLeft;
  private Vector2 offScreenRight;
  private bool isAnimating = false;
  private List<InventionData> availableInventions;
  private InventionData nextInvention;

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
    if (plusButton != null)
      plusButton.onClick.AddListener(OnPlusButtonClicked);
    if (minusButton != null)
      minusButton.onClick.AddListener(OnMinusButtonClicked);

    gameOverPanel.SetActive(false);
    factArea.SetActive(false);
    sliderArea.SetActive(true);

    ResetAvailableInventions();

    cardStartPosition = inventionCard.anchoredPosition;
    float screenWidth = GetComponentInParent<Canvas>().GetComponent<RectTransform>().rect.width;
    offScreenLeft = new Vector2(-screenWidth * 1.5f, cardStartPosition.y);
    offScreenRight = new Vector2(screenWidth * 1.5f, cardStartPosition.y);
    UpdateSliderColorForPlayer(currentPlayerIndex);

    StartNewRound();
  }

  void OnPlusButtonClicked() {
    if (isRoundFinished)
      return;

    float newValue = yearSlider.value + yearStep;
    newValue = Mathf.Clamp(newValue, yearSlider.minValue, yearSlider.maxValue);
    yearSlider.value = newValue;
  }

  void OnMinusButtonClicked() {
    if (isRoundFinished)
      return;

    float newValue = yearSlider.value - yearStep;
    newValue = Mathf.Clamp(newValue, yearSlider.minValue, yearSlider.maxValue);
    yearSlider.value = newValue;
  }

  void ResetAvailableInventions() {
    availableInventions = new List<InventionData>(allInventions);
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
    if (isAnimating)
      return;

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

    mainButton.interactable = false;

    StartCoroutine(AnimateCardFlyOut(() => {
      sliderArea.SetActive(false);
      factArea.SetActive(true);
      factText.text = $"<b>Интересный факт:</b>\n{currentInvention.description}";
      isRoundFinished = true;
      buttonText.text = "Продолжить";
      mainButton.interactable = true;
    }));
  }

  void ContinueToNextPlayer() {
    if (isAnimating)
      return;

    bool isLastPlayerInRound = (currentPlayerIndex == numberOfPlayers - 1);

    // Переключаем игрока
    currentPlayerIndex = (currentPlayerIndex + 1) % numberOfPlayers;
    if (currentPlayerIndex == 0)
      currentRound++;

    // Проверка конца игры
    if (currentRound >= totalRounds) {
      EndGame();
      return;
    }

    mainButton.interactable = false;

    if (isLastPlayerInRound) {
      PrepareNextInvention();
      StartCoroutine(AnimateCardFlyIn(() => {
        StartNewRoundAfterAnimation();
        mainButton.interactable = true;
      }));
    } else {
      StartCoroutine(AnimateCardFlyInWithSameInvention(() => {
        float startValue = (currentInvention.minYear + currentInvention.maxYear) / 2f;
        yearSlider.value = startValue;
        OnSliderChanged(startValue);

        string playerColorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(currentPlayerIndex));
        statusText.text =
            $"Раунд {currentRound + 1}/{totalRounds} | Ход <color=#{playerColorHex}>Игрока {currentPlayerIndex + 1}</color>";

        sliderArea.SetActive(true);
        factArea.SetActive(false);
        isRoundFinished = false;
        buttonText.text = "Угадать";
        mainButton.interactable = true;
      }));
    }
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

  void PrepareNextInvention() {
    if (availableInventions.Count == 0)
      ResetAvailableInventions();
    int randomIndex = Random.Range(0, availableInventions.Count);
    nextInvention = availableInventions[randomIndex];
    availableInventions.RemoveAt(randomIndex);

    inventionNameText.text = nextInvention.inventionName;
    inventionImage.sprite = nextInvention.inventionImage;
    yearSlider.minValue = nextInvention.minYear;
    yearSlider.maxValue = nextInvention.maxYear;
  }

  void StartNewRoundAfterAnimation() {
    currentInvention = nextInvention;
    float startValue = (currentInvention.minYear + currentInvention.maxYear) / 2f;
    yearSlider.value = startValue;
    OnSliderChanged(startValue);

    string playerColorHex = ColorUtility.ToHtmlStringRGB(GetPlayerColor(currentPlayerIndex));
    statusText.text =
        $"Раунд {currentRound + 1}/{totalRounds} | Ход <color=#{playerColorHex}>Игрока {currentPlayerIndex + 1}</color>";

    sliderArea.SetActive(true);
    factArea.SetActive(false);
    isRoundFinished = false;
    buttonText.text = "Угадать";
    UpdateSliderColorForPlayer(currentPlayerIndex);
  }

  IEnumerator AnimateCardFlyOut(System.Action onComplete) {
    isAnimating = true;
    float elapsed = 0f;
    Vector2 startPos = inventionCard.anchoredPosition;
    Quaternion startRot = inventionCard.localRotation;
    float startAlpha = cardCanvasGroup.alpha;
    Quaternion targetRot = startRot * Quaternion.Euler(0, 0, flyOutRotationAmount);

    while (elapsed < flyOutDuration) {
      elapsed += Time.deltaTime;
      float t = elapsed / flyOutDuration;

      float moveT = flyOutMoveCurve.Evaluate(t);
      float rotT = flyOutRotateCurve.Evaluate(t);
      float alphaT = flyOutAlphaCurve.Evaluate(t);

      inventionCard.anchoredPosition = Vector2.Lerp(startPos, offScreenLeft, moveT);
      inventionCard.localRotation = Quaternion.Lerp(startRot, targetRot, rotT);
      cardCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, alphaT);
      yield return null;
    }

    inventionCard.anchoredPosition = offScreenLeft;
    cardCanvasGroup.alpha = 0f;
    isAnimating = false;
    onComplete?.Invoke();
  }

  void UpdateSliderColorForPlayer(int playerIndex) {
    if (yearSlider != null && playerColors != null && playerIndex < playerColors.Length) {
      sliderFillImage.color = playerColors[playerIndex];
    }
  }

  IEnumerator AnimateCardFlyIn(System.Action onComplete) {
    isAnimating = true;

    inventionCard.anchoredPosition = offScreenRight;
    inventionCard.localRotation = Quaternion.Euler(0, 0, flyInRotationAmount);
    cardCanvasGroup.alpha = 0f;

    float elapsed = 0f;
    Vector2 startPos = offScreenRight;
    Quaternion startRot = inventionCard.localRotation;
    float startAlpha = 0f;

    while (elapsed < flyInDuration) {
      elapsed += Time.deltaTime;
      float t = elapsed / flyInDuration;

      float moveT = flyInMoveCurve.Evaluate(t);
      float rotT = flyInRotateCurve.Evaluate(t);
      float alphaT = flyInAlphaCurve.Evaluate(t);

      inventionCard.anchoredPosition = Vector2.Lerp(startPos, cardStartPosition, moveT);
      inventionCard.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, rotT);
      cardCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, alphaT);
      yield return null;
    }

    inventionCard.anchoredPosition = cardStartPosition;
    inventionCard.localRotation = Quaternion.identity;
    cardCanvasGroup.alpha = 1f;
    isAnimating = false;
    onComplete?.Invoke();
  }

  IEnumerator AnimateCardFlyInWithSameInvention(System.Action onComplete) {
    isAnimating = true;

    inventionCard.anchoredPosition = offScreenRight;
    inventionCard.localRotation = Quaternion.Euler(0, 0, flyInRotationAmount);
    cardCanvasGroup.alpha = 0f;

    float elapsed = 0f;
    Vector2 startPos = offScreenRight;
    Quaternion startRot = inventionCard.localRotation;
    float startAlpha = 0f;

    while (elapsed < flyInDuration) {
      elapsed += Time.deltaTime;
      float t = elapsed / flyInDuration;

      float moveT = flyInMoveCurve.Evaluate(t);
      float rotT = flyInRotateCurve.Evaluate(t);
      float alphaT = flyInAlphaCurve.Evaluate(t);

      inventionCard.anchoredPosition = Vector2.Lerp(startPos, cardStartPosition, moveT);
      inventionCard.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, rotT);
      cardCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, alphaT);
      yield return null;
    }

    inventionCard.anchoredPosition = cardStartPosition;
    inventionCard.localRotation = Quaternion.identity;
    cardCanvasGroup.alpha = 1f;
    isAnimating = false;
    UpdateSliderColorForPlayer(currentPlayerIndex);
    onComplete?.Invoke();
  }
}
