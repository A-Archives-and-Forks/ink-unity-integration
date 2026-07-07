# Ink Player Window

The Ink Player editor window is a powerful debugging tool for `.ink` stories.

## Features

 - **Play your stories**: Play stories, just like in Inky! Auto-play can run the story on its own to surface loose ends — optionally from a fixed random seed, so you replay the exact same route. You can also search the history, save and load state, undo/redo, and filter which content is shown.

 - **Tether to live stories**: Link your stories at runtime for debugging!
 	 	
 - **Edit variables at runtime**: View and edit story variables, and observe them to be notified when they change.
 
 - **Run functions**: Call any ink function with arguments and see its return value and any text it produces.

 - **Divert**: Divert to any knot in the story.

 - **Profiler**: Provides performance statistics to help optimise your stories.
  
## Tethering

Tethering (or "attaching") links a `Story` running in your game to the Ink Player window at runtime, so you can watch its state, history and variables live — and edit variables to debug. The Demo scene shows this in action; here's the code.

### Attach a story from your own editor code

Call `InkPlayerWindow.Attach(story)` to open the window (if needed) and tether a running story:

```csharp
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEditor;

[CustomEditor(typeof(MyStoryBehaviour))]
public class MyStoryBehaviourEditor : Editor {
    public override void OnInspectorGUI () {
        base.OnInspectorGUI();
        var story = ((MyStoryBehaviour)target).story; // your runtime Ink.Runtime.Story
        if (story != null && GUILayout.Button("Attach to Ink Player"))
            InkPlayerWindow.Attach(story);
    }
}
```

### Attach automatically when the story is created

Expose an event when you build your `Story`, then attach to it from an `[InitializeOnLoad]` editor class (this is exactly what the demo's `BasicInkExampleEditor` does):

```csharp
[InitializeOnLoad]
public class MyStoryBehaviourEditor : Editor {
    static MyStoryBehaviourEditor () {
        MyStoryBehaviour.OnCreateStory += story => {
            InkPlayerWindow.GetWindow();       // open the window
            InkPlayerWindow.Attach(story);     // tether the running story
        };
    }
}
```

### An inspector field with attach/detach controls

`InkPlayerWindow.DrawStoryPropertyField` draws a labelled row with Attach/Detach + "Open Player Window" buttons (and expands to the story's live state in play mode):

```csharp
static bool storyExpanded;

public override void OnInspectorGUI () {
    base.OnInspectorGUI();
    var story = ((MyStoryBehaviour)target).story;
    InkPlayerWindow.DrawStoryPropertyField(story, ref storyExpanded, new GUIContent("Story"));
}
```

By default a tethered story lets you edit its variables (the game drives play and choices) — handy for debugging.
