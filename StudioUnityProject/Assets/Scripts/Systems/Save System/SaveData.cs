using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveData
{
	private static SaveData _current;
	public static SaveData Current
	{
		get
		{
			if (_current == null)
			{
				_current = new SaveData();
				if (SaveManager.current != null)
				{
					SaveManager.current.ForceSave();
				}
				else
				{
					Debug.LogError("Save Manager does not exsist. Cannot save any data!");
				}
			}

			return _current;
		}
		set
		{
			_current = value;
		}
	}

	//public PlayerProfile Profile;

	public float Sensitivity = 1f;
	public bool ToggleCrouch = false;

	public int CurrentLevel = 0;

	// keybinds
}
