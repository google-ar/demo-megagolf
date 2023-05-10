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
using UnityEngine;

[CreateAssetMenu(fileName = "GameplaySettings", menuName = "ScriptableObjects/Gameplay Settings", order = 1)]
public class GameplaySettings : ScriptableObject
{
    public float forcePerSecond;
    public float ballDiameter;
    public float ballDrag;
    public float defaultBounciness;
    public float ballSpawnDistance;
    public float ballSpawnHeight;
    public float holeSpawnDistanceMin;
    public float holeSpawnDistanceMax;
    public float holeSpawnAngleMax;
    public float holeRadius;
    public float holeSpawnPadding;
    public float buildingOpacityDefault;
    public float buildingOpacityOnHit;
    public float floorOffset;
    public float obstacleHoleMargin;
    public float obstacleBallMargin;
    
    public event Action OnUpdated;

    public void PublishUpdates()
    {
        OnUpdated?.Invoke();
    }
}
