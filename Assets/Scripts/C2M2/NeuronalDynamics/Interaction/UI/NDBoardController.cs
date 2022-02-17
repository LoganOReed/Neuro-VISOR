﻿using System.Collections;
using UnityEngine;
using C2M2.NeuronalDynamics.Simulation;
using TMPro;
using UnityEngine.UI;

namespace C2M2.NeuronalDynamics.Interaction.UI
{
    public class NDBoardController : MonoBehaviour
    {
        public Color defaultCol = new Color(1f, 0.75f, 0f);
        public Color highlightCol = new Color(1f, 0.85f, 0.4f);
        public Color pressedCol = new Color(1f, 0.9f, 0.6f);
        public Color errorCol = Color.red;
        public Image[] defColTargets = new Image[0];
        public Image[] hiColTargets = new Image[0];
        public Image[] pressColTargets = new Image[0];
        public Image[] errColTargets = new Image[0];

        public GameObject defaultBackground;
        public GameObject minimizedBackground;


        private bool Minimized
        {
            get
            {
                return !defaultBackground.activeSelf;
            }
        }

        private void Start()
        {
            StartCoroutine(UpdateColRoutine(0.5f));
        }

        private void UpdateCols()
        {
            foreach (TextMeshProUGUI text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if(text != null) text.color = defaultCol;
            }
            foreach(Image i in defColTargets)
            {
                if(i != null) i.color = defaultCol;
            }
            foreach (Image i in hiColTargets)
            {
                if(i != null) i.color = highlightCol;
            }
            foreach (Image i in pressColTargets)
            {
                if(i != null) i.color = pressedCol;
            }
            foreach(Image i in errColTargets)
            {
                if(i != null) i.color = errorCol;
            }
        }

        IEnumerator UpdateColRoutine(float waitTime)
        {
            while (true)
            {
                UpdateCols();
                yield return new WaitForSeconds(waitTime);
            }
        }

        public void AddSimulation()
        {
            // Minimize control board if there is one
            MinimizeBoard(true);

            // Reactivate cell previewer
            GameObject cellPreviewer = GameManager.instance.cellPreviewer;

            cellPreviewer.SetActive(true);
        }

        public void CloseAllSimulations()
        {
            for(int i = GameManager.instance.activeSims.Count-1; i >= 0; i--)
            {
                CloseSimulation(i);
            }
        }

        public void CloseSimulation(int simIndex)
        {
            NDSimulation sim = (NDSimulation)GameManager.instance.activeSims[simIndex];
            if (sim != null)
            {
                GameManager.instance.activeSims.Remove(sim);

                // Destroy the cell
                Destroy(sim.gameObject);
                
                if (GameManager.instance.cellPreviewer != null)
                {
                    // Reenable the cell previewer
                    GameManager.instance.cellPreviewer.SetActive(true);

                    // Destroy this control panel
                    Destroy(transform.root.gameObject);
                }

                // Destroy ruler if no cells are left
                // TODO See NDSimulationLoader for note on ruler generation and removal improvement
                if (GameManager.instance.activeSims.Count == 0) Destroy(GameObject.Find("Ruler"));
            }
        }

        public void MinimizeBoard(bool minimize)
        {
            if (defaultBackground == null || minimizedBackground == null)
            {
                Debug.LogWarning("Can't find minimize targets");
                return;
            }
            defaultBackground.SetActive(!minimize);
            minimizedBackground.SetActive(minimize);
        }

        public void MinimizeToggle()
        {
            MinimizeBoard(!Minimized);
        }
    }
}