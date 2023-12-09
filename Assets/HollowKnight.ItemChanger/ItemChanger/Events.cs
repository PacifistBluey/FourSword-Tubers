﻿using ItemChanger.Extensions;
using ItemChanger.Internal;
using ItemChanger.Util;

namespace ItemChanger
{
    /// <summary>
    /// The main class in ItemChanger for organizing events. Some specific events are defined in AbstractPlacement and AbstractItem instead.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Universal hook for editing ItemChanger text.
        /// </summary>
        public static event Action<StringGetArgs>? OnStringGet;

        /// <summary>
        /// Universal hook for editing ItemChanger sprites.
        /// </summary>
        public static event Action<SpriteGetArgs>? OnSpriteGet;

        /// <summary>
        /// Called whenever a transition is overriden through ItemChanger, giving access to the original transition data and the result of the override.
        /// </summary>
        public static event TransitionOverrideListener? OnTransitionOverride;

        /// <summary>
        /// Called immediately prior to the BeginSceneTransition routine. If transition overrides take place through ItemChanger, these are applied before the event is invoked.
        /// </summary>
        public static event Action<Transition>? OnBeginSceneTransition;

        /// <summary>
        /// Called before GameManager.StartNewGame.
        /// </summary>
        public static event Action? BeforeStartNewGame;

        /// <summary>
        /// Called after GameManager.StartNewGame.
        /// </summary>
        public static event Action? AfterStartNewGame;

        /// <summary>
        /// Called after ItemChanger hooks, which occurs either when ItemChanger settings are created or when ItemChanger settings are loaded from a save file.
        /// </summary>
        public static event Action? OnItemChangerHook;

        /// <summary>
        /// Called after ItemChanger unhooks, which occurs when ItemChanger settings are nulled on returning to menu.
        /// </summary>
        public static event Action? OnItemChangerUnhook;

        /// <summary>
        /// Called on starting or continuing a save.
        /// <br/>If continuing or starting with a custom start, it is called before GM.ContinueGame.
        /// <br/>If starting with the base start, it is called before GM.StartNewGame.
        /// </summary>
        public static event Action? OnEnterGame;

        /// <summary>
        /// Called after persistent items reset, on every active scene change.
        /// </summary>
        public static event Action? OnPersistentUpdate;

        /// <summary>
        /// Called on every active scene change with the new scene as parameter.
        /// </summary>
        public static event Action<Scene>? OnSceneChange;

        /// <summary>
        /// Called after semipersistent data resets, i.e. on bench, death, special cutscenes, etc.
        /// </summary>
        public static event Action? OnSemiPersistentUpdate;



        /// <summary>
        /// The action will be invoked on any fsm matching the id.
        /// </summary>
        public static void AddFsmEdit(FsmID id, Action<PlayMakerFSM> action)
        {
            if (globalOnEnable.ContainsKey(id))
            {
                globalOnEnable[id] += action;
            }
            else
            {
                globalOnEnable[id] = action;
            }
        }

        /// <summary>
        /// Removes the action from the global hook associated to the FsmID.
        /// </summary>
        public static void RemoveFsmEdit(FsmID id, Action<PlayMakerFSM> action)
        {
            if (globalOnEnable.ContainsKey(id))
            {
                globalOnEnable[id] -= action;
            }
        }

        /// <summary>
        /// The action will be invoked on any fsm matching the id in the specified scene.
        /// </summary>
        public static void AddFsmEdit(string sceneName, FsmID id, Action<PlayMakerFSM> action)
        {
            if (!localOnEnable.TryGetValue(sceneName, out var dict))
            {
                localOnEnable[sceneName] = dict = new();
            }

            if (dict.ContainsKey(id)) dict[id] += action;
            else dict[id] = action;
        }

        /// <summary>
        /// Removes the action from the scene-specific hook associated to the FsmID.
        /// </summary>
        public static void RemoveFsmEdit(string sceneName, FsmID id, Action<PlayMakerFSM> action)
        {
            if (localOnEnable.TryGetValue(sceneName, out var dict) && dict.ContainsKey(id))
            {
                dict[id] -= action;
            }
        }

        /// <summary>
        /// The action will be invoked whenever sceneName becomes the name of the active scene.
        /// </summary>
        public static void AddSceneChangeEdit(string sceneName, Action<Scene> action)
        {
            if (activeSceneChangeEdits.ContainsKey(sceneName)) activeSceneChangeEdits[sceneName] += action;
            else activeSceneChangeEdits[sceneName] = action;
        }

        /// <summary>
        /// Removes the action from the scene-specific active scene hook.
        /// </summary>
        public static void RemoveSceneChangeEdit(string sceneName, Action<Scene> action)
        {
            if (activeSceneChangeEdits.ContainsKey(sceneName))
            {
                activeSceneChangeEdits[sceneName] -= action;
            }
        }

        /// <summary>
        /// Delegate type used to communicate when a transition is overriden.
        /// </summary>
        public delegate void TransitionOverrideListener(Transition source, Transition origTarget, ITransition newTarget);

        /// <summary>
        /// Delegate type which allows subscriber to optionally edit the input string.
        /// </summary>
        public delegate void LanguageEdit(ref string value);

        /// <summary>
        /// Hooks LanguageGet for the given key.
        /// </summary>
        public static void AddLanguageEdit(LanguageKey key, LanguageEdit func)
        {
            if (languageHooks.ContainsKey(key)) languageHooks[key] += func;
            else languageHooks[key] =  func;
        }

        /// <summary>
        /// Unhooks LanguageGet for the given key.
        /// </summary>
        public static void RemoveLanguageEdit(LanguageKey key, LanguageEdit func)
        {
            if (languageHooks.ContainsKey(key)) languageHooks[key] -= func;
        }

        /*
         *************************************************************************************
         Public API above. Implementations below.
         *************************************************************************************
        */

        private static readonly Dictionary<FsmID, Action<PlayMakerFSM>?> globalOnEnable = new();
        private static readonly Dictionary<string, Dictionary<FsmID, Action<PlayMakerFSM>?>> localOnEnable = new();
        private static readonly Dictionary<string, Action<Scene>?> activeSceneChangeEdits = new();
        private static readonly Dictionary<LanguageKey, LanguageEdit?> languageHooks = new();

        internal static void Hook()
        {
            On.PlayMakerFSM.OnEnable += FsmHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Modding.ModHooks.LanguageGetHook += LanguageGetHook;
            On.GameManager.StartNewGame += BeforeStartNewGameHook;
            On.GameManager.ContinueGame += OnContinueGame;
            On.GameManager.BeginSceneTransition += TransitionHook;
            On.GameManager.ResetSemiPersistentItems += OnResetSemiPersistentItems;
            try
            {
                OnItemChangerHook?.Invoke();
            }
            catch (Exception e)
            {
                LogError("Error invoking OnItemChangerHook:\n" + e);
            }
        }

        internal static void Unhook()
        {
            On.PlayMakerFSM.OnEnable -= FsmHook;
            UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Modding.ModHooks.LanguageGetHook -= LanguageGetHook;
            On.GameManager.StartNewGame -= BeforeStartNewGameHook;
            On.GameManager.ContinueGame -= OnContinueGame;
            On.GameManager.BeginSceneTransition -= TransitionHook;
            On.GameManager.ResetSemiPersistentItems -= OnResetSemiPersistentItems;
            try
            {
                OnItemChangerUnhook?.Invoke();
            }
            catch (Exception e)
            {
                LogError("Error invoking OnItemChangerUnhook:\n" + e);
            }
        }


        private static void FsmHook(On.PlayMakerFSM.orig_OnEnable orig, PlayMakerFSM fsm)
        {
            orig(fsm);

            FsmID weakID = new(fsm.FsmName);
            FsmID id = new(fsm);
            FsmID strongID = new("/" + fsm.gameObject.transform.GetPathInHierarchy(), fsm.FsmName);
            string sceneName = fsm.gameObject.scene.name;

            // The early container hook lets the following hooks act on a container after it has already been modified.
            // Of course, this requires the ContainerInfo component to be already on the object.
            try
            {
                Container.OnEnable(fsm);
            }
            catch (Exception e)
            {
                LogError($"Error during early container fsm hook on {strongID}:\n{e}");
            }

            // Global fsm hooks are run if the fsm matches the id, regardless of scene.
            try
            {
                globalOnEnable.GetOrDefault(weakID)?.Invoke(fsm);
                globalOnEnable.GetOrDefault(id)?.Invoke(fsm);
                globalOnEnable.GetOrDefault(strongID)?.Invoke(fsm);
            }
            catch (Exception e)
            {
                LogError($"Error during global fsm hook on {strongID}:\n{e}");
            }
            
            // Local fsm hooks are run if the fsm matches the scene and id.
            try
            {
                if (localOnEnable.TryGetValue(sceneName, out var dict))
                {
                    dict.GetOrDefault(weakID)?.Invoke(fsm);
                    dict.GetOrDefault(id)?.Invoke(fsm);
                    dict.GetOrDefault(strongID)?.Invoke(fsm);
                }
            }
            catch (Exception e)
            {
                LogError($"Error during local fsm hook on {strongID} in scene {sceneName}:\n{e}");
            }

            // Run the local search a second time for boss scenes, etc, in case the more general scene name was used to hook.
            if (SceneUtil.TryGetSuperScene(sceneName, out string normalizedSceneName))
            {
                try
                {
                    if (localOnEnable.TryGetValue(normalizedSceneName, out var dict))
                    {
                        dict.GetOrDefault(weakID)?.Invoke(fsm);
                        dict.GetOrDefault(id)?.Invoke(fsm);
                        dict.GetOrDefault(strongID)?.Invoke(fsm);
                    }
                }
                catch (Exception e)
                {
                    LogError($"Error during local fsm hook on {strongID} in scene {normalizedSceneName}:\n{e}");
                }
            }

            // the late container hook lets hooks above add ContainerInfo components 
            try
            {
                Container.OnEnable(fsm);
            }
            catch (Exception e)
            {
                LogError($"Error during late container fsm hook on {strongID}:\n{e}");
            }
        }

        private static void OnActiveSceneChanged(Scene from, Scene to)
        {
            if (Ref.Settings == null) return; // Settings is nulled by the API on active scene change to Menu_Title.

            try
            {
                Ref.Settings.ResetPersistentItems();
                OnPersistentUpdate?.Invoke();
            }
            catch(Exception e)
            {
                LogError($"Error during persistent update leaving {from.name} and entering {to.name}:\n{e}");
            }

            try
            {
                OnSceneChange?.Invoke(to);
            }
            catch (Exception e)
            {
                LogError($"Error during Events.OnSceneChange:\n{e}");
            }

            try
            {
                activeSceneChangeEdits?.GetOrDefault(to.name)?.Invoke(to);
            }
            catch(Exception e)
            {
                LogError($"Error during local activeSceneChangeEdits leaving {from.name} and entering {to.name}:\n{e}");
            }
        }

        private static string LanguageGetHook(string key, string sheetTitle, string value)
        {
            LanguageKey lk = new(sheetTitle, key);
            LanguageKey wk = new(key);

            try
            {
                LanguageEdit? func;
                if (languageHooks.TryGetValue(lk, out func))
                {
                    func?.Invoke(ref value);
                }
                if (languageHooks.TryGetValue(wk, out func))
                {
                    func?.Invoke(ref value);
                }
            }
            catch (Exception e)
            {
                LogError($"Error in Events.OnLanguageGet for {lk}::{value}:\n{e}");
            }

            return value;
        }


        private static void OnResetSemiPersistentItems(On.GameManager.orig_ResetSemiPersistentItems orig, GameManager self)
        {
            Ref.Settings.ResetSemiPersistentItems();
            try
            {
                OnSemiPersistentUpdate?.Invoke();
            }
            catch (Exception e)
            {
                LogError("Error invoking OnSemiPersistentUpdate:\n" + e);
            }
            
            orig(self);
        }

        private static void DoOnEnterGame()
        {
            try
            {
                Ref.Settings.Load();
            }
            catch (Exception e)
            {
                LogError($"Error loading settings:\n{e}");
            }
            
            try
            {
                OnEnterGame?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"Error during Events.OnEnterGame:\n{e}");
            }
        }

        private static void BeforeStartNewGameHook(On.GameManager.orig_StartNewGame orig, GameManager self, bool permadeathMode, bool bossRushMode)
        {
            try
            {
                BeforeStartNewGame?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"Error in BeforeStartNewGame event:\n{e}");
                throw;
            }

            if (Ref.Settings.Start != null)
            {
                if (permadeathMode) self.playerData.permadeathMode = 1;
                Ref.Settings.Start.ApplyToPlayerData(self.playerData);
                try
                {
                    typeof(Modding.ModHooks).GetMethod("OnNewGame", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .Invoke(null, Array.Empty<object>());
                }
                catch (Exception e)
                {
                    LogError($"Error invoking ModHooks.OnNewGame via reflection:\n{e}");
                }
                
                self.ContinueGame();
            }
            else
            {
                DoOnEnterGame();
                orig(self, permadeathMode, bossRushMode);
            }

            try
            {
                AfterStartNewGame?.Invoke();
            }
            catch (Exception e)
            {
                LogError($"Error in AfterStartNewGame event:\n{e}");
                throw;
            }
        }

        private static void OnContinueGame(On.GameManager.orig_ContinueGame orig, GameManager self)
        {
            DoOnEnterGame();
            orig(self);
        }

        private static void TransitionHook(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
        {
            string sceneName = self.sceneName;
            string? gateName = null;
            Transition origTarget = new(info.SceneName, info.EntryGateName);
            Transition target = new(info.SceneName, info.EntryGateName);

            if (info.GetType() == typeof(GameManager.SceneLoadInfo)) // gives an easy way for mods to avoid the ItemChanger hook by subclassing SceneLoadInfo
            {
                TransitionPoint tp = UnityEngine.Object.FindObjectsOfType<TransitionPoint>().FirstOrDefault(x => x.entryPoint == info.EntryGateName && x.targetScene == info.SceneName);
                if (!tp)
                {
                    switch (sceneName)
                    {
                        case SceneNames.Fungus3_44 when info.EntryGateName == "left1":
                        case SceneNames.Crossroads_02 when info.EntryGateName == "left1":
                        case SceneNames.Crossroads_06 when info.EntryGateName == "left1":
                        case SceneNames.Deepnest_10 when info.EntryGateName == "left1":
                        case SceneNames.Ruins1_04 when info.SceneName == SceneNames.Room_nailsmith:
                        case SceneNames.Fungus3_48 when info.SceneName == SceneNames.Room_Queen:
                            gateName = "door1";
                            break;
                        case SceneNames.Town when info.SceneName == SceneNames.Room_shop:
                            gateName = "door_sly";
                            break;
                        case SceneNames.Town when info.SceneName == SceneNames.Room_Town_Stag_Station:
                            gateName = "door_station";
                            break;
                        case SceneNames.Town when info.SceneName == SceneNames.Room_Bretta:
                            gateName = "door_bretta";
                            break;
                        case SceneNames.Town when info.SceneName == SceneNames.Grimm_Main_Tent:
                            gateName = "room_grimm";
                            break;
                        case SceneNames.Crossroads_04 when info.SceneName == SceneNames.Room_Charm_Shop:
                            gateName = "door_charmshop";
                            break;
                        case SceneNames.Crossroads_04 when info.SceneName == SceneNames.Room_Mender_House:
                            gateName = "door_Mender_House";
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    gateName = tp.name.Split(null)[0];
                    if (sceneName == SceneNames.Fungus2_14 && gateName[0] == 'b') gateName = "bot3";
                    else if (sceneName == SceneNames.Fungus2_15 && gateName[0] == 't') gateName = "top3";
                }

                if (sceneName != null && gateName != null)
                {
                    Transition source = new(sceneName, gateName);
                    if (Ref.Settings.TransitionOverrides.TryGetValue(source, out ITransition modified))
                    {
                        info.SceneName = modified.SceneName;
                        info.EntryGateName = modified.GateName;

                        // automatically handle the split Mantis Village transition (while still using a consistent transition value for events)
                        // the original behavior is not possible when the source is a horizontal transition, in which case the gate is not changed.
                        if (info.SceneName == SceneNames.Fungus2_14 && info.EntryGateName == "bot3" && HeroController.SilentInstance?.cState?.facingRight == false 
                            && !source.GateName.StartsWith("left") && !source.GateName.StartsWith("right"))
                        {
                            info.EntryGateName = "bot1";
                        }

                        try
                        {
                            OnTransitionOverride?.Invoke(source, origTarget, modified);
                        }
                        catch (Exception e)
                        {
                            LogError($"Error invoking OnTransitionOverride with parameters {source}, {origTarget}, {modified}:\n{e}");
                        }
                        target = new(modified.SceneName, modified.GateName);
                    }
                }
            }

            try
            {
                OnBeginSceneTransition?.Invoke(target);
            }
            catch (Exception e)
            {
                LogError($"Error invoking OnBeginSceneTransition with parameter {target}:\n{e}");
            }

            orig(self, info);
        }

        internal static string GetValue(this IString source)
        {
            if (OnStringGet != null)
            {
                StringGetArgs args = new(source);
                OnStringGet(args);
                return args.Current;
            }
            else return source.Value;
        }

        internal static Sprite GetValue(this ISprite source)
        {
            if (OnSpriteGet != null)
            {
                SpriteGetArgs args = new(source);
                OnSpriteGet(args);
                return args.Current;
            }
            else return source.Value;
        }
    }
}
