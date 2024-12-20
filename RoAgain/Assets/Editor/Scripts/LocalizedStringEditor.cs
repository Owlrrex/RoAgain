#if UNITY_EDITOR
using Shared;
using UnityEditor;
using UnityEngine;

namespace Client
{
    [CustomEditor(typeof(LocalizedStringText))]
    public class LocalizedStringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            if (!LocalizedStringTable.IsReady())
            {
                LocalizedStringTable loaded = new();
                loaded.Register();
            }

            ILocalizedStringTable.Instance.SetClientLanguage(LocalizationLanguage.English);

            LocalizedStringText text = (LocalizedStringText)target;

            DrawDefaultInspector();
        }
    }

    [CustomPropertyDrawer(typeof(LocalizedStringId))]
    public class LocalizedStringIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!LocalizedStringTable.IsReady())
            {
                LocalizedStringTable loaded = new();
                loaded.Register();
                loaded.SetClientLanguage(LocalizationLanguage.English);
            }

            if (!LocalizedStringTable.IsReady())
                return;

            EditorGUI.BeginProperty(position, null, property);

            SerializedProperty idProp = property.FindPropertyRelative("Id");
            Rect newRect = EditorGUI.PrefixLabel(position, new GUIContent(property.name));
            EditorGUI.PropertyField(new Rect(newRect.x, newRect.y, newRect.width, 18), idProp, GUIContent.none);

            EditorGUI.indentLevel++;
            string locValue = ((LocalizedStringId)property.boxedValue).Resolve();
            newRect = EditorGUI.PrefixLabel(new Rect(position.x, position.y + EditorGUI.GetPropertyHeight(idProp) + 1, position.width, 18), new GUIContent("English string: "));
            EditorGUI.LabelField(newRect, locValue);
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 40;
        }

        [MenuItem("Localization/Reload Localization Table")]
        public static void ReloadLocTableEditor()
        {
            ILocalizedStringTable.Instance.ReloadStrings();
        }
    }
}
#endif
