using UnityEngine;
using UnityEditor;

namespace ZombieGame.NPC.Enemy.Zombie.Editor
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(WanderStep))]
    public class WanderStepDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Get properties
            SerializedProperty directionProp = property.FindPropertyRelative("direction");
            SerializedProperty distanceProp = property.FindPropertyRelative("distance");
            SerializedProperty angleProp = property.FindPropertyRelative("angle");
            
            // Calculate rects
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            
            Rect directionRect = new Rect(position.x, position.y, position.width, singleLineHeight);
            Rect distanceRect = new Rect(position.x, position.y + singleLineHeight + spacing, position.width, singleLineHeight);
            
            // Draw direction and distance
            EditorGUI.PropertyField(directionRect, directionProp);
            EditorGUI.PropertyField(distanceRect, distanceProp);
            
            // Only show angle field for angled directions
            WanderDirection direction = (WanderDirection)directionProp.enumValueIndex;
            if (direction == WanderDirection.ForwardWithAngle || direction == WanderDirection.BackwardWithAngle)
            {
                Rect angleRect = new Rect(position.x, position.y + (singleLineHeight + spacing) * 2, position.width, singleLineHeight);
                EditorGUI.PropertyField(angleRect, angleProp);
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty directionProp = property.FindPropertyRelative("direction");
            WanderDirection direction = (WanderDirection)directionProp.enumValueIndex;
            
            float singleLineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            
            // Base height for direction and distance
            float height = singleLineHeight * 2 + spacing;
            
            // Add height for angle field if needed
            if (direction == WanderDirection.ForwardWithAngle || direction == WanderDirection.BackwardWithAngle)
            {
                height += singleLineHeight + spacing;
            }
            
            return height;
        }
    }
#endif
} 