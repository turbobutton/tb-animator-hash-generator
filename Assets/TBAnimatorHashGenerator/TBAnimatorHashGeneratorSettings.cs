using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Animations;
using UnityEditor;
#endif

namespace TButt.Tools
{
	public class TBAnimatorHashGeneratorSettings : ScriptableObject
	{
		public List<Preset> Presets = new List<Preset>();

		[System.Serializable]
		public class Preset
		{
			public string name;
			public string savePath;
			public string className;
			public Type type;
#if UNITY_EDITOR
			public List<AnimatorController> targetControllers = new List<AnimatorController> ();
#endif
			public string targetFolder;

			public Formatting variableNameFormatting;
			public Delimiter variableDelimiter;
			public VariableTypeIndicators variableTypeIndicators;
			public TypeIndicatorLocation typeIndicatorLocation;
			public Formatting typeIndicatorFormatting;
			public Delimiter typeIndicatorDelimiter;

			public bool includeLayers;
			public Formatting layerFormatting;
			public Delimiter layerDelimiter;

			public Preset (string name)
			{
				this.name = name;
			}

			public Preset (Preset presetToCopy)
			{
				name = presetToCopy.name;
				savePath = presetToCopy.savePath;
				className = presetToCopy.className;
				type = presetToCopy.type;
				targetControllers = new List<AnimatorController> (presetToCopy.targetControllers);
				targetFolder = presetToCopy.targetFolder;

				variableNameFormatting = presetToCopy.variableNameFormatting;
				variableDelimiter = presetToCopy.variableDelimiter;
				variableTypeIndicators = presetToCopy.variableTypeIndicators;
				typeIndicatorLocation = presetToCopy.typeIndicatorLocation;
				typeIndicatorFormatting = presetToCopy.typeIndicatorFormatting;
				typeIndicatorDelimiter = presetToCopy.typeIndicatorDelimiter;

				includeLayers = presetToCopy.includeLayers;
				layerFormatting = presetToCopy.layerFormatting;
				layerDelimiter = presetToCopy.layerDelimiter;
			}

			public Preset (string name, Preset presetToCopy)
			{
				this.name = name;
				savePath = presetToCopy.savePath;
				className = presetToCopy.className;
				type = presetToCopy.type;
				targetControllers = new List<AnimatorController>(presetToCopy.targetControllers);
				targetFolder = presetToCopy.targetFolder;

				variableNameFormatting = presetToCopy.variableNameFormatting;
				variableDelimiter = presetToCopy.variableDelimiter;
				variableTypeIndicators = presetToCopy.variableTypeIndicators;
				typeIndicatorLocation = presetToCopy.typeIndicatorLocation;
				typeIndicatorFormatting = presetToCopy.typeIndicatorFormatting;
				typeIndicatorDelimiter = presetToCopy.typeIndicatorDelimiter;

				includeLayers = presetToCopy.includeLayers;
				layerFormatting = presetToCopy.layerFormatting;
				layerDelimiter = presetToCopy.layerDelimiter;
			}
		}

		public enum Type
		{
			Folder,
			ControllersList
		}

		public enum Formatting
		{
			None,
			Upper,
			Lower
		}

		public enum VariableTypeIndicators
		{
			None,
			SingleLetter,
			FullType
		}

		public enum TypeIndicatorLocation
		{
			Prefix,
			Suffix
		}

		public enum Delimiter
		{
			None,
			Underscore
		}

	}

#if UNITY_EDITOR
	[CustomEditor (typeof (TBAnimatorHashGeneratorSettings))]
	public class TBAnimatorHashGeneratorSettingsEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			//Editing the file directly in the inspector can potentially cause issues, so we just lock it down.
			//We still want the information to be visible but just not editable.
			EditorGUI.BeginDisabledGroup(true);
			base.OnInspectorGUI();
			EditorGUI.EndDisabledGroup();
		}
	}
#endif
}
