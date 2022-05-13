using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace AU_Document
{
    class DocMataData
    {
        DataTable dataTable;
        DataTable convertedDataTable;

        // read Document MataData from the staging table
        public DataTable ReadAllDocMataData(List<string> userInputs)
        {
            // change config to support main database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig("Main", userInputs);

            dataTable = new DataTable();

            try
            {
                string query = "select * from staging.AU_DocMataData /*where SEQ_NO > 116590*/ order by SEQ_NO";

                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                // create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // this will query your database and return the result to your datatable
                da.Fill(dataTable);

                conn.Close();
                da.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dataTable;
        }

        // this function will check if the Physical file name exists in the MataData staging table and update encode if exists
        public void UpdateStagingRecords(ProgressBar progressBar1, string[,] allfiles2d, List<string> userInputs, string batch)
        {
            // change config to support main database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig("Main", userInputs);
            // reset ProgressBar
            Utility objUtility = new Utility();
            objUtility.ResetProgressBar(progressBar1);
            // Set Maximum to the total number of files to copy.
            progressBar1.Maximum = dataTable.Rows.Count;

            foreach (DataRow rowST in dataTable.Rows)
            {
                string fileNamehMata = rowST["OutputFileName"].ToString();

                for (int i = 0; i < allfiles2d.GetLength(0); i++)
                {
                    // update staging table if physical file name and metadata file name matach
                    if (allfiles2d[i, 2] == fileNamehMata)
                    {
                        try
                        {
                            // storing document path and decode values to the staging table
                            SqlConnection conn = new SqlConnection(connString);
                            SqlCommand cmd = new SqlCommand("Update staging.AU_DocMataData set FullPath = @rFullPath" +
                                                                                  ", PathWithoutName = @rPathWithoutName" +
                                                                                  ", NameWithoutPath = @rNameWithoutPath" +
                                                                                  ", FileExe = @rFileExe" +
                                                                                  ", FileDecode = @rFileDecode " +
                                                                                  ", Batch = @rBatch " +
                                                                                  "where trim(OutputFileName) = @rOutputFileName", conn);
                            conn.Open();

                            // document mata data and decode values 
                            cmd.Parameters.Add("@rFullPath", SqlDbType.VarChar).Value = allfiles2d[i, 0];
                            cmd.Parameters.Add("@rPathWithoutName", SqlDbType.VarChar).Value = allfiles2d[i, 1];
                            cmd.Parameters.Add("@rNameWithoutPath", SqlDbType.VarChar).Value = allfiles2d[i, 2];
                            cmd.Parameters.Add("@rFileExe", SqlDbType.VarChar).Value = allfiles2d[i, 3];
                            cmd.Parameters.Add("@rFileDecode", SqlDbType.Text).Value = ""; //allfiles2d[i, 4];                            
                            cmd.Parameters.Add("@rOutputFileName", SqlDbType.VarChar).Value = allfiles2d[i, 2].ToString().Trim();
                            cmd.Parameters.Add("@rBatch", SqlDbType.VarChar).Value = batch;

                            cmd.ExecuteNonQuery();
                            conn.Close();
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Perform the increment on the ProgressBar.
                        progressBar1.PerformStep();
                    }
                }
            }
        }

        // this function create an array with all valid converted document data that ready to insert to ARMNet interface tables.
        public DataTable ReadConvertedStagingData(List<string> userInputs, Boolean loadAllBatches)
        {
            // change config to support main database
            DataBaseConfig objDBCongig = new DataBaseConfig();
            string connString = objDBCongig.GetDatabaseConfig("Main", userInputs);
            // conditonally assign delete all 
            string loadAll = loadAllBatches ? "" : "and Batch = '" + userInputs[7] + "'";

            convertedDataTable = new DataTable();

            try
            {
                string query = "SELECT '{' + lower(cast(newid() as varchar(38))) + '}' as [DMR_ID],'{}' as [Ownership], 'Conversion_AU-D | Migrated on ' + lower(cast(cast(getdate() as date) as varchar(10))) as [Note], YMR_IDLink_ARMNet[DMR_IDLink_Code], " +
                               "XLK_ID[DMR_IDLink_XLK], '{5385c7db-840d-4b62-bf1b-a76902e5f2f8}'[DMR_IDLink_CreatedBy], '{5457433b-48a9-468b-b58d-5b98f83ea995}' [DMR_IDLink_XSYSdoc], 0 [DMR_DocLinkType], cast(RequestDate as datetime)[DMR_DateCreated], " +
                               "substring(PathWithoutName, 0, len(PathWithoutName))[DMR_UNCPath], substring(NameWithoutPath, 0, len(NameWithoutPath) - len(FileExe))[DMR_FileOriginalName], substring(NameWithoutPath, 0, len(NameWithoutPath) - len(FileExe))[DMR_FileDisplayName]," +
                               "FileExe[DMR_FileDisplayExtension], XLK_AlternateDetail[DMR_FileSubject], '{' + lower(cast(newid() as varchar(38))) + '}' as [DPK_ID], FileExe[DPK_FileDisplayExtension],FileDecode[DPK_Base64Data] " +
                               "from staging.AU_DocMataData join iO_Keys_MasterReference on YMR_IDLink_Foreign = cast(cast(AccountNumber as bigint) as varchar(40)) left join iO_Control_LinkMaster on replace(XLK_AlternateDetail, ' ', '') = [Description] and XLK_IDLink_XLKc = 14 " +
                               " where YMR_IDLink_XFK = '{ef3baea0-8d36-4c5a-8a2d-6ce9e119335b}' and FileDecode is not null " + loadAll;

                SqlConnection conn = new SqlConnection(connString);
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                // create data adapter
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                // this will query your database and return the result to your datatable
                da.Fill(convertedDataTable);

                conn.Close();
                da.Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return convertedDataTable;
        }

        public string ContertedRecordCount()
        {
            return convertedDataTable.Rows.Count.ToString();
        }
    }
}
