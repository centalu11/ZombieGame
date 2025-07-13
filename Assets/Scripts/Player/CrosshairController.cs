using UnityEngine;
using UnityEngine.UI;

namespace ZombieGame.Player
{
    public class CrosshairController : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private Color crosshairColor = Color.white;
        [SerializeField] private float crosshairSize = 10f;
        [SerializeField] private float thickness = 2f;
        [SerializeField] private float gapSize = 5f;

        private Canvas _crosshairCanvas;
        private RectTransform _crosshairContainer;
        private RectTransform[] _crosshairLines = new RectTransform[4]; // Top, Right, Bottom, Left

        private void Start()
        {
            SetupCrosshair();
        }

        private void SetupCrosshair()
        {
            // Create canvas
            var canvasObj = new GameObject("CrosshairCanvas");
            canvasObj.transform.SetParent(transform);
            _crosshairCanvas = canvasObj.AddComponent<Canvas>();
            _crosshairCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Add canvas scaler
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Create container
            var containerObj = new GameObject("CrosshairContainer");
            containerObj.transform.SetParent(_crosshairCanvas.transform);
            _crosshairContainer = containerObj.AddComponent<RectTransform>();
            _crosshairContainer.anchoredPosition = Vector2.zero;
            _crosshairContainer.sizeDelta = new Vector2(crosshairSize * 2, crosshairSize * 2);
            
            // Create the four lines
            string[] directions = { "Top", "Right", "Bottom", "Left" };
            for (int i = 0; i < 4; i++)
            {
                var lineObj = new GameObject($"Crosshair{directions[i]}");
                lineObj.transform.SetParent(_crosshairContainer);
                
                var image = lineObj.AddComponent<Image>();
                image.color = crosshairColor;
                
                var rectTransform = image.rectTransform;
                _crosshairLines[i] = rectTransform;
                
                // Set size and position based on direction
                if (i % 2 == 0) // Vertical lines (Top/Bottom)
                {
                    rectTransform.sizeDelta = new Vector2(thickness, crosshairSize);
                    rectTransform.anchoredPosition = new Vector2(0, (i == 0 ? 1 : -1) * (crosshairSize / 2 + gapSize));
                }
                else // Horizontal lines (Right/Left)
                {
                    rectTransform.sizeDelta = new Vector2(crosshairSize, thickness);
                    rectTransform.anchoredPosition = new Vector2((i == 1 ? 1 : -1) * (crosshairSize / 2 + gapSize), 0);
                }
            }

            // Center the crosshair
            _crosshairContainer.anchorMin = _crosshairContainer.anchorMax = new Vector2(0.5f, 0.5f);
        }

        public void SetCrosshairColor(Color color)
        {
            crosshairColor = color;
            foreach (var line in _crosshairLines)
            {
                if (line != null)
                {
                    line.GetComponent<Image>().color = color;
                }
            }
        }

        public void SetCrosshairSize(float size)
        {
            crosshairSize = size;
            if (_crosshairContainer != null)
            {
                _crosshairContainer.sizeDelta = new Vector2(size * 2, size * 2);
                UpdateCrosshairLayout();
            }
        }

        public void SetGapSize(float gap)
        {
            gapSize = gap;
            UpdateCrosshairLayout();
        }

        private void UpdateCrosshairLayout()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_crosshairLines[i] != null)
                {
                    if (i % 2 == 0) // Vertical lines
                    {
                        _crosshairLines[i].sizeDelta = new Vector2(thickness, crosshairSize);
                        _crosshairLines[i].anchoredPosition = new Vector2(0, (i == 0 ? 1 : -1) * (crosshairSize / 2 + gapSize));
                    }
                    else // Horizontal lines
                    {
                        _crosshairLines[i].sizeDelta = new Vector2(crosshairSize, thickness);
                        _crosshairLines[i].anchoredPosition = new Vector2((i == 1 ? 1 : -1) * (crosshairSize / 2 + gapSize), 0);
                    }
                }
            }
        }

        private void OnValidate()
        {
            if (GetComponent<PlayerLoader>() == null)
            {
                Debug.LogError($"[CrosshairController] PlayerLoader component is required on {gameObject.name}! Add PlayerLoader to ensure all required components are properly set up.");
            }
        }
    }
} 