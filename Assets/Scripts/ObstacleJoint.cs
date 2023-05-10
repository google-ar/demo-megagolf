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
using UnityEngine.Serialization;

public class ObstacleJoint : MonoBehaviour
{
    [FormerlySerializedAs("spring")] [SerializeField]
    private float rotationSpring;
    
    [FormerlySerializedAs("damper")] [SerializeField]
    private float rotationDamper;

    [SerializeField]
    private float positionSpring;
    
    [SerializeField]
    private float positionDamper;
    
    private new Rigidbody rigidbody;

    private Vector3 anchorPosition;
    
    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        anchorPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // rotation
        Vector3 springTorque = rotationSpring * Vector3.Cross(rigidbody.transform.up, Vector3.up);
        Vector3 dampTorque = rotationDamper * -rigidbody.angularVelocity;
        rigidbody.AddTorque(springTorque + dampTorque, ForceMode.Force);
        
        // translation
        Vector3 springForce = positionSpring * (anchorPosition - transform.position);
        Vector3 dampVelocity = positionDamper * -rigidbody.velocity;
        
        rigidbody.AddForce(springForce + dampVelocity, ForceMode.Force);
    }
}
