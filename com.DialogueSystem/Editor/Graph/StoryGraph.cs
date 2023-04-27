using System.Linq;
using NodeBasedDialogueSystem.com.DialogueSystem.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Editor.Graph
{
    public class StoryGraph : EditorWindow
    {
        string _fileName;
        StoryGraphView _graphView;
        DialogueContainer _dialogueContainer;

        [MenuItem("Graph/Narrative Graph")]
        public static void CreateGraphViewWindow()
        {
            var window = GetWindow<StoryGraph>();
            window.titleContent = new GUIContent("Narrative Graph");
        }

        void ConstructGraphView()
        {
            _graphView = new StoryGraphView(this) {
                name = "Narrative Graph",
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        void RegenerateToolbar()
        {
            // remove the old toolbar
            rootVisualElement.Remove(rootVisualElement.Q<Toolbar>());
            // generate a new toolbar
            GenerateToolbar();
        }
        
        void GenerateToolbar() 
        {
            var toolbar           = new Toolbar();
            // add a label for the file name, non editable
            // add the save and load buttons
            toolbar.Add(new Button(() => RequestDataOperation(true)) {text  = "Save Data"});
            toolbar.Add(new Button(() => RequestDataOperation(false)) {text = "Load Data"});
            var fileNameTextField = new Label($"File Name: {_fileName}");
            toolbar.Add(fileNameTextField);
            // toolbar.Add(new Button(() => _graphView.CreateNewDialogueNode("Dialogue Node")) {text = "New Node",});
            rootVisualElement.Add(toolbar);
        }

        void RequestDataOperation(bool save) 
        {
            var saveUtility = GraphSaveUtility.GetInstance(_graphView);
            if (save) {
                saveUtility.SaveGraph();
            } else {
                saveUtility.LoadNarrative(out _fileName);
                RegenerateToolbar();
            }
        }

        void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();
            GenerateBlackBoard();
        }

        void GenerateMiniMap()
        {
            var miniMap = new MiniMap {anchored = true};
            var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            _graphView.Add(miniMap);
        }

        void GenerateBlackBoard()
        {
            var blackboard = new Blackboard(_graphView);
            blackboard.Add(new BlackboardSection {title = "Exposed Variables"});
            blackboard.addItemRequested = _ => {
                _graphView.AddPropertyToBlackBoard(ExposedProperty.CreateInstance());
            };
            blackboard.editTextRequested = (_, element, newValue) => {
                var oldPropertyName = ((BlackboardField) element).text;
                if (_graphView.ExposedProperties.Any(x => x.propertyName == newValue)) {
                    EditorUtility.DisplayDialog("Error", "This property name already exists, please chose another one.",
                        "OK");
                    return;
                }

                var targetIndex = _graphView.ExposedProperties.FindIndex(x => x.propertyName == oldPropertyName);
                _graphView.ExposedProperties[targetIndex].propertyName = newValue;
                ((BlackboardField) element).text = newValue;
            };
            blackboard.SetPosition(new Rect(10,30,200,300));
            _graphView.Add(blackboard);
            _graphView.Blackboard = blackboard;
        }

        void OnDisable() => rootVisualElement.Remove(_graphView);
    }
}