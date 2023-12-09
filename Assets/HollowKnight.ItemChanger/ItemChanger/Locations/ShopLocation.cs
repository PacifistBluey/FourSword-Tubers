﻿﻿using ItemChanger.Placements;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Components;
using ItemChanger.FsmStateActions;
using ItemChanger.Extensions;
using TMPro;
using ItemChanger.Internal;

namespace ItemChanger.Locations
{
    /// <summary>
    /// Location for modifying any of the vanilla shops.
    /// </summary>
    public class ShopLocation : AbstractLocation
    {
        /// <summary>
        /// The npc's objectName, for spawning items.
        /// </summary>
        public string? objectName;
        public string fsmName;

        /// <summary>
        /// If more than one placement modifies the same shop, the intersection of all default shop items are kept.
        /// </summary>
        public DefaultShopItems defaultShopItems;

        /// <summary>
        /// The strategy used to determine how costs in this shop should be displayed. By default, all geo costs
        /// are summed and all additional costs are displayed in grey text (as charm requirements in vanilla Salubra
        /// are displayed). This strategy is applied uniformly to all items.
        /// </summary>
        public ICostDisplayerSelectionStrategy costDisplayerSelectionStrategy = new SingleCostDisplayerSelectionStrategy() 
        { 
            CostDisplayer = new GeoCostDisplayer() 
        };

        /// <summary>
        /// If this field is set, the PlayerData requirement to appear in stock is applied to all items at this location in addition to any item-specific requirements.
        /// </summary>
        public string requiredPlayerDataBool = string.Empty;
        public bool dungDiscount;

        protected override void OnLoad()
        {
            Events.AddFsmEdit(UnsafeSceneName, new("Shop Region", "Shop Region"), EditShopRegion);
            Events.AddFsmEdit(UnsafeSceneName, new("Shop Menu", "shop_control"), EditShopControl);
            Events.AddFsmEdit(UnsafeSceneName, new("Item List", "Item List Control"), EditItemListControl);
            Events.AddFsmEdit(UnsafeSceneName, new("UI List", "Confirm Control"), EditConfirmControl);
            // brrr
            Events.AddFsmEdit(UnsafeSceneName, new("Item List", "Item List Control"), HastenItemListControl);
            Events.AddFsmEdit(UnsafeSceneName, new("Item List", "ui_list_getinput"), HastenUIListGetInput);
            Events.AddFsmEdit(UnsafeSceneName, new("UI List", "Confirm Control"), HastenConfirmControl);
            Events.AddFsmEdit(UnsafeSceneName, new("UI List", "ui_list"), HastenUIList);
            Events.AddFsmEdit(UnsafeSceneName, new("UI List", "ui_list_button_listen"), HastenUIListButtonListen);
        }

        protected override void OnUnload()
        {
            Events.RemoveFsmEdit(UnsafeSceneName, new("Shop Region", "Shop Region"), EditShopRegion);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Shop Menu", "shop_control"), EditShopControl);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Item List", "Item List Control"), EditItemListControl);
            Events.RemoveFsmEdit(UnsafeSceneName, new("UI List", "Confirm Control"), EditConfirmControl);
            
            Events.RemoveFsmEdit(UnsafeSceneName, new("Item List", "Item List Control"), HastenItemListControl);
            Events.RemoveFsmEdit(UnsafeSceneName, new("Item List", "ui_list_getinput"), HastenUIListGetInput);
            Events.RemoveFsmEdit(UnsafeSceneName, new("UI List", "Confirm Control"), HastenConfirmControl);
            Events.RemoveFsmEdit(UnsafeSceneName, new("UI List", "ui_list"), HastenUIList);
            Events.RemoveFsmEdit(UnsafeSceneName, new("UI List", "ui_list_button_listen"), HastenUIListButtonListen);
        }

        private void EditShopRegion(PlayMakerFSM fsm)
        {
            FsmState checkRelics = fsm.GetState("Check Relics");
            bool hasBeenEdited = checkRelics.GetActionsOfType<DelegateBoolTest>().Any();
            if (hasBeenEdited)
            {
                return;
            }

            // spoof having relics if any items are available
            bool ItemsInStock()
            {
                ShopMenuStock shop = fsm.FsmVariables.FindFsmGameObject("Shop Object").Value.GetComponent<ShopMenuStock>();
                return shop.StockLeft();
            }
            IntCompare relicComparison = checkRelics.GetFirstActionOfType<IntCompare>();
            checkRelics.ReplaceAction(new DelegateBoolTest(ItemsInStock, "HAS RELIC", "NO RELIC"),
                checkRelics.Actions.IndexOf(relicComparison));
        }

        /// <summary>
        /// Change how the shop stock is constructed.
        /// </summary>
        protected void EditShopControl(PlayMakerFSM fsm)
        {
            ShopMenuStock shop = fsm.gameObject.GetComponent<ShopMenuStock>();
            GameObject itemPrefab = ObjectCache.ShopItem;

            shop.stock = ((IShopPlacement)Placement).GetNewStock(shop.stock, itemPrefab);
            if (shop.stockAlt != null)
            {
                shop.stockAlt = ((IShopPlacement)Placement).GetNewAltStock(shop.stock, shop.stockAlt, itemPrefab);
            }

            // apparently in vanilla lemm cannot go out of stock!
            // make sure to route his "no stock" convo to his "no relic" convo
            FsmState chooseNoStockConvo = fsm.GetState("Choose Convo");
            bool hasBeenEdited = chooseNoStockConvo.GetTransition(4) != null;
            if (hasBeenEdited)
            {
                return;
            }
            chooseNoStockConvo.AddTransition("FINISHED", "Relic Dealer");

            FsmState checkRelics = fsm.GetState("Check Relics");
            // Instead of doing an actual comparison for relic count, unconditionally route to
            // relic counting states for Lemm, allowing shop to be auto-closed if emptied.
            // Lemm also checks for stock in the overworld before allowing the shop to open.
            IntCompare relicComparison = checkRelics.GetFirstActionOfType<IntCompare>();
            checkRelics.ReplaceAction(new Lambda(() => fsm.SendEvent("HAS RELIC")),
                checkRelics.Actions.IndexOf(relicComparison));
        }

        /// <summary>
        /// Change how the shop stock is presented.
        /// </summary>
        /// <param name="fsm"></param>
        protected void EditItemListControl(PlayMakerFSM fsm)
        {
            FsmState init = fsm.GetState("Init");
            
            bool hasBeenEdited = init.GetActionsOfType<Lambda>().Any(); // for cases like sly, sly key, only one placement needs to edit the shop functionality
            if (hasBeenEdited)
            {
                return;
            }

            FsmState getDetailsInit = fsm.GetState("Get Details Init");
            FsmState getDetails = fsm.GetState("Get Details");
            FsmState charmsRequiredInit = fsm.GetState("Charms Required? Init");
            FsmState charmsRequired = fsm.GetState("Charms Required?");
            FsmState notchDisplayInit = fsm.GetState("Notch Display Init");
            FsmState notchDisplay = fsm.GetState("Notch Display?");
            FsmState checkCanBuy = fsm.GetState("Check Can Buy");
            FsmState activateConfirm = fsm.GetState("Activate confirm");
            FsmState activateUI = fsm.GetState("Activate UI");

            var textSetters = getDetails.GetActionsOfType<SetTextMeshProText>();

            void SetName()
            {
                int index = fsm.FsmVariables.FindFsmInt("Current Item").Value;
                GameObject shopItem = fsm.gameObject.GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                string name;
                if (mod && mod.item != null)
                {
                    name = mod.GetPreviewName();
                }
                else
                {
                    name = Language.Language.Get(shopItem.GetComponent<ShopItemStats>().GetNameConvo(), "UI");
                }

                fsm.FsmVariables.FindFsmGameObject("Item name").Value.GetComponent<TextMeshPro>().text = name;
            }

            void ResetSprites()
            {
                foreach (GameObject shopItem in fsm.gameObject.GetComponent<ShopMenuStock>().stockInv)
                {
                    var mod = shopItem.GetComponent<ModShopItemStats>();
                    if (!mod || mod.item == null) continue;
                    shopItem.transform.Find("Item Sprite").gameObject.GetComponent<SpriteRenderer>().sprite = mod.GetSprite();
                }
            }

            void SetDesc()
            {
                int index = fsm.FsmVariables.FindFsmInt("Current Item").Value;
                GameObject shopItem = fsm.gameObject.GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                string desc;
                if (mod && mod.item != null)
                {
                    desc = mod.GetShopDesc();
                    if (mod.cost is not null && !mod.cost.Paid)
                    {
                        string? costText = mod.GetShopCostText();
                        if (!string.IsNullOrEmpty(costText))
                        {
                            desc += $"\n\n<#888888>{costText}";
                        }
                    }
                }
                else
                {
                    int charmsRequired = shopItem.GetComponent<ShopItemStats>().GetCharmsRequired();
                    if (charmsRequired > 0)
                    {
                        charmsRequired -= PlayerData.instance.GetInt(nameof(PlayerData.charmsOwned));
                    }
                    if (charmsRequired > 0)
                    {
                        desc = string.Concat(Language.Language.Get(shopItem.GetComponent<ShopItemStats>().GetDescConvo() + "_NE", "UI").Replace("<br>", "\n"), " ", charmsRequired.ToString(), " ", 
                            Language.Language.Get("CHARMS_REMAINING", "UI"));
                    }
                    else
                    {
                        desc = Language.Language.Get(shopItem.GetComponent<ShopItemStats>().GetDescConvo(), "UI").Replace("<br>", "\n");
                    }
                }

                fsm.FsmVariables.FindFsmGameObject("Item desc").Value.GetComponent<TextMeshPro>()
                    .text = desc;
            }

            void GetNotchCost()
            {
                int index = fsm.FsmVariables.FindFsmInt("Current Item").Value;
                GameObject shopItem = fsm.gameObject.GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                var stats = shopItem.GetComponent<ShopItemStats>();
                int notchCost = 0;
                if (mod && !mod.IsSecretItem() && mod.item is AbstractItem item)
                {
                    if (item.GetTag<IShopNotchCostTag>() is IShopNotchCostTag notchCostTag)
                    {
                        notchCost = notchCostTag.GetNotchCost(item);
                    }
                    else if (item is Items.CharmItem charm)
                    {
                        notchCost = PlayerData.instance.GetInt($"charmCost_{charm.charmNum}");
                    }
                }
                else
                {
                    notchCost = stats.GetNotchCost();
                }

                fsm.FsmVariables.FindFsmInt("Notch Cost").Value = notchCost;
            }

            bool CanBuy()
            {
                int index = fsm.FsmVariables.FindFsmInt("Current Item").Value;
                GameObject shopItem = fsm.gameObject.GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                if (mod)
                {
                    Cost? cost = mod.cost;
                    return cost == null || cost.Paid || cost.CanPay();
                }
                else
                {
                    return fsm.gameObject.GetComponent<ShopMenuStock>().CanBuy(index);
                }
            }

            void SetConfirmName()
            {
                int index = fsm.FsmVariables.FindFsmInt("Current Item").Value;
                GameObject shopItem = fsm.gameObject.GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                string name;
                if (mod && mod.item != null)
                {
                    name = mod.GetPreviewName();
                }
                else
                {
                    name = Language.Language.Get(shopItem.GetComponent<ShopItemStats>().GetNameConvo(), "UI");
                }

                fsm.FsmVariables.FindFsmGameObject("Confirm").Value.transform.Find("Item name").GetComponent<TextMeshPro>()
                    .text = name;
            }

            void AddIntToConfirm()
            {
                GameObject uiList = fsm.FsmVariables.FindFsmGameObject("UI List").Value;
                PlayMakerFSM confirmControl = uiList.LocateMyFSM("Confirm Control");
                FsmInt itemIndex = confirmControl.FsmVariables.FindFsmInt("Item Index");
                if (itemIndex == null)
                {
                    int length = confirmControl.FsmVariables.IntVariables.Length;
                    FsmInt[] fsmInts = new FsmInt[length + 1];
                    confirmControl.FsmVariables.IntVariables.CopyTo(fsmInts, 0);
                    itemIndex = fsmInts[length] = new FsmInt
                    {
                        Name = "Item Index",
                    };
                    confirmControl.FsmVariables.IntVariables = fsmInts;
                }
                itemIndex.Value = fsm.FsmVariables.FindFsmInt("Current Item").Value;
            }

            Lambda resetSprites = new Lambda(ResetSprites);
            Lambda setName = new Lambda(SetName);
            Lambda setSprite = new Lambda(ResetSprites);
            Lambda setDesc = new Lambda(SetDesc);
            Lambda getNotchCost = new Lambda(GetNotchCost);
            DelegateBoolTest canBuy = new DelegateBoolTest(CanBuy, checkCanBuy.GetFirstActionOfType<BoolTest>());
            Lambda setConfirmName = new Lambda(SetConfirmName);
            Lambda addIntToConfirm = new Lambda(AddIntToConfirm);

            init.AddLastAction(resetSprites);
            getDetailsInit.SetActions(
                setName, 
                setSprite,
                // 7-8 Activate detail pane
                getDetailsInit.Actions[7],
                getDetailsInit.Actions[8]);
            getDetails.SetActions(setName);
            charmsRequiredInit.SetActions(setDesc);
            charmsRequired.SetActions(setDesc);
            notchDisplayInit.AddFirstAction(getNotchCost);
            notchDisplay.AddFirstAction(getNotchCost);
            checkCanBuy.SetActions(canBuy);
            activateConfirm.SetActions(
                // Find Children
                activateConfirm.Actions[0],
                activateConfirm.Actions[1],
                activateConfirm.Actions[2],
                // 3-4 Set Confirm Name -- replace
                setConfirmName,
                // 5-6 Set Confirm Cost
                activateConfirm.Actions[5],
                activateConfirm.Actions[6],
                // 7-10 Set and adjust sprite
                activateConfirm.Actions[7],
                activateConfirm.Actions[8],
                activateConfirm.Actions[9],
                activateConfirm.Actions[10],
                // 11 Set relic number
                activateConfirm.Actions[11],
                // 12-15 Activate and send events
                activateConfirm.Actions[12],
                activateConfirm.Actions[13],
                activateConfirm.Actions[14],
                activateConfirm.Actions[15]
            );
            activateUI.AddLastAction(addIntToConfirm);
        }

        /// <summary>
        /// Change the effects of purchasing a shop item.
        /// </summary>
        protected void EditConfirmControl(PlayMakerFSM fsm)
        {
            FsmState deductSet = fsm.GetState("Deduct Geo and set PD");
            if (deductSet.GetActionsOfType<Lambda>().Any())
            {
                return; // Fsm has already been edited
            }

            bool ShouldSell()
            {
                int index = fsm.FsmVariables.FindFsmInt("Item Index").Value;
                GameObject shopItem = fsm.transform.parent.parent.Find("Item List").GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();

                // only vanilla items in a selling shop are eligible for sale
                return !mod && fsm.FsmVariables.FindFsmBool("Selling Shop").Value;
            }

            FsmState trinketCheck = fsm.GetState("Trinket?");
            BoolTest sellTest = trinketCheck.GetFirstActionOfType<BoolTest>();
            trinketCheck.ReplaceAction(new DelegateBoolTest(ShouldSell, sellTest), 
                trinketCheck.Actions.IndexOf(sellTest));

            void Give()
            {
                int index = fsm.FsmVariables.FindFsmInt("Item Index").Value;
                GameObject shopItem = fsm.transform.parent.parent.Find("Item List").GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();

                if (mod)
                {
                    mod.item.Give(mod.placement, new GiveInfo
                    {
                        Container = mod.placement?.MainContainerType,
                        FlingType = this.flingType,
                        MessageType = MessageType.Corner,
                        Transform = GameObject.Find(objectName)?.transform,
                    });
                }
                else
                {
                    string boolName = shopItem.GetComponent<ShopItemStats>().GetPlayerDataBoolName();
                    PlayerData.instance.SetBool(boolName, true);
                }
            }

            void Pay()
            {
                int index = fsm.FsmVariables.FindFsmInt("Item Index").Value;
                GameObject shopItem = fsm.transform.parent.parent.Find("Item List").GetComponent<ShopMenuStock>().stockInv[index];
                var mod = shopItem.GetComponent<ModShopItemStats>();
                var stats = shopItem.GetComponent<ShopItemStats>();

                if (mod)
                {
                    Cost? cost = mod.cost;
                    if (cost is null || cost.Paid) return;
                    cost.Pay();
                }
                else
                {
                    if (stats.cost > 0)
                    {
                        HeroController.instance.TakeGeo(stats.cost);
                    }
                }
            }

            Lambda give = new Lambda(Give);
            Lambda pay = new Lambda(Pay);

            deductSet.SetActions(give, pay);
        }

        protected void HastenItemListControl(PlayMakerFSM fsm)
        {
            FsmState menuDown = fsm.GetState("Menu Down");
            FsmState blankName = fsm.GetState("Blank Name and Desc");
            FsmState activateConfirm = fsm.GetState("Activate confirm");

            void ReduceFadeOutTime()
            {
                var fade = fsm.FsmVariables.FindFsmGameObject("Parent").Value.GetComponent<FadeGroup>();
                fade.fadeOutTimeFast = fade.fadeOutTime = 0.01f;
            }
            menuDown.AddFirstAction(new Lambda(ReduceFadeOutTime));
            menuDown.GetFirstActionOfType<Wait>().time = 0.01f;
            foreach (var a in menuDown.GetActionsOfType<SendEventByName>())
            {
                if (a.sendEvent.Value == "DOWN")
                {
                    a.sendEvent.Value = "DOWN INSTANT";
                }
            }

            void ReduceFadeInTime()
            {
                var fade = fsm.FsmVariables.FindFsmGameObject("Confirm").Value.GetComponent<FadeGroup>();
                fade.fadeInTime = 0.01f;
            }

            blankName.AddLastAction(new Lambda(ReduceFadeInTime));
            activateConfirm.GetFirstActionOfType<Wait>().time = 0.01f;
        }

        protected void HastenConfirmControl(PlayMakerFSM fsm)
        {
            FsmState particles = fsm.GetState("Particles");
            particles.GetFirstActionOfType<Wait>().time = 0.2f;
            FsmState bob = fsm.GetState("Bob");
            bob.SetActions(bob.Actions[0], bob.Actions[1]);
            FsmState specialType = fsm.GetState("Special Type?");
            bob.Transitions[0].SetToState(specialType);

            //FsmState thankFade = fsm.GetState("Thank Fade");
            //thankFade.GetFirstActionOfType<SendEventByName>().sendEvent.Value = "DOWN INSTANT";
            //thankFade.GetFirstActionOfType<Wait>().time = 0.01f;
        }

        protected void HastenUIList(PlayMakerFSM fsm)
        {
            FsmState selectionMade = fsm.GetState("Selection Made");
            FsmState selectionMadeCancel = fsm.GetState("Selection Made Cancel");
            selectionMade.GetFirstActionOfType<Wait>().time = 0.01f;
            selectionMadeCancel.GetFirstActionOfType<Wait>().time = 0.01f;
        }

        protected void HastenUIListGetInput(PlayMakerFSM fsm)
        {
            FsmState confirm = fsm.GetState("Confirm");
            FsmState cancel = fsm.GetState("Cancel");
            confirm.GetFirstActionOfType<Wait>().time = 0.01f;
            cancel.GetFirstActionOfType<Wait>().time = 0.01f;

            FsmState stillUp = fsm.GetState("Still Up?");
            FsmState stillLeft = fsm.GetState("Still Left?");
            FsmState stillRight = fsm.GetState("Still Right?");
            FsmState stillDown = fsm.GetState("Still Down?");
            stillUp.GetFirstActionOfType<Wait>().time = 0.15f;
            stillLeft.GetFirstActionOfType<Wait>().time = 0.15f;
            stillRight.GetFirstActionOfType<Wait>().time = 0.15f;
            stillDown.GetFirstActionOfType<Wait>().time = 0.15f;

            FsmState repeatUp = fsm.GetState("Repeat Up");
            FsmState repeatLeft = fsm.GetState("Repeat Left");
            FsmState repeatRight = fsm.GetState("Repeat Right");
            FsmState repeatDown = fsm.GetState("Repeat Down");
            repeatUp.GetFirstActionOfType<Wait>().time = 0.1f;
            repeatLeft.GetFirstActionOfType<Wait>().time = 0.1f;
            repeatRight.GetFirstActionOfType<Wait>().time = 0.1f;
            repeatDown.GetFirstActionOfType<Wait>().time = 0.1f;
        }

        protected void HastenUIListButtonListen(PlayMakerFSM fsm)
        {
            FsmState selectPressed = fsm.GetState("Select Pressed");
            FsmState cancelPressed = fsm.GetState("Cancel Pressed");

            selectPressed.GetFirstActionOfType<Wait>().time = 0.1f;
            cancelPressed.GetFirstActionOfType<Wait>().time = 0.1f;
        }

        public override AbstractPlacement Wrap()
        {
            return new ShopPlacement(name)
            {
                Location = this,
            };
        }
    }
}
