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
using System.Collections;
using Google.XR.ARCoreExtensions;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public class Facade : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int HoleCenterId = Shader.PropertyToID("_HoleCenter");
    private static readonly int RadiusId = Shader.PropertyToID("_HoleRadius");

    private enum State
    {
        Transparent,
        Highlighted,
        Invisible
    }

    private new Renderer renderer;
    private MeshFilter meshFilter;
    private new MeshCollider collider;

    [SerializeField] 
    private MeshFilter shadowCatcher;

    [SerializeField]
    private GameplaySettings settings;

    [SerializeField]
    private float pulseDuration;

    private State currentState;

    public StreetscapeGeometryType GeometryType { get; set; }
    public TrackableId TrackableId { get; set; }
    
    void Awake()
    {
        renderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        collider = GetComponent<MeshCollider>();
        renderer.material = new Material(renderer.sharedMaterial);
    }

    private void Start()
    {
        settings.OnUpdated += RefreshAlpha;
    }

    public Mesh Mesh
    {
        set
        {
            meshFilter.mesh = value;
            shadowCatcher.mesh = value;
            collider.sharedMesh = value;
        }
    }

    public Color Color
    {
        set => renderer.material.SetColor(BaseColorId, value);
    }
    
    private void RefreshAlpha()
    {
        switch (currentState)
        {
            // set color again to read it again from settings
            case State.Highlighted:
                SetTransparent(State.Highlighted);
                break;
            case State.Transparent:
                SetTransparent(State.Transparent);
                break;
            case State.Invisible:
                SetInvisible();
                break;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != LayerMask.NameToLayer("Ball")) return;

        Debug.Log(
            $"{name}[{GeometryType}][{LayerMask.LayerToName(gameObject.layer)}], OnCollisionEnter {collision.gameObject.GetHashCode()}");

        if (GeometryType == StreetscapeGeometryType.Building)
        {
            Pulse();
        }
    }

    public void SetTransparent() => SetTransparent(State.Transparent);
    public void SetHighlighted() => SetTransparent(State.Highlighted);
    
    private void SetTransparent(State newState)
    {
        Color color = renderer.material.GetColor(BaseColorId);
        color.a = newState == State.Highlighted ? settings.buildingOpacityOnHit : settings.buildingOpacityDefault;
        renderer.material.SetColor(BaseColorId, color);
        currentState = newState;
        renderer.enabled = true;
    }

    public void SetInvisible()
    {
        Color color = renderer.material.GetColor(BaseColorId);
        color.a = 0f;
        renderer.material.SetColor(BaseColorId, color);
        currentState = State.Invisible;
    }

    private void OnDestroy()
    {
        settings.OnUpdated -= RefreshAlpha;
    }

    public void Pulse()
    {
        StartCoroutine(PulseCoroutine());
    }

    private IEnumerator PulseCoroutine()
    {
        float endAlpha;
        switch (currentState)
        {
            case State.Transparent:
                endAlpha = settings.buildingOpacityDefault;
                break;
            case State.Highlighted:
                yield break;
            case State.Invisible:
                endAlpha = 0f;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        State startState = currentState;
        SetTransparent(State.Highlighted);
        Color color = renderer.material.GetColor(BaseColorId);
        float startAlpha = color.a;
        float startTime = Time.timeSinceLevelLoad;
        float elapsed;
        while ((elapsed = Time.timeSinceLevelLoad - startTime) <= pulseDuration)
        {
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / pulseDuration);
            renderer.material.SetColor(BaseColorId, color);
            yield return null;
        }

        if (startState == State.Invisible)
        {
            SetInvisible();
        }
        else
        {
            SetTransparent(startState);
        }
    }

    public Vector3 HolePosition
    {
        set => renderer.material.SetVector(HoleCenterId, value);
    }

    public float HoleRadius
    {
        set => renderer.material.SetFloat(RadiusId, value);
    }
}
