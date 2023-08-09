using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Barmetler.RoadSystem
{
	public static class RoadSystemSettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateRoadSystemSettingsProvider()
		{
			var settings = RoadSystemSettings.SerializedInstance;

			var names = new HashSet<string>();

			var prop = settings.GetIterator();
			while (prop.NextVisible(true))
			{
				if (prop.name != "m_Script")
					names.Add(prop.displayName);
			}

			return new SettingsProvider("Project/MBRoadSystemSettings", SettingsScope.Project)
			{
				label = "MB RoadSystem",
				guiHandler = (searchContext) =>
				{
					var settings = RoadSystemSettings.SerializedInstance;
					var prop = settings.GetIterator();

					prop.NextVisible(true);
					do
					{
						if (prop.name != "m_Script")
							EditorGUILayout.PropertyField(prop, new GUIContent(prop.displayName));
					} while (prop.NextVisible(false));

					settings.ApplyModifiedProperties();
				},

				keywords = names,
			};
		}
	}
}
