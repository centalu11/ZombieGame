# Modified Third Party Files

## StarterAssets Package

### Assets/StarterAssets/InputSystem/StarterAssets.inputactions

Added "Interact" action to the Player action map:

```json
{
    "name": "Interact",
    "type": "Button",
    "id": "9ee25f4d-5c82-4e3d-8b9a-5eedc8b2a085",
    "expectedControlType": "Button",
    "processors": "",
    "interactions": ""
}
```

Added binding for the Interact action:

```json
{
    "name": "",
    "id": "7d76c96d-1ef2-4c74-8c46-4a8d65e9d3f6",
    "path": "<Keyboard>/f",
    "interactions": "",
    "processors": "",
    "groups": "KeyboardMouse",
    "action": "Interact",
    "isComposite": false,
    "isPartOfComposite": false
}
```

Location: Added to the "actions" array in the "Player" action map, and its binding added to the "bindings" array.

Reason for modification: Added Interact action for car entry/exit functionality without creating a separate input system. 