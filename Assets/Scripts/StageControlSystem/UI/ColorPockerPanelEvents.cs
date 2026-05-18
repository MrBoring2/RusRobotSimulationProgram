using Assets.Scripts.Models;
using Assets.Scripts.StageControlSystem.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.StageControlSystem.UI
{
    public class ColorPockerPanelEvent : BaseModalWindow
    {
        // HSV
        private float hue;
        private float saturation = 1f;
        private float value = 1f;
        private float alpha = 1f;

        // RGB (вычисляются из HSV)
        private float r = 1f, g = 1f, b = 1f;

        // UI элементы
        private Slider hueSlider;
        private VisualElement svPicker;
        private VisualElement svHandle;
        private Slider rSlider, gSlider, bSlider, alphaSlider;
        private TextField rInput, gInput, bInput;
        private VisualElement previewSwatch;
        private TextField hexInput;
        private Button applyBtn, cancelBtn;

        private bool isDraggingSV;
        private bool isUpdatingUI;

        private ColorPickerResult _result;

        protected override void OnBeforeShow(ModalParameters parameters)
        {
            _result = new ColorPickerResult();
            if (parameters != null)
            {
                var c = parameters.Get<Color>("initialColor");
                SetColor(c);
                _result.Color = c;
            }
            else
            {
                SetColor(Color.white);
                _result.Color = Color.white;
            }
            _result.IsApplied = false;
        }
        private void CreateHueSliderBackground()
        {
            int width = 256;
            int height = 1;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float h = (float)x / (width - 1);
                pixels[x] = Color.HSVToRGB(h, 1f, 1f);
            }

            tex.SetPixels(pixels);
            tex.Apply();

            hueSlider.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }
        protected override void InitializeElements(VisualElement root)
        {
            hueSlider = root.Q<Slider>("hue-slider");
            svPicker = root.Q<VisualElement>("sv-picker");
            rSlider = root.Q<Slider>("r-slider");
            gSlider = root.Q<Slider>("g-slider");
            bSlider = root.Q<Slider>("b-slider");
            //alphaSlider = root.Q<Slider>("alpha-slider");
            rInput = root.Q<TextField>("r-input");
            gInput = root.Q<TextField>("g-input");
            bInput = root.Q<TextField>("b-input");
            previewSwatch = root.Q<VisualElement>("preview-swatch");
            hexInput = root.Q<TextField>("hex-input");
            applyBtn = root.Q<Button>("apply-btn");
            cancelBtn = root.Q<Button>("cancel-btn");

            // Блокируем ТОЛЬКО svPicker, чтобы не дёргалось окно
            BlockEventBubbling(svPicker);
            // Слайдеры НЕ блокируем!
            CreateHueSliderBackground();
            svHandle = new VisualElement();
            svHandle.style.position = Position.Absolute;
            svHandle.style.width = 14;
            svHandle.style.height = 14;
            svHandle.style.backgroundColor = new Color(0, 0, 0, 0.3f);
            svHandle.style.translate = new Translate(-7, -7);
            svHandle.pickingMode = PickingMode.Ignore;
            svPicker.Add(svHandle);
       
            UpdateAllUI();
        }

        private void BlockEventBubbling(VisualElement element)
        {
            element.RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
            element.RegisterCallback<MouseMoveEvent>(evt => evt.StopPropagation());
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            // ===== HUE SLIDER =====
            hueSlider.RegisterValueChangedCallback(evt =>
            {
                hue = evt.newValue;
                UpdateSVBackground();
                UpdateRGBFromHSV();
            });

            // ===== RGB SLIDERS =====
            rSlider.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                r = evt.newValue / 255f;
                UpdateHSVFromRGB();
            });
            gSlider.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                g = evt.newValue / 255f;
                UpdateHSVFromRGB();
            });
            bSlider.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                b = evt.newValue / 255f;
                UpdateHSVFromRGB();
            });
            //alphaSlider.RegisterValueChangedCallback(evt =>
            //{
            //    Debug.Log($"Alpha: {evt.newValue}");
            //    alpha = evt.newValue / 255f;
            //    UpdatePreviewAndHex();
            //});

            // ===== RGB INPUTS =====
            rInput.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                if (float.TryParse(evt.newValue, out float val))
                {
                    r = Mathf.Clamp01(val / 255f);
                    UpdateHSVFromRGB();
                }
            });
            gInput.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                if (float.TryParse(evt.newValue, out float val))
                {
                    g = Mathf.Clamp01(val / 255f);
                    UpdateHSVFromRGB();
                }
            });
            bInput.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                if (float.TryParse(evt.newValue, out float val))
                {
                    b = Mathf.Clamp01(val / 255f);
                    UpdateHSVFromRGB();
                }
            });

            // ===== HEX =====
            hexInput.RegisterValueChangedCallback(evt =>
            {
                if (isUpdatingUI) return;
                string hex = evt.newValue;
                if (!hex.StartsWith("#")) hex = "#" + hex;
                if (ColorUtility.TryParseHtmlString(hex, out Color c))
                {
                    SetColor(c);
                }
            });

            // ===== SV PICKER =====
            svPicker.RegisterCallback<PointerDownEvent>(OnSVPickerDown);
            svPicker.RegisterCallback<PointerMoveEvent>(OnSVPickerMove);
            svPicker.RegisterCallback<PointerUpEvent>(OnSVPickerUp);

            applyBtn.clicked += ApplyColorChange;
            cancelBtn.clicked += CancelColorChange;
        }

        // ===== SV DRAG =====
        private void OnSVPickerDown(PointerDownEvent evt)
        {
            isDraggingSV = true;
            svPicker.CapturePointer(evt.pointerId);
            UpdateSVFromPointer(evt.localPosition);
        }

        private void OnSVPickerMove(PointerMoveEvent evt)
        {
            if (isDraggingSV)
                UpdateSVFromPointer(evt.localPosition);
        }

        private void OnSVPickerUp(PointerUpEvent evt)
        {
            if (isDraggingSV)
            {
                isDraggingSV = false;
                svPicker.ReleasePointer(evt.pointerId);
            }
        }

        private void UpdateSVFromPointer(Vector2 localPos)
        {
            float w = svPicker.resolvedStyle.width;
            float h = svPicker.resolvedStyle.height;

            saturation = Mathf.Clamp01(localPos.x / w);
            value = Mathf.Clamp01(localPos.y / h);  // Было: 1f - (localPos.y / h)

            UpdateSVHandlePosition();
            UpdateRGBFromHSV();
        }

        // ===== ОБНОВЛЕНИЕ =====
        private void UpdateRGBFromHSV()
        {
            Color c = Color.HSVToRGB(hue / 360f, saturation, value);
            r = c.r;
            g = c.g;
            b = c.b;
            UpdateAllUI();
        }

        private void UpdateHSVFromRGB()
        {
            Color.RGBToHSV(new Color(r, g, b), out hue, out saturation, out value);
            hue *= 360f;
            UpdateSVBackground();
            UpdateSVHandlePosition();
            UpdateAllUI();
        }

        private void UpdateAllUI()
        {
            isUpdatingUI = true;

            hueSlider.SetValueWithoutNotify(hue);
            rSlider.SetValueWithoutNotify(r * 255f);
            gSlider.SetValueWithoutNotify(g * 255f);
            bSlider.SetValueWithoutNotify(b * 255f);
            //alphaSlider.SetValueWithoutNotify(alpha * 255f);

            rInput.SetValueWithoutNotify(Mathf.RoundToInt(r * 255).ToString());
            gInput.SetValueWithoutNotify(Mathf.RoundToInt(g * 255).ToString());
            bInput.SetValueWithoutNotify(Mathf.RoundToInt(b * 255).ToString());

            UpdatePreviewAndHex();

            isUpdatingUI = false;
        }

        private void UpdatePreviewAndHex()
        {
            Color c = new Color(r, g, b, alpha);
            previewSwatch.style.backgroundColor = new StyleColor(c);
            isUpdatingUI = true;
            hexInput.SetValueWithoutNotify(ColorUtility.ToHtmlStringRGB(c));
            isUpdatingUI = false;
        }

        // ===== SV ФОН =====
        private void UpdateSVBackground()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGB24, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = 1f - (float)y / (size - 1);
                for (int x = 0; x < size; x++)
                {
                    float s = (float)x / (size - 1);
                    pixels[y * size + x] = Color.HSVToRGB(hue / 360f, s, v);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            svPicker.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
        }

        private void UpdateSVHandlePosition()
        {
            float w = svPicker.resolvedStyle.width;
            float h = svPicker.resolvedStyle.height;

            svHandle.style.left = saturation * w;
            svHandle.style.top = value * h;  // Было: (1f - value) * h
        }

        // ===== ПУБЛИЧНЫЕ =====
        public void SetColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            alpha = color.a;
            Color.RGBToHSV(color, out hue, out saturation, out value);
            hue *= 360f;
            UpdateAllUI();
            UpdateSVBackground();
            UpdateSVHandlePosition();
        }

        public Color GetCurrentColor()
        {
            return new Color(r, g, b, alpha);
        }

        private void ApplyColorChange()
        {
            _result.Color = GetCurrentColor();
            _result.IsApplied = true;
            CloseWithValue(_result);
        }

        private void CancelColorChange()
        {
            _result.IsApplied = false;
            CloseWithValue(_result);
        }
    }
}
