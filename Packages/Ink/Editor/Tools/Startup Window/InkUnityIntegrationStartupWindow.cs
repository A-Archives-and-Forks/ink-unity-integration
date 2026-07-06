using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ink.UnityIntegration {
	/// <summary>
	/// The "what's new" window shown once after the plugin updates (unless suppressed in Ink settings).
	/// Displays the changelog; also opened via the "Show changelog" button in Project Settings ▸ Ink.
	/// </summary>
	[InitializeOnLoad]
	public class InkUnityIntegrationStartupWindow : EditorWindow {
		const string editorPrefsKeyForVersionSeen = "Ink Unity Integration Startup Window Version Confirmed";
		const int announcementVersion = 2;

		static int announcementVersionPreviouslySeen;
		static string changelogText;

		static InkUnityIntegrationStartupWindow () {
			EditorApplication.delayCall += TryCreateWindow;
		}

		static void TryCreateWindow () {
			if (InkSettings.instance.suppressStartupWindow) return;
			announcementVersionPreviouslySeen = EditorPrefs.GetInt(editorPrefsKeyForVersionSeen, -1);
			if (announcementVersion != announcementVersionPreviouslySeen) {
				ShowWindow();
			}
		}

		public static void ShowWindow () {
			var window = GetWindow(typeof(InkUnityIntegrationStartupWindow), true, "Ink Update " + InkEditorUtils.unityIntegrationVersionCurrent, true) as InkUnityIntegrationStartupWindow;
			window.minSize = new Vector2(200, 200);
			var size = new Vector2(520, 320);
			window.position = new Rect((Screen.currentResolution.width - size.x) * 0.5f, (Screen.currentResolution.height - size.y) * 0.5f, size.x, size.y);
			EditorPrefs.SetInt(editorPrefsKeyForVersionSeen, announcementVersion);
		}

		void OnEnable () {
			var packageDirectory = InkEditorUtils.FindAbsolutePluginDirectory();
			if (packageDirectory != null) {
				var changelogPath = Path.Combine(packageDirectory, "CHANGELOG.md");
				if (File.Exists(changelogPath)) changelogText = File.ReadAllText(changelogPath);
			}
		}

		void CreateGUI () {
			var root = rootVisualElement;
			root.style.paddingTop = 10;
			root.style.paddingLeft = 10;
			root.style.paddingRight = 10;
			root.style.paddingBottom = 10;

			if (InkEditorUtils.inkLogoIcon != null) {
				var logo = new Image { image = InkEditorUtils.inkLogoIcon, scaleMode = ScaleMode.ScaleToFit };
				logo.style.height = 80;
				logo.style.marginBottom = 4;
				root.Add(logo);
			}
			root.Add(CenteredGrey("Version " + InkEditorUtils.unityIntegrationVersionCurrent));
			root.Add(CenteredGrey("Ink version " + InkEditorUtils.inkVersionCurrent));

			if (announcementVersionPreviouslySeen == -1) {
				var newToInk = new Label("New to ink?");
				newToInk.style.unityFontStyleAndWeight = FontStyle.Bold;
				newToInk.style.marginTop = 6;
				root.Add(newToInk);
			}

			var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.inklestudios.com/ink/")) { text = "About Ink" }));
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://www.patreon.com/inkle")) { text = "❤️ Support Us! ❤️" }));
			buttons.Add(Grow(new Button(() => Application.OpenURL("https://discord.gg/inkle")) { text = "Discord Community + Support" }));
			buttons.Add(Grow(new Button(Close) { text = "Close" }));
			root.Add(buttons);

			if (!string.IsNullOrEmpty(changelogText)) {
				var scroll = new ScrollView { style = { flexGrow = 1, marginTop = 8 } };
				foreach (var section in Regex.Split(changelogText, "## ")) {
					if (string.IsNullOrWhiteSpace(section)) continue;
					var lines = section.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					var box = new VisualElement { style = { marginBottom = 6 } };
					var version = new Label(lines[0]);
					version.style.unityFontStyleAndWeight = FontStyle.Bold;
					box.Add(version);
					for (int i = 1; i < lines.Length; i++) {
						var bullet = new Label("• " + lines[i].TrimStart('-').TrimStart(' '));
						bullet.style.whiteSpace = WhiteSpace.Normal;
						box.Add(bullet);
					}
					scroll.Add(box);
				}
				root.Add(scroll);
			}
		}

		static Label CenteredGrey (string text) {
			var label = new Label(text);
			label.style.unityTextAlign = TextAnchor.MiddleCenter;
			label.style.color = new Color(0.5f, 0.5f, 0.5f);
			label.style.fontSize = 10;
			return label;
		}

		static T Grow<T> (T element) where T : VisualElement {
			element.style.flexGrow = 1;
			return element;
		}
	}
}
