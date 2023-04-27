using System;
using System.Collections.Generic;
using System.Linq;
using NodeBasedDialogueSystem.com.DialogueSystem.Editor.Graph;
using NodeBasedDialogueSystem.com.DialogueSystem.Editor.Nodes;
using NodeBasedDialogueSystem.com.DialogueSystem.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Editor
{
    public class GraphSaveUtility
    {
        IEnumerable<Edge> Edges => _graphView.edges.ToList();
        List<DialogueNode> Nodes => _graphView.nodes.ToList().Cast<DialogueNode>().ToList();

        List<Group> CommentBlocks =>
            _graphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();
        
        DialogueContainer _dialogueContainer;
        StoryGraphView _graphView;

        public static GraphSaveUtility GetInstance(StoryGraphView graphView) => new GraphSaveUtility
        {
            _graphView = graphView
        };

        public void SaveGraph()
        {
            var filePath = EditorUtility.SaveFilePanelInProject("Save Narrative", "New Narrative", "asset", "Pick a save location");
            if (string.IsNullOrEmpty(filePath))
                return;

            SaveGraph(filePath);
            EditorUtility.RevealInFinder($"{filePath}");
        }

        public void SaveGraph(string filePath)
        {
            var dialogueContainerObject = ScriptableObject.CreateInstance<DialogueContainer>();
            if (!SaveNodes(dialogueContainerObject))
                return;
            SaveExposedProperties(dialogueContainerObject);
            SaveCommentBlocks(dialogueContainerObject);

            var loadedAsset = AssetDatabase.LoadAssetAtPath($"{filePath}", typeof(DialogueContainer));

            if (loadedAsset == null || !AssetDatabase.Contains(loadedAsset)) {
                AssetDatabase.CreateAsset(dialogueContainerObject, $"{filePath}");
            } else {
                var container = loadedAsset as DialogueContainer;
                if (container != null) {
                    container.nodeLinks         = dialogueContainerObject.nodeLinks;
                    container.dialogueNodeData  = dialogueContainerObject.dialogueNodeData;
                    container.exposedProperties = dialogueContainerObject.exposedProperties;
                    container.commentBlockData  = dialogueContainerObject.commentBlockData;
                    EditorUtility.SetDirty(container);
                }
            }

            AssetDatabase.SaveAssets();
        }

        bool SaveNodes(DialogueContainer dialogueContainerObject)
        {
            if (!Edges.Any()) return false;
            Edge[] connectedSockets = Edges.Where(x => x.input.node != null).ToArray();
            for (var i = 0; i < connectedSockets.Count(); i++) {
                var inputNode = (connectedSockets[i].input.node as DialogueNode);
                if (connectedSockets[i].output.node is not DialogueNode outputNode)
                    continue;
                if (inputNode != null) {
                    dialogueContainerObject.nodeLinks.Add(new NodeLinkData {
                        baseNodeGuid   = outputNode.GUID,
                        portName       = connectedSockets[i].output.portName,
                        targetNodeGuid = inputNode.GUID
                    });
                }
            }

            foreach (var node in Nodes.Where(node => !node.EntryPoint)) {
                dialogueContainerObject.dialogueNodeData.Add(new DialogueNodeData {
                    nodeGuid = node.GUID,
                    dialogueText = node.DialogueText,
                    position = node.GetPosition().position
                });
            }

            return true;
        }

        void SaveExposedProperties(DialogueContainer dialogueContainer)
        {
            dialogueContainer.exposedProperties.Clear();
            dialogueContainer.exposedProperties.AddRange(_graphView.ExposedProperties);
        }

        void SaveCommentBlocks(DialogueContainer dialogueContainer)
        {
            foreach (var block in CommentBlocks) {
                List<string> nodes = block.containedElements.Where(x => x is DialogueNode).Cast<DialogueNode>().Select(x => x.GUID)
                                          .ToList();

                dialogueContainer.commentBlockData.Add(new CommentBlockData {
                    childNodes = nodes,
                    title = block.title,
                    position = block.GetPosition().position
                });
            }
        }

        public void LoadNarrative(out string filePath, out string fileName)
        {
            fileName = String.Empty;
            filePath = EditorUtility.OpenFilePanel("Load Narrative", Application.dataPath + "/Resources", "asset");
            if (filePath.Length == 0)
                return;
            
            // reduce the file path to only include the path to the file from the Application.dataPath folder
            filePath = filePath.Replace(Application.dataPath, "Assets");
            // find the last / in the file path and get the file name
            var startIndex = filePath.LastIndexOf("/", StringComparison.Ordinal) + 1;
            var endIndex   = filePath.LastIndexOf(".asset", StringComparison.Ordinal);
            fileName = filePath.Substring(startIndex, endIndex - startIndex);
            // shorten the file path to only include the path to the file from the Assets folder
            _dialogueContainer = AssetDatabase.LoadAssetAtPath<DialogueContainer>(filePath);

            ClearGraph();
            GenerateDialogueNodes();
            ConnectDialogueNodes();
            AddExposedProperties();
            GenerateCommentBlocks();
        }

        /// <summary>
        /// Set Entry point GUID then Get All Nodes, remove all and their edges. Leave only the entrypoint node. (Remove its edge too)
        /// </summary>
        void ClearGraph()
        {
            Nodes.Find(x => x.EntryPoint).GUID = _dialogueContainer.nodeLinks[0].baseNodeGuid;
            foreach (var perNode in Nodes.Where(perNode => !perNode.EntryPoint)) {
                Edges.Where(x => x.input.node == perNode).ToList()
                     .ForEach(edge => _graphView.RemoveElement(edge));
                _graphView.RemoveElement(perNode);
            }
        }

        /// <summary>
        /// Create All serialized nodes and assign their guid and dialogue text to them
        /// </summary>
        void GenerateDialogueNodes()
        {
            foreach (var perNode in _dialogueContainer.dialogueNodeData) {
                var tempNode = _graphView.CreateNode(perNode.dialogueText, Vector2.zero);
                tempNode.GUID = perNode.nodeGuid;
                _graphView.AddElement(tempNode);

                List<NodeLinkData> nodePorts = _dialogueContainer.nodeLinks.Where(x => x.baseNodeGuid == perNode.nodeGuid).ToList();
                nodePorts.ForEach(x => _graphView.AddChoicePort(tempNode, x.portName));
            }
        }

        void ConnectDialogueNodes()
        {
            for (var i = 0; i < Nodes.Count; i++) {
                var k= i; //Prevent access to modified closure
                List<NodeLinkData> connections = _dialogueContainer.nodeLinks.Where(x => x.baseNodeGuid == Nodes[k].GUID).ToList();
                for (var j = 0; j < connections.Count(); j++) {
                    var targetNodeGuid = connections[j].targetNodeGuid;
                    var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);
                    LinkNodesTogether(Nodes[i].outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);

                    targetNode.SetPosition(new Rect(
                        _dialogueContainer.dialogueNodeData.First(x => x.nodeGuid == targetNodeGuid).position,
                        _graphView.DefaultNodeSize));
                }
            }
        }

        void LinkNodesTogether(Port outputSocket, Port inputSocket)
        {
            var tempEdge = new Edge() {
                output = outputSocket,
                input = inputSocket
            };
            tempEdge.input.Connect(tempEdge);
            tempEdge.output.Connect(tempEdge);
            _graphView.Add(tempEdge);
        }

        void AddExposedProperties()
        {
            _graphView.ClearBlackBoardAndExposedProperties();
            foreach (var exposedProperty in _dialogueContainer.exposedProperties) {
                _graphView.AddPropertyToBlackBoard(exposedProperty);
            }
        }

        void GenerateCommentBlocks()
        {
            foreach (var commentBlock in CommentBlocks) {
                _graphView.RemoveElement(commentBlock);
            }

            foreach (var commentBlockData in _dialogueContainer.commentBlockData) {
               var block = _graphView.CreateCommentBlock(new Rect(commentBlockData.position, _graphView.DefaultCommentBlockSize), commentBlockData);
               block.AddElements(Nodes.Where(x=>commentBlockData.childNodes.Contains(x.GUID)));
            }
        }
    }
}