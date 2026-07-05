using System.IO;
using UnityEditor;
using UnityEngine;

/* 
* This script allows you to set custom icons for folders in project browser.
* Recommended icon sizes - small: 16x16 px, large: 64x64 px;
*/

namespace Ink.UnityIntegration {
	[InitializeOnLoad]
	public class InkBrowserIcons {
		private static bool isRetina {
			get {
				float unityVersion = float.Parse(Application.unityVersion.Substring (0, 3));
				return Application.platform == RuntimePlatform.OSXEditor && unityVersion >= 5.4f;
			}
		}
	    private const float largeIconSize = 64f;

		private static Texture2D _inkFileIcon;
		public static Texture2D inkFileIcon {
			get {
				if(_inkFileIcon == null) {
					if(isRetina) {
						_inkFileIcon = Resources.Load<Texture2D>("InkFileIcon-retina");
					} else {
						_inkFileIcon = Resources.Load<Texture2D>("InkFileIcon");
					}
				}
				return _inkFileIcon;
			}
		}
		private static Texture2D _inkFileIconLarge;
		public static Texture2D inkFileIconLarge {
			get {
				if(_inkFileIconLarge == null) {
					_inkFileIconLarge = Resources.Load<Texture2D>("InkFileIcon-large");
				}
				return _inkFileIconLarge;
			}
		}
		private static Texture2D _errorIcon;
		public static Texture2D errorIcon {
			get {
				if(_errorIcon == null) {
					_errorIcon = Resources.Load<Texture2D>("InkErrorIcon");
				}
				return _errorIcon;
			}
		}
		private static Texture2D _warningIcon;
		public static Texture2D warningIcon {
			get {
				if(_warningIcon == null) {
					_warningIcon = Resources.Load<Texture2D>("InkWarningIcon");
				}
				return _warningIcon;
			}
		}
		private static Texture2D _todoIcon;
		public static Texture2D todoIcon {
			get {
				if(_todoIcon == null) {
					_todoIcon = Resources.Load<Texture2D>("InkTodoIcon");
				}
				return _todoIcon;
			}
		}
		private static Texture2D _manualIcon;
		public static Texture2D manualIcon {
			get {
				if(_manualIcon == null) {
					_manualIcon = Resources.Load<Texture2D>("InkCompileManualIcon");
				}
				return _manualIcon;
			}
		}
		private static Texture2D _childIcon;
		public static Texture2D childIcon {
			get {
				if(_childIcon == null) {
					_childIcon = Resources.Load<Texture2D>("InkChildIcon");
				}
				return _childIcon;
			}
		}
		private static Texture2D _childIconLarge;
		public static Texture2D childIconLarge {
			get {
				if(_childIconLarge == null) {
					_childIconLarge = Resources.Load<Texture2D>("InkChildIcon-Large");
				}
				return _childIconLarge;
			}
		}
		private static Texture2D _unknownFileIcon;
		public static Texture2D unknownFileIcon {
			get {
				if(_unknownFileIcon == null) {
					_unknownFileIcon = Resources.Load<Texture2D>("InkUnknownFileIcon");
				}
				return _unknownFileIcon;
			}
		}

	}
}