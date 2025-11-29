using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace K4Missions;

public sealed partial class Plugin
{
	/// <summary>
	/// Loads and manages mission definitions from missions.json
	/// </summary>
	public sealed class MissionLoader
	{
		private readonly List<MissionDefinition> _missions = [];

		/// <summary>
		/// Load missions from missions.json file
		/// </summary>
		public void LoadFromFile(string moduleDirectory)
		{
			var filePath = Path.Combine(moduleDirectory, "missions.json");

			if (!File.Exists(filePath))
			{
				// Copy from resources if not exists
				var resourcePath = Path.Combine(moduleDirectory, "resources", "missions.json");
				if (File.Exists(resourcePath))
				{
					File.Copy(resourcePath, filePath);
					Core.Logger.LogInformation("Copied default missions.json from resources.");
				}
				else
				{
					Core.Logger.LogError("missions.json not found and no default in resources.");
					return;
				}
			}

			try
			{
				var jsonString = File.ReadAllText(filePath);
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true,
					ReadCommentHandling = JsonCommentHandling.Skip
				};

				var loadedMissions = JsonSerializer.Deserialize<List<MissionDefinition>>(jsonString, options);

				if (loadedMissions == null || loadedMissions.Count == 0)
				{
					Core.Logger.LogError("No missions found in missions.json");
					return;
				}

				_missions.Clear();
				_missions.AddRange(loadedMissions);

				Core.Logger.LogInformation("Loaded {Count} missions from configuration.", _missions.Count);
			}
			catch (JsonException ex)
			{
				Core.Logger.LogError(ex, "Failed to parse missions.json");
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to load missions.json");
			}
		}

		/// <summary>
		/// Get all loaded missions
		/// </summary>
		public IReadOnlyList<MissionDefinition> GetAllMissions() => _missions;

		/// <summary>
		/// Get all missions that match a specific event type
		/// </summary>
		public IEnumerable<MissionDefinition> GetByEvent(string eventType)
		{
			return _missions.Where(m => m.Event == eventType);
		}

		/// <summary>
		/// Get missions available to a player (respecting flag restrictions)
		/// </summary>
		public IEnumerable<MissionDefinition> GetAvailableMissions(MissionPlayer player, Func<string, bool> hasPermission)
		{
			return _missions.Where(m => m.Flag == null || hasPermission(m.Flag));
		}
	}
}
