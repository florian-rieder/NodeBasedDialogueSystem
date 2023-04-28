using System;
using System.Collections.Generic;
using System.Linq;
using NodeBasedDialogueSystem.com.DialogueSystem.Editor.Nodes;
using NodeBasedDialogueSystem.com.DialogueSystem.Runtime;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Editor.Graph
{
    public class StoryGraphView : GraphView
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
        public readonly Vector2 DefaultCommentBlockSize = new Vector2(300, 200);
        public DialogueNode EntryPointNode;
        public Blackboard Blackboard = new Blackboard();
        internal List<ExposedProperty> ExposedProperties { get; set; } = new List<ExposedProperty>();
        private NodeSearchWindow _searchWindow;

        public StoryGraphView(StoryGraph editorWindow)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("NarrativeGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(GetEntryPointNodeInstance());
            AddSearchWindow(editorWindow);
        }


        private void AddSearchWindow(StoryGraph editorWindow)
        {
            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Configure(editorWindow, this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }


        public void ClearBlackBoardAndExposedProperties()
        {
            ExposedProperties.Clear();
            Blackboard.Clear();
        }

        public Group CreateCommentBlock(Rect rect, CommentBlockData commentBlockData = null)
        {
            commentBlockData ??= new CommentBlockData();
            var group = new Group {
                autoUpdateGeometry = true,
                title              = commentBlockData.title
            };
            AddElement(group);
            group.SetPosition(rect);
            return group;
        }

        public void AddPropertyToBlackBoard(ExposedProperty property, bool loadMode = false)
        {
            var localPropertyName  = property.propertyName;
            var localPropertyValue = property.propertyValue;
            if (!loadMode) {
                while (ExposedProperties.Any(x => x.propertyName == localPropertyName))
                    localPropertyName = $"{localPropertyName}(1)";
            }

            var item = ExposedProperty.CreateInstance();
            item.propertyName  = localPropertyName;
            item.propertyValue = localPropertyValue;
            ExposedProperties.Add(item);

            var container = new VisualElement();
            var field     = new BlackboardField { text = localPropertyName, typeText = "string" };
            container.Add(field);

            var propertyValueTextField = new TextField("Value:") {
                value = localPropertyValue
            };
            propertyValueTextField.RegisterValueChangedCallback(evt => {
                var index = ExposedProperties.FindIndex(x => x.propertyName == item.propertyName);
                ExposedProperties[index].propertyValue = evt.newValue;
            });
            var sa = new BlackboardRow(field, propertyValueTextField);
            container.Add(sa);
            Blackboard.Add(container);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startPortView   = startPort;

            ports.ForEach((port) => {
                if (startPortView != port && startPortView.node != port.node)
                    compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public void CreateNewDialogueNode(List<string> nodeName, Vector2 position) =>
            AddElement(CreateNode(nodeName, position));

        public DialogueNode CreateNode(List<string> dialogue, Vector2 position)
        {
            var tempDialogueNode = new DialogueNode() {
                GUID = Guid.NewGuid().ToString(),
                DialogueText = dialogue,
            };
            tempDialogueNode.styleSheets.Add(Resources.Load<StyleSheet>("Node"));
            var inputPort = GetPortInstance(tempDialogueNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            tempDialogueNode.inputContainer.Add(inputPort);
            tempDialogueNode.RefreshExpandedState();
            tempDialogueNode.RefreshPorts();
            tempDialogueNode.SetPosition(new Rect(position,
                                                  DefaultNodeSize)); //To-Do: implement screen center instantiation positioning

            var dialogueButton = new Button(() => { AddTextField(tempDialogueNode); }) {
                text = "Add Dialogue"
            };
            tempDialogueNode.titleButtonContainer.Add(dialogueButton);

            if (dialogue.Count > 0) {
                foreach (var text in dialogue)
                    AddTextField(tempDialogueNode, text);
            } else {
                AddTextField(tempDialogueNode);
            }

            var choiceButton = new Button(() => { AddChoicePort(tempDialogueNode); }) {
                text = "Add Choice"
            };
            tempDialogueNode.titleButtonContainer.Add(choiceButton);
            return tempDialogueNode;
        }

        void AddTextField(DialogueNode nodeCache, string text = "text")
        {
            var textField = new TextField() {
                name      = "textField",
                value     = text,
                multiline = true,
                tripleClickSelectsLine = true,
            };
            nodeCache.mainContainer.Add(textField);
            var deleteButton = new Button(() => RemoveTextField(nodeCache, textField)) {
                text = "X"
            };
            textField.Add(deleteButton);
        }

        void RemoveTextField(DialogueNode nodeCache, TextField textField)
        {
            nodeCache.DialogueText.Remove(textField.value);
            nodeCache.mainContainer.Remove(textField);
        }

        public void AddChoicePort(Node nodeCache, string overriddenPortName = "")
        {
            var generatedPort = GetPortInstance(nodeCache, Direction.Output);
            var portLabel = generatedPort.contentContainer.Q<Label>("type");
            generatedPort.contentContainer.Remove(portLabel);

            var outputPortCount = nodeCache.outputContainer.Query("connector").ToList().Count();
            var outputPortName = string.IsNullOrEmpty(overriddenPortName)
                ? $"Option {outputPortCount + 1}"
                : overriddenPortName;


            var textField = new TextField() {
                name  = "port",
                value = outputPortName
            };
            textField.RegisterValueChangedCallback(evt => generatedPort.portName = evt.newValue);
            generatedPort.contentContainer.Add(new Label("  "));
            generatedPort.contentContainer.Add(textField);
            var deleteButton = new Button(() => RemovePort(nodeCache, generatedPort)) {
                text = "X"
            };
            generatedPort.contentContainer.Add(deleteButton);
            generatedPort.portName = outputPortName;
            nodeCache.outputContainer.Add(generatedPort);
            nodeCache.RefreshPorts();
            nodeCache.RefreshExpandedState();
        }

        /// <summary> Removes a port from a node.</summary>
        /// <param name="node">The node to remove the port from.</param>
        /// <param name="socket">The port to remove.</param>
        void RemovePort(Node node, Port socket)
        {
            IEnumerable<Edge> targetEdge = edges.ToList()
                                                .Where(x => x.output.portName == socket.portName && x.output.node == socket.node);
            Edge[] enumerable = targetEdge as Edge[] ?? targetEdge.ToArray();
            if (enumerable.Any()) {
                var edge = enumerable.First();
                edge.input.Disconnect(edge);
                RemoveElement(enumerable.First());
            }

            node.outputContainer.Remove(socket);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        /// <summary> Creates a new port instance for a given node.</summary>
        /// <param name="node">The node to create the port for.</param>
        /// <param name="nodeDirection">The direction of the port.</param>
        /// <param name="capacity">The capacity of the port.</param>
        /// <returns>The port instance.</returns>
        static Port GetPortInstance(Node node, Direction nodeDirection,
                                    Port.Capacity capacity = Port.Capacity.Single) => node.InstantiatePort(Orientation.Horizontal, nodeDirection, capacity, typeof(float));
    
        /// <summary>Creates a new entry point node</summary>
        /// <returns>The created node.</returns>
        static DialogueNode GetEntryPointNodeInstance()
        {
            var nodeCache = new DialogueNode() {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                DialogueText = new List<string> {"ENTRYPOINT"},
                EntryPoint = true
            };

            var generatedPort = GetPortInstance(nodeCache, Direction.Output);
            generatedPort.portName = "Next";
            nodeCache.outputContainer.Add(generatedPort);

            nodeCache.capabilities &= ~Capabilities.Movable;
            nodeCache.capabilities &= ~Capabilities.Deletable;

            nodeCache.RefreshExpandedState();
            nodeCache.RefreshPorts();
            nodeCache.SetPosition(new Rect(100, 200, 100, 150));
            return nodeCache;
        }
    }
}