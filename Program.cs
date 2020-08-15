// Created by Ryan White to do some quick benchmarks on some different duplicate space removal methods. 
// https://stackoverflow.com/questions/1279859/how-to-replace-multiple-white-spaces-with-one-white-space

using System;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Collections.Generic;

static class Program
{
    public static void Main()
    {
        long seed = ConfigProgramForBenchmarking();
        string seedAsString = seed.ToString();
        Stopwatch sw = new Stopwatch();
        Func<string, string> f;

        List<string> inputs = new List<string>();
        List<string> outputs = new List<string>();

        inputs .Add("This is   a Warm ONLY up function for\tbest   \r\n benchmark results." + seed);
        outputs.Add("This is   a Warm ONLY up function for\tbest   \r\n benchmark results." + seed);

        inputs .Add("Hello World,    how are   you           doing?" + seed);
        outputs.Add("Hello World, how are you doing?" + seed);

        inputs .Add("It\twas\t \tso    nice  to\t\t see you \tin 1950.  \t" + seed);
        outputs.Add("It was so nice to see you in 1950. " + seed);

        inputs.Add("That car\r\nis sooooooooo     fast." + seed);
        outputs.Add("That car is sooooooooo fast." + seed);

        inputs.Add("  " + seed);
        outputs.Add(" " + seed);

        inputs.Add(" " + seed);
        outputs.Add(" " + seed);


        //warm-up timer function
        f = (x) => seedAsString;
        long baseVal = TestMethod(f, sw, inputs, outputs, 0, "BASELINE").ElapsedTicks;
        Console.Clear();
        Console.WriteLine(@"|                           | Time  |   TEST 1    |   TEST 2    |   TEST 3    |   TEST 4    |   TEST 5    |");
        Console.WriteLine(@"| Function Name             |(ticks)| dup. spaces | spaces+tabs | spaces+CR/LF| "" "" -> "" ""  | "" "" -> "" "" |");
        Console.WriteLine(@"|---------------------------|-------|-------------|-------------|-------------|-------------|-------------|");


        // InPlace Replace by Felipe Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
        TestMethod(SwitchStmtBuildSpaceOnly, sw, inputs, outputs, baseVal);

        // InPlace Replace by Felipe Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
        TestMethod(InPlaceCharArraySpaceOnly, sw, inputs, outputs, baseVal);

        // InPlace Replace by Felipe Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
        TestMethod(SwitchStmtBuild, sw, inputs, outputs, baseVal);

        // InPlace Replace by Felipe Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
        TestMethod(SwitchStmtBuild2, sw, inputs, outputs, baseVal);

        //StringBuilder by David S 2013 (https://stackoverflow.com/a/16035044/2352507)
        TestMethod(SingleSpacedTrim, sw, inputs, outputs, baseVal);

        //StringBuilder by fubo (https://stackoverflow.com/a/27502353/2352507
        TestMethod(Fubo, sw, inputs, outputs, baseVal);

        //Split And Join by Jon Skeet (https://stackoverflow.com/a/1280227/2352507)
        TestMethod(SplitAndJoinOnSpace, sw, inputs, outputs, baseVal);

        //Regex with compile by Jon Skeet (https://stackoverflow.com/a/1280227/2352507)
        f = (x) => MultipleSpaces.Replace(x, " ");
        TestMethod(f, sw, inputs, outputs, baseVal, "RegExWithCompile");

        //StringBuilder by user214147 (https://stackoverflow.com/a/2156660/2352507
        TestMethod(User214147, sw, inputs, outputs, baseVal);

        //Regex by Brandon (https://stackoverflow.com/a/1279878/2352507
        f = (x) => Regex.Replace(x, @"\s{2,}", " ");
        TestMethod(f, sw, inputs, outputs, baseVal, "RegExBrandon");

        //Regex with non-compile Tim Hoolihan (https://stackoverflow.com/a/1279874/2352507)
        f = (x) => Regex.Replace(x, @"\s+", " ");
        TestMethod(f, sw, inputs, outputs, baseVal, "RegExNoCompile");


    }

    private static Stopwatch TestMethod(Func<string,string> Method, Stopwatch sw, List<string> inputs, List<string> correctOutputs, long baseVal, string name = "")
    {
        int hash = 0;
        int inputsCount = inputs.Count;
        string[] outputs = new string[inputsCount];
        for (int i = 0; i < 10; i++)
        {
            hash = Method(inputs[0]).Length;

            sw.Restart();
            for (int j = 1; j < inputsCount; j++)
            {
                outputs[j] = Method(inputs[j]);
            }
            sw.Stop();
        }

        if (string.IsNullOrEmpty(name))
            name = Method.Method.Name;

        Console.Write("| " + name.PadRight(25) + " |" + string.Format("{0,6:#########} |", sw.ElapsedTicks - baseVal));

        for (int i = 1; i < inputs.Count; i++)
        {
            Console.Write((outputs[i] == correctOutputs[i] ? "    PASS     |" : "    FAIL     |"));
        }

        Console.Write(hash + "|\r\n");

        return sw;
    }

    // InPlace Replace by Felipe Machado and slightly modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
    static string InPlaceCharArraySpaceOnly(string str)
    {
        var len = str.Length;
        var src = str.ToCharArray();
        int dstIdx = 0;
        bool lastWasWS = false;
        for (int i = 0; i < len; i++)
        {
            var ch = src[i];
            if (src[i] == '\u0020')
            {
                if (lastWasWS == false)
                {
                    src[dstIdx++] = ch;
                    lastWasWS = true;
                }
            }
            else
            {
                lastWasWS = false;
                src[dstIdx++] = ch;
            }
        }
        return new string(src, 0, dstIdx);
    }

    // InPlace Replace by Felipe R. Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
    static string SwitchStmtBuildSpaceOnly(string str)
    {
        var len = str.Length;
        var src = str.ToCharArray();
        int dstIdx = 0;
        bool lastWasWS = false; //Added line
        for (int i = 0; i < len; i++)
        {
            var ch = src[i];
            switch (ch)
            {
                case '\u0020': //SPACE
                //case '\u00A0': //NO-BREAK SPACE
                //case '\u1680': //OGHAM SPACE MARK
                //case '\u2000': // EN QUAD
                //case '\u2001': //EM QUAD
                //case '\u2002': //EN SPACE
                //case '\u2003': //EM SPACE
                //case '\u2004': //THREE-PER-EM SPACE
                //case '\u2005': //FOUR-PER-EM SPACE
                //case '\u2006': //SIX-PER-EM SPACE
                //case '\u2007': //FIGURE SPACE
                //case '\u2008': //PUNCTUATION SPACE
                //case '\u2009': //THIN SPACE
                //case '\u200A': //HAIR SPACE
                //case '\u202F': //NARROW NO-BREAK SPACE
                //case '\u205F': //MEDIUM MATHEMATICAL SPACE
                //case '\u3000': //IDEOGRAPHIC SPACE
                //case '\u2028': //LINE SEPARATOR
                //case '\u2029': //PARAGRAPH SEPARATOR
                //case '\u0009': //[ASCII Tab]
                //case '\u000A': //[ASCII Line Feed]
                //case '\u000B': //[ASCII Vertical Tab]
                //case '\u000C': //[ASCII Form Feed]
                //case '\u000D': //[ASCII Carriage Return]
                case '\u0085': //NEXT LINE
                    if (lastWasWS == false) //Added line
                    {
                        src[dstIdx++] = ch; //Added line
                        lastWasWS = true; //Added line
                    }
                    continue;
                default:
                    lastWasWS = false; //Added line 
                    src[dstIdx++] = ch;
                    break;
            }
        }
        return new string(src, 0, dstIdx);
    }

    // InPlace Replace by Felipe R. Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
    static string SwitchStmtBuild(string str)
    {
        var len = str.Length;
        var src = str.ToCharArray();
        int dstIdx = 0;
        bool lastWasWS = false; //Added line
        for (int i = 0; i < len; i++)
        {
            var ch = src[i];
            switch (ch)
            {
                case '\u0020': //SPACE
                case '\u00A0': //NO-BREAK SPACE
                case '\u1680': //OGHAM SPACE MARK
                case '\u2000': // EN QUAD
                case '\u2001': //EM QUAD
                case '\u2002': //EN SPACE
                case '\u2003': //EM SPACE
                case '\u2004': //THREE-PER-EM SPACE
                case '\u2005': //FOUR-PER-EM SPACE
                case '\u2006': //SIX-PER-EM SPACE
                case '\u2007': //FIGURE SPACE
                case '\u2008': //PUNCTUATION SPACE
                case '\u2009': //THIN SPACE
                case '\u200A': //HAIR SPACE
                case '\u202F': //NARROW NO-BREAK SPACE
                case '\u205F': //MEDIUM MATHEMATICAL SPACE
                case '\u3000': //IDEOGRAPHIC SPACE
                case '\u2028': //LINE SEPARATOR
                case '\u2029': //PARAGRAPH SEPARATOR
                case '\u0009': //[ASCII Tab]
                case '\u000A': //[ASCII Line Feed]
                case '\u000B': //[ASCII Vertical Tab]
                case '\u000C': //[ASCII Form Feed]
                case '\u000D': //[ASCII Carriage Return]
                case '\u0085': //NEXT LINE
                    if (lastWasWS == false) //Added line
                    {
                        src[dstIdx++] = ch; //Added line
                        lastWasWS = true; //Added line
                    }
                    continue;
                default:
                    lastWasWS = false; //Added line 
                    src[dstIdx++] = ch;
                    break;
            }
        }
        return new string(src, 0, dstIdx);
    }

    // InPlace Replace by Felipe R. Machado but modified by Ryan for multi-space removal (http://www.codeproject.com/Articles/1014073/Fastest-method-to-remove-all-whitespace-from-Strin)
    static string SwitchStmtBuild2(string str)
    {
        var len = str.Length;
        var src = str.ToCharArray();
        int dstIdx = 0;
        bool lastWasWS = false; //Added line
        for (int i = 0; i < len; i++)
        {
            var ch = src[i];
            switch (ch)
            {
                case '\u0020': //SPACE
                case '\u00A0': //NO-BREAK SPACE
                case '\u1680': //OGHAM SPACE MARK
                case '\u2000': // EN QUAD
                case '\u2001': //EM QUAD
                case '\u2002': //EN SPACE
                case '\u2003': //EM SPACE
                case '\u2004': //THREE-PER-EM SPACE
                case '\u2005': //FOUR-PER-EM SPACE
                case '\u2006': //SIX-PER-EM SPACE
                case '\u2007': //FIGURE SPACE
                case '\u2008': //PUNCTUATION SPACE
                case '\u2009': //THIN SPACE
                case '\u200A': //HAIR SPACE
                case '\u202F': //NARROW NO-BREAK SPACE
                case '\u205F': //MEDIUM MATHEMATICAL SPACE
                case '\u3000': //IDEOGRAPHIC SPACE
                case '\u2028': //LINE SEPARATOR
                case '\u2029': //PARAGRAPH SEPARATOR
                case '\u0009': //[ASCII Tab]
                case '\u000A': //[ASCII Line Feed]
                case '\u000B': //[ASCII Vertical Tab]
                case '\u000C': //[ASCII Form Feed]
                case '\u000D': //[ASCII Carriage Return]
                case '\u0085': //NEXT LINE
                    if (lastWasWS == false) //Added line
                    {
                        src[dstIdx++] = ' '; // Updated by Ryan
                        lastWasWS = true; //Added line
                    }
                    continue;
                default:
                    lastWasWS = false; //Added line 
                    src[dstIdx++] = ch;
                    break;
            }
        }
        return new string(src, 0, dstIdx);
    }

    static readonly Regex MultipleSpaces =
        new Regex(@" {2,}", RegexOptions.Compiled);

    //Split And Join by Jon Skeet (https://stackoverflow.com/a/1280227/2352507)
    static string SplitAndJoinOnSpace(string input)
    {
        string[] split = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", split);
    }

    //StringBuilder by user214147 (https://stackoverflow.com/a/2156660/2352507
    public static string User214147(string S)
    {
        string s = S.Trim();
        bool iswhite = false;
        int sLength = s.Length;
        StringBuilder sb = new StringBuilder(sLength);
        foreach (char c in s.ToCharArray())
        {
            if (char.IsWhiteSpace(c))
            {
                if (iswhite)
                {
                    //Continuing whitespace ignore it.
                    continue;
                }
                else
                {
                    //New WhiteSpace

                    //Replace whitespace with a single space.
                    sb.Append(" ");
                    //Set iswhite to True and any following whitespace will be ignored
                    iswhite = true;
                }
            }
            else
            {
                sb.Append(c.ToString());
                //reset iswhitespace to false
                iswhite = false;
            }
        }
        return sb.ToString();
    }

    //StringBuilder by fubo (https://stackoverflow.com/a/27502353/2352507
    public static string Fubo(this string Value)
    {
        StringBuilder sbOut = new StringBuilder();
        if (!string.IsNullOrEmpty(Value))
        {
            bool IsWhiteSpace = false;
            for (int i = 0; i < Value.Length; i++)
            {
                if (char.IsWhiteSpace(Value[i])) //Comparison with WhiteSpace
                {
                    if (!IsWhiteSpace) //Comparison with previous Char
                    {
                        sbOut.Append(Value[i]);
                        IsWhiteSpace = true;
                    }
                }
                else
                {
                    IsWhiteSpace = false;
                    sbOut.Append(Value[i]);
                }
            }
        }
        return sbOut.ToString();
    }

    //David S. 2013 (https://stackoverflow.com/a/16035044/2352507)
    public static string SingleSpacedTrim(string inString)
    {
        StringBuilder sb = new StringBuilder();
        bool inBlanks = false;
        foreach (char c in inString)
        {
            switch (c)
            {
                case '\r':
                case '\n':
                case '\t':
                case ' ':
                    if (!inBlanks)
                    {
                        inBlanks = true;
                        sb.Append(' ');
                    }
                    continue;
                default:
                    inBlanks = false;
                    sb.Append(c);
                    break;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// We want to run this item with max priory to lower the odds of
    /// the OS from doing program context switches in the middle of our code. 
    /// source:https://stackoverflow.com/a/16157458 
    /// </summary>
    /// <returns>random seed</returns>
    private static long ConfigProgramForBenchmarking()
    {
        //prevent the JIT Compiler from optimizing Fkt calls away
        long seed = Environment.TickCount;
        //use the second Core/Processor for the test
        Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
        //prevent "Normal" Processes from interrupting Threads
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        //prevent "Normal" Threads from interrupting this thread
        Thread.CurrentThread.Priority = ThreadPriority.Highest;
        return seed;
    }
}