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
using Random = UnityEngine.Random;

public class Ball : MonoBehaviour
{
    [SerializeField]
    private GameplaySettings settings;

    private new Rigidbody rigidbody;

    private new SphereCollider collider;

    private AudioSource audioSource;

    [SerializeField]
    private AudioClip[] collisions;

    [SerializeField]
    private AudioClip shotHit;
    
    public event Action<Facade> OnFacadeCollision; 
    
    private Facade ground;
    private Vector3? oldGroundPosition;
    
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        collider = GetComponent<SphereCollider>();
        audioSource = GetComponent<AudioSource>();
        settings.OnUpdated += ApplySettings;
        ApplySettings();
    }

    public Hole Hole
    {
        set => ground = value.Ground;
    }
    
    private void ApplySettings()
    {
        transform.localScale = settings.ballDiameter * Vector3.one;
        rigidbody.drag = settings.ballDrag;
        collider.sharedMaterial.bounciness = settings.defaultBounciness;
    }

    private void OnDestroy()
    {
        settings.OnUpdated -= ApplySettings;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (audioSource != null && collisions != null && collisions.Length > 0)
        {
            audioSource.PlayOneShot(collisions[Random.Range(0, collisions.Length)]);
        }

        Facade facade = other.gameObject.GetComponent<Facade>();
        if (facade != null)
        {
            OnFacadeCollision?.Invoke(facade);
        }
    }
    
    private void FixedUpdate()
    {
        if (ground != null)
        {
            if (oldGroundPosition.HasValue)
            {
                rigidbody.position += ground.transform.position - oldGroundPosition.Value;
            }

            oldGroundPosition = ground.transform.position;
        }
        else
        {
            oldGroundPosition = null;
        }
    }

    public float Radius => collider.radius * transform.lossyScale.x;

    public void OnShotHit()
    {
        if (audioSource != null && shotHit != null)
        {
            audioSource.PlayOneShot(shotHit);
        }
    }
}
