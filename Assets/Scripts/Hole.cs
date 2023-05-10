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
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class Hole : MonoBehaviour
{
    [SerializeField]
    private TriggerPublish ballInTrigger;

    [SerializeField]
    private TriggerPublish ballOnTopTrigger;

    [SerializeField]
    private GameplaySettings settings;

    [SerializeField] 
    private float ballDestroyDelay;
    
    private Vector3 sphereColliderCenter;

    private bool ballInside;

    private Facade ground;

    private SphereCollider ballOnTopCollider;

    private event Action<Ball> onBallIn;

    private readonly HashSet<Ball> ballsOnTop = new();
    
    public Facade Ground
    {
        get => ground;
        set
        {
            ground = value;

            if (ground != null)
            {
                Debug.Log($"Hole is anchored to: {ground.TrackableId}");
            }
        }
    }

    private void Awake()
    {
        ballOnTopCollider = ballOnTopTrigger.GetComponent<SphereCollider>();
        settings.OnUpdated += RefreshSize;
        ballInTrigger.OnStateChanged += OnBallInChanged;
        ballOnTopTrigger.OnStateChanged += OnBallOnTop;
    }

    private void Start()
    {
        RefreshSize();
    }

    private void OnBallOnTop(bool isBallOnTop, Collider ballCollider)
    {
        Debug.Log($"BallOnTop {isBallOnTop} {ballCollider.gameObject.GetHashCode()}");
        int newLayer = LayerMask.NameToLayer(isBallOnTop ? "Fall Through" : "Ball");
        Transform ballRoot = ballCollider.transform.root;
        ballRoot.gameObject.SetLayerRecursively(newLayer);
        Ball ball = ballRoot.GetComponentInChildren<Ball>();
        if (isBallOnTop)
        {
            ballsOnTop.Add(ball);
        }
        else
        {
            ballsOnTop.Remove(ball);
        }
    }

    public void AddBallInListener(Action<Ball> onBallIn)
    {
        this.onBallIn += onBallIn;
    }

    public void RemoveBallInListener(Action<Ball> onBallIn)
    {
        this.onBallIn -= onBallIn;
    }

    private void OnBallInChanged(bool isBallIn, Collider ballCollider)
    {
        Debug.Log($"BallInChanged {isBallIn} {ballCollider.gameObject.GetHashCode()}");
        if (isBallIn)
        {
            Ball ball = ballCollider.transform.root.GetComponent<Ball>();
            onBallIn?.Invoke(ball);
            StartCoroutine(DestroyBall(ball, ballDestroyDelay));
        }
    }

    private void RefreshSize()
    {
        float localScale = 2f * settings.holeRadius / ballOnTopCollider.transform.localScale.x;
        transform.localScale = localScale * Vector3.one;
    }

    private IEnumerator DestroyBall(Ball ball, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ball != null)
        {
            Destroy(ball.gameObject);
        }
    }

    public bool IsOnTop(Ball ball)
    {
        return ballsOnTop.Contains(ball);
    }

    public Vector3 Origin => ballOnTopCollider.transform.position;

    public float Radius => ballOnTopCollider.radius * ballOnTopCollider.transform.lossyScale.x;

    private void OnDestroy()
    {
        settings.OnUpdated -= RefreshSize;
    }
}
