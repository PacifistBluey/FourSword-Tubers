﻿using HutongGames.PlayMaker.Actions;
using ItemChanger.FsmStateActions;
using ItemChanger.Components;
using ItemChanger.Extensions;
using ItemChanger.Internal;

namespace ItemChanger.Util
{
    public static class ShinyUtility
    {
        public static readonly Color WasEverObtainedColor = new(1f, 213f / 255f, 0.5f);

        /// <summary>
        /// Makes a Shiny Item with a name tied to location and item index. Apply FSM edits in OnEnable instead.
        /// </summary>
        public static GameObject MakeNewShiny(AbstractPlacement placement, AbstractItem item, FlingType flingType)
        {
            GameObject shiny = ObjectCache.ShinyItem;
            shiny.name = GetShinyName(placement, item);
            shiny.AddComponent<ContainerInfoComponent>().info = new ContainerInfo(Container.Shiny, placement, item.Yield(), flingType);
            // don't set ShinyFling for single shinies -- usually the container decides how to fling

            return shiny;
        }

        public static GameObject MakeNewMultiItemShiny(ContainerInfo info)
        {
            GameObject shiny = ObjectCache.ShinyItem;
            shiny.name = GetShinyPrefix(info.giveInfo.placement);
            shiny.AddComponent<ContainerInfoComponent>().info = info;

            if (info.giveInfo.placement.GetPlacementAndLocationTags().OfType<Tags.ShinyFlingTag>().FirstOrDefault() is Tags.ShinyFlingTag sft)
            {
                SetShinyFling(shiny.LocateMyFSM("Shiny Control"), sft.fling);
            }
            else
            {
                SetShinyFling(shiny.LocateMyFSM("Shiny Control"), ShinyFling.Down);
            }

            return shiny;
        }

        public static GameObject MakeNewMultiItemShiny(AbstractPlacement placement, IEnumerable<AbstractItem> items, FlingType flingType, Cost? cost = null, Transition? changeSceneTo = null)
        {
            return MakeNewMultiItemShiny(new ContainerInfo(Container.Shiny, placement, items, flingType, cost, changeSceneTo.HasValue ? new(changeSceneTo.Value) : null));
        }

        public static bool TryGetItemFromShinyName(string shinyObjectName, AbstractPlacement placement, out AbstractItem item)
        {
            item = null!;
            if (!shinyObjectName.StartsWith(GetShinyPrefix(placement))
                || !int.TryParse(shinyObjectName.Split('-').Last(), out int index)
                || index < 0
                || index >= placement.Items.Count()) return false;

            item = placement.Items.ElementAt(index);
            return true;
        }

        public static string GetShinyName(AbstractPlacement placement, AbstractItem item)
        {
            return $"{GetShinyPrefix(placement)}-{item.name}-{placement.Items.TakeWhile(i => i != item).Count()}";
        }

        public static string GetShinyPrefix(AbstractPlacement placement)
        {
            return $"Shiny Item-{placement.Name}";
        }

        public static void SetShinyColor(GameObject shiny, IEnumerable<AbstractItem> items)
        {
            SpriteRenderer sr = shiny.GetComponent<SpriteRenderer>();
            if (!sr || items == null) return;
            
            sr.color = items.All(i => i.WasEverObtained()) ? WasEverObtainedColor : Color.white;
        }

        public static void PutShinyInContainer(GameObject container, GameObject shiny)
        {
            shiny.SetActive(false);
            shiny.transform.SetParent(container.transform);
            shiny.transform.position = new(container.transform.position.x, container.transform.position.y, 0);
        }

        public static void SetShinyFling(PlayMakerFSM shinyFsm, ShinyFling fling)
        {
            switch (fling)
            {
                case ShinyFling.Down:
                    FlingShinyDown(shinyFsm);
                    break;
                case ShinyFling.Left:
                    FlingShinyLeft(shinyFsm);
                    break;
                case ShinyFling.Right:
                    FlingShinyRight(shinyFsm);
                    break;
                case ShinyFling.RandomLR:
                    FlingShinyRandomly(shinyFsm);
                    break;
                case ShinyFling.None:
                    DontFlingShiny(shinyFsm);
                    break;
            }
        }

        public static void FlingShinyRandomly(PlayMakerFSM shinyFsm)
        {
            FsmState shinyFling = shinyFsm.GetState("Fling?");
            FsmState flingRNG = shinyFsm.GetState("Fling RNG");
            if (flingRNG == null)
            {
                // this could be done without adding a state, but this minimizes how much we have to edit Fling?, so that the change is easily reverted
                flingRNG = new(shinyFsm.Fsm)
                {
                    Name = "Fling RNG",
                };
                flingRNG.SetActions(
                    new SendRandomEvent
                    {
                        events = new FsmEvent[]
                            {
                                FsmEvent.GetFsmEvent("FLING R"),
                                FsmEvent.GetFsmEvent("FLING L"),
                            },
                        weights = new FsmFloat[]
                            {
                                1f, 1f
                            },
                        delay = 0f,
                    }
                );
                flingRNG.AddTransition("FLING L", "Fling L");
                flingRNG.AddTransition("FLING R", "Fling R");
                shinyFsm.AddState(flingRNG);
            }

            shinyFling.ClearTransitions();
            shinyFling.AddTransition(FsmEvent.Finished, flingRNG);
            shinyFsm.FsmVariables.FindFsmBool("Fling On Start").Value = true;
        }

        public static void FlingShinyDown(PlayMakerFSM shinyFsm)
        {
            FsmState fling = shinyFsm.GetState("Fling?");
            FsmState flingD = shinyFsm.GetState("Fling D");
            if (flingD == null)
            {
                flingD = new(shinyFsm.Fsm)
                {
                    Name = "Fling D",
                };
                flingD.SetActions(
                    new FlingObject
                    {
                        speedMin = 0.1f,
                        speedMax = 0.1f,
                        angleMin = 270f,
                        angleMax = 270f,
                        flungObject = new FsmOwnerDefault() { OwnerOption = OwnerDefaultOption.UseOwner },
                    }
                );
                flingD.AddTransition("FINISHED", "In Air");
                shinyFsm.AddState(flingD);
            }

            fling.ClearTransitions();
            fling.AddTransition(FsmEvent.Finished, flingD);
            shinyFsm.FsmVariables.FindFsmBool("Fling On Start").Value = true;
        }

        public static void FlingShinyLeft(PlayMakerFSM shinyFsm)
        {
            FsmState fling = shinyFsm.GetState("Fling?");
            fling.ClearTransitions();
            fling.AddTransition("FINISHED", "Fling L");
            shinyFsm.FsmVariables.FindFsmBool("Fling On Start").Value = true;
        }

        public static void FlingShinyRight(PlayMakerFSM shinyFsm)
        {
            FsmState fling = shinyFsm.GetState("Fling?");
            fling.ClearTransitions();
            fling.AddTransition("FINISHED", "Fling R");
            shinyFsm.FsmVariables.FindFsmBool("Fling On Start").Value = true;
        }

        public static void DontFlingShiny(PlayMakerFSM shinyFsm)
        {
            FsmState fling = shinyFsm.GetState("Fling?");
            fling.ClearTransitions();
            fling.AddTransition("FINISHED", "Idle");
            shinyFsm.GetComponent<Rigidbody2D>().gravityScale = 0f;
            shinyFsm.FsmVariables.FindFsmBool("Fling On Start").Value = false; // skip activating the shiny's trail and its gravity
        }

        public static void AddChangeSceneToShiny(PlayMakerFSM shinyFsm, ChangeSceneInfo info)
        {
            if (info.dreamReturn)
            {
                shinyFsm.FsmVariables.FindFsmBool("Exit Dream").Value = true;
                shinyFsm.FsmVariables.FindFsmString("Return Door").Value = info.transition.GateName;
                FsmState fadePause = shinyFsm.GetState("Fade Pause");
                string sceneName = info.transition.SceneName;
                fadePause.AddFirstAction(new Lambda(() =>
                {
                    PlayerData.instance.SetString(nameof(PlayerData.dreamReturnScene), sceneName);
                }));
                if (info.deactivateNoCharms)
                {
                    fadePause.AddFirstAction(new Lambda(DeactivateNoCharms));
                }
            }
            else
            {
                FsmState finish = shinyFsm.GetState("Finish");
                if (info.deactivateNoCharms)
                {
                    finish.AddLastAction(new Lambda(DeactivateNoCharms));
                }
                finish.AddLastAction(new ChangeSceneAction(info.transition.SceneName, info.transition.GateName));
            }


            static void DeactivateNoCharms() => HeroController.instance.proxyFSM.FsmVariables.GetFsmBool("No Charms").Value = false;
            // fixes minion spawning issue after Dream Nail, Dreamers, etc
            // could extremely rarely be undesired, if the target scene is in Godhome
        }

        public static void ModifyMultiShiny(PlayMakerFSM shinyFsm, FlingType flingType, AbstractPlacement placement, IEnumerable<AbstractItem> items)
        {
            FsmState init = shinyFsm.GetState("Init");
            FsmState pdBool = shinyFsm.GetState("PD Bool?");
            FsmState charm = shinyFsm.GetState("Charm?");
            FsmState trinkFlash = shinyFsm.GetState("Trink Flash");

            GiveInfo info = new()
            {
                Container = Container.Shiny,
                FlingType = flingType,
                Transform = shinyFsm.transform,
                MessageType = MessageType.Any,
            };
            FsmStateAction checkAction = new Lambda(() => shinyFsm.SendEvent(items.All(i => i.IsObtained()) ? "COLLECTED" : null));
            FsmStateAction giveAction = new Lambda(() => ItemUtility.GiveSequentially(items, placement, info, callback: () => shinyFsm.SendEvent("GAVE ITEM")));

            // Remove actions that stop shiny from spawning
            init.RemoveActionsOfType<BoolTest>();
            pdBool.ClearActions();

            // Set shiny color to orange when all items have been previously obtained
            init.AddFirstAction(new Lambda(() => SetShinyColor(shinyFsm.gameObject, items)));

            // Change pd bool test to our new bool
            pdBool.AddLastAction(checkAction);

            // Charm must be preserved as the entry point for AddYNDialogueToShiny
            charm.ClearTransitions();
            charm.AddTransition("FINISHED", "Trink Flash");

            trinkFlash.ClearTransitions();
            trinkFlash.SetActions(
                trinkFlash.Actions[0], // Audio
                trinkFlash.Actions[1], // Audio
                trinkFlash.Actions[2], // visual effect
                trinkFlash.Actions[3], // hide shiny
                trinkFlash.Actions[4], // pickup animation
                // [5] -- spawn message
                // [6] -- store message text
                // [7] -- store message icon
                giveAction // give item
            );
            trinkFlash.AddTransition("GAVE ITEM", "Hero Up");
            trinkFlash.AddTransition("HERO DAMAGED", "Finish");
        }

        /// <summary>
        /// Call after ModifyShiny to add cost.
        /// </summary>
        public static void AddYNDialogueToShiny(PlayMakerFSM shinyFsm, Cost cost, AbstractPlacement placement, IEnumerable<AbstractItem> items)
        {
            FsmState idle = shinyFsm.GetState("Idle");
            FsmState charm = shinyFsm.GetState("Charm?");
            FsmState resumeState = shinyFsm.GetState(charm.Transitions[0].ToState);

            FsmState noState = shinyFsm.AddState("YN No");
            FsmState yesState = shinyFsm.AddState("YN Yes");
            FsmState giveControl = shinyFsm.AddState("YN Give Control");
            FsmState damageState = shinyFsm.AddState("YN Damaged");

            Lambda closeYNDialogue = new(YNUtil.CloseYNDialogue);
            Lambda endInspect = new(() => PlayMakerFSM.BroadcastEvent("END INSPECT"));
            Tk2dPlayAnimationWithEvents heroUp = new()
            {
                gameObject = new FsmOwnerDefault
                {
                    OwnerOption = OwnerDefaultOption.SpecifyGameObject,
                    GameObject = HeroController.instance.gameObject
                },
                clipName = "Collect Normal 3",
                animationTriggerEvent = null,
                animationCompleteEvent = FsmEvent.Finished
            };

            noState.AddTransition(FsmEvent.Finished, giveControl);
            noState.AddTransition(FsmEvent.GetFsmEvent("HERO DAMAGED"), giveControl);
            noState.AddLastAction(closeYNDialogue);
            noState.AddLastAction(heroUp);

            yesState.AddLastAction(closeYNDialogue);
            yesState.AddLastAction(new Lambda(cost.Pay));
            yesState.AddTransition(FsmEvent.Finished, resumeState);

            giveControl.AddLastAction(endInspect);
            giveControl.AddTransition(FsmEvent.Finished, idle);

            // For some reason playing the animation doesn't work if we come here from being damaged, locking us in the
            // YN No state. I think just having a separate state to come from if we were damaged is the simplest fix.
            damageState.AddLastAction(closeYNDialogue);
            damageState.AddTransition(FsmEvent.Finished, giveControl);

            charm.ClearTransitions();
            charm.AddTransition("HERO DAMAGED", damageState);
            charm.AddTransition("NO", noState);
            charm.AddTransition("YES", yesState);
            charm.AddTransition("FREE", resumeState);

            charm.AddFirstAction(new Lambda(() => YNUtil.OpenYNDialogue(shinyFsm.gameObject, placement, items, cost)));
            charm.AddFirstAction(new DelegateBoolTest(() => cost is null || cost.Paid, "FREE", null)); // skip yn dialogue when there is no cost
        }
    }
}
