using Dommel;
using K4Missions.Database.Migrations;
using Microsoft.Extensions.Logging;

namespace K4Missions;

public sealed partial class Plugin
{
	/// <summary>
	/// Handles mission persistence in database (MySQL, PostgreSQL, SQLite)
	/// </summary>
	public sealed class DatabaseService(string connectionName)
	{
		private readonly string _connectionName = connectionName;

		internal const string TableName = "k4_missions";

		/// <summary>True if database is configured and ready</summary>
		public bool IsEnabled { get; private set; }

		/// <summary>
		/// Initialize database tables using FluentMigrator
		/// </summary>
		public async Task InitializeAsync()
		{
			try
			{
				// Run FluentMigrator migrations
				using var connection = Core.Database.GetConnection(_connectionName);
				MigrationRunner.RunMigrations(connection);

				IsEnabled = true;
				Core.Logger.LogInformation("Database initialized with migrations. Table: {Table}", TableName);
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				var results = await connection.SelectAsync<DbMission>(m => m.SteamId64 == (long)steamId);
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
				var dbMission = DbMission.FromPlayerMission(steamId, mission, expiresAt);

				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				var id = await connection.InsertAsync(dbMission);
				return Convert.ToInt32(id);
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				foreach (var mission in missionList)
				{
					var dbMission = await connection.GetAsync<DbMission>(mission.Id);
					if (dbMission != null)
					{
						dbMission.Progress = mission.Progress;
						dbMission.Completed = mission.IsCompleted;
						await connection.UpdateAsync(dbMission);
					}
				}
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				await connection.DeleteMultipleAsync<DbMission>(m => ids.Contains(m.Id));
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				var dbMission = await connection.GetAsync<DbMission>(missionId);
				if (dbMission == null || dbMission.Completed)
					return false;

				dbMission.Completed = true;
				await connection.UpdateAsync(dbMission);
				return true;
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				var expiredMissions = await connection.SelectAsync<DbMission>(m => m.ExpiresAt != null && m.ExpiresAt < DateTime.UtcNow);
				return expiredMissions.Select(m => (ulong)m.SteamId64).Distinct().ToList();
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
				using var connection = Core.Database.GetConnection(_connectionName);
				connection.Open();

				await connection.DeleteMultipleAsync<DbMission>(m => m.ExpiresAt != null && m.ExpiresAt < DateTime.UtcNow);
			}
			catch (Exception ex)
			{
				Core.Logger.LogError(ex, "Failed to cleanup expired missions");
			}
		}
	}
}
