
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Data;
using System.Diagnostics;
using SwinGameSDK;


/// <summary>
/// The DeploymentController controls the players actions
/// during the deployment phase.
/// </summary>
static class DeploymentController
{
	private const int SHIPS_TOP = 98;
	private const int SHIPS_LEFT = 20;
	private const int SHIPS_HEIGHT = 90;

	private const int SHIPS_WIDTH = 300;
	private const int TOP_BUTTONS_TOP = 72;

	private const int TOP_BUTTONS_HEIGHT = 46;
	private const int PLAY_BUTTON_LEFT = 693;

	private const int PLAY_BUTTON_WIDTH = 80;
	private const int UP_DOWN_BUTTON_LEFT = 410;

	private const int LEFT_RIGHT_BUTTON_LEFT = 350;
	private const int RANDOM_BUTTON_LEFT = 547;

	private const int RANDOM_BUTTON_WIDTH = 51;

	private const int DIR_BUTTONS_WIDTH = 47;

	private const int TEXT_OFFSET = 5;
	private static Direction _currentDirection = Direction.UpDown;

	private static ShipName _selectedShip = ShipName.Tug;

	/// <summary>
	/// Handles user input for the Deployment phase of the game.
	/// </summary>
	/// <remarks>
	/// Involves selecting the ships, deloying ships, changing the direction
	/// of the ships to add, randomising deployment, end then ending
	/// deployment
	/// </remarks>
	public static void HandleDeploymentInput() {
		if (SwinGame.KeyTyped(KeyCode.vk_ESCAPE)) {
			GameController.AddNewState(GameState.ViewingGameMenu);
		}

		if (SwinGame.KeyTyped(KeyCode.vk_UP)) {
			_currentDirection = Direction.UpDown;
		}
		if (SwinGame.KeyTyped(KeyCode.vk_LEFT) | SwinGame.KeyTyped(KeyCode.vk_RIGHT)) {
			_currentDirection = Direction.LeftRight;
		}

		if (SwinGame.KeyTyped(KeyCode.vk_r)) {
			GameController.HumanPlayer.RandomizeDeployment();
		}

		if (SwinGame.MouseClicked(MouseButton.LeftButton)) {
			ShipName selected = default(ShipName);
			selected = GetShipMouseIsOver();
			if (selected != ShipName.None) {
				_selectedShip = selected;
			} else {
				Ship moving = GameController.HumanPlayer.PlayerGrid.GetShipNamed(_selectedShip);
				try {
					DoDeployClick();
				} catch (Exception ex) {
					Audio.PlaySoundEffect(GameResources.GameSound("Error"));
					UtilityFunctions.Message = ex.Message;
					Direction currentDir = _currentDirection;
					_currentDirection = moving.Direction;
					DeployShip(moving.Row,moving.Column);
					_currentDirection = currentDir;
				}
			}

			if (GameController.HumanPlayer.ReadyToDeploy & UtilityFunctions.IsMouseInRectangle(PLAY_BUTTON_LEFT,TOP_BUTTONS_TOP,PLAY_BUTTON_WIDTH,TOP_BUTTONS_HEIGHT)) {
				GameController.EndDeployment();
			} else if (UtilityFunctions.IsMouseInRectangle(UP_DOWN_BUTTON_LEFT,TOP_BUTTONS_TOP,DIR_BUTTONS_WIDTH,TOP_BUTTONS_HEIGHT)) {
				_currentDirection = Direction.UpDown;
			} else if (UtilityFunctions.IsMouseInRectangle(LEFT_RIGHT_BUTTON_LEFT,TOP_BUTTONS_TOP,DIR_BUTTONS_WIDTH,TOP_BUTTONS_HEIGHT)) {
				_currentDirection = Direction.LeftRight;
			} else if (UtilityFunctions.IsMouseInRectangle(RANDOM_BUTTON_LEFT,TOP_BUTTONS_TOP,RANDOM_BUTTON_WIDTH,TOP_BUTTONS_HEIGHT)) {
				GameController.HumanPlayer.RandomizeDeployment();
			}
		}
		if (SwinGame.MouseClicked(MouseButton.RightButton)) {
			Tile clickedCell = GetClickedCell(true);
			bool valid = clickedCell.Ship != null;
			if (clickedCell.Ship != null) {
				RotateClickedShip(clickedCell);
			}
		}
	}

	private static void RotateClickedShip(Tile clickedCell)
	{
		int oldRow = clickedCell.Ship.Row;
		int oldCol = clickedCell.Ship.Column;
		Direction oldDirection = clickedCell.Ship.Direction;
		Direction oldCurrentDir = _currentDirection;
		bool horizontal = clickedCell.Ship.Direction == Direction.LeftRight;
		int cellPosition = horizontal ? clickedCell.Column : clickedCell.Row;
		int shipPosition = horizontal ? clickedCell.Ship.Column : clickedCell.Ship.Row;
		int relativePosition = cellPosition - shipPosition;
		_selectedShip = clickedCell.Ship.Type;
		_currentDirection = horizontal ? Direction.UpDown : Direction.LeftRight;
		// Ship at relative position (-3, 0) should go to relative position (0, -3) and invert the direction
		try {
			if (horizontal) {
				DeployShip(clickedCell.Row - relativePosition,clickedCell.Column,false,true);
			} else {
				DeployShip(clickedCell.Row,clickedCell.Column - relativePosition,false,true);
			}
		} catch (Exception ex) {
			_currentDirection = oldDirection;
			DeployShip(oldRow,oldCol);
			_currentDirection = oldCurrentDir;
			Audio.PlaySoundEffect(GameResources.GameSound("Error"));
			UtilityFunctions.Message = ex.Message;
		}
	}

	private static Tile GetClickedCell(bool getShip)
	{
		Point2D mouse = default(Point2D);
		mouse = SwinGame.MousePosition();
		//Calculate the row/col clicked
		int row = 0;
		int col = 0;
		row = Convert.ToInt32(Math.Floor((mouse.Y - UtilityFunctions.FIELD_TOP)/(UtilityFunctions.CELL_HEIGHT + UtilityFunctions.CELL_GAP)));
		col = Convert.ToInt32(Math.Floor((mouse.X - UtilityFunctions.FIELD_LEFT)/(UtilityFunctions.CELL_WIDTH + UtilityFunctions.CELL_GAP)));
		Tile res = new Tile(row,col,null);
		if (getShip) {
			res.Ship = GameController.HumanPlayer.PlayerGrid.GetShipAtTile(res);
		}
		return res;
	}
	private static Tile GetClickedCell()
	{
		return GetClickedCell(false);
	}

	/// <summary>
	/// The user has clicked somewhere on the screen, check if its is a deployment and deploy
	/// the current ship if that is the case.
	/// </summary>
	/// <remarks>
	/// If the click is in the grid it deploys to the selected location
	/// with the indicated direction
	/// </remarks>
	private static void DoDeployClick()
	{
		Tile clickedCell = GetClickedCell();
		DeployShip(clickedCell.Row, clickedCell.Column, false, false);
	}

	private static void DeployShip(int row,int col,bool supressExceptions,bool throwIfOutOfRange) {
		bool inRange = row >= 0 && row < GameController.HumanPlayer.PlayerGrid.Height;
		if (inRange) {
			inRange = col >= 0 && col < GameController.HumanPlayer.PlayerGrid.Width;
			if (inRange) {
				//if in the area try to deploy
				try {
					GameController.HumanPlayer.PlayerGrid.MoveShip(row,col,_selectedShip,_currentDirection);
				} catch (Exception ex) {
					if (supressExceptions) {
						Audio.PlaySoundEffect(GameResources.GameSound("Error"));
						UtilityFunctions.Message = ex.Message;
					} else {
						throw ex;
					}
				}
			}
		}
		if (throwIfOutOfRange && !inRange) {
			throw new ArgumentOutOfRangeException("Ship can't fit on the board");
		}
	}
	private static void DeployShip(int row,int col)
	{
		DeployShip(row,col,true,false);
	}
	/// <summary>
	/// Draws the deployment screen showing the field and the ships
	/// that the player can deploy.
	/// </summary>
	public static void DrawDeployment()
	{
		UtilityFunctions.DrawField(GameController.HumanPlayer.PlayerGrid, GameController.HumanPlayer, true);

		//Draw the Left/Right and Up/Down buttons
		if (_currentDirection == Direction.LeftRight) {
			SwinGame.DrawBitmap(GameResources.GameImage("LeftRightButton"), LEFT_RIGHT_BUTTON_LEFT, TOP_BUTTONS_TOP);
			
		} else {
			SwinGame.DrawBitmap(GameResources.GameImage("UpDownButton"), LEFT_RIGHT_BUTTON_LEFT, TOP_BUTTONS_TOP);
			
		}

		//DrawShips
		foreach (ShipName sn in Enum.GetValues(typeof(ShipName))) {
			int i = 0;
			i = ((int)sn) - 1;
			if (i >= 0) {
				if (sn == _selectedShip) {
					SwinGame.DrawBitmap(GameResources.GameImage("SelectedShip"), SHIPS_LEFT, SHIPS_TOP + i * SHIPS_HEIGHT);
					
				}

				

			}
		}

		if (GameController.HumanPlayer.ReadyToDeploy) {
			SwinGame.DrawBitmap(GameResources.GameImage("PlayButton"), PLAY_BUTTON_LEFT, TOP_BUTTONS_TOP);
			
		}

		SwinGame.DrawBitmap(GameResources.GameImage("RandomButton"), RANDOM_BUTTON_LEFT, TOP_BUTTONS_TOP);

		UtilityFunctions.DrawMessage();
	}

	/// <summary>
	/// Gets the ship that the mouse is currently over in the selection panel.
	/// </summary>
	/// <returns>The ship selected or none</returns>
	private static ShipName GetShipMouseIsOver()
	{
		foreach (ShipName sn in Enum.GetValues(typeof(ShipName))) {
			int i = 0;
			i =((int)sn) - 1;

			if (UtilityFunctions.IsMouseInRectangle(SHIPS_LEFT, SHIPS_TOP + i * SHIPS_HEIGHT, SHIPS_WIDTH, SHIPS_HEIGHT)) {
				return sn;
			}
		}

		return ShipName.None;
	}
}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
