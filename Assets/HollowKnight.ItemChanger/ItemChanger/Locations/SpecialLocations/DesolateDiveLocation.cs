﻿using HutongGames.PlayMaker.Actions;
using ItemChanger.FsmStateActions;
using ItemChanger.Extensions;

namespace ItemChanger.Locations.SpecialLocations
{
    /// <summary>
    /// FsmObjectLocation with various changes to support being spawned from the Quake Pickup after Soul Master.
    /// </summary>
    public class DesolateDiveLocation : FsmObjectLocation
    {
        protected override void OnLoad()
        {
            base.OnLoad();
            Events.AddFsmEdit(UnsafeSceneName, new("Quake Pickup", "Pickup"), EditQuakePickup);
            Events.AddFsmEdit(UnsafeSceneName, new("BG Control"), EditBGControl);
            Events.AddFsmEdit(UnsafeSceneName, new("Destroy if Quake"), EditDestroyIfQuake);
        }

        protected override void OnUnload()
        {
            base.OnUnload();
            Events.RemoveFsmEdit(UnsafeSceneName, new("Quake Pickup", "Pickup"), EditQuakePickup);
            Events.RemoveFsmEdit(UnsafeSceneName, new("BG Control"), EditBGControl);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Destroy if Quake"), EditDestroyIfQuake);
        }

        private void EditQuakePickup(PlayMakerFSM fsm)
        {
            FsmState idle = fsm.GetState("Idle");
            idle.RemoveActionsOfType<IntCompare>();
        }

        private void EditDestroyIfQuake(PlayMakerFSM fsm)
        {
            FsmState check = fsm.GetState("Check");
            check.SetActions(
                new DelegateBoolTest(() => PlayerData.instance.GetBool(nameof(PlayerData.mageLordDefeated)), "DESTROY", null)
            );
        }

        private void EditBGControl(PlayMakerFSM fsm)
        {
            foreach (FsmState state in fsm.FsmStates)
            {
                if (state.Transitions.FirstOrDefault(t => t.EventName == "BG OPEN") is FsmTransition transition)
                {
                    state.AddTransition("QUAKE PICKUP START", transition.ToState);
                }
            }
        }
    }
}
