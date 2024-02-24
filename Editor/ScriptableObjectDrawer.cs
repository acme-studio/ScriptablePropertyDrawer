#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

#if UNITY_2022_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


[CustomPropertyDrawer(typeof(ShowInInspector), useForChildren: true)]
public class ScriptableObjectDrawer : PropertyDrawer
{
    private ObjectField objectField;
    private SerializedProperty property;
    private Button button;
    private VisualElement inspector, containerElement, firstLineContainer;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        this.property = property;

        inspector = new VisualElement();
        button = new Button(ButtonCallback);
        objectField = new ObjectField();
        containerElement = new VisualElement();
        firstLineContainer = new VisualElement();

        firstLineContainer.style.flexDirection = FlexDirection.Row;
        firstLineContainer.style.flexShrink = 1;
        firstLineContainer.style.flexGrow = 1;
        firstLineContainer.style.justifyContent = Justify.SpaceBetween;
        containerElement.style.backgroundColor = new Color(0f, 0f, 0f, .2f);

        if (fieldInfo.FieldType.IsGenericType && (fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>)))
        {
            objectField.objectType = fieldInfo.FieldType.GetGenericArguments().Single();
        }
        else
        {
            objectField.objectType = fieldInfo.FieldType;
        }

        objectField.style.flexGrow = 1;
        objectField.style.flexShrink = 1;
        objectField.label = property.displayName;
        objectField.BindProperty(property);
        objectField.RegisterValueChangedCallback(OnObjectFieldChanged);


        firstLineContainer.Add(objectField);
        firstLineContainer.Add(button);
        inspector.Add(firstLineContainer);
        inspector.Add(containerElement);

        CheckFoldout();

        return inspector;
    }

    private void OnObjectFieldChanged(ChangeEvent<UnityEngine.Object> evt)
    {
        CheckFoldout();
    }

    private void CheckFoldout()
    {
        if (objectField.value == null)
            button.style.display = DisplayStyle.None;
        else
        {
            button.style.display = DisplayStyle.Flex;
            if (property.isExpanded)
            {
                button.text = "HIDE";
                firstLineContainer.style.backgroundColor = new Color(0f, 0f, 0f, .2f);
                DrawNestedProperties(objectField.value);
            }
            else
            {
                button.text = "SHOW";
                firstLineContainer.style.backgroundColor = Color.clear;
                containerElement.Clear();
            }
        }
    }

    private void ButtonCallback()
    {
        property.isExpanded = !property.isExpanded;
        CheckFoldout();
    }

    private void DrawNestedProperties(UnityEngine.Object obj)
    {
        containerElement.Clear();
        if (obj != null)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var fieldNames = obj.GetType().GetFields(bindingFlags).Select(field => field.Name).ToList();

            foreach (string fieldName in fieldNames)
            {
                PropertyField prop = new PropertyField();

                var serObj = new SerializedObject(obj);
                var attrProp = serObj.FindProperty(fieldName);
                prop.BindProperty(attrProp);
                containerElement.Add(prop);
            }
        }
        else
        {
            containerElement.Clear();
        }
    }
#elif UNITY_2020_1_OR_NEWER
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        EditorGUI.BeginChangeCheck();
        EditorGUI.ObjectField(new Rect(position.x, position.y, position.width - 80, EditorGUIUtility.singleLineHeight), property, label);
        if (EditorGUI.EndChangeCheck())
        {
            property.isExpanded = false;
        }

        var buttonContent = property.isExpanded ? "Hide" : "Show";
        var buttonWidth = 60;
        if (GUI.Button(new Rect(position.x + position.width - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight), buttonContent))
        {
            property.isExpanded = !property.isExpanded;
        }

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawNestedProperties(position, property);
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawNestedProperties(Rect position, SerializedProperty property)
    {
        var obj = property.objectReferenceValue;
        if (obj != null)
        {
            var serializedObject = new SerializedObject(obj);
            var iterator = serializedObject.GetIterator();
            var propertyRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
            var depth = iterator.depth;

            EditorGUI.BeginChangeCheck();

            while (iterator.NextVisible(true) && iterator.depth > depth)
            {
                EditorGUI.PropertyField(propertyRect, iterator, true);
                propertyRect.y += EditorGUI.GetPropertyHeight(iterator, true);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
        {
            var obj = property.objectReferenceValue;
            if (obj != null)
            {
                var serializedObject = new SerializedObject(obj);
                var iterator = serializedObject.GetIterator();
                var depth = iterator.depth;
                while (iterator.NextVisible(true) && iterator.depth > depth)
                {
                    height += EditorGUI.GetPropertyHeight(iterator, true);
                }
            }
        }
        return height;
    }
}
#endif



