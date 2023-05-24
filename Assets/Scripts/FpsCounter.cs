// Copyright 2023 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;

public class FpsCounter : MonoBehaviour
{
    // Attach this to any object to make a frames/second indicator.
    //
    // It calculates frames/second over each updateInterval,
    // so the display does not keep changing wildly.
    //
    // It is also fairly accurate at very low FPS counts (<10).
    // We do this not by simply counting frames per interval, but
    // by accumulating FPS for each frame. This way we end up with
    // corstartRect overall FPS even if the interval renders something like
    // 5.5 frames.

    public Rect startRect = new Rect(10 , 75, 100, 50); // The rect the window is initially displayed at.
    public float frequency = 0.5F; // The update frequency of the fps
    public int nbDecimal = 1; // How many decimal do you want to display
    public int fontSize = 50;
    
    private float accum = 0f; // FPS accumulated over the interval
    private int frames = 0; // Frames drawn over the interval
    private string sFPS = ""; // The fps formatted into a string.
    private GUIStyle style; // The style the text will be displayed at, based en defaultSkin.label.

    public Color redColor;
    public Color greenColor;
    
    private float lastFps;

    private Texture2D greenTex;
    private Texture2D redTex;

    private Texture2D texToUse;

    public Font font;
    
    void Start()
    {
        StartCoroutine(FPS());
    }

    void Update()
    {
        accum += Time.timeScale / Time.deltaTime;
        ++frames;
    }

    IEnumerator FPS()
    {
        // Infinite loop executed every "frenquency" secondes.
        while (true)
        {
            // Update the FPS
            float fps = accum / frames;
            sFPS = fps.ToString("f" + Mathf.Clamp(nbDecimal, 0, 10));

            accum = 0.0F;
            frames = 0;

            if (fps - lastFps < -1)
            {
                texToUse = redTex;
            }
            else
            {
                texToUse = greenTex;
            }
            
            lastFps = fps;

            yield return new WaitForSeconds(frequency);
        }
    }

    void OnGUI()
    {
        // Copy the default label skin, change the color and the alignment
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.white;
            style.alignment = TextAnchor.MiddleCenter;
            style.font = font;
            greenTex = new Texture2D(2,2);
            greenTex. SetPixels(new Color[] {greenColor, greenColor, greenColor, greenColor}); 
            greenTex.Apply();
            
            redTex = new Texture2D(2,2);
            redTex. SetPixels(new Color[] {redColor, redColor, redColor, redColor});
            redTex.Apply();
        }

        style.normal.background = texToUse;
        
        style.fontSize = fontSize * Screen.height / 1080;
        
        GUI.Label(new Rect(startRect.x / 1920 * Screen.width, startRect.y / 1080 * Screen.height, startRect.width / 1920 * Screen.width, startRect.height / 1080 * Screen.height), sFPS + " FPS", style);
    }
}
