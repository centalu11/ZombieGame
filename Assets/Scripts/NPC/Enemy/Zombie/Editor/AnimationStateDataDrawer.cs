using UnityEngine;
using UnityEditor;

using ZombieGame.NPC.Enemy.Zombie.Structs;

namespace ZombieGame.NPC.Enemy.Zombie.Editor
{
    /// <summary>
    /// Custom property drawer for AnimationStateData to improve inspector usability
    /// </summary>
    [CustomPropertyDrawer(typeof(AnimationStateData))]
    public class AnimationStateDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Get the serialized properties
            SerializedProperty stateNameProp = property.FindPropertyRelative("stateName");
            SerializedProperty animationClipsProp = property.FindPropertyRelative("animationClips");
            SerializedProperty randomizedProp = property.FindPropertyRelative("randomized");
            SerializedProperty selectedClipIndexProp = property.FindPropertyRelative("selectedClipIndex");
            
            // Calculate positions
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float currentY = position.y;
            
            // Draw the main label
            Rect labelRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.LabelField(labelRect, label, EditorStyles.boldLabel);
            currentY += lineHeight + spacing;
            
            // Draw state name field
            Rect stateNameRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(stateNameRect, stateNameProp, new GUIContent("State Name"));
            currentY += lineHeight + spacing;
            
            // Draw animation clips array with custom handling
            Rect clipsRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.LabelField(clipsRect, "Animation Clips");
            currentY += lineHeight + spacing;
            
            // Draw the array with custom element handling
            if (animationClipsProp.isExpanded)
            {
                // Draw array size
                Rect sizeRect = new Rect(position.x + 10, currentY, position.width - 10, lineHeight);
                EditorGUI.PropertyField(sizeRect, animationClipsProp.FindPropertyRelative("Array.size"), new GUIContent("Size"));
                currentY += lineHeight + spacing;
                
                // Draw each element
                for (int i = 0; i < animationClipsProp.arraySize; i++)
                {
                    SerializedProperty elementProp = animationClipsProp.GetArrayElementAtIndex(i);
                    SerializedProperty clipProp = elementProp.FindPropertyRelative("clip");
                    SerializedProperty animSpeedProp = elementProp.FindPropertyRelative("animationSpeed");
                    SerializedProperty moveSpeedProp = elementProp.FindPropertyRelative("movementSpeed");
                    
                    // Ensure default values are set for new elements
                    if (animSpeedProp.floatValue == 0f && clipProp.objectReferenceValue == null)
                    {
                        animSpeedProp.floatValue = 1.0f;
                        moveSpeedProp.floatValue = 0f;
                    }
                    
                    // Element header
                    Rect elementHeaderRect = new Rect(position.x + 10, currentY, position.width - 10, lineHeight);
                    EditorGUI.LabelField(elementHeaderRect, $"Element {i}", EditorStyles.boldLabel);
                    currentY += lineHeight + spacing;
                    
                    // Clip field
                    Rect clipRect = new Rect(position.x + 20, currentY, position.width - 20, lineHeight);
                    EditorGUI.PropertyField(clipRect, clipProp, new GUIContent("Animation Clip"));
                    currentY += lineHeight + spacing;
                    
                    // Animation Speed field
                    Rect animSpeedRect = new Rect(position.x + 20, currentY, position.width - 20, lineHeight);
                    EditorGUI.PropertyField(animSpeedRect, animSpeedProp, new GUIContent("Animation Speed"));
                    currentY += lineHeight + spacing;
                    
                    // Movement Speed field
                    Rect moveSpeedRect = new Rect(position.x + 20, currentY, position.width - 20, lineHeight);
                    EditorGUI.PropertyField(moveSpeedRect, moveSpeedProp, new GUIContent("Movement Speed"));
                    currentY += lineHeight + spacing;
                    
                    // Add/Remove buttons for this element
                    Rect buttonRect = new Rect(position.x + 20, currentY, 60, lineHeight);
                    if (GUI.Button(buttonRect, "Remove"))
                    {
                        animationClipsProp.DeleteArrayElementAtIndex(i);
                        break; // Exit loop since array size changed
                    }
                    currentY += lineHeight + spacing;
                }
                
                // Add new element button
                Rect addButtonRect = new Rect(position.x + 10, currentY, 80, lineHeight);
                if (GUI.Button(addButtonRect, "Add Clip"))
                {
                    animationClipsProp.arraySize++;
                    // Set default values for the new element
                    SerializedProperty newElement = animationClipsProp.GetArrayElementAtIndex(animationClipsProp.arraySize - 1);
                    newElement.FindPropertyRelative("clip").objectReferenceValue = null;
                    newElement.FindPropertyRelative("animationSpeed").floatValue = 1.0f;
                    newElement.FindPropertyRelative("movementSpeed").floatValue = 0f;
                }
                currentY += lineHeight + spacing;
            }
            else
            {
                // Just show the expand/collapse arrow
                Rect expandRect = new Rect(position.x + 10, currentY, position.width - 10, lineHeight);
                EditorGUI.PropertyField(expandRect, animationClipsProp, GUIContent.none);
                currentY += lineHeight + spacing;
            }
            
            // Draw randomized toggle
            Rect randomizedRect = new Rect(position.x, currentY, position.width, lineHeight);
            EditorGUI.PropertyField(randomizedRect, randomizedProp, new GUIContent("Randomized"));
            currentY += lineHeight + spacing;
            
            // Draw selected clip index (only if not randomized)
            if (!randomizedProp.boolValue)
            {
                // Count valid (non-empty) clips
                int validClipCount = 0;
                for (int i = 0; i < animationClipsProp.arraySize; i++)
                {
                    SerializedProperty elementProp = animationClipsProp.GetArrayElementAtIndex(i);
                    SerializedProperty clipProp = elementProp.FindPropertyRelative("clip");
                    if (clipProp.objectReferenceValue != null)
                    {
                        validClipCount++;
                    }
                }
                
                // Calculate max index based on valid clips only
                int maxIndex = validClipCount - 1;
                if (maxIndex < 0) maxIndex = 0;
                
                // Clamp the current value
                int currentValue = selectedClipIndexProp.intValue;
                if (currentValue > maxIndex) selectedClipIndexProp.intValue = maxIndex;
                if (currentValue < 0) selectedClipIndexProp.intValue = 0;
                
                Rect indexRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.IntSlider(indexRect, selectedClipIndexProp, 0, maxIndex, new GUIContent("Selected Clip Index"));
                currentY += lineHeight + spacing;
                
                // Show which clip is selected (only if there are valid clips)
                if (validClipCount > 0 && selectedClipIndexProp.intValue < validClipCount)
                {
                    // Find the actual clip at the selected index (skipping empty slots)
                    int actualIndex = -1;
                    int validIndex = 0;
                    for (int i = 0; i < animationClipsProp.arraySize; i++)
                    {
                        SerializedProperty elementProp = animationClipsProp.GetArrayElementAtIndex(i);
                        SerializedProperty clipProp = elementProp.FindPropertyRelative("clip");
                        if (clipProp.objectReferenceValue != null)
                        {
                            if (validIndex == selectedClipIndexProp.intValue)
                            {
                                actualIndex = i;
                                break;
                            }
                            validIndex++;
                        }
                    }
                    
                    if (actualIndex >= 0)
                    {
                        SerializedProperty selectedElement = animationClipsProp.GetArrayElementAtIndex(actualIndex);
                        SerializedProperty selectedClip = selectedElement.FindPropertyRelative("clip");
                        SerializedProperty selectedAnimSpeed = selectedElement.FindPropertyRelative("animationSpeed");
                        SerializedProperty selectedMoveSpeed = selectedElement.FindPropertyRelative("movementSpeed");
                        
                        if (selectedClip.objectReferenceValue != null)
                        {
                            Rect clipNameRect = new Rect(position.x, currentY, position.width, lineHeight);
                            EditorGUI.LabelField(clipNameRect, "Selected Clip", selectedClip.objectReferenceValue.name);
                            currentY += lineHeight + spacing;
                            
                            Rect speedInfoRect = new Rect(position.x, currentY, position.width, lineHeight);
                            EditorGUI.LabelField(speedInfoRect, $"Speed: {selectedAnimSpeed.floatValue:F1}, Movement: {selectedMoveSpeed.floatValue:F1}", EditorStyles.miniLabel);
                        }
                    }
                }
                else if (validClipCount == 0)
                {
                    Rect noClipsRect = new Rect(position.x, currentY, position.width, lineHeight);
                    EditorGUI.LabelField(noClipsRect, "No valid clips available", EditorStyles.miniLabel);
                }
            }
            else
            {
                // Show info about randomization
                Rect infoRect = new Rect(position.x, currentY, position.width, lineHeight);
                EditorGUI.LabelField(infoRect, "Clip will be randomly selected on start", EditorStyles.miniLabel);
            }
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty randomizedProp = property.FindPropertyRelative("randomized");
            SerializedProperty animationClipsProp = property.FindPropertyRelative("animationClips");
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            
            // Base height: label + state name + clips
            float height = (lineHeight + spacing) * 3;
            
            // Add height for the animation clips array
            if (animationClipsProp.isExpanded)
            {
                // Size field
                height += lineHeight + spacing;
                
                // Each element: header + clip + anim speed + move speed + remove button + spacing
                height += (lineHeight + spacing) * 6 * animationClipsProp.arraySize;
                
                // Add button
                height += lineHeight + spacing;
            }
            else
            {
                // Just the expand/collapse field
                height += lineHeight + spacing;
            }
            
            // Add height for randomized toggle
            height += lineHeight + spacing;
            
            // Add height for selected clip index if not randomized
            if (!randomizedProp.boolValue)
            {
                height += lineHeight + spacing; // Index slider
                height += lineHeight + spacing; // Selected clip info
            }
            else
            {
                // Add height for randomization info
                height += lineHeight + spacing;
            }
            
            return height;
        }
    }
} 