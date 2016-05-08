
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
// using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Color = SwinGameSDK.Color;

/// <summary>
/// Player has its own _PlayerGrid, and can see an _EnemyGrid, it can also check if
/// all ships are deployed and if all ships are detroyed. A Player can also attach.
/// </summary>
public class Player : IEnumerable<Ship>
{
	private static readonly Regex _VALID_CODE = new Regex("^#([0-9a-f]{3}|[0-9a-f]{6})$",RegexOptions.IgnoreCase);
	/// <summary>
	/// Provides a random number generator.
	/// </summary>
	protected static Random _Random = new Random();
	private Dictionary<ShipName, Ship> _Ships = new Dictionary<ShipName, Ship>();
	private SeaGrid _playerGrid;
	private ISeaGrid _enemyGrid;

	/// <summary>
	/// Provides access to the current game.
	/// </summary>
	protected BattleShipsGame _game;
	private int _shots;
	private int _hits;

	private int _misses;
	private readonly Color _col;

	/// <summary>
	/// Gets the color of this player's turn indicator light.
	/// </summary>
	/// <value>The turn indicator.</value>
	public virtual Color TurnIndicator {
		get {
			return _col;
		}
	}

	private static byte HexCharLookup(char digit) {
		// For some reason .NET doesn't have this built-in, unless you cast to an Int32 first
		byte res;
		switch (digit) {
			case '0':
				res = 0;
				break;
			case '1':
				res = 1;
				break;
			case '2':
				res = 2;
				break;
			case '3':
				res = 3;
				break;
			case '4':
				res = 4;
				break;
			case '5':
				res = 5;
				break;
			case '6':
				res = 6;
				break;
			case '7':
				res = 7;
				break;
			case '8':
				res = 8;
				break;
			case '9':
				res = 9;
				break;
			case 'A':
			case 'a':
				res = 10;
				break;
			case 'B':
			case 'b':
				res = 11;
				break;
			case 'C':
			case 'c':
				res = 12;
				break;
			case 'D':
			case 'd':
				res = 13;
				break;
			case 'E':
			case 'e':
				res = 14;
				break;
			case 'F':
			case 'f':
				res = 15;
				break;
			default:
				throw new ArgumentOutOfRangeException("digit","Passed char must be a valid hexadecimal digit (0-9 A-F a-f).");
		}
		return res;
	}
	private byte LoadShorthand(char digit) {
		byte d = HexCharLookup(digit);
		return (byte)((d << 4) | d);
	}
	private byte LoadLonghand(char leadDigit,char trailingDigit) {
		byte d1 = HexCharLookup(leadDigit);
		byte d2 = HexCharLookup(trailingDigit);
		return (byte)((d1 << 4) | d2);
	}
	/// <summary>
	/// Generates a SwinGame Color based on the given color code.
	/// </summary>
	/// <param name="code">The color to generate.</param>
	protected Color GetColor(string code) {
		Color res;
		Match m = _VALID_CODE.Match(code);
		if (m.Success) {
			bool shorthand = code.Length == 4;
			byte r = shorthand ? LoadShorthand(code[1]) : LoadLonghand(code[1], code[2]);
			byte g = shorthand ? LoadShorthand(code[2]) : LoadLonghand(code[3], code[4]);
			byte b = shorthand ? LoadShorthand(code[3]) : LoadLonghand(code[5], code[6]);
			byte a = 255;
			res = Color.FromArgb((a << 24) + (r << 16) + (g << 8) + b);
		} else {
			res = Color.Transparent;
		}
		return res;
	}


	/// <summary>
	/// Returns the game that the player is part of.
	/// </summary>
	/// <value>The game</value>
	/// <returns>The game that the player is playing</returns>
	public BattleShipsGame Game {
		get { return _game; }
		set { _game = value; }
	}

	/// <summary>
	/// Sets the grid of the enemy player
	/// </summary>
	/// <value>The enemy's sea grid</value>
	public ISeaGrid Enemy {
		set { _enemyGrid = value; }
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Player"/> class.
	/// </summary>
	/// <param name="controller">The current game.</param>
	public Player(BattleShipsGame controller)
	{
		_game = controller;
    _playerGrid = new SeaGrid(_Ships);

		//for each ship add the ships name so the seagrid knows about them
		foreach (ShipName name in Enum.GetValues(typeof(ShipName))) {
			if (name != ShipName.None) {
				_Ships.Add(name, new Ship(name));
			}
		}
		_col = GetColor("#4ac925");

		RandomizeDeployment();
	}

	/// <summary>
	/// The EnemyGrid is a ISeaGrid because you shouldn't be allowed to see the enemies ships
	/// </summary>
	public ISeaGrid EnemyGrid {
		get { return _enemyGrid; }
		set { _enemyGrid = value; }
	}

	/// <summary>
	/// The PlayerGrid is just a normal SeaGrid where the players ships can be deployed and seen
	/// </summary>
	public SeaGrid PlayerGrid {
		get { return _playerGrid; }
	}

	/// <summary>
	/// ReadyToDeploy returns true if all ships are deployed
	/// </summary>
	public bool ReadyToDeploy {
		get { return _playerGrid.AllDeployed; }
	}

	/// <summary>
	/// Gets a value indicating whether all of the player's ships have been destroyed.
	/// </summary>
	/// <value><c>true</c> if all ships are destroyed; otherwise, <c>false</c>.</value>
	public bool IsDestroyed {
//Check if all ships are destroyed... -1 for the none ship
		get { return _playerGrid.ShipsKilled == Enum.GetValues(typeof(ShipName)).Length - 1; }
	}

	/// <summary>
	/// Returns the Player's ship with the given name.
	/// </summary>
	/// <param name="name">the name of the ship to return</param>
	/// <value>The ship</value>
	/// <returns>The ship with the indicated name</returns>
	/// <remarks>The none ship returns nothing/null</remarks>
	public Ship Ship(ShipName name) {
		if (name == ShipName.None)
			return null;

		return _Ships[name];
	}

	/// <summary>
	/// The number of shots the player has made
	/// </summary>
	/// <value>shots taken</value>
	/// <returns>teh number of shots taken</returns>
	public int Shots {
		get { return _shots; }
	}

	/// <summary>
	/// Gets the number of hits.
	/// </summary>
	public int Hits {
		get { return _hits; }
	}

	/// <summary>
	/// Total number of shots that missed
	/// </summary>
	/// <value>miss count</value>
	/// <returns>the number of shots that have missed ships</returns>
	public int Missed {
		get { return _misses; }
	}

	/// <summary>
	/// The current score.
	/// </summary>
	public int Score {
		get {
			if (IsDestroyed) {
				return 0;
			} else {
				return (Hits * 12) - Shots - (PlayerGrid.ShipsKilled * 20);
			}
		}
	}

	/// <summary>
	/// Makes it possible to enumerate over the ships the player
	/// has.
	/// </summary>
	/// <returns>A Ship enumerator</returns>
	public IEnumerator<Ship> GetShipEnumerator()
	{
		Ship[] result = new Ship[_Ships.Values.Count + 1];
		_Ships.Values.CopyTo(result, 0);
		List<Ship> lst = new List<Ship>();
		lst.AddRange(result);

		return lst.GetEnumerator();
	}
	/// <summary>
	/// Gets the enumerator.
	/// </summary>
	/// <returns>The enumerator.</returns>
	IEnumerator<Ship> IEnumerable<Ship>.GetEnumerator()
	{
		return GetShipEnumerator();
	}

	/// <summary>
	/// Makes it possible to enumerate over the ships the player
	/// has.
	/// </summary>
	/// <returns>A Ship enumerator</returns>
	public IEnumerator GetEnumerator()
	{
		Ship[] result = new Ship[_Ships.Values.Count + 1];
		_Ships.Values.CopyTo(result, 0);
		List<Ship> lst = new List<Ship>();
		lst.AddRange(result);

		return lst.GetEnumerator();
	}

	/// <summary>
	/// Vitual Attack allows the player to shoot
	/// </summary>
	public virtual AttackResult Attack()
	{
		//human does nothing here...
		return null;
	}

	/// <summary>
	/// Shoot at a given row/column
	/// </summary>
	/// <param name="row">the row to attack</param>
	/// <param name="col">the column to attack</param>
	/// <returns>the result of the attack</returns>
	internal AttackResult Shoot(int row, int col)
	{
		
		AttackResult result = default(AttackResult);
		result = EnemyGrid.HitTile(row, col);

		switch (result.Value) {
			case ResultOfAttack.Destroyed:
			case ResultOfAttack.Hit:
                _shots += 1;
				_hits += 1;
				break;
			case ResultOfAttack.Miss:
                _shots += 1;
				_misses += 1;
				break;
		}

		return result;
	}

	/// <summary>
	/// Ramdomizes the positioning of this player's ships.
	/// </summary>
	public virtual void RandomizeDeployment()
	{
		bool placementSuccessful = false;
		Direction heading = default(Direction);

		//for each ship to deploy in shipist

		foreach (ShipName shipToPlace in Enum.GetValues(typeof(ShipName))) {
			if (shipToPlace == ShipName.None)
				continue;

			placementSuccessful = false;

			//generate random position until the ship can be placed
			do {
				int dir = _Random.Next(2);
				int x = _Random.Next(0, 11);
				int y = _Random.Next(0, 11);


				if (dir == 0) {
					heading = Direction.UpDown;
				} else {
					heading = Direction.LeftRight;
				}

				//try to place ship, if position unplaceable, generate new coordinates
				try {
					PlayerGrid.MoveShip(x, y, shipToPlace, heading);
					placementSuccessful = true;
				} catch {
					placementSuccessful = false;
				}
			} while (!placementSuccessful);
		}
	}
}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
