﻿using System;
using UnityEngine;

namespace C2M2.Simulation
{
    using Utils;
    using Interaction.VR;
    /// <summary>
    /// Simulation of type Vector3[] for simulating positional fields
    /// </summary>
    public abstract class PositionFieldSimulation : Simulation<Vector3[], Transform[], VRRaycastableColliders, VRGrabbableColliders>
    {
        protected override void OnAwake(Transform[] viz)
        {
            if (!dryRun)
            {
                Vector3[] pos = new Vector3[viz.Length];

                // Add custom grabbable here
                // gameObject.AddComponent<VRGrabbableColliders>();

                Collider[] colliders = new Collider[viz.Length];
                for (int i = 0; i < viz.Length; i++)
                {
                    colliders[i] = viz[i].GetComponent<Collider>();
                }

                VRRaycastableColliders raycastable = gameObject.AddComponent<VRRaycastableColliders>();
                raycastable.SetSource(colliders);

            }
        }

        protected override void UpdateVisualization(in Vector3[] simulationValues)
        {
            for (int i = 0; i < simulationValues.Length; i++)
            {
                viz[i].localPosition = simulationValues[i];
            }

            UpdateVisChild(simulationValues);
        }
        /// <summary>
        /// Allow derived classes to implement custom visualization features
        /// </summary>
        protected virtual void UpdateVisChild(in Vector3[] simulationValues) { }
    }
}
