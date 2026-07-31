using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class UI
    {
        public Canvas Canvas => Main.instance.canvas;
        public Main main => Main.instance;

        public UI()
        {
            cursorTextObj = new GameObject("CursorText", new Type[] { typeof(RectTransform), typeof(TextMeshProUGUI) });
            cursorTextObj.GetComponent<RectTransform>().parent = Canvas.transform;
            cursorTextObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            cursorTextObj.GetComponent<RectTransform>().anchorMax = Vector2.zero;
            cursorTextObj.GetComponent<RectTransform>().pivot = new(0,1);
            cursorTextObj.GetComponent<TextMeshProUGUI>().text = "Lorem Ipsum";
            cursorTextObj.GetComponent<TextMeshProUGUI>().fontSize = 16f;




            Shader.SetGlobalInt("_ViewMode", (int)viewMode);
        }

        public GameObject cursorTextObj;
        public enum ViewMode
        {
            Gas,
            Pressure,
            Temperature,
        }
        public static ViewMode viewMode;
        public Vector2 ScreenPosToCanvas(Vector2 screenPos)
        {
            return new(screenPos.x / Camera.main.pixelWidth * (Canvas.transform as RectTransform).sizeDelta.x, screenPos.y / Camera.main.pixelHeight * (Canvas.transform as RectTransform).sizeDelta.y);
        }

        public void Update()
        {
            (cursorTextObj.transform as RectTransform).position = ScreenPosToCanvas((Vector2)Input.mousePosition + Vector2.right * 10);

            if (Input.GetKeyDown(KeyCode.A))
            {
                if (viewMode == ViewMode.Temperature)
                {
                    viewMode = ViewMode.Gas;
                }
                else
                {
                    viewMode++;
                }
                Shader.SetGlobalInt("_ViewMode", (int)viewMode);
            }
            Vector2 normalizedMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition) / 10f;
            Shader.SetGlobalVector("_MousePos", normalizedMousePos);
            int gridPosX = Mathf.Clamp(Mathf.FloorToInt(normalizedMousePos.x * main.gridWidth), 0, main.gridWidth - 1);
            int gridPosY = Mathf.Clamp(Mathf.FloorToInt(normalizedMousePos.y * main.gridHeight), 0, main.gridHeight - 1);
            Shader.SetGlobalInt("_MousePosX", gridPosX);
            Shader.SetGlobalInt("_MousePosY", gridPosY);
            switch (viewMode)
            {
                case ViewMode.Gas:
                    cursorTextObj.GetComponent<TextMeshProUGUI>().text = $"Gases:\n H2 {Main.mainGrid[gridPosX, gridPosY].amount1}\n O2 {Main.mainGrid[gridPosX, gridPosY].amount2}";
                    break;
                case ViewMode.Pressure:
                    cursorTextObj.GetComponent<TextMeshProUGUI>().text = $"{Main.mainGrid[gridPosX, gridPosY].Pressure} Pa";
                    break;
                case ViewMode.Temperature:
                    cursorTextObj.GetComponent<TextMeshProUGUI>().text = $"{Main.mainGrid[gridPosX, gridPosY].Temperature} K";
                    break;
            }
        }
    }
}
