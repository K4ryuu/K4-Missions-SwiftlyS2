using SwiftlyS2.Shared.Players;

namespace K4Missions;

/// <summary>
/// Represents a player with their assigned missions
/// </summary>
public sealed class MissionPlayer
{
	public required ulong SteamId { get; init; }
	public required IPlayer Player { get; init; }

	/// <summary>List of currently assigned missions</summary>
	public List<PlayerMission> Missions { get; } = [];

	/// <summary>Whether player data has been loaded from database</summary>
	public bool IsLoaded { get; set; }

	/// <summary>Whether this player has VIP status</summary>
	public bool IsVip { get; set; }

	/// <summary>
	/// Gets whether all missions are completed
	/// </summary>
	public bool AllMissionsCompleted => Missions.Count > 0 && Missions.All(m => m.IsCompleted);

	/// <summary>
	/// Checks if the player is valid and connected
	/// </summary>
	public bool IsValid => Player.IsValid && !Player.IsFakeClient;
}
