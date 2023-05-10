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
using System.Linq;
using Google.XR.ARCoreExtensions;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameplayController : MonoBehaviour
{
    [SerializeField] private float groundRayLength;

    [SerializeField] private float messageReadTime;

    [SerializeField] private Ball ballPrefab;

    [SerializeField] private FacadesMonitor facadesMonitor;

    [SerializeField] private Hole holePrefab;

    [SerializeField] private XROrigin sessionOrigin;

    [SerializeField] private GameplaySettings settings;

    [SerializeField] private float holeAngleIncrement;

    [SerializeField] private Transform welcomePopup;

    [SerializeField] private TextMeshProUGUI welcomePopupText;

    [SerializeField] private WelcomePopupAnimation welcomePopupAnimation;

    [SerializeField] private Button welcomePopupButton;

    [SerializeField] private float welcomePopupDelay;
    
    [SerializeField] private float welcomeTextAnimationDuration;

    [SerializeField] private RectTransform loadingCourseLabel;

    [SerializeField] private RectTransform tapToShootLabel;

    [SerializeField] private RectTransform holeHitLabel;

    [SerializeField] private RectTransform tryAgainLabel;

    [SerializeField] private Button newCourseButton;

    [SerializeField] private Button addObstacleButton;

    [SerializeField] private RectTransform cantPlaceHoleLabel;

    [SerializeField] private float obstacleAngleIncrement;

    [SerializeField] private int raycastsPerFrame;

    [SerializeField] private Obstacle[] obstaclePrefabs;

    [SerializeField] private float obstacleRayMaxDistance;

    [SerializeField] private RectTransform cantPlaceObstacleLabel;

    [SerializeField] private RectTransform notBilliardsLabel;

    [SerializeField] private float shotPredictionTime;

    [SerializeField] private float shotMinDuration;

    [SerializeField] private float shotMaxDuration;

    [SerializeField] private RectTransform powerBar;

    [SerializeField] private float powerBarMaxWidth;

    [SerializeField] private float powerBarMaxPower;

    [SerializeField] private float playerActiveLingerTime;

    [SerializeField] private AudioSource music;

    [SerializeField] private AudioSource holeHitSound;

    [Range(0f, 1f)] [SerializeField] private float musicLowVolumePercent;

    [SerializeField] private ParticleSystem confettiParticles;

    [SerializeField] private AudioSource shotChargingAudio;

    private Ball activeBall;
    private Rigidbody activeBallRigidbody;
    private Hole hole;
    private bool holeSet;

    private float shootForce;

    private bool shotCharging;
    private bool shotFired;
    private bool freshHole;
    private Transform ballPositionTransform;

    private Obstacle obstacle;

    private readonly List<Ball> oldBalls = new();

    private Coroutine monitorShotCoroutine;

    public Vector3 BallPosition => ballPositionTransform.position;

    public event Action OnNewCourse;

    private float lastCourseResetTime;
    private bool firstFacadeReady;

    void Start()
    {
        shotFired = true;
        ResetShotUI();

        loadingCourseLabel.gameObject.SetActive(false);
        newCourseButton.gameObject.SetActive(false);
        cantPlaceHoleLabel.gameObject.SetActive(false);

        facadesMonitor.OnFacadeAdded += OnFacadeAdded;
        
        welcomePopup.gameObject.SetActive(false);
        Invoke(nameof(ShowWelcomePopup), welcomePopupDelay);
    }

    private void ShowWelcomePopup()
    {
        welcomePopupText.gameObject.SetActive(false);
        welcomePopupButton.gameObject.SetActive(false);
        welcomePopup.gameObject.SetActive(true);
        welcomePopupAnimation.OnAnimationEnd += OnWelcomeAnimationEnd;
        welcomePopupAnimation.StartAnimation();
    }

    private IEnumerator WelcomeTextFadeIn(Action onComplete)
    {
        Color color = welcomePopupText.color;
        float startTime = Time.timeSinceLevelLoad;
        float elapsed;
        while ((elapsed = Time.timeSinceLevelLoad - startTime) < welcomeTextAnimationDuration)
        {
            color.a = elapsed / welcomeTextAnimationDuration;
            welcomePopupText.color = color;
            yield return null;
        }

        color.a = 1f;
        welcomePopupText.color = color;
        onComplete?.Invoke();
    }
    
    private void OnWelcomeAnimationEnd()
    {
        welcomePopupText.gameObject.SetActive(true);
        StartCoroutine(WelcomeTextFadeIn(() => welcomePopupButton.gameObject.SetActive(true)));
    }

    public void OnWelcomePopupClosed()
    {
        welcomePopup.gameObject.SetActive(false);
        if (music != null && music.clip != null)
        {
            music.Play();
        }

        if (facadesMonitor.Facades.Any())
        {
            NewCourse();
        }
        else
        {
            loadingCourseLabel.gameObject.SetActive(true);
        }
    }
    
    private void OnFacadeAdded(Facade newFacade)
    {
        if (hole != null || cantPlaceHoleLabel.gameObject.activeInHierarchy)
        {
            newFacade.SetTransparent();
        }
        else
        {
            // the game hasn't started yet...
            newFacade.SetInvisible();
            if (!firstFacadeReady)
            {
                //...because it's waiting for geometries
                firstFacadeReady = true;
                // give a little time for all geometries to get loaded
                Invoke(nameof(OnFacadesReady), 1f);
            }
        }
    }

    private void OnFacadesReady()
    {
        if (loadingCourseLabel.gameObject.activeInHierarchy)
        {
            loadingCourseLabel.gameObject.SetActive(false);
            NewCourse();
        }
    }
    
    public void NewCourse()
    {
        loadingCourseLabel.gameObject.SetActive(false);
        newCourseButton.gameObject.SetActive(true);
        cantPlaceHoleLabel.gameObject.SetActive(false);
        
        ResetShotUI();

        if (monitorShotCoroutine != null)
        {
            StopCoroutine(monitorShotCoroutine);
        }

        foreach (Ball ball in oldBalls)
        {
            if (ball != null)
            {
                DestroyBall(ball);
            }
        }

        oldBalls.Clear();
        DestroyBall(activeBall);

        RespawnHole();

        if (hole == null)
        {
            cantPlaceHoleLabel.gameObject.SetActive(true);
            return;
        }
        
        SpawnNewBall();
        activeBall.OnFacadeCollision += OnActiveBallCollision;
        tapToShootLabel.gameObject.SetActive(true);

        if (obstacle != null)
        {
            Destroy(obstacle.gameObject);
            obstacle = null;
        }

        lastCourseResetTime = Time.timeSinceLevelLoad;
        OnNewCourse?.Invoke();

        foreach (Facade facade in facadesMonitor.Facades)
        {
            facade.SetTransparent();

            if (facade.GeometryType == StreetscapeGeometryType.Terrain)
            {
                facade.HolePosition = hole.transform.position;
                facade.HoleRadius = hole.Radius;
            }

            facade.Pulse();
        }
    }

    private void ResetShotUI()
    {
        tapToShootLabel.gameObject.SetActive(false);
        holeHitLabel.gameObject.SetActive(false);
        tryAgainLabel.gameObject.SetActive(false);
        cantPlaceObstacleLabel.gameObject.SetActive(false);
        notBilliardsLabel.gameObject.SetActive(false);
        addObstacleButton.gameObject.SetActive(false);
        powerBar.sizeDelta = new Vector2(0f, powerBar.sizeDelta.y);
    }

    private void RespawnHole()
    {
        if (hole != null)
        {
            hole.RemoveBallInListener(OnBallIn);
            Destroy(hole.gameObject);
            hole = null;
            holeSet = false;
        }

        Vector3? holePosition = NewHolePosition(out Transform holeParent);

        if (!holePosition.HasValue)
        {
            Debug.LogWarning("Can't place the hole.");
            return;
        }

        // hole prefab is setup in such a way that we want the z axis to face the player
        Vector3 horizontalHolePosToCamera = Camera.main.transform.position - holePosition.Value;
        horizontalHolePosToCamera.y = 0f;
        Quaternion holeRotation = Quaternion.LookRotation(horizontalHolePosToCamera, Vector3.up);

        hole = Instantiate(holePrefab, holePosition.Value, holeRotation, holeParent);
        holeSet = true;
        hole.AddBallInListener(OnBallIn);
        Facade groundFacade = null;
        if (holeParent != null)
        {
            groundFacade = holeParent.GetComponent<Facade>();
        }

        hole.Ground = groundFacade;

        freshHole = true;
    }

    private Vector3? NewHolePosition(out Transform holeParent)
    {
        Vector3 horizontalForward = Camera.main.transform.forward;
        horizontalForward.y = 0f;

        // key - angle, value - distance hit
        List<KeyValuePair<float, float>> availableDistances = new();
        for (float currentAngle = -settings.holeSpawnAngleMax;
            currentAngle <= settings.holeSpawnAngleMax;
            currentAngle += holeAngleIncrement)
        {
            float availableDistance = float.PositiveInfinity;
            Vector3 currentDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * horizontalForward;

            if (Physics.SphereCast(Camera.main.transform.position, settings.holeRadius + settings.holeSpawnPadding, currentDirection, out RaycastHit hitInfo,
                float.PositiveInfinity, LayerMask.GetMask("Facade")))
            {
                availableDistance = hitInfo.distance;
            }

            if (availableDistance > settings.holeSpawnDistanceMin)
            {
                KeyValuePair<float, float> newDistance = new(currentAngle, availableDistance);
                availableDistances.Add(newDistance);
            }
        }

        if (availableDistances.Count == 0)
        {
            holeParent = null;
            return null;
        }

        var distanceData = availableDistances[Random.Range(0, availableDistances.Count)];
        float angle = distanceData.Key;
        float distance = Random.Range(settings.holeSpawnDistanceMin,
            Mathf.Min(distanceData.Value, settings.holeSpawnDistanceMax));
        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * horizontalForward;
        Vector3 holePositionXZ = Camera.main.transform.position + direction.normalized * distance;

        Vector3 holePosition = holePositionXZ + Vector3.down * sessionOrigin.CameraYOffset;
        Debug.Log($"Hole pos: {holePosition}");
        Vector3 rayStart = holePosition + Vector3.up * groundRayLength;
        Vector3 rayDirection = Vector3.down;
        float rayLength = groundRayLength * 2f;
        Ray ray = new Ray(rayStart, rayDirection);
        holeParent = null;
        if (Physics.Raycast(ray, out RaycastHit hitInfo2, rayLength, LayerMask.GetMask("Ground")))
        {
            holePosition = hitInfo2.point;
            holeParent = hitInfo2.collider.transform;
            Debug.Log($"Ray hit at {hitInfo2.point}, collider {hitInfo2.collider.name}, holePos: {holePosition}");
        }

        return holePosition;
    }

    private void SpawnNewBall()
    {
        if (activeBall != null)
        {
            oldBalls.Add(activeBall);
        }

        if (freshHole)
        {
            Vector3 ballPosition = NewBallPosition(out Transform parent);
            
            if (ballPositionTransform == null)
            {
                ballPositionTransform = new GameObject("Ball Position").GetComponent<Transform>();
            }

            ballPositionTransform.position = ballPosition;
            ballPositionTransform.parent = parent;
        }

        Vector3 ballSpawnPosition =
            ballPositionTransform.position + Vector3.up * settings.ballSpawnHeight;
        activeBall = Instantiate(ballPrefab, ballSpawnPosition, Quaternion.identity);
        activeBallRigidbody = activeBall.GetComponent<Rigidbody>();

        activeBallRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ |
                                          RigidbodyConstraints.FreezeRotation;

        shotFired = false;

        Debug.Log(
            $"Ball spawned at {activeBall.transform.position}, user is at {Camera.main.transform.position}, looking at {Camera.main.transform.forward}");
    }

    private void DestroyBall(Ball ball)
    {
        if (ball != null)
        {
            ball.OnFacadeCollision -= OnActiveBallCollision;
            Destroy(ball.gameObject);
        }
    }

    private Vector3 NewBallPosition(out Transform parent)
    {
        Vector3 horizontalForward = Camera.main.transform.forward;
        horizontalForward.y = 0f;
        Vector3 position = Camera.main.transform.position + horizontalForward * settings.ballSpawnDistance;
        Vector3 rayStart = position + Vector3.up * groundRayLength;
        Vector3 rayDirection = Vector3.down;
        float rayLength = groundRayLength * 2f + sessionOrigin.CameraYOffset;
        Ray ray = new Ray(rayStart, rayDirection);
        parent = null;
        if (Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, LayerMask.GetMask("Ground")))
        {
            position = hitInfo.point;
            parent = hitInfo.transform;
        }

        return position;
    }

    private IEnumerator AddObstacle(Action<bool> onComplete)
    {
        float holeDistance = Vector3.Distance(hole.transform.position, ballPositionTransform.position);
        Vector3 holeDirection = hole.transform.position - ballPositionTransform.position;
        float obstacleDistance = Random.Range(settings.obstacleBallMargin, holeDistance - settings.obstacleHoleMargin);
        Vector3 obstaclePosition = ballPositionTransform.position + holeDirection.normalized * obstacleDistance;

        Debug.Log($"Hole distance: {holeDistance}, obstacle distance: {obstacleDistance}");

        Vector3 rayStart = obstaclePosition + Vector3.up * groundRayLength;
        Vector3 rayDirection = Vector3.down;
        float rayLength = groundRayLength * 2f;
        Ray ray = new Ray(rayStart, rayDirection);
        Transform obstacleParent = null;
        if (Physics.Raycast(ray, out RaycastHit hitInfo, rayLength, LayerMask.GetMask("Ground")))
        {
            obstaclePosition = hitInfo.point;
            obstacleParent = hitInfo.transform;
        }

        obstacle = Instantiate(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)], obstaclePosition,
            Quaternion.identity, obstacleParent);
        obstacle.Renderer.enabled = false;

        yield return null;

        Vector3 horizontalForward = holeDirection;
        horizontalForward.y = 0f;
        float ballRadius = settings.ballDiameter * 0.5f;
        Vector3 ballCenterPosition = ballPositionTransform.position + ballRadius * Vector3.up;

        bool bouncePathFound = false;

        int raycastsLeft = raycastsPerFrame;

        for (float currentAngle = -90f; currentAngle <= 90; currentAngle += obstacleAngleIncrement)
        {
            Vector3 currentDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * horizontalForward;

            rayStart = ballCenterPosition;
            rayDirection = currentDirection;
            rayLength = obstacleRayMaxDistance;

            if (Physics.SphereCast(rayStart, ballRadius, rayDirection, out hitInfo,
                rayLength, LayerMask.GetMask("Facade", "Obstacle")))
            {
                if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle")) continue;

                Vector3 facadeHitPoint = hitInfo.point;
                Transform facadeHit = hitInfo.transform;

                rayStart += rayDirection.normalized * hitInfo.distance;
                float angle = Vector3.Angle(hitInfo.normal, -rayDirection);
                rayDirection = Vector3.RotateTowards(hitInfo.normal, -rayDirection, -Mathf.Deg2Rad * angle,
                    float.MaxValue);

                if (Physics.SphereCast(rayStart, ballRadius, rayDirection, out hitInfo,
                    rayLength, LayerMask.GetMask("Hole", "Obstacle")))
                {
                    if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Obstacle")) continue;

                    bouncePathFound = true;
                    break;
                }

                raycastsLeft--;
            }

            raycastsLeft--;

            if (raycastsLeft <= 0)
            {
                yield return null;
                raycastsLeft = raycastsPerFrame;
            }
        }

        if (bouncePathFound)
        {
            obstacle.Renderer.enabled = true;
            onComplete?.Invoke(true);
        }
        else
        {
            Debug.Log("Couldn't place obstacle");
            Destroy(obstacle.gameObject);
            obstacle = null;
            onComplete?.Invoke(false);
        }
    }

    public void OnAddObstacleClicked()
    {
        addObstacleButton.interactable = false;
        holeHitLabel.gameObject.SetActive(false);
        StartCoroutine(AddObstacle(OnObstacleAddFinished));
    }

    private void OnObstacleAddFinished(bool success)
    {
        if (success)
        {
            addObstacleButton.gameObject.SetActive(false);
            SpawnNewBall();
        }
        else
        {
            cantPlaceObstacleLabel.gameObject.SetActive(true);
            Invoke(nameof(HideCantPlaceObstacle), messageReadTime);
        }

        addObstacleButton.interactable = true;
    }

    private void HideCantPlaceObstacle()
    {
        cantPlaceObstacleLabel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (shotCharging)
        {
            shootForce += Time.deltaTime * settings.forcePerSecond * 100f;
            float powerBarWidth = shootForce / powerBarMaxPower * powerBarMaxWidth;
            powerBar.sizeDelta = new Vector2(powerBarWidth, powerBar.sizeDelta.y);
            Debug.Log($"shoot force: {shootForce}, power bar width: {powerBarWidth}");
        }

        if (holeSet)
        {
            foreach (Facade facade in facadesMonitor.Facades.Where(facade => facade.GeometryType == StreetscapeGeometryType.Terrain))
            {
                facade.HolePosition = hole.transform.position;
                facade.HoleRadius = hole.Radius;
            }
        }
    }

    public void OnTouchDown()
    {
        if (!shotFired)
        {
            shotCharging = true;
            Debug.Log("Launch touch start");
            shootForce = 0f;
            powerBar.gameObject.SetActive(true);
            powerBar.sizeDelta = new Vector2(0f, powerBar.sizeDelta.y);
            tapToShootLabel.gameObject.SetActive(false);
            if (shotChargingAudio.clip != null)
            {
                shotChargingAudio.time = 0.2f;
                shotChargingAudio.Play();
            }
        }
    }

    public void OnTouchUp()
    {
        if (shotCharging)
        {
            Vector3 direction = Vector3.ProjectOnPlane(activeBall.transform.position - Camera.main.transform.position,
                Vector3.up);
            direction.y = 0f;
            Debug.Log($"Shooting, force: {shootForce}, forward: {direction}");
            activeBallRigidbody.constraints = RigidbodyConstraints.None;
            activeBallRigidbody.AddForce(direction.normalized * shootForce, ForceMode.Impulse);
            shootForce = 0f;
            powerBar.gameObject.SetActive(false);
            shotCharging = false;
            freshHole = false;
            shotFired = true;
            shotChargingAudio.Stop();
            activeBall.OnShotHit();

            monitorShotCoroutine = StartCoroutine(MonitorShot());
        }
    }

    private void OnBallIn(Ball ball)
    {
        if (ball == activeBall)
        {
            if (monitorShotCoroutine != null)
            {
                StopCoroutine(monitorShotCoroutine);
            }

            ball.OnFacadeCollision -= OnActiveBallCollision;
            holeHitLabel.gameObject.SetActive(true);
            if (holeHitSound != null)
            {
                if (music != null)
                {
                    music.volume *= musicLowVolumePercent;
                }

                if (holeHitSound != null && holeHitSound.clip != null)
                {
                    holeHitSound.Play();
                }

                confettiParticles.Play();
                StartCoroutine(ResumeMusicCoroutine());
            }

            Invoke(nameof(HoleHitNextChallenge), messageReadTime);
        }
        else
        {
            notBilliardsLabel.gameObject.SetActive(true);
            Invoke(nameof(HideNotBilliardsLabel), messageReadTime);
        }
    }

    private IEnumerator ResumeMusicCoroutine()
    {
        yield return new WaitWhile(() => holeHitSound.isPlaying);
        music.volume /= musicLowVolumePercent;
    }

    private void HoleHitNextChallenge()
    {
        if (obstacle == null)
        {
            addObstacleButton.gameObject.SetActive(true);
        }
        else
        {
            NewCourse();
        }
    }

    private void HideHoleHitLabel()
    {
        holeHitLabel.gameObject.SetActive(false);
    }

    private void HideNotBilliardsLabel()
    {
        notBilliardsLabel.gameObject.SetActive(false);
    }

    private void OnShotMissed()
    {
        tryAgainLabel.gameObject.SetActive(true);
        SpawnNewBall();
        activeBall.OnFacadeCollision += OnActiveBallCollision;
    }

    private void OnActiveBallCollision(Facade facade)
    {
        // since we spawn the ball above terrain
        // the first collision with the terrain
        // is when it drops and is ready for the shot
        if (facade.GeometryType == StreetscapeGeometryType.Terrain)
        {
            activeBall.OnFacadeCollision -= OnActiveBallCollision;
            tryAgainLabel.gameObject.SetActive(false);
            tapToShootLabel.gameObject.SetActive(true);
        }
    }

    private IEnumerator MonitorShot()
    {
        float startTime = Time.timeSinceLevelLoad;
        yield return new WaitForSeconds(shotMinDuration);
        while (Time.timeSinceLevelLoad - startTime < shotMaxDuration && CanActiveShotHit())
        {
            yield return null;
        }

        OnShotMissed();
    }

    private bool CanActiveShotHit()
    {
        if (hole.IsOnTop(activeBall)) return true;

        Vector3 ballToHole = hole.Origin - activeBallRigidbody.position;
        Vector3 projectedVelocity = Vector3.Project(activeBallRigidbody.velocity, ballToHole);
        float maxDistance = activeBallRigidbody.velocity.magnitude * shotPredictionTime;

        // since we expect the ball to slow down as time passes
        // if it can't reach hole or something to bounce off at current speed
        // then we determine the shot can not hit
        if (Vector3.Dot(projectedVelocity, ballToHole) > 0f)
        {
            // moving towards the hole
            // is there enough speed to reach it?
            return ballToHole.magnitude - hole.Radius < maxDistance;
        }
        else
        {
            // moving away from the hole
            // can the ball bounce of something back to the hole?
            if (!Physics.SphereCast(activeBallRigidbody.position, activeBall.Radius, activeBallRigidbody.velocity,
                out RaycastHit _, maxDistance, LayerMask.GetMask("Facade", "Ball", "Obstacle", "Hole")))
            {
                // there's nothing to bounce of
                return false;
            }
            else
            {
                // there is something to bounce off
                // is there enough speed to reach the hole after the bounce?
                return ballToHole.magnitude - hole.Radius < maxDistance;
            }
        }
    }

    public bool PlayerActive => shotCharging || shotFired ||
                                Time.timeSinceLevelLoad - lastCourseResetTime < playerActiveLingerTime;

    public float MessageReadTime => messageReadTime;

    public bool NotBilliardsShown => notBilliardsLabel.gameObject.activeInHierarchy;

    private void OnDestroy()
    {
        facadesMonitor.OnFacadeAdded -= OnFacadeAdded;
    }
}