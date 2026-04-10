using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventionCarousel : MonoBehaviour {
  [Header("Настройки")]
  public RectTransform container;
  public GameObject itemPrefab;
  public int visibleCount = 7;
  public float scrollSpeed = 50f;
  public float overlapFactor = 0.3f;

  [Header("Визуал")]
  public float centerScale = 1.2f;
  public float edgeScale = 0.7f;
  public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
  public int centerSortingOrder = 10;
  public int edgeSortingOrder = 0;
  public bool smoothLayerTransition = true;
  public float layerSmoothSpeed = 5f;

  private List<InventionData> allInventions;
  private List<CarouselItem> items = new List<CarouselItem>();
  private float itemWidth;
  private float spacing;
  private float leftBound, rightBound;
  private Dictionary<CarouselItem, float> currentSortingOrders =
      new Dictionary<CarouselItem, float>();

  private class CarouselItem {
    public RectTransform rect;
    public Image image;
    public Canvas canvas;
  }

  IEnumerator Start() {
    yield return null;
    allInventions = new List<InventionData>(Resources.LoadAll<InventionData>("Inventions"));
    if (allInventions.Count == 0) {
      Debug.LogError("Нет изобретений в Resources/Inventions!");
      yield return null;
    }

    CalculateSizes();
    CreateItems();
    InitialPlacement();
  }

  void Update() {
    foreach (var item in items) {
      Vector2 pos = item.rect.anchoredPosition;
      pos.x -= scrollSpeed * Time.deltaTime;
      item.rect.anchoredPosition = pos;
    }

    foreach (var item in items) {
      float x = item.rect.anchoredPosition.x;

      if (x < leftBound) {
        float newX = GetRightmostX() + spacing;
        item.rect.anchoredPosition = new Vector2(newX, 0);
        item.image.sprite = GetRandomInventionSprite();
      } else if (x > rightBound) {
        float newX = GetLeftmostX() - spacing;
        item.rect.anchoredPosition = new Vector2(newX, 0);
        item.image.sprite = GetRandomInventionSprite();
      }
    }

    ApplyVisualEffects();
  }

  void CalculateSizes() {
    float containerWidth = container.rect.width;
    itemWidth = containerWidth / (visibleCount * (1f - overlapFactor));
    spacing = itemWidth * (1f - overlapFactor);
    leftBound = -containerWidth * 0.5f - spacing;
    rightBound = containerWidth * 0.5f + spacing;
  }

  void CreateItems() {
    int total = visibleCount + 4;
    for (int i = 0; i < total; i++) {
      GameObject go = Instantiate(itemPrefab, container);
      CarouselItem item = new CarouselItem();
      item.rect = go.GetComponent<RectTransform>();
      item.image = go.GetComponent<Image>();
      item.canvas = go.GetComponent<Canvas>();
      if (item.canvas == null)
        item.canvas = go.AddComponent<Canvas>();
      go.AddComponent<GraphicRaycaster>();
      item.canvas.overrideSorting = true;
      item.image.preserveAspect = true;

      item.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth);
      item.rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemWidth);

      items.Add(item);
      currentSortingOrders[item] = edgeSortingOrder;
    }
  }

  void InitialPlacement() {
    int mid = items.Count / 2;
    for (int i = 0; i < items.Count; i++) {
      int offset = i - mid;
      float x = offset * spacing;
      items[i].rect.anchoredPosition = new Vector2(x, 0);
      items[i].image.sprite = GetRandomInventionSprite();
    }
  }

  float GetRightmostX() {
    float max = float.MinValue;
    foreach (var item in items)
      if (item.rect.anchoredPosition.x > max)
        max = item.rect.anchoredPosition.x;
    return max;
  }

  float GetLeftmostX() {
    float min = float.MaxValue;
    foreach (var item in items)
      if (item.rect.anchoredPosition.x < min)
        min = item.rect.anchoredPosition.x;
    return min;
  }

  Sprite GetRandomInventionSprite() {
    return allInventions[Random.Range(0, allInventions.Count)].inventionImage;
  }

  void ApplyVisualEffects() {
    float halfWidth = container.rect.width * 0.5f;

    foreach (var item in items) {
      float x = item.rect.anchoredPosition.x;
      float dist = Mathf.Abs(x) / halfWidth;
      dist = Mathf.Clamp01(dist);

      float scale = Mathf.Lerp(centerScale, edgeScale, scaleCurve.Evaluate(dist));
      item.rect.localScale = Vector3.one * scale;

      float targetOrder = Mathf.Lerp(edgeSortingOrder, centerSortingOrder, 1f - dist);

      if (smoothLayerTransition) {
        float current = currentSortingOrders[item];
        current = Mathf.Lerp(current, targetOrder, Time.deltaTime * layerSmoothSpeed);
        currentSortingOrders[item] = current;
        item.canvas.sortingOrder = Mathf.RoundToInt(current);
      } else {
        item.canvas.sortingOrder = Mathf.RoundToInt(targetOrder);
      }

      Color c = item.image.color;
      c.a = Mathf.Lerp(0.5f, 1f, 1f - dist * 0.5f);
      item.image.color = c;
    }
  }
}
