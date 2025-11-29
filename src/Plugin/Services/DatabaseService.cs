using Dapper;
using Microsoft.Extensions.Logging;

namespace K4Missions;

public sealed partial class Plugin
{
	/// <summary>
	/// Handles mission persistence in MySQL database
	/// </summary>
	public sealed class DatabaseService(string connectionName)
	{
		private readonly string _connectionName = connectionName;

		/// <summary>True if database is configured and ready</summary>
		public bool IsEnabled { get; private set; }

		/// <summary>
		/// Initialize database tables
		/// </summary>
		public async Task InitializeAsync()
		{
			try
			{
				const string sql = """
					CREATE TABLE IF NOT EXISTS `k4_missions` (
						`id` INT AUTO_INCREMENT PRIMARY KEY,
						`steamid64` BIGINT UNSIGNED NOT NULL,
						`event` VARCHAR(64) NOT NULL,
						`target` VARCHAR(64) NOT NULL,
						`amount` INT NOT NULL,
						`phrase` VARCHAR(255) NOT NULL,
						`reward_phrase` VARCHAR(255) NOT NULL,
						`reward_commands` TEXT NOT NULL,
						`progress` INT NOT NULL DEFAULT 0,
						`completed` BOOLEAN NOT NULL DEFAULT FALSE,
						`expires_at` DATETIME NULL,
						INDEX `idx_steamid` (`steamid64`),
						INDEX `idx_expires_at` (`expires_at`)
					) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				await connection.ExecuteAsync(sql);

				IsEnabled = true;
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to initialize database. Missions will not persist.");
				IsEnabled = false;
			}
		}

		/// <summary>
		/// Load player's missions from database
		/// </summary>
		public async Task<List<DbMission>> GetPlayerMissionsAsync(ulong steamId)
		{
			if (!IsEnabled)
				return [];

			try
			{
				const string sql = """
					SELECT `id` AS Id, `event` AS Event, `target` AS Target, `amount` AS Amount,
						   `phrase` AS Phrase, `reward_phrase` AS RewardPhrase, `reward_commands` AS RewardCommands,
						   `progress` AS Progress, `completed` AS Completed, `expires_at` AS ExpiresAt
					FROM `k4_missions`
					WHERE `steamid64` = @SteamId;
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				var results = await connection.QueryAsync<DbMission>(sql, new { SteamId = steamId });
				return results.ToList();
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to load missions for {SteamId}", steamId);
				return [];
			}
		}

		/// <summary>
		/// Add a new mission to player's record
		/// </summary>
		public async Task<int> AddMissionAsync(ulong steamId, PlayerMission mission, DateTime? expiresAt)
		{
			if (!IsEnabled)
				return -1;

			try
			{
				const string sql = """
					INSERT INTO `k4_missions` (`steamid64`, `event`, `target`, `amount`, `phrase`, `reward_phrase`, `reward_commands`, `progress`, `completed`, `expires_at`)
					VALUES (@SteamId, @Event, @Target, @Amount, @Phrase, @RewardPhrase, @RewardCommands, 0, FALSE, @ExpiresAt);
					SELECT LAST_INSERT_ID();
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				return await connection.ExecuteScalarAsync<int>(sql, new
				{
					SteamId = steamId,
					mission.Event,
					mission.Target,
					mission.Amount,
					mission.Phrase,
					mission.RewardPhrase,
					RewardCommands = string.Join("|", mission.RewardCommands),
					ExpiresAt = expiresAt
				});
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to add mission for {SteamId}", steamId);
				return -1;
			}
		}

		/// <summary>
		/// Batch update multiple missions
		/// </summary>
		public async Task UpdateMissionsAsync(IEnumerable<PlayerMission> missions)
		{
			if (!IsEnabled)
				return;

			var missionList = missions.Where(m => m.Id > 0).ToList();
			if (missionList.Count == 0)
				return;

			try
			{
				const string sql = """
					UPDATE `k4_missions`
					SET `progress` = @Progress, `completed` = @Completed
					WHERE `id` = @Id;
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				await connection.ExecuteAsync(sql, missionList.Select(m => new
				{
					m.Id,
					m.Progress,
					Completed = m.IsCompleted
				}));
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to batch update missions");
			}
		}

		/// <summary>
		/// Remove missions by IDs
		/// </summary>
		public async Task RemoveMissionsAsync(IEnumerable<int> missionIds)
		{
			if (!IsEnabled)
				return;

			var ids = missionIds.Where(id => id > 0).ToList();
			if (ids.Count == 0)
				return;

			try
			{
				const string sql = "DELETE FROM `k4_missions` WHERE `id` IN @Ids;";

				using var connection = Core.Database.GetConnection(_connectionName);
				await connection.ExecuteAsync(sql, new { Ids = ids });
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to remove missions");
			}
		}

		/// <summary>
		/// Mark a mission as completed
		/// </summary>
		public async Task<bool> CompleteMissionAsync(int missionId)
		{
			if (!IsEnabled || missionId <= 0)
				return false;

			try
			{
				const string sql = """
					UPDATE `k4_missions`
					SET `completed` = TRUE
					WHERE `id` = @Id AND `completed` = FALSE;
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				var affected = await connection.ExecuteAsync(sql, new { Id = missionId });
				return affected > 0;
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to complete mission {MissionId}", missionId);
				return false;
			}
		}

		/// <summary>
		/// Get all steam IDs with expired missions
		/// </summary>
		public async Task<List<ulong>> GetPlayersWithExpiredMissionsAsync()
		{
			if (!IsEnabled)
				return [];

			try
			{
				const string sql = """
					SELECT DISTINCT `steamid64`
					FROM `k4_missions`
					WHERE `expires_at` IS NOT NULL AND `expires_at` < NOW();
					""";

				using var connection = Core.Database.GetConnection(_connectionName);
				var results = await connection.QueryAsync<ulong>(sql);
				return results.ToList();
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to get players with expired missions");
				return [];
			}
		}

		/// <summary>
		/// Cleanup expired missions from database
		/// </summary>
		public async Task CleanupExpiredMissionsAsync()
		{
			if (!IsEnabled)
				return;

			try
			{
				const string sql = "DELETE FROM `k4_missions` WHERE `expires_at` IS NOT NULL AND `expires_at` < NOW();";

				using var connection = Core.Database.GetConnection(_connectionName);
				await connection.ExecuteAsync(sql);
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to cleanup expired missions");
			}
		}
	}

	/// <summary>
	/// Database mission record
	/// </summary>
	public sealed class DbMission
	{
		public int Id { get; set; }
		public string Event { get; set; } = string.Empty;
		public string Target { get; set; } = string.Empty;
		public int Amount { get; set; }
		public string Phrase { get; set; } = string.Empty;
		public string RewardPhrase { get; set; } = string.Empty;
		public string RewardCommands { get; set; } = string.Empty;
		public int Progress { get; set; }
		public bool Completed { get; set; }
		public DateTime? ExpiresAt { get; set; }

		public List<string> GetRewardCommandsList() => RewardCommands.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();
	}
}
