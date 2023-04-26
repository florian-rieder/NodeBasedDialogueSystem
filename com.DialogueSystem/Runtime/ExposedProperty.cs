using UnityEngine.Serialization;

namespace NodeBasedDialogueSystem.com.DialogueSystem.Runtime
{
    [System.Serializable]
    public class ExposedProperty
    {
        public static ExposedProperty CreateInstance() => new ExposedProperty();

        [FormerlySerializedAs("PropertyName")] public string propertyName = "New String";
        [FormerlySerializedAs("PropertyValue")] public string propertyValue = "New Value";
    }
}