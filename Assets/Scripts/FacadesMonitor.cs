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
using System.Collections.Generic;
using System.Linq;
using Google.XR.ARCoreExtensions;
using UnityEngine;

public class FacadesMonitor : MonoBehaviour
{
    [SerializeField]
    private Facade buildingPrefab;
    
    [SerializeField]
    private Facade terrainPrefab;

    [SerializeField]
    private GameplaySettings settings;

    private readonly Dictionary<ARStreetscapeGeometry, Facade> facades = new();
    
    public event Action<Facade> OnFacadeAdded;

    private ARStreetscapeGeometryManager facadeManager;
    
    private readonly List<Color> colorsLeft = new();
    
    private readonly Color[] colors =
    {
        new(66f / 255f, 133f / 255f, 234f / 255f), // google blue 
        new(219f / 255f, 68f / 255f, 55f / 255f), // google red
        new(244f / 255f, 160f / 255f, 0f / 255f), // google yellow
        new(15f / 255f, 157f / 255f, 88f / 255f) // google green
    };
    
    private void Awake()
    {
        facadeManager = GetComponent<ARStreetscapeGeometryManager>();
    }

    private void Start()
    {
        facadeManager.StreetscapeGeometriesChanged += OnFacadesChanged;
        Debug.Log("Facades monitor registered");
    }

    private void OnDestroy()
    {
        facadeManager.StreetscapeGeometriesChanged -= OnFacadesChanged;
        Debug.Log("Facades monitor unregistered");
    }

    private void OnFacadesChanged(ARStreetscapeGeometriesChangedEventArgs args)
    {
        if (args.Removed != null)
        {
            foreach (var removedFacade in args.Removed)
            {
                Debug.Log($"{nameof(FacadesMonitor)}: {Time.frameCount} Removed facade: {removedFacade.trackableId}");

                if (facades.TryGetValue(removedFacade, out var facade))
                {
                    Destroy(facade.gameObject);
                    facades.Remove(removedFacade);
                }
            }
        }

        if (args.Added != null)
        {
            if (args.Added.Count > 0)
            {
                Debug.Log($"Facades to add: {args.Added.Count}");
            }

            foreach (var addedFacade in args.Added)
            {
                Debug.Log($"{nameof(FacadesMonitor)}: {Time.frameCount} Adding facade: {addedFacade.trackableId} type: {addedFacade.streetscapeGeometryType}");
                Facade prefab;
                switch (addedFacade.streetscapeGeometryType)
                {
                    case StreetscapeGeometryType.Terrain:
                        prefab = terrainPrefab;
                        break;
                    case StreetscapeGeometryType.Building:
                        prefab = buildingPrefab;
                        break;
                    default:
                        throw new ArgumentException();
                }
                var facade = Instantiate(prefab);
                var mesh = addedFacade.mesh;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                facade.Mesh = mesh;
                facade.GeometryType = addedFacade.streetscapeGeometryType;
                facade.Color = addedFacade.streetscapeGeometryType == StreetscapeGeometryType.Building
                    ? NextRandomColor()
                    : Color.white;
                facade.TrackableId = addedFacade.trackableId;
                facade.gameObject.name = addedFacade.trackableId.ToString();
                facade.transform.position = addedFacade.pose.position +
                                            (addedFacade.streetscapeGeometryType == StreetscapeGeometryType.Terrain
                                                ? Vector3.down * settings.floorOffset
                                                : Vector3.zero);
                facade.transform.rotation = addedFacade.pose.rotation;
                
                facades[addedFacade] = facade;
                
                OnFacadeAdded?.Invoke(facade);
            }
        }

        if (args.Updated != null)
        {
            foreach (var updatedFacade in args.Updated)
            {
                // Debug.Log($"{nameof(FacadesMonitor)}: {Time.frameCount} Updated facade: {updatedFacade.trackableId}");
                if (facades.TryGetValue(updatedFacade, out var facade))
                {
                    facade.transform.position = updatedFacade.pose.position +
                                                (updatedFacade.streetscapeGeometryType == StreetscapeGeometryType.Terrain
                                                    ? Vector3.down * settings.floorOffset
                                                    : Vector3.zero);
                    facade.transform.rotation = updatedFacade.pose.rotation;
                }
            }
        }
    }

    private Color NextRandomColor()
    {
        if (colorsLeft.Count <= 0)
        {
            var rnd = new System.Random();
            var colorsShuffled = colors.OrderBy(color => rnd.Next());
            colorsLeft.AddRange(colorsShuffled);
        }

        Color toReturn = colorsLeft[0];
        colorsLeft.RemoveAt(0);
        return toReturn;
    }
    
    public IEnumerable<Facade> Facades => facades.Values;
}
