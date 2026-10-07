using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace OttoEngine
{
    /// <summary>
    /// Steps through the engine's phases: the assembled engine, the four strokes (one animation each) and the running
    /// engine (a loop). Each phase is its own object with the animated parts, the gas effects and the engine sound;
    /// showing it (re)starts its animation, particles and sound. Keys: Space / Right arrow = next, R = reset.
    /// </summary>
    public class EngineShowcase : MonoBehaviour
    {
        [Serializable]
        public class Phase
        {
            public string title;
            [TextArea(2, 5)] public string description;
            public GameObject root;
        }

        [Tooltip("0 = assembled, 1-4 = intake, compression, power, exhaust, last = running.")]
        public Phase[] phases;
        public Text title, description, nextLabel;
        public Button next, run, reset;
        public OrbitCamera orbit;

        private int current = -1;

        private void Start()
        {
            next.onClick.AddListener(Next);
            run.onClick.AddListener(() => Show(phases.Length - 1));
            reset.onClick.AddListener(() =>
            {
                Show(0);
                if (orbit != null) orbit.ResetView();
            });
            Show(0);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame) Next();
            if (keyboard.rKey.wasPressedThisFrame) Show(0);
        }

        /// <summary>The next stroke; after the running engine it starts again with intake.</summary>
        public void Next() => Show(current >= phases.Length - 1 ? 1 : current + 1);

        private void Show(int index)
        {
            foreach (var phase in phases) phase.root.SetActive(false);
            phases[index].root.SetActive(true);                 // also restarts the phase when it is shown again
            current = index;
            title.text = phases[index].title;
            description.text = phases[index].description;
            var last = phases.Length - 1;
            nextLabel.text = index == 0 ? "Start: intake stroke" : index < last - 1 ? "Next stroke" :
                index == last - 1 ? "Let it run" : "Step through again";
            run.gameObject.SetActive(index != last);
        }
    }
}
