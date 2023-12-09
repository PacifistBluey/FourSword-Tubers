﻿using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
#pragma warning disable IDE1006 // Naming Styles

namespace ItemChanger.Modules
{
    /// <summary>
    /// Module which implements the split claw custom skills.
    /// </summary>
    public class SplitClaw : Module
    {
        public bool hasWalljumpLeft { get; set; }
        public bool hasWalljumpRight { get; set; }
        public ClothFungalEncounterLeaveCondition ClothFungalEncounterHandling = ClothFungalEncounterLeaveCondition.DisappearAfterFullClaw;
        [Newtonsoft.Json.JsonIgnore] public bool hasWalljumpAny => hasWalljumpLeft ^ hasWalljumpRight || PlayerData.instance.GetBoolInternal(nameof(PlayerData.hasWalljump));
        [Newtonsoft.Json.JsonIgnore] public bool hasWalljumpBoth => hasWalljumpLeft && hasWalljumpRight || PlayerData.instance.GetBoolInternal(nameof(PlayerData.hasWalljump));

        public override void Initialize()
        {
            Events.AddFsmEdit(new("Equipment", "Build Equipment List"), EditInventory);
            Modding.ModHooks.GetPlayerBoolHook += SkillBoolGetOverride;
            Modding.ModHooks.SetPlayerBoolHook += SkillBoolSetOverride;
            Events.AddLanguageEdit(new("UI", "INV_NAME_WALLJUMP"), EditClawName);
            Events.AddLanguageEdit(new("UI", "INV_DESC_WALLJUMP"), EditClawDesc);
            Events.AddFsmEdit(SceneNames.Fungus2_09, new("Cloth NPC 1", "Destroy"), EditClothFungalEncounter);
        }

        public override void Unload()
        {
            Events.RemoveFsmEdit(new("Equipment", "Build Equipment List"), EditInventory);
            Modding.ModHooks.GetPlayerBoolHook -= SkillBoolGetOverride;
            Modding.ModHooks.SetPlayerBoolHook -= SkillBoolSetOverride;
            Events.RemoveLanguageEdit(new("UI", "INV_NAME_WALLJUMP"), EditClawName);
            Events.RemoveLanguageEdit(new("UI", "INV_DESC_WALLJUMP"), EditClawDesc);
            Events.RemoveFsmEdit(SceneNames.Fungus2_09, new("Cloth NPC 1", "Destroy"), EditClothFungalEncounter);
        }

        private void EditInventory(PlayMakerFSM fsm)
        {
            fsm.GetState("Walljump").GetFirstActionOfType<PlayerDataBoolTest>().boolName.Value = nameof(hasWalljumpAny);
        }

        private bool SkillBoolGetOverride(string boolName, bool value)
        {
            return boolName switch
            {
                nameof(hasWalljumpLeft) => hasWalljumpLeft,
                nameof(hasWalljumpRight) => hasWalljumpRight,
                nameof(PlayerData.hasWalljump) => 
                       HeroController.instance.touchingWallL && hasWalljumpLeft && !hasWalljumpRight
                    || HeroController.instance.touchingWallR && hasWalljumpRight && !hasWalljumpLeft
                    || value,
                nameof(hasWalljumpAny) => hasWalljumpAny,
                nameof(hasWalljumpBoth) => hasWalljumpBoth,
                _ => value,
            };
        }

        private bool SkillBoolSetOverride(string boolName, bool value)
        {
            switch (boolName)
            {
                // bools for left and right claw
                // If the player has one piece and gets the other, then we give them the full mantis claw. This allows the split claw to work with other mods more easily, 
                // unless of course they have only one piece.
                case nameof(hasWalljumpLeft):
                    hasWalljumpLeft = value;
                    goto BoolChanged;

                case nameof(hasWalljumpRight):
                    hasWalljumpRight = value;
                    goto BoolChanged;

                BoolChanged:
                    if (hasWalljumpLeft && hasWalljumpRight)
                    {
                        PlayerData.instance.SetBool(nameof(PlayerData.hasWalljump), true);
                        PlayerData.instance.SetBool(nameof(PlayerData.canWallJump), true);
                    }
                    break;
            }
            return value;
        }

        private void EditClawName(ref string value)
        {
            if (hasWalljumpLeft && !hasWalljumpRight)
            {
                value = Language.Language.Get("ITEMCHANGER_NAME_LEFT_CLAW", "UI");
            }
            else if (!hasWalljumpLeft && hasWalljumpRight)
            {
                value = Language.Language.Get("ITEMCHANGER_NAME_RIGHT_CLAW", "UI");
            }
        }

        private void EditClawDesc(ref string value)
        {
            if (hasWalljumpLeft && !hasWalljumpRight)
            {
                value = Language.Language.Get("ITEMCHANGER_SHOP_DESC_LEFT_CLAW", "UI");
            }
            else if (!hasWalljumpLeft && hasWalljumpRight)
            {
                value = Language.Language.Get("ITEMCHANGER_SHOP_DESC_RIGHT_CLAW", "UI");
            }
        }

        public enum ClothFungalEncounterLeaveCondition
        {
            Vanilla,
            DisappearAfterFullClaw = Vanilla,
            DisappearAfterEitherClaw,
            DisappearAfterLeftClaw,
            DisappearAfterRightClaw,
            NoClawDependence,
        }
        private void EditClothFungalEncounter(PlayMakerFSM fsm)
        {
            FsmState check = fsm.GetState("Check");
            PlayerDataBoolTest pdbt = check.GetFirstActionOfType<PlayerDataBoolTest>();
            switch (ClothFungalEncounterHandling)
            {
                case ClothFungalEncounterLeaveCondition.Vanilla:
                    pdbt.boolName = nameof(hasWalljumpBoth);
                    break;
                case ClothFungalEncounterLeaveCondition.DisappearAfterEitherClaw:
                    pdbt.boolName = nameof(hasWalljumpAny);
                    break;
                case ClothFungalEncounterLeaveCondition.DisappearAfterLeftClaw:
                    pdbt.boolName = nameof(hasWalljumpLeft);
                    break;
                case ClothFungalEncounterLeaveCondition.DisappearAfterRightClaw:
                    pdbt.boolName = nameof(hasWalljumpRight);
                    break;
                case ClothFungalEncounterLeaveCondition.NoClawDependence:
                    check.ClearTransitions();
                    break;
            }
        }
    }
}
