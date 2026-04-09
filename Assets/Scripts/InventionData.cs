using UnityEngine;

[CreateAssetMenu(fileName = "NewInvention", menuName = "Invention Data")]
public class InventionData : ScriptableObject {
  public string inventionName;
  [Header("Диапазон слайдера")]
  [Tooltip("Минимальный год для слайдера")]
  public int minYear = 1800;
  [Tooltip("Максимальный год для слайдера")]
  public int maxYear = 2026;
  [Header("Правильный ответ")]
  [Tooltip("Год изобретения (должен быть в пределах minYear..maxYear)")]
  public int correctYear = 1900;
  public Sprite inventionImage;
  [TextArea]
  public string description;

  private void OnValidate() {
    if (minYear > maxYear)
      minYear = maxYear - 1;
    correctYear = Mathf.Clamp(correctYear, minYear, maxYear);
  }
}
