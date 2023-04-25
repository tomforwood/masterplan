using Microsoft.VisualStudio.TestTools.UnitTesting;

using Masterplan.Tools;
using System.Text.RegularExpressions;

namespace TestMasterplan
{
    [TestClass]
    public class DiceTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            string expr = "2d12 + 6 necrotic damage, and the voidsoul specter regains 5 hit points for every creature damaged by the attack. ";
            DiceExpression de1 = DiceExpression.Parse(expr);
            string res1 = de1.ToString();
            StringAssert.Matches(res1, new Regex(@"\[\d+,\d+\] \+6 necrotic damage, and the voidsoul specter regains 5 hit points for every creature damaged by the attack\. "));

            expr = "1d10 + 8 damage plus 1d10 radiant damage.";
            DiceExpression de2 = DiceExpression.Parse(expr);
            string res2 = de2.ToString();
            StringAssert.Matches(res2, new Regex(@"\[\d+] \+8 damage plus \[\d\] radiant damage"));

            expr = "3d8 + 9 t damage, a pushed 3 s and ongoing 10 psychic and is slowed(save ends both).";
            DiceExpression de3 = DiceExpression.Parse(expr);
            string res3 = de3.ToString();
            StringAssert.Matches(res3, new Regex(@"\[\d+,\d+,\d+] \+9 t damage, a pushed 3 s and ongoing 10 psychic and is slowed\(save ends both\)\."));

        }
    }
}
