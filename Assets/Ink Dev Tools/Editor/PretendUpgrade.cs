using System.IO;
using UnityEditor;
using UnityEngine;
using Ink.UnityIntegration;

// Package-author dev tool: simulates a 1.x -> 2.0 upgrade so the Ink Update window and its migrate
// prompt can be tested. Lives under Assets (never shipped in the package); not for end users.
public static class PretendUpgrade {
	const string legacyJsonPath = "Assets/Ink Dev Tools/LegacyStory.json";
	// Must match InkUnityIntegrationStartupWindow.editorPrefsKeyForVersionSeen.
	const string startupVersionSeenKey = "Ink Unity Integration Startup Window Version Confirmed";

	[MenuItem("Ink Test/Simulate Upgrade (show update window)")]
	static void SimulateUpgrade () {
		// 1. Recreate a leftover 1.x compiled .json so the migrate prompt has something to find.
		Directory.CreateDirectory("Assets/Ink Dev Tools");
		File.WriteAllText(legacyJsonPath,
			"{\"inkVersion\":21,\"root\":[[\"^Pretend upgrade story.\",\"\\n\",[\"done\",null],null],\"done\",null],\"listDefs\":{}}");
		AssetDatabase.Refresh();

		// 2. Refresh the migration cache so HasLegacyJson() reports true.
		InkMigrationTool.RecheckLegacyJson();

		// 3. Mark the update window as never-seen, so this launch is treated as an upgrade.
		EditorPrefs.SetInt(startupVersionSeenKey, -1);

		// 4. The window won't show if it's suppressed in settings.
		if (InkSettings.instance.suppressStartupWindow)
			Debug.LogWarning("Ink: 'Suppress Startup Window' is ON (Project Settings > Ink) — turn it off or the window won't auto-show on reload.");

		// Show it now — this is what the automatic on-load path does after an upgrade.
		InkUnityIntegrationStartupWindow.ShowWindow();
		Debug.Log("Ink: simulated upgrade. The Ink Update window should show the changelog + a migrate prompt.");
	}

	[MenuItem("Ink Test/Reset Upgrade Simulation")]
	static void ResetSimulation () {
		AssetDatabase.DeleteAsset(legacyJsonPath);
		InkMigrationTool.RecheckLegacyJson();
		Debug.Log("Ink: removed the simulated legacy .json.");
	}
}
