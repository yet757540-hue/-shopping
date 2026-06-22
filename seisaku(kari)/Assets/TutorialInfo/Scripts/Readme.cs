using System;
using UnityEngine;

// Unity のチュートリアル表示用 Readme データを保持します。
public class Readme : ScriptableObject {
	public Texture2D icon;
	public string title;
	public Section[] sections;
	public bool loadedLayout;
	
	[Serializable]
	// Readme 内の各セクション情報です。
	public class Section {
		public string heading, text, linkText, url;
	}
}
