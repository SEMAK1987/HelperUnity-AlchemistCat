using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент процедурного заострения концов UI Image для энергетических лучей и полос шкалы.
/// Придает лучам форму ограненного кристалла или наконечника стрелы в соответствии с формой золотой рамки.
/// </summary>
[RequireComponent(typeof(Graphic))]
[AddComponentMenu("UI/Effects/UI Tapered Beam Effect")]
public class UI_TaperedBeamEffect : BaseMeshEffect
{
    public enum TaperSide // Сторона скоса
    {
        Left,  // Заострение левого края (для левого луча)
        Right, // Заострение правого края (для правого луча)
        Both   // Заострение с обеих сторон (форма кристалла)
    }

    [Header("=== Настройки формы острия ===")]
    public TaperSide taperSide = TaperSide.Left; // Выбранная сторона заострения
    [Range(0f, 150f)] public float taperWidth = 35f; // Длина скоса (глубина острия в пикселях)

    public override void ModifyMesh(VertexHelper vh) // Модификация вершин графического полигона
    {
        if (!IsActive() || vh.currentVertCount < 4) return; // Проверка активности

        RectTransform rt = graphic.rectTransform; // Трансформ элемента
        Rect r = rt.rect; // Прямоугольник верстки
        float width = r.width; // Полная ширина
        float height = r.height; // Полная высота

        if (width <= 2f || height <= 2f) return; // Защита от нулевого размера

        // Эффективная длина скоса (не более 50% ширины)
        float actualTaper = Mathf.Min(taperWidth, width * 0.5f);
        if (actualTaper <= 0.5f) return; // Если скос нулевой, оставляем стандартный прямоугольник

        vh.Clear(); // Очистка стандартных вершин

        float xMin = r.xMin; // Левая граница
        float xMax = r.xMax; // Правая граница
        float yMin = r.yMin; // Нижняя граница
        float yMax = r.yMax; // Верхняя граница
        float yMid = (yMin + yMax) * 0.5f; // Центральная горизонтальная ось острия

        Color32 col = graphic.canvasRenderer.GetColor(); // Текущий цвет элемента

        if (taperSide == TaperSide.Left) // Заострение левого конца (влево)
        {
            float xCut = xMin + actualTaper; // Точка перехода острия в прямоугольник
            float uCut = Mathf.Clamp01(actualTaper / width); // Текстурная координата U

            // 1. Треугольное острие слева (вершина на острие xMin, yMid)
            UIVertex tip = CreateVert(new Vector3(xMin, yMid, 0), new Vector2(0f, 0.5f), col);
            UIVertex topCut = CreateVert(new Vector3(xCut, yMax, 0), new Vector2(uCut, 1f), col);
            UIVertex botCut = CreateVert(new Vector3(xCut, yMin, 0), new Vector2(uCut, 0f), col);

            vh.AddVert(tip); // 0
            vh.AddVert(topCut); // 1
            vh.AddVert(botCut); // 2
            vh.AddTriangle(0, 1, 2); // Треугольник наконечника

            // 2. Основной прямоугольный блок справа
            UIVertex rightTop = CreateVert(new Vector3(xMax, yMax, 0), new Vector2(1f, 1f), col);
            UIVertex rightBot = CreateVert(new Vector3(xMax, yMin, 0), new Vector2(1f, 0f), col);

            vh.AddVert(topCut);   // 3
            vh.AddVert(rightTop); // 4
            vh.AddVert(rightBot); // 5
            vh.AddVert(botCut);   // 6

            vh.AddTriangle(3, 4, 5); // Верхний треугольник прямоугольника
            vh.AddTriangle(5, 6, 3); // Нижний треугольник прямоугольника
        }
        else if (taperSide == TaperSide.Right) // Заострение правого конца (вправо)
        {
            float xCut = xMax - actualTaper; // Точка перехода в правое острие
            float uCut = Mathf.Clamp01((width - actualTaper) / width); // Текстурная координата U

            // 1. Основной прямоугольный блок слева
            UIVertex leftBot = CreateVert(new Vector3(xMin, yMin, 0), new Vector2(0f, 0f), col);
            UIVertex leftTop = CreateVert(new Vector3(xMin, yMax, 0), new Vector2(0f, 1f), col);
            UIVertex cutTop = CreateVert(new Vector3(xCut, yMax, 0), new Vector2(uCut, 1f), col);
            UIVertex cutBot = CreateVert(new Vector3(xCut, yMin, 0), new Vector2(uCut, 0f), col);

            vh.AddVert(leftBot); // 0
            vh.AddVert(leftTop); // 1
            vh.AddVert(cutTop);  // 2
            vh.AddVert(cutBot);  // 3

            vh.AddTriangle(0, 1, 2); // Верхний треугольник
            vh.AddTriangle(2, 3, 0); // Нижний треугольник

            // 2. Треугольное острие справа (вершина на острие xMax, yMid)
            UIVertex tip = CreateVert(new Vector3(xMax, yMid, 0), new Vector2(1f, 0.5f), col);

            vh.AddVert(cutTop); // 4
            vh.AddVert(tip);    // 5
            vh.AddVert(cutBot); // 6

            vh.AddTriangle(4, 5, 6); // Треугольник правого наконечника
        }
        else // Both — Заострение с обоих концов (форма кристалла)
        {
            float xCutL = xMin + actualTaper; // Левый срез
            float xCutR = xMax - actualTaper; // Правый срез
            float uCutL = Mathf.Clamp01(actualTaper / width); // UV левого среза
            float uCutR = Mathf.Clamp01((width - actualTaper) / width); // UV правого среза

            // Левое острие
            UIVertex tipL = CreateVert(new Vector3(xMin, yMid, 0), new Vector2(0f, 0.5f), col);
            UIVertex topL = CreateVert(new Vector3(xCutL, yMax, 0), new Vector2(uCutL, 1f), col);
            UIVertex botL = CreateVert(new Vector3(xCutL, yMin, 0), new Vector2(uCutL, 0f), col);

            vh.AddVert(tipL); // 0
            vh.AddVert(topL); // 1
            vh.AddVert(botL); // 2
            vh.AddTriangle(0, 1, 2);

            // Центральный блок
            UIVertex topR = CreateVert(new Vector3(xCutR, yMax, 0), new Vector2(uCutR, 1f), col);
            UIVertex botR = CreateVert(new Vector3(xCutR, yMin, 0), new Vector2(uCutR, 0f), col);

            vh.AddVert(topL); // 3
            vh.AddVert(topR); // 4
            vh.AddVert(botR); // 5
            vh.AddVert(botL); // 6
            vh.AddTriangle(3, 4, 5);
            vh.AddTriangle(5, 6, 3);

            // Правое острие
            UIVertex tipR = CreateVert(new Vector3(xMax, yMid, 0), new Vector2(1f, 0.5f), col);
            vh.AddVert(topR); // 7
            vh.AddVert(tipR); // 8
            vh.AddVert(botR); // 9
            vh.AddTriangle(7, 8, 9);
        }
    }

    private UIVertex CreateVert(Vector3 pos, Vector2 uv, Color32 col) // Создание вершины полигона
    {
        UIVertex v = new UIVertex();
        v.position = pos; // Позиция в пикселях RectTransform
        v.uv0 = uv; // Текстурная координата
        v.color = col; // Цвет с учетом прозрачности
        v.normal = Vector3.back; // Нормаль к плоскости экрана
        v.tangent = new Vector4(1, 0, 0, -1); // Тангент
        return v;
    }
}
