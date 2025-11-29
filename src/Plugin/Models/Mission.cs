using System.Text.Json;

namespace K4Missions;

/// <summary>
/// Base mission definition loaded from missions.json
/// </summary>
public sealed class MissionDefinition
{
	public string Event { get; set; } = string.Empty;

	public Dictionary<string, JsonElement>? EventProperties { get; set; }

	public string Target { get; set; } = string.Empty;

	public List<string> RewardCommands { get; set; } = [];

	public int Amount { get; set; }

	public string Phrase { get; set; } = string.Empty;

	public string RewardPhrase { get; set; } = string.Empty;

	public string? Flag { get; set; }

	public string? MapName { get; set; }

	/// <summary>
	/// Creates a player mission instance from this definition
	/// </summary>
	public PlayerMission CreatePlayerMission(DateTime? expiresAt = null)
	{
		return new PlayerMission
		{
			Event = Event,
			Target = Target,
			Amount = Amount,
			Phrase = Phrase,
			RewardPhrase = RewardPhrase,
			RewardCommands = RewardCommands,
			EventProperties = EventProperties,
			MapName = MapName,
			Flag = Flag,
			Progress = 0,
			IsCompleted = false,
			ExpiresAt = expiresAt
		};
	}
}

/// <summary>
/// A mission assigned to a specific player with progress tracking
/// </summary>
public sealed class PlayerMission
{
	/// <summary>Database ID for this mission assignment</summary>
	public int Id { get; set; } = -1;

	/// <summary>Event type for this mission</summary>
	public string Event { get; set; } = string.Empty;

	/// <summary>Target field from the event</summary>
	public string Target { get; set; } = string.Empty;

	/// <summary>Amount required to complete</summary>
	public int Amount { get; set; }

	/// <summary>Phrase key for localization</summary>
	public string Phrase { get; set; } = string.Empty;

	/// <summary>Reward phrase key for localization</summary>
	public string RewardPhrase { get; set; } = string.Empty;

	/// <summary>Commands to execute on completion</summary>
	public List<string> RewardCommands { get; set; } = [];

	/// <summary>Event property filters</summary>
	public Dictionary<string, JsonElement>? EventProperties { get; set; }

	/// <summary>Map restriction</summary>
	public string? MapName { get; set; }

	/// <summary>Permission flag required</summary>
	public string? Flag { get; set; }

	/// <summary>Current progress towards completion</summary>
	public int Progress { get; set; }

	/// <summary>Whether this mission has been completed</summary>
	public bool IsCompleted { get; set; }

	/// <summary>When this mission expires (null for PerMap/Instant modes)</summary>
	public DateTime? ExpiresAt { get; set; }

	/// <summary>
	/// Checks if this mission matches the given event and target
	/// </summary>
	public bool Matches(string eventType, string target, string? currentMap, Dictionary<string, object?>? eventProperties)
	{
		if (IsCompleted)
			return false;

		if (!string.Equals(Event, eventType, StringComparison.OrdinalIgnoreCase) ||
			!string.Equals(Target, target, StringComparison.OrdinalIgnoreCase))
			return false;

		// Check map restriction
		if (MapName != null && MapName != currentMap)
			return false;

		// Check event properties if defined
		if (EventProperties != null && eventProperties != null)
		{
			if (!MatchesEventProperties(eventProperties))
				return false;
		}

		return true;
	}

	private bool MatchesEventProperties(Dictionary<string, object?> eventProperties)
	{
		if (EventProperties == null)
			return true;

		foreach (var (key, missionValue) in EventProperties)
		{
			if (!eventProperties.TryGetValue(key, out var eventValue) || eventValue == null)
				return false;

			if (!ComparePropertyValue(missionValue, eventValue))
				return false;
		}

		return true;
	}

	private static bool ComparePropertyValue(JsonElement missionValue, object eventValue)
	{
		return missionValue.ValueKind switch
		{
			JsonValueKind.True or JsonValueKind.False =>
				eventValue is bool eventBool && missionValue.GetBoolean() == eventBool,

			JsonValueKind.Number when missionValue.TryGetInt32(out var missionInt) =>
				eventValue switch
				{
					int eventInt => eventInt >= missionInt,
					byte eventByte => eventByte >= missionInt,
					_ => false
				},

			JsonValueKind.Number when missionValue.TryGetDouble(out var missionDouble) =>
				eventValue switch
				{
					double eventDouble => eventDouble >= missionDouble,
					float eventFloat => eventFloat >= missionDouble,
					int eventInt => eventInt >= missionDouble,
					_ => false
				},

			JsonValueKind.String when missionValue.GetString() is { } missionString =>
				eventValue is string eventString &&
				eventString.Contains(missionString, StringComparison.OrdinalIgnoreCase),

			_ => false
		};
	}
}
