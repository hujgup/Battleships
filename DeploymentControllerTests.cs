using System;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;

namespace Battleships {
	/// <summary>
	/// Provides unit tests for the DeploymentController class.
	/// </summary>
	[TestFixture()]
	public class DeploymentControllerTests {
		private void TestDeploymentActual(int x,int y,Direction dir,ShipName shipType)
		{
			// The fact that all this reflection is necessary IMO speaks volumes about how badly designed this whole solution is.

			BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;
			int found = 0;
			{
				FieldInfo[] fields = typeof(DeploymentController).GetFields(privateStatic);
				foreach (FieldInfo info in fields) {
					if (info.Name == "_currentDirection") {
						info.SetValue(null, dir);
						Assert.AreEqual(dir, info.GetValue(null), "Reflection error: _currentDirection not set to the proper value.");
						found++;
					} else if (info.Name == "_selectedShip") {
						info.SetValue(null, shipType);
						Assert.AreEqual(shipType, info.GetValue(null), "Reflection error: _selectedShip not set to the proper value.");
						found++;
					}
					if (found >= 2) {
						break;
					}
				}
			}

			if (found < 2) {
				Assert.Fail("Reflection error: One or more of (_currentDirection, _selectedShip) were not found.");
			} else {
				MethodInfo deployShip = null;
				{
					MethodInfo[] methods = typeof(DeploymentController).GetMethods(privateStatic);
					foreach (MethodInfo info in methods) {
						if (info.Name == "DeployShip" && info.GetParameters().Length == 4) {
							deployShip = info;
							break;
						}
					}
				}

				if (deployShip == null) {
					Assert.Fail("Reflection error: DeployShip (4 args) method is undefined.");
				} else {
					int i = 0;
					int max = 2048;
					while (i <= max) {
						try {
							deployShip.Invoke(null, new object[] {
								x,
								y,
								false,
								false
							});
							break;
						} catch (Exception) {
							GameController.HumanPlayer.RandomizeDeployment();
							i++;
						}
					}
					if (i > max) {
						Assert.Fail("Adding a ship to a valid grid square should not throw an exception. Be aware that, due to randomization, it is possible for this test to fail when nothing is actually wrong - try running the test again first.");
					} else {
						Ship s = GameController.HumanPlayer.PlayerGrid.GetShipNamed(shipType);
						Assert.IsNotNull(s, "The " + shipType.ToString() + " must exist.");
						Assert.AreEqual((int)shipType, s.Size, "The" + shipType.ToString() + " must occupy exatly 3 tiles.");

						List<Tile> occupied = s.OccupiedTiles;
						Assert.AreEqual(s.Size, occupied.Count, "OccupiedTiles's Count property must be the same as the Size property.");
						for (i = 0; i < s.Size; i++) {
							Assert.Contains(new Tile(x, y, s), occupied, "The tiles contained within the " + shipType.ToString() + " must extend to the right out from the deployment point (at point x = " + x.ToString() + ", y = " + y.ToString() + ").");
							if (dir == Direction.UpDown) {
								x++;
							} else {
								y++;
							}
							/*
								"But why is X being incremented if you set _currentDirection to UpDown, and vice versa?"
									Because for some reason the code we were given expects UpDown to behave like LeftRight logically should, and the same the other way around.
									I know where the bug is (Model/SeaGrid.cs, lines 161-171), but seeing as everything else is expecting this weird behavior I'm just going with it (and this unit test is now enforcing it).
							*/
						}
					}
				}
			}
		}
		/// <summary>
		/// Tests that ship deployment works correctly.
		/// </summary>
		[Test()]
		public void TestDeployment()
		{
			TestDeploymentActual(4, 5, Direction.UpDown, ShipName.Destroyer);
			TestDeploymentActual(0, 0, Direction.LeftRight, ShipName.AircraftCarrier);
			TestDeploymentActual(7, 8, Direction.UpDown, ShipName.Tug);
			TestDeploymentActual(5, 2, Direction.LeftRight, ShipName.Submarine);
			TestDeploymentActual(3, 3, Direction.UpDown, ShipName.Battleship);
		}
	}
}


