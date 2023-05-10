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

public class TriggerPublish : MonoBehaviour
{
    public event Action<bool, Collider> OnStateChanged;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OnTriggerEnter {gameObject.name}");
        OnStateChanged?.Invoke(true, other);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"OnTriggerExit {gameObject.name}");
        OnStateChanged?.Invoke(false, other);
    }
}
