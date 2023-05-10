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

using System;
using TMPro;
using UnityEngine;

public class ParamsUI : MonoBehaviour
{
    [SerializeField] 
    private GameplaySettings settings;
    
    [SerializeField]
    private Transform paramsContent;

    [SerializeField]
    private TMP_InputField forcePerSecond;
    
    [SerializeField]
    private TMP_InputField ballDiameter;
    
    [SerializeField]
    private TMP_InputField defaultBounciness;
    
    [SerializeField]
    private TMP_InputField ballSpawnDistance;
    
    [SerializeField]
    private TMP_InputField ballSpawnHeight;
    
    [SerializeField]
    private TMP_InputField holeSpawnDistanceMin;
    
    [SerializeField]
    private TMP_InputField holeSpawnDistanceMax;
    
    [SerializeField]
    private TMP_InputField holeSpawnAngleMax;
    
    [SerializeField]
    private TMP_InputField holeSize;
    
    [SerializeField]
    private TMP_InputField buildingOpacityOnHit;
    
    [SerializeField]
    private TMP_InputField buildingOpacityDefault;
    
    [SerializeField]
    private TMP_InputField floorOffset;
    
    private void Awake()
    {
        paramsContent.gameObject.SetActive(false);
    }

    private void ApplyParams()
    {
        try
        {
            settings.forcePerSecond = float.Parse(forcePerSecond.text);
            settings.ballDiameter = float.Parse(ballDiameter.text);
            settings.defaultBounciness = float.Parse(defaultBounciness.text);
            settings.ballSpawnDistance = float.Parse(ballSpawnDistance.text);
            settings.ballSpawnHeight = float.Parse(ballSpawnHeight.text);
            settings.holeSpawnDistanceMin = float.Parse(holeSpawnDistanceMin.text);
            settings.holeSpawnDistanceMax = float.Parse(holeSpawnDistanceMax.text);
            settings.holeSpawnAngleMax = float.Parse(holeSpawnAngleMax.text);
            settings.holeRadius = float.Parse(holeSize.text);
            settings.buildingOpacityOnHit = float.Parse(buildingOpacityOnHit.text);
            settings.buildingOpacityDefault = float.Parse(buildingOpacityDefault.text);
            settings.floorOffset = float.Parse(floorOffset.text);
            
            settings.PublishUpdates();
        }
        catch (FormatException)
        {
            
        }
    }

    private void LoadParams()
    {
        forcePerSecond.text = settings.forcePerSecond.ToString();
        ballDiameter.text = settings.ballDiameter.ToString();
        defaultBounciness.text = settings.defaultBounciness.ToString();
        ballSpawnDistance.text = settings.ballSpawnDistance.ToString();
        ballSpawnHeight.text = settings.ballSpawnHeight.ToString();
        holeSpawnDistanceMin.text = settings.holeSpawnDistanceMin.ToString();
        holeSpawnDistanceMax.text = settings.holeSpawnDistanceMax.ToString();
        holeSpawnAngleMax.text = settings.holeSpawnAngleMax.ToString();
        holeSize.text = settings.holeRadius.ToString();
        buildingOpacityOnHit.text = settings.buildingOpacityOnHit.ToString();
        buildingOpacityDefault.text = settings.buildingOpacityDefault.ToString();
        floorOffset.text = settings.floorOffset.ToString();
    }
    
    
    void Start()
    {
        forcePerSecond.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        ballDiameter.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        defaultBounciness.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        ballSpawnDistance.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        ballSpawnHeight.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        holeSpawnDistanceMin.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        holeSpawnDistanceMax.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        holeSpawnAngleMax.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        holeSize.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        buildingOpacityOnHit.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        buildingOpacityDefault.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        floorOffset.characterValidation = TMP_InputField.CharacterValidation.Decimal;
        
        LoadParams();
    }

    public void OnOpenClicked()
    {
        paramsContent.gameObject.SetActive(true);
    }

    public void OnCloseClicked()
    {
        paramsContent.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!paramsContent.gameObject.activeInHierarchy) return;
        
        ApplyParams();
    }
}
