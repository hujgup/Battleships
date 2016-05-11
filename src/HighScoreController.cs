
using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Data;
using System.Diagnostics;
using System.IO;
using SwinGameSDK;

/// <summary>
/// Controls displaying and collecting high score data.
/// </summary>
/// <remarks>
/// Data is saved to a file.
/// </remarks>
static class HighScoreController
{
	private const int NAME_WIDTH = 5;

	private const int SCORES_LEFT = 490;

	private static class ScoreIO
	{
		/*
			File structore definition:

			Repeat the following until EOF:
				The first byte describes the byte length of the player name
					The next n bytes are the player name
				The next byte describes the byte length of the score value
					The next n bytes are the score value

			If EOF is found in the middle of an iteration, readers should throw an EndOfStreamException.
		*/
		private static readonly string _PATH = SwinGame.PathToResource("highscores.txt");
		private static byte[] GetComponents(long value)
		{
			long length = (long)Math.Ceiling(Math.Log(value, 8));
			byte[] buffer = new byte[length];
			byte[] bytes = BitConverter.GetBytes(value);
			for (long i = 0; i < length; i++)
			{
				buffer[i] = bytes[i];
			}
			return buffer;
		}
		private static byte SafeReadByte(Stream stream)
		{
			if (stream.Position >= stream.Length) {
				throw new EndOfStreamException("Unexpected end of file at position " + stream.Position.ToString());
			}
			return (byte)stream.ReadByte();
		}
		public static List<Score> Read()
		{
			List<Score> res = new List<Score>();
			using (FileStream stream = new FileStream(_PATH, FileMode.Open))
			{
				Score s;
				s.Name = "";
				byte nameLength;
				byte scoreLength;
				byte i;
				while (stream.Position < stream.Length)
				{
					s = new Score();
					s.Name = "";
					nameLength = SafeReadByte(stream);
					for (i = 0; i < nameLength; i++)
					{
						s.Name += (char)stream.ReadByte();
					}
					scoreLength = SafeReadByte(stream);
					int multiplier = 1;
					for (i = 0; i < scoreLength; i++, multiplier *= 256)
					{
						s.Value += multiplier*stream.ReadByte();
					}
					res.Add(s);
				}
			}
			return res;
		}
		public static void Write(List<Score> scores)
		{
			using (FileStream stream = new FileStream(_PATH,FileMode.Append))
			{
				foreach (Score s in scores)
				{
					stream.WriteByte((byte)s.Name.Length);
					for (int i = 0; i < s.Name.Length; i++)
					{
						stream.WriteByte((byte)s.Name[i]);
					}
					byte[] components = GetComponents(s.Value);
					stream.WriteByte((byte)components.Length);
					for (int i = 0; i < components.Length; i++)
					{
						stream.WriteByte(components[i]);
					}
				}
			}

		}
		public static void Write(string name, int value)
		{
			Score s = new Score();
			s.Name = name;
			s.Value = value;
			Write(s);
		}
		public static void Write(Score s)
		{
			List<Score> scores = new List<Score>(1);
			scores.Add(s);
			Write(scores);
		}
	}

	/// <summary>
	/// The score structure is used to keep the name and
	/// score of the top players together.
	/// </summary>
	private struct Score : IEquatable<Score>, IComparable<Score>, IComparable
	{
		public string Name;

		public int Value;
		public bool Equals(Score other)
		{
			return Name == other.Name && Value == other.Value;
		}
		public int CompareTo(Score other)
		{
			return Math.Sign(other.Value - Value);
		}
		/// <summary>
		/// Allows scores to be compared to facilitate sorting
		/// </summary>
		/// <param name="obj">the object to compare to</param>
		/// <returns>a value that indicates the sort order</returns>
		public int CompareTo(object obj)
		{
			if (obj is Score) {
				return CompareTo((Score)obj);
/*
				Score other = (Score)obj;

				return other.Value - this.Value;
*/
			} else {
				return 0;
			}
		}
		public override bool Equals(object obj)
		{
			if (obj is Score) {
				return Equals((Score)obj);
			} else {
				return false;
			}
		}
		public override int GetHashCode()
		{
			unchecked {
				return Name.GetHashCode() + Value.GetHashCode();
			}
		}
		public static bool operator ==(Score a, Score b)
		{
			return a.Equals(b);
		}
		public static bool operator !=(Score a, Score b)
		{
			return !a.Equals(b);
		}
	}

	private static List<Score> _ScoresInFile = new List<Score>();
	private static List<Score> _Scores = new List<Score>();
	/// <summary>
	/// Loads the scores from the highscores text file.
	/// </summary>
	/// <remarks>
	/// The format is
	/// # of scores
	/// NNNSSS
	/// 
	/// Where NNN is the name and SSS is the score
	/// </remarks>
	private static void LoadScores()
	{
		_Scores = ScoreIO.Read();
		_Scores.Sort();
		_ScoresInFile = new List<Score>(_Scores);

/*
		string filename = null;
		filename = SwinGame.PathToResource("highscores.txt");

		StreamReader input = default(StreamReader);
		input = new StreamReader(filename);

		//Read in the # of scores
		int numScores = 0;
		numScores = Convert.ToInt32(input.ReadLine());

		_Scores.Clear();

		int i = 0;

		for (i = 1; i <= numScores; i++) {
			Score s = default(Score);
			string line = null;

			line = input.ReadLine();

			s.Name = line.Substring(0, NAME_WIDTH);
			s.Value = Convert.ToInt32(line.Substring(NAME_WIDTH));
			_Scores.Add(s);
		}
		input.Close();
*/
	}

	/// <summary>
	/// Saves the scores back to the highscores text file.
	/// </summary>
	/// <remarks>
	/// The format is
	/// # of scores
	/// NNNSSS
	/// 
	/// Where NNN is the name and SSS is the score
	/// </remarks>
	private static void SaveScores() {
		foreach (Score s in _Scores) {
			if (!_ScoresInFile.Contains(s)) {
				ScoreIO.Write(s);
			}
		}
		_ScoresInFile = new List<Score>(_Scores);
/*
		string filename = null;
		filename = SwinGame.PathToResource("highscores.txt");

		StreamWriter output = default(StreamWriter);
		output = new StreamWriter(filename);

		output.WriteLine(_Scores.Count);

		foreach (Score s in _Scores) {
			output.WriteLine(s.Name + s.Value);
		}

		output.Close();
*/
	}

	/// <summary>
	/// Draws the high scores to the screen.
	/// </summary>
	public static void DrawHighScores()
	{
		const int SCORES_HEADING = 40;
		const int SCORES_TOP = 80;
		const int SCORE_GAP = 30;

		if (_Scores.Count == 0)
			LoadScores();

		SwinGame.DrawText("   High Scores   ", Color.White, GameResources.GameFont("Courier"), SCORES_LEFT, SCORES_HEADING);

		//For all of the scores
		int i = 0;
		for (i = 0; i <= _Scores.Count - 1; i++) {
			Score s = default(Score);

			s = _Scores[i];

			//for scores 1 - 9 use 01 - 09
			if (i < 9) {
				SwinGame.DrawText(" " + (i + 1) + ":   " + s.Name + "   " + s.Value, Color.White, GameResources.GameFont("Courier"), SCORES_LEFT, SCORES_TOP + i * SCORE_GAP);
			} else {
				SwinGame.DrawText(i + 1 + ":   " + s.Name + "   " + s.Value, Color.White, GameResources.GameFont("Courier"), SCORES_LEFT, SCORES_TOP + i * SCORE_GAP);
			}
		}
	}

	/// <summary>
	/// Handles the user input during the top score screen.
	/// </summary>
	/// <remarks></remarks>
	public static void HandleHighScoreInput()
	{
		if (SwinGame.MouseClicked(MouseButton.LeftButton) || SwinGame.KeyTyped(KeyCode.vk_ESCAPE) || SwinGame.KeyTyped(KeyCode.vk_RETURN)) {
			GameController.EndCurrentState();
		}
	}

	/// <summary>
	/// Read the user's name for their highsSwinGame.
	/// </summary>
	/// <param name="value">the player's sSwinGame.</param>
	/// <remarks>
	/// This verifies if the score is a highsSwinGame.
	/// </remarks>
	public static void ReadHighScore(int value)
	{
		const int ENTRY_TOP = 500;

		if (_Scores.Count == 0)
			LoadScores();

		//is it a high score
		if (value > _Scores[_Scores.Count - 1].Value) {
			Score s = new Score();
			s.Value = value;

			GameController.AddNewState(GameState.ViewingHighScores);

			int x = 0;
			x = SCORES_LEFT + SwinGame.TextWidth(GameResources.GameFont("Courier"), "Name: ");

			SwinGame.StartReadingText(Color.White, NAME_WIDTH, GameResources.GameFont("Courier"), x, ENTRY_TOP);

			//Read the text from the user
			while (SwinGame.ReadingText()) {
				SwinGame.ProcessEvents();

				UtilityFunctions.DrawBackground();
				DrawHighScores();
				SwinGame.DrawText("Name: ", Color.White, GameResources.GameFont("Courier"), SCORES_LEFT, ENTRY_TOP);
				SwinGame.RefreshScreen();
			}

			s.Name = SwinGame.TextReadAsASCII();

			if (s.Name.Length < 3) {
				s.Name = s.Name + new string(Convert.ToChar(" "), 3 - s.Name.Length);
			}

			_Scores.RemoveAt(_Scores.Count - 1);
			_Scores.Add(s);
			_Scores.Sort();
			SaveScores ();

			GameController.EndCurrentState();
		}
	}
}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
