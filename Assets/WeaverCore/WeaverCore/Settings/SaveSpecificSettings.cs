﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using WeaverCore.Attributes;
using WeaverCore.Implementations;

namespace WeaverCore.Settings
{

    /// <summary>
    /// Used for storing save file specific settings.
    /// </summary>
    [ShowFeature]
	public abstract class SaveSpecificSettings : ScriptableObject
	{
		static SaveSpecificSettings_I impl = ImplFinder.GetImplementation<SaveSpecificSettings_I>();
		/// <summary>
		/// Returns true if a save file is currently loaded
		/// </summary>
		public static bool SaveCurrentlyLoaded { get; private set; }

		public static int CurrentSaveSlot { get; private set; }

		[OnFeatureLoad]
		static void SaveRegistered(SaveSpecificSettings settings)
		{
			if (GetSaveSettings(settings.GetType()) == null)
			{
				RegisterSaveSpecificSettings(settings);
			}
		}

		[OnFeatureUnload]
		static void SaveUnRegistered(SaveSpecificSettings settings)
		{
			UnregisterSaveSpecificSettings(settings);
		}

		/// <summary>
		/// A list of all the save specific data
		/// </summary>
		static HashSet<SaveSpecificSettings> saveData = new HashSet<SaveSpecificSettings>();

		public static T RegisterSaveSpecificSettings<T>() where T : SaveSpecificSettings
		{
			var data = (T)Activator.CreateInstance(typeof(T));
			saveData.Add(data);
			if (SaveCurrentlyLoaded)
			{
				impl.LoadSettings(data);
				data.OnSaveLoaded(CurrentSaveSlot);
			}
			return data;
		}

		public static SaveSpecificSettings RegisterSaveSpecificSettings(SaveSpecificSettings saveSpecificData)
		{
			saveData.Add(saveSpecificData);
			if (SaveCurrentlyLoaded)
			{
				impl.LoadSettings(saveSpecificData);
				saveSpecificData.OnSaveLoaded(CurrentSaveSlot);
			}
			return saveSpecificData;
		}

		public static void UnregisterSaveSpecificSettings<T>() where T : SaveSpecificSettings
		{
			if (SaveCurrentlyLoaded)
			{
				foreach (var save in saveData.Where(d => d is T))
				{
					save.OnSaveUnloaded(CurrentSaveSlot);
					impl.SaveSettings(save);
				}
			}
			saveData.RemoveWhere(d => d is T);
		}

		public static void UnregisterSaveSpecificSettings(SaveSpecificSettings saveSpecificData)
		{
			if (saveData.Contains(saveSpecificData))
			{
				if (SaveCurrentlyLoaded)
				{
					saveSpecificData.OnSaveUnloaded(CurrentSaveSlot);
					impl.SaveSettings(saveSpecificData);
				}
				saveData.Remove(saveSpecificData);
			}
		}

		/// <summary>
		/// Finds the save specific settings of the specified type. Returns null if it has not been registered
		/// </summary>
		/// <typeparam name="T">The save settings type to retrieve</typeparam>
		/// <returns>The save specific settings data. Returns null if it has not been registered</returns>
		public static T GetSaveSettings<T>() where T : SaveSpecificSettings
		{
			return saveData.OfType<T>().FirstOrDefault();
		}


		/// <summary>
		/// Finds the save specific settings of the specified type. Returns null if it has not been registered
		/// </summary>
		/// <returns>The save specific settings data. Returns null if it has not been registered</returns>
		public static SaveSpecificSettings GetSaveSettings(Type type)
		{
			return saveData.FirstOrDefault(s => type.IsAssignableFrom(saveData.GetType()));
		}


		/// <summary>
		/// Called after the save data has been loaded from a file
		/// </summary>
		/// <param name="saveFileNumber">The save file number</param>
		internal protected virtual void OnSaveLoaded(int saveFileNumber)
		{

		}

		/// <summary>
		/// Called right before the save data has been saved to a file
		/// </summary>
		/// <param name="saveFileNumber">The save file number</param>
		internal protected virtual void OnSaveUnloaded(int saveFileNumber)
		{

		}

		public static void LoadSaveSlot(int slot)
		{
			SaveCurrentlyLoaded = true;
			CurrentSaveSlot = slot;

			WeaverLog.Log("Loading WeaverCore Save data for slot " + CurrentSaveSlot);
			foreach (var save in saveData)
			{
				impl.LoadSettings(save);
				save.OnSaveLoaded(CurrentSaveSlot);
			}
		}

		public static void SaveAllSettings()
		{
			WeaverLog.Log("Saving WeaverCore Save data for slot " + CurrentSaveSlot);
			foreach (var save in saveData)
			{
				save.OnSaveUnloaded(CurrentSaveSlot);
				impl.SaveSettings(save);
			}
		}
	}
}
