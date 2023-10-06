﻿namespace BomberMatch
{
	public sealed class Match
	{
		#region Consts

		private const int BompPlantingSign = 10;

		#endregion

		#region Fields

		private readonly Arena arena;
		private readonly Dictionary<string, IBomber> bombersByNames = new();
		private readonly uint matchActionsNumber;
		private readonly uint bombDetonationRadius;
		private readonly uint bombTimeToDetonate;

		#endregion

		#region Ctor

		public Match(
			Arena arena,
			IReadOnlyList<IBomber> bombers,
			uint matchActionsNumber,
			uint bombDetonationRadius,
			uint bombTimeToDetonate)
		{
			this.arena = arena;
			this.matchActionsNumber = matchActionsNumber;
			this.bombDetonationRadius = bombDetonationRadius;
			this.bombTimeToDetonate = bombTimeToDetonate;

			bombersByNames = bombers.ToDictionary(bomber => bomber.Name, bomber => bomber);

			foreach (var bomber in bombers)
			{
				arena.RespawnBomber(bomber.Name, bombDetonationRadius, bombTimeToDetonate);
			}
		}

		#endregion

		#region Public methods

		public string BombIt()
		{
			foreach (var bomber in bombersByNames.Values)
			{
				bomber.SetRules(
					matchActionsNumber: (int)matchActionsNumber,
					detonationRadius: (int)bombDetonationRadius,
					timeToDetonate: (int)bombTimeToDetonate);
			}

			for (var actionNumber = 0; actionNumber < matchActionsNumber; actionNumber++)
			{
				GetArenaState(out var arenaMatrix, out var aliveBombersPoints);

				foreach (var bomberName in arena.AliveBombers)
				{
					var bomber = GetBomber(bomberName);
				
					var bombersMatrix = CreateBombersMatrix(aliveBombersPoints, bomber.Name);
					var availableMoves = arena
						.GetAvailableBomberMoves(bomber.Name)
						.Select(ConvertDirectionToCode)
						.ToArray();

					var bomberActionCode = bomber.Go(arenaMatrix, bombersMatrix, availableMoves);

					if (bomberActionCode >= BompPlantingSign)
					{
						arena.PlantBomb(bomber.Name);
						bomberActionCode -= BompPlantingSign;
					}

					if (TryConvertCodeToDirection(bomberActionCode, out var direction))
					{
						arena.MoveBomber(bomber.Name, direction);
					}
				}

				arena.Flush();

				var aliveBombers = arena.AliveBombers;
				switch (aliveBombers.Count)
				{
					case 0:
						return $"Draw! No one survived [action #{actionNumber}]";
					case 1:
						return $"The winner is ... {aliveBombers[0]} [action #{actionNumber}]";
				}
			}

			return "Draw! Timeout";
		}

		#endregion

		#region Private methods

		private IBomber GetBomber(string bomberName)
		{
			if (!bombersByNames.TryGetValue(bomberName, out var bomber))
			{
				throw new InvalidOperationException($"Bomber '{bomberName}' not found");
			}
			return bomber;
		}

		private void GetArenaState(out int[,] matrix, out Dictionary<string, Point> aliveBombersPoints)
		{
			matrix = new int[arena.Fields.GetLength(0), arena.Fields.GetLength(1)];
			aliveBombersPoints = new Dictionary<string, Point>();

			for (var i = 0; i < matrix.GetLength(0); i++)
			{
				for (var j = 0; j < matrix.GetLength(1); j++)
				{
					var field = arena.Fields[i, j];
					if (field == null)
					{
						matrix[i, j] = -1;
					}
					else
					{
						if (field.HasBomb)
						{
							matrix[i, j] = (int)field.Bomb.TimeToDetonate;
						}
						else
						{
							matrix[i, j] = 0;
						}

						foreach (var bomber in field.Bombers)
						{
							if (bomber.IsAlive)
							{
								aliveBombersPoints.Add(bomber.Name, new Point { i = i, j = j });
							}
						}
					}
				}
			}
		}

		private int[,] CreateBombersMatrix(Dictionary<string, Point> bombersPoints, string mainBomberName)
		{
			var matrix = new int[bombersPoints.Count, 2];

			var mainBomberPoint = bombersPoints[mainBomberName];
			matrix[0, 0] = mainBomberPoint.i;
			matrix[0, 1] = mainBomberPoint.j;

			var rowIndex = 1;
			foreach (var bombersPoint in bombersPoints)
			{
				if (!string.Equals(bombersPoint.Key, mainBomberName))
				{
					matrix[rowIndex, 0] = bombersPoint.Value.i;
					matrix[rowIndex, 1] = bombersPoint.Value.j;
					rowIndex++;
				}
			}

			return matrix;
		}

		#endregion

		#region Helpers

		private static int ConvertDirectionToCode(Direction direction)
		{
			switch (direction)
			{
				case Direction.Up:
					return 1;

				case Direction.Down:
					return 2;

				case Direction.Left:
					return 3;

				case Direction.Right:
					return 4;

				default:
					throw new ArgumentOutOfRangeException(nameof(direction));
			}
		}

		private static bool TryConvertCodeToDirection(int directionCode, out Direction direction)
		{
			switch (directionCode)
			{
				case 1:
					direction = Direction.Up;
					return true;

				case 2:
					direction = Direction.Down;
					return true;

				case 3:
					direction = Direction.Left;
					return true;

				case 4:
					direction = Direction.Right;
					return true;

				default:
					direction = default(Direction);
					return false;
			}
		}

		#endregion

		#region Nested

		private struct Point
		{
			public int i;
			public int j;
		}

		#endregion
	}
}