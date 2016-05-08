using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Color = SwinGameSDK.Color;

/// <summary>
/// The AIEasyPlayer is a type of AIPlayer that will always randomly fire, regardless
/// of if it has hit a ship
/// </summary>

public class AIEasyPlayer : AIPlayer 
{
	/// <summary>
	/// Private enumarator for AI states. currently there is one state
	/// </summary>
	private enum AIStates
	{
		Searching
	}

	private AIStates _CurrentState = AIStates.Searching;

	private readonly Color _col;

	/// <summary>
	/// Initializes a new instance of an AIEasyPlayer
	/// </summary>
	/// <value>A new AIEasyPlayer</value>
	/// <returns>A new AIEasyPlayer</returns>
	/// <param name="game">The game that this AIEasyPlayer is a player in.</param>
	public AIEasyPlayer(BattleShipsGame game) : base(game)
	{
		_col = GetColor("#005682");
	}

	/// <summary>
	/// Gets the color of this player's turn indicator light.
	/// </summary>
	public override Color TurnIndicator {
		get {
			return _col;
		}
	}

	/// <summary>
	/// GenerateCoords will call upon the right methods to generate the appropriate shooting
	/// coordinates
	/// </summary>
	/// <param name="row">the row that will be shot at</param>
	/// <param name="column">the column that will be shot at</param>
	protected override void GenerateCoords(ref int row, ref int column)
	{
		do {
			//always search
			switch (_CurrentState) {
			case AIStates.Searching:
				SearchCoords(ref row, ref column);
				break;
			default:
				throw new ApplicationException("AI has gone in an invalid state");
			}
		} while ((row < 0 || column < 0 || row >= EnemyGrid.Height || column >= EnemyGrid.Width || EnemyGrid[row, column] != TileView.Sea));
		//while inside the grid and not a sea tile do the search
	}

	/// <summary>
	/// SearchCoords will randomly generate shots within the grid as long as its not hit that tile already
	/// </summary>
	/// <param name="row">the generated row</param>
	/// <param name="column">the generated column</param>
	private void SearchCoords(ref int row, ref int column)
	{
		row = _Random.Next(0, EnemyGrid.Height);
		column = _Random.Next(0, EnemyGrid.Width);
	}

	protected override void ProcessShot(int row, int col, AttackResult result)
	{
	}
}