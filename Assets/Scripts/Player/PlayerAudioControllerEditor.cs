using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ZombieGame.Player
{
    [CustomEditor(typeof(PlayerAudioController))]
    public class PlayerAudioControllerEditor : Editor
    {
        private SerializedProperty ambienceMusicClipProp;
        private SerializedProperty ambienceVolumeProp;
        private SerializedProperty ambienceLoopProp;
        
        private SerializedProperty chaseMusicClipProp;
        private SerializedProperty chaseVolumeProp;
        private SerializedProperty chaseFadeProp;
        private SerializedProperty chaseFadeInProp;
        private SerializedProperty chaseFadeOutProp;
        private SerializedProperty chaseLoopProp;
        
        private SerializedProperty chaseMusicRangeProp;
        
        // Audio preview variables
        private AudioSource previewAudioSource;
        private bool isPreviewPlaying = false;
        private float previewStartTime = 0f;
        private float previewEndTime = 0f;
        private bool showApplyTimeDropdown = false;
        private int selectedTimeField = 0; // 0 = start, 1 = loop start, 2 = end
        private string[] timeFieldOptions = { "Start Time", "Loop Start Time", "End Time" };
        
        // Loop handle dragging
        private int draggingLoopHandle = -1;
        
        private void OnEnable()
        {
            ambienceMusicClipProp = serializedObject.FindProperty("ambienceMusicClip");
            ambienceVolumeProp = serializedObject.FindProperty("ambienceVolume");
            ambienceLoopProp = serializedObject.FindProperty("ambienceLoop");
            
            chaseMusicClipProp = serializedObject.FindProperty("chaseMusicClip");
            chaseVolumeProp = serializedObject.FindProperty("chaseVolume");
            chaseFadeProp = serializedObject.FindProperty("chaseFade");
            chaseFadeInProp = serializedObject.FindProperty("chaseFadeIn");
            chaseFadeOutProp = serializedObject.FindProperty("chaseFadeOut");
            chaseLoopProp = serializedObject.FindProperty("chaseLoop");
            
            chaseMusicRangeProp = serializedObject.FindProperty("chaseMusicRange");
            
            // Initialize preview AudioSource
            if (previewAudioSource == null)
            {
                GameObject previewObject = new GameObject("AudioPreview");
                previewObject.hideFlags = HideFlags.HideAndDontSave;
                previewAudioSource = previewObject.AddComponent<AudioSource>();
                previewAudioSource.playOnAwake = false;
            }
        }
        
        private void OnDisable()
        {
            // Clean up preview AudioSource
            if (previewAudioSource != null)
            {
                if (isPreviewPlaying)
                {
                    previewAudioSource.Stop();
                    isPreviewPlaying = false;
                }
                if (previewAudioSource.gameObject != null)
                {
                    DestroyImmediate(previewAudioSource.gameObject);
                }
                previewAudioSource = null;
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Ambience Background Music Section
            EditorGUILayout.PropertyField(ambienceMusicClipProp);
            EditorGUILayout.PropertyField(ambienceVolumeProp);
            EditorGUILayout.PropertyField(ambienceLoopProp);
            
            EditorGUILayout.Space();
            
            // Chase Background Music Section
            EditorGUILayout.PropertyField(chaseMusicClipProp);
            EditorGUILayout.PropertyField(chaseVolumeProp);
            EditorGUILayout.PropertyField(chaseFadeProp);
            
            // Show fade settings only if chaseFade is enabled
            if (chaseFadeProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(chaseFadeInProp);
                EditorGUILayout.PropertyField(chaseFadeOutProp);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.PropertyField(chaseLoopProp);
            
            EditorGUILayout.Space();
            
            // Chase Music Range Section
            EditorGUILayout.LabelField("Change Music Range");
            
            AudioClip chaseClip = chaseMusicClipProp.objectReferenceValue as AudioClip;
            if (chaseClip != null)
            {
                float clipLength = chaseClip.length;
                
                // Get the range values
                SerializedProperty startTimeProp = chaseMusicRangeProp.FindPropertyRelative("startTime");
                SerializedProperty endTimeProp = chaseMusicRangeProp.FindPropertyRelative("endTime");
                SerializedProperty loopStartTimesProp = chaseMusicRangeProp.FindPropertyRelative("loopStartTimes");
                
                float startTime = startTimeProp.floatValue;
                float endTime = endTimeProp.floatValue;
                List<float> loopStartTimes = new List<float>();
                for (int i = 0; i < loopStartTimesProp.arraySize; i++)
                {
                    loopStartTimes.Add(loopStartTimesProp.GetArrayElementAtIndex(i).floatValue);
                }
                
                // Convert to seconds for display
                float startTimeSeconds = startTime * clipLength;
                float endTimeSeconds = endTime * clipLength;
                List<float> loopStartTimesSeconds = new List<float>();
                for (int i = 0; i < loopStartTimes.Count; i++)
                {
                    if (loopStartTimes[i] >= 0f)
                    {
                        loopStartTimesSeconds.Add(loopStartTimes[i] * clipLength);
                    }
                    else
                    {
                        loopStartTimesSeconds.Add(0f); // Placeholder for null values
                    }
                }
                
                EditorGUILayout.LabelField($"Clip Length: {clipLength:F2} seconds");
                
                // Range slider
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Range:", GUILayout.Width(50));
                
                // Create a custom range slider
                EditorGUILayout.BeginVertical();
                
                // Draw the range slider
                Rect sliderRect = EditorGUILayout.GetControlRect(false, 20);
                DrawRangeSlider(sliderRect, ref startTime, ref endTime, ref loopStartTimes, clipLength, chaseLoopProp.boolValue);
                
                // Manual input fields
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Start:", GUILayout.Width(50));
                float newStartSeconds = EditorGUILayout.FloatField(startTimeSeconds, GUILayout.Width(60));
                if (newStartSeconds != startTimeSeconds)
                {
                    startTime = newStartSeconds / clipLength;
                    // Validation: ensure start time is before end time
                    if (startTime >= endTime)
                    {
                        endTime = Mathf.Min(1f, startTime + 0.1f);
                    }
                    // Validation: ensure loop start time is after start time
                    if (loopStartTimes.Count > 0 && loopStartTimes[0] < startTime)
                    {
                        loopStartTimes[0] = startTime;
                    }
                }
                
                EditorGUILayout.LabelField("End:", GUILayout.Width(40));
                float newEndSeconds = EditorGUILayout.FloatField(endTimeSeconds, GUILayout.Width(60));
                if (newEndSeconds != endTimeSeconds)
                {
                    endTime = newEndSeconds / clipLength;
                    // Validation: ensure end time is after start time
                    if (endTime <= startTime)
                    {
                        startTime = Mathf.Max(0f, endTime - 0.1f);
                    }
                    // Validation: ensure loop start time is before end time
                    if (loopStartTimes.Count > 0 && loopStartTimes[0] >= endTime)
                    {
                        loopStartTimes[0] = endTime - 0.1f;
                    }
                }
                
                EditorGUILayout.LabelField($"Duration: {(endTimeSeconds - startTimeSeconds):F2}s", GUILayout.Width(100));
                EditorGUILayout.EndHorizontal();
                
                // Loop start times input (only show when looping is enabled)
                if (chaseLoopProp.boolValue)
                {
                    EditorGUILayout.LabelField("Loop Start Times:");
                    
                    // Display existing loop start times
                    for (int i = 0; i < loopStartTimes.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Loop {i + 1}:", GUILayout.Width(50));
                        
                        // Handle null values - make them editable
                        if (loopStartTimes[i] < 0f)
                        {
                            // Show editable field with placeholder for null values
                            string inputText = EditorGUILayout.TextField("", GUILayout.Width(60));
                            if (!string.IsNullOrEmpty(inputText) && float.TryParse(inputText, out float newLoopStartSeconds))
                            {
                                if (newLoopStartSeconds > 0f)
                                {
                                    loopStartTimes[i] = newLoopStartSeconds / clipLength;
                                    // Validation: ensure loop start time is between start and end
                                    if (loopStartTimes[i] < startTime)
                                    {
                                        loopStartTimes[i] = startTime;
                                    }
                                    if (loopStartTimes[i] >= endTime)
                                    {
                                        loopStartTimes[i] = endTime - 0.1f;
                                    }
                                }
                            }
                        }
                        else
                        {
                            float newLoopStartSeconds = EditorGUILayout.FloatField(loopStartTimesSeconds[i], GUILayout.Width(60));
                            if (newLoopStartSeconds != loopStartTimesSeconds[i])
                            {
                                loopStartTimes[i] = newLoopStartSeconds / clipLength;
                                // Validation: ensure loop start time is between start and end
                                if (loopStartTimes[i] < startTime)
                                {
                                    loopStartTimes[i] = startTime;
                                }
                                if (loopStartTimes[i] >= endTime)
                                {
                                    loopStartTimes[i] = endTime - 0.1f;
                                }
                            }
                        }
                        
                        // Remove button
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            loopStartTimes.RemoveAt(i);
                            // Force a repaint to clear any cached values
                            EditorUtility.SetDirty(target);
                            break; // Exit loop to avoid index issues
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // Add new loop start time button
                    if (GUILayout.Button("Add Loop Point", GUILayout.Width(120)))
                    {
                        // Add null loop point (represented by -1f)
                        loopStartTimes.Add(-1f);
                    }
                    
                    EditorGUILayout.LabelField("(Right-click on slider to add loop points)", EditorStyles.miniLabel);
                    
                    // Show random selection info
                    int validLoopPoints = 0;
                    foreach (float time in loopStartTimes)
                    {
                        if (time >= 0f) validLoopPoints++;
                    }
                    
                    if (validLoopPoints > 1)
                    {
                        EditorGUILayout.LabelField($"Random selection active: {validLoopPoints} loop points available", EditorStyles.boldLabel);
                    }
                    else if (validLoopPoints == 1)
                    {
                        EditorGUILayout.LabelField("Single loop point: no random selection", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("No valid loop points", EditorStyles.miniLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                
                // Update the serialized properties
                startTimeProp.floatValue = startTime;
                endTimeProp.floatValue = endTime;
                loopStartTimesProp.arraySize = loopStartTimes.Count;
                for (int i = 0; i < loopStartTimes.Count; i++)
                {
                    loopStartTimesProp.GetArrayElementAtIndex(i).floatValue = loopStartTimes[i];
                }
                
                // Audio Preview Section
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Audio Preview", EditorStyles.boldLabel);
                
                DrawAudioPreview(chaseClip, startTime, endTime, loopStartTimes, clipLength);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a chase music clip to configure the range.", MessageType.Info);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawRangeSlider(Rect position, ref float startValue, ref float endValue, ref List<float> loopValues, float clipLength, bool showLoopHandle)
        {
            // Draw background
            EditorGUI.DrawRect(position, new Color(0.2f, 0.2f, 0.2f));
            
            // Calculate slider positions
            float sliderWidth = position.width - 30; // Leave space for handles
            float startX = position.x + 15 + (startValue * sliderWidth);
            float endX = position.x + 15 + (endValue * sliderWidth);
            
            // Draw range fill
            Rect fillRect = new Rect(startX, position.y + 2, endX - startX, position.height - 4);
            EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 0.3f));
            
            // Draw start handle (white)
            Rect startHandle = new Rect(startX - 5, position.y, 10, position.height);
            EditorGUI.DrawRect(startHandle, Color.white);
            
            // Draw end handle (white)
            Rect endHandle = new Rect(endX - 5, position.y, 10, position.height);
            EditorGUI.DrawRect(endHandle, Color.white);
            
            // Draw loop handles (yellow) - only if looping is enabled
            if (showLoopHandle)
            {
                for (int i = 0; i < loopValues.Count; i++)
                {
                    // Skip null values
                    if (loopValues[i] < 0f) continue;
                    
                    float loopX = position.x + 15 + (loopValues[i] * sliderWidth);
                    Rect loopHandle = new Rect(loopX - 5, position.y, 10, position.height);
                    EditorGUI.DrawRect(loopHandle, Color.yellow);
                    
                    // Draw loop handle number
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                    labelStyle.normal.textColor = Color.black;
                    labelStyle.fontSize = 8;
                    labelStyle.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.LabelField(new Rect(loopX - 5, position.y + 2, 10, position.height - 4), (i + 1).ToString(), labelStyle);
                }
            }
            
            // Handle input
            Event e = Event.current;
            if (e.type == EventType.MouseDown && position.Contains(e.mousePosition))
            {
                float mouseX = e.mousePosition.x - position.x - 15;
                float normalizedValue = Mathf.Clamp01(mouseX / sliderWidth);
                
                // Check if clicking on existing loop handle
                if (showLoopHandle)
                {
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        // Skip null values
                        if (loopValues[i] < 0f) continue;
                        
                        float loopX = position.x + 15 + (loopValues[i] * sliderWidth);
                        if (Mathf.Abs(e.mousePosition.x - loopX) <= 5)
                        {
                            draggingLoopHandle = i;
                            e.Use();
                            GUI.changed = true;
                            return;
                        }
                    }
                    
                    // Check if right-clicking to add new loop point
                    if (e.button == 1) // Right click
                    {
                        // Add null loop point (represented by -1f)
                        loopValues.Add(-1f);
                        e.Use();
                        GUI.changed = true;
                        return;
                    }
                }
                
                // Determine which handle to drag
                float startDist = Mathf.Abs(normalizedValue - startValue);
                float endDist = Mathf.Abs(normalizedValue - endValue);
                float minLoopDist = float.MaxValue;
                
                if (showLoopHandle && loopValues.Count > 0)
                {
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        float dist = Mathf.Abs(normalizedValue - loopValues[i]);
                        if (dist < minLoopDist) minLoopDist = dist;
                    }
                }
                
                if (startDist < endDist && startDist < minLoopDist)
                {
                    // Dragging start handle
                    startValue = Mathf.Clamp(normalizedValue, 0f, endValue - 0.01f);
                    // Ensure all loop start times are after new start time
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        if (loopValues[i] < startValue)
                        {
                            loopValues[i] = startValue;
                        }
                    }
                }
                else if (endDist < minLoopDist)
                {
                    // Dragging end handle
                    endValue = Mathf.Clamp(normalizedValue, startValue + 0.01f, 1f);
                    // Ensure all loop start times are before new end time
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        if (loopValues[i] >= endValue)
                        {
                            loopValues[i] = endValue - 0.01f;
                        }
                    }
                }
                
                e.Use();
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseDrag && position.Contains(e.mousePosition))
            {
                float mouseX = e.mousePosition.x - position.x - 15;
                float normalizedValue = Mathf.Clamp01(mouseX / sliderWidth);
                
                // Handle loop handle dragging
                if (draggingLoopHandle >= 0 && draggingLoopHandle < loopValues.Count)
                {
                    loopValues[draggingLoopHandle] = Mathf.Clamp(normalizedValue, startValue, endValue - 0.01f);
                    // Don't sort - keep them in the order they were added for random selection
                    e.Use();
                    GUI.changed = true;
                    return;
                }
                
                // Handle start/end handle dragging (same logic as above)
                float startDist = Mathf.Abs(normalizedValue - startValue);
                float endDist = Mathf.Abs(normalizedValue - endValue);
                float minLoopDist = float.MaxValue;
                
                if (showLoopHandle && loopValues.Count > 0)
                {
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        float dist = Mathf.Abs(normalizedValue - loopValues[i]);
                        if (dist < minLoopDist) minLoopDist = dist;
                    }
                }
                
                if (startDist < endDist && startDist < minLoopDist)
                {
                    startValue = Mathf.Clamp(normalizedValue, 0f, endValue - 0.01f);
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        if (loopValues[i] < startValue)
                        {
                            loopValues[i] = startValue;
                        }
                    }
                }
                else if (endDist < minLoopDist)
                {
                    endValue = Mathf.Clamp(normalizedValue, startValue + 0.01f, 1f);
                    for (int i = 0; i < loopValues.Count; i++)
                    {
                        if (loopValues[i] >= endValue)
                        {
                            loopValues[i] = endValue - 0.01f;
                        }
                    }
                }
                
                e.Use();
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseUp)
            {
                draggingLoopHandle = -1;
            }
        }
        
        private void DrawAudioPreview(AudioClip clip, float startTime, float endTime, List<float> loopStartTimes, float clipLength)
        {
            if (clip == null || previewAudioSource == null) return;
            
            // Update preview times
            previewStartTime = startTime * clipLength;
            previewEndTime = endTime * clipLength;
            
            // Set the clip
            previewAudioSource.clip = clip;
            
            // Preview controls
            EditorGUILayout.BeginHorizontal();
            
            if (isPreviewPlaying)
            {
                if (GUILayout.Button("Pause", GUILayout.Width(60)))
                {
                    PausePreview();
                }
            }
            else
            {
                if (GUILayout.Button("Play", GUILayout.Width(60)))
                {
                    StartPreview();
                }
            }
            
            // Reset to start button
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                ResetPreview();
            }
            
            // Current time display
            float currentTime = previewAudioSource.time;
            EditorGUILayout.LabelField($"Time: {currentTime:F2}s / {previewEndTime:F2}s", GUILayout.Width(150));
            
            EditorGUILayout.EndHorizontal();
            
            // Draggable progress bar
            Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
            DrawDraggableProgressBar(progressRect, currentTime, previewStartTime, previewEndTime);
            
            // Apply Current Time button (only show when paused)
            if (!isPreviewPlaying && previewAudioSource.time > 0)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Apply Current Time", GUILayout.Width(120)))
                {
                    showApplyTimeDropdown = !showApplyTimeDropdown;
                }
                
                if (showApplyTimeDropdown)
                {
                    selectedTimeField = EditorGUILayout.Popup(selectedTimeField, timeFieldOptions, GUILayout.Width(120));
                    
                    if (GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        ApplyCurrentTime();
                        showApplyTimeDropdown = false;
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Handle preview end
            if (isPreviewPlaying && previewAudioSource.time >= previewEndTime)
            {
                PausePreview();
            }
        }
        
        private void StartPreview()
        {
            if (previewAudioSource != null && previewAudioSource.clip != null)
            {
                // If we're starting fresh (not resuming), go to start time
                if (previewAudioSource.time < previewStartTime || previewAudioSource.time >= previewEndTime)
                {
                    previewAudioSource.time = previewStartTime;
                }
                previewAudioSource.Play();
                isPreviewPlaying = true;
            }
        }
        
        private void PausePreview()
        {
            if (previewAudioSource != null)
            {
                previewAudioSource.Pause();
                isPreviewPlaying = false;
            }
        }
        
        private void ResetPreview()
        {
            if (previewAudioSource != null)
            {
                previewAudioSource.time = previewStartTime;
                if (isPreviewPlaying)
                {
                    previewAudioSource.Play();
                }
            }
        }
        
        private void DrawDraggableProgressBar(Rect position, float currentTime, float startTime, float endTime)
        {
            // Draw background
            EditorGUI.DrawRect(position, new Color(0.2f, 0.2f, 0.2f));
            
            // Calculate progress
            float progress = (currentTime - startTime) / (endTime - startTime);
            progress = Mathf.Clamp01(progress);
            
            // Draw filled portion
            float fillWidth = position.width * progress;
            Rect fillRect = new Rect(position.x, position.y + 2, fillWidth, position.height - 4);
            EditorGUI.DrawRect(fillRect, new Color(0.3f, 0.7f, 0.3f));
            
            // Draw progress handle
            float handleX = position.x + (position.width * progress);
            Rect handleRect = new Rect(handleX - 3, position.y, 6, position.height);
            EditorGUI.DrawRect(handleRect, Color.white);
            
            // Handle input
            Event e = Event.current;
            if (e.type == EventType.MouseDown && position.Contains(e.mousePosition))
            {
                float mouseX = e.mousePosition.x - position.x;
                float newProgress = Mathf.Clamp01(mouseX / position.width);
                float newTime = startTime + (newProgress * (endTime - startTime));
                
                if (previewAudioSource != null)
                {
                    previewAudioSource.time = newTime;
                    if (isPreviewPlaying)
                    {
                        previewAudioSource.Play();
                    }
                }
                
                e.Use();
                GUI.changed = true;
            }
            else if (e.type == EventType.MouseDrag && position.Contains(e.mousePosition))
            {
                float mouseX = e.mousePosition.x - position.x;
                float newProgress = Mathf.Clamp01(mouseX / position.width);
                float newTime = startTime + (newProgress * (endTime - startTime));
                
                if (previewAudioSource != null)
                {
                    previewAudioSource.time = newTime;
                }
                
                e.Use();
                GUI.changed = true;
            }
        }
        
        private void ApplyCurrentTime()
        {
            if (previewAudioSource == null) return;
            
            float currentTime = previewAudioSource.time;
            float normalizedTime = currentTime / previewAudioSource.clip.length;
            
            SerializedProperty startTimeProp = chaseMusicRangeProp.FindPropertyRelative("startTime");
            SerializedProperty endTimeProp = chaseMusicRangeProp.FindPropertyRelative("endTime");
            SerializedProperty loopStartTimesProp = chaseMusicRangeProp.FindPropertyRelative("loopStartTimes");
            
            switch (selectedTimeField)
            {
                case 0: // Start Time
                    startTimeProp.floatValue = normalizedTime;
                    // Ensure end time is after start time
                    if (endTimeProp.floatValue <= normalizedTime)
                    {
                        endTimeProp.floatValue = Mathf.Min(1f, normalizedTime + 0.1f);
                    }
                    // Ensure all loop start times are after start time
                    for (int i = 0; i < loopStartTimesProp.arraySize; i++)
                    {
                        if (loopStartTimesProp.GetArrayElementAtIndex(i).floatValue < normalizedTime)
                        {
                            loopStartTimesProp.GetArrayElementAtIndex(i).floatValue = normalizedTime;
                        }
                    }
                    break;
                    
                case 1: // Loop Start Time
                    // Add new loop start time at current position
                    loopStartTimesProp.arraySize++;
                    loopStartTimesProp.GetArrayElementAtIndex(loopStartTimesProp.arraySize - 1).floatValue = normalizedTime;
                    // Ensure loop start time is between start and end
                    if (normalizedTime < startTimeProp.floatValue)
                    {
                        loopStartTimesProp.GetArrayElementAtIndex(loopStartTimesProp.arraySize - 1).floatValue = startTimeProp.floatValue;
                    }
                    if (normalizedTime >= endTimeProp.floatValue)
                    {
                        loopStartTimesProp.GetArrayElementAtIndex(loopStartTimesProp.arraySize - 1).floatValue = endTimeProp.floatValue - 0.1f;
                    }
                    break;
                    
                case 2: // End Time
                    endTimeProp.floatValue = normalizedTime;
                    // Ensure start time is before end time
                    if (startTimeProp.floatValue >= normalizedTime)
                    {
                        startTimeProp.floatValue = Mathf.Max(0f, normalizedTime - 0.1f);
                    }
                    // Ensure all loop start times are before end time
                    for (int i = 0; i < loopStartTimesProp.arraySize; i++)
                    {
                        if (loopStartTimesProp.GetArrayElementAtIndex(i).floatValue >= normalizedTime)
                        {
                            loopStartTimesProp.GetArrayElementAtIndex(i).floatValue = normalizedTime - 0.1f;
                        }
                    }
                    break;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
} 