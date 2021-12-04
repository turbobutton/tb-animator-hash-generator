using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.VersionControl;

namespace TButt.Tools
{
	//A tool to generate .cs files with cached Animator hashes
	public class TBAnimatorHashGenerator : EditorWindow
	{
		private TBAnimatorHashGeneratorSettings _Settings;
		private List<TBAnimatorHashGeneratorSettings.Preset> _workingPresetCopies = new List<TBAnimatorHashGeneratorSettings.Preset>();

		private Vector2 _scroll;
		private Vector2 _presetsScroll;
		private int _toolbar;
		private bool _hasChanges;

		private int _currentPresetIndex;
		private string[] _slotNames;
		private string _newSlotName;

		private static readonly string SETTINGS_PATH = "TBAnimatorHashGenerator/AnimatorHashGeneratorSettings.asset";

		private static readonly string[] TOOLBAR_LABELS = new string[2] { "Settings", "Formatting" };

		const string QUOTE = "\"";
		const string PROGRESS_TITLE = "Generating Animator Hash File...";

		private static readonly string WINDOW_TITLE = "Generate Animator Hashes";

		private static readonly string VERSION_CONTROL_HELP_BOX = "NOTE: If your file already exists, you may need to manually check it out first depending on your version control software.";

		[MenuItem("Tools/Generate Animator Hashes...")]
		static void Init()
		{
			TBAnimatorHashGenerator window = (TBAnimatorHashGenerator)EditorWindow.GetWindow(typeof(TBAnimatorHashGenerator));
			window.titleContent.text = WINDOW_TITLE;

			window.Show();
		}

		private void OnEnable()
		{
			GenerateHeaderStyles(20);
			GetOrCreateSettingsFile();
			LoadData();
		}

		private void OnDisable()
		{
			//Make sure we save any changes when closing the window.

			if (_hasChanges)
			{
				bool saveChanges = EditorUtility.DisplayDialog("Unsaved Changes!", "Save changes before closing?", "Save", "Cancel");

				if (saveChanges)
				{
					SaveData();
				}
			}
		}

		private static readonly GUILayoutOption[] LAYOUT_HEIGHT_30 = new GUILayoutOption[] { GUILayout.Height(30) };
		private static readonly GUILayoutOption[] LAYOUT_MAX_WIDTH_300 = new GUILayoutOption[] { GUILayout.MaxWidth(300) };
		private void OnGUI()
		{
			GetOrCreateSettingsFile();

			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.BeginVertical(LAYOUT_MAX_WIDTH_300);

			DrawPresetSelector();

			EditorGUILayout.EndVertical();

			EditorGUILayout.BeginVertical();
			_toolbar = GUILayout.Toolbar(_toolbar, TOOLBAR_LABELS, LAYOUT_HEIGHT_30);

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			_scroll = EditorGUILayout.BeginScrollView(_scroll);

			EditorGUI.BeginChangeCheck();
			switch (_toolbar)
			{
				case 0:
					BeginChangeCheck();
					DrawControllerSelectionSection();
					EndChangeCheck();

					EditorGUILayout.Space();

					DrawSaveFileSection();
					break;
				case 1:
					BeginChangeCheck();
					DrawOptionsSection();
					EndChangeCheck();
					break;
			}

			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			DrawSaveChangesButton();
			DrawGenerateButton();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.HelpBox(VERSION_CONTROL_HELP_BOX, MessageType.Warning);
		}

		#region GUI DRAWING
		private static readonly string SELECTED_PRESET_LABEL = "Selected Preset";
		private static readonly GUILayoutOption[] LAYOUT_WIDTH_100 = new GUILayoutOption[] { GUILayout.Width(100) };
		private static readonly GUILayoutOption[] LAYOUT_WIDTH_30 = new GUILayoutOption[] { GUILayout.Width(30) };
		private static readonly GUILayoutOption[] LAYOUT_WIDTH_25 = new GUILayoutOption[] { GUILayout.Width(28) };
		private static readonly GUIContent ADD_BUTTON_LABEL = new GUIContent ("+", "Create a new preset. The currently-selected preset settings will be duplicated.");
		private static readonly GUIContent REMOVE_BUTTON_LABEL = new GUIContent ("-", "Remove preset");

		void DrawPresetSelector ()
		{
			int presetCount = RefreshPresetSelectorDropdown();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			_presetsScroll = EditorGUILayout.BeginScrollView(_presetsScroll);

			Color defaultColor = GUI.backgroundColor;
			for (int i = 0; i < presetCount; i++)
			{
				EditorGUILayout.BeginHorizontal();
				if (i > 0)
				{
					GUI.backgroundColor = defaultColor;

					bool deleteButtonPressed = GUILayout.Button(REMOVE_BUTTON_LABEL, LAYOUT_WIDTH_30);
					if (deleteButtonPressed)
					{
						bool confirmDelete = EditorUtility.DisplayDialog("Deleting Preset...", string.Format ("Are you sure you want to delete preset {0}?", _workingPresetCopies[i].name), "Yes", "No");
						
						if (confirmDelete)
						{
							//We want to check to make sure the deleted preset is not the last in the list.
							bool presetIndexOk = _currentPresetIndex < presetCount - 1;
							_workingPresetCopies.RemoveAt(i);

							presetCount = RefreshPresetSelectorDropdown();

							//If it is the last in the list, then we need to force the currently selected preset to be the NEW last in the list.
							if (!presetIndexOk)
							{
								_currentPresetIndex = presetCount - 1;
							}

							//Manually tell the system it has changes because GUI change checks don't work with confimation dialogues.
							SetHasChanges(true);

							//if we deleted a preset, let's just start the whole loop over.
							i = 0;
							continue;
						}
					}
				}
				else
				{
					GUI.backgroundColor = Color.clear;
					GUILayout.Box(string.Empty, LAYOUT_WIDTH_25);
				}
				

				if (i == _currentPresetIndex)
				{
					GUI.backgroundColor = new Color(0.2f, 0.2f, 0.5f);
				}
				else
				{
					GUI.backgroundColor = defaultColor;
				}

				if (GUILayout.Button(_workingPresetCopies[i].name, EditorStyles.toolbarButton))
				{
					_currentPresetIndex = i;
				}

				EditorGUILayout.EndHorizontal();
			}

			GUI.backgroundColor = defaultColor;

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_newSlotName) || string.IsNullOrWhiteSpace(_newSlotName));
			BeginChangeCheck();
			if (GUILayout.Button(ADD_BUTTON_LABEL, LAYOUT_WIDTH_30))
			{
				_workingPresetCopies.Add(new TBAnimatorHashGeneratorSettings.Preset(_newSlotName, _workingPresetCopies[_currentPresetIndex]));

				_newSlotName = string.Empty;
				_currentPresetIndex = _workingPresetCopies.Count - 1;
			}
			EndChangeCheck();
			EditorGUI.EndDisabledGroup();

			_newSlotName = EditorGUILayout.TextField(_newSlotName);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndScrollView();

			EditorGUILayout.EndVertical();
		}

		private static readonly string SAVING_HEADER = "Saving";
		private static readonly string SELECT_SAVE_LOCATION_BUTTON_LABEL = "Select Save Location";
		private static readonly string ASSETS_FOLDER_PATH = "Assets";
		private static readonly char FORWARD_SLASH_CHAR = '/';
		private static readonly char PERIOD_CHAR = '.';
		private static readonly string CLASS_NAME_LABEL = "Class Name";
		private static readonly string SAVE_PATH_LABEL = "Save Path";
		private static readonly GUILayoutOption[] LAYOUT_MAX_WIDTH_220_HEIGHT_30 = new GUILayoutOption[] { GUILayout.MaxWidth(220), GUILayout.Height(30) };
		void DrawSaveFileSection()
		{
			var currentSlot = _workingPresetCopies[_currentPresetIndex];

			DrawH1(SAVING_HEADER);
			if (GUILayout.Button (SELECT_SAVE_LOCATION_BUTTON_LABEL, LAYOUT_MAX_WIDTH_220_HEIGHT_30))
			{
				string startingFolderPath = string.IsNullOrEmpty(currentSlot.savePath) ? ASSETS_FOLDER_PATH : currentSlot.savePath;
				string startingFileName = string.IsNullOrEmpty(currentSlot.className) ? "AnimHashIDs.cs" : (currentSlot.className + ".cs");
				string savePath = EditorUtility.SaveFilePanel(SELECT_SAVE_LOCATION_BUTTON_LABEL, startingFolderPath, startingFileName, "cs");

				//This makes it so the previous save path doesn't get blown out if the user decides to cancel out of the save window.
				if (!string.IsNullOrEmpty (savePath))
				{
					currentSlot.savePath = FormatToRelativeFilePath (savePath);
				}

				if (currentSlot.savePath.Length != 0)
				{
					int lastSlashIndex = currentSlot.savePath.LastIndexOf(FORWARD_SLASH_CHAR);
					int lastPeriodIndex = currentSlot.savePath.LastIndexOf(PERIOD_CHAR);

					currentSlot.className = currentSlot.savePath.Remove(lastPeriodIndex, 3);
					string folderPath = currentSlot.className.Substring(0, lastSlashIndex + 1);
					currentSlot.className = currentSlot.className.Replace(folderPath, "");
				}
			}

			EditorGUILayout.BeginVertical (EditorStyles.helpBox);
			DrawH2(CLASS_NAME_LABEL);
			EditorGUILayout.LabelField(currentSlot.className);
			EditorGUILayout.Space();
			DrawH2(SAVE_PATH_LABEL);
			EditorGUILayout.LabelField(currentSlot.savePath);
			EditorGUILayout.EndVertical();
		}

		private static readonly string ANIMATOR_CONTROLLERS_LABEL = "Animator Controllers";
		private static readonly GUILayoutOption[] LAYOUT_MAX_WIDTH_220 = new GUILayoutOption[] { GUILayout.MaxWidth(220) };
		private static readonly string SELECT_CONTAINING_FOLDER_LABEL = "Select Containing Folder";
		private static readonly string FOLDER_HEADER = "Containing Folder";
		private static readonly string TARGET_CONTROLLERS_HEADER = "Target Controllers";
		void DrawControllerSelectionSection()
		{
			var currentPreset = _workingPresetCopies[_currentPresetIndex];

			DrawH1(ANIMATOR_CONTROLLERS_LABEL);
			currentPreset.type = (TBAnimatorHashGeneratorSettings.Type)EditorGUILayout.EnumPopup(currentPreset.type, LAYOUT_MAX_WIDTH_220);

			switch (currentPreset.type)
			{
				case TBAnimatorHashGeneratorSettings.Type.Folder:
					if (GUILayout.Button(SELECT_CONTAINING_FOLDER_LABEL, LAYOUT_MAX_WIDTH_220_HEIGHT_30))
					{
						string startingFolderLocation = string.IsNullOrEmpty(currentPreset.targetFolder) ? ASSETS_FOLDER_PATH : currentPreset.targetFolder;
						string targetFolder = EditorUtility.OpenFolderPanel("Select AnimatorController folder", startingFolderLocation, "");

						if (!string.IsNullOrEmpty (targetFolder))
						{
							currentPreset.targetFolder = FormatToRelativeFilePath (targetFolder);
						}
					}

					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					DrawH2(FOLDER_HEADER);

					EditorGUILayout.LabelField(currentPreset.targetFolder);
					EditorGUILayout.EndVertical();
					break;
				case TBAnimatorHashGeneratorSettings.Type.ControllersList:
					DrawControllersList();
					break;
			}
		}

		private static readonly GUIContent DROP_CONTROLLERS_HERE = new GUIContent("Drop Controllers Here", "Drag and drop Animator Controllers from your project window to add them to the list.");
		private static readonly GUIContent REMOVE_CONTROLLER_LABEL = new GUIContent("-", "Remove controller from list");
		void DrawControllersList ()
		{
			var currentPreset = _workingPresetCopies[_currentPresetIndex];

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			DrawH2(TARGET_CONTROLLERS_HEADER);

			Event evt = Event.current;

			Rect dropAreaRect = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
			Color startColor = GUI.color;
			GUI.color = Color.white;
			GUI.Box(dropAreaRect, DROP_CONTROLLERS_HERE);
			GUI.color = startColor;

			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!dropAreaRect.Contains(evt.mousePosition))
						return;

					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();

						foreach (Object draggedObject in DragAndDrop.objectReferences)
						{
							if (draggedObject.GetType () == typeof (AnimatorController))
							{
								AnimatorController controller = (AnimatorController)draggedObject;

								if (currentPreset.targetControllers.Contains (controller))
								{
									Debug.Log(string.Format("Target controller {0} is already in the list.", controller.name));
								}
								else
								{
									currentPreset.targetControllers.Add(controller);

									//Manually tell the window there are changes because the drag and drop box does not automatically register changes.
									SetHasChanges(true);
								}
							}
						}
					}
					break;
			}

			int listCout = currentPreset.targetControllers.Count;

			for (int i = 0; i < listCout; i++)
			{
				EditorGUILayout.BeginHorizontal();

				currentPreset.targetControllers[i] = (AnimatorController)EditorGUILayout.ObjectField(currentPreset.targetControllers[i], typeof(AnimatorController), false, LAYOUT_MAX_WIDTH_300);

				if (GUILayout.Button (REMOVE_CONTROLLER_LABEL, LAYOUT_WIDTH_25))
				{
					currentPreset.targetControllers.RemoveAt(i);
					listCout = currentPreset.targetControllers.Count;
					i = 0;
					continue;
				}

				EditorGUILayout.EndHorizontal();
			}

			if (listCout > 0)
			{
				if (GUILayout.Button ("Clear List", LAYOUT_MAX_WIDTH_300))
				{
					currentPreset.targetControllers.Clear();
				}
			}

			EditorGUILayout.EndVertical();
		}

		private static readonly string PARAMETERS_HEADER = "Parameters";
		private static readonly string VARIABLE_NAME_HEADER = "Variable Name";
		private static readonly string TYPE_INDICATOR_HEADER = "Type Indicator";
		private static readonly GUILayoutOption[] LAYOUT_MAX_WIDTH_250 = new GUILayoutOption[] { GUILayout.MaxWidth(250) };
		private static readonly string VARIABLE_FORMATTING_LABEL = "Variable Formatting";
		private static readonly string VARIABLE_DELIMITER_LABEL = "Variable Delimiter";
		private static readonly string TYPE_INDICATORS_LABEL = "Type Indicators";
		private static readonly string TYPE_FORMATTING_LABEL = "Type Formatting";
		private static readonly string TYPE_LOCATION_LABEL = "Type Location";
		private static readonly string TYPE_DELIMITER_LABEL = "Type Delimiter";
		private static readonly string NEW_LINE_ESCAPE = "\n";
		private static readonly string WALK_LABEL = "walk";
		private static readonly string IS_WALKING_LABEL = "isWalking";
		private static readonly string WALK_SPEED_LABEL = "walkSpeed";
		private static readonly string WALK_VARIATION_LABEL = "walkVariation";
		private static readonly string TRIGGER_LABEL = "trigger";
		private static readonly string BOOL_LABEL = "bool";
		private static readonly string FLOAT_LABEL = "float";
		private static readonly string INT_LABEL = "int";
		private static readonly string LAYERS_HEADER = "Layers";
		private static readonly string INCLUDE_LAYERS_LABEL = "Include Layers";
		private static readonly string LAYER_FORMATTING_LABEL = "Layer Formatting";
		private static readonly string LAYER_DELIMITER_LABEL = "Layer Delimiter";
		private static readonly string BASE_LAYER_LABEL = "Base Layer";

		void DrawOptionsSection()
		{
			var currentPreset = _workingPresetCopies[_currentPresetIndex];

			DrawH1(PARAMETERS_HEADER);

			DrawH2(VARIABLE_NAME_HEADER);
			//variable name
			currentPreset.variableNameFormatting = (TBAnimatorHashGeneratorSettings.Formatting)EditorGUILayout.EnumPopup(VARIABLE_FORMATTING_LABEL, currentPreset.variableNameFormatting, LAYOUT_MAX_WIDTH_250);
			currentPreset.variableDelimiter = (TBAnimatorHashGeneratorSettings.Delimiter)EditorGUILayout.EnumPopup(VARIABLE_DELIMITER_LABEL, currentPreset.variableDelimiter, LAYOUT_MAX_WIDTH_250);

			//type indicator
			EditorGUILayout.Space();
			DrawH2(TYPE_INDICATOR_HEADER);
			currentPreset.variableTypeIndicators = (TBAnimatorHashGeneratorSettings.VariableTypeIndicators)EditorGUILayout.EnumPopup(TYPE_INDICATORS_LABEL, currentPreset.variableTypeIndicators, LAYOUT_MAX_WIDTH_250);

			EditorGUI.BeginDisabledGroup(currentPreset.variableTypeIndicators == TBAnimatorHashGeneratorSettings.VariableTypeIndicators.None);
			currentPreset.typeIndicatorFormatting = (TBAnimatorHashGeneratorSettings.Formatting)EditorGUILayout.EnumPopup(TYPE_FORMATTING_LABEL, currentPreset.typeIndicatorFormatting, LAYOUT_MAX_WIDTH_250);
			currentPreset.typeIndicatorLocation = (TBAnimatorHashGeneratorSettings.TypeIndicatorLocation)EditorGUILayout.EnumPopup(TYPE_LOCATION_LABEL, currentPreset.typeIndicatorLocation, LAYOUT_MAX_WIDTH_250);
			currentPreset.typeIndicatorDelimiter = (TBAnimatorHashGeneratorSettings.Delimiter)EditorGUILayout.EnumPopup(TYPE_DELIMITER_LABEL, currentPreset.typeIndicatorDelimiter, LAYOUT_MAX_WIDTH_250);
			EditorGUI.EndDisabledGroup();

			string examples = ConstructVariableLine(WALK_LABEL, TRIGGER_LABEL, false);
			examples += NEW_LINE_ESCAPE + ConstructVariableLine(IS_WALKING_LABEL, BOOL_LABEL, false);
			examples += NEW_LINE_ESCAPE + ConstructVariableLine(WALK_SPEED_LABEL, FLOAT_LABEL, false);
			examples += NEW_LINE_ESCAPE + ConstructVariableLine(WALK_VARIATION_LABEL, INT_LABEL, false);
			EditorGUILayout.HelpBox(examples, MessageType.Info);

			EditorGUILayout.Space();

			DrawH1(LAYERS_HEADER);
			currentPreset.includeLayers = EditorGUILayout.Toggle(INCLUDE_LAYERS_LABEL, currentPreset.includeLayers);
			EditorGUI.BeginDisabledGroup(!currentPreset.includeLayers);
			currentPreset.layerFormatting = (TBAnimatorHashGeneratorSettings.Formatting)EditorGUILayout.EnumPopup(LAYER_FORMATTING_LABEL, currentPreset.layerFormatting, LAYOUT_MAX_WIDTH_250);
			currentPreset.layerDelimiter = (TBAnimatorHashGeneratorSettings.Delimiter)EditorGUILayout.EnumPopup(LAYER_DELIMITER_LABEL, currentPreset.layerDelimiter, LAYOUT_MAX_WIDTH_250);

			examples = ConstructLayerLine(BASE_LAYER_LABEL, 0, false);
			EditorGUILayout.HelpBox(examples, MessageType.Info);
			EditorGUI.EndDisabledGroup();
		}

		private static readonly GUIContent SAVE_CHANGES_LABEL = new GUIContent ("Save Changes", "You will be prompted to save when closing the window, but it's nice to have a button for it too.");
		private static GUILayoutOption[] LAYOUT_HEIGHT_40 = new GUILayoutOption[] { GUILayout.Height(40) };
		void DrawSaveChangesButton ()
		{
			Color startColor = GUI.color;
			EditorGUI.BeginDisabledGroup(!_hasChanges);

			GUI.color = Color.yellow;
			if (GUILayout.Button(SAVE_CHANGES_LABEL, LAYOUT_HEIGHT_40))
			{
				SaveData();
			}
			GUI.color = startColor;

			EditorGUI.EndDisabledGroup();
		}

		private static readonly string GENERATE_FILE_LABEL = "Generate File";
		void DrawGenerateButton()
		{
			var currentSlot = _workingPresetCopies[_currentPresetIndex];

			bool disable = string.IsNullOrEmpty(currentSlot.savePath);

			switch (currentSlot.type)
			{
				case TBAnimatorHashGeneratorSettings.Type.Folder:
					disable |= string.IsNullOrEmpty(currentSlot.targetFolder);
					break;
				case TBAnimatorHashGeneratorSettings.Type.ControllersList:
					disable |= currentSlot.targetControllers.Count < 1;
					break;
			}

			EditorGUI.BeginDisabledGroup(disable);
			Color startColor = GUI.color;

			if (disable)
			{
				GUI.color = startColor;
			}
			else
			{
				GUI.color = Color.green;
			}

			if (GUILayout.Button(GENERATE_FILE_LABEL, LAYOUT_HEIGHT_40))
			{
				GenerateFile();
			}
			EditorGUI.EndDisabledGroup();

			GUI.color = startColor;
		}
		#endregion

		#region GUI HELPERS
		void DrawH1(string headerText)
		{
			DrawHeader(headerText, 20);
		}

		void DrawH2(string headerText)
		{
			DrawHeader(headerText, 15);
		}

		void DrawHeader(string headerText, int size)
		{
			GUILayout.Label(headerText, _headerStyles[Mathf.Clamp(size - 1, 0, _headerStylesCount)]);
		}

		private int _headerStylesCount;
		private GUIStyle[] _headerStyles;
		private GUIStyle _defaultLabelStyle;
		void GenerateHeaderStyles(int count)
		{
			if (_defaultLabelStyle == null)
				_defaultLabelStyle = EditorStyles.label;

			_headerStylesCount = count;
			_headerStyles = new GUIStyle[count];
			for (int i = 0; i < count; i++)
			{
				GUIStyle style = new GUIStyle(_defaultLabelStyle);
				style.fontStyle = FontStyle.Bold;
				style.fontSize = i + 1;
				_headerStyles[i] = style;
			}
		}

		int RefreshPresetSelectorDropdown()
		{
			int slotCount = _workingPresetCopies.Count;
			if (_slotNames == null || _slotNames.Length != slotCount)
				_slotNames = new string[slotCount];

			for (int i = 0; i < slotCount; i++)
			{
				_slotNames[i] = _workingPresetCopies[i].name;
			}

			return slotCount;
		}

		void BeginChangeCheck()
		{
			EditorGUI.BeginChangeCheck();
		}

		void EndChangeCheck()
		{
			if (EditorGUI.EndChangeCheck())
			{
				if (Provider.enabled)
				{
					string guid;
					long localID;
					if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_Settings, out guid, out localID))
					{
						if (!Provider.IsOpenForEdit(Provider.GetAssetByGUID(guid)))
						{
							Task task = Provider.Checkout(_Settings, CheckoutMode.Both);
							task.Wait();
						}
					}
				}

				SetHasChanges(true);
			}
		}

		void SetHasChanges (bool hasChanges)
		{
			if (hasChanges)
			{
				EditorUtility.SetDirty(_Settings);
			}

			_hasChanges = hasChanges;
		}

		string FormatToRelativeFilePath(string rawPath)
		{
			string folderPathLabel = rawPath;
			int assetsIndex = folderPathLabel.IndexOf("Assets");

			return folderPathLabel.Substring(assetsIndex);
		}

		string FormatToAbsoluteFilePath (string relativePath)
		{
			string dataPath = Application.dataPath;
			int assetsIndex = dataPath.IndexOf("Assets");
			dataPath = dataPath.Remove(assetsIndex);

			return dataPath + relativePath;
		}
		#endregion

		#region FUNCTIONALITY
		private void SaveData ()
		{
			if (_hasChanges)
			{
				int workingPresetsCount = _workingPresetCopies.Count;
				var temp = new TBAnimatorHashGeneratorSettings.Preset[workingPresetsCount];
				for (int i = 0; i < workingPresetsCount; i++)
				{
					EditorUtility.DisplayProgressBar("Saving...", string.Format("Saving preset {0}", _workingPresetCopies[i].name), ((float)i) / workingPresetsCount);
					temp[i] = new TBAnimatorHashGeneratorSettings.Preset(_workingPresetCopies[i]);
				}

				_Settings.Presets = new List<TBAnimatorHashGeneratorSettings.Preset> (temp);

				EditorUtility.SetDirty(_Settings);
			}

#if UNITY_2020_4_OR_NEWER
			AssetDatabase.SaveAssetIfDirty(_Settings);
#else
			AssetDatabase.SaveAssets();
#endif
			_hasChanges = false;

			EditorUtility.ClearProgressBar();
		}

		void LoadData ()
		{
			int presetsCount = _Settings.Presets.Count;
			var temp = new TBAnimatorHashGeneratorSettings.Preset[_Settings.Presets.Count];
			//_workingPresetCopies = new List<TBAnimatorHashGeneratorSettings.Preset>(_Settings.Presets.Count);

			for (int i = 0; i < presetsCount; i++)
			{
				temp[i] = new TBAnimatorHashGeneratorSettings.Preset(_Settings.Presets[i]);
			}

			_workingPresetCopies = new List<TBAnimatorHashGeneratorSettings.Preset>(temp);
		}

		private void GetOrCreateSettingsFile()
		{
			if (_Settings == null)
			{
				_Settings = EditorGUIUtility.Load(SETTINGS_PATH) as TBAnimatorHashGeneratorSettings;

				if (_Settings == null)
				{
					if (!AssetDatabase.IsValidFolder ("Assets/Editor Default Resources"))
					{
						AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
					}

					if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources/TBAnimatorHashGenerator"))
					{
						AssetDatabase.CreateFolder("Assets/Editor Default Resources", "TBAnimatorHashGenerator");
					}
					AssetDatabase.CreateAsset(new TBAnimatorHashGeneratorSettings(), "Assets/Editor Default Resources/" + SETTINGS_PATH);

					AssetDatabase.SaveAssets();
					_Settings = EditorGUIUtility.Load(SETTINGS_PATH) as TBAnimatorHashGeneratorSettings;

					_Settings.Presets.Add(new TBAnimatorHashGeneratorSettings.Preset("Default"));
				}
			}
		}

		void GenerateFile()
		{
			List<string> parameterNames = new List<string>();
			List<string> layerNames = new List<string>();

			var currentSlot = _workingPresetCopies[_currentPresetIndex];

			switch (currentSlot.type)
			{
				case TBAnimatorHashGeneratorSettings.Type.Folder:
					int assetsIndex = currentSlot.targetFolder.IndexOf("Assets");
					string folder = currentSlot.targetFolder.Remove(0, assetsIndex);

					string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", "AnimatorController"), new string[] { folder });

					using (FileStream fs = File.Open(FormatToAbsoluteFilePath (currentSlot.savePath), System.IO.FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
					{
						using (StreamWriter sw = new StreamWriter(fs))
						{
							fs.SetLength(0);

							sw.WriteLine("using UnityEngine;\n");
							sw.WriteLine("public static class " + currentSlot.className);
							sw.WriteLine("{");

							for (int i = 0; i < guids.Length; i++)
							{
								string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
								AnimatorController a = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);
								EditorUtility.DisplayProgressBar(PROGRESS_TITLE, string.Format("Generating property hashes for {0}", a.name), (float)i / guids.Length);
								sw.WriteLine("\t//" + a.name);

								for (int j = 0; j < a.parameters.Length; j++)
								{
									var p = a.parameters[j];

									if (parameterNames.Contains(p.name))
										continue;

									parameterNames.Add(p.name);
									sw.WriteLine(ConstructVariableLine(p));
								}

								sw.Write("\n");
							}

							EditorUtility.ClearProgressBar();

							if (currentSlot.includeLayers)
							{
								sw.WriteLine("\tpublic static class Layers");
								sw.WriteLine("\t{");

								for (int i = 0; i < guids.Length; i++)
								{
									string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
									AnimatorController a = AssetDatabase.LoadAssetAtPath<AnimatorController>(assetPath);

									EditorUtility.DisplayProgressBar(PROGRESS_TITLE, string.Format("Generating layer hashes for {0}", a.name), (float)i / guids.Length);

									if (a.layers.Length == 1 && layerNames.Contains("Base Layer"))
										continue;

									sw.WriteLine("\t\t//" + a.name);

									for (int j = 0; j < a.layers.Length; j++)
									{
										string layerName = a.layers[j].name;

										if (layerNames.Contains(layerName))
											continue;

										layerNames.Add(layerName);
										sw.WriteLine(ConstructLayerLine(a.layers[j], j));
									}

									sw.Write("\n");
								}

								EditorUtility.ClearProgressBar();

								sw.WriteLine("\t}");
							}
							sw.WriteLine("}");

							sw.Close();
							fs.Close();
						}
					}
					break;
				case TBAnimatorHashGeneratorSettings.Type.ControllersList:
					using (FileStream fs = File.Open(FormatToAbsoluteFilePath(currentSlot.savePath), System.IO.FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
					{
						using (StreamWriter sw = new StreamWriter(fs))
						{
							fs.SetLength(0);

							sw.WriteLine("using UnityEngine;\n");
							sw.WriteLine("public static class " + currentSlot.className);
							sw.WriteLine("{");

							int targetControllersCount = currentSlot.targetControllers.Count;
							for (int i = 0; i < targetControllersCount; i++)
							{
								AnimatorController a = currentSlot.targetControllers[i];
								sw.WriteLine("\t//" + a.name);

								for (int j = 0; j < a.parameters.Length; j++)
								{
									var p = a.parameters[j];

									if (parameterNames.Contains(p.name))
										continue;

									EditorUtility.DisplayProgressBar(PROGRESS_TITLE, string.Format("Generating parameter hash for {0}", a.parameters[j].name), (float)j / a.parameters.Length);

									parameterNames.Add(p.name);
									sw.WriteLine(ConstructVariableLine(p));
								}
							}
							
							EditorUtility.ClearProgressBar();

							if (currentSlot.includeLayers)
							{
								sw.WriteLine("\tpublic static class Layers");
								sw.WriteLine("\t{");

								for (int i = 0; i < targetControllersCount; i++)
								{
									AnimatorController a = currentSlot.targetControllers[i];
									sw.WriteLine("\t\t//" + a.name);

									for (int j = 0; j < a.layers.Length; j++)
									{
										string layerName = a.layers[j].name;

										if (layerNames.Contains(layerName))
											continue;

										EditorUtility.DisplayProgressBar(PROGRESS_TITLE, string.Format("Generating layer hash for {0}", a.layers[j].name), (float)j / a.layers.Length);

										layerNames.Add(layerName);
										sw.WriteLine(ConstructLayerLine(a.layers[j], j));
									}
								}

								EditorUtility.ClearProgressBar();

								sw.Write("\n");

								sw.WriteLine("\t}");

								sw.Write("\n");
							}
							sw.WriteLine("}");

							sw.Close();
							fs.Close();
						}
						break;
					}
			}

			EditorUtility.DisplayProgressBar(PROGRESS_TITLE, "Reimporting files...", 0f);
			AssetDatabase.Refresh ();
			EditorUtility.ClearProgressBar();
		}

		string ConstructVariableLine (string name, string type, bool includeTabs = true)
		{
			string variableName = ConstructVariable (name, type);

			string finalString;
			string tabs = includeTabs ? "\t" : string.Empty;
			finalString = tabs + string.Format("public static readonly int {0} = Animator.StringToHash ({1}{2}{1});", variableName, QUOTE, name);

			return finalString;
		}

		string ConstructVariableLine(AnimatorControllerParameter p)
		{
			return ConstructVariableLine(p.name, p.type.ToString());
		}

		string ConstructVariable (string name, string type)
		{
			string finalName = name;
			string typeIndicator = string.Empty;
			string typeDelimiter = string.Empty;

			var currentSlot = _workingPresetCopies[_currentPresetIndex];

			switch (currentSlot.variableTypeIndicators)
			{
				case TBAnimatorHashGeneratorSettings.VariableTypeIndicators.None:
					break;
				case TBAnimatorHashGeneratorSettings.VariableTypeIndicators.SingleLetter:
					typeIndicator = type[0].ToString ();
					break;
				case TBAnimatorHashGeneratorSettings.VariableTypeIndicators.FullType:
					typeIndicator = type;
					break;
				default:
					break;
			}

			switch (currentSlot.typeIndicatorFormatting)
			{
				case TBAnimatorHashGeneratorSettings.Formatting.None:
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Upper:
					typeIndicator = typeIndicator.ToUpper();
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Lower:
					typeIndicator = typeIndicator.ToLower();
					break;
				default:
					break;
			}

			switch (currentSlot.typeIndicatorDelimiter)
			{
				case TBAnimatorHashGeneratorSettings.Delimiter.None:
					break;
				case TBAnimatorHashGeneratorSettings.Delimiter.Underscore:
					typeDelimiter = "_";
					break;
				default:
					break;
			}

			switch (currentSlot.variableDelimiter)
			{
				case TBAnimatorHashGeneratorSettings.Delimiter.None:
					break;
				case TBAnimatorHashGeneratorSettings.Delimiter.Underscore:
					finalName = SplitCamelCase(finalName);
					finalName = finalName.Replace(' ', '_');
					break;
				default:
					break;
			}

			switch (currentSlot.variableNameFormatting)
			{
				case TBAnimatorHashGeneratorSettings.Formatting.None:
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Upper:
					finalName = finalName.ToUpper();
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Lower:
					finalName = finalName.ToLower();
					break;
				default:
					break;
			}

			switch (currentSlot.typeIndicatorLocation)
			{
				case TBAnimatorHashGeneratorSettings.TypeIndicatorLocation.Prefix:
					if (currentSlot.variableTypeIndicators != TBAnimatorHashGeneratorSettings.VariableTypeIndicators.None)
					{
						typeIndicator += typeDelimiter;
					}

					finalName = typeIndicator + finalName;
					break;
				case TBAnimatorHashGeneratorSettings.TypeIndicatorLocation.Suffix:
					if (currentSlot.variableTypeIndicators != TBAnimatorHashGeneratorSettings.VariableTypeIndicators.None)
					{
						typeIndicator = typeDelimiter + typeIndicator;
					}
					finalName += typeIndicator;
					break;
				default:
					break;
			}

			return finalName;
		}

		string ConstructVariable (AnimatorControllerParameter p)
		{
			return ConstructVariable(p.name, p.type.ToString ());
		}

		string ConstructLayerLine(string layerName, int index, bool includeTabs = true)
		{
			var currentSlot = _workingPresetCopies[_currentPresetIndex];

			string variableName = layerName;

			switch (currentSlot.layerDelimiter)
			{
				case TBAnimatorHashGeneratorSettings.Delimiter.None:
					variableName = variableName.Replace(" ", string.Empty);
					break;
				case TBAnimatorHashGeneratorSettings.Delimiter.Underscore:
					variableName = variableName.Replace(' ', '_');
					break;
				default:
					break;
			}

			switch (currentSlot.layerFormatting)
			{
				case TBAnimatorHashGeneratorSettings.Formatting.None:
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Upper:
					variableName = variableName.ToUpper();
					break;
				case TBAnimatorHashGeneratorSettings.Formatting.Lower:
					variableName = variableName.ToLower();
					break;
				default:
					break;
			}

			string tabs = includeTabs ? "\t\t" : string.Empty;
			string finalString = string.Format("{0}public static readonly int {1} = {2};", tabs, variableName, index);
			return finalString;
		}

		string ConstructLayerLine(AnimatorControllerLayer layer, int index)
		{
			return ConstructLayerLine(layer.name, index);
		}

		private static string SplitCamelCase(string str)
		{
			return Regex.Replace(Regex.Replace(str, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
		}
#endregion
	}
}

