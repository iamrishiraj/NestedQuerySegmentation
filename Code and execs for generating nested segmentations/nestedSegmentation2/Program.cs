using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
//using porter;

namespace nestedSegmentation_hybrid
{
    class Stemmer
    {
        private char[] b;
        private int i,     /* offset into b */
            i_end, /* offset to end of stemmed word */
            j, k;
        private static int INC = 50;
        /* unit of size whereby b is increased */

        public Stemmer()
        {
            b = new char[INC];
            i = 0;
            i_end = 0;
        }

        /**
         * Add a character to the word being stemmed.  When you are finished
         * adding characters, you can call stem(void) to stem the word.
         */

        public void add(char ch)
        {
            if (i == b.Length)
            {
                char[] new_b = new char[i + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            b[i++] = ch;
        }


        /** Adds wLen characters to the word being stemmed contained in a portion
         * of a char[] array. This is like repeated calls of add(char ch), but
         * faster.
         */

        public void add(char[] w, int wLen)
        {
            if (i + wLen >= b.Length)
            {
                char[] new_b = new char[i + wLen + INC];
                for (int c = 0; c < i; c++)
                    new_b[c] = b[c];
                b = new_b;
            }
            for (int c = 0; c < wLen; c++)
                b[i++] = w[c];
        }

        /**
         * After a word has been stemmed, it can be retrieved by toString(),
         * or a reference to the internal buffer can be retrieved by getResultBuffer
         * and getResultLength (which is generally more efficient.)
         */
        public override string ToString()
        {
            return new String(b, 0, i_end);
        }

        /**
         * Returns the length of the word resulting from the stemming process.
         */
        public int getResultLength()
        {
            return i_end;
        }

        /**
         * Returns a reference to a character buffer containing the results of
         * the stemming process.  You also need to consult getResultLength()
         * to determine the length of the result.
         */
        public char[] getResultBuffer()
        {
            return b;
        }

        /* cons(i) is true <=> b[i] is a consonant. */
        private bool cons(int i)
        {
            switch (b[i])
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u': return false;
                case 'y': return (i == 0) ? true : !cons(i - 1);
                default: return true;
            }
        }

        /* m() measures the number of consonant sequences between 0 and j. if c is
           a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
           presence,

              <c><v>       gives 0
              <c>vc<v>     gives 1
              <c>vcvc<v>   gives 2
              <c>vcvcvc<v> gives 3
              ....
        */
        private int m()
        {
            int n = 0;
            int i = 0;
            while (true)
            {
                if (i > j) return n;
                if (!cons(i)) break; i++;
            }
            i++;
            while (true)
            {
                while (true)
                {
                    if (i > j) return n;
                    if (cons(i)) break;
                    i++;
                }
                i++;
                n++;
                while (true)
                {
                    if (i > j) return n;
                    if (!cons(i)) break;
                    i++;
                }
                i++;
            }
        }

        /* vowelinstem() is true <=> 0,...j contains a vowel */
        private bool vowelinstem()
        {
            int i;
            for (i = 0; i <= j; i++)
                if (!cons(i))
                    return true;
            return false;
        }

        /* doublec(j) is true <=> j,(j-1) contain a double consonant. */
        private bool doublec(int j)
        {
            if (j < 1)
                return false;
            if (b[j] != b[j - 1])
                return false;
            return cons(j);
        }

        /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
           and also if the second c is not w,x or y. this is used when trying to
           restore an e at the end of a short word. e.g.

              cav(e), lov(e), hop(e), crim(e), but
              snow, box, tray.

        */
        private bool cvc(int i)
        {
            if (i < 2 || !cons(i) || cons(i - 1) || !cons(i - 2))
                return false;
            int ch = b[i];
            if (ch == 'w' || ch == 'x' || ch == 'y')
                return false;
            return true;
        }

        private bool ends(String s)
        {
            int l = s.Length;
            int o = k - l + 1;
            if (o < 0)
                return false;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                if (b[o + i] != sc[i])
                    return false;
            j = k - l;
            return true;
        }

        /* setto(s) sets (j+1),...k to the characters in the string s, readjusting
           k. */
        private void setto(String s)
        {
            int l = s.Length;
            int o = j + 1;
            char[] sc = s.ToCharArray();
            for (int i = 0; i < l; i++)
                b[o + i] = sc[i];
            k = j + l;
        }

        /* r(s) is used further down. */
        private void r(String s)
        {
            if (m() > 0)
                setto(s);
        }

        /* step1() gets rid of plurals and -ed or -ing. e.g.
               caresses  ->  caress
               ponies    ->  poni
               ties      ->  ti
               caress    ->  caress
               cats      ->  cat

               feed      ->  feed
               agreed    ->  agree
               disabled  ->  disable

               matting   ->  mat
               mating    ->  mate
               meeting   ->  meet
               milling   ->  mill
               messing   ->  mess

               meetings  ->  meet

        */

        private void step1()
        {
            if (b[k] == 's')
            {
                if (ends("sses"))
                    k -= 2;
                else if (ends("ies"))
                    setto("i");
                else if (b[k - 1] != 's')
                    k--;
            }
            if (ends("eed"))
            {
                if (m() > 0)
                    k--;
            }
            else if ((ends("ed") || ends("ing")) && vowelinstem())
            {
                k = j;
                if (ends("at"))
                    setto("ate");
                else if (ends("bl"))
                    setto("ble");
                else if (ends("iz"))
                    setto("ize");
                else if (doublec(k))
                {
                    k--;
                    int ch = b[k];
                    if (ch == 'l' || ch == 's' || ch == 'z')
                        k++;
                }
                else if (m() == 1 && cvc(k)) setto("e");
            }
        }

        /* step2() turns terminal y to i when there is another vowel in the stem. */
        private void step2()
        {
            if (ends("y") && vowelinstem())
                b[k] = 'i';
        }

        /* step3() maps double suffices to single ones. so -ization ( = -ize plus
           -ation) maps to -ize etc. note that the string before the suffix must give
           m() > 0. */
        private void step3()
        {
            if (k == 0)
                return;

            /* For Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("ational")) { r("ate"); break; }
                    if (ends("tional")) { r("tion"); break; }
                    break;
                case 'c':
                    if (ends("enci")) { r("ence"); break; }
                    if (ends("anci")) { r("ance"); break; }
                    break;
                case 'e':
                    if (ends("izer")) { r("ize"); break; }
                    break;
                case 'l':
                    if (ends("bli")) { r("ble"); break; }
                    if (ends("alli")) { r("al"); break; }
                    if (ends("entli")) { r("ent"); break; }
                    if (ends("eli")) { r("e"); break; }
                    if (ends("ousli")) { r("ous"); break; }
                    break;
                case 'o':
                    if (ends("ization")) { r("ize"); break; }
                    if (ends("ation")) { r("ate"); break; }
                    if (ends("ator")) { r("ate"); break; }
                    break;
                case 's':
                    if (ends("alism")) { r("al"); break; }
                    if (ends("iveness")) { r("ive"); break; }
                    if (ends("fulness")) { r("ful"); break; }
                    if (ends("ousness")) { r("ous"); break; }
                    break;
                case 't':
                    if (ends("aliti")) { r("al"); break; }
                    if (ends("iviti")) { r("ive"); break; }
                    if (ends("biliti")) { r("ble"); break; }
                    break;
                case 'g':
                    if (ends("logi")) { r("log"); break; }
                    break;
                default:
                    break;
            }
        }

        /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
        private void step4()
        {
            switch (b[k])
            {
                case 'e':
                    if (ends("icate")) { r("ic"); break; }
                    if (ends("ative")) { r(""); break; }
                    if (ends("alize")) { r("al"); break; }
                    break;
                case 'i':
                    if (ends("iciti")) { r("ic"); break; }
                    break;
                case 'l':
                    if (ends("ical")) { r("ic"); break; }
                    if (ends("ful")) { r(""); break; }
                    break;
                case 's':
                    if (ends("ness")) { r(""); break; }
                    break;
            }
        }

        /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
        private void step5()
        {
            if (k == 0)
                return;

            /* for Bug 1 */
            switch (b[k - 1])
            {
                case 'a':
                    if (ends("al")) break; return;
                case 'c':
                    if (ends("ance")) break;
                    if (ends("ence")) break; return;
                case 'e':
                    if (ends("er")) break; return;
                case 'i':
                    if (ends("ic")) break; return;
                case 'l':
                    if (ends("able")) break;
                    if (ends("ible")) break; return;
                case 'n':
                    if (ends("ant")) break;
                    if (ends("ement")) break;
                    if (ends("ment")) break;
                    /* element etc. not stripped before the m */
                    if (ends("ent")) break; return;
                case 'o':
                    if (ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
                    /* j >= 0 fixes Bug 2 */
                    if (ends("ou")) break; return;
                /* takes care of -ous */
                case 's':
                    if (ends("ism")) break; return;
                case 't':
                    if (ends("ate")) break;
                    if (ends("iti")) break; return;
                case 'u':
                    if (ends("ous")) break; return;
                case 'v':
                    if (ends("ive")) break; return;
                case 'z':
                    if (ends("ize")) break; return;
                default:
                    return;
            }
            if (m() > 1)
                k = j;
        }

        /* step6() removes a final -e if m() > 1. */
        private void step6()
        {
            j = k;

            if (b[k] == 'e')
            {
                int a = m();
                if (a > 1 || a == 1 && !cvc(k - 1))
                    k--;
            }
            if (b[k] == 'l' && doublec(k) && m() > 1)
                k--;
        }

        /** Stem the word placed into the Stemmer buffer through calls to add().
         * Returns true if the stemming process resulted in a word different
         * from the input.  You can retrieve the result with
         * getResultLength()/getResultBuffer() or toString().
         */
        public void stem()
        {
            k = i - 1;
            if (k > 1)
            {
                step1();
                step2();
                step3();
                step4();
                step5();
                step6();
            }
            i_end = k + 1;
            i = 0;
        }

        /** Test program for demonstrating the Stemmer.  It reads text from a
         * a list of files, stems each word, and writes the result to standard
         * output. Note that the word stemmed is expected to be in lower case:
         * forcing lower case must be done outside the Stemmer class.
         * Usage: Stemmer file-name file-name ...
         */
    }



    class Program
    {

        static string nest_pmi(string seg, Dictionary<string, double> scores)
        {
            string res = "";
            char[] space = { ' ' };
            int segLen = segmentLength(seg);
            if (segLen == 1)
            {
                res = "(" + seg + ")";
                return res;
            }
            if (segLen == 2)
            {
                string[] words2 = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
                res = "((" + words2[0] + ") (" + words2[1] + "))";
                return res;
            }


            seg = seg.ToLower();
            string[] words = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
            string[] stemmedWords = new string[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                Stemmer s = new Stemmer();
                char[] arr = words[i].ToCharArray();
                s.add(arr, words[i].Length);
                s.stem();
                stemmedWords[i] = s.ToString();
            }
            double[] biGramScores = new double[words.Length - 1];
            double[] triGramScores = new double[words.Length - 2];
            int[] biGramIndex = new int[words.Length - 1];
            int[] triGramIndex = new int[words.Length - 2];
            string bi, tri;
            for (int i = 0; i < words.Length - 2; i++)
            {
                biGramIndex[i] = i;
                triGramIndex[i] = i;
                bi = stemmedWords[i] + " " + stemmedWords[i + 1];
                string bi2 = stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                tri = stemmedWords[i] + " " + stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                biGramScores[i] = 0;
                triGramScores[i] = 0;
                if (scores.ContainsKey(bi) && scores.ContainsKey(stemmedWords[i]) && scores.ContainsKey(stemmedWords[i + 1]))
                {
                    biGramScores[i] = Math.Log(scores[bi] / (scores[stemmedWords[i]] * scores[stemmedWords[i + 1]]), 2);
                }
                if (scores.ContainsKey(tri) && scores[tri] > 0 && scores.ContainsKey(bi) && scores.ContainsKey(bi2))
                {
                    triGramScores[i] = Math.Log(scores[tri] / (scores[bi] * scores[bi2]), 2);
                }
            }
            bi = stemmedWords[words.Length - 2] + " " + stemmedWords[words.Length - 1];
            if (scores.ContainsKey(bi) && scores.ContainsKey(stemmedWords[words.Length - 2]) && scores.ContainsKey(stemmedWords[words.Length - 1]))
            {
                biGramScores[words.Length - 2] = Math.Log(scores[bi] / (scores[stemmedWords[words.Length - 1]] * scores[stemmedWords[words.Length - 2]]), 2);
            }
            else
            {
                biGramScores[words.Length - 2] = 0;
            }
            biGramIndex[words.Length - 2] = words.Length - 2;
            //////////

            for (int i = 0; i < triGramScores.Length; i++)
            {
                //Console.WriteLine(words[i] + " " + words[i + 1] + " " + words[i + 2]);
                //Console.WriteLine(triGramScores[i] + " " + biGramScores[i] + " " + biGramScores[i + 1]);
                //Console.ReadLine();
                //if (triGramScores[i] > biGramScores[i] && triGramScores[i] > biGramScores[i + 1])
                //{
                //    //countTri++;
                //    //Console.WriteLine("*" + "\t" + countTri);
                //    //sw3.WriteLine(words[i] + " " + words[i + 1] + " " + words[i + 2]);

                //}
                //else
                //{
                //    //countTri2++;
                //}

            }

            //////////
            Array.Sort(biGramScores, biGramIndex);
            Array.Sort(triGramScores, triGramIndex);
            Array.Reverse(biGramIndex);
            Array.Reverse(biGramScores);
            Array.Reverse(triGramScores);
            Array.Reverse(triGramIndex);
            /////
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = "(" + words[i] + ")";
            }
            ///////
            int count = 0;
            for (int i = 0; i < words.Length - 1; i++)
            {
                //Console.WriteLine(biGramIndex[i] + "\t" + biGramScores[i] + "\t" + stemmedWords[biGramIndex[i]] + " " + stemmedWords[biGramIndex[i] + 1]);
            }
            //Console.WriteLine();
            for (int i = 0; i < words.Length - 2; i++)
            {
                //Console.WriteLine(triGramIndex[i] + "\t" + triGramScores[i]);
            }
            //Console.ReadLine();
            int[] PartOfBi = new int[words.Length];
            int[] PartOfTri = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                PartOfBi[i] = -1;
                PartOfTri[i] = -1;
            }
            int j = 0, k = 0;
            while (j < biGramIndex.Length && biGramScores[j] > 0 && k < triGramIndex.Length && triGramScores[k] > 0)
            {
                if (biGramScores[j] > triGramScores[k])
                {

                    int idx = biGramIndex[j++];
                    Console.WriteLine(biGramScores[j - 1] + "\t" + words[idx] + " " + words[idx + 1]);
                    //Console.Read();
                    if (PartOfBi[idx] == -1 && PartOfBi[idx + 1] == -1 && PartOfTri[idx] == PartOfTri[idx + 1])
                    {
                        PartOfBi[idx] = count;
                        PartOfBi[idx + 1] = count++;
                        words[idx] = "(" + words[idx];
                        words[idx + 1] = words[idx + 1] + ")";
                    }
                }
                else
                {
                    int idx = triGramIndex[k++];
                    Console.WriteLine(triGramScores[k - 1] + "\t" + words[idx] + " " + words[idx + 1] + " " + words[idx + 2]);
                    //Console.Read();
                    if (PartOfTri[idx] != -1 || PartOfTri[idx + 2] != -1)
                        continue;
                    if ((PartOfBi[idx] == -1 && PartOfBi[idx + 2] == -1) || (PartOfBi[idx] == -1 && PartOfBi[idx + 1] != -1) || (PartOfBi[idx + 2] == -1 && PartOfBi[idx + 1] != -1))
                    {
                        PartOfTri[idx] = PartOfTri[idx + 1] = PartOfTri[idx + 2] = count++;
                        words[idx] = "(" + words[idx];
                        words[idx + 2] = words[idx + 2] + ")";
                    }
                }
            }

            while (j < biGramIndex.Length && biGramScores[j] > 0)
            {
                int idx = biGramIndex[j++];
                Console.WriteLine(biGramScores[j - 1] + "\t" + words[idx] + " " + words[idx + 1]);
                //Console.Read();

                if (PartOfBi[idx] == -1 && PartOfBi[idx + 1] == -1 && PartOfTri[idx] == PartOfTri[idx + 1])
                {
                    PartOfBi[idx] = count;
                    PartOfBi[idx + 1] = count++;
                    words[idx] = "(" + words[idx];
                    words[idx + 1] = words[idx + 1] + ")";
                }
            }

            while (k < triGramIndex.Length && triGramScores[k] > 0)
            {
                int idx = triGramIndex[k++];
                Console.WriteLine(triGramScores[k - 1] + "\t" + words[idx] + " " + words[idx + 1] + " " + words[idx + 2]);
                //Console.Read();

                if (PartOfTri[idx] != -1 || PartOfTri[idx + 2] != -1)
                    continue;
                if ((PartOfBi[idx] == -1 && PartOfBi[idx + 2] == -1) || (PartOfBi[idx] == -1 && PartOfBi[idx + 1] != -1) || (PartOfBi[idx + 2] == -1 && PartOfBi[idx + 1] != -1))
                {
                    PartOfTri[idx] = PartOfTri[idx + 1] = PartOfTri[idx + 2] = count++;
                    words[idx] = "(" + words[idx];
                    words[idx + 2] = words[idx + 2] + ")";
                }
            }

            for (int i = 0; i < words.Length; i++)
            {
                res = res + words[i] + " ";
            }
            res = res.Trim();
            if (!(words.Length == 3 && PartOfTri[0] != -1 && PartOfTri[2] != -1))
                res = "(" + res + ")";
            //Console.WriteLine(res);
            return res;
        }

        static string mergeSegments_pmi(List<string> segments, Dictionary<string, double> scores)
        {
            if (segments.Count == 1)
                return segments.ElementAt(0);
            if (segments.Count == 2)
                return "(" + segments.ElementAt(0) + " " + segments.ElementAt(1) + ")";
            List<string> firstWord = new List<string>();
            List<string> lastWord = new List<string>();
            for (int i = 0; i < segments.Count; i++)
            {
                string seg = segments.ElementAt(i);
                int idx = seg.IndexOf(' ');
                string word1, word2;
                if (idx == -1)
                {
                    word1 = word2 = seg;
                }
                else
                {
                    word1 = seg.Substring(0, seg.IndexOf(' '));
                    word2 = seg.Substring(seg.LastIndexOf(' ') + 1);

                }
                char[] chars = { ' ', '(', ')' };
                word1 = word1.Trim(chars);
                word2 = word2.Trim(chars);
                Stemmer s = new Stemmer();
                char[] arr = word1.ToCharArray();
                s.add(arr, word1.Length);
                s.stem();
                string stemmedWord1 = s.ToString();
                s = new Stemmer();
                char[] arr2 = word2.ToCharArray();
                s.add(arr2, word2.Length);
                s.stem();
                string stemmedWord2 = s.ToString();
                firstWord.Add(stemmedWord1);
                lastWord.Add(stemmedWord2);
            }
            //////////
            //Console.WriteLine("**");
            for (int i = 0; i < segments.Count; i++)
            {
                //Console.WriteLine(segments.ElementAt(i) + "\t" + firstWord.ElementAt(i) + "\t" + lastWord.ElementAt(i));
            }
            //Console.WriteLine("**");
            //////////
            while (segments.Count > 2)
            {
                int maxPos = -1;
                double maxPmi = 0;
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    //Console.WriteLine("i=" + i);
                    string word2 = lastWord.ElementAt(i);
                    string word1 = firstWord.ElementAt(i + 1);
                    double pmi;
                    double p1, p2, p12;
                    if (!scores.ContainsKey(word1))
                    {
                        continue;
                    }
                    else
                    {
                        p1 = scores[word1];
                    }
                    if (!scores.ContainsKey(word2))
                    {
                        continue;
                    }
                    else
                    {
                        p2 = scores[word2];
                    }
                    if (!scores.ContainsKey(word2 + " " + word1))
                    {
                        continue;
                    }
                    else
                    {
                        p12 = scores[word2 + " " + word1];
                    }
                    pmi = Math.Log(p12 / (p1 * p2), 2);
                    //Console.WriteLine("i = " + i + "  " + pmi);
                    if (pmi > maxPmi)
                    {
                        maxPmi = pmi;
                        maxPos = i;
                    }
                }
                if (maxPos == -1)
                    break;
                string newSeg = "(" + segments.ElementAt(maxPos) + " " + segments.ElementAt(maxPos + 1) + ")";
                //Console.WriteLine(newSeg);

                segments.RemoveRange(maxPos, 2);
                segments.Insert(maxPos, newSeg);
                firstWord.RemoveAt(maxPos + 1);
                lastWord.RemoveAt(maxPos);
            }
            string ans = "(";
            for (int i = 0; i < segments.Count; i++)
            {
                ans = ans + segments.ElementAt(i) + " ";
            }
            ans = ans.Trim();
            ans = ans + ")";
            //Console.WriteLine(ans);
            //Console.Read();
            return ans;
        }

        static string mergeSegments_pmi_2(List<string> segments, Dictionary<string, double> scores, HashSet<string> conjPrepDet)
        {
            if (segments.Count == 1)
                return segments.ElementAt(0);
            if (segments.Count == 2)
                return "(" + segments.ElementAt(0) + " " + segments.ElementAt(1) + ")";
            List<string> firstWord = new List<string>();
            List<string> lastWord = new List<string>();
            for (int i = 0; i < segments.Count; i++)
            {
                string seg = segments.ElementAt(i);
                int idx = seg.IndexOf(' ');
                string word1, word2;
                if (idx == -1)
                {
                    word1 = word2 = seg;
                }
                else
                {
                    word1 = seg.Substring(0, seg.IndexOf(' '));
                    word2 = seg.Substring(seg.LastIndexOf(' ') + 1);

                }
                char[] chars = { ' ', '(', ')' };
                word1 = word1.Trim(chars);
                word2 = word2.Trim(chars);
                Stemmer s = new Stemmer();
                char[] arr = word1.ToCharArray();
                s.add(arr, word1.Length);
                s.stem();
                string stemmedWord1 = s.ToString();
                s = new Stemmer();
                char[] arr2 = word2.ToCharArray();
                s.add(arr2, word2.Length);
                s.stem();
                string stemmedWord2 = s.ToString();
                firstWord.Add(stemmedWord1);
                lastWord.Add(stemmedWord2);
            }
            //////////
            //Console.WriteLine("**");
            for (int i = 0; i < segments.Count; i++)
            {
                //Console.WriteLine(segments.ElementAt(i) + "\t" + firstWord.ElementAt(i) + "\t" + lastWord.ElementAt(i));
            }
            //Console.WriteLine("**");
            //////////
            while (segments.Count > 2)
            {
                int maxPos = -1;
                double maxPmi = 0;
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    //Console.WriteLine("i=" + i);
                    string word2 = lastWord.ElementAt(i);
                    string word1 = firstWord.ElementAt(i + 1);
                    string seg1 = segments.ElementAt(i);
                    string seg2 = segments.ElementAt(i + 1);
                    string word1_unstemmed, word2_unstemmed;
                    int idx = seg1.IndexOf(' ');
                    if (idx == -1)
                    {
                        word2_unstemmed = seg1;
                    }
                    else
                    {
                        word2_unstemmed = seg1.Substring(seg1.LastIndexOf(' ') + 1);
                    }

                    idx = seg2.IndexOf(' ');
                    if (idx == -1)
                    {
                        word1_unstemmed = seg2;
                    }
                    else
                    {
                        word1_unstemmed = seg2.Substring(0, seg2.IndexOf(' '));
                    }

                    char[] chars = { ' ', '(', ')' };
                    word1_unstemmed = word1_unstemmed.Trim(chars);
                    word2_unstemmed = word2_unstemmed.Trim(chars);
                    if (conjPrepDet.Contains(word2_unstemmed) || conjPrepDet.Contains(word1_unstemmed))
                    {
                        maxPos = i;
                        break;
                    }
                    double pmi;
                    double p1, p2, p12;
                    if (!scores.ContainsKey(word1))
                    {
                        continue;
                    }
                    else
                    {
                        p1 = scores[word1];
                    }
                    if (!scores.ContainsKey(word2))
                    {
                        continue;
                    }
                    else
                    {
                        p2 = scores[word2];
                    }
                    if (!scores.ContainsKey(word2 + " " + word1))
                    {
                        continue;
                    }
                    else
                    {
                        p12 = scores[word2 + " " + word1];
                    }
                    pmi = Math.Log(p12 / (p1 * p2), 2);
                    //Console.WriteLine("i = " + i + "  " + pmi);
                    if (pmi > maxPmi)
                    {
                        maxPmi = pmi;
                        maxPos = i;
                    }
                }
                if (maxPos == -1)
                    break;
                string newSeg = "(" + segments.ElementAt(maxPos) + " " + segments.ElementAt(maxPos + 1) + ")";
                //Console.WriteLine(newSeg);

                segments.RemoveRange(maxPos, 2);
                segments.Insert(maxPos, newSeg);
                firstWord.RemoveAt(maxPos + 1);
                lastWord.RemoveAt(maxPos);
            }
            string ans = "(";
            for (int i = 0; i < segments.Count; i++)
            {
                ans = ans + segments.ElementAt(i) + " ";
            }
            ans = ans.Trim();
            ans = ans + ")";
            //Console.WriteLine(ans);
            //Console.Read();
            return ans;
        }

        static int segmentLength(string seg)
        {
            int count = 1;
            for (int i = 0; i < seg.Length; i++)
            {
                if (seg[i] == ' ')
                    count++;
            }
            return count;
        }

        static string nest_hoeffding(string seg, Dictionary<string, double> scores)
        {
            string res = "";
            char[] space = { ' ' };
            int segLen = segmentLength(seg);
            if (segLen == 1)
            {
                res = "(" + seg + ")";
                return res;
            }
            if (segLen == 2)
            {
                string[] words2 = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
                res = "((" + words2[0] + ") (" + words2[1] + "))";
                return res;
            }


            seg = seg.ToLower();
            string[] words = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
            string[] stemmedWords = new string[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                Stemmer s = new Stemmer();
                char[] arr = words[i].ToCharArray();
                s.add(arr, words[i].Length);
                s.stem();
                stemmedWords[i] = s.ToString();
            }
            double[] biGramScores = new double[words.Length - 1];
            double[] triGramScores = new double[words.Length - 2];
            int[] biGramIndex = new int[words.Length - 1];
            int[] triGramIndex = new int[words.Length - 2];
            string bi, tri;
            for (int i = 0; i < words.Length - 2; i++)
            {
                biGramIndex[i] = i;
                triGramIndex[i] = i;
                bi = stemmedWords[i] + " " + stemmedWords[i + 1];
                tri = stemmedWords[i] + " " + stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                biGramScores[i] = 0;
                triGramScores[i] = 0;
                if (scores.ContainsKey(bi))
                {
                    biGramScores[i] = scores[bi];
                }
                if (scores.ContainsKey(tri))
                {
                    triGramScores[i] = scores[tri];
                }
            }
            bi = stemmedWords[words.Length - 2] + " " + stemmedWords[words.Length - 1];
            if (scores.ContainsKey(bi))
            {
                biGramScores[words.Length - 2] = scores[bi];
            }
            else
            {
                biGramScores[words.Length - 2] = 0;
            }
            biGramIndex[words.Length - 2] = words.Length - 2;
            //////////

            for (int i = 0; i < triGramScores.Length; i++)
            {
                //Console.WriteLine(words[i] + " " + words[i + 1] + " " + words[i + 2]);
                //Console.WriteLine(triGramScores[i] + " " + biGramScores[i] + " " + biGramScores[i + 1]);

            }

            //////////
            Array.Sort(biGramScores, biGramIndex);
            Array.Sort(triGramScores, triGramIndex);
            Array.Reverse(biGramIndex);
            Array.Reverse(biGramScores);
            Array.Reverse(triGramScores);
            Array.Reverse(triGramIndex);
            /////
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = "(" + words[i] + ")";
            }
            ///////
            int count = 0;
            for (int i = 0; i < words.Length - 1; i++)
            {
                //Console.WriteLine(biGramIndex[i] + "\t" + biGramScores[i] + "\t" + stemmedWords[biGramIndex[i]] + " " + stemmedWords[biGramIndex[i] + 1]);
            }
            //Console.WriteLine();
            for (int i = 0; i < words.Length - 2; i++)
            {
                //Console.WriteLine(triGramIndex[i] + "\t" + triGramScores[i]);
            }
            //Console.ReadLine();
            int[] PartOfBi = new int[words.Length];
            int[] PartOfTri = new int[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                PartOfBi[i] = -1;
                PartOfTri[i] = -1;
            }
            int j = 0, k = 0;
            while (j < biGramIndex.Length && biGramScores[j] > 0 && k < triGramIndex.Length && triGramScores[k] > 0)
            {
                if (biGramScores[j] > triGramScores[k])
                {
                    int idx = biGramIndex[j++];
                    if (PartOfBi[idx] == -1 && PartOfBi[idx + 1] == -1 && PartOfTri[idx] == PartOfTri[idx + 1])
                    {
                        PartOfBi[idx] = count;
                        PartOfBi[idx + 1] = count++;
                        words[idx] = "(" + words[idx];
                        words[idx + 1] = words[idx + 1] + ")";
                    }
                }
                else
                {
                    int idx = triGramIndex[k++];
                    if (PartOfTri[idx] != -1 || PartOfTri[idx + 2] != -1)
                        continue;
                    if ((PartOfBi[idx] == -1 && PartOfBi[idx + 2] == -1) || (PartOfBi[idx] == -1 && PartOfBi[idx + 1] != -1) || (PartOfBi[idx + 2] == -1 && PartOfBi[idx + 1] != -1))
                    {
                        PartOfTri[idx] = PartOfTri[idx + 1] = PartOfTri[idx + 2] = count++;
                        words[idx] = "(" + words[idx];
                        words[idx + 2] = words[idx + 2] + ")";
                    }
                }
            }

            while (j < biGramIndex.Length && biGramScores[j] > 0)
            {
                int idx = biGramIndex[j++];
                if (PartOfBi[idx] == -1 && PartOfBi[idx + 1] == -1 && PartOfTri[idx] == PartOfTri[idx + 1])
                {
                    PartOfBi[idx] = count;
                    PartOfBi[idx + 1] = count++;
                    words[idx] = "(" + words[idx];
                    words[idx + 1] = words[idx + 1] + ")";
                }
            }

            while (k < triGramIndex.Length && triGramScores[k] > 0)
            {
                int idx = triGramIndex[k++];
                if (PartOfTri[idx] != -1 || PartOfTri[idx + 2] != -1)
                    continue;
                if ((PartOfBi[idx] == -1 && PartOfBi[idx + 2] == -1) || (PartOfBi[idx] == -1 && PartOfBi[idx + 1] != -1) || (PartOfBi[idx + 2] == -1 && PartOfBi[idx + 1] != -1))
                {
                    PartOfTri[idx] = PartOfTri[idx + 1] = PartOfTri[idx + 2] = count++;
                    words[idx] = "(" + words[idx];
                    words[idx + 2] = words[idx + 2] + ")";
                }
            }

            for (int i = 0; i < words.Length; i++)
            {
                res = res + words[i] + " ";
            }
            res = res.Trim();
            if (!(words.Length == 3 && PartOfTri[0] != -1 && PartOfTri[2] != -1))
                res = "(" + res + ")";
            //Console.WriteLine(res);
            return res;
        }

        static string mergeSegments_hoeffding(List<string> segments, Dictionary<string, double> scores)
        {
            if (segments.Count == 1)
                return segments.ElementAt(0);
            if (segments.Count == 2)
                return "(" + segments.ElementAt(0) + " " + segments.ElementAt(1) + ")";
            List<string> firstWord = new List<string>();
            List<string> lastWord = new List<string>();
            for (int i = 0; i < segments.Count; i++)
            {
                string seg = segments.ElementAt(i);
                int idx = seg.IndexOf(' ');
                string word1, word2;
                if (idx == -1)
                {
                    word1 = word2 = seg;
                }
                else
                {
                    word1 = seg.Substring(0, seg.IndexOf(' '));
                    word2 = seg.Substring(seg.LastIndexOf(' ') + 1);

                }
                char[] chars = { ' ', '(', ')' };
                word1 = word1.Trim(chars);
                word2 = word2.Trim(chars);
                Stemmer s = new Stemmer();
                char[] arr = word1.ToCharArray();
                s.add(arr, word1.Length);
                s.stem();
                string stemmedWord1 = s.ToString();
                s = new Stemmer();
                char[] arr2 = word2.ToCharArray();
                s.add(arr2, word2.Length);
                s.stem();
                string stemmedWord2 = s.ToString();
                firstWord.Add(stemmedWord1);
                lastWord.Add(stemmedWord2);
            }
            //////////
            Console.WriteLine("**");
            for (int i = 0; i < segments.Count; i++)
            {
                Console.WriteLine(segments.ElementAt(i) + "\t" + firstWord.ElementAt(i) + "\t" + lastWord.ElementAt(i));
            }
            Console.WriteLine("**");
            //////////
            while (segments.Count > 2)
            {
                int maxPos = -1;
                double maxScore = 0;
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    Console.WriteLine("i=" + i);
                    string word2 = lastWord.ElementAt(i);
                    string word1 = firstWord.ElementAt(i + 1);
                    double score;
                    if (!scores.ContainsKey(word2 + " " + word1))
                    {
                        continue;
                    }
                    else
                    {
                        score = scores[word2 + " " + word1];
                    }
                    //pmi = Math.Log(p12 / (p1 * p2), 2);
                    Console.WriteLine("i = " + i + "  " + word2 + " " + word1 + " " + score);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxPos = i;
                    }
                }
                if (maxPos == -1)
                    break;
                string newSeg = "(" + segments.ElementAt(maxPos) + " " + segments.ElementAt(maxPos + 1) + ")";
                Console.WriteLine(newSeg);

                segments.RemoveRange(maxPos, 2);
                segments.Insert(maxPos, newSeg);
                firstWord.RemoveAt(maxPos + 1);
                lastWord.RemoveAt(maxPos);
            }
            string ans = "(";
            for (int i = 0; i < segments.Count; i++)
            {
                ans = ans + segments.ElementAt(i) + " ";
            }
            ans = ans.Trim();
            ans = ans + ")";
            Console.WriteLine(ans);
            //Console.Read();
            return ans;
        }

        static string mergeSegments_hoeffding_2(List<string> segments, Dictionary<string, double> scores, HashSet<string> conjPrepDet)
        {
            if (segments.Count == 1)
                return segments.ElementAt(0);
            if (segments.Count == 2)
                return "(" + segments.ElementAt(0) + " " + segments.ElementAt(1) + ")";
            List<string> firstWord = new List<string>();
            List<string> lastWord = new List<string>();
            for (int i = 0; i < segments.Count; i++)
            {
                string seg = segments.ElementAt(i);
                int idx = seg.IndexOf(' ');
                string word1, word2;
                if (idx == -1)
                {
                    word1 = word2 = seg;
                }
                else
                {
                    word1 = seg.Substring(0, seg.IndexOf(' '));
                    word2 = seg.Substring(seg.LastIndexOf(' ') + 1);

                }
                char[] chars = { ' ', '(', ')' };
                word1 = word1.Trim(chars);
                word2 = word2.Trim(chars);
                Stemmer s = new Stemmer();
                char[] arr = word1.ToCharArray();
                s.add(arr, word1.Length);
                s.stem();
                string stemmedWord1 = s.ToString();
                s = new Stemmer();
                char[] arr2 = word2.ToCharArray();
                s.add(arr2, word2.Length);
                s.stem();
                string stemmedWord2 = s.ToString();
                firstWord.Add(stemmedWord1);
                lastWord.Add(stemmedWord2);
            }
            //////////
            Console.WriteLine("**");
            for (int i = 0; i < segments.Count; i++)
            {
                Console.WriteLine(segments.ElementAt(i) + "\t" + firstWord.ElementAt(i) + "\t" + lastWord.ElementAt(i));
            }
            Console.WriteLine("**");
            //////////
            while (segments.Count > 2)
            {
                int maxPos = -1;
                double maxScore = 0;
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    Console.WriteLine("i=" + i);
                    string word2 = lastWord.ElementAt(i);
                    string word1 = firstWord.ElementAt(i + 1);
                    string seg1 = segments.ElementAt(i);
                    string seg2 = segments.ElementAt(i + 1);
                    string word1_unstemmed, word2_unstemmed;
                    int idx = seg1.IndexOf(' ');
                    if (idx == -1)
                    {
                        word2_unstemmed = seg1;
                    }
                    else
                    {
                        word2_unstemmed = seg1.Substring(seg1.LastIndexOf(' ') + 1);
                    }

                    idx = seg2.IndexOf(' ');
                    if (idx == -1)
                    {
                        word1_unstemmed = seg2;
                    }
                    else
                    {
                        word1_unstemmed = seg2.Substring(0, seg2.IndexOf(' '));
                    }

                    char[] chars = { ' ', '(', ')' };
                    word1_unstemmed = word1_unstemmed.Trim(chars);
                    word2_unstemmed = word2_unstemmed.Trim(chars);
                    if (conjPrepDet.Contains(word2_unstemmed) || conjPrepDet.Contains(word1_unstemmed))
                    {
                        maxPos = i;
                        break;
                    }
                    double score;
                    if (!scores.ContainsKey(word2 + " " + word1))
                    {
                        continue;
                    }
                    else
                    {
                        score = scores[word2 + " " + word1];
                    }
                    //pmi = Math.Log(p12 / (p1 * p2), 2);
                    Console.WriteLine("i = " + i + "  " + word2 + " " + word1 + " " + score);
                    if (score > maxScore)
                    {
                        maxScore = score;
                        maxPos = i;
                    }
                }
                if (maxPos == -1)
                    break;
                string newSeg = "(" + segments.ElementAt(maxPos) + " " + segments.ElementAt(maxPos + 1) + ")";
                Console.WriteLine(newSeg);

                segments.RemoveRange(maxPos, 2);
                segments.Insert(maxPos, newSeg);
                firstWord.RemoveAt(maxPos + 1);
                lastWord.RemoveAt(maxPos);
            }
            string ans = "(";
            for (int i = 0; i < segments.Count; i++)
            {
                ans = ans + segments.ElementAt(i) + " ";
            }
            ans = ans.Trim();
            ans = ans + ")";
            Console.WriteLine(ans);
            //Console.Read();
            return ans;
        }

        double maximum(double a, double b)
        {
            return a > b ? a : b;
        }

        static string nest_hoeffding_optimized(string seg, Dictionary<string, double> scores)
        {
            int segLen = segmentLength(seg);
            string res = "";
            char[] space = { ' ' };
            if (segLen == 1)
            {
                res = "(" + seg + ")";
                return res;
            }
            if (segLen == 2)
            {
                string[] words2 = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
                res = "((" + words2[0] + ") (" + words2[1] + "))";
                return res;
            }


            seg = seg.ToLower();
            string[] words = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
            string[] stemmedWords = new string[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                Stemmer s = new Stemmer();
                char[] arr = words[i].ToCharArray();
                s.add(arr, words[i].Length);
                s.stem();
                stemmedWords[i] = s.ToString();
            }
            double[,] maxScore = new double[segLen, segLen];
            int[,] index = new int[segLen, segLen];
            for (int i = 0; i < segLen; i++)
            {
                for (int j = i; j < segLen; j++)
                {
                    maxScore[i, j] = 0;
                    index[i, j] = -1;
                }
            }
            for (int i = 0; i < segLen - 1; i++)
            {
                string bi = stemmedWords[i] + " " + stemmedWords[i + 1];
                if (scores.ContainsKey(bi))
                    maxScore[i, i + 1] = scores[bi];
                else
                    maxScore[i, i + 1] = 0;
            }
            for (int i = 0; i < segLen - 2; i++)
            {
                string tri = stemmedWords[i] + " " + stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                if (scores.ContainsKey(tri))
                    maxScore[i, i + 2] = scores[tri];
                else
                    maxScore[i, i + 2] = 0;
            }

            for (int k = 1; k < segLen; k++)
            {
                for (int i = 0; i + k < segLen; i++)
                {
                    double max = maxScore[i, i + k];
                    int idx = -1;
                    for (int j = i; j < i + k; j++)
                    {
                        double currScore = maxScore[i, j] + maxScore[j + 1, i + k];
                        if (currScore > max)
                        {
                            max = currScore;
                            idx = j;
                        }
                    }
                    maxScore[i, i + k] = max;
                    index[i, i + k] = idx;
                }
            }

            /////
            //Console.WriteLine(seg);
            //for (int i = 0; i < segLen; i++)
            //{
            //    for (int j = 0; j < segLen; j++)
            //    {
            //        Console.Write(maxScore[i, j] + "/" + index[i, j] + "\t");
            //    }
            //    Console.WriteLine();
            //}
            //Console.Read();
            ///////
            for (int i = 0; i < segLen; i++)
            {
                words[i] = "(" + words[i] + ")";
            }
            getSegmentation(maxScore, index, words, 0, segLen - 1);
            //for (int i = 0; i < segLen; i++)
            //{
            //    Console.Write(words[i] + " ");
            //}
            //Console.WriteLine();
            if (!(segLen == 3 && index[0, 2] == -1))
            {
                words[0] = "(" + words[0];
                words[segLen - 1] = words[segLen - 1] + ")";
            }
            for (int i = 0; i < segLen; i++)
            {
                res = res + words[i] + " ";
            }

            return res.Trim();
        }

        static string nest_pmi_optimized(string seg, Dictionary<string, double> scores)
        {
            int segLen = segmentLength(seg);
            string res = "";
            char[] space = { ' ' };
            if (segLen == 1)
            {
                res = "(" + seg + ")";
                return res;
            }
            if (segLen == 2)
            {
                string[] words2 = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
                res = "((" + words2[0] + ") (" + words2[1] + "))";
                return res;
            }


            seg = seg.ToLower();
            string[] words = seg.Split(space, StringSplitOptions.RemoveEmptyEntries);
            string[] stemmedWords = new string[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                Stemmer s = new Stemmer();
                char[] arr = words[i].ToCharArray();
                s.add(arr, words[i].Length);
                s.stem();
                stemmedWords[i] = s.ToString();
            }
            double[,] maxScore = new double[segLen, segLen];
            int[,] index = new int[segLen, segLen];
            for (int i = 0; i < segLen; i++)
            {
                for (int j = i; j < segLen; j++)
                {
                    maxScore[i, j] = 0;
                    index[i, j] = -1;
                }
            }
            for (int i = 0; i < segLen - 1; i++)
            {
                string bi = stemmedWords[i] + " " + stemmedWords[i + 1];
                if (scores.ContainsKey(bi) && scores.ContainsKey(stemmedWords[i]) && scores.ContainsKey(stemmedWords[i + 1]))
                    maxScore[i, i + 1] = Math.Log(scores[bi] / (scores[stemmedWords[i]] * scores[stemmedWords[i + 1]]), 2);
                else
                    maxScore[i, i + 1] = 0;
            }
            for (int i = 0; i < segLen - 2; i++)
            {
                string tri = stemmedWords[i] + " " + stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                string bi1 = stemmedWords[i] + " " + stemmedWords[i + 1];
                string bi2 = stemmedWords[i + 1] + " " + stemmedWords[i + 2];
                if (scores.ContainsKey(tri) && scores[tri] > 0 && scores.ContainsKey(bi1) && scores.ContainsKey(bi2))
                    maxScore[i, i + 2] = Math.Log(scores[tri] / (scores[bi1] * scores[bi2]), 2);
                else
                    maxScore[i, i + 2] = 0;
            }

            for (int k = 1; k < segLen; k++)
            {
                for (int i = 0; i + k < segLen; i++)
                {
                    double max = maxScore[i, i + k];
                    int idx = -1;
                    for (int j = i; j < i + k; j++)
                    {
                        double currScore = maxScore[i, j] + maxScore[j + 1, i + k];
                        if (currScore > max)
                        {
                            max = currScore;
                            idx = j;
                        }
                    }
                    maxScore[i, i + k] = max;
                    index[i, i + k] = idx;
                }
            }

            /////
            //Console.WriteLine(seg);
            //for (int i = 0; i < segLen; i++)
            //{
            //    for (int j = 0; j < segLen; j++)
            //    {
            //        Console.Write(maxScore[i, j] + "/" + index[i, j] + "\t");
            //    }
            //    Console.WriteLine();
            //}
            //Console.Read();
            ///////
            for (int i = 0; i < segLen; i++)
            {
                words[i] = "(" + words[i] + ")";
            }
            getSegmentation(maxScore, index, words, 0, segLen - 1);
            //for (int i = 0; i < segLen; i++)
            //{
            //    Console.Write(words[i] + " ");
            //}
            //Console.WriteLine();
            if (!(segLen == 3 && index[0, 2] == -1))
            {
                words[0] = "(" + words[0];
                words[segLen - 1] = words[segLen - 1] + ")";
            }
            for (int i = 0; i < segLen; i++)
            {
                res = res + words[i] + " ";
            }

            return res.Trim();
        }

        static void getSegmentation(double[,] maxScores, int[,] index, string[] words, int start, int end)
        {
            if (start == end)
                return;
            if (start == end - 1)
            {
                if (index[start, end] != -1)
                {
                    return;
                }
                words[start] = "(" + words[start];
                words[end] = words[end] + ")";
                return;
            }
            if (start == end - 2)
            {
                if (maxScores[start, end] == 0)
                    return;
                if (index[start, end] == -1)
                {
                    words[start] = "(" + words[start];
                    words[end] = words[end] + ")";
                    if (maxScores[start, start + 1] > maxScores[start + 1, end] && maxScores[start, start + 1] > 0)
                    {
                        words[start] = "(" + words[start];
                        words[start + 1] = words[start + 1] + ")";
                    }
                    else if (maxScores[start, start + 1] < maxScores[start + 1, end] && maxScores[start + 1, end] > 0)
                    {
                        words[start + 1] = "(" + words[start + 1];
                        words[end] = words[end] + ")";
                    }
                    return;
                }
            }
            if (index[start, end] == -1 || maxScores[start, end] == 0)
            {
                return;
            }
            getSegmentation(maxScores, index, words, start, index[start, end]);
            getSegmentation(maxScores, index, words, index[start, end] + 1, end);
        }

        static void nestedSegmentation(string[] args)
        {
            HashSet<string> conjPrepDet = new HashSet<string>();
            using (StreamReader sr = new StreamReader("EnglishConjunctions.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }
            using (StreamReader sr = new StreamReader("EnglishPrepositions.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }
            using (StreamReader sr = new StreamReader("EnglishDeterminers.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }

            Dictionary<string, double> scores_pmi = new Dictionary<string, double>();
            char[] tab = { '\t' };
            using (StreamReader sr = new StreamReader("1_2_gram_probab.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_pmi[cols[0]] = double.Parse(cols[1]);
                }
            }

            using (StreamReader sr = new StreamReader("triGramProb_trec_wt09-12.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_pmi[cols[0]] = double.Parse(cols[1]);
                }
            }

            Dictionary<string, double> scores_hoeff = new Dictionary<string, double>();
            using (StreamReader sr = new StreamReader("model1.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_hoeff[cols[1]] = double.Parse(cols[0]);
                }
            }
            using (StreamWriter sw = new StreamWriter("scheme_005.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string ans = "";
                        List<string> segments = new List<string>();
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }

                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_006.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string ans = "";
                        List<string> segments = new List<string>();
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }

                }
            }


            using (StreamWriter sw = new StreamWriter("scheme_001.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi(seg, scores_pmi);
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_002.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi(seg, scores_pmi);
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_011.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_009.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_012.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_010.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_014.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_013.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_008.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_007.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }


            using (StreamWriter sw = new StreamWriter("scheme_004.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_003.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_016.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_015.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            //ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }
        }

        static void nestedSegmentation_NE(string[] args)
        {
            HashSet<string> conjPrepDet = new HashSet<string>();
            using (StreamReader sr = new StreamReader("EnglishConjunctions.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }
            using (StreamReader sr = new StreamReader("EnglishPrepositions.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }
            using (StreamReader sr = new StreamReader("EnglishDeterminers.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    conjPrepDet.Add(line);
                }
            }

            Dictionary<string, double> scores_pmi = new Dictionary<string, double>();
            char[] tab = { '\t' };
            using (StreamReader sr = new StreamReader("1_2_gram_probab.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_pmi[cols[0]] = double.Parse(cols[1]);
                }
            }

            using (StreamReader sr = new StreamReader("triGramProb_trec_wt09-12.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_pmi[cols[0]] = double.Parse(cols[1]);
                }
            }

            Dictionary<string, double> scores_hoeff = new Dictionary<string, double>();
            using (StreamReader sr = new StreamReader("model1.txt"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] cols = line.Split(tab);
                    scores_hoeff[cols[1]] = double.Parse(cols[0]);
                }
            }
            HashSet<string> ne = new HashSet<string>();
            using (StreamReader sr = new StreamReader(args[1]))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!ne.Contains(line))
                        ne.Add(line);
                }
            }
            using (StreamWriter sw = new StreamWriter("scheme_005.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string ans = "";
                        List<string> segments = new List<string>();
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                            {
                                nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            }
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }

                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_006.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string ans = "";
                        List<string> segments = new List<string>();
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }

                }
            }


            using (StreamWriter sw = new StreamWriter("scheme_001.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_002.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_011.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_009.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_012.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }

                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_010.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_014.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_013.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_008.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_007.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_hoeffding_optimized(seg, scores_hoeff);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }


            using (StreamWriter sw = new StreamWriter("scheme_004.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi_2(segments, scores_pmi, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_003.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_pmi(segments, scores_pmi);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_016.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding_2(segments, scores_hoeff, conjPrepDet);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter("scheme_015.txt"))
            {
                using (StreamReader sr = new StreamReader(args[0]))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        List<string> segments = new List<string>();
                        string ans = "";
                        while (line.Length > 0)
                        {
                            int idx1 = line.IndexOf('"');
                            line = line.Substring(idx1 + 1);
                            idx1 = line.IndexOf('"');
                            string seg = line.Substring(0, idx1);
                            line = line.Substring(idx1 + 1).Trim();
                            string nestedSeg;
                            if (ne.Contains(seg))
                            {
                                nestedSeg = "";
                                char[] space = { ' ' };
                                string[] words = seg.Split(space);
                                for (int i = 0; i < words.Length; i++)
                                {
                                    nestedSeg += "(" + words[i] + ") ";
                                }
                                nestedSeg = nestedSeg.Trim();
                                if (words.Length > 1)
                                    nestedSeg = "(" + nestedSeg + ")";
                            }
                            else
                                nestedSeg = nest_pmi_optimized(seg, scores_pmi);
                            ans = ans + nestedSeg + " ";
                            segments.Add(nestedSeg);
                            ans = mergeSegments_hoeffding(segments, scores_hoeff);
                        }
                        sw.Write(ans.Trim());
                        sw.WriteLine();
                    }
                }
            }
        }

        static void Main(string[] args)
        {
           // nestedSegmentation_NE(args);
            nestedSegmentation(args);
        }
    }
}

