#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Client
{
    [CustomEditor(typeof(LocalizedStringText))]
    public class LocalizedStringEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LocalizedStringText text = (LocalizedStringText)target;

            DrawDefaultInspector();

            if (!LocalizedStringTable.IsReady())
            {
                LocalizedStringTable loaded = new();
                loaded.Register();
                LocalizedStringTable.SetClientLanguage(ClientLanguage.English);
            }


            if (!LocalizedStringTable.IsReady())
                return;

            GUILayout.Label(LocalizedStringTable.GetStringById(text.LocalizedStringId));
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
                LocalizedStringTable.SetClientLanguage(ClientLanguage.English);
            }

            if (!LocalizedStringTable.IsReady())
                return;

            EditorGUI.BeginProperty(position, null, property);

            SerializedProperty idProp = property.FindPropertyRelative("Id");
            Rect newRect = EditorGUI.PrefixLabel(position, new GUIContent(property.name));
            EditorGUI.PropertyField(new Rect(newRect.x, newRect.y, newRect.width, 18), idProp, GUIContent.none);

            EditorGUI.indentLevel++;
            string locValue = LocalizedStringTable.GetStringById((LocalizedStringId)property.boxedValue);
            newRect = EditorGUI.PrefixLabel(new Rect(position.x, position.y + EditorGUI.GetPropertyHeight(idProp) + 1, position.width, 18), new GUIContent("English string: "));
            EditorGUI.LabelField(newRect, locValue);
            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 40;
        }

#if UNITY_EDITOR
        [MenuItem("Localization/Reload Localization Table")]
        public static void ReloadLocTableEditor()
        {
            LocalizedStringTable.ReloadStrings();
        }
#endif
    }
}
#endif
