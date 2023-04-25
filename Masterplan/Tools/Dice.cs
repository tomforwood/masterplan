using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Masterplan.Tools
{
    class DiceStatistics
    {
        public static Dictionary<int, int> Odds(List<int> dice, int constant)
        {
            Dictionary<int, int> odds = new Dictionary<int, int>();

            if (dice.Count > 0)
            {
                int combinations = 1;
                foreach (int die in dice)
                    combinations *= die;

                // Work out how quickly each die rolls over
                int[] frequencies = new int[dice.Count];
                frequencies[dice.Count - 1] = 1;
                for (int n = dice.Count - 2; n >= 0; --n)
                    frequencies[n] = frequencies[n + 1] * dice[n + 1];

                for (int n = 0; n != combinations; ++n)
                {
                    // Work out the number for each die
                    List<int> rolls = new List<int>();
                    for (int index = 0; index != dice.Count; ++index)
                    {
                        int die = dice[index];
                        int roll = ((n / frequencies[index]) % die) + 1;

                        rolls.Add(roll);
                    }

                    // Work out the sum
                    int sum = constant;
                    foreach (int roll in rolls)
                        sum += roll;

                    if (!odds.ContainsKey(sum))
                        odds[sum] = 0;

                    odds[sum] += 1;
                }
            }

            return odds;
        }

        public static string Expression(List<int> dice, int constant)
        {
            int d4 = 0;
            int d6 = 0;
            int d8 = 0;
            int d10 = 0;
            int d12 = 0;
            int d20 = 0;

            foreach (int die in dice)
            {
                switch (die)
                {
                    case 4:
                        d4 += 1;
                        break;
                    case 6:
                        d6 += 1;
                        break;
                    case 8:
                        d8 += 1;
                        break;
                    case 10:
                        d10 += 1;
                        break;
                    case 12:
                        d12 += 1;
                        break;
                    case 20:
                        d20 += 1;
                        break;
                }
            }

            string exp = "";
            if (d4 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d4 + "d4";
            }
            if (d6 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d6 + "d6";
            }
            if (d8 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d8 + "d8";
            }
            if (d10 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d10 + "d10";
            }
            if (d12 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d12 + "d12";
            }
            if (d20 != 0)
            {
                if (exp != "")
                    exp += " + ";

                exp += d20 + "d20";
            }

			if (constant != 0)
			{
				exp += " ";

				if (constant > 0)
					exp += "+";

				exp += constant.ToString();
			}

            return exp;
        }
    }

	class DamageComponent
	{
		int[] fThrows;
		int fConstant;

		DamageComponent(int sides, int throws, int constant)
		{
			fThrows = DiceExpression.DiceIndividual(throws, sides);
			fConstant = constant;
			Maximum = sides * throws + constant;
			double mean = (double)(sides + 1) / 2;
			Average = (throws * mean) + constant;
		}

		public int Evaluate()
		{
			return fThrows.Sum() + fConstant;
		}

		public int Maximum { get; }

		public double Average
		{
			get;
		}

		internal static DamageComponent Parse(Match m)
		{
			int throwsI;
			int sidesI;
			int constI;
			int.TryParse(m.Groups["dCount"]?.Value, out throwsI);
			int.TryParse(m.Groups["dSize"]?.Value, out sidesI);
			int.TryParse(m.Groups["dConst"]?.Value.Replace("+", ""), out constI);
			return new DamageComponent(sidesI, throwsI, constI);
		}

		public override string ToString()
		{
			StringBuilder res = new StringBuilder();
			if (fThrows != null && fThrows.Length > 0)
			{
				res.Append("[");
				res.Append(String.Join(",", fThrows));
				res.Append("]");
				if (fConstant > 0)
				{
					res.Append(" +");
					res.Append(fConstant);
				}
			}
			else if (fConstant > 0)
			{
				res.Append(fConstant);
			}
			return res.ToString();
		}
		public string ToCritString()
		{
			return Maximum.ToString();
		}
	}

	class DamageBlock
	{
		private const string damagePattern = @"(((?<dCount>\d{1,2})d(?<dSize>\d{1,2})( *(?<dConst>(\+|-)[ ]*\d{1,2}))?)|(?<dConst>\d{1,2}))";
		List<DamageComponent> components = new List<DamageComponent>();
		String text;
		public int Evaluate()
		{
			return components.Select(component => component.Evaluate()).Sum();
		}

		public int Maximum
		{
			get
			{
				return components.Select(component => component.Maximum).Sum();
			}
		}

		public double Average
		{
			get
			{
				return components.Select(component => component.Average).Sum();
			}
		}

		public static DamageBlock Parse(string str)
		{
			DamageBlock res = new DamageBlock();
			res.text = str;
			if (str.Contains("ongoing")) return res;//ongoing doesnt get rolled or added in to the total

			res.components = Regex.Matches(str, damagePattern)
				.Cast<Match>()
				.Select(m => DamageComponent.Parse(m))
				.ToList();

			return res;

		}

		public override string ToString()
		{
			DamageReplacer<DamageComponent> r = new DamageReplacer<DamageComponent>(components, d => d.ToString());
			MatchEvaluator me = new MatchEvaluator(r.replaceNumbered);
			string res = Regex.Replace(text, damagePattern, me);
			return res;
		}
		public string ToCritString()
		{
			DamageReplacer<DamageComponent> r = new DamageReplacer<DamageComponent>(components, d => d.ToCritString());
			MatchEvaluator me = new MatchEvaluator(r.replaceNumbered);
			string res = Regex.Replace(text, damagePattern, me);
			return res;
		}
	}

	class DiceExpression
	{
		string text;
		List<DamageBlock> damages = new List<DamageBlock>();

		public static DiceExpression Parse(string str)
		{
			DiceExpression exp = new DiceExpression();

			//remove anything starting "(was" - that is the before adjustment damage
			str = Regex.Replace(str, "\\(was[^)]*\\)", "");
			exp.text = str;

			List<DamageBlock> damages = Regex.Matches(str, ".*?(dmg|damage)").Cast<Match>().Select(s => DamageBlock.Parse(s.Value)).ToList();
			exp.damages = damages;

			return exp;
		}

		public int Evaluate()
		{
			return damages.Select(component => component.Evaluate()).Sum();
		}

		public int Maximum
		{
			get
			{
				return damages.Select(component => component.Maximum).Sum();
			}
		}

		public double Average
		{
			get
			{
				return damages.Select(component => component.Average).Sum();
			}
		}

		public override string ToString()
		{
			DamageReplacer<DamageBlock> r = new DamageReplacer<DamageBlock>(damages, d=>d.ToString());
			MatchEvaluator me = new MatchEvaluator(r.replaceNumbered);
			string res = Regex.Replace(text, ".*?(dmg|damage)", me);
			return res;
		}

		public string ToCritString()
		{
			DamageReplacer<DamageBlock> r = new DamageReplacer<DamageBlock>(damages,d => d.ToCritString());
			MatchEvaluator me = new MatchEvaluator(r.replaceNumbered);
			string res = Regex.Replace(text, ".*?(dmg|damage)", me);
			return res;
		}

		public DiceExpression Adjust(int level_adjustment)
		{
			
			/*Array dmgs = Enum.GetValues(typeof(DamageExpressionType));

			// Choose the closest level and work out the differences (in throws / sides / constant)
			int min_difference = int.MaxValue;
			int best_level = 0;
			DamageExpressionType best_det = DamageExpressionType.Normal;
			DiceExpression best_exp = null;
			for (int level = 1; level <= 30; ++level)
			{
				foreach (DamageExpressionType det in dmgs)
				{
					DiceExpression exp = DiceExpression.Parse(Statistics.Damage(level, det));

					int diff_throws = Math.Abs(fThrows - exp.Throws);
					int diff_sides = Math.Abs(fSides - exp.Sides) / 2;
					int diff_const = Math.Abs(fConstant - exp.Constant);

					int difference = (diff_throws * 10) + (diff_sides * 100) + diff_const;
					if (difference < min_difference)
					{
						min_difference = difference;
						best_level = level;
						best_det = det;
						best_exp = exp;
					}
				}
			}

			if (best_exp == null)
				return this;

			int throw_diff = fThrows - best_exp.Throws;
			int sides_diff = fSides - best_exp.Sides;
			int const_diff = fConstant - best_exp.Constant;

			// Adjust the new expression
			int adj_level = Math.Max(best_level + level_adjustment, 1);
			DiceExpression adjusted = DiceExpression.Parse(Statistics.Damage(adj_level, best_det));
			adjusted.Throws += throw_diff;
			adjusted.Sides += sides_diff;
			adjusted.Constant += const_diff;

			if (fThrows == 0)
				adjusted.Throws = 0;
			else
				adjusted.Throws = Math.Max(adjusted.Throws, 1);

			// Make sure we have a valid dice type
			switch (adjusted.Sides)
			{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
					adjusted.Sides = 4;
					break;
				case 5:
				case 6:
					adjusted.Sides = 6;
					break;
				case 7:
				case 8:
					adjusted.Sides = 8;
					break;
				case 9:
				case 10:
					adjusted.Sides = 10;
					break;
				case 11:
				case 12:
				case 13:
				case 14:
				case 15:
				case 16:
					adjusted.Sides = 12;
					break;
				default:
					adjusted.Sides = 20;
					break;
			}

			return adjusted;*/
			return null;
		}
		public DiceExpression Adjust(double percentage_adjustment)
		{
			/*double diceExpected = Throws * (Sides + 1) / 2.0f;
			double expected = diceExpected + Constant;
			double adjusted = expected * percentage_adjustment;
			double adjustedConstant = adjusted - diceExpected;

			return new DiceExpression(Throws, Sides, (int)adjustedConstant);*/
			return null;
		}

		static Random fRandom = new Random();

		public static int Dice(int throws, int sides)
		{
			int result = 0;

			for (int n = 0; n != throws; ++n)
			{
				int roll = 1 + fRandom.Next() % sides;
				result += roll;
			}

			return result;
		}
		public static int[] DiceIndividual(int throws, int sides)
		{
			int[] result = new int[throws];

			for (int n = 0; n != throws; ++n)
			{
				int roll = 1 + fRandom.Next() % sides;
				result[n] = roll;
			}

			return result;
		}
	}
	class DamageReplacer<T>
	{
		readonly List<T> damages;
		readonly Func<T, string> stringFunc;
		public DamageReplacer(List<T> d, Func<T, string> stringFunc) { 
			damages = d;
			this.stringFunc = stringFunc;
		}
		int replaceNum = 0;
		public string replaceNumbered(Match m)
		{
			return stringFunc(damages[replaceNum++]);
		}
	}


}
