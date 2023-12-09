﻿using ItemChanger.Extensions;

namespace ItemChanger.Modules
{
    /// <summary>
    /// Module for changing the darkness level of a scene. Permitted values are -1, 0 (normal), 1, 2 (fully dark)
    /// <br/>Darkness regions in modified scenes will be disabled.
    /// </summary>
    public class DarknessLevelEditModule : Module
    {
        public Dictionary<string, int> darknessLevelsByScene = new();

        public override void Initialize()
        {
            On.SceneManager.Start += OnSceneManagerStart;
            Events.AddFsmEdit(new("Darkness Region"), DisableDarknessRegion);
        }

        public override void Unload()
        {
            On.SceneManager.Start -= OnSceneManagerStart;
            Events.RemoveFsmEdit(new("Darkness Region"), DisableDarknessRegion);
        }

        private void OnSceneManagerStart(On.SceneManager.orig_Start orig, SceneManager self)
        {
            if (darknessLevelsByScene.TryGetValue(self.gameObject.scene.name, out int darknessLevel))
            {
                self.darknessLevel = darknessLevel;
            }
            orig(self);
        }

        private void DisableDarknessRegion(PlayMakerFSM fsm)
        {
            if (darknessLevelsByScene.ContainsKey(fsm.gameObject.scene.name)) fsm.GetState("Init").ClearTransitions();
        }
    }
}
