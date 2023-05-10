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

﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SafeAreaPanel : MonoBehaviour
{

	public bool symetrical = true;
	private RectTransform _rectTransform;

	private void Awake() {
		_rectTransform = GetComponent<RectTransform>();
		RefreshPanel(Screen.safeArea);
	}

	private void OnEnable() {
		SafeAreaDetection.OnSafeAreaChanged += RefreshPanel;
	}

	private void OnDisable() {
		SafeAreaDetection.OnSafeAreaChanged -= RefreshPanel;
	}

	private void RefreshPanel(Rect safeArea) {

		var anchorMin = safeArea.position;
		var anchorMax = safeArea.position + safeArea.size;

		if (symetrical && safeArea.size.x < Screen.width && anchorMin.x < 0.1f)
		{
			// safe area not equal on both sides, add space to the left
			anchorMin = new Vector2(anchorMin.x + Screen.width - safeArea.size.x, anchorMin.y);
		} else if (symetrical && safeArea.size.x < Screen.width && Math.Abs(anchorMax.x - Screen.width) < 0.1f)
		{
			// safe area not equal on both sides, remove space from the right
			anchorMax = new Vector2(anchorMax.x - (Screen.width - safeArea.size.x), anchorMax.y);
		}

		anchorMin.x /= Screen.width;
		anchorMin.y /= Screen.height;
		anchorMax.x /= Screen.width;
		anchorMax.y /= Screen.height;

		_rectTransform.anchorMin = anchorMin;
		_rectTransform.anchorMax = anchorMax;
	}
}
