1.Copy the following to the Debug folder :
  Folder named MishraWikiNestedSeg containing the files storing nested segmented queries
  Folder named Web_Documents conatining the document collection
  Folder named Top 30 containing top K results for each query. The files are named query_<query number>_gId_<query number*2>_top_<K>_results_anno.txt
2.Create a folder named RerankedDocs in the Debug folder
3.Start Visual Studio and run the solution with command line argument as the name of the file containing queries whose results have to be re-ranked.
  The parameters of nested segmentation have been set in the following lines:
   Number of documents to be re-ranked - Line 417 (currently set to 30)
   Weight given to new rank : Line 402 (currently set to 4.0)
   Weight given to old rank : Line 386 (currently set to 1.0)
   Tree distance cutoff : Line 485 (currently set to 5)
   Number of minimum distances(k) : Line 473 and Line 474 (currently set to 3)
   Window size(win) : Line 194 and Line 153 (currently set to 5)
4.The output files are RerankedDocs2/<input file name>_<i>_query_<j> where i is 1,2,3 or 4 and j is a 3 digit string starting varying from 001 to number of queries.
  The file <input file name>_i_query_j stores the re-ranked documents for the jth query. The value of i gives the re-ranking scheme used.
  i = 1 : Doc
  i = 2 : Query
  i = 3 : Tree
  i = 4 : Hybrid

   
 
  