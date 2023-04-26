using System;
using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [Serializable]
    public class NodeLinkData
    {
        [FormerlySerializedAs("BaseNodeGUID")] public string baseNodeGuid;
        [FormerlySerializedAs("PortName")] public string portName;
        [FormerlySerializedAs("TargetNodeGUID")] public string targetNodeGuid;
    }
}