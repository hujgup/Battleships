using System;
using NUnit.Framework;

namespace Battleships
{
	[TestFixture()]
	public class BattleshipTests
	{
		[Test()]
		public void TestAIExists ()
		{
			GameController.StartGame ();
			Assert.IsNotNull (GameController.ComputerPlayer);

		}
	}
}

