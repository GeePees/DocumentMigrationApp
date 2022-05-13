using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AU_Document
{
    class DocFiles
    {
        // read every end points in the given file directory
        public string[,] ReadAllDocuments(ProgressBar progressBar1, string directoryPath)
        {
            // instasiate utility 
            Utility objUtility = new Utility();

            string[] allfiles = null;
            string[,] allfiles2d;

            // reset ProgressBar            
            objUtility.ResetProgressBar(progressBar1);

            try
            {
                allfiles = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

                // add document data to this 2d array temporarily
                allfiles2d = new string[allfiles.Length, 5];
                var i = 0;
                // Set Maximum to the total number of files to copy.
                progressBar1.Maximum = allfiles.Length;

                foreach (string files in allfiles)
                {
                    // add path + name of the file
                    allfiles2d[i, 0] = files;
                    // add path without name of the file
                    allfiles2d[i, 1] = files.Substring(0, (files.Length - objUtility.Reverse(files).IndexOf('\\')));
                    // add file name without path
                    allfiles2d[i, 2] = files.Substring(allfiles2d[i, 1].Length);
                    // add file extension
                    allfiles2d[i, 3] = files.Substring(files.Length - objUtility.Reverse(files).IndexOf('.'));
                    // convert doc to the 64 bit and store 
                    //allfiles2d[i, 4] = ToBase64String(files);  // Removed cos too much memory consumed.                  

                    // Perform the increment on the ProgressBar.
                    progressBar1.PerformStep();

                    // increment
                    i++;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            // progress bar label - end
            //label9.Text = "";
            return allfiles2d;
        }
    }
}
