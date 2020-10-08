﻿using System;
using System.Collections.Generic;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using C2M2.Utils.MeshUtils;
using Grid = C2M2.NeuronalDynamics.UGX.Grid;
using C2M2.NeuronalDynamics.UGX;
using C2M2.Visualization;
using System.Collections;
using C2M2.Utils;

namespace C2M2.NeuronalDynamics.Interaction
{
    public class NeuronClamp : MonoBehaviour
    {
        public bool clampLive { get; private set; } = false;
 
        public double clampPower = 55;

        public int nearestVert = -1;

        public Material activeMaterial = null;
        public Material inactiveMaterial = null;

        public NeuronSimulation1D activeTarget = null;

        private Tuple<int, double>[] newValues = null;

        private Vector3 lastLocalPos;
        private Vector3 origScale;

        private Vector3 simLastPos;
        private bool ClampMoved { get { return !lastLocalPos.Equals(transform.localPosition); } }
        private bool use1DVerts = true;
        private OVRGrabbable grabbable;
        private MeshRenderer mr;
        private MeshFilter mf;
        private Bounds bounds;
        private Vector3 LocalExtents { get { return transform.localScale / 2; } }
        private Vector3 posFocus = Vector3.zero;
        private Color32 clampCol = Color.black;
        public Color32 ClampCol
        {
            get { return clampCol; }
            private set
            {
                clampCol = value;
                mr.material.SetColor("_Color", clampCol);
            }
        }
        private LUTGradient gradientLUT = null;

        float currentVisualizationScale = 1;

        private void VisualInflationChangeHandler(double newInflation)
        {
            UpdateScale((float)newInflation);
        }

        private void Awake()
        {
            mr = GetComponent<MeshRenderer>();
            mf = GetComponent<MeshFilter>();

            origScale = transform.parent.localScale;
            grabbable = GetComponentInParent<OVRGrabbable>();
        }

        private void FixedUpdate()
        {
            if(activeTarget != null)
            {
                if (clampLive)
                {
                    activeTarget.Set1DValues(newValues);
                  
                    ClampCol = gradientLUT.EvaluateUnscaled((float)clampPower);
                }
            }
        }

        public void ActivateClamp()
        {
            clampLive = true;

            if (activeMaterial != null)
            {
                mr.material = activeMaterial;
            }
        }
        public void DeactivateClamp()
        {
            clampLive = false;
            if (inactiveMaterial != null)
            {
                mr.material = inactiveMaterial;
            }
        }

        public void ToggleClamp()
        {
            if (clampLive) DeactivateClamp();
            else ActivateClamp();
        }

        // Scan new targets for ND simulations
        private void OnTriggerEnter(Collider other)
        {
            if (activeTarget == null)
            {
                NeuronSimulation1D simulation = other.GetComponentInParent<NeuronSimulation1D>() ?? other.GetComponent<NeuronSimulation1D>();
                if (simulation != null)
                {
                    ReportSimulation(simulation, transform.parent.position);
                    //GetComponent<Rigidbody>().isKinematic = true;
                }
            }
        }

        public NeuronSimulation1D ReportSimulation(NeuronSimulation1D simulation, Vector3 contactPoint)
        {
            if (activeTarget == null)
            {
                activeTarget = simulation;

                transform.parent.parent = simulation.transform;

                int clampIndex = GetNearestPoint(activeTarget, contactPoint);

                Tuple<int, double> newVal = new Tuple<int, double>(clampIndex, clampPower);
                newValues = new Tuple<int, double>[] { newVal };

                NeuronCell.NodeData clampCellNodeData = new NeuronCell(simulation.getGrid1D()).nodeData[clampIndex];
                SetScale(activeTarget, clampCellNodeData);
                SetRotation(activeTarget, clampCellNodeData);

                simulation.OnVisualInflationChange += VisualInflationChangeHandler;

                // Change object layer to Raycast so the clamp does not continue to interact physically with the simulation
                gameObject.layer = LayerMask.NameToLayer("Raycast");
                Destroy(gameObject.GetComponent<Rigidbody>());

                gradientLUT = activeTarget.GetComponent<LUTGradient>();
            }

            return activeTarget;
        }

        private int GetNearestPoint(NeuronSimulation1D simulation, Vector3 worldPoint)
        {
            // Translate contact point to local space
            Vector3 localPoint = activeTarget.transform.InverseTransformPoint(worldPoint);

            Vector3[] verts;
            if (use1DVerts)
            {
                verts = simulation.Verts1D;
            }
            else
            {
                MeshFilter mf = simulation.GetComponent<MeshFilter>();
                if (mf == null) return -1;
                Mesh mesh = mf.sharedMesh ?? mf.mesh;
                if (mesh == null) return -1;

                verts = mesh.vertices;
            }

            int nearestVertInd = -1;
            Vector3 nearestPos = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            float nearestDist = float.PositiveInfinity;
            for (int i = 0; i < verts.Length; i++)
            {
                float dist = Vector3.Distance(localPoint, verts[i]);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestVertInd = i;
                    nearestPos = verts[i];
                }
            }

            nearestVert = nearestVertInd;

            posFocus = nearestPos;

            transform.parent.localPosition = posFocus;

            transform.parent.name = "AttachedNeuronClamp" + nearestVertInd;
            return nearestVertInd;
        }

        [Tooltip("Hold down a raycast for this many frames in order to destroy a clamp")]
        public int destroyCount = 50;
        int holdCount = 0;
        /// <summary>
        /// If the user holds a raycast down for X seconds on a clamp, it should destroy the clamp
        /// </summary>
        public void ListenForDestroy()
        {
            holdCount++;
            if(holdCount == destroyCount)
            {
                Debug.Log("Destroying " + transform.parent.name);
                Destroy(transform.parent.gameObject);
            }
        }
        public void ResetHoldCount()
        {
            holdCount = 0;
        }

        private void SetScale(NeuronSimulation1D simulation, NeuronCell.NodeData cellNodeData)
        {
            currentVisualizationScale = (float) simulation.VisualInflation;

            float radiuScalarRatio = 1.5f;

            float heightScalarRatio = 7.5f;

            double dendriteDiameter = cellNodeData.nodeRadius * 2;

            float radiusScalingValue = (float)(radiuScalarRatio * dendriteDiameter * currentVisualizationScale);
            transform.parent.localScale = new Vector3(radiusScalingValue, radiusScalingValue, heightScalarRatio);
        }

        public void UpdateScale(float newScale)
        {
            if (this != null)
            {
                float modifiedScale = newScale/currentVisualizationScale;
                Vector3 tempVector = transform.parent.localScale;
                tempVector.x *= modifiedScale;
                tempVector.y *= modifiedScale;
                transform.parent.localScale = tempVector;
                currentVisualizationScale = newScale;
            }

        }

        public void SetRotation(NeuronSimulation1D simulation, NeuronCell.NodeData cellNodeData)
        {
            List<int> neighbors = cellNodeData.neighborIDs;
            List<Vector3> neighborVectors = new List<Vector3>();
            foreach (int neighbor in neighbors)
            {
                neighborVectors.Add(simulation.Verts1D[neighbor]);
            }
            if (neighborVectors.Count == 2)
            {
                transform.parent.localRotation = Quaternion.LookRotation(neighborVectors[0] - neighborVectors[1]);
            }
            else if (neighborVectors.Count > 0)
            {
                transform.parent.localRotation = Quaternion.LookRotation(simulation.Verts1D[cellNodeData.pid] - transform.parent.localPosition);
            }
        }
    }
}
