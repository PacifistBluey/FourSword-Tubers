﻿using HutongGames.PlayMaker.Actions;
using ItemChanger.FsmStateActions;
using ItemChanger.Extensions;

namespace ItemChanger.Locations.SpecialLocations
{
    /// <summary>
    /// FsmObjectLocation which replaces Vengeful Spirit and disables the Ancestral Mound trap sequence.
    /// </summary>
    public class VengefulSpiritLocation : FsmObjectLocation
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            Events.AddFsmEdit(UnsafeSceneName, new("Shaman Meeting", "Conversation Control"), EditShamanConvo);
            Events.AddFsmEdit(UnsafeSceneName, new("Shaman Trapped", "Conversation Control"), Destroy);
            Events.AddFsmEdit(UnsafeSceneName, new("Shaman Killed Blocker", "Conversation Control"), Destroy);
            Events.AddFsmEdit(UnsafeSceneName, new("Bone Gate", "Bone Gate"), Destroy);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            Events.RemoveFsmEdit(UnsafeSceneName, new("Shaman Meeting", "Conversation Control"), EditShamanConvo);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Shaman Trapped", "Conversation Control"), Destroy);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Shaman Killed Blocker", "Conversation Control"), Destroy);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Bone Gate", "Bone Gate"), Destroy);
        }


        public override void PlaceContainer(GameObject obj, string containerType)
        {
            base.PlaceContainer(obj, containerType);
            if (PlayerData.instance.GetInt(nameof(PlayerData.shaman)) >= 1) obj.SetActive(true);
        }

        private void EditShamanConvo(PlayMakerFSM fsm)
        {
            FsmState checkActive = fsm.GetState("Check Active");
            FsmState checkSummoned = fsm.GetState("Check Summoned");
            FsmState spellAppear = fsm.GetState("Spell Appear");

            checkActive.ClearActions();
            checkSummoned.RemoveActionsOfType<FindChild>();
            checkSummoned.GetActionsOfType<ActivateGameObject>().First(a => a.gameObject.GameObject.Name == "Vengeful Spirit").recursive = false;
            spellAppear.GetActionsOfType<ActivateGameObject>().First(a => a.gameObject.GameObject.Name == "Vengeful Spirit").recursive = false;
            spellAppear.ReplaceAction(new Lambda(() => { }), 8); // this replaces a wait after the spawn animation and seems to prevent a freeze
        }

        private void Destroy(PlayMakerFSM fsm)
        {
            UObject.Destroy(fsm.gameObject);
        }
    }
}
