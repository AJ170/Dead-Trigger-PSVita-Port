using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

// Unity's Development PS Vita player on a retail (HENkaku) console. The player calls devkit-only libraries (Razor HUD
// and capture, Razor CPU markers, Deci4p); on a retail console those imports point at address 0 and the player dies in
// GfxDeviceGXM::InitializeGXM right after launch. Each of those calls in the built player becomes "movs r0, #value"
// (the result the player then sees), from the tables in Tools/devplayer (make_tables.py explains them).
public static class VitaDevPlayer
{
	private const string TableFolder = "Tools/devplayer";

	private const string ConnectMenu = "Tools/Vita Dev Player/Connect To Editor Profiler";

	private const string ConnectPref = "VitaDevPlayer.ConnectToEditor";

	private class Table
	{
		public string Player;
		public string Original;
		public string Patched;
		public readonly List<KeyValuePair<long, byte[]>> Sites = new List<KeyValuePair<long, byte[]>>();
	}

	// Off by default: with no editor listening, a player that connects retries every few seconds and never starts.
	[MenuItem(ConnectMenu)]
	private static void ToggleConnect()
	{
		EditorPrefs.SetBool(ConnectPref, !EditorPrefs.GetBool(ConnectPref, false));
	}

	[MenuItem(ConnectMenu, true)]
	private static bool ToggleConnectCheck()
	{
		Menu.SetChecked(ConnectMenu, EditorPrefs.GetBool(ConnectPref, false));
		return true;
	}

	[PostProcessBuild(1000)]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		if (target != BuildTarget.PSP2 || !EditorUserBuildSettings.development)
		{
			return;
		}
		string folder = Directory.Exists(pathToBuiltProject) ? pathToBuiltProject : Path.GetDirectoryName(pathToBuiltProject);
		if (Patch(folder) && EditorPrefs.GetBool(ConnectPref, false))
		{
			ConnectToEditor(folder);
		}
	}

	// Patches the player (.self) at the root of a Development build folder. False, with the reason logged, when it is
	// neither a Unity Development player in Tools/devplayer nor one already patched.
	private static bool Patch(string buildFolder)
	{
		string[] players = Directory.GetFiles(buildFolder, "*.self", SearchOption.TopDirectoryOnly);
		if (players.Length != 1)
		{
			Debug.LogError(string.Format("[VitaDevPlayer] {0} players (.self) in {1}, expected 1", players.Length, buildFolder));
			return false;
		}
		string path = players[0];
		string hash = Sha256(path);
		foreach (Table table in LoadTables())
		{
			if (hash == table.Patched)
			{
				Debug.Log("[VitaDevPlayer] " + path + " (" + table.Player + ") is already patched for retail consoles");
				return true;
			}
			if (hash != table.Original)
			{
				continue;
			}
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Write))
			{
				foreach (KeyValuePair<long, byte[]> site in table.Sites)
				{
					stream.Position = site.Key;
					stream.Write(site.Value, 0, site.Value.Length);
				}
			}
			if (Sha256(path) != table.Patched)
			{
				Debug.LogError("[VitaDevPlayer] " + path + " does not match the patched " + table.Player + " after patching");
				return false;
			}
			Debug.Log(string.Format("[VitaDevPlayer] {0} ({1}) patched for retail consoles, {2} devkit calls", path, table.Player, table.Sites.Count));
			return true;
		}
		Debug.LogError("[VitaDevPlayer] " + path + " is not a Unity Development player listed in " + TableFolder + ", not patched");
		return false;
	}

	// The editor could not reach a retail Vita's listening player, so the player connects to this PC's editor instead.
	private static void ConnectToEditor(string buildFolder)
	{
		string[] configs = Directory.GetFiles(buildFolder, "boot.config", SearchOption.AllDirectories);
		if (configs.Length == 0)
		{
			Debug.LogError("[VitaDevPlayer] no boot.config in " + buildFolder + ", the player will not connect to the editor");
			return;
		}
		foreach (string config in configs)
		{
			string text = File.ReadAllText(config);
			File.WriteAllText(config, text.Replace("player-connection-mode=Listen", "player-connection-mode=Connect"));
			Debug.Log("[VitaDevPlayer] connects to the editor's profiler (" + config + ")");
		}
	}

	private static List<Table> LoadTables()
	{
		List<Table> tables = new List<Table>();
		foreach (string file in Directory.GetFiles(TableFolder, "*.txt"))
		{
			Table table = new Table();
			foreach (string line in File.ReadAllLines(file))
			{
				if (line.Length == 0 || line[0] == '#')
				{
					continue;
				}
				string[] fields = line.Split(' ');
				switch (fields[0])
				{
					case "player": table.Player = fields[1]; break;
					case "size": break;
					case "original": table.Original = fields[1]; break;
					case "patched": table.Patched = fields[1]; break;
					default:
						// file offset, original bytes, patched bytes, import, caller
						table.Sites.Add(new KeyValuePair<long, byte[]>(Convert.ToInt64(fields[0], 16), Hex(fields[2])));
						break;
				}
			}
			tables.Add(table);
		}
		return tables;
	}

	private static byte[] Hex(string text)
	{
		byte[] bytes = new byte[text.Length / 2];
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = Convert.ToByte(text.Substring(i * 2, 2), 16);
		}
		return bytes;
	}

	private static string Sha256(string path)
	{
		using (SHA256 sha = SHA256.Create())
		using (FileStream stream = File.OpenRead(path))
		{
			return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
		}
	}
}
