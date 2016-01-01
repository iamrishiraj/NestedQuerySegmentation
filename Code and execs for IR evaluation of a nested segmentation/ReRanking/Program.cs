using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using porter;

namespace ReRanking
{
    class Program
    {
        static int docLength(string docName)
        {
            int count = 0;
            StreamReader sr;
            try
            {
                sr = new StreamReader("Web_Documents/" + docName);
            }
            catch (Exception)
            {
                return 0;
            }
            string line;
            char[] delim = { '.', ',', ';', ':', '-', '!', '?', '"', '\'', '`', '(', ')', '[', ']', '{', '}', ' ', '\t' };
            while ((line = sr.ReadLine()) != null)
            {
                string[] words = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                count += words.Length;
            }
            sr.Close();
            //}
            return count;
        }

        static List<int> positions(string word, string docName)
        {
            word = word.ToLower();
            Stemmer s = new Stemmer();
            char[] arr = word.ToCharArray();
            s.add(arr, word.Length);
            s.stem();
            word = s.ToString();
            List<int> pos = new List<int>();
            using (StreamReader sr = new StreamReader("Web_Documents/" + docName))
            {
                string line;
                int count = 0;
                char[] delim = { '.', ',', ';', ':', '-', '!', '?', '"', '\'', '`', '(', ')', '[', ']', '{', '}', ' ', '\t' };
                while ((line = sr.ReadLine()) != null)
                {
                    //line = line.Replace(".", " ");
                    //line = line.Replace(",", " ");
                    //line = line.Replace(";", " ");
                    //line = line.Replace(":", " ");
                    //line = line.Replace("-", " ");
                    //line = line.Replace("!", " ");
                    //line = line.Replace("?", " ");
                    //line = line.Replace("\"", "");
                    //line = line.Replace("'", " ");
                    //line = line.Replace("`", " ");
                    //line = line.Replace("(", "");
                    //line = line.Replace(")", "");
                    //line = line.Replace("[", "");
                    //line = line.Replace("]", "");
                    //line = line.Replace("{", "");
                    //line = line.Replace("}", "");
                    string[] words = line.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    //for (int i = 0; i < words.Length; i++)
                    //{
                    //    Console.Write(words[i] + " ");
                    //}
                    //Console.WriteLine();
                    for (int i = 0; i < words.Length; i++)
                    {
                        words[i] = words[i].ToLower();
                        Stemmer s2 = new Stemmer();
                        char[] arr2 = words[i].ToCharArray();
                        s2.add(arr2, words[i].Length);
                        s2.stem();
                        words[i] = s2.ToString();
                        count++;
                        if (words[i].Equals(word))
                        {
                            pos.Add(count);
                            //Console.Write(count);
                        }
                    }
                    //Console.WriteLine();
                    //Console.Read();
                }
            }

            return pos;
        }

        static int getMinDist(List<int> pos1, List<int> pos2)
        {
            //int i, j;
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            int min = Math.Abs(pos1.ElementAt(0) - pos2.ElementAt(0));
            for (int i = 0, j = 0; i < pos1.Count && j < pos2.Count; )
            {
                int p1 = pos1.ElementAt(i), p2 = pos2.ElementAt(j);
                int diff = Math.Abs(p1 - p2);
                if (diff < min)
                {
                    min = diff;
                }
                if (p1 < p2)
                    i++;
                else
                    j++;
            }
            return min;
        }

        static double kMin2(List<int> pos1, List<int> pos2, int k)
        {
            int[] dist = new int[k];
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            for (int i = 0; i < k; i++)
            {
                dist[i] = 10000000;
            }
            for (int i = 0; i < pos1.Count; i++)
            {
                int a = pos1.ElementAt(i);
                for (int j = 0; j < pos2.Count; j++)
                {
                    int b = pos2.ElementAt(j);
                    int diff = Math.Abs(a - b);
                    int idx = k - 1;
                    while (idx >= 0 && dist[idx] > diff)
                    {
                        idx--;
                    }
                    if (idx == k - 1)
                        continue;
                    for (int l = k - 1; l > idx + 1; l--)
                    {
                        dist[l] = dist[l - 1];
                    }
                    dist[idx + 1] = diff;
                }
            }
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                //Console.Write(dist[i]+"\t");
                if (dist[i] != 0 && dist[i] != 10000000 && dist[i] <= 5)
                    sum += 1.0 / dist[i];
            }
            //Console.WriteLine(sum);
            //Console.Read();
            return sum;
        }

        static double kMin3(List<int> pos1, List<int> pos2, int k, int d)
        {
            int[] dist = new int[k];
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            for (int i = 0; i < k; i++)
            {
                dist[i] = 10000000;
            }
            for (int i = 0; i < pos1.Count; i++)
            {
                int a = pos1.ElementAt(i);
                for (int j = 0; j < pos2.Count; j++)
                {
                    int b = pos2.ElementAt(j);
                    int diff = Math.Abs(a - b);
                    int idx = k - 1;
                    while (idx >= 0 && dist[idx] > diff)
                    {
                        idx--;
                    }
                    if (idx == k - 1)
                        continue;
                    for (int l = k - 1; l > idx + 1; l--)
                    {
                        dist[l] = dist[l - 1];
                    }
                    dist[idx + 1] = diff;
                }
            }
            double sum = 0;
            for (int i = 0; i < k; i++)
            {
                //Console.Write(dist[i]+"\t");
                if (dist[i] != 0 && dist[i] != 10000000 && dist[i] <= 5)
                    sum += 1.0 / (Math.Abs(dist[i] - d) + 1);
            }
            //Console.WriteLine(sum);
            //Console.Read();
            return sum;
        }

        static int kMin(List<int> pos1, List<int> pos2, int k)
        {
            int[] dist = new int[k];
            if (pos1.Count == 0 || pos2.Count == 0)
                return 30;
            for (int i = 0; i < k; i++)
            {
                dist[i] = 10000000;
            }
            for (int i = 0; i < pos1.Count; i++)
            {
                int a = pos1.ElementAt(i);
                for (int j = 0; j < pos2.Count; j++)
                {
                    int b = pos2.ElementAt(j);
                    int diff = Math.Abs(a - b);
                    int idx = k - 1;
                    while (idx >= 0 && dist[idx] > diff)
                    {
                        idx--;
                    }
                    if (idx == k - 1)
                        continue;
                    for (int l = k - 1; l > idx + 1; l--)
                    {
                        dist[l] = dist[l - 1];
                    }
                    dist[idx + 1] = diff;
                }
            }
            int sum = dist[0];
            for (int i = 1; i < k; i++)
            {
                if (dist[i] > 9)
                    sum += 10;
                else
                    sum += dist[i];
            }
            return sum;
        }

        static int getMaxDist(List<int> pos1, List<int> pos2)
        {
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            int first1 = pos1.ElementAt(0), first2 = pos2.ElementAt(0), last1 = pos1.ElementAt(pos1.Count - 1), last2 = pos2.ElementAt(pos2.Count - 1);
            int d1 = Math.Abs(last1 - first2), d2 = Math.Abs(last2 - first1);
            return d1 > d2 ? d1 : d2;
        }

        static double getAvgDist(List<int> pos1, List<int> pos2)
        {
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            int sum = 0, count = 0;
            foreach (int p1 in pos1)
            {
                foreach (int p2 in pos2)
                {
                    count++;
                    sum += Math.Abs(p1 - p2);
                }
            }
            return (double)sum / count;
        }

        static double kMin2_NoCutoff(List<int> pos1, List<int> pos2)
        {
            if (pos1.Count == 0 || pos2.Count == 0)
                return 0;
            double sum = 0;
            foreach (int p1 in pos1)
            {
                foreach (int p2 in pos2)
                {

                    sum += 1.0 / Math.Abs(p1 - p2);
                }
            }
            return sum;
        }


        static int treeDist(string segmentedQ, int start, int end)
        {
            //char []space = {' '};
            //string q = segmentedQ.Replace("(","");
            //q = q.Replace(")","");
            //string[] words = q.Split(space,StringSplitOptions.RemoveEmptyEntries);
            //int p1 = segmentedQ.IndexOf("(" + words[index1] + ")");
            //int p2 = segmentedQ.IndexOf("(" + words[index2] + ")", p1+1);
            //int start = p1 + 1;
            //int end = p2;
            int level = 0, common_ancestor_level = 0;
            for (int i = start; i <= end; i++)
            {
                char ch = segmentedQ[i];
                if (ch == ')')
                {
                    level--;
                    if (level < common_ancestor_level)
                        common_ancestor_level = level;
                }
                else if (ch == '(')
                    level++;
            }
            return level - 2 * common_ancestor_level;
        }

        static double pmi(string ti, string tj, Dictionary<string, double> prob)
        {
            ti = ti.ToLower();
            Stemmer s = new Stemmer();
            char[] arr = ti.ToCharArray();
            s.add(arr, ti.Length);
            s.stem();
            ti = s.ToString();

            tj = tj.ToLower();
            Stemmer s2 = new Stemmer();
            char[] arr2 = tj.ToCharArray();
            s2.add(arr2, tj.Length);
            s2.stem();
            tj = s2.ToString();
            double pmi = 0;
            if (!prob.ContainsKey(ti + " " + tj) && !prob.ContainsKey(tj + " " + ti))
            {
                return 0;
            }
            if (prob.ContainsKey(ti + " " + tj))
            {
                pmi += prob[ti + " " + tj];
            }
            if (prob.ContainsKey(tj + " " + ti))
            {
                pmi += prob[tj + " " + ti];
            }
            if (!prob.ContainsKey(ti) || !prob.ContainsKey(tj))
                return 0;
            pmi /= (prob[ti] * prob[tj]);
            pmi = Math.Log(pmi, 2);
            return pmi;
        }

        static double hoeffding(string ti, string tj, Dictionary<string, double> hoeff)
        {
            ti = ti.ToLower();
            Stemmer s = new Stemmer();
            char[] arr = ti.ToCharArray();
            s.add(arr, ti.Length);
            s.stem();
            ti = s.ToString();

            tj = tj.ToLower();
            Stemmer s2 = new Stemmer();
            char[] arr2 = tj.ToCharArray();
            s2.add(arr2, tj.Length);
            s2.stem();
            tj = s2.ToString();

            string str1 = ti + " " + tj;
            string str2 = tj + " " + ti;
            double max = 0;
            if (hoeff.ContainsKey(str1))
                max = hoeff[str1];
            if (hoeff.ContainsKey(str2))
                max = hoeff[str2] > max ? hoeff[str2] : max;
            return max;
        }

        static void combineRanks(double[][] scores, int pos)
        {
            double[][] newScores = new double[4][];

            for (int i = 0; i < 4; i++)
            {
                newScores[i] = new double[pos];
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < pos; j++)
                {
                    newScores[i][j] = 1.0 / (j + 2);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < pos; j++)
                {
                    int rank = 1;
                    for (int k = 0; k < pos; k++)
                    {
                        if (j == k)
                            continue;
                        if (scores[i][k] > scores[i][j])
                            rank++;
                    }
                    newScores[i][j] += 4.0 / (rank + 1);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < pos; j++)
                {
                    scores[i][j] = newScores[i][j];
                }
            }
        }

        static void Main(string[] args)
        {
            int K = 30;//Number of documents to be re-ranked
            string fileName = @"MishraWikiNestedSeg\" + args[0];//Nested segmentation
            args[0] = args[0].Substring(0, args[0].Length - 4);
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;

                int line_num = 0;
                char[] space = { ' ' };
                while ((line = sr.ReadLine()) != null)
                {
                    string q = line.Replace("(", "");
                    q = q.Replace(")", "");
                    line_num++;
                    Console.Write(line_num + "\r");
                    string[] words = q.Split(space, StringSplitOptions.RemoveEmptyEntries);
                    int n = words.Length;
                    int[] indices = new int[n];
                    int p = -1;
                    for (int i = 0; i < n; i++)
                    {
                        p = line.IndexOf("(" + words[i] + ")", p + 1);
                        indices[i] = p;
                    }
                    string[] urls = new string[K];
                    string[] ratings = new string[K];
                    double[][] scores = new double[4][];
                    double[] tempScore = new double[4];
                    for (int i = 0; i < 4; i++)
                        scores[i] = new double[K];
                    int pos = 0;
                    string infile = "Top 30/query_" + line_num / 100 + (line_num / 10) % 10 + line_num % 10 + "_gId_" + line_num * 2 + "_top_" + K + "_results_anno.txt";//Top 30 documents
                    using (StreamReader sr2 = new StreamReader(infile))
                    {
                        string line2;
                        sr2.ReadLine();
                        while ((line2 = sr2.ReadLine()) != null)
                        {
                            urls[pos] = line2.Substring(0, line2.IndexOf('\t')) + ".txt";
                            ratings[pos] = line2.Substring(line2.LastIndexOf('\t'));
                            for (int i = 0; i < 4; i++)
                            {
                                scores[i][pos] = 0;
                            }
                            int l_d = docLength(urls[pos]);
                            if (l_d == 0)
                                continue;
                            List<List<int>> wordPositions = new List<List<int>>();
                            for (int i = 0; i < words.Length; i++)
                            {
                                wordPositions.Add(positions(words[i], urls[pos]));
                            }
                            for (int i = 0; i < n - 1; i++)
                            {
                                for (int j = i + 1; j < n; j++)
                                {
                                    double docDist_i_j_Min = kMin3(wordPositions.ElementAt(i), wordPositions.ElementAt(j), 3, j - i);
                                    double docDist_i_j_Min2 = kMin2(wordPositions.ElementAt(i), wordPositions.ElementAt(j), 3);
                                    int treeDist_i_j = treeDist(line, indices[i] + 1, indices[j]);                                    
                                    //Uncomment to run flat segmentation evaluation
                                   
                                    //if (treeDist_i_j == 2)
                                    //    treeDist_i_j = 1;
                                    //else
                                    //    continue;
                                   
                                    tempScore[0] = docDist_i_j_Min2;//doc-dist
                                    tempScore[1] = docDist_i_j_Min2 / (j - i);//query-doc
                                    if (treeDist_i_j <= 5)
                                    {
                                        tempScore[2] = tempScore[0] / treeDist_i_j;//doc-tree
                                    }
                                    else
                                        tempScore[2] = 0;
                                    tempScore[3] = docDist_i_j_Min / treeDist_i_j;//hybrid
                                    for (int k = 0; k < 4; k++)
                                    {
                                        if (tempScore[k] != 0)
                                            scores[k][pos] += tempScore[k];
                                    }
                                }
                            }
                            pos++;
                        }
                    }
                    for (int i = 0; i < pos; i++)
                        urls[i] = urls[i] + ratings[i];
                    string[] tempUrls = new string[K];
                    combineRanks(scores, pos);
                    for (int i = 0; i < 4; i++)
                    {
                        for (int j = 0; j < pos; j++)
                        {
                            tempUrls[j] = string.Copy(urls[j]);
                        }
                        Array.Sort(scores[i], tempUrls, 0, pos);
                        Array.Reverse(scores[i], 0, pos);
                        Array.Reverse(tempUrls, 0, pos);
                        using (StreamWriter sw = new StreamWriter("RerankedDocs/" + args[0] + "_" + (i + 1) + "_query_" + line_num / 100 + (line_num / 10) % 10 + line_num % 10 + ".txt"))
                        {
                            for (int j = 0; j < pos; j++)
                            {
                                sw.WriteLine(tempUrls[j] + "\t" + scores[i][j]);
                            }
                        }
                    }
                }
            }
        }

    }
}

